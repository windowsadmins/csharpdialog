using csharpDialog.Core.Models;
using System.Text.RegularExpressions;

namespace csharpDialog.Core.Services;

/// <summary>
/// Parses commands from the command file using swiftDialog-compatible syntax
/// </summary>
public class CommandParser : ICommandParser
{
    private static readonly string[] ValidCommands = {
        "title", "message", "progress", "progresstext", "progressincrement", "progressreset", 
        "quit", "listitem", "list", "config", "style", "theme", "execute", "executepowershell", 
        "executeoutput", "width", "height", "position", "icon", "image", "button1text", "button2text"
    };

    /// <summary>
    /// Parse a single command line into a Command object
    /// </summary>
    public Command? ParseCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine) || commandLine.StartsWith("#"))
        {
            return null; // Skip empty lines and comments
        }

        var trimmedLine = commandLine.Trim();
        
        // Basic command format: "command: value"
        var colonIndex = trimmedLine.IndexOf(':');
        if (colonIndex == -1)
        {
            return null; // Invalid format
        }

        var commandType = trimmedLine[..colonIndex].Trim().ToLowerInvariant();
        var commandValue = trimmedLine[(colonIndex + 1)..].Trim();

        if (!ValidCommands.Contains(commandType))
        {
            return null; // Unknown command
        }

        var command = new Command
        {
            Type = commandType,
            Value = commandValue,
            RawCommand = commandLine,
            Timestamp = DateTime.UtcNow
        };

        // Parse complex commands with parameters (e.g., listitem)
        if (commandType == "listitem" || commandType == "list")
        {
            ParseComplexCommand(command, commandValue);
        }

        return command;
    }

    /// <summary>
    /// Parse multiple command lines
    /// </summary>
    public IEnumerable<Command> ParseCommands(string[] commandLines)
    {
        var commands = new List<Command>();
        
        foreach (var line in commandLines)
        {
            var command = ParseCommand(line);
            if (command != null)
            {
                commands.Add(command);
            }
        }

        return commands;
    }

    /// <summary>
    /// Validate if a command line is properly formatted
    /// </summary>
    public bool IsValidCommand(string commandLine)
    {
        return ParseCommand(commandLine) != null;
    }

    /// <summary>
    /// Parse complex commands with multiple parameters
    /// Examples:
    /// - listitem: title: Install Software, status: success, statustext: Completed
    /// - listitem: index: 0, status: progress, statustext: Installing...
    /// </summary>
    private static void ParseComplexCommand(Command command, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // Handle special cases
        if (command.Type == "list" && value.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            command.Parameters["action"] = "clear";
            return;
        }

        // Parse comma-separated parameters: "key: value, key2: value2"
        var parameters = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var param in parameters)
        {
            var paramTrimmed = param.Trim();
            var paramColonIndex = paramTrimmed.IndexOf(':');
            
            if (paramColonIndex > 0)
            {
                var paramKey = paramTrimmed[..paramColonIndex].Trim().ToLowerInvariant();
                var paramValue = paramTrimmed[(paramColonIndex + 1)..].Trim();
                
                // Remove quotes if present
                if (paramValue.StartsWith('"') && paramValue.EndsWith('"'))
                {
                    paramValue = paramValue[1..^1];
                }
                
                command.Parameters[paramKey] = paramValue;
            }
        }

        // For backward compatibility, if no parameters were found, treat the entire value as title
        if (command.Parameters.Count == 0 && command.Type == "listitem")
        {
            command.Parameters["title"] = value;
        }
    }
}

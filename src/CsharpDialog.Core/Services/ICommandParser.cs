using csharpDialog.Core.Models;

namespace csharpDialog.Core.Services;

/// <summary>
/// Interface for parsing commands from the command file
/// </summary>
public interface ICommandParser
{
    /// <summary>
    /// Parse a single command line into a Command object
    /// </summary>
    /// <param name="commandLine">The raw command line</param>
    /// <returns>Parsed command or null if invalid</returns>
    Command? ParseCommand(string commandLine);

    /// <summary>
    /// Parse multiple command lines
    /// </summary>
    /// <param name="commandLines">Array of command lines</param>
    /// <returns>Collection of valid commands</returns>
    IEnumerable<Command> ParseCommands(string[] commandLines);

    /// <summary>
    /// Validate if a command line is properly formatted
    /// </summary>
    /// <param name="commandLine">The command line to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidCommand(string commandLine);
}

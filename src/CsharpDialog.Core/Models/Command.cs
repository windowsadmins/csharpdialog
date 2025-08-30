namespace csharpDialog.Core.Models;

/// <summary>
/// Represents a command parsed from the command file
/// </summary>
public class Command
{
    /// <summary>
    /// The command type (e.g., "title", "message", "progress", "quit")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The command argument/value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Additional parameters for complex commands (e.g., listitem parameters)
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// The raw command line for logging purposes
    /// </summary>
    public string RawCommand { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the command was parsed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Type}: {Value} [{string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"))}]";
    }
}

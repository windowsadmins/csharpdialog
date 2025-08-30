using csharpDialog.Core.Models;

namespace csharpDialog.Core.Services;

/// <summary>
/// Interface for monitoring command file changes and processing commands
/// </summary>
public interface ICommandFileMonitor : IDisposable
{
    /// <summary>
    /// Event fired when new commands are received
    /// </summary>
    event EventHandler<CommandReceivedEventArgs>? CommandReceived;

    /// <summary>
    /// Event fired when an error occurs during monitoring
    /// </summary>
    event EventHandler<CommandFileErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Path to the command file being monitored
    /// </summary>
    string CommandFilePath { get; }

    /// <summary>
    /// Whether the monitor is currently active
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Start monitoring the command file
    /// </summary>
    /// <param name="commandFilePath">Path to the command file</param>
    Task StartMonitoringAsync(string commandFilePath);

    /// <summary>
    /// Stop monitoring the command file
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// Create the command file if it doesn't exist
    /// </summary>
    /// <param name="commandFilePath">Path to create the file at</param>
    Task CreateCommandFileAsync(string commandFilePath);

    /// <summary>
    /// Clear the command file contents
    /// </summary>
    Task ClearCommandFileAsync();
}

/// <summary>
/// Event arguments for command received events
/// </summary>
public class CommandReceivedEventArgs : EventArgs
{
    public Command Command { get; }
    public DateTime Timestamp { get; }

    public CommandReceivedEventArgs(Command command)
    {
        Command = command;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for command file error events
/// </summary>
public class CommandFileErrorEventArgs : EventArgs
{
    public string Message { get; }
    public Exception? Exception { get; }
    public DateTime Timestamp { get; }

    public CommandFileErrorEventArgs(string message, Exception? exception = null)
    {
        Message = message;
        Exception = exception;
        Timestamp = DateTime.UtcNow;
    }
}

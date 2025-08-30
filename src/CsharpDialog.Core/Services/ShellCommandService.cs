using System.Diagnostics;
using System.Text;

namespace csharpDialog.Core.Services;

/// <summary>
/// Service for executing shell commands with real-time output capture
/// </summary>
public class ShellCommandService
{
    /// <summary>
    /// Event raised when command output is received
    /// </summary>
    public event EventHandler<CommandOutputEventArgs>? OutputReceived;

    /// <summary>
    /// Event raised when command execution completes
    /// </summary>
    public event EventHandler<CommandCompletedEventArgs>? CommandCompleted;

    /// <summary>
    /// Event raised when command execution encounters an error
    /// </summary>
    public event EventHandler<CommandErrorEventArgs>? CommandError;

    /// <summary>
    /// Execute a shell command asynchronously with real-time output
    /// </summary>
    public async Task<CommandExecutionResult> ExecuteCommandAsync(string command, string? workingDirectory = null, int timeoutMs = 30000)
    {
        return await ExecuteCommandInternalAsync(command, "cmd", "/c", workingDirectory, timeoutMs);
    }

    /// <summary>
    /// Execute a PowerShell script asynchronously with real-time output
    /// </summary>
    public async Task<CommandExecutionResult> ExecutePowerShellAsync(string script, string? workingDirectory = null, int timeoutMs = 30000)
    {
        return await ExecuteCommandInternalAsync(script, "powershell", "-Command", workingDirectory, timeoutMs);
    }

    /// <summary>
    /// Execute a command and capture all output without real-time streaming
    /// </summary>
    public async Task<CommandExecutionResult> ExecuteAndCaptureAsync(string command, string? workingDirectory = null, int timeoutMs = 30000)
    {
        var result = await ExecuteCommandInternalAsync(command, "cmd", "/c", workingDirectory, timeoutMs, captureOnly: true);
        return result;
    }

    /// <summary>
    /// Internal method for executing commands with various shells
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteCommandInternalAsync(
        string command, 
        string shell, 
        string shellArgs, 
        string? workingDirectory, 
        int timeoutMs,
        bool captureOnly = false)
    {
        var result = new CommandExecutionResult
        {
            Command = command,
            Shell = shell,
            StartTime = DateTime.UtcNow
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = shell;
            process.StartInfo.Arguments = $"{shellArgs} \"{command.Replace("\"", "\\\"")}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            // Set up output handlers
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBuilder.AppendLine(args.Data);
                    if (!captureOnly)
                    {
                        OutputReceived?.Invoke(this, new CommandOutputEventArgs
                        {
                            Command = command,
                            Output = args.Data,
                            IsError = false,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    errorBuilder.AppendLine(args.Data);
                    if (!captureOnly)
                    {
                        OutputReceived?.Invoke(this, new CommandOutputEventArgs
                        {
                            Command = command,
                            Output = args.Data,
                            IsError = true,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
            };

            // Start the process
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            using var cancellationToken = new CancellationTokenSource(timeoutMs);
            try
            {
                await process.WaitForExitAsync(cancellationToken.Token);
                result.ExitCode = process.ExitCode;
                result.Success = process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                result.TimedOut = true;
                result.Success = false;
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // Ignore kill errors
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.StandardOutput = outputBuilder.ToString();
            result.StandardError = errorBuilder.ToString();

            // Raise completion event
            CommandCompleted?.Invoke(this, new CommandCompletedEventArgs
            {
                Command = command,
                Result = result
            });

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.Success = false;
            result.Exception = ex;
            result.StandardError = ex.Message;

            CommandError?.Invoke(this, new CommandErrorEventArgs
            {
                Command = command,
                Exception = ex,
                Message = ex.Message
            });

            return result;
        }
    }

    /// <summary>
    /// Validate if a command is safe to execute (basic security check)
    /// </summary>
    public static bool IsCommandSafe(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // Basic blacklist of dangerous commands
        var dangerousCommands = new[]
        {
            "format", "del", "rmdir", "rd", "erase", "attrib", "fdisk",
            "diskpart", "shutdown", "restart", "reboot", "net user",
            "net localgroup", "reg delete", "reg add", "sc delete",
            "taskkill", "wmic", "powercfg", "bcdedit"
        };

        var lowerCommand = command.ToLowerInvariant();
        return !dangerousCommands.Any(dangerous => lowerCommand.Contains(dangerous));
    }

    /// <summary>
    /// Get a safe version of a command by removing potentially dangerous elements
    /// </summary>
    public static string SanitizeCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return string.Empty;

        // Remove potentially dangerous characters
        var sanitized = command
            .Replace("&", "")
            .Replace("|", "")
            .Replace(";", "")
            .Replace(">", "")
            .Replace("<", "")
            .Replace(">>", "");

        return sanitized.Trim();
    }
}

/// <summary>
/// Result of command execution
/// </summary>
public class CommandExecutionResult
{
    public string Command { get; set; } = string.Empty;
    public string Shell { get; set; } = string.Empty;
    public int ExitCode { get; set; }
    public bool Success { get; set; }
    public bool TimedOut { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public override string ToString()
    {
        return $"Command: {Command}, Success: {Success}, ExitCode: {ExitCode}, Duration: {Duration.TotalMilliseconds}ms";
    }
}

/// <summary>
/// Event args for command output
/// </summary>
public class CommandOutputEventArgs : EventArgs
{
    public string Command { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event args for command completion
/// </summary>
public class CommandCompletedEventArgs : EventArgs
{
    public string Command { get; set; } = string.Empty;
    public CommandExecutionResult Result { get; set; } = new();
}

/// <summary>
/// Event args for command errors
/// </summary>
public class CommandErrorEventArgs : EventArgs
{
    public string Command { get; set; } = string.Empty;
    public Exception Exception { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

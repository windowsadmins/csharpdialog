using csharpDialog.Core.Models;
using System.Text;

namespace csharpDialog.Core.Services;

/// <summary>
/// Monitors a command file for changes and processes new commands in real-time
/// Compatible with swiftDialog command syntax
/// </summary>
public class CommandFileMonitor : ICommandFileMonitor
{
    private readonly ICommandParser _commandParser;
    private FileSystemWatcher? _fileWatcher;
    private string _commandFilePath = string.Empty;
    private long _lastReadPosition = 0;
    private readonly object _lockObject = new();
    private bool _disposed = false;
    private Timer? _processingTimer;
    private readonly Queue<string> _pendingLines = new();

    public event EventHandler<CommandReceivedEventArgs>? CommandReceived;
    public event EventHandler<CommandFileErrorEventArgs>? ErrorOccurred;

    public string CommandFilePath => _commandFilePath;
    public bool IsMonitoring => _fileWatcher?.EnableRaisingEvents == true;

    public CommandFileMonitor(ICommandParser commandParser)
    {
        _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
    }

    /// <summary>
    /// Start monitoring the specified command file
    /// </summary>
    public async Task StartMonitoringAsync(string commandFilePath)
    {
        if (string.IsNullOrWhiteSpace(commandFilePath))
            throw new ArgumentException("Command file path cannot be empty", nameof(commandFilePath));

        StopMonitoring();

        _commandFilePath = Path.GetFullPath(commandFilePath);
        
        try
        {
            // Create the command file if it doesn't exist
            await CreateCommandFileAsync(_commandFilePath);

            // Get initial file size to start reading from the end
            var fileInfo = new FileInfo(_commandFilePath);
            _lastReadPosition = fileInfo.Exists ? fileInfo.Length : 0;

            // Set up file system watcher
            var directory = Path.GetDirectoryName(_commandFilePath);
            var fileName = Path.GetFileName(_commandFilePath);

            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Invalid command file path");

            _fileWatcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Error += OnFileWatcherError;

            // Set up timer for processing queued commands (debouncing)
            _processingTimer = new Timer(ProcessPendingCommands, null, Timeout.Infinite, Timeout.Infinite);

            OnErrorOccurred($"Started monitoring command file: {_commandFilePath}");
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Failed to start monitoring: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Stop monitoring the command file
    /// </summary>
    public void StopMonitoring()
    {
        lock (_lockObject)
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;

            _processingTimer?.Dispose();
            _processingTimer = null;

            _pendingLines.Clear();
            _lastReadPosition = 0;
        }
    }

    /// <summary>
    /// Create the command file if it doesn't exist
    /// </summary>
    public async Task CreateCommandFileAsync(string commandFilePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(commandFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(commandFilePath))
            {
                await File.WriteAllTextAsync(commandFilePath, $"# csharpDialog command file created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n# Commands will be processed as they are appended to this file\n\n");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Failed to create command file: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Clear the command file contents
    /// </summary>
    public async Task ClearCommandFileAsync()
    {
        if (string.IsNullOrEmpty(_commandFilePath))
            return;

        try
        {
            await File.WriteAllTextAsync(_commandFilePath, string.Empty);
            lock (_lockObject)
            {
                _lastReadPosition = 0;
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Failed to clear command file: {ex.Message}", ex);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid file changes by queuing them for later processing
        _processingTimer?.Change(100, Timeout.Infinite); // Process after 100ms delay
    }

    private void ProcessPendingCommands(object? state)
    {
        try
        {
            var newLines = ReadNewLines();
            
            foreach (var line in newLines)
            {
                var command = _commandParser.ParseCommand(line);
                if (command != null)
                {
                    OnCommandReceived(command);
                }
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred($"Error processing commands: {ex.Message}", ex);
        }
    }

    private List<string> ReadNewLines()
    {
        var newLines = new List<string>();

        lock (_lockObject)
        {
            try
            {
                if (!File.Exists(_commandFilePath))
                    return newLines;

                var fileInfo = new FileInfo(_commandFilePath);
                var currentLength = fileInfo.Length;

                if (currentLength <= _lastReadPosition)
                    return newLines; // No new content

                using var fileStream = new FileStream(_commandFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(_lastReadPosition, SeekOrigin.Begin);

                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        newLines.Add(line);
                    }
                }

                _lastReadPosition = fileStream.Position;
            }
            catch (IOException ex)
            {
                // File might be locked, try again later
                OnErrorOccurred($"IO error reading file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error reading new lines: {ex.Message}", ex);
            }
        }

        return newLines;
    }

    private void OnFileWatcherError(object sender, ErrorEventArgs e)
    {
        OnErrorOccurred($"File watcher error: {e.GetException().Message}", e.GetException());
    }

    private void OnCommandReceived(Command command)
    {
        CommandReceived?.Invoke(this, new CommandReceivedEventArgs(command));
    }

    private void OnErrorOccurred(string message, Exception? exception = null)
    {
        ErrorOccurred?.Invoke(this, new CommandFileErrorEventArgs(message, exception));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopMonitoring();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

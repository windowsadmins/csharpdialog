using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using csharpDialog.Core.Models;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace csharpDialog.Core.Services;

/// <summary>
/// Monitors Cimian (managedsoftwareupdate) progress similar to cimistatus
/// Provides real-time updates for software installation during first-run scenarios
/// </summary>
public class CimianMonitor : IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly Timer? _progressTimer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;
    private string _cimianLogPath = string.Empty;
    private long _lastLogPosition = 0;
    private DateTime _lastUpdateTime = DateTime.MinValue;
    
    // Events for progress updates
    public event EventHandler<CimianProgressEventArgs>? ProgressUpdated;
    public event EventHandler<CimianInstallEventArgs>? InstallStarted;
    public event EventHandler<CimianInstallEventArgs>? InstallCompleted;
    public event EventHandler<CimianErrorEventArgs>? ErrorOccurred;
    
    // Progress tracking
    private readonly List<CimianInstallItem> _installItems = new();
    private int _totalItems = 0;
    private int _completedItems = 0;
    
    // Cimian process monitoring
    private Process? _cimianProcess;
    private FileSystemWatcher? _logWatcher;
    
    public CimianMonitor(IDialogService dialogService)
    {
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Initialize Cimian paths and settings
        InitializeCimianPaths();
        
        // Create a timer for periodic updates (every 2 seconds)
        _progressTimer = new Timer(UpdateProgress, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }
    
    /// <summary>
    /// Starts monitoring Cimian progress for first-run software installation
    /// </summary>
    public async Task<bool> StartFirstRunMonitoringAsync()
    {
        try
        {
            // Check if we're in a first-run scenario
            if (!IsFirstRunScenario())
            {
                throw new InvalidOperationException("Not in a first-run scenario. Cimian monitoring is only for initial device setup.");
            }
            
            // Setup log monitoring
            if (!SetupLogMonitoring())
            {
                throw new InvalidOperationException("Could not setup Cimian log monitoring. Ensure Cimian is installed and running.");
            }
            
            // Check if Cimian is already running, if not try to start it
            if (!await EnsureCimianRunningAsync())
            {
                throw new InvalidOperationException("Could not start or detect running Cimian process.");
            }
            
            // Parse existing manifest to get expected installation items
            await ParseManifestAsync();
            
            // Start monitoring the log file
            StartLogFileMonitoring();
            
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
            {
                Message = $"Failed to start Cimian monitoring: {ex.Message}",
                Exception = ex
            });
            return false;
        }
    }
    
    /// <summary>
    /// Stops monitoring Cimian progress
    /// </summary>
    public void StopMonitoring()
    {
        _progressTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _logWatcher?.Dispose();
        _logWatcher = null;
        _cancellationTokenSource.Cancel();
    }
    
    /// <summary>
    /// Checks if the current session is a first-run scenario
    /// </summary>
    private static bool IsFirstRunScenario()
    {
        try
        {
            // Check for first logon indicators
            // 1. Check if user profile is being created for the first time
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var profileCreationMarker = Path.Combine(userProfilePath, "ntuser.dat");
            
            if (File.Exists(profileCreationMarker))
            {
                var creationTime = File.GetCreationTime(profileCreationMarker);
                // Consider first-run if profile was created within last 10 minutes
                if (DateTime.Now - creationTime < TimeSpan.FromMinutes(10))
                {
                    return true;
                }
            }
            
            // 2. Check registry for Cimian first-run markers
#if WINDOWS
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Cimian");
            if (key != null)
            {
                var firstRun = key.GetValue("FirstRun");
                if (firstRun != null && firstRun.ToString() == "1")
                {
                    return true;
                }
            }
#endif
            
            // 3. Check for bootstrap completion markers
            var bootstrapMarker = @"C:\ProgramData\Cimian\bootstrap_complete";
            if (File.Exists(bootstrapMarker))
            {
                var creationTime = File.GetCreationTime(bootstrapMarker);
                // Bootstrap completed recently
                if (DateTime.Now - creationTime < TimeSpan.FromHours(1))
                {
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            // If we can't determine, assume it's not first-run for safety
            return false;
        }
    }
    
    /// <summary>
    /// Initializes Cimian paths and configuration
    /// </summary>
    private void InitializeCimianPaths()
    {
        // Standard Cimian log locations
        var possibleLogPaths = new[]
        {
            @"C:\ProgramData\Cimian\Logs\managedsoftwareupdate.log",
            @"C:\Program Files\Cimian\Logs\managedsoftwareupdate.log",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Cimian\Logs\managedsoftwareupdate.log")
        };
        
        foreach (var path in possibleLogPaths)
        {
            if (File.Exists(path))
            {
                _cimianLogPath = path;
                break;
            }
        }
        
        // If no existing log found, use the standard location
        if (string.IsNullOrEmpty(_cimianLogPath))
        {
            _cimianLogPath = @"C:\ProgramData\Cimian\Logs\managedsoftwareupdate.log";
        }
    }
    
    /// <summary>
    /// Sets up log file monitoring for Cimian progress
    /// </summary>
    private bool SetupLogMonitoring()
    {
        try
        {
            var logDirectory = Path.GetDirectoryName(_cimianLogPath);
            if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
            {
                return false;
            }
            
            // Get current log file size to track new entries
            if (File.Exists(_cimianLogPath))
            {
                _lastLogPosition = new FileInfo(_cimianLogPath).Length;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Starts monitoring the Cimian log file for changes
    /// </summary>
    private void StartLogFileMonitoring()
    {
        var logDirectory = Path.GetDirectoryName(_cimianLogPath);
        var logFileName = Path.GetFileName(_cimianLogPath);
        
        if (string.IsNullOrEmpty(logDirectory) || string.IsNullOrEmpty(logFileName))
            return;
        
        _logWatcher = new FileSystemWatcher(logDirectory, logFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        
        _logWatcher.Changed += OnLogFileChanged;
    }
    
    /// <summary>
    /// Handles log file changes to parse new progress information
    /// </summary>
    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Debounce: ignore rapid successive changes
            if (DateTime.Now - _lastUpdateTime < TimeSpan.FromMilliseconds(500))
                return;
                
            _lastUpdateTime = DateTime.Now;
            
            // Read new log entries
            var newEntries = ReadNewLogEntries();
            foreach (var entry in newEntries)
            {
                ParseLogEntry(entry);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
            {
                Message = $"Error processing log file change: {ex.Message}",
                Exception = ex
            });
        }
    }
    
    /// <summary>
    /// Reads new log entries since last check
    /// </summary>
    private List<string> ReadNewLogEntries()
    {
        var entries = new List<string>();
        
        try
        {
            if (!File.Exists(_cimianLogPath))
                return entries;
            
            using var fileStream = new FileStream(_cimianLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var currentLength = fileStream.Length;
            
            if (currentLength <= _lastLogPosition)
                return entries;
            
            fileStream.Seek(_lastLogPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fileStream);
            
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    entries.Add(line);
                }
            }
            
            _lastLogPosition = currentLength;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
            {
                Message = $"Error reading log entries: {ex.Message}",
                Exception = ex
            });
        }
        
        return entries;
    }
    
    /// <summary>
    /// Parses a log entry for progress information
    /// </summary>
    private void ParseLogEntry(string logLine)
    {
        try
        {
            // Parse different types of Cimian log entries
            // Examples of what we're looking for:
            // - "Installing Chrome..." 
            // - "Download progress: Chrome 45%"
            // - "Installation complete: Chrome"
            // - "Error installing: Zoom - Access denied"
            
            // Installation started
            var installStartPattern = @"Installing\s+(.+?)\.\.\.";
            var installStartMatch = Regex.Match(logLine, installStartPattern, RegexOptions.IgnoreCase);
            if (installStartMatch.Success)
            {
                var softwareName = installStartMatch.Groups[1].Value.Trim();
                HandleInstallationStarted(softwareName);
                return;
            }
            
            // Download/Installation progress
            var progressPattern = @"(?:Download|Installation)\s+progress:\s+(.+?)\s+(\d+)%";
            var progressMatch = Regex.Match(logLine, progressPattern, RegexOptions.IgnoreCase);
            if (progressMatch.Success)
            {
                var softwareName = progressMatch.Groups[1].Value.Trim();
                var progress = int.Parse(progressMatch.Groups[2].Value);
                HandleInstallationProgress(softwareName, progress);
                return;
            }
            
            // Installation completed
            var completePattern = @"Installation\s+complete:\s+(.+?)(?:\s|$)";
            var completeMatch = Regex.Match(logLine, completePattern, RegexOptions.IgnoreCase);
            if (completeMatch.Success)
            {
                var softwareName = completeMatch.Groups[1].Value.Trim();
                HandleInstallationCompleted(softwareName, true);
                return;
            }
            
            // Installation failed/error
            var errorPattern = @"(?:Error|Failed)\s+installing:\s+(.+?)\s+-\s+(.+)";
            var errorMatch = Regex.Match(logLine, errorPattern, RegexOptions.IgnoreCase);
            if (errorMatch.Success)
            {
                var softwareName = errorMatch.Groups[1].Value.Trim();
                var errorMessage = errorMatch.Groups[2].Value.Trim();
                HandleInstallationCompleted(softwareName, false, errorMessage);
                return;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
            {
                Message = $"Error parsing log entry: {ex.Message}",
                Exception = ex
            });
        }
    }
    
    /// <summary>
    /// Handles when a software installation starts
    /// </summary>
    private void HandleInstallationStarted(string softwareName)
    {
        var existingItem = _installItems.FirstOrDefault(i => i.Name.Equals(softwareName, StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
        {
            existingItem.Status = CimianInstallStatus.Installing;
            existingItem.Progress = 0;
        }
        else
        {
            var newItem = new CimianInstallItem
            {
                Name = softwareName,
                Status = CimianInstallStatus.Installing,
                Progress = 0,
                StartTime = DateTime.Now
            };
            _installItems.Add(newItem);
        }
        
        // Update dialog
        _ = Task.Run(async () =>
        {
            await _dialogService.UpdateListItemAsync(softwareName, ListItemStatus.Progress, "Installing...");
        });
        
        InstallStarted?.Invoke(this, new CimianInstallEventArgs
        {
            SoftwareName = softwareName,
            Status = CimianInstallStatus.Installing
        });
    }
    
    /// <summary>
    /// Handles installation progress updates
    /// </summary>
    private void HandleInstallationProgress(string softwareName, int progress)
    {
        var item = _installItems.FirstOrDefault(i => i.Name.Equals(softwareName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            item.Progress = progress;
            item.Status = CimianInstallStatus.Installing;
        }
        
        // Update dialog
        _ = Task.Run(async () =>
        {
            await _dialogService.UpdateListItemAsync(softwareName, ListItemStatus.Progress, $"Installing... {progress}%");
        });
        
        ProgressUpdated?.Invoke(this, new CimianProgressEventArgs
        {
            SoftwareName = softwareName,
            Progress = progress,
            TotalProgress = CalculateOverallProgress()
        });
    }
    
    /// <summary>
    /// Handles when installation completes (success or failure)
    /// </summary>
    private void HandleInstallationCompleted(string softwareName, bool success, string? errorMessage = null)
    {
        var item = _installItems.FirstOrDefault(i => i.Name.Equals(softwareName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            item.Status = success ? CimianInstallStatus.Completed : CimianInstallStatus.Failed;
            item.Progress = success ? 100 : 0;
            item.EndTime = DateTime.Now;
            item.ErrorMessage = errorMessage;
            
            if (success)
            {
                _completedItems++;
            }
        }
        
        // Update dialog
        _ = Task.Run(async () =>
        {
            if (success)
            {
                await _dialogService.UpdateListItemAsync(softwareName, ListItemStatus.Success, "Installation complete");
            }
            else
            {
                await _dialogService.UpdateListItemAsync(softwareName, ListItemStatus.Error, 
                    $"Installation failed: {errorMessage}");
            }
        });
        
        InstallCompleted?.Invoke(this, new CimianInstallEventArgs
        {
            SoftwareName = softwareName,
            Status = item?.Status ?? CimianInstallStatus.Failed,
            ErrorMessage = errorMessage
        });
        
        // Check if all installations are complete
        if (_completedItems >= _totalItems && _totalItems > 0)
        {
            _ = Task.Run(async () =>
            {
                await _dialogService.SetProgressAsync(100, "All installations complete!");
                // Mark first-run as complete
                MarkFirstRunComplete();
            });
        }
    }
    
    /// <summary>
    /// Calculates overall progress across all installations
    /// </summary>
    private int CalculateOverallProgress()
    {
        if (_installItems.Count == 0)
            return 0;
        
        var totalProgress = _installItems.Sum(i => i.Progress);
        return (int)(totalProgress / _installItems.Count);
    }
    
    /// <summary>
    /// Parses the Cimian manifest to get expected installation items
    /// </summary>
    private async Task ParseManifestAsync()
    {
        try
        {
            // Look for Cimian manifest files
            var possibleManifests = new[]
            {
                @"C:\ProgramData\Cimian\manifests\staff.json",
                @"C:\ProgramData\Cimian\manifests\default.json",
                @"C:\Program Files\Cimian\manifests\staff.json"
            };
            
            string? manifestPath = null;
            foreach (var path in possibleManifests)
            {
                if (File.Exists(path))
                {
                    manifestPath = path;
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(manifestPath))
            {
                // No manifest found, add some default expected items
                await AddDefaultExpectedItems();
                return;
            }
            
            var manifestContent = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<CimianManifest>(manifestContent);
            
            if (manifest?.ManagedInstalls != null)
            {
                foreach (var install in manifest.ManagedInstalls)
                {
                    var item = new CimianInstallItem
                    {
                        Name = install.Name ?? install.DisplayName ?? "Unknown",
                        Status = CimianInstallStatus.Pending,
                        Progress = 0
                    };
                    _installItems.Add(item);
                    
                    // Add to dialog
                    await _dialogService.AddListItemAsync(item.Name, ListItemStatus.Pending, "Waiting...");
                }
                
                _totalItems = _installItems.Count;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
            {
                Message = $"Error parsing manifest: {ex.Message}",
                Exception = ex
            });
            
            // Fallback to default items
            await AddDefaultExpectedItems();
        }
    }
    
    /// <summary>
    /// Adds default expected installation items if no manifest is found
    /// </summary>
    private async Task AddDefaultExpectedItems()
    {
        var defaultItems = new[] { "Chrome", "Zoom", "PaperCut" };
        
        foreach (var itemName in defaultItems)
        {
            var item = new CimianInstallItem
            {
                Name = itemName,
                Status = CimianInstallStatus.Pending,
                Progress = 0
            };
            _installItems.Add(item);
            
            await _dialogService.AddListItemAsync(itemName, ListItemStatus.Pending, "Waiting...");
        }
        
        _totalItems = _installItems.Count;
    }
    
    /// <summary>
    /// Ensures Cimian process is running
    /// </summary>
    private async Task<bool> EnsureCimianRunningAsync()
    {
        try
        {
            // Check if managedsoftwareupdate is already running
            var existingProcesses = Process.GetProcessesByName("managedsoftwareupdate");
            if (existingProcesses.Length > 0)
            {
                _cimianProcess = existingProcesses[0];
                return true;
            }
            
            // Try to start it
            var cimianPaths = new[]
            {
                @"C:\Program Files\Cimian\managedsoftwareupdate.exe",
                @"C:\ProgramData\Cimian\managedsoftwareupdate.exe"
            };
            
            foreach (var path in cimianPaths)
            {
                if (File.Exists(path))
                {
                    var startInfo = new ProcessStartInfo(path)
                    {
                        Arguments = "--auto-run",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    _cimianProcess = Process.Start(startInfo);
                    if (_cimianProcess != null)
                    {
                        // Wait a moment for process to initialize
                        await Task.Delay(2000);
                        return !_cimianProcess.HasExited;
                    }
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Marks the first-run as complete
    /// </summary>
    private static void MarkFirstRunComplete()
    {
        try
        {
#if WINDOWS
            // Set registry marker
            using var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Cimian");
            key?.SetValue("FirstRun", "0");
            key?.SetValue("FirstRunCompleted", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
#endif
        }
        catch
        {
            // Ignore errors in marking complete
        }
    }
    
    /// <summary>
    /// Periodic progress update method
    /// </summary>
    private void UpdateProgress(object? state)
    {
        if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
            return;
        
        try
        {
            var overallProgress = CalculateOverallProgress();
            var progressText = _completedItems > 0 
                ? $"Installing software... ({_completedItems}/{_totalItems} complete)"
                : "Installing software...";
            
            _ = Task.Run(async () =>
            {
                await _dialogService.SetProgressAsync(overallProgress, progressText);
            });
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
            {
                Message = $"Error in periodic update: {ex.Message}",
                Exception = ex
            });
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        StopMonitoring();
        _progressTimer?.Dispose();
        _logWatcher?.Dispose();
        _cancellationTokenSource.Dispose();
        _cimianProcess?.Dispose();
        
        _disposed = true;
    }
}

/// <summary>
/// Represents a Cimian installation item
/// </summary>
public class CimianInstallItem
{
    public string Name { get; set; } = string.Empty;
    public CimianInstallStatus Status { get; set; } = CimianInstallStatus.Pending;
    public int Progress { get; set; } = 0;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Status of a Cimian installation
/// </summary>
public enum CimianInstallStatus
{
    Pending,
    Downloading,
    Installing,
    Completed,
    Failed
}

/// <summary>
/// Event args for Cimian progress updates
/// </summary>
public class CimianProgressEventArgs : EventArgs
{
    public string SoftwareName { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int TotalProgress { get; set; }
}

/// <summary>
/// Event args for Cimian installation events
/// </summary>
public class CimianInstallEventArgs : EventArgs
{
    public string SoftwareName { get; set; } = string.Empty;
    public CimianInstallStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Event args for Cimian errors
/// </summary>
public class CimianErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}

/// <summary>
/// Simplified Cimian manifest structure for parsing
/// </summary>
public class CimianManifest
{
    public List<CimianManagedInstall>? ManagedInstalls { get; set; }
}

/// <summary>
/// Represents a managed install item from Cimian manifest
/// </summary>
public class CimianManagedInstall
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Version { get; set; }
}
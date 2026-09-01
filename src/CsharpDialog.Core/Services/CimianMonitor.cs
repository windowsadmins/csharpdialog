using System.Diagnostics;
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
                throw new InvalidOperationException(
                    $"Could not set up Cimian log monitoring: no log found at {_cimianLogPath}.");
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
            RaiseError("Failed to start Cimian monitoring", ex);
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
            
            // 3. Cimian's bootstrap flag exists for the life of a bootstrap run
            if (File.Exists(CimianPaths.BootstrapFlagFile))
            {
                return true;
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
    /// Resolves the log this monitor tails.
    /// </summary>
    /// <remarks>
    /// The client truncates and rewrites %ProgramData%\ManagedInstalls\reports\run.log at
    /// the start of every run, so it is the simplest live source: one file, one path, and
    /// it only ever holds the current run. When it is absent the newest per-session
    /// logs\yyyy-MM-dd\HHmm\run.log is used instead.
    /// </remarks>
    private void InitializeCimianPaths()
    {
        _cimianLogPath = CimianPaths.ReportRunLog;

        if (File.Exists(_cimianLogPath))
        {
            FileLog.Info($"Cimian monitor tailing {_cimianLogPath}");
            return;
        }

        var newestSessionLog = FindNewestSessionLog();
        if (!string.IsNullOrEmpty(newestSessionLog))
        {
            _cimianLogPath = newestSessionLog;
            FileLog.Info($"Cimian monitor tailing {_cimianLogPath} (no report log yet)");
            return;
        }

        FileLog.Warn($"No Cimian run log found under {CimianPaths.ManagedInstallsRoot}");
    }

    /// <summary>
    /// Newest %ProgramData%\ManagedInstalls\logs\&lt;yyyy-MM-dd&gt;\&lt;HHmm&gt;\run.log,
    /// or null when the client has never run here.
    /// </summary>
    private static string? FindNewestSessionLog()
    {
        try
        {
            if (!Directory.Exists(CimianPaths.LogsDir))
                return null;

            return Directory.EnumerateFiles(CimianPaths.LogsDir, "run.log", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
        catch
        {
            return null;
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
            RaiseError("Error processing log file change", ex);
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
            RaiseError("Error reading log entries", ex);
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
            RaiseError("Error parsing log entry", ex);
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
    /// Reads the client's cached manifests to get the expected installation items
    /// </summary>
    /// <remarks>
    /// The client caches every manifest it resolved as
    /// %ProgramData%\ManagedInstalls\manifests\&lt;name&gt;.yaml. The expected items are the
    /// union of their managed_installs lists.
    /// </remarks>
    private async Task ParseManifestAsync()
    {
        try
        {
            var names = new List<string>();

            if (Directory.Exists(CimianPaths.ManifestsDir))
            {
                foreach (var manifestPath in Directory.EnumerateFiles(CimianPaths.ManifestsDir, "*.yaml"))
                {
                    names.AddRange(ReadManagedInstalls(await File.ReadAllLinesAsync(manifestPath)));
                }
            }

            names = names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (names.Count == 0)
            {
                FileLog.Warn($"No managed_installs found under {CimianPaths.ManifestsDir}; using the default item list");
                await AddDefaultExpectedItems();
                return;
            }

            foreach (var name in names)
            {
                _installItems.Add(new CimianInstallItem
                {
                    Name = name,
                    Status = CimianInstallStatus.Pending,
                    Progress = 0
                });

                await _dialogService.AddListItemAsync(name, ListItemStatus.Pending, "Waiting...");
            }

            _totalItems = _installItems.Count;
            FileLog.Info($"Expecting {_totalItems} managed install(s) from {CimianPaths.ManifestsDir}");
        }
        catch (Exception ex)
        {
            RaiseError("Error parsing manifest", ex);

            // Fallback to default items
            await AddDefaultExpectedItems();
        }
    }

    /// <summary>
    /// Collects the entries of every managed_installs list in a manifest. The manifests are
    /// plain YAML with flat string lists, so a line scan is enough and avoids a YAML dependency.
    /// </summary>
    internal static List<string> ReadManagedInstalls(IEnumerable<string> lines)
    {
        var items = new List<string>();
        var inList = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.Equals("managed_installs:", StringComparison.OrdinalIgnoreCase))
            {
                inList = true;
                continue;
            }

            if (!inList)
                continue;

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                var name = line[2..].Trim().Trim('"', '\'');
                if (name.Length > 0)
                    items.Add(name);
                continue;
            }

            // Any other line is the next key, which ends the list
            inList = false;
        }

        return items;
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
            var exePath = CimianPaths.ManagedSoftwareUpdateExe;
            if (!File.Exists(exePath))
            {
                FileLog.Warn($"Cimian is not running and {exePath} was not found");
                return false;
            }

            var startInfo = new ProcessStartInfo(exePath)
            {
                Arguments = "--auto-run",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            FileLog.Info($"Starting {exePath} --auto-run");
            _cimianProcess = Process.Start(startInfo);
            if (_cimianProcess != null)
            {
                // Wait a moment for process to initialize
                await Task.Delay(2000);
                return !_cimianProcess.HasExited;
            }

            return false;
        }
        catch (Exception ex)
        {
            FileLog.Error("Could not start or attach to the Cimian process", ex);
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
            RaiseError("Error in periodic update", ex);
        }
    }
    
    /// <summary>
    /// Records an error in the file log and raises it to subscribers
    /// </summary>
    private void RaiseError(string message, Exception exception)
    {
        FileLog.Error(message, exception);
        ErrorOccurred?.Invoke(this, new CimianErrorEventArgs
        {
            Message = $"{message}: {exception.Message}",
            Exception = exception
        });
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

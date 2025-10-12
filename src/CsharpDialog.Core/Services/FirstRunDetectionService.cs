using System.Security.Principal;
#if WINDOWS
using Microsoft.Win32;
using System.Management;
#endif

namespace csharpDialog.Core.Services;

/// <summary>
/// Service for detecting first-run scenarios and device bootstrap states
/// Used to determine when to show Cimian progress dialogs during OOBE and first login
/// </summary>
public class FirstRunDetectionService
{
    /// <summary>
    /// Comprehensive check to determine if this is a first-run scenario
    /// </summary>
    public static FirstRunResult DetectFirstRunScenario()
    {
        var result = new FirstRunResult();
        
        try
        {
            // Check multiple indicators for first-run scenarios
            result.IsFirstUserLogin = CheckFirstUserLogin();
            result.IsPostOOBE = CheckPostOOBE();
            result.IsCimianFirstRun = CheckCimianFirstRun();
            result.IsDeviceBootstrap = CheckDeviceBootstrap();
            result.ProfileAge = GetUserProfileAge();
            
            // Determine overall first-run status
            result.IsFirstRun = result.IsFirstUserLogin || 
                               result.IsPostOOBE || 
                               result.IsCimianFirstRun || 
                               result.IsDeviceBootstrap ||
                               result.ProfileAge < TimeSpan.FromMinutes(15);
            
            // Additional context
            result.UserSid = GetCurrentUserSid();
            result.DeviceJoinStatus = GetDeviceJoinStatus();
            result.LastBootTime = GetLastBootTime();
            
        }
        catch (Exception ex)
        {
            result.DetectionError = ex.Message;
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if this is the user's first login to this device
    /// </summary>
    private static bool CheckFirstUserLogin()
    {
        try
        {
            // Method 1: Check user profile creation time
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var ntUserDat = Path.Combine(userProfile, "ntuser.dat");
            
            if (File.Exists(ntUserDat))
            {
                var creationTime = File.GetCreationTime(ntUserDat);
                var profileAge = DateTime.Now - creationTime;
                
                // If profile created within last 10 minutes, likely first login
                if (profileAge < TimeSpan.FromMinutes(10))
                {
                    return true;
                }
            }
            
#if WINDOWS
            // Method 2: Check for first-login registry markers
            using var currentUserKey = Registry.CurrentUser;
            
            // Check Windows first-login markers
            using var explorerKey = currentUserKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer");
            if (explorerKey != null)
            {
                var firstRun = explorerKey.GetValue("FirstRun");
                if (firstRun != null && firstRun.ToString() == "1")
                {
                    return true;
                }
            }
            
            // Check for RunOnce keys (often used for first-login tasks)
            using var runOnceKey = currentUserKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\RunOnce");
            if (runOnceKey != null && runOnceKey.ValueCount > 0)
            {
                // Check if any RunOnce entries are related to first-login
                foreach (var valueName in runOnceKey.GetValueNames())
                {
                    if (valueName.Contains("FirstLogin", StringComparison.OrdinalIgnoreCase) ||
                        valueName.Contains("Welcome", StringComparison.OrdinalIgnoreCase) ||
                        valueName.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
#endif
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Checks if the system recently completed OOBE (Out-of-Box Experience)
    /// </summary>
    private static bool CheckPostOOBE()
    {
        try
        {
#if WINDOWS
            // Check OOBE completion markers in registry
            using var oobeKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\OOBE");
            if (oobeKey != null)
            {
                var oobeCompleted = oobeKey.GetValue("OOBECompleted");
                if (oobeCompleted != null && oobeCompleted.ToString() == "1")
                {
                    // Check when OOBE was completed
                    var oobeCompletedTime = oobeKey.GetValue("OOBECompletedTime");
                    if (oobeCompletedTime != null && DateTime.TryParse(oobeCompletedTime.ToString(), out DateTime completedTime))
                    {
                        var timeSinceOOBE = DateTime.Now - completedTime;
                        // If OOBE completed within last hour, consider post-OOBE
                        return timeSinceOOBE < TimeSpan.FromHours(1);
                    }
                }
            }
#endif
            
            // Check for first boot after OOBE
            var lastBootTime = GetLastBootTime();
            if (lastBootTime.HasValue)
            {
                var timeSinceBoot = DateTime.Now - lastBootTime.Value;
                // Fresh boot within 30 minutes could indicate post-OOBE
                if (timeSinceBoot < TimeSpan.FromMinutes(30))
                {
                    // Additional check: look for OOBE-related processes or files
                    var oobeProcesses = new[] { "oobe", "msoobe", "CloudExperienceHost" };
                    foreach (var processName in oobeProcesses)
                    {
                        if (System.Diagnostics.Process.GetProcessesByName(processName).Length > 0)
                        {
                            return true;
                        }
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
    /// Checks if this is Cimian's first run on this device
    /// </summary>
    private static bool CheckCimianFirstRun()
    {
        try
        {
#if WINDOWS
            // Method 1: Check Cimian registry markers
            using var cimianKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Cimian");
            if (cimianKey != null)
            {
                var firstRun = cimianKey.GetValue("FirstRun");
                if (firstRun != null && firstRun.ToString() == "1")
                {
                    return true;
                }
                
                var installDate = cimianKey.GetValue("InstallDate");
                if (installDate != null && DateTime.TryParse(installDate.ToString(), out DateTime installTime))
                {
                    var timeSinceInstall = DateTime.Now - installTime;
                    // If installed within last 2 hours, likely first run
                    if (timeSinceInstall < TimeSpan.FromHours(2))
                    {
                        return true;
                    }
                }
            }
            
            // Method 2: Check Cimian user-specific markers
            using var userCimianKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Cimian");
            if (userCimianKey != null)
            {
                var userFirstRun = userCimianKey.GetValue("FirstRun");
                if (userFirstRun != null && userFirstRun.ToString() == "1")
                {
                    return true;
                }
            }
#endif
            
            // Method 3: Check for bootstrap completion markers
            var bootstrapMarkers = new[]
            {
                @"C:\ProgramData\Cimian\bootstrap_in_progress",
                @"C:\ProgramData\Cimian\.bootstrap_running"
            };
            
            foreach (var marker in bootstrapMarkers)
            {
                if (File.Exists(marker))
                {
                    var creationTime = File.GetCreationTime(marker);
                    var age = DateTime.Now - creationTime;
                    // Active bootstrap process
                    if (age < TimeSpan.FromHours(4))
                    {
                        return true;
                    }
                }
            }
            
            // Method 4: Check for recent bootstrap completion
            var completionMarker = @"C:\ProgramData\Cimian\bootstrap_complete";
            if (File.Exists(completionMarker))
            {
                var completionTime = File.GetCreationTime(completionMarker);
                var timeSinceCompletion = DateTime.Now - completionTime;
                // Bootstrap completed recently
                if (timeSinceCompletion < TimeSpan.FromHours(1))
                {
                    return true;
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
    /// Checks if the device is currently in bootstrap mode
    /// </summary>
    private static bool CheckDeviceBootstrap()
    {
        try
        {
            // Check for active bootstrap processes
            var bootstrapProcesses = new[] { "cimian", "managedsoftwareupdate", "cimian_bootstrap" };
            
            foreach (var processName in bootstrapProcesses)
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    // Check if any process was started recently (within last hour)
                    foreach (var process in processes)
                    {
                        try
                        {
                            var startTime = process.StartTime;
                            var runtime = DateTime.Now - startTime;
                            if (runtime < TimeSpan.FromHours(1))
                            {
                                return true;
                            }
                        }
                        catch
                        {
                            // If we can't get start time, assume it might be bootstrap
                            return true;
                        }
                    }
                }
            }
            
            // Check for ESP (Enrollment Status Page) indicators
            var espMarkers = new[]
            {
                @"C:\Windows\System32\ESP_in_progress",
                @"C:\ProgramData\Microsoft\Windows\AppRepository\ESP"
            };
            
            foreach (var marker in espMarkers)
            {
                if (File.Exists(marker) || Directory.Exists(marker))
                {
                    return true;
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
    /// Gets the age of the current user profile
    /// </summary>
    private static TimeSpan GetUserProfileAge()
    {
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Directory.Exists(userProfile))
            {
                var creationTime = Directory.GetCreationTime(userProfile);
                return DateTime.Now - creationTime;
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return TimeSpan.Zero;
    }
    
    /// <summary>
    /// Gets the current user's SID
    /// </summary>
    private static string GetCurrentUserSid()
    {
        try
        {
#if WINDOWS
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.ToString() ?? string.Empty;
#else
            return string.Empty;
#endif
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Determines if the device is domain joined or Azure AD joined
    /// </summary>
    private static DeviceJoinStatus GetDeviceJoinStatus()
    {
        try
        {
#if WINDOWS
            // Check domain join status
            using var computerKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters");
            if (computerKey != null)
            {
                var domain = computerKey.GetValue("Domain");
                if (domain != null && !string.IsNullOrEmpty(domain.ToString()))
                {
                    return DeviceJoinStatus.DomainJoined;
                }
            }
            
            // Check Azure AD join status
            using var aadKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo");
            if (aadKey != null && aadKey.SubKeyCount > 0)
            {
                return DeviceJoinStatus.AzureADJoined;
            }
            
            // Check workplace join
            using var wpjKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AAD\Storage");
            if (wpjKey != null)
            {
                return DeviceJoinStatus.WorkplaceJoined;
            }
            
            return DeviceJoinStatus.Workgroup;
#else
            return DeviceJoinStatus.Unknown;
#endif
        }
        catch
        {
            return DeviceJoinStatus.Unknown;
        }
    }
    
    /// <summary>
    /// Gets the last boot time of the system
    /// </summary>
    private static DateTime? GetLastBootTime()
    {
        try
        {
#if WINDOWS
            // Method 1: Use WMI to get system boot time
            using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
            using var collection = searcher.Get();
            
            foreach (ManagementObject obj in collection)
            {
                var bootTimeStr = obj["LastBootUpTime"]?.ToString();
                if (!string.IsNullOrEmpty(bootTimeStr))
                {
                    // WMI datetime format: 20231203142030.500000-480
                    var bootTime = ManagementDateTimeConverter.ToDateTime(bootTimeStr);
                    return bootTime;
                }
            }
#endif
        }
        catch
        {
            // Fallback: Use Environment.TickCount (less precise but works)
            try
            {
                var tickCount = Environment.TickCount;
                return DateTime.Now - TimeSpan.FromMilliseconds(tickCount);
            }
            catch
            {
                // Final fallback
                return null;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Marks that first-run processing has been completed
    /// </summary>
    public static void MarkFirstRunCompleted()
    {
        try
        {
#if WINDOWS
            // Mark in user registry
            using var userKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Cimian");
            userKey?.SetValue("FirstRun", "0");
            userKey?.SetValue("FirstRunCompleted", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            // Mark in local machine registry (if we have permissions)
            try
            {
                using var machineKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Cimian");
                machineKey?.SetValue("FirstRun", "0");
                machineKey?.SetValue("LastUserFirstRun", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch
            {
                // Ignore if we don't have admin permissions
            }
#endif
            
            // Create completion marker file
            var markerDirectory = @"C:\ProgramData\Cimian";
            if (Directory.Exists(markerDirectory))
            {
                var markerPath = Path.Combine(markerDirectory, "first_run_completed");
                File.WriteAllText(markerPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }
        catch
        {
            // Ignore errors in marking completion
        }
    }
    
    /// <summary>
    /// Checks if csharpDialog should automatically launch for first-run
    /// </summary>
    public static bool ShouldAutoLaunchForFirstRun()
    {
        var firstRun = DetectFirstRunScenario();
        
        // Only auto-launch if it's truly a first-run scenario and we're not already running
        if (!firstRun.IsFirstRun)
            return false;
        
        // Check if we're not already running
        var currentProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        var existingProcesses = System.Diagnostics.Process.GetProcessesByName(currentProcessName);
        
        if (existingProcesses.Length > 1)
            return false; // Already running
        
        // Check if user is interactive (not running as service/system)
        if (Environment.UserInteractive && !string.IsNullOrEmpty(Environment.UserName))
        {
            return true;
        }
        
        return false;
    }
}

/// <summary>
/// Result of first-run detection
/// </summary>
public class FirstRunResult
{
    public bool IsFirstRun { get; set; }
    public bool IsFirstUserLogin { get; set; }
    public bool IsPostOOBE { get; set; }
    public bool IsCimianFirstRun { get; set; }
    public bool IsDeviceBootstrap { get; set; }
    public TimeSpan ProfileAge { get; set; }
    public string UserSid { get; set; } = string.Empty;
    public DeviceJoinStatus DeviceJoinStatus { get; set; }
    public DateTime? LastBootTime { get; set; }
    public string? DetectionError { get; set; }
    
    public override string ToString()
    {
        return $"FirstRun: {IsFirstRun}, UserLogin: {IsFirstUserLogin}, PostOOBE: {IsPostOOBE}, " +
               $"CimianFirstRun: {IsCimianFirstRun}, Bootstrap: {IsDeviceBootstrap}, " +
               $"ProfileAge: {ProfileAge.TotalMinutes:F1}min, JoinStatus: {DeviceJoinStatus}";
    }
}

/// <summary>
/// Device join status enumeration
/// </summary>
public enum DeviceJoinStatus
{
    Unknown,
    Workgroup,
    DomainJoined,
    AzureADJoined,
    WorkplaceJoined
}
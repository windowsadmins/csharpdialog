using csharpDialog.Core;
using csharpDialog.Core.Services;
using System;
using System.IO;
using System.Threading;
using System.Runtime.Versioning;

namespace csharpDialog.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // WPF requires STA thread - create one if on Windows
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var staThread = new Thread(() =>
                {
                    MainAsync(args).GetAwaiter().GetResult();
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
            else
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
        }
        
        static async Task MainAsync(string[] args)
        {
            try
            {
                // Check for auto-launch mode first
                if (args.Length == 1 && args[0] == "--autolaunch")
                {
                    await HandleAutoLaunch();
                    return;
                }
                
                // Parse command line arguments
                var configuration = CommandLineParser.ParseArguments(args);
                
                // Check if first-run detection should be performed
                if (configuration.Metadata.ContainsKey("AutoLaunchFirstRun"))
                {
                    var firstRunResult = FirstRunDetectionService.DetectFirstRunScenario();
                    if (!firstRunResult.IsFirstRun)
                    {
                        Console.WriteLine("Not a first-run scenario. Exiting.");
                        Environment.Exit(0);
                    }
                    
                    // Configure for first-run
                    configuration.Metadata["FirstRunMode"] = true;
                    configuration.Metadata["EnableCimianMonitoring"] = true;
                    configuration.Metadata["FullscreenMode"] = true;
                    configuration.Title = "Setting up your device...";
                    configuration.Message = "Please wait while we install your software and configure your device.";
                    configuration.ShowProgressBar = true;
                    configuration.ShowListItems = true;
                    configuration.Topmost = true;
                }
                
                // Initialize WPF if needed for fullscreen/kiosk/window scenarios OR command file monitoring
                bool needsWpf = configuration.Metadata.ContainsKey("FullscreenMode") ||
                               configuration.Metadata.ContainsKey("KioskMode") ||
                               configuration.Metadata.ContainsKey("FirstRunMode") ||
                               configuration.Metadata.ContainsKey("WindowMode") ||
                               configuration.EnableCommandFile;  // WPF required for command file monitoring
                
                IDialogService dialogService;
                
                if (needsWpf)
                {
                    InitializeWpfIfNeeded();
                    
                    // Try to create WPF service directly
                    try
                    {
                        Console.WriteLine("[DEBUG] Attempting to load WPF service directly...");
                        var wpfAssembly = System.Reflection.Assembly.LoadFrom(
                            Path.Combine(AppContext.BaseDirectory, "csharpDialog.WPF.dll"));
                        
                        var wpfServiceType = wpfAssembly.GetType("csharpDialog.WPF.Services.WpfDialogService");
                        Console.WriteLine($"[DEBUG] Got type: {wpfServiceType?.FullName}");
                        
                        if (wpfServiceType != null)
                        {
                            Console.WriteLine("[DEBUG] Creating instance...");
                            var wpfServiceInstance = Activator.CreateInstance(wpfServiceType);
                            Console.WriteLine($"[DEBUG] Instance created: {wpfServiceInstance?.GetType().Name}");
                            
#pragma warning disable CS8600
                            dialogService = wpfServiceInstance as IDialogService;
#pragma warning restore CS8600
                            if (dialogService != null)
                            {
                                Console.WriteLine("[DEBUG] Successfully cast to IDialogService!");
                            }
                            else
                            {
                                Console.WriteLine("[DEBUG] FAILED to cast to IDialogService");
                                dialogService = DialogServiceFactory.CreateDialogService(configuration);
                            }
                        }
                        else
                        {
                            Console.WriteLine("[DEBUG] Type not found, using factory");
                            dialogService = DialogServiceFactory.CreateDialogService(configuration);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Direct WPF load failed: {ex.Message}");
                        Console.WriteLine($"[DEBUG] Stack: {ex.StackTrace}");
                        dialogService = DialogServiceFactory.CreateDialogService(configuration);
                    }
                }
                else
                {
                    // Create dialog service (WPF for first-run, console otherwise)
                    dialogService = DialogServiceFactory.CreateDialogService(configuration);
                }
                
                // Start Cimian monitoring if enabled
                CimianMonitor? cimianMonitor = null;
                if (configuration.Metadata.ContainsKey("EnableCimianMonitoring"))
                {
                    cimianMonitor = new CimianMonitor(dialogService);
                    var monitoringStarted = await cimianMonitor.StartFirstRunMonitoringAsync();
                    
                    if (!monitoringStarted)
                    {
                        Console.WriteLine("Warning: Could not start Cimian monitoring. Continuing with regular dialog.");
                    }
                }
                
                // Show dialog
                var result = dialogService.ShowDialog(configuration);
                
                // Stop monitoring
                cimianMonitor?.StopMonitoring();
                cimianMonitor?.Dispose();
                
                // Mark first-run as complete if this was a first-run scenario
                if (configuration.Metadata.ContainsKey("FirstRunMode"))
                {
                    FirstRunDetectionService.MarkFirstRunCompleted();
                }
                
                // Output result for scripting purposes
                Console.WriteLine($"Result: {result.ButtonPressed}");
                Console.WriteLine($"Timestamp: {result.Timestamp}");
                
                // Exit with appropriate code
                Environment.Exit(result.ButtonPressed == "ok" || result.ButtonPressed == "button1" ? 0 : 1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// Handles auto-launch mode for first-run scenarios
        /// </summary>
        private static async Task HandleAutoLaunch()
        {
            try
            {
                // Detect if this is a first-run scenario
                var firstRunResult = FirstRunDetectionService.DetectFirstRunScenario();
                
                Console.WriteLine($"First-run detection result: {firstRunResult}");
                
                if (!firstRunResult.IsFirstRun)
                {
                    Console.WriteLine("Not a first-run scenario. Auto-launch cancelled.");
                    Environment.Exit(0);
                }
                
                Console.WriteLine("First-run detected! Launching Cimian progress dialog...");
                
                // Create configuration for first-run
                var configuration = new DialogConfiguration
                {
                    Title = "Setting up your device",
                    Message = "Welcome! Please wait while we install software and configure your device for first use.",
                    ShowProgressBar = true,
                    ShowListItems = true,
                    Topmost = true,
                    CenterOnScreen = true,
                    BackgroundColor = "#0078d4", // Microsoft blue
                    TextColor = "#ffffff",
                    FontFamily = "Segoe UI",
                    FontSize = 14
                };
                
                configuration.Metadata["FirstRunMode"] = true;
                configuration.Metadata["EnableCimianMonitoring"] = true;
                configuration.Metadata["FullscreenMode"] = true;
                configuration.Metadata["KioskMode"] = true;
                
                // Create dialog service (WPF for first-run, console otherwise)
                var dialogService = DialogServiceFactory.CreateDialogService(configuration);
                
                // Start Cimian monitoring
                using var cimianMonitor = new CimianMonitor(dialogService);
                var monitoringStarted = await cimianMonitor.StartFirstRunMonitoringAsync();
                
                if (!monitoringStarted)
                {
                    Console.WriteLine("Warning: Could not start Cimian monitoring. Showing basic first-run dialog.");
                }
                
                // Show the dialog
                var result = dialogService.ShowDialog(configuration);
                
                // Mark first-run as complete
                FirstRunDetectionService.MarkFirstRunCompleted();
                
                Console.WriteLine("First-run setup completed!");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Auto-launch error: {ex.Message}");
                Environment.Exit(1);
            }
        }
        
        /// <summary>
        /// Initializes WPF Application if needed for GUI scenarios
        /// </summary>
        private static void InitializeWpfIfNeeded()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;
                
            try
            {
                // Load WPF assembly
                var wpfAssembly = System.Reflection.Assembly.LoadFrom(
                    Path.Combine(AppContext.BaseDirectory, "csharpDialog.WPF.dll"));
                
                Console.WriteLine($"[DEBUG] Loaded WPF assembly from: {wpfAssembly.Location}");
                
                // Get the Application type from System.Windows namespace
                var applicationType = Type.GetType("System.Windows.Application, PresentationFramework");
                if (applicationType == null)
                {
                    Console.WriteLine("[DEBUG] Could not find System.Windows.Application type");
                    return;
                }
                
                // Check if an Application instance already exists
                var currentProperty = applicationType.GetProperty("Current");
                var currentApp = currentProperty?.GetValue(null);
                
                Console.WriteLine($"[DEBUG] Current WPF Application: {(currentApp == null ? "null" : "exists")}");
                
                if (currentApp == null)
                {
                    // Create a new WPF Application instance
                    var app = Activator.CreateInstance(applicationType);
                    Console.WriteLine("[DEBUG] Created new WPF Application instance");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] WPF initialization error: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using csharpDialog.Core.Services;
using csharpDialog.Core.Models;

namespace csharpDialog.Core
{
    /// <summary>
    /// Interface for dialog services with command file support
    /// </summary>
    public interface IDialogService
    {
        Task<DialogResult> ShowDialogAsync(DialogConfiguration configuration);
        DialogResult ShowDialog(DialogConfiguration configuration);
        
        // Command file monitoring support
        event EventHandler<CommandReceivedEventArgs>? CommandReceived;
        Task StartCommandMonitoringAsync(string commandFilePath);
        void StopCommandMonitoring();
        bool IsCommandMonitoringActive { get; }

        // Phase 2: Dynamic List Item Operations
        /// <summary>
        /// Processes a command and updates the dialog accordingly
        /// Returns true if the command was processed successfully
        /// </summary>
        Task<bool> ProcessCommandAsync(Command command);

        /// <summary>
        /// Updates a list item by title
        /// </summary>
        Task<bool> UpdateListItemAsync(string title, ListItemStatus status, string statusText = "");

        /// <summary>
        /// Updates a list item by index (0-based)
        /// </summary>
        Task<bool> UpdateListItemByIndexAsync(int index, ListItemStatus status, string statusText = "");

        /// <summary>
        /// Adds a new list item
        /// </summary>
        Task<bool> AddListItemAsync(string title, ListItemStatus status = ListItemStatus.None, string statusText = "");

        /// <summary>
        /// Removes a list item by title
        /// </summary>
        Task<bool> RemoveListItemAsync(string title);

        /// <summary>
        /// Removes a list item by index (0-based)
        /// </summary>
        Task<bool> RemoveListItemByIndexAsync(int index);

        /// <summary>
        /// Clears all list items
        /// </summary>
        Task<bool> ClearListItemsAsync();

        // Phase 3: Enhanced Progress Controls
        /// <summary>
        /// Sets progress to a specific value (0-100)
        /// </summary>
        Task<bool> SetProgressAsync(int value, string? text = null);

        /// <summary>
        /// Increments progress by a specific amount
        /// </summary>
        Task<bool> IncrementProgressAsync(int increment, string? text = null);

        /// <summary>
        /// Resets progress to zero
        /// </summary>
        Task<bool> ResetProgressAsync(string? text = null);

        /// <summary>
        /// Updates only the progress text without changing the value
        /// </summary>
        Task<bool> UpdateProgressTextAsync(string text);

        // Phase 4: Advanced JSON Configuration Support
        /// <summary>
        /// Loads a JSON configuration and applies it to the dialog
        /// </summary>
        Task<bool> LoadJsonConfigurationAsync(string json);

        /// <summary>
        /// Loads a JSON configuration from file and applies it to the dialog
        /// </summary>
        Task<bool> LoadJsonConfigurationFromFileAsync(string filePath);

        /// <summary>
        /// Applies styling configuration to the dialog
        /// </summary>
        Task<bool> ApplyStyleConfigurationAsync(string styleJson);

        /// <summary>
        /// Updates the dialog theme
        /// </summary>
        Task<bool> UpdateThemeAsync(string theme);

        // Phase 5: Shell Command Execution Integration
        /// <summary>
        /// Executes a shell command with real-time output streaming
        /// </summary>
        Task<CommandExecutionResult> ExecuteShellCommandAsync(string command, string? workingDirectory = null);

        /// <summary>
        /// Executes a PowerShell script with real-time output streaming
        /// </summary>
        Task<CommandExecutionResult> ExecutePowerShellScriptAsync(string script, string? workingDirectory = null);

        /// <summary>
        /// Executes a command and captures all output without streaming
        /// </summary>
        Task<CommandExecutionResult> ExecuteAndCaptureOutputAsync(string command, string? workingDirectory = null);

        /// <summary>
        /// Event raised when command output is received during execution
        /// </summary>
        event EventHandler<CommandOutputEventArgs>? CommandOutputReceived;
    }

    /// <summary>
    /// Factory for creating dialog services
    /// </summary>
    public static class DialogServiceFactory
    {
        public static IDialogService CreateWpfDialogService()
        {
            // This will be implemented in the WPF project
            throw new NotImplementedException("WPF Dialog Service not implemented yet");
        }

        public static IDialogService CreateConsoleDialogService()
        {
            return new ConsoleDialogService();
        }
    }

    /// <summary>
    /// Basic console implementation of dialog service
    /// </summary>
    internal class ConsoleDialogService : IDialogService
    {
        private ICommandFileMonitor? _commandMonitor;
        private ShellCommandService? _shellService;
        
        public event EventHandler<CommandReceivedEventArgs>? CommandReceived;
        public event EventHandler<CommandOutputEventArgs>? CommandOutputReceived;
        
        public bool IsCommandMonitoringActive => _commandMonitor?.IsMonitoring == true;

        public async Task<DialogResult> ShowDialogAsync(DialogConfiguration configuration)
        {
            return await Task.Run(() => ShowDialog(configuration));
        }

        public DialogResult ShowDialog(DialogConfiguration configuration)
        {
            Console.WriteLine($"Title: {configuration.Title}");
            Console.WriteLine($"Message: {configuration.Message}");
            Console.WriteLine();

            // Show progress bar if configured
            if (configuration.ShowProgressBar)
            {
                Console.WriteLine($"Progress: {configuration.ProgressValue}/{configuration.ProgressMaximum}");
                if (!string.IsNullOrEmpty(configuration.ProgressText))
                {
                    Console.WriteLine($"Status: {configuration.ProgressText}");
                }
                Console.WriteLine();
            }

            // Show list items if configured
            if (configuration.ShowListItems && configuration.ListItems.Count > 0)
            {
                Console.WriteLine("Items:");
                foreach (var item in configuration.ListItems)
                {
                    var statusIcon = GetStatusIcon(item.Status);
                    Console.WriteLine($"  {statusIcon} {item.Title}");
                    if (!string.IsNullOrEmpty(item.StatusText))
                    {
                        Console.WriteLine($"    {item.StatusText}");
                    }
                }
                Console.WriteLine();
            }

            if (configuration.Buttons.Count > 0)
            {
                Console.WriteLine("Options:");
                for (int i = 0; i < configuration.Buttons.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {configuration.Buttons[i].Text}");
                }
                Console.Write("Select an option: ");
                
                if (int.TryParse(Console.ReadLine(), out int choice) && 
                    choice > 0 && choice <= configuration.Buttons.Count)
                {
                    return new DialogResult
                    {
                        ButtonPressed = configuration.Buttons[choice - 1].Action
                    };
                }
            }
            else
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            return new DialogResult
            {
                ButtonPressed = "ok"
            };
        }

        public async Task StartCommandMonitoringAsync(string commandFilePath)
        {
            if (_commandMonitor != null)
            {
                StopCommandMonitoring();
            }

            var parser = new CommandParser();
            _commandMonitor = new CommandFileMonitor(parser);
            _commandMonitor.CommandReceived += OnCommandReceived;
            _commandMonitor.ErrorOccurred += OnCommandError;

            await _commandMonitor.StartMonitoringAsync(commandFilePath);
            Console.WriteLine($"Started monitoring command file: {commandFilePath}");
        }

        public void StopCommandMonitoring()
        {
            if (_commandMonitor != null)
            {
                _commandMonitor.CommandReceived -= OnCommandReceived;
                _commandMonitor.ErrorOccurred -= OnCommandError;
                _commandMonitor.Dispose();
                _commandMonitor = null;
                Console.WriteLine("Stopped command monitoring");
            }
        }

        private void OnCommandReceived(object? sender, CommandReceivedEventArgs e)
        {
            Console.WriteLine($"Command received: {e.Command}");
            CommandReceived?.Invoke(this, e);
        }

        private void OnCommandError(object? sender, CommandFileErrorEventArgs e)
        {
            Console.WriteLine($"Command file error: {e.Message}");
        }

        private static string GetStatusIcon(ListItemStatus status)
        {
            return status switch
            {
                ListItemStatus.Success => "✓",
                ListItemStatus.Fail => "✗",
                ListItemStatus.Error => "⚠",
                ListItemStatus.Wait => "⏳",
                ListItemStatus.Pending => "○",
                ListItemStatus.Progress => "◉",
                _ => "•"
            };
        }

        // Phase 2: Dynamic List Item Operations Implementation
        public async Task<bool> ProcessCommandAsync(Command command)
        {
            return await Task.Run(async () =>
            {
                Console.WriteLine($"Processing command: {command.Type} = {command.Value}");
                
                switch (command.Type.ToLowerInvariant())
                {
                    case "listitem":
                        return ProcessListItemCommand(command);
                    case "list":
                        return ProcessListCommand(command);
                    case "title":
                    case "message":
                        Console.WriteLine($"Command processed: {command.Type}");
                        return true;
                    case "progress":
                        if (int.TryParse(command.Value, out int progressValue))
                        {
                            return SetProgressAsync(progressValue).Result;
                        }
                        return false;
                    case "progresstext":
                        return UpdateProgressTextAsync(command.Value).Result;
                    case "progressincrement":
                        if (int.TryParse(command.Value, out int incrementValue))
                        {
                            return IncrementProgressAsync(incrementValue).Result;
                        }
                        return false;
                    case "progressreset":
                        return ResetProgressAsync(command.Value).Result;
                    case "config":
                        return LoadJsonConfigurationAsync(command.Value).Result;
                    case "style":
                        return ApplyStyleConfigurationAsync(command.Value).Result;
                    case "theme":
                        return UpdateThemeAsync(command.Value).Result;
                    case "execute":
                        var executeResult = await ExecuteShellCommandAsync(command.Value);
                        return executeResult.Success;
                    case "executepowershell":
                        var psResult = await ExecutePowerShellScriptAsync(command.Value);
                        return psResult.Success;
                    case "executeoutput":
                        var captureResult = await ExecuteAndCaptureOutputAsync(command.Value);
                        return captureResult.Success;
                    default:
                        Console.WriteLine($"Unknown command: {command.Type}");
                        return false;
                }
            });
        }

        public async Task<bool> UpdateListItemAsync(string title, ListItemStatus status, string statusText = "")
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Updated list item '{title}': {status} - {statusText}");
                return true;
            });
        }

        public async Task<bool> UpdateListItemByIndexAsync(int index, ListItemStatus status, string statusText = "")
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Updated list item at index {index}: {status} - {statusText}");
                return true;
            });
        }

        public async Task<bool> AddListItemAsync(string title, ListItemStatus status = ListItemStatus.None, string statusText = "")
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Added list item: '{title}' ({status}) - {statusText}");
                return true;
            });
        }

        public async Task<bool> RemoveListItemAsync(string title)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Removed list item: '{title}'");
                return true;
            });
        }

        public async Task<bool> RemoveListItemByIndexAsync(int index)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Removed list item at index: {index}");
                return true;
            });
        }

        public async Task<bool> ClearListItemsAsync()
        {
            return await Task.Run(() =>
            {
                Console.WriteLine("Cleared all list items");
                return true;
            });
        }

        // Phase 3: Enhanced Progress Controls Implementation
        public async Task<bool> SetProgressAsync(int value, string? text = null)
        {
            return await Task.Run(() =>
            {
                var clampedValue = Math.Clamp(value, 0, 100);
                if (text != null)
                {
                    Console.WriteLine($"Progress set to {clampedValue}%: {text}");
                }
                else
                {
                    Console.WriteLine($"Progress set to {clampedValue}%");
                }
                return true;
            });
        }

        public async Task<bool> IncrementProgressAsync(int increment, string? text = null)
        {
            return await Task.Run(() =>
            {
                if (text != null)
                {
                    Console.WriteLine($"Progress incremented by {increment}: {text}");
                }
                else
                {
                    Console.WriteLine($"Progress incremented by {increment}");
                }
                return true;
            });
        }

        public async Task<bool> ResetProgressAsync(string? text = null)
        {
            return await Task.Run(() =>
            {
                var resetText = text ?? "Starting...";
                Console.WriteLine($"Progress reset: {resetText}");
                return true;
            });
        }

        public async Task<bool> UpdateProgressTextAsync(string text)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Progress text updated: {text}");
                return true;
            });
        }

        // Phase 4: Advanced JSON Configuration Support Implementation
        public async Task<bool> LoadJsonConfigurationAsync(string json)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var jsonConfig = JsonConfigurationService.ParseConfiguration(json);
                    if (jsonConfig != null)
                    {
                        var validation = JsonConfigurationService.ValidateConfiguration(jsonConfig);
                        if (validation.IsValid)
                        {
                            Console.WriteLine("JSON configuration loaded successfully");
                            if (validation.HasWarnings)
                            {
                                foreach (var warning in validation.Warnings)
                                {
                                    Console.WriteLine($"Warning: {warning}");
                                }
                            }
                            return true;
                        }
                        else
                        {
                            Console.WriteLine("JSON configuration validation failed:");
                            foreach (var error in validation.Errors)
                            {
                                Console.WriteLine($"Error: {error}");
                            }
                            return false;
                        }
                    }
                    return false;
                }
                catch (JsonConfigurationException ex)
                {
                    Console.WriteLine($"JSON configuration error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> LoadJsonConfigurationFromFileAsync(string filePath)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var jsonConfig = await JsonConfigurationService.ParseConfigurationFromFileAsync(filePath);
                    if (jsonConfig != null)
                    {
                        Console.WriteLine($"JSON configuration loaded from file: {filePath}");
                        return await LoadJsonConfigurationAsync(System.Text.Json.JsonSerializer.Serialize(jsonConfig));
                    }
                    return false;
                }
                catch (JsonConfigurationException ex)
                {
                    Console.WriteLine($"JSON configuration file error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> ApplyStyleConfigurationAsync(string styleJson)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"Applying style configuration: {styleJson}");
                    // TODO: Parse and apply styling in console context
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Style configuration error: {ex.Message}");
                    return false;
                }
            });
        }

        public async Task<bool> UpdateThemeAsync(string theme)
        {
            return await Task.Run(() =>
            {
                Console.WriteLine($"Theme updated to: {theme}");
                // TODO: Apply theme in console context (colors, etc.)
                return true;
            });
        }

        // Phase 5: Shell Command Execution Integration Implementation
        public async Task<CommandExecutionResult> ExecuteShellCommandAsync(string command, string? workingDirectory = null)
        {
            _shellService ??= new ShellCommandService();
            _shellService.OutputReceived += OnShellOutputReceived;
            
            Console.WriteLine($"Executing shell command: {command}");
            if (!ShellCommandService.IsCommandSafe(command))
            {
                Console.WriteLine($"⚠️ Warning: Command may not be safe: {command}");
            }
            
            var result = await _shellService.ExecuteCommandAsync(command, workingDirectory);
            Console.WriteLine($"Command completed: {result}");
            
            _shellService.OutputReceived -= OnShellOutputReceived;
            return result;
        }

        public async Task<CommandExecutionResult> ExecutePowerShellScriptAsync(string script, string? workingDirectory = null)
        {
            _shellService ??= new ShellCommandService();
            _shellService.OutputReceived += OnShellOutputReceived;
            
            Console.WriteLine($"Executing PowerShell script: {script}");
            var result = await _shellService.ExecutePowerShellAsync(script, workingDirectory);
            Console.WriteLine($"PowerShell completed: {result}");
            
            _shellService.OutputReceived -= OnShellOutputReceived;
            return result;
        }

        public async Task<CommandExecutionResult> ExecuteAndCaptureOutputAsync(string command, string? workingDirectory = null)
        {
            _shellService ??= new ShellCommandService();
            
            Console.WriteLine($"Executing and capturing: {command}");
            var result = await _shellService.ExecuteAndCaptureAsync(command, workingDirectory);
            
            Console.WriteLine($"Captured output ({result.StandardOutput.Length} chars):");
            if (!string.IsNullOrEmpty(result.StandardOutput))
            {
                Console.WriteLine(result.StandardOutput);
            }
            if (!string.IsNullOrEmpty(result.StandardError))
            {
                Console.WriteLine($"Errors: {result.StandardError}");
            }
            
            return result;
        }

        private void OnShellOutputReceived(object? sender, CommandOutputEventArgs e)
        {
            var prefix = e.IsError ? "ERR" : "OUT";
            Console.WriteLine($"[{prefix}] {e.Output}");
            CommandOutputReceived?.Invoke(this, e);
        }

        private bool ProcessListItemCommand(Command command)
        {
            if (command.Parameters.ContainsKey("title"))
            {
                var title = command.Parameters["title"];
                var status = command.Parameters.ContainsKey("status") 
                    ? StatusIconProvider.FromString(command.Parameters["status"]) 
                    : ListItemStatus.None;
                var statusText = command.Parameters.ContainsKey("statustext") 
                    ? command.Parameters["statustext"] 
                    : "";

                Console.WriteLine($"List item command: '{title}' -> {status} ({statusText})");
                return true;
            }
            else if (command.Parameters.ContainsKey("index"))
            {
                if (int.TryParse(command.Parameters["index"], out int index))
                {
                    var status = command.Parameters.ContainsKey("status") 
                        ? StatusIconProvider.FromString(command.Parameters["status"]) 
                        : ListItemStatus.None;
                    var statusText = command.Parameters.ContainsKey("statustext") 
                        ? command.Parameters["statustext"] 
                        : "";

                    Console.WriteLine($"List item command [index {index}]: {status} ({statusText})");
                    return true;
                }
            }
            
            return false;
        }

        private bool ProcessListCommand(Command command)
        {
            if (command.Parameters.ContainsKey("action") && 
                command.Parameters["action"] == "clear")
            {
                Console.WriteLine("Clearing all list items");
                return true;
            }
            
            return false;
        }
    }
}

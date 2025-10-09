using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using csharpDialog.Core;
using csharpDialog.Core.Models;
using CSharpDialog.Core.Models;
using csharpDialog.Core.Services;

namespace csharpDialog.WPF.Services
{
    public class WpfDialogService : IDialogService
    {
        private ProgressDialogWindow? _window;
        private ICommandFileMonitor? _commandMonitor;
        private DialogConfiguration? _configuration;
        private readonly Dictionary<string, ListItemControl> _listItems = new();
        private int _nextListItemIndex = 0;
        private bool _isDarkMode = false;

        public event EventHandler<CommandReceivedEventArgs>? CommandReceived;
        public bool IsCommandMonitoringActive => _commandMonitor?.IsMonitoring ?? false;

        public async Task<DialogResult> ShowDialogAsync(DialogConfiguration configuration)
        {
            return await Task.Run(() => ShowDialog(configuration));
        }

        public DialogResult ShowDialog(DialogConfiguration configuration)
        {
            _configuration = configuration;
            DialogResult result = new DialogResult { ButtonPressed = "ok", Timestamp = DateTime.UtcNow };

            Application.Current?.Dispatcher.Invoke(() =>
            {
                _window = new ProgressDialogWindow();
                
                // Detect dark mode from the window
                _isDarkMode = IsSystemDarkMode();
                
                Console.WriteLine($"[DEBUG] ProgressDialogWindow created (Dark Mode: {_isDarkMode})");
                
                // Apply configuration immediately for window size and title
                if (configuration.Width > 0)
                    _window.Width = configuration.Width;
                
                if (configuration.Height > 0)
                    _window.Height = configuration.Height;
                
                if (!string.IsNullOrEmpty(configuration.Title))
                    _window.Title = configuration.Title;
                
                // Wait for window to load before accessing content elements
                _window.Loaded += (s, e) =>
                {
                    Console.WriteLine($"[DEBUG] Window Loaded event fired");
                    
                    // Find XAML elements by name AFTER window loads
                    var titleText = _window.FindName("TitleText") as TextBlock;
                    var messageText = _window.FindName("MessageText") as TextBlock;
                    var listItemsPanel = _window.FindName("ListItemsPanel") as StackPanel;
                    
                    Console.WriteLine($"[DEBUG] FindName results: TitleText={titleText != null}, MessageText={messageText != null}, ListItemsPanel={listItemsPanel != null}");
                    
                    // Apply configuration
                    if (titleText != null && !string.IsNullOrEmpty(configuration.Title))
                        titleText.Text = configuration.Title;
                    
                    if (messageText != null && !string.IsNullOrEmpty(configuration.Message))
                        messageText.Text = configuration.Message;

                    // Clear sample items ONLY if they exist
                    if (listItemsPanel != null)
                    {
                        var childCount = listItemsPanel.Children.Count;
                        listItemsPanel.Children.Clear();
                        Console.WriteLine($"[DEBUG] Cleared {childCount} sample items from ListItemsPanel");
                    }
                    
                    Console.WriteLine($"[DEBUG] Window loaded and ready for commands");
                };

                // Start command monitoring if enabled
                if (configuration.EnableCommandFile && !string.IsNullOrEmpty(configuration.CommandFilePath))
                {
                    Console.WriteLine($"[DEBUG] Starting command file monitoring: {configuration.CommandFilePath}");
                    Task.Run(async () =>
                    {
                        // Wait a bit for window to load
                        await Task.Delay(1000);
                        await StartCommandMonitoringAsync(configuration.CommandFilePath);
                    });
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Command monitoring NOT enabled. EnableCommandFile={configuration.EnableCommandFile}, Path={configuration.CommandFilePath}");
                }

                _window.ShowDialog();
            });

            return result;
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
        }

        public void StopCommandMonitoring()
        {
            if (_commandMonitor != null)
            {
                _commandMonitor.CommandReceived -= OnCommandReceived;
                _commandMonitor.ErrorOccurred -= OnCommandError;
                _commandMonitor.Dispose();
                _commandMonitor = null;
            }
        }

        private void OnCommandReceived(object? sender, CommandReceivedEventArgs e)
        {
            Console.WriteLine($"[DEBUG] OnCommandReceived - Type: {e.Command.Type}, Value: {e.Command.Value}");
            
            if (_window != null)
            {
                // Process command synchronously on UI thread
                _window.Dispatcher.Invoke(() =>
                {
                    Console.WriteLine($"[DEBUG] Dispatcher.Invoke - Processing command on UI thread...");
                    ProcessCommandSync(e.Command);
                    Console.WriteLine($"[DEBUG] Dispatcher.Invoke - Command processing complete");
                });
            }
            else
            {
                Console.WriteLine($"[DEBUG] Window is null, cannot process command!");
            }
            
            CommandReceived?.Invoke(this, e);
        }
        
        // Synchronous command processing (already on UI thread)
        private void ProcessCommandSync(Command command)
        {
            try
            {
                Console.WriteLine($"[DEBUG] ProcessCommandSync - Type: {command.Type}");
                
                switch (command.Type.ToLowerInvariant())
                {
                    case "title":
                        var titleText = _window!.FindName("TitleText") as TextBlock;
                        if (titleText != null)
                            titleText.Text = command.Value;
                        break;

                    case "message":
                        var messageText = _window!.FindName("MessageText") as TextBlock;
                        if (messageText != null)
                            messageText.Text = command.Value;
                        break;

                    case "progress":
                        if (int.TryParse(command.Value, out int progress))
                        {
                            var progressBar = _window!.FindName("ProgressBarControl") as ProgressBar;
                            if (progressBar != null)
                                progressBar.Value = progress;
                        }
                        break;

                    case "progresstext":
                        var progressText = _window!.FindName("ProgressText") as TextBlock;
                        if (progressText != null)
                            progressText.Text = command.Value;
                        break;

                    case "listitem":
                        Console.WriteLine($"[DEBUG] Processing listitem command");
                        ProcessListItemCommand(command);
                        break;

                    case "quit":
                        _window!.Close();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] ERROR in ProcessCommandSync: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }

        private void OnCommandError(object? sender, CommandFileErrorEventArgs e)
        {
            Console.WriteLine($"Command file error: {e.Message}");
        }

        public async Task<bool> ProcessCommandAsync(Command command)
        {
            if (_window == null) return false;

            await _window.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    switch (command.Type.ToLowerInvariant())
                    {
                        case "title":
                            var titleText = _window.FindName("TitleText") as TextBlock;
                            if (titleText != null)
                                titleText.Text = command.Value;
                            break;

                        case "message":
                            var messageText = _window.FindName("MessageText") as TextBlock;
                            if (messageText != null)
                                messageText.Text = command.Value;
                            break;

                        case "progress":
                            if (int.TryParse(command.Value, out int progress))
                            {
                                var progressBar = _window.FindName("ProgressBarControl") as ProgressBar;
                                if (progressBar != null)
                                    progressBar.Value = progress;
                            }
                            break;

                        case "progresstext":
                            var progressText = _window.FindName("ProgressText") as TextBlock;
                            if (progressText != null)
                                progressText.Text = command.Value;
                            break;

                        case "listitem":
                            ProcessListItemCommand(command);
                            break;

                        case "quit":
                            _window.Close();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command: {ex.Message}");
                }
            });

            return true;
        }

        private void ProcessListItemCommand(Command command)
        {
            if (_window == null) return;

            // Parse listitem command parameters
            // The action can be in different places depending on the format:
            // 1. As a parameter key: "listitem: add, title: X" → Parameters["add"] exists
            // 2. As "action" parameter: "listitem: action: add, title: X" → Parameters["action"] = "add"
            // 3. As first word of Value before comma: "listitem: add, title: X" → Value = "add, title: X"
            
            string action = "update"; // default
            
            // Check if action is first word before comma in Value
            if (!string.IsNullOrEmpty(command.Value))
            {
                var firstComma = command.Value.IndexOf(',');
                if (firstComma > 0)
                {
                    var firstPart = command.Value.Substring(0, firstComma).Trim().ToLowerInvariant();
                    if (firstPart == "add" || firstPart == "update" || firstPart == "delete")
                    {
                        action = firstPart;
                    }
                }
                else if (command.Value.ToLowerInvariant() is "add" or "update" or "delete")
                {
                    action = command.Value.ToLowerInvariant();
                }
            }
            
            // Check parameters dictionary as fallback
            if (action == "update")
            {
                action = command.Parameters.GetValueOrDefault("action", "update");
                
                // Check if "add", "update", or "delete" exist as keys (swiftDialog format)
                if (command.Parameters.ContainsKey("add"))
                    action = "add";
                else if (command.Parameters.ContainsKey("update"))
                    action = "update";
                else if (command.Parameters.ContainsKey("delete"))
                    action = "delete";
            }
            
            string? title = command.Parameters.GetValueOrDefault("title");
            string? status = command.Parameters.GetValueOrDefault("status");
            string? statusText = command.Parameters.GetValueOrDefault("statustext");
            string? indexStr = command.Parameters.GetValueOrDefault("index");

            Console.WriteLine($"[DEBUG] ProcessListItemCommand - action:{action}, title:{title}, status:{status}, statusText:{statusText}");

            if (action == "add" && !string.IsNullOrEmpty(title))
            {
                // Add new list item
                Console.WriteLine($"[DEBUG] Adding list item: {title}, status: {status}, statusText: {statusText}");
                var listItem = new ListItemControl(title, _nextListItemIndex, _isDarkMode);
                
                // Set initial status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    Console.WriteLine($"[DEBUG] Setting initial status: {status}");
                    listItem.UpdateStatus(status);
                }
                if (!string.IsNullOrEmpty(statusText))
                {
                    Console.WriteLine($"[DEBUG] Setting initial statusText: {statusText}");
                    listItem.UpdateStatusText(statusText);
                }
                
                _listItems[title] = listItem;
                Console.WriteLine($"[DEBUG] Added to _listItems dictionary. Total tracked items: {_listItems.Count}");
                
                // Add to UI - try multiple times to find the panel
                StackPanel? listItemsPanel = null;
                for (int attempt = 0; attempt < 5 && listItemsPanel == null; attempt++)
                {
                    listItemsPanel = _window.FindName("ListItemsPanel") as StackPanel;
                    if (listItemsPanel == null && attempt < 4)
                    {
                        Console.WriteLine($"[DEBUG] FindName(ListItemsPanel) returned NULL, attempt {attempt + 1}/5, retrying...");
                        System.Threading.Thread.Sleep(100);
                    }
                }
                
                Console.WriteLine($"[DEBUG] FindName(ListItemsPanel) returned: {(listItemsPanel != null ? "SUCCESS" : "NULL AFTER 5 ATTEMPTS")}");
                
                if (listItemsPanel != null)
                {
                    Console.WriteLine($"[DEBUG] ListItemsPanel.Children.Count before add: {listItemsPanel.Children.Count}");
                    listItemsPanel.Children.Add(listItem.Element);
                    Console.WriteLine($"[DEBUG] ListItemsPanel.Children.Count after add: {listItemsPanel.Children.Count}");
                    Console.WriteLine($"[DEBUG] ✓ List item successfully added to UI!");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] ✗ FATAL: ListItemsPanel is NULL after 5 attempts - cannot add item to UI!");
                    Console.WriteLine($"[DEBUG] Window state: IsLoaded={_window.IsLoaded}, IsVisible={_window.IsVisible}");
                }
                
                _nextListItemIndex++;
            }
            else
            {
                // Update existing item by title or index
                ListItemControl? item = null;

                if (!string.IsNullOrEmpty(title) && _listItems.ContainsKey(title))
                {
                    item = _listItems[title];
                }
                else if (!string.IsNullOrEmpty(indexStr) && int.TryParse(indexStr, out int index))
                {
                    item = _listItems.Values.FirstOrDefault(i => i.Index == index);
                }

                if (item != null)
                {
                    Console.WriteLine($"[DEBUG] Updating list item: {title ?? $"index {indexStr}"}");
                    if (!string.IsNullOrEmpty(status))
                        item.UpdateStatus(status);
                    
                    if (!string.IsNullOrEmpty(statusText))
                        item.UpdateStatusText(statusText);
                }
                else
                {
                    Console.WriteLine($"[DEBUG] List item not found for update: {title ?? $"index {indexStr}"}");
                }
            }
        }

        public Task<bool> UpdateListItemAsync(string title, ListItemStatus status, string statusText = "")
        {
            return ProcessCommandAsync(new Command
            {
                Type = "listitem",
                Parameters = new Dictionary<string, string>
                {
                    ["title"] = title,
                    ["status"] = status.ToString().ToLowerInvariant(),
                    ["statustext"] = statusText
                }
            });
        }

        public Task<bool> UpdateListItemByIndexAsync(int index, ListItemStatus status, string statusText = "")
        {
            return ProcessCommandAsync(new Command
            {
                Type = "listitem",
                Parameters = new Dictionary<string, string>
                {
                    ["index"] = index.ToString(),
                    ["status"] = status.ToString().ToLowerInvariant(),
                    ["statustext"] = statusText
                }
            });
        }

        public Task<bool> AddListItemAsync(string title, ListItemStatus status = ListItemStatus.Pending, string statusText = "")
        {
            return ProcessCommandAsync(new Command
            {
                Type = "listitem",
                Parameters = new Dictionary<string, string>
                {
                    ["action"] = "add",
                    ["title"] = title,
                    ["status"] = status.ToString().ToLowerInvariant(),
                    ["statustext"] = string.IsNullOrEmpty(statusText) ? "Pending" : statusText
                }
            });
        }

        public Task<bool> RemoveListItemAsync(string title)
        {
            // Not implemented yet
            return Task.FromResult(false);
        }

        public Task<bool> RemoveListItemByIndexAsync(int index)
        {
            // Not implemented yet
            return Task.FromResult(false);
        }

        public Task<bool> ClearListItemsAsync()
        {
            if (_window != null)
            {
                _window.Dispatcher.Invoke(() =>
                {
                    var listItemsPanel = _window.FindName("ListItemsPanel") as StackPanel;
                    if (listItemsPanel != null)
                    {
                        listItemsPanel.Children.Clear();
                    }
                    _listItems.Clear();
                    _nextListItemIndex = 0;
                });
            }
            return Task.FromResult(true);
        }

        public Task<bool> UpdateProgressAsync(int value, string text = "")
        {
            Application.Current?.Dispatcher.InvokeAsync(async () =>
            {
                await ProcessCommandAsync(new Command { Type = "progress", Value = value.ToString() });
                if (!string.IsNullOrEmpty(text))
                    await ProcessCommandAsync(new Command { Type = "progresstext", Value = text });
            });
            return Task.FromResult(true);
        }

        public Task<bool> SetProgressAsync(int value, string? text = null)
        {
            return UpdateProgressAsync(value, text ?? "");
        }

        public Task<bool> IncrementProgressAsync(int increment, string? text = null)
        {
            if (_window != null)
            {
                _window.Dispatcher.Invoke(() =>
                {
                    var progressBar = _window.FindName("ProgressBarControl") as ProgressBar;
                    if (progressBar != null)
                    {
                        var newValue = (int)progressBar.Value + increment;
                        progressBar.Value = Math.Min(newValue, 100);
                    }
                    if (!string.IsNullOrEmpty(text))
                    {
                        var progressText = _window.FindName("ProgressText") as TextBlock;
                        if (progressText != null)
                            progressText.Text = text;
                    }
                });
            }
            return Task.FromResult(true);
        }

        public Task<bool> ResetProgressAsync(string? text = null)
        {
            return SetProgressAsync(0, text);
        }

        public Task<bool> UpdateProgressTextAsync(string text)
        {
            return ProcessCommandAsync(new Command { Type = "progresstext", Value = text });
        }

        public Task<bool> UpdateTitleAsync(string title)
        {
            return ProcessCommandAsync(new Command { Type = "title", Value = title });
        }

        public Task<bool> UpdateMessageAsync(string message)
        {
            return ProcessCommandAsync(new Command { Type = "message", Value = message });
        }

        // Stub implementations for other interface methods
        public event EventHandler<CommandOutputEventArgs>? CommandOutputReceived;
        
        private bool IsSystemDarkMode()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                // If AppsUseLightTheme is 0, dark mode is enabled
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // Default to light mode if we can't read the registry
            }
        }
        
        public Task<bool> LoadJsonConfigurationAsync(string jsonConfig) => Task.FromResult(false);
        public Task<bool> LoadJsonConfigurationFromFileAsync(string filePath) => Task.FromResult(false);
        public Task<bool> ApplyStyleConfigurationAsync(string styleJson) => Task.FromResult(false);
        public Task<bool> UpdateThemeAsync(string theme) => Task.FromResult(false);
        public Task<bool> ApplyDialogThemeAsync(string themeName) => Task.FromResult(false);
        public Task<bool> ApplyCustomThemeAsync(ThemeConfiguration theme) => Task.FromResult(false);
        public Task<bool> ApplyStylePropertyAsync(string element, string property, object value) => Task.FromResult(false);
        public Task<bool> ApplyStyleSheetAsync(StyleSheet styleSheet) => Task.FromResult(false);
        public Task<bool> ApplyBrandingAsync(BrandConfiguration branding) => Task.FromResult(false);
        public Task<bool> ApplyAnimationAsync(string animationType, Dictionary<string, object> parameters) => Task.FromResult(false);
        public Task<List<string>> GetAvailableThemesAsync() => Task.FromResult(new List<string>());
        public Task<Dictionary<string, object>> GetSupportedStylePropertiesAsync(string element) => Task.FromResult(new Dictionary<string, object>());
        
        public Task<CommandExecutionResult> ExecuteShellCommandAsync(string command, string? workingDirectory = null) 
            => Task.FromResult(new CommandExecutionResult());
        public Task<CommandExecutionResult> ExecutePowerShellScriptAsync(string script, string? workingDirectory = null)
            => Task.FromResult(new CommandExecutionResult());
        public Task<CommandExecutionResult> ExecuteAndCaptureOutputAsync(string command, string? workingDirectory = null)
            => Task.FromResult(new CommandExecutionResult());
    }

    // Helper class to manage list item UI elements
    internal class ListItemControl
    {
        public Border Element { get; }
        public int Index { get; }
        private readonly TextBlock _titleText;
        private readonly TextBlock _statusText;
        private readonly Ellipse _statusIcon;
        private readonly bool _isDarkMode;

        public ListItemControl(string title, int index, bool isDarkMode = false)
        {
            Index = index;
            _isDarkMode = isDarkMode;

            // Create the UI element
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Status icon (circle)
            _statusIcon = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = new SolidColorBrush(Colors.Gray),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_statusIcon, 0);
            grid.Children.Add(_statusIcon);

            // Title - theme aware
            _titleText = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(_isDarkMode ? Color.FromRgb(240, 240, 240) : Color.FromRgb(51, 51, 51)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_titleText, 1);
            grid.Children.Add(_titleText);

            // Status text - theme aware
            _statusText = new TextBlock
            {
                Text = "Pending",
                FontSize = 12,
                Foreground = new SolidColorBrush(_isDarkMode ? Color.FromRgb(180, 180, 180) : Color.FromRgb(102, 102, 102)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };
            Grid.SetColumn(_statusText, 2);
            grid.Children.Add(_statusText);

            // Border container - theme aware with standard Windows styling
            Element = new Border
            {
                Background = new SolidColorBrush(_isDarkMode ? Color.FromRgb(40, 40, 40) : Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(_isDarkMode ? Color.FromRgb(60, 60, 60) : Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(12, 10, 12, 10),
                Child = grid
            };
        }

        public void UpdateStatus(string status)
        {
            Color color = status.ToLowerInvariant() switch
            {
                "success" => Colors.Green,
                "wait" => Colors.Orange,
                "fail" => Colors.Red,
                "error" => Colors.Red,
                _ => Colors.Gray
            };

            _statusIcon.Fill = new SolidColorBrush(color);
        }

        public void UpdateStatusText(string text)
        {
            _statusText.Text = text;
        }
    }
}

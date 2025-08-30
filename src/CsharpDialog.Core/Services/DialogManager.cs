using System;
using System.Threading.Tasks;
using CSharpDialog.Core.Models;
using csharpDialog.Core;
using csharpDialog.Core.Models;

namespace CSharpDialog.Core.Services
{
    public class DialogManager : IDialogManager
    {
        private readonly IDialogService _dialogService;
        private readonly IThemeService _themeService;
        private readonly IStylingService _stylingService;
        private DialogConfiguration? _currentConfiguration;
        private bool _isDialogOpen = false;

        public DialogManager(IDialogService dialogService, IThemeService? themeService = null, IStylingService? stylingService = null)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _themeService = themeService ?? new ThemeService();
            _stylingService = stylingService ?? new StylingService();
        }

        public bool IsDialogOpen => _isDialogOpen;

        public async Task SendCommandAsync(string command)
        {
            try
            {
                await ProcessDialogCommandAsync(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing dialog command '{command}': {ex.Message}");
            }
        }

        public async Task<bool> UpdateStyleAsync(string element, string property, object value)
        {
            try
            {
                return await _stylingService.ApplyStyleAsync(element, property, value, this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating style {element}.{property}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApplyThemeAsync(string themeName)
        {
            try
            {
                return await _themeService.ApplyThemeAsync(themeName, this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme '{themeName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ShowDialogAsync()
        {
            try
            {
                if (_currentConfiguration == null)
                {
                    _currentConfiguration = new DialogConfiguration
                    {
                        Title = "CSharpDialog",
                        Message = "Dialog initialized"
                    };
                }

                _isDialogOpen = true;
                await _dialogService.ShowDialogAsync(_currentConfiguration);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing dialog: {ex.Message}");
                _isDialogOpen = false;
                return false;
            }
        }

        public async Task CloseDialogAsync()
        {
            try
            {
                await Task.Delay(0); // Async consistency
                _isDialogOpen = false;
                Console.WriteLine("Dialog closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing dialog: {ex.Message}");
            }
        }

        private async Task ProcessDialogCommandAsync(string command)
        {
            await Task.Delay(0); // Async consistency

            var parts = command.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2) return;

            var commandType = parts[0].ToLowerInvariant();
            var commandValue = parts[1];

            switch (commandType)
            {
                case "setstyle":
                    await ProcessSetStyleCommand(commandValue);
                    break;
                case "applytheme":
                    await ProcessApplyThemeCommand(commandValue);
                    break;
                case "setlogo":
                    await ProcessSetLogoCommand(commandValue);
                    break;
                case "setwatermark":
                    await ProcessSetWatermarkCommand(commandValue);
                    break;
                case "applycss":
                    await ProcessApplyCssCommand(commandValue);
                    break;
                case "animate":
                    await ProcessAnimateCommand(commandValue);
                    break;
                case "title":
                    await ProcessTitleCommand(commandValue);
                    break;
                case "message":
                    await ProcessMessageCommand(commandValue);
                    break;
                case "quit":
                case "close":
                    await CloseDialogAsync();
                    break;
                default:
                    Console.WriteLine($"Unknown dialog command: {commandType}");
                    break;
            }
        }

        private async Task ProcessSetStyleCommand(string commandValue)
        {
            var parts = commandValue.Split(',', 3, StringSplitOptions.TrimEntries);
            if (parts.Length == 3)
            {
                var element = parts[0];
                var property = parts[1];
                var value = parts[2];
                
                await _stylingService.ApplyStyleAsync(element, property, value, this);
                Console.WriteLine($"Style applied: {element}.{property} = {value}");
            }
        }

        private async Task ProcessApplyThemeCommand(string themeName)
        {
            var success = await _themeService.ApplyThemeAsync(themeName, this);
            if (success)
            {
                Console.WriteLine($"Theme '{themeName}' applied successfully");
            }
            else
            {
                Console.WriteLine($"Failed to apply theme '{themeName}'");
            }
        }

        private async Task ProcessSetLogoCommand(string logoPath)
        {
            await Task.Delay(0); // Async consistency
            Console.WriteLine($"Logo set: {logoPath}");
            // TODO: Implement logo setting logic
        }

        private async Task ProcessSetWatermarkCommand(string watermarkText)
        {
            await Task.Delay(0); // Async consistency
            Console.WriteLine($"Watermark set: {watermarkText}");
            // TODO: Implement watermark setting logic
        }

        private async Task ProcessApplyCssCommand(string css)
        {
            await Task.Delay(0); // Async consistency
            Console.WriteLine($"CSS applied: {css}");
            // TODO: Implement CSS application logic
        }

        private async Task ProcessAnimateCommand(string animationConfig)
        {
            await Task.Delay(0); // Async consistency
            Console.WriteLine($"Animation applied: {animationConfig}");
            // TODO: Implement animation logic
        }

        private async Task ProcessTitleCommand(string title)
        {
            await Task.Delay(0); // Async consistency
            if (_currentConfiguration != null)
            {
                _currentConfiguration.Title = title;
                Console.WriteLine($"Title updated: {title}");
            }
        }

        private async Task ProcessMessageCommand(string message)
        {
            await Task.Delay(0); // Async consistency
            if (_currentConfiguration != null)
            {
                _currentConfiguration.Message = message;
                Console.WriteLine($"Message updated: {message}");
            }
        }

        public void SetConfiguration(DialogConfiguration configuration)
        {
            _currentConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public DialogConfiguration? GetConfiguration()
        {
            return _currentConfiguration;
        }
    }
}

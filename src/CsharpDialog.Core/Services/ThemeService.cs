using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpDialog.Core.Models;

namespace CSharpDialog.Core.Services
{
    public interface IThemeService
    {
        Task<ThemeConfiguration> GetThemeAsync(string themeName);
        Task<bool> ApplyThemeAsync(string themeName, IDialogManager dialogManager);
        Task<bool> ApplyCustomThemeAsync(ThemeConfiguration theme, IDialogManager dialogManager);
        Task<List<string>> GetAvailableThemesAsync();
        Task<ThemeConfiguration> CreateCustomThemeAsync(Dictionary<string, object> styleProperties);
        Task<bool> ValidateThemeAsync(ThemeConfiguration theme);
    }

    public class ThemeService : IThemeService
    {
        private readonly Dictionary<string, ThemeConfiguration> _predefinedThemes;

        public ThemeService()
        {
            _predefinedThemes = InitializePredefinedThemes();
        }

        public async Task<ThemeConfiguration> GetThemeAsync(string themeName)
        {
            await Task.Delay(0); // Async consistency
            
            if (_predefinedThemes.ContainsKey(themeName.ToLower()))
            {
                return _predefinedThemes[themeName.ToLower()];
            }
            
            return _predefinedThemes["corporate"]; // Default fallback
        }

        public async Task<bool> ApplyThemeAsync(string themeName, IDialogManager dialogManager)
        {
            try
            {
                var theme = await GetThemeAsync(themeName);
                return await ApplyCustomThemeAsync(theme, dialogManager);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme '{themeName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApplyCustomThemeAsync(ThemeConfiguration theme, IDialogManager dialogManager)
        {
            try
            {
                await Task.Delay(0); // Async consistency
                
                if (!await ValidateThemeAsync(theme))
                {
                    return false;
                }

                // Apply theme properties to dialog manager
                if (theme.WindowStyle != null)
                {
                    foreach (var style in theme.WindowStyle)
                    {
                        await ApplyStyleProperty(dialogManager, "window", style.Key, style.Value);
                    }
                }

                if (theme.ButtonStyle != null)
                {
                    foreach (var style in theme.ButtonStyle)
                    {
                        await ApplyStyleProperty(dialogManager, "button", style.Key, style.Value);
                    }
                }

                if (theme.ProgressStyle != null)
                {
                    foreach (var style in theme.ProgressStyle)
                    {
                        await ApplyStyleProperty(dialogManager, "progress", style.Key, style.Value);
                    }
                }

                if (theme.TextStyle != null)
                {
                    foreach (var style in theme.TextStyle)
                    {
                        await ApplyStyleProperty(dialogManager, "text", style.Key, style.Value);
                    }
                }

                if (theme.ListStyle != null)
                {
                    foreach (var style in theme.ListStyle)
                    {
                        await ApplyStyleProperty(dialogManager, "list", style.Key, style.Value);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying custom theme: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetAvailableThemesAsync()
        {
            await Task.Delay(0); // Async consistency
            return new List<string>(_predefinedThemes.Keys);
        }

        public async Task<ThemeConfiguration> CreateCustomThemeAsync(Dictionary<string, object> styleProperties)
        {
            await Task.Delay(0); // Async consistency
            
            var theme = new ThemeConfiguration
            {
                Name = "Custom",
                Description = "Custom theme created from style properties",
                WindowStyle = new Dictionary<string, object>(),
                ButtonStyle = new Dictionary<string, object>(),
                ProgressStyle = new Dictionary<string, object>(),
                TextStyle = new Dictionary<string, object>(),
                ListStyle = new Dictionary<string, object>()
            };

            // Parse style properties and categorize them
            foreach (var property in styleProperties)
            {
                var parts = property.Key.Split('.');
                if (parts.Length == 2)
                {
                    var category = parts[0].ToLower();
                    var styleName = parts[1];

                    switch (category)
                    {
                        case "window":
                            theme.WindowStyle[styleName] = property.Value;
                            break;
                        case "button":
                            theme.ButtonStyle[styleName] = property.Value;
                            break;
                        case "progress":
                            theme.ProgressStyle[styleName] = property.Value;
                            break;
                        case "text":
                            theme.TextStyle[styleName] = property.Value;
                            break;
                        case "list":
                            theme.ListStyle[styleName] = property.Value;
                            break;
                    }
                }
            }

            return theme;
        }

        public async Task<bool> ValidateThemeAsync(ThemeConfiguration theme)
        {
            await Task.Delay(0); // Async consistency
            
            if (theme == null || string.IsNullOrEmpty(theme.Name))
            {
                return false;
            }

            // Validate required properties exist
            return true; // Basic validation - can be expanded
        }

        private async Task ApplyStyleProperty(IDialogManager dialogManager, string element, string property, object value)
        {
            try
            {
                await Task.Delay(0); // Async consistency
                
                // Convert value to string for command
                var valueStr = value?.ToString() ?? string.Empty;
                
                // Send style command to dialog manager
                var command = $"setstyle: {element}, {property}, {valueStr}";
                await dialogManager.SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying style property {element}.{property}: {ex.Message}");
            }
        }

        private Dictionary<string, ThemeConfiguration> InitializePredefinedThemes()
        {
            var themes = new Dictionary<string, ThemeConfiguration>();

            // Corporate Theme
            themes["corporate"] = new ThemeConfiguration
            {
                Name = "Corporate",
                Description = "Professional corporate theme with blue accents",
                WindowStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#FFFFFF" },
                    { "borderColor", "#0078D4" },
                    { "borderWidth", "2" },
                    { "cornerRadius", "8" },
                    { "shadow", "true" },
                    { "shadowOpacity", "0.3" }
                },
                ButtonStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#0078D4" },
                    { "foregroundColor", "#FFFFFF" },
                    { "borderColor", "#106EBE" },
                    { "borderWidth", "1" },
                    { "cornerRadius", "4" },
                    { "fontSize", "14" },
                    { "fontWeight", "Medium" },
                    { "padding", "12,8" }
                },
                ProgressStyle = new Dictionary<string, object>
                {
                    { "foregroundColor", "#0078D4" },
                    { "backgroundColor", "#F3F2F1" },
                    { "borderColor", "#D2D0CE" },
                    { "height", "20" },
                    { "cornerRadius", "10" },
                    { "animationSpeed", "300" }
                },
                TextStyle = new Dictionary<string, object>
                {
                    { "titleColor", "#323130" },
                    { "messageColor", "#605E5C" },
                    { "titleFontSize", "18" },
                    { "messageFontSize", "14" },
                    { "titleFontWeight", "SemiBold" },
                    { "messageFontWeight", "Regular" }
                },
                ListStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#FAFAFA" },
                    { "borderColor", "#D2D0CE" },
                    { "alternateRowColor", "#F8F8F8" },
                    { "textColor", "#323130" },
                    { "statusColorSuccess", "#107C10" },
                    { "statusColorError", "#D13438" },
                    { "statusColorWarning", "#FF8C00" },
                    { "statusColorInfo", "#0078D4" }
                }
            };

            // Dark Theme
            themes["dark"] = new ThemeConfiguration
            {
                Name = "Dark",
                Description = "Modern dark theme with accent colors",
                WindowStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#1E1E1E" },
                    { "borderColor", "#404040" },
                    { "borderWidth", "1" },
                    { "cornerRadius", "8" },
                    { "shadow", "true" },
                    { "shadowOpacity", "0.5" }
                },
                ButtonStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#0E639C" },
                    { "foregroundColor", "#FFFFFF" },
                    { "borderColor", "#1177BB" },
                    { "borderWidth", "1" },
                    { "cornerRadius", "4" },
                    { "fontSize", "14" },
                    { "fontWeight", "Medium" },
                    { "padding", "12,8" }
                },
                ProgressStyle = new Dictionary<string, object>
                {
                    { "foregroundColor", "#0E639C" },
                    { "backgroundColor", "#3C3C3C" },
                    { "borderColor", "#505050" },
                    { "height", "20" },
                    { "cornerRadius", "10" },
                    { "animationSpeed", "300" }
                },
                TextStyle = new Dictionary<string, object>
                {
                    { "titleColor", "#FFFFFF" },
                    { "messageColor", "#CCCCCC" },
                    { "titleFontSize", "18" },
                    { "messageFontSize", "14" },
                    { "titleFontWeight", "SemiBold" },
                    { "messageFontWeight", "Regular" }
                },
                ListStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#2D2D30" },
                    { "borderColor", "#404040" },
                    { "alternateRowColor", "#252526" },
                    { "textColor", "#CCCCCC" },
                    { "statusColorSuccess", "#4EC9B0" },
                    { "statusColorError", "#F44747" },
                    { "statusColorWarning", "#FFCC02" },
                    { "statusColorInfo", "#569CD6" }
                }
            };

            // Modern Theme
            themes["modern"] = new ThemeConfiguration
            {
                Name = "Modern",
                Description = "Clean modern theme with subtle animations",
                WindowStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#F8F9FA" },
                    { "borderColor", "#DEE2E6" },
                    { "borderWidth", "1" },
                    { "cornerRadius", "12" },
                    { "shadow", "true" },
                    { "shadowOpacity", "0.15" },
                    { "shadowBlur", "20" }
                },
                ButtonStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#6F42C1" },
                    { "foregroundColor", "#FFFFFF" },
                    { "borderColor", "transparent" },
                    { "borderWidth", "0" },
                    { "cornerRadius", "8" },
                    { "fontSize", "14" },
                    { "fontWeight", "Medium" },
                    { "padding", "14,20" },
                    { "transition", "all 0.2s ease" }
                },
                ProgressStyle = new Dictionary<string, object>
                {
                    { "foregroundColor", "#6F42C1" },
                    { "backgroundColor", "#E9ECEF" },
                    { "borderColor", "transparent" },
                    { "height", "8" },
                    { "cornerRadius", "4" },
                    { "animationSpeed", "400" },
                    { "gradient", "linear-gradient(90deg, #6F42C1, #8B5FBF)" }
                },
                TextStyle = new Dictionary<string, object>
                {
                    { "titleColor", "#212529" },
                    { "messageColor", "#6C757D" },
                    { "titleFontSize", "20" },
                    { "messageFontSize", "16" },
                    { "titleFontWeight", "Bold" },
                    { "messageFontWeight", "Regular" },
                    { "lineHeight", "1.5" }
                },
                ListStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#FFFFFF" },
                    { "borderColor", "#DEE2E6" },
                    { "alternateRowColor", "#F8F9FA" },
                    { "textColor", "#495057" },
                    { "statusColorSuccess", "#28A745" },
                    { "statusColorError", "#DC3545" },
                    { "statusColorWarning", "#FFC107" },
                    { "statusColorInfo", "#17A2B8" },
                    { "cornerRadius", "8" },
                    { "rowPadding", "12" }
                }
            };

            // Enterprise Theme
            themes["enterprise"] = new ThemeConfiguration
            {
                Name = "Enterprise",
                Description = "Professional enterprise theme for corporate environments",
                WindowStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#FFFFFF" },
                    { "borderColor", "#8A8886" },
                    { "borderWidth", "1" },
                    { "cornerRadius", "4" },
                    { "shadow", "true" },
                    { "shadowOpacity", "0.25" }
                },
                ButtonStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#323130" },
                    { "foregroundColor", "#FFFFFF" },
                    { "borderColor", "#605E5C" },
                    { "borderWidth", "1" },
                    { "cornerRadius", "2" },
                    { "fontSize", "14" },
                    { "fontWeight", "Regular" },
                    { "padding", "10,16" }
                },
                ProgressStyle = new Dictionary<string, object>
                {
                    { "foregroundColor", "#323130" },
                    { "backgroundColor", "#F3F2F1" },
                    { "borderColor", "#8A8886" },
                    { "height", "16" },
                    { "cornerRadius", "2" },
                    { "animationSpeed", "200" }
                },
                TextStyle = new Dictionary<string, object>
                {
                    { "titleColor", "#323130" },
                    { "messageColor", "#605E5C" },
                    { "titleFontSize", "16" },
                    { "messageFontSize", "14" },
                    { "titleFontWeight", "SemiBold" },
                    { "messageFontWeight", "Regular" }
                },
                ListStyle = new Dictionary<string, object>
                {
                    { "backgroundColor", "#FFFFFF" },
                    { "borderColor", "#8A8886" },
                    { "alternateRowColor", "#FAF9F8" },
                    { "textColor", "#323130" },
                    { "statusColorSuccess", "#107C10" },
                    { "statusColorError", "#A80000" },
                    { "statusColorWarning", "#FFB900" },
                    { "statusColorInfo", "#0078D4" }
                }
            };

            return themes;
        }
    }
}

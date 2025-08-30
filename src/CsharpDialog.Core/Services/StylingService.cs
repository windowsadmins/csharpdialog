using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpDialog.Core.Models;

namespace CSharpDialog.Core.Services
{
    public interface IStylingService
    {
        Task<bool> ApplyStyleAsync(string element, string property, object value, IDialogManager dialogManager);
        Task<bool> ApplyStyleSheetAsync(StyleSheet styleSheet, IDialogManager dialogManager);
        Task<StyleSheet> CreateStyleSheetAsync(string name, Dictionary<string, object> properties);
        Task<bool> ValidateStylePropertyAsync(string element, string property, object value);
        Task<Dictionary<string, object>> GetSupportedPropertiesAsync(string element);
        Task<bool> ApplyAnimationAsync(string animationType, Dictionary<string, object> parameters, IDialogManager dialogManager);
        Task<bool> ApplyBrandingAsync(BrandConfiguration brandConfig, IDialogManager dialogManager);
    }

    public class StylingService : IStylingService
    {
        private readonly Dictionary<string, HashSet<string>> _supportedProperties;
        private readonly Dictionary<string, Func<object, bool>> _propertyValidators;

        public StylingService()
        {
            _supportedProperties = InitializeSupportedProperties();
            _propertyValidators = InitializePropertyValidators();
        }

        public async Task<bool> ApplyStyleAsync(string element, string property, object value, IDialogManager dialogManager)
        {
            try
            {
                await Task.Delay(0); // Async consistency

                if (!await ValidateStylePropertyAsync(element, property, value))
                {
                    Console.WriteLine($"Invalid style property: {element}.{property} = {value}");
                    return false;
                }

                var command = $"setstyle: {element}, {property}, {value}";
                await dialogManager.SendCommandAsync(command);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying style {element}.{property}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApplyStyleSheetAsync(StyleSheet styleSheet, IDialogManager dialogManager)
        {
            try
            {
                var successCount = 0;
                var totalCount = styleSheet.Properties.Count;

                // Apply individual style properties
                foreach (var property in styleSheet.Properties)
                {
                    var success = await ApplyStyleAsync(property.Element, property.Property, property.Value, dialogManager);
                    if (success) successCount++;
                }

                // Apply CSS rules if supported
                if (styleSheet.CssRules.Any())
                {
                    var cssCommand = $"applycss: {string.Join(";", styleSheet.CssRules)}";
                    await dialogManager.SendCommandAsync(cssCommand);
                }

                Console.WriteLine($"Applied {successCount}/{totalCount} style properties from stylesheet '{styleSheet.Name}'");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying stylesheet '{styleSheet.Name}': {ex.Message}");
                return false;
            }
        }

        public async Task<StyleSheet> CreateStyleSheetAsync(string name, Dictionary<string, object> properties)
        {
            await Task.Delay(0); // Async consistency

            var styleSheet = new StyleSheet
            {
                Name = name,
                Properties = new List<StyleProperty>()
            };

            foreach (var prop in properties)
            {
                var parts = prop.Key.Split('.');
                if (parts.Length >= 2)
                {
                    var element = parts[0];
                    var property = string.Join(".", parts.Skip(1));

                    styleSheet.Properties.Add(new StyleProperty
                    {
                        Element = element,
                        Property = property,
                        Value = prop.Value
                    });
                }
            }

            return styleSheet;
        }

        public async Task<bool> ValidateStylePropertyAsync(string element, string property, object value)
        {
            await Task.Delay(0); // Async consistency

            if (string.IsNullOrEmpty(element) || string.IsNullOrEmpty(property))
            {
                return false;
            }

            var elementKey = element.ToLower();
            if (!_supportedProperties.ContainsKey(elementKey))
            {
                return false;
            }

            if (!_supportedProperties[elementKey].Contains(property.ToLower()))
            {
                return false;
            }

            // Apply specific property validators
            var validatorKey = $"{elementKey}.{property.ToLower()}";
            if (_propertyValidators.ContainsKey(validatorKey))
            {
                return _propertyValidators[validatorKey](value);
            }

            return true;
        }

        public async Task<Dictionary<string, object>> GetSupportedPropertiesAsync(string element)
        {
            await Task.Delay(0); // Async consistency

            var result = new Dictionary<string, object>();
            var elementKey = element.ToLower();

            if (_supportedProperties.ContainsKey(elementKey))
            {
                result["properties"] = _supportedProperties[elementKey].ToList();
                result["element"] = element;
            }

            return result;
        }

        public async Task<bool> ApplyAnimationAsync(string animationType, Dictionary<string, object> parameters, IDialogManager dialogManager)
        {
            try
            {
                await Task.Delay(0); // Async consistency

                var animationConfig = JsonSerializer.Serialize(new
                {
                    type = animationType,
                    parameters = parameters
                });

                var command = $"animate: {animationConfig}";
                await dialogManager.SendCommandAsync(command);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying animation '{animationType}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApplyBrandingAsync(BrandConfiguration brandConfig, IDialogManager dialogManager)
        {
            try
            {
                await Task.Delay(0); // Async consistency

                var brandingCommands = new List<string>();

                if (!string.IsNullOrEmpty(brandConfig.LogoPath))
                {
                    brandingCommands.Add($"setlogo: {brandConfig.LogoPath}");
                }

                if (!string.IsNullOrEmpty(brandConfig.PrimaryColor))
                {
                    brandingCommands.Add($"setstyle: window, primaryColor, {brandConfig.PrimaryColor}");
                }

                if (!string.IsNullOrEmpty(brandConfig.SecondaryColor))
                {
                    brandingCommands.Add($"setstyle: window, secondaryColor, {brandConfig.SecondaryColor}");
                }

                if (!string.IsNullOrEmpty(brandConfig.FontFamily))
                {
                    brandingCommands.Add($"setstyle: text, fontFamily, {brandConfig.FontFamily}");
                }

                if (brandConfig.WatermarkEnabled && !string.IsNullOrEmpty(brandConfig.WatermarkText))
                {
                    brandingCommands.Add($"setwatermark: {brandConfig.WatermarkText}");
                }

                if (!string.IsNullOrEmpty(brandConfig.CustomCss))
                {
                    brandingCommands.Add($"applycss: {brandConfig.CustomCss}");
                }

                foreach (var command in brandingCommands)
                {
                    await dialogManager.SendCommandAsync(command);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying branding: {ex.Message}");
                return false;
            }
        }

        private Dictionary<string, HashSet<string>> InitializeSupportedProperties()
        {
            return new Dictionary<string, HashSet<string>>
            {
                ["window"] = new HashSet<string>
                {
                    "backgroundcolor", "bordercolor", "borderwidth", "cornerradius",
                    "shadow", "shadowopacity", "shadowblur", "width", "height",
                    "minwidth", "minheight", "maxwidth", "maxheight", "opacity",
                    "primarycolor", "secondarycolor", "fontfamily"
                },
                ["button"] = new HashSet<string>
                {
                    "backgroundcolor", "foregroundcolor", "bordercolor", "borderwidth",
                    "cornerradius", "fontsize", "fontweight", "padding", "margin",
                    "transition", "hovercolor", "pressedcolor", "disabledcolor"
                },
                ["progress"] = new HashSet<string>
                {
                    "foregroundcolor", "backgroundcolor", "bordercolor", "height",
                    "cornerradius", "animationspeed", "gradient", "striped",
                    "animated", "thickness"
                },
                ["text"] = new HashSet<string>
                {
                    "titlecolor", "messagecolor", "titlefontsize", "messagefontsize",
                    "titlefontweight", "messagefontweight", "lineheight", "textalign",
                    "fontfamily", "letterspacing", "wordspacing"
                },
                ["list"] = new HashSet<string>
                {
                    "backgroundcolor", "bordercolor", "alternaterowcolor", "textcolor",
                    "statuscolorsuccess", "statuscolorerror", "statuscolorwarning",
                    "statuscolorinfo", "cornerradius", "rowpadding", "headercolor",
                    "headerbackground", "selectedcolor"
                },
                ["icon"] = new HashSet<string>
                {
                    "color", "size", "opacity", "filter", "rotation", "animation"
                }
            };
        }

        private Dictionary<string, Func<object, bool>> InitializePropertyValidators()
        {
            return new Dictionary<string, Func<object, bool>>
            {
                ["window.width"] = value => ValidateNumeric(value, 100, 3000),
                ["window.height"] = value => ValidateNumeric(value, 100, 2000),
                ["window.borderwidth"] = value => ValidateNumeric(value, 0, 20),
                ["window.cornerradius"] = value => ValidateNumeric(value, 0, 50),
                ["window.shadowopacity"] = value => ValidateNumeric(value, 0, 1),
                ["button.fontsize"] = value => ValidateNumeric(value, 8, 72),
                ["button.borderwidth"] = value => ValidateNumeric(value, 0, 10),
                ["button.cornerradius"] = value => ValidateNumeric(value, 0, 25),
                ["progress.height"] = value => ValidateNumeric(value, 4, 100),
                ["progress.animationspeed"] = value => ValidateNumeric(value, 0, 5000),
                ["text.titlefontsize"] = value => ValidateNumeric(value, 8, 72),
                ["text.messagefontsize"] = value => ValidateNumeric(value, 8, 72),
                ["text.lineheight"] = value => ValidateNumeric(value, 0.5, 3.0),
                ["list.rowpadding"] = value => ValidateNumeric(value, 0, 50)
            };
        }

        private bool ValidateNumeric(object value, double min, double max)
        {
            if (double.TryParse(value?.ToString(), out double numValue))
            {
                return numValue >= min && numValue <= max;
            }
            return false;
        }
    }
}

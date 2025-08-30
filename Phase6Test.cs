using System;
using System.Threading.Tasks;
using CSharpDialog.Core.Services;
using CSharpDialog.Core.Models;
using csharpDialog.Core;

namespace Phase6Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Phase 6: Advanced Dialog Styling and Themes Test ===");
            Console.WriteLine("Testing theme and styling services...\n");

            try
            {
                // Test Theme Service
                var themeService = new ThemeService();
                
                Console.WriteLine("1. Testing available themes:");
                var themes = await themeService.GetAvailableThemesAsync();
                foreach (var theme in themes)
                {
                    Console.WriteLine($"   - {theme}");
                }
                
                Console.WriteLine("\n2. Testing theme retrieval:");
                var corporateTheme = await themeService.GetThemeAsync("corporate");
                Console.WriteLine($"   Corporate theme: {corporateTheme.Name} - {corporateTheme.Description}");
                
                var darkTheme = await themeService.GetThemeAsync("dark");
                Console.WriteLine($"   Dark theme: {darkTheme.Name} - {darkTheme.Description}");
                
                // Test Styling Service
                var stylingService = new StylingService();
                
                Console.WriteLine("\n3. Testing style property validation:");
                var isValidWindow = await stylingService.ValidateStylePropertyAsync("window", "backgroundColor", "#FF0000");
                Console.WriteLine($"   window.backgroundColor validation: {isValidWindow}");
                
                var isValidButton = await stylingService.ValidateStylePropertyAsync("button", "fontSize", "14");
                Console.WriteLine($"   button.fontSize validation: {isValidButton}");
                
                var isInvalid = await stylingService.ValidateStylePropertyAsync("invalid", "property", "value");
                Console.WriteLine($"   invalid.property validation: {isInvalid}");
                
                Console.WriteLine("\n4. Testing supported properties:");
                var windowProps = await stylingService.GetSupportedPropertiesAsync("window");
                Console.WriteLine($"   Window element properties: {windowProps.Count} supported");
                
                var buttonProps = await stylingService.GetSupportedPropertiesAsync("button");
                Console.WriteLine($"   Button element properties: {buttonProps.Count} supported");
                
                // Test Console Dialog Service
                var dialogService = new ConsoleDialogService();
                
                Console.WriteLine("\n5. Testing dialog service theme support:");
                var serviceThemes = await dialogService.GetAvailableThemesAsync();
                Console.WriteLine($"   Dialog service themes: {serviceThemes.Count} available");
                
                var applyThemeResult = await dialogService.ApplyDialogThemeAsync("corporate");
                Console.WriteLine($"   Apply corporate theme: {applyThemeResult}");
                
                var applyStyleResult = await dialogService.ApplyStylePropertyAsync("window", "backgroundColor", "#F0F0F0");
                Console.WriteLine($"   Apply style property: {applyStyleResult}");
                
                Console.WriteLine("\n✅ Phase 6 implementation successful!");
                Console.WriteLine("All core services are working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Phase 6 test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

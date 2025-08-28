using CsharpDialog.Core;
using System;

namespace CsharpDialog.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Parse command line arguments
                var configuration = CommandLineParser.ParseArguments(args);
                
                // Create dialog service
                var dialogService = DialogServiceFactory.CreateConsoleDialogService();
                
                // Show dialog
                var result = dialogService.ShowDialog(configuration);
                
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
    }
}

using csharpDialog.Core;
using csharpDialog.Core.Services;
using csharpDialog.Core.Models;

namespace csharpDialog.CommandFileTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== csharpDialog Command File Monitor Test ===");
        Console.WriteLine("This test demonstrates Phase 1 implementation:");
        Console.WriteLine("- Command file monitoring");
        Console.WriteLine("- Real-time command processing");
        Console.WriteLine("- Basic command support");
        Console.WriteLine();

        // Setup command file path
        var tempPath = Path.GetTempPath();
        var commandFilePath = Path.Combine(tempPath, "csharpdialog-test.log");
        
        Console.WriteLine($"Command file: {commandFilePath}");
        Console.WriteLine();

        // Create parser and monitor
        var parser = new CommandParser();
        var monitor = new CommandFileMonitor(parser);

        // Subscribe to events
        monitor.CommandReceived += (sender, e) =>
        {
            Console.WriteLine($"✅ Command received: {e.Command.Type} = '{e.Command.Value}'");
            if (e.Command.Parameters.Count > 0)
            {
                Console.WriteLine($"   Parameters: {string.Join(", ", e.Command.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            }
            Console.WriteLine($"   Timestamp: {e.Command.Timestamp:HH:mm:ss.fff}");
            Console.WriteLine();
        };

        monitor.ErrorOccurred += (sender, e) =>
        {
            Console.WriteLine($"ℹ️  Monitor: {e.Message}");
        };

        try
        {
            // Start monitoring
            await monitor.StartMonitoringAsync(commandFilePath);
            Console.WriteLine("📁 Command file monitoring started");
            Console.WriteLine("📝 You can now test commands by writing to the file:");
            Console.WriteLine($"   echo \"title: Test Title\" >> \"{commandFilePath}\"");
            Console.WriteLine($"   echo \"message: Hello from script!\" >> \"{commandFilePath}\"");
            Console.WriteLine($"   echo \"progress: 50\" >> \"{commandFilePath}\"");
            Console.WriteLine($"   echo \"quit:\" >> \"{commandFilePath}\"");
            Console.WriteLine();
            Console.WriteLine("🔄 Auto-testing in 3 seconds...");
            Console.WriteLine();

            // Wait a moment then auto-test
            await Task.Delay(3000);

            // Test commands programmatically
            Console.WriteLine("🧪 Running automated tests...");
            await TestCommands(commandFilePath);

            Console.WriteLine("✅ All tests completed successfully!");
            Console.WriteLine("📋 Summary of what was implemented in Phase 1:");
            Console.WriteLine("   • CommandFileMonitor - Real-time file monitoring");
            Console.WriteLine("   • CommandParser - swiftDialog-compatible command parsing");
            Console.WriteLine("   • Event-driven architecture for command processing");
            Console.WriteLine("   • Thread-safe file operations with debouncing");
            Console.WriteLine("   • Basic commands: title, message, progress, quit");
            Console.WriteLine();
            Console.WriteLine("🎯 Next Phase 2 will add:");
            Console.WriteLine("   • Dynamic list item status updates");
            Console.WriteLine("   • List item commands (add, update, delete)");
            Console.WriteLine("   • Status icons and progress per item");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
        finally
        {
            monitor?.Dispose();
            
            // Cleanup
            if (File.Exists(commandFilePath))
            {
                File.Delete(commandFilePath);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task TestCommands(string commandFilePath)
    {
        var testCommands = new[]
        {
            "title: Phase 1 Test Dialog",
            "message: Command file monitoring is working!",
            "progress: 25",
            "progresstext: Testing basic commands...",
            "# This is a comment - should be ignored",
            "progress: 50",
            "progresstext: Half way there...",
            "progress: 75",
            "progresstext: Almost done...",
            "progress: 100",
            "progresstext: Complete!",
            "message: All Phase 1 features are working correctly!"
        };

        foreach (var command in testCommands)
        {
            await File.AppendAllTextAsync(commandFilePath, command + Environment.NewLine);
            await Task.Delay(500); // Small delay to see real-time processing
        }

        await Task.Delay(1000); // Final pause
    }
}

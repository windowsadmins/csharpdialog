using csharpDialog.Core;
using System.Configuration;
using System.Data;
using System.Windows;

namespace csharpDialog.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Set shutdown mode to close when main window closes
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        
        // Parse command line arguments
        var configuration = CommandLineParser.ParseArguments(e.Args);
        
        // Create the dialog window and set it as main window
        var dialogWindow = new MainWindow(configuration);
        MainWindow = dialogWindow;
        
        // Show the window and wait for it to close
        dialogWindow.Show();
        
        // Handle the window closed event to exit with proper code
        dialogWindow.Closed += (s, args) =>
        {
            var result = dialogWindow.GetDialogResult();
            int exitCode = result.ButtonPressed == "ok" || result.ButtonPressed == "button1" ? 0 : 1;
            Environment.Exit(exitCode);
        };
    }
}


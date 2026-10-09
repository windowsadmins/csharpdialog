using csharpDialog.Core;
using csharpDialog.Core.Services;
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

        var args = e.Args;
        if (DialogAuthorisation.Enforce(ref args) is int refused)
        {
            FileLog.Info($"Exiting with code {refused}");
            Environment.Exit(refused);
            return;
        }

        FileLog.Info($"csharpdialog {typeof(App).Assembly.GetName().Version} starting (WPF host): {string.Join(' ', args)}");

        // Parse command line arguments
        var configuration = CommandLineParser.ParseArguments(args);
        
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
            FileLog.Info($"Exiting with code {exitCode}");
            Environment.Exit(exitCode);
        };
    }
}


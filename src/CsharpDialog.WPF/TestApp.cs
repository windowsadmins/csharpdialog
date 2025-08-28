using csharpDialog.Core;
using csharpDialog.WPF;
using System;
using System.Windows;

namespace csharpDialog.WPF
{
    /// <summary>
    /// WPF Dialog Service Implementation
    /// </summary>
    public class WpfDialogService : IDialogService
    {
        public async Task<DialogResult> ShowDialogAsync(DialogConfiguration configuration)
        {
            return await Task.Run(() => ShowDialog(configuration));
        }

        public DialogResult ShowDialog(DialogConfiguration configuration)
        {
            DialogResult result = new DialogResult();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new MainWindow(configuration);
                result = dialog.ShowDialogWithResult();
            });
            
            return result;
        }
    }
}

namespace csharpDialog.WPF.Test
{
    /// <summary>
    /// WPF Test Application Entry Point
    /// </summary>
    public class TestApp : Application
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new TestApp();
            app.Run(new TestWindow(args));
        }
    }

    /// <summary>
    /// Test Window for Visual Demos
    /// </summary>
    public partial class TestWindow : Window
    {
        private readonly string[] _args;

        public TestWindow(string[] args)
        {
            _args = args;
            InitializeComponent();
            this.Loaded += TestWindow_Loaded;
        }

        private void InitializeComponent()
        {
            Title = "csharpDialog Visual Test";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            var button = new System.Windows.Controls.Button
            {
                Content = "Test Visual Dialog",
                Width = 200,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            button.Click += Button_Click;
            Content = button;
        }

        private void TestWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-run if arguments provided
            if (_args.Length > 0)
            {
                RunVisualTest();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RunVisualTest();
        }

        private void RunVisualTest()
        {
            try
            {
                // Parse command line arguments or use defaults
                var configuration = _args.Length > 0 
                    ? CommandLineParser.ParseArguments(_args)
                    : new DialogConfiguration
                    {
                        Title = "Visual Test Dialog",
                        Message = "This is a visual WPF dialog test!\n\nFeatures:\n• Custom title and message\n• Multiple buttons\n• Rich UI styling\n• Windows native look and feel",
                        Buttons = new List<DialogButton>
                        {
                            new DialogButton { Text = "Awesome!", Action = "button1", IsDefault = true },
                            new DialogButton { Text = "Cancel", Action = "button2", IsCancel = true }
                        }
                    };

                var dialog = new MainWindow(configuration);
                var result = dialog.ShowDialogWithResult();

                // Show result in a message box
                MessageBox.Show(
                    $"Dialog Result:\nButton: {result.ButtonPressed}\nTimestamp: {result.Timestamp}\nTimed Out: {result.TimedOut}",
                    "Test Result",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

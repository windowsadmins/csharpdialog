using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace csharpDialog.WPF
{
    /// <summary>
    /// Modern progress dialog window inspired by swiftDialog with automatic dark mode
    /// </summary>
    public partial class ProgressDialogWindow : Window
    {
        private bool _isDarkMode;

        // Win32 API for dark mode title bar
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public ProgressDialogWindow()
        {
#pragma warning disable CS0103
            InitializeComponent();
#pragma warning restore CS0103
            
            // Detect and apply system theme
            _isDarkMode = IsSystemDarkMode();
            
            // Apply theme after window is loaded
            this.Loaded += (s, e) =>
            {
                ApplyTheme(_isDarkMode);
                SetDarkModeTitleBar(_isDarkMode);
            };
        }

        private void SetDarkModeTitleBar(bool isDarkMode)
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                int useImmersiveDarkMode = isDarkMode ? 1 : 0;
                
                // Try Windows 10 20H1+ first
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
                
                // Fallback for older Windows 10 versions
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
            }
            catch
            {
                // Ignore errors - title bar will remain light
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private bool IsSystemDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                // If AppsUseLightTheme is 0, dark mode is enabled
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // Default to light mode if we can't read the registry
            }
        }

        private void ApplyTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                // Dark mode colors
                if (FindName("MainGrid") is System.Windows.Controls.Grid mainGrid)
                    mainGrid.Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

                if (FindName("TitleText") is System.Windows.Controls.TextBlock titleText)
                    titleText.Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240));

                if (FindName("MessageText") is System.Windows.Controls.TextBlock messageText)
                    messageText.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                if (FindName("ProgressText") is System.Windows.Controls.TextBlock progressText)
                    progressText.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

                if (FindName("ProgressBarControl") is System.Windows.Controls.ProgressBar progressBar)
                {
                    progressBar.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                    progressBar.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                }
            }
            else
            {
                // Light mode colors (already set in XAML as defaults)
                if (FindName("MainGrid") is System.Windows.Controls.Grid mainGrid)
                    mainGrid.Background = new SolidColorBrush(Colors.White);

                if (FindName("TitleText") is System.Windows.Controls.TextBlock titleText)
                    titleText.Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51));

                if (FindName("MessageText") is System.Windows.Controls.TextBlock messageText)
                    messageText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));

                if (FindName("ProgressText") is System.Windows.Controls.TextBlock progressText)
                    progressText.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));

                if (FindName("ProgressBarControl") is System.Windows.Controls.ProgressBar progressBar)
                {
                    progressBar.Background = new SolidColorBrush(Color.FromRgb(233, 236, 239));
                    progressBar.Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                }
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
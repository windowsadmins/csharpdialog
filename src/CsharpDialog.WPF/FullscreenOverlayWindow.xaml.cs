using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace csharpDialog.WPF
{
    /// <summary>
    /// Fullscreen overlay window that displays a blurred/dimmed background
    /// with the dialog centered on top, similar to swiftDialog on macOS
    /// </summary>
    public partial class FullscreenOverlayWindow : Window
    {
        private Window? _hostedDialog;

        // Windows 10/11 Acrylic/Backdrop blur support
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMSBT_MAINWINDOW = 2; // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4; // Tabbed

        public FullscreenOverlayWindow()
        {
#pragma warning disable CS0103
            InitializeComponent();
#pragma warning restore CS0103

            // Set to cover all screens
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
            
            Console.WriteLine($"[DEBUG] Fullscreen overlay created: {this.Width}x{this.Height}");

            // Apply backdrop blur when window loads
            this.Loaded += (s, e) =>
            {
                ApplyBackdropBlur();
            };
        }

        private void ApplyBackdropBlur()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                // Try to enable acrylic backdrop (Windows 11 22H2+)
                int backdropType = DWMSBT_TRANSIENTWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                
                Console.WriteLine($"[DEBUG] Applied Windows backdrop blur effect");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Could not apply backdrop blur: {ex.Message}");
            }
        }

        /// <summary>
        /// Host a dialog window in the center of the fullscreen overlay
        /// </summary>
        public void HostDialog(Window dialog)
        {
            _hostedDialog = dialog;

            // Wait for both windows to load before transferring content
            this.Loaded += (s, e) =>
            {
                // Make sure dialog loads first (but don't show it)
                dialog.Visibility = Visibility.Hidden;
                dialog.WindowStyle = WindowStyle.None;
                dialog.AllowsTransparency = true;
                dialog.Background = Brushes.Transparent;
                
                // Force the dialog to initialize but not show
                dialog.Show();
                dialog.Hide();
                
                // Get the DialogHost border
                var dialogHost = this.FindName("DialogHost") as Border;
                if (dialogHost != null)
                {
                    // Extract the dialog's content
                    var dialogContent = dialog.Content as UIElement;
                    if (dialogContent != null)
                    {
                        // Remove content from original dialog
                        dialog.Content = null;

                        // Add to our host
                        dialogHost.Child = dialogContent;
                        
                        Console.WriteLine($"[DEBUG] Dialog content successfully hosted in fullscreen overlay");
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] WARNING: Dialog has no content to host");
                    }
                }
                else
                {
                    Console.WriteLine($"[DEBUG] WARNING: Could not find DialogHost in overlay");
                }
            };

            // Handle dialog closing
            dialog.Closed += (s, e) =>
            {
                this.Close();
            };
            
            // When overlay closes, close the hidden dialog too
            this.Closed += (s, e) =>
            {
                if (_hostedDialog != null && _hostedDialog.IsVisible)
                {
                    _hostedDialog.Close();
                }
            };
        }

        /// <summary>
        /// Show the overlay with a hosted dialog
        /// </summary>
        public static void ShowWithDialog(Window dialog)
        {
            var overlay = new FullscreenOverlayWindow();
            overlay.HostDialog(dialog);
            overlay.Show();
            
            // Ensure overlay stays behind the dialog content
            overlay.Topmost = true;
        }
    }
}

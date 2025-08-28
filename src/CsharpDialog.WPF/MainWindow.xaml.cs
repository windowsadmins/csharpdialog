using csharpDialog.Core;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace csharpDialog.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DialogConfiguration? _configuration;
    private DialogResult _result;
    private DispatcherTimer? _timeoutTimer;

    public MainWindow()
    {
        InitializeComponent();
        _result = new DialogResult();
    }

    public MainWindow(DialogConfiguration configuration) : this()
    {
        _configuration = configuration;
        ApplyConfiguration();
    }

    public DialogResult ShowDialogWithResult()
    {
        ShowDialog();
        return _result;
    }

    public DialogResult GetDialogResult()
    {
        return _result;
    }

    private void ApplyConfiguration()
    {
        if (_configuration == null) return;

        // Set title
        Title = _configuration.Title;
        DialogTitle.Text = _configuration.Title;

        // Set message
        MessageText.Text = _configuration.Message;

        // Set window size
        if (!_configuration.Size.AutoSize)
        {
            Width = _configuration.Size.Width;
            Height = _configuration.Size.Height;
            SizeToContent = SizeToContent.Manual;
        }
        else
        {
            SizeToContent = SizeToContent.WidthAndHeight;
        }

        // Set colors
        if (!string.IsNullOrEmpty(_configuration.BackgroundColor))
        {
            try
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_configuration.BackgroundColor));
            }
            catch { /* Invalid color format */ }
        }

        if (!string.IsNullOrEmpty(_configuration.TextColor))
        {
            try
            {
                MessageText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_configuration.TextColor));
                DialogTitle.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_configuration.TextColor));
            }
            catch { /* Invalid color format */ }
        }

        // Set font
        MessageText.FontFamily = new FontFamily(_configuration.FontFamily);
        MessageText.FontSize = _configuration.FontSize;
        DialogTitle.FontFamily = new FontFamily(_configuration.FontFamily);

        // Set window properties
        if (_configuration.CenterOnScreen)
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Topmost = _configuration.Topmost;

        // Load icon
        LoadIcon();

        // Load image
        LoadImage();

        // Load video (placeholder)
        LoadVideo();

        // Create buttons
        CreateButtons();

        // Set timeout
        SetTimeout();
    }

    private void LoadIcon()
    {
        if (_configuration == null || string.IsNullOrEmpty(_configuration.Icon)) return;

        try
        {
            if (File.Exists(_configuration.Icon))
            {
                DialogIcon.Source = new BitmapImage(new Uri(_configuration.Icon, UriKind.Absolute));
                DialogIcon.Visibility = Visibility.Visible;
            }
        }
        catch { /* Invalid icon path */ }
    }

    private void LoadImage()
    {
        if (_configuration == null || string.IsNullOrEmpty(_configuration.ImagePath)) return;

        try
        {
            if (File.Exists(_configuration.ImagePath))
            {
                ContentImage.Source = new BitmapImage(new Uri(_configuration.ImagePath, UriKind.Absolute));
                ContentImage.Visibility = Visibility.Visible;
            }
        }
        catch { /* Invalid image path */ }
        }

    private void LoadVideo()
    {
        if (_configuration == null || string.IsNullOrEmpty(_configuration.VideoPath)) return;

        // Video playback would require MediaElement or similar
        // For now, just show placeholder
        VideoContainer.Visibility = Visibility.Visible;
    }

        private void CreateButtons()
        {
            if (_configuration == null) return;
            
            ButtonPanel.Children.Clear();

            foreach (var buttonConfig in _configuration.Buttons)
            {
                var button = new Button
                {
                    Content = buttonConfig.Text,
                    Margin = new Thickness(8, 0, 0, 0),
                    Padding = new Thickness(20, 8, 20, 8),
                    MinWidth = 90,
                    Height = 32,
                    FontSize = 13,
                    IsDefault = buttonConfig.IsDefault,
                    IsCancel = buttonConfig.IsCancel,
                    Background = buttonConfig.IsDefault ? 
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078d4")) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f8f9fa")),
                    Foreground = buttonConfig.IsDefault ?
                        new SolidColorBrush(Colors.White) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2c3e50")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dee2e6")),
                    BorderThickness = new Thickness(1)
                };

                // Add hover effects
                button.Style = CreateButtonStyle(buttonConfig.IsDefault);

                string action = buttonConfig.Action;
                button.Click += (s, e) => ButtonClicked(action);

                ButtonPanel.Children.Add(button);
            }
        }
        
        private Style CreateButtonStyle(bool isPrimary)
        {
            var style = new Style(typeof(Button));
            
            // Set base properties
            style.Setters.Add(new Setter(Button.FontFamilyProperty, new FontFamily("Segoe UI")));
            style.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            
            return style;
        }    private void SetTimeout()
    {
        if (_configuration == null || !_configuration.Timeout.HasValue || _configuration.Timeout <= 0) return;
        
        _timeoutTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_configuration.Timeout.Value)
        };
        _timeoutTimer.Tick += (s, e) =>
        {
            _timeoutTimer.Stop();
            _result.TimedOut = true;
            _result.ButtonPressed = "timeout";
            Close();
        };
        _timeoutTimer.Start();
    }

    private void ButtonClicked(string action)
    {
        _timeoutTimer?.Stop();
        _result.ButtonPressed = action;
        Close();
    }
}
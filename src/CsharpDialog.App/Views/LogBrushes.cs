using Microsoft.UI.Xaml.Media;
using csharpDialog.Core.Notifications;

namespace csharpDialog.App.Views;

/// <summary>One colour per log level, shared by the Run console and the Logs viewer.</summary>
internal static class LogBrushes
{
    public static SolidColorBrush ForLevel(LogLineLevel level) => level switch
    {
        LogLineLevel.Error   => new SolidColorBrush(Microsoft.UI.Colors.IndianRed),
        LogLineLevel.Warning => new SolidColorBrush(Microsoft.UI.Colors.Goldenrod),
        LogLineLevel.Success => new SolidColorBrush(Microsoft.UI.Colors.MediumSeaGreen),
        LogLineLevel.Debug   => new SolidColorBrush(Windows.UI.Color.FromArgb(204, 128, 128, 128)),
        LogLineLevel.Header  => new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue),
        _ => (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimaryBrush"],
    };
}

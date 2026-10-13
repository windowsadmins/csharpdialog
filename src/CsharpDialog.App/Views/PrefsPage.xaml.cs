using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using csharpDialog.App.ViewModels;

namespace csharpDialog.App.Views;

public sealed partial class PrefsPage : Page
{
    public PrefsViewModel ViewModel { get; } = new();

    public PrefsPage()
    {
        InitializeComponent();

        var iconPath = System.IO.Path.Combine(
            AppContext.BaseDirectory, "Assets", "ManagedNotificationsDialog.png");
        if (System.IO.File.Exists(iconPath))
        {
            AppIcon.Source = new BitmapImage(new Uri(iconPath));
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.Load();
    }

    private void OpenInstallFolder_Click(object sender, RoutedEventArgs e)
        => ViewModel.OpenFolder(ViewModel.InstallDirectory);

    private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        => ViewModel.OpenFolder(ViewModel.LogDirectory);
}

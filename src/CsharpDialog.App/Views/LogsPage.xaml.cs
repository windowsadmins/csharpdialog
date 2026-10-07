using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using csharpDialog.App.ViewModels;

namespace csharpDialog.App.Views;

public sealed partial class LogsPage : Page
{
    private readonly LogsViewModel _vm = new();

    public LogsPage()
    {
        InitializeComponent();
        _vm.LogFiles.CollectionChanged += (_, _) => UpdateEmptyState();

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        _vm.Refresh();

        LogFileList.ItemsSource = _vm.LogFiles;
        LogFileList.SelectedItem = _vm.SelectedLog;
        UpdateLogContent();
        UpdateEmptyState();
    }

    // ── Event Handlers ───────────────────────────────────────────

    private void LogFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LogFileList.SelectedItem is LogsViewModel.LogFile file)
            _vm.SelectedLog = file;
    }

    private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => _vm.FilterText = FilterBox.Text;

    private void OpenEditor_Click(object sender, RoutedEventArgs e)
        => _vm.OpenInEditorCommand.Execute(null);

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
        => _vm.OpenFolderCommand.Execute(null);

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _vm.Refresh();
        LogFileList.ItemsSource = null;
        LogFileList.ItemsSource = _vm.LogFiles;
        LogFileList.SelectedItem = _vm.SelectedLog;
    }

    // ── UI State ─────────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogsViewModel.FilteredLines))
            UpdateLogContent();
    }

    private void UpdateEmptyState()
    {
        var noLogs = _vm.LogFiles.Count == 0;
        NoLogsState.Visibility = noLogs ? Visibility.Visible : Visibility.Collapsed;
        NoLogsPath.Text = LogsViewModel.LogDirectory;
        EmptyTitle.Text = noLogs ? "No logs" : "No Log Selected";
        EmptySubtitle.Text = noLogs
            ? "No csharpDialog log yet. dialog.exe writes one on its first run."
            : "Select a log from the sidebar to view its contents.";
    }

    private void UpdateLogContent()
    {
        EmptyState.Visibility = _vm.SelectedLog is null ? Visibility.Visible : Visibility.Collapsed;
        OpenEditorBtn.IsEnabled = _vm.SelectedLog is not null;

        LogOutput.Blocks.Clear();
        foreach (var line in _vm.FilteredLines)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run { Text = line.Text });
            paragraph.Foreground = LogBrushes.ForLevel(line.Level);
            paragraph.Margin = new Thickness(0, 1, 0, 1);
            LogOutput.Blocks.Add(paragraph);
        }
    }
}

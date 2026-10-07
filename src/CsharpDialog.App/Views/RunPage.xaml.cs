using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using csharpDialog.App.ViewModels;
using csharpDialog.Core.Notifications;

namespace csharpDialog.App.Views;

public sealed partial class RunPage : Page
{
    private readonly RunViewModel _vm;

    public RunPage()
    {
        InitializeComponent();

        _vm = new RunViewModel(DispatcherQueue.GetForCurrentThread());
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        _vm.OutputLines.CollectionChanged += (_, _) => ScrollToBottom();

        PresetPicker.ItemsSource = _vm.Presets;
        PresetPicker.SelectedItem = _vm.SelectedPreset;
        UpdatePresetInfo();
    }

    // ── Button Handlers ─────────────────────────────────────────

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.IsRunning)
            _vm.StopCommand.Execute(null);
        else
            await _vm.RunCommand.ExecuteAsync(null);
    }

    private void PresetPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetPicker.SelectedItem is TestDialogPresets.Preset preset)
            _vm.SelectedPreset = preset;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
        => _vm.ClearCommand.Execute(null);

    private void DebugToggle_Changed(object sender, RoutedEventArgs e)
        => _vm.ShowDebug = DebugToggle.IsChecked ?? false;

    // ── UI State Sync ────────────────────────────────────────────

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RunViewModel.IsRunning):
                UpdateRunningState();
                break;
            case nameof(RunViewModel.LastExitCode):
                UpdateStatusIndicator();
                UpdateResultBanner();
                break;
            case nameof(RunViewModel.FilteredLines):
                UpdateConsoleItems();
                break;
            case nameof(RunViewModel.SelectedPreset):
                UpdatePresetInfo();
                break;
        }
    }

    private void UpdatePresetInfo()
    {
        PresetDescription.Text = _vm.SelectedPreset.Description;
        PresetCommand.Text = _vm.SelectedCommandLine;
    }

    private void UpdateRunningState()
    {
        if (_vm.IsRunning)
        {
            RunIcon.Glyph = "\uE71A"; // Stop icon
            RunText.Text = "Close Dialog";
            RunButton.Background = new SolidColorBrush(Microsoft.UI.Colors.IndianRed);
            RunningProgress.IsActive = true;
            RunningLabel.Visibility = Visibility.Visible;
            ClearButton.Visibility = Visibility.Collapsed;
            PresetPicker.IsEnabled = false;
            ResultBanner.IsOpen = false;
        }
        else
        {
            RunIcon.Glyph = "\uE768"; // Play icon
            RunText.Text = "Show Test Dialog";
            RunButton.Background = null; // Reset to default
            RunningProgress.IsActive = false;
            RunningLabel.Visibility = Visibility.Collapsed;
            ClearButton.Visibility = _vm.OutputLines.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            PresetPicker.IsEnabled = true;
        }
    }

    private void UpdateStatusIndicator()
    {
        if (_vm.LastExitCode is null)
        {
            StatusPanel.Visibility = Visibility.Collapsed;
            return;
        }

        StatusPanel.Visibility = Visibility.Visible;

        if (_vm.LastExitCode == 0)
        {
            StatusIcon.Glyph = "\uE73E"; // Checkmark
            StatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
            StatusText.Text = "Completed successfully";
            StatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.ForestGreen);
        }
        else
        {
            StatusIcon.Glyph = "\uE7BA"; // Warning
            StatusIcon.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Goldenrod);
            StatusText.Text = $"Exit code {_vm.LastExitCode}";
            StatusText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Goldenrod);
        }
    }

    private void UpdateConsoleItems()
    {
        ConsoleOutput.Blocks.Clear();
        foreach (var line in _vm.FilteredLines)
        {
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(new Run { Text = line.Text });
            paragraph.Foreground = LogBrushes.ForLevel(line.Level);
            paragraph.Margin = new Thickness(0, 1, 0, 1);
            ConsoleOutput.Blocks.Add(paragraph);
        }
    }

    private void ScrollToBottom()
    {
        DispatcherQueue.TryEnqueue(() =>
            ConsoleScroller.ChangeView(null, ConsoleScroller.ScrollableHeight, null));
    }

    // ── Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// dialog.exe exits 0 for OK or button 1 and 1 for anything else, including button 2 and
    /// the timeout, so a non-zero exit from a test dialog is a choice, not a failure.
    /// </summary>
    private void UpdateResultBanner()
    {
        if (_vm.LastExitCode is null)
        {
            ResultBanner.IsOpen = false;
            return;
        }

        var result = string.IsNullOrEmpty(_vm.LastResult) ? "none reported" : _vm.LastResult;
        ResultBanner.IsOpen = true;
        if (_vm.ErrorCount > 0)
        {
            ResultBanner.Severity = InfoBarSeverity.Error;
            ResultBanner.Title = $"dialog.exe reported {_vm.ErrorCount} error{(_vm.ErrorCount == 1 ? "" : "s")}";
            ResultBanner.Message = "Check the console output for details";
        }
        else if (_vm.LastExitCode == 0)
        {
            ResultBanner.Severity = InfoBarSeverity.Success;
            ResultBanner.Title = "Dialog closed";
            ResultBanner.Message = $"Result: {result}";
        }
        else
        {
            ResultBanner.Severity = InfoBarSeverity.Informational;
            ResultBanner.Title = $"Dialog closed with exit code {_vm.LastExitCode}";
            ResultBanner.Message = $"Result: {result}";
        }
    }
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using csharpDialog.Core.Notifications;

namespace csharpDialog.App.ViewModels;

/// <summary>
/// ViewModel for the Logs tab: the CLI's rolling diagnostic log, csharpdialog.log and its
/// rotated generations, each line coloured by level.
/// </summary>
public partial class LogsViewModel : ObservableObject
{
    private static string LogDirectory => InstallPaths.LogDirectory;

    public ObservableCollection<LogFile> LogFiles { get; } = [];

    [ObservableProperty] private LogFile? _selectedLog;
    [ObservableProperty] private string _logContent = string.Empty;
    [ObservableProperty] private string _filterText = string.Empty;

    public IEnumerable<LogLine> FilteredLines
    {
        get
        {
            var lines = LogContent.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => new LogLine(l, LogLineClassifier.Classify(l)));
            if (string.IsNullOrWhiteSpace(FilterText))
                return lines;
            return lines.Where(l => l.Text.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
        }
    }

    // ── Models ───────────────────────────────────────────────────

    public record LogFile(string Name, string Path, DateTime Date, long SizeBytes)
    {
        public string DisplayDate => Name;

        public string DisplayTime => $"Last written {Date:yyyy-MM-dd HH:mm:ss}";

        public string DisplaySize => SizeBytes switch
        {
            < 1024        => $"{SizeBytes} B",
            < 1024 * 1024 => $"{SizeBytes / 1024.0:0.#} KB",
            _             => $"{SizeBytes / (1024.0 * 1024):0.#} MB",
        };
    }

    public record LogLine(string Text, LogLineLevel Level);

    // ── Refresh ──────────────────────────────────────────────────

    [RelayCommand]
    public void Refresh()
    {
        var selectedPath = SelectedLog?.Path;
        LogFiles.Clear();

        foreach (var f in LogFileLocator.List(InstallPaths.LogFile))
            LogFiles.Add(new LogFile(f.Name, f.Path, f.Modified, f.SizeBytes));

        // Keep the selection across a refresh, else select the active log, and reload it.
        var selected = LogFiles.FirstOrDefault(f => f.Path == selectedPath) ?? LogFiles.FirstOrDefault();
        SelectedLog = null;
        SelectedLog = selected;
    }

    // ── Load Content ─────────────────────────────────────────────

    partial void OnSelectedLogChanged(LogFile? value)
    {
        if (value is null)
        {
            LogContent = string.Empty;
            return;
        }

        try
        {
            // FileShare.ReadWrite so a log dialog.exe is writing right now can still be read.
            using var fs = new FileStream(value.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            LogContent = reader.ReadToEnd();
        }
        catch
        {
            LogContent = "Unable to read log file.";
        }
    }

    partial void OnLogContentChanged(string value) => OnPropertyChanged(nameof(FilteredLines));
    partial void OnFilterTextChanged(string value) => OnPropertyChanged(nameof(FilteredLines));

    // ── Actions ──────────────────────────────────────────────────

    [RelayCommand]
    private void OpenInEditor()
    {
        if (SelectedLog is null) return;
        Process.Start(new ProcessStartInfo("notepad.exe", $"\"{SelectedLog.Path}\"") { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (!Directory.Exists(LogDirectory)) return;
        Process.Start(new ProcessStartInfo(LogDirectory) { UseShellExecute = true });
    }
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using csharpDialog.Core.Notifications;
using Microsoft.UI.Dispatching;

namespace csharpDialog.App.ViewModels;

/// <summary>
/// ViewModel for the Run tab. Shows one of the preset test dialogs by running dialog.exe with
/// that preset's arguments, unelevated, so the dialog appears on this user's desktop the way a
/// script's would. The CLI's stdout and stderr stream into the console as it runs.
/// </summary>
public partial class RunViewModel : ObservableObject
{
    private Process? _cliProcess;
    private CancellationTokenSource? _cts;
    private readonly DispatcherQueue _dispatcher;

    public RunViewModel(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher;
    }

    // ── Observable State ─────────────────────────────────────────

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private int? _lastExitCode;
    [ObservableProperty] private bool _showDebug;
    [ObservableProperty] private int _errorCount;
    [ObservableProperty] private string _lastResult = string.Empty;
    [ObservableProperty] private TestDialogPresets.Preset _selectedPreset = TestDialogPresets.Info;

    public IReadOnlyList<TestDialogPresets.Preset> Presets => TestDialogPresets.All;

    public ObservableCollection<OutputLine> OutputLines { get; } = [];

    public IEnumerable<OutputLine> FilteredLines =>
        ShowDebug ? OutputLines : OutputLines.Where(l => l.Level != LogLineLevel.Debug);

    public string SelectedCommandLine => $"dialog.exe {TestDialogPresets.ToCommandLine(SelectedPreset.Arguments)}";

    partial void OnSelectedPresetChanged(TestDialogPresets.Preset value) => OnPropertyChanged(nameof(SelectedCommandLine));

    // ── Output Line Model ────────────────────────────────────────

    public record OutputLine(string Text, LogLineLevel Level);

    // ── Run ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RunAsync()
    {
        if (IsRunning) return;

        IsRunning = true;
        LastExitCode = null;
        ErrorCount = 0;
        LastResult = string.Empty;
        OutputLines.Clear();
        OnPropertyChanged(nameof(FilteredLines));

        _cts = new CancellationTokenSource();
        var preset = SelectedPreset;

        var cliPath = InstallPaths.FindCli(AppContext.BaseDirectory);
        if (cliPath is null)
        {
            AppendLine($"Error: {InstallPaths.CliExecutableName} not found. Looked in: {string.Join("; ", InstallPaths.CliCandidates(AppContext.BaseDirectory))}", LogLineLevel.Error);
            IsRunning = false;
            return;
        }

        AppendLine($"=== {preset.Name} test dialog ===", LogLineLevel.Header);
        AppendLine($"[DEBUG] {cliPath} {TestDialogPresets.ToCommandLine(preset.Arguments)}", LogLineLevel.Debug);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cliPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(cliPath) ?? string.Empty,
            };
            foreach (var arg in preset.Arguments)
                startInfo.ArgumentList.Add(arg);

            _cliProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _cliProcess.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) AppendLine(e.Data, LogLineClassifier.Classify(e.Data)); };
            _cliProcess.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) AppendLine(e.Data, LogLineLevel.Error); };

            if (!_cliProcess.Start())
            {
                AppendLine("Error: Failed to start dialog.exe.", LogLineLevel.Error);
                return;
            }
            _cliProcess.BeginOutputReadLine();
            _cliProcess.BeginErrorReadLine();

            AppendLine($"Started dialog.exe (PID: {_cliProcess.Id})", LogLineLevel.Default);

            await _cliProcess.WaitForExitAsync(_cts.Token);
            // WaitForExit without a timeout drains the redirected streams.
            _cliProcess.WaitForExit();
            var exitCode = _cliProcess.ExitCode;
            _dispatcher.TryEnqueue(() => LastExitCode = exitCode);
        }
        catch (OperationCanceledException)
        {
            AppendLine("Warning: Test dialog closed from the Run tab.", LogLineLevel.Warning);
        }
        catch (Exception ex)
        {
            AppendLine($"Error: {ex.Message}", LogLineLevel.Error);
        }
        finally
        {
            _cliProcess?.Dispose();
            _cliProcess = null;
            _dispatcher.TryEnqueue(() => IsRunning = false);
        }
    }

    // ── Stop ─────────────────────────────────────────────────────

    [RelayCommand]
    private void Stop()
    {
        if (!IsRunning) return;

        try
        {
            _cts?.Cancel();
            if (_cliProcess is { HasExited: false })
                _cliProcess.Kill(entireProcessTree: true);
        }
        catch { }

        LastExitCode = null;
    }

    // ── Clear ────────────────────────────────────────────────────

    [RelayCommand]
    private void Clear()
    {
        OutputLines.Clear();
        LastExitCode = null;
        LastResult = string.Empty;
        OnPropertyChanged(nameof(FilteredLines));
    }

    // ── Helpers ──────────────────────────────────────────────────

    private void AppendLine(string text, LogLineLevel level)
    {
        _dispatcher.TryEnqueue(() =>
        {
            OutputLines.Add(new OutputLine(text, level));
            OnPropertyChanged(nameof(FilteredLines));

            if (text.StartsWith("Result:", StringComparison.Ordinal))
                LastResult = text["Result:".Length..].Trim();

            if (level == LogLineLevel.Error)
                ErrorCount++;
        });
    }

    partial void OnShowDebugChanged(bool value) => OnPropertyChanged(nameof(FilteredLines));
}

using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using csharpDialog.Core.Notifications;
using csharpDialog.Core.Services;

namespace csharpDialog.App.ViewModels;

/// <summary>
/// ViewModel for the Prefs tab. Every dialog option is a command-line flag on dialog.exe; the one
/// setting csharpDialog reads is AuthorisationKey, and only from policy
/// (HKLM\SOFTWARE\Policies\csharpDialog). So the tab has nothing to edit and no Unlock; it shows
/// whether policy sets the key, the version and the paths the tool uses, read-only.
/// </summary>
public partial class PrefsViewModel : ObservableObject
{
    [ObservableProperty] private string _cliPath = "";
    [ObservableProperty] private string _cliVersion = "";

    public string AppVersion { get; } = ReadAppVersion();

    public string VersionDisplay => $"Version {AppVersion}";

    public string GuiPath { get; } = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, InstallPaths.GuiExecutableName);

    public string InstallDirectory { get; } = InstallPaths.InstallDirectory;

    public string LogDirectory { get; } = InstallPaths.LogDirectory;

    public string LogFile { get; } = InstallPaths.LogFile;

    /// <summary>True when policy sets AuthorisationKey, so callers must supply the key.</summary>
    public bool AuthorisationKeyManaged { get; } = DialogAuthorisation.IsManaged;

    public string AuthorisationKeyStatus => AuthorisationKeyManaged
        ? "Managed: set by policy. Callers must pass the key in DIALOG_AUTH_KEY or with --key, or dialog.exe exits 30."
        : "Not set. Any caller can show a dialog.";

    public void Load()
    {
        var cli = InstallPaths.FindCli(AppContext.BaseDirectory);
        CliPath = cli ?? $"Not found ({string.Join("; ", InstallPaths.CliCandidates(AppContext.BaseDirectory))})";
        CliVersion = cli is null ? "Not installed" : ReadFileVersion(cli);
    }

    public void OpenFolder(string path)
    {
        var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

    // The version the release stamps (build.ps1 passes /p:Version), zero-padded, never the
    // time this copy happened to be built.
    private static string ReadAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return ReleaseVersion.Display(
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString());
    }

    private static string ReadFileVersion(string path)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            return info.ProductVersion?.Split('+')[0] ?? info.FileVersion ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}

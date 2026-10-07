using csharpDialog.Core.Services;

namespace csharpDialog.Core.Notifications;

/// <summary>
/// Where csharpDialog lives and writes on a machine. The Managed Notifications Dialog GUI
/// shows these read-only on its Prefs tab, finds the CLI through them and lists logs from them.
/// </summary>
public static class InstallPaths
{
    /// <summary>File name of the command-line tool the GUI drives.</summary>
    public const string CliExecutableName = "dialog.exe";

    /// <summary>File name of the GUI itself, installed beside the CLI.</summary>
    public const string GuiExecutableName = "Managed Notifications Dialog.exe";

    /// <summary>%ProgramFiles%\csharpDialog, the folder the MSI and .pkg install into.</summary>
    public static string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "csharpDialog");

    /// <summary>The folder holding the CLI's rolling diagnostic log.</summary>
    public static string LogDirectory => Path.GetDirectoryName(FileLog.LogPath) ?? string.Empty;

    /// <summary>The CLI's active diagnostic log.</summary>
    public static string LogFile => FileLog.LogPath;

    /// <summary>
    /// The CLI to run: the copy beside this GUI first, so a GUI run from a build folder
    /// drives the CLI it was built with, then the installed copy. Null when neither exists.
    /// </summary>
    public static string? FindCli(string baseDirectory)
        => CliCandidates(baseDirectory).FirstOrDefault(File.Exists);

    /// <summary>The places <see cref="FindCli"/> looks, in order.</summary>
    public static IReadOnlyList<string> CliCandidates(string baseDirectory)
        => new[]
        {
            Path.Combine(baseDirectory, CliExecutableName),
            Path.Combine(InstallDirectory, CliExecutableName),
        }
        .Select(Path.GetFullPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

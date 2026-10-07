namespace csharpDialog.Core.Notifications;

/// <summary>
/// The test dialogs the GUI's Run tab can show. Each is an ordinary set of CLI arguments, so a
/// preset exercises the same path a script calling dialog.exe would. Every preset opens a
/// window (--window) and closes itself after a minute so a forgotten test never lingers.
/// </summary>
public static class TestDialogPresets
{
    public const int TimeoutSeconds = 60;

    public record Preset(string Id, string Name, string Description, IReadOnlyList<string> Arguments)
    {
        /// <summary>The display name, which is also what screen readers announce for a list item.</summary>
        public override string ToString() => Name;
    }

    public static readonly Preset Info = new(
        "info",
        "Information",
        "A plain message with one button.",
        [
            "--window",
            "--title", "Information",
            "--message", "This is a test information dialog from Managed Notifications Dialog.",
            "--button1", "OK",
            "--timeout", TimeoutSeconds.ToString(),
        ]);

    public static readonly Preset Progress = new(
        "progress",
        "Progress",
        "A progress bar with status text and list items.",
        [
            "--window",
            "--title", "Installing software",
            "--message", "This is a test progress dialog. Nothing is being installed.",
            "--progress", "40",
            "--progresstext", "Installing Example App 2 of 3",
            "--listitem", "Example App 1",
            "--listitem", "Example App 2",
            "--listitem", "Example App 3",
            "--button1", "Close",
            "--timeout", TimeoutSeconds.ToString(),
        ]);

    public static readonly Preset Alert = new(
        "alert",
        "Alert",
        "A topmost alert with two choices.",
        [
            "--window",
            "--topmost",
            "--title", "Action required",
            "--message", "This is a test alert. A real one would ask you to restart or save your work.",
            "--button1", "Restart later",
            "--button2", "Dismiss",
            "--timeout", TimeoutSeconds.ToString(),
        ]);

    public static IReadOnlyList<Preset> All { get; } = [Info, Progress, Alert];

    public static Preset? Find(string id)
        => All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>The arguments as one command line, quoted where needed, for display.</summary>
    public static string ToCommandLine(IEnumerable<string> arguments)
        => string.Join(' ', arguments.Select(Quote));

    private static string Quote(string arg)
        => arg.Length > 0 && !arg.Any(c => char.IsWhiteSpace(c) || c == '"')
            ? arg
            : $"\"{arg.Replace("\"", "\\\"")}\"";
}

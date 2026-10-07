using System.Text;

namespace csharpDialog.Core.Services;

/// <summary>
/// Minimal diagnostic file log with no dependencies.
///
/// Writes one line per entry, "[yyyy-MM-dd HH:mm:ss] LEVEL message" with the level padded
/// to five characters, to %ProgramData%\ManagedNotifications\logs\csharpdialog.log. The file
/// rolls at 5 MB and five generations are kept (csharpdialog.log.1 is the newest).
///
/// The dialog tools own their own root. This file is otherwise identical to the copies in
/// sbin-installer, taskbarutil and airname, which log under ManagedUtilities; the root here
/// differs deliberately, so do not "fix" the inconsistency by aligning them.
///
/// This is for errors, warnings and process lifecycle only. The dialog's own output stays on
/// stdout because callers parse it. Logging never throws: a failure to write the log must not
/// take the dialog down.
/// </summary>
public static class FileLog
{
    public const long MaxBytes = 5L * 1024 * 1024;
    public const int Generations = 5;

    private static readonly object Gate = new();
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static string? _logPath;
    private static bool _migrated;

    /// <summary>
    /// Full path of the active log file. Defaults to
    /// %ProgramData%\ManagedNotifications\logs\csharpdialog.log; settable so a harness can
    /// redirect it.
    /// </summary>
    public static string LogPath
    {
        get => _logPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ManagedNotifications", "logs", "csharpdialog.log");
        set { _logPath = value; _migrated = false; }
    }

    /// <summary>
    /// Where earlier builds wrote, before the dialog tools were given their own root.
    /// </summary>
    public static string LegacyLogPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ManagedUtilities", "logs", "csharpdialog.log");

    /// <summary>
    /// One-time move of the previous log and every rotated generation to the new root, so a
    /// machine that upgrades keeps its history instead of starting blank. Runs once per
    /// process, before the first write, and never throws: a diagnostic log is not worth
    /// failing a dialog over. A generation already present at the destination is left alone,
    /// which makes the move safe to attempt repeatedly.
    /// </summary>
    internal static void MigrateLegacyLog()
    {
        try
        {
            if (File.Exists(LogPath) || !File.Exists(LegacyLogPath))
                return;

            File.Move(LegacyLogPath, LogPath);

            for (var generation = 1; generation <= Generations; generation++)
            {
                var from = $"{LegacyLogPath}.{generation}";
                var to = $"{LogPath}.{generation}";
                if (File.Exists(from) && !File.Exists(to))
                    File.Move(from, to);
            }
        }
        catch
        {
            // Keeping history is nice to have; never block logging on it.
        }
    }

    public static void Debug(string message) => Write("DEBUG", message);
    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message, Exception? exception = null) => Write("WARN", Describe(message, exception));
    public static void Error(string message, Exception? exception = null) => Write("ERROR", Describe(message, exception));

    /// <summary>Formats one log line in the shared convention.</summary>
    public static string FormatLine(DateTime timestamp, string level, string message)
        => $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {level,-5} {message}";

    private static string Describe(string message, Exception? exception)
        => exception == null ? message : $"{message} -- {exception.GetType().Name}: {exception.Message}";

    private static void Write(string level, string message)
    {
        var line = FormatLine(DateTime.Now, level, SingleLine(message)) + Environment.NewLine;

        lock (Gate)
        {
            try
            {
                var path = LogPath;
                var directory = Path.GetDirectoryName(path);

                // The logs folder is user-writable, and dialog.exe can run as SYSTEM, so never
                // write through a link a user left in it: drop links on the way down from the
                // data root before creating anything, then open the file itself without
                // following one (OpenAppend below).
                var root = string.IsNullOrEmpty(directory) ? null : Path.GetDirectoryName(directory);
                if (!string.IsNullOrEmpty(root))
                    SafeLogFile.RemoveLinks(root, path);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                if (!_migrated)
                {
                    // After CreateDirectory, so the destination root exists to move into.
                    _migrated = true;
                    MigrateLegacyLog();
                }

                RotateIfNeeded(path, Utf8NoBom.GetByteCount(line));
                using var writer = SafeLogFile.OpenAppend(path);
                writer.Write(line);
            }
            catch
            {
                // A diagnostic log that cannot be written is not worth failing over.
            }
        }
    }

    private static void RotateIfNeeded(string path, int incomingBytes)
    {
        var current = new FileInfo(path);
        if (!current.Exists || current.Length + incomingBytes <= MaxBytes)
            return;

        var oldest = $"{path}.{Generations}";
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (var generation = Generations - 1; generation >= 1; generation--)
        {
            var from = $"{path}.{generation}";
            if (File.Exists(from))
                File.Move(from, $"{path}.{generation + 1}");
        }

        File.Move(path, $"{path}.1");
    }

    private static string SingleLine(string message)
        => message.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
}

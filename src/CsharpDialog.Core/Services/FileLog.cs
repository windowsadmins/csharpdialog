using System.Text;

namespace csharpDialog.Core.Services;

/// <summary>
/// Minimal diagnostic file log with no dependencies.
///
/// Writes one line per entry, "[yyyy-MM-dd HH:mm:ss] LEVEL message" with the level padded
/// to five characters, to %ProgramData%\ManagedUtilities\logs\csharpdialog.log. The file
/// rolls at 5 MB and five generations are kept (csharpdialog.log.1 is the newest).
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

    /// <summary>
    /// Full path of the active log file. Defaults to
    /// %ProgramData%\ManagedUtilities\logs\csharpdialog.log; settable so a harness can
    /// redirect it.
    /// </summary>
    public static string LogPath
    {
        get => _logPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ManagedUtilities", "logs", "csharpdialog.log");
        set => _logPath = value;
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
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                RotateIfNeeded(path, Utf8NoBom.GetByteCount(line));
                File.AppendAllText(path, line, Utf8NoBom);
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

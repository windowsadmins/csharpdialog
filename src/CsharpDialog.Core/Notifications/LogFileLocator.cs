using csharpDialog.Core.Services;

namespace csharpDialog.Core.Notifications;

/// <summary>
/// Lists the CLI's diagnostic log and its rotated generations, newest first:
/// csharpdialog.log, then csharpdialog.log.1 through .5.
/// </summary>
public static class LogFileLocator
{
    public record LogFile(string Name, string Path, DateTime Modified, long SizeBytes);

    public static IReadOnlyList<LogFile> List(string activeLogPath)
    {
        var candidates = new List<string> { activeLogPath };
        for (var generation = 1; generation <= FileLog.Generations; generation++)
            candidates.Add($"{activeLogPath}.{generation}");

        var files = new List<LogFile>();
        foreach (var path in candidates)
        {
            try
            {
                var info = new FileInfo(path);
                if (info.Exists)
                    files.Add(new LogFile(info.Name, info.FullName, info.LastWriteTime, info.Length));
            }
            catch
            {
                // An unreadable generation is skipped rather than failing the list.
            }
        }
        return files;
    }
}

namespace csharpDialog.Core.Notifications;

public enum LogLineLevel { Default, Error, Warning, Success, Debug, Header }

/// <summary>
/// Colours a line of csharpDialog output by level. Two forms appear: the file log's
/// "[yyyy-MM-dd HH:mm:ss] LEVEL message" with an unbracketed, padded level token, and the
/// CLI's stdout, which tags diagnostics "[DEBUG] ..." and failures "Error: ...".
/// </summary>
public static class LogLineClassifier
{
    public static LogLineLevel Classify(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return LogLineLevel.Default;
        var trimmed = line.TrimStart();

        if (HasLevel(trimmed, "ERROR") || StartsWithTag(trimmed, "ERROR") || trimmed.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
            return LogLineLevel.Error;
        if (HasLevel(trimmed, "WARN") || StartsWithTag(trimmed, "WARN") || StartsWithTag(trimmed, "WARNING") || trimmed.StartsWith("Warning:", StringComparison.OrdinalIgnoreCase))
            return LogLineLevel.Warning;
        if (HasLevel(trimmed, "DEBUG") || StartsWithTag(trimmed, "DEBUG"))
            return LogLineLevel.Debug;
        if (trimmed.StartsWith("Result:", StringComparison.Ordinal))
            return LogLineLevel.Success;
        if (trimmed.StartsWith("===", StringComparison.Ordinal) || IsStartupLine(trimmed))
            return LogLineLevel.Header;
        return LogLineLevel.Default;
    }

    /// <summary>True for "[timestamp] LEVEL ..." where LEVEL is the given token.</summary>
    private static bool HasLevel(string line, string level)
    {
        if (!line.StartsWith('[')) return false;
        var close = line.IndexOf("] ", StringComparison.Ordinal);
        if (close < 0) return false;
        var rest = line.AsSpan(close + 2).TrimStart();
        return rest.StartsWith(level, StringComparison.Ordinal)
            && (rest.Length == level.Length || rest[level.Length] == ' ');
    }

    private static bool StartsWithTag(string line, string tag)
        => line.StartsWith($"[{tag}]", StringComparison.OrdinalIgnoreCase);

    /// <summary>The CLI logs "csharpdialog &lt;version&gt; starting: ..." once per run.</summary>
    private static bool IsStartupLine(string line)
        => line.Contains("csharpdialog ", StringComparison.Ordinal) && line.Contains(" starting:", StringComparison.Ordinal);
}

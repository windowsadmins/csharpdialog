using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace csharpDialog.Core.Services;

/// <summary>
/// The authorisation key, matching swiftDialog's AuthorisationKey. When policy sets
/// HKLM\SOFTWARE\Policies\csharpDialog\AuthorisationKey to the SHA-256 (hex) of a key, a dialog
/// is shown only to a caller that supplies that key, in the DIALOG_AUTH_KEY environment variable
/// or with --key. Anything else gets no window and exit code 30.
///
/// Only policy can turn this on. A hash written anywhere a standard user could reach, such as
/// HKCU or the tool's own settings key, is ignored -- otherwise anyone could pick their own key.
/// csharpDialog never reads the key from a file: where the plain key lives is the caller's business.
/// </summary>
public static class DialogAuthorisation
{
    public const string PolicyKeyPath = @"SOFTWARE\Policies\csharpDialog";
    public const string ValueName = "AuthorisationKey";
    public const string EnvironmentVariable = "DIALOG_AUTH_KEY";
    public const string KeyArgument = "--key";
    public const int RejectedExitCode = 30;
    public const string RejectedMessage = "Key authorisation required";

    /// <summary>Places a hash is ignored from, checked only so the log can say so.</summary>
    private static readonly (RegistryHive Hive, string Path)[] IgnoredSources =
    {
        (RegistryHive.CurrentUser, PolicyKeyPath),
        (RegistryHive.CurrentUser, @"SOFTWARE\csharpDialog"),
        (RegistryHive.LocalMachine, @"SOFTWARE\csharpDialog\Settings"),
        (RegistryHive.LocalMachine, @"SOFTWARE\csharpDialog"),
    };

    /// <summary>Lower-case hex SHA-256 of the trimmed key, as policy stores it.</summary>
    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim()))).ToLowerInvariant();

    /// <summary>
    /// Removes every --key and its value from <paramref name="args"/>, returning the last value
    /// seen, so the key never reaches the parser, the log or the console.
    /// </summary>
    public static string? ExtractKeyArgument(string[] args, out string[] remaining)
    {
        string? key = null;
        var rest = new List<string>(args.Length);
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], KeyArgument, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) key = args[++i];
                continue;
            }
            if (args[i].StartsWith(KeyArgument + "=", StringComparison.OrdinalIgnoreCase))
            {
                key = args[i][(KeyArgument.Length + 1)..];
                continue;
            }
            rest.Add(args[i]);
        }
        remaining = rest.ToArray();
        return key;
    }

    /// <summary>
    /// True when the dialog may be shown: no policy hash is set, or the supplied key (the
    /// environment variable first, then --key) hashes to it.
    /// </summary>
    public static bool IsAuthorised(string? policyHash, string? environmentKey, string? argumentKey)
    {
        if (string.IsNullOrWhiteSpace(policyHash)) return true;

        var key = !string.IsNullOrWhiteSpace(environmentKey) ? environmentKey : argumentKey;
        if (string.IsNullOrWhiteSpace(key)) return false;

        var expected = policyHash.Trim().ToLowerInvariant();
        var actual = Hash(key);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected));
    }

    /// <summary>The hash policy sets, or null. Reads the 64-bit HKLM view only.</summary>
    public static string? ReadPolicyHash()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(PolicyKeyPath);
            return key?.GetValue(ValueName) as string is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
        }
        catch (Exception ex)
        {
            FileLog.Warn($"Could not read {ValueName} from policy", ex);
            return null;
        }
    }

    /// <summary>True when policy sets a hash, for the GUI's Managed indicator.</summary>
    public static bool IsManaged => ReadPolicyHash() is not null;

    /// <summary>
    /// The check every entry point runs before anything else. Strips --key from
    /// <paramref name="args"/>; returns null to carry on, or the exit code to stop with after
    /// telling the caller why.
    /// </summary>
    public static int? Enforce(ref string[] args, Func<string?>? readPolicyHash = null, Func<string, string?>? readEnvironment = null)
    {
        var argumentKey = ExtractKeyArgument(args, out var remaining);
        args = remaining;

        var policyHash = (readPolicyHash ?? ReadPolicyHash)();
        if (readPolicyHash is null) LogIgnoredSources();

        var environmentKey = (readEnvironment ?? Environment.GetEnvironmentVariable)(EnvironmentVariable);
        if (IsAuthorised(policyHash, environmentKey, argumentKey)) return null;

        FileLog.Error(RejectedMessage);
        Console.Error.WriteLine(RejectedMessage);
        return RejectedExitCode;
    }

    private static void LogIgnoredSources()
    {
        if (!OperatingSystem.IsWindows()) return;
        foreach (var (hive, path) in IgnoredSources)
        {
            try
            {
                using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = root.OpenSubKey(path);
                if (key?.GetValue(ValueName) is not null)
                    FileLog.Warn($"Ignoring {ValueName} under {hive}\\{path}: only HKLM\\{PolicyKeyPath} is honoured");
            }
            catch { }
        }
    }
}

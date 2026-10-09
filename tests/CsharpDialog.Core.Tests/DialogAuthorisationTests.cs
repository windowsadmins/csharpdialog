using csharpDialog.Core.Services;
using Xunit;

namespace CsharpDialog.Core.Tests;

[Collection("FileLog")]
public sealed class DialogAuthorisationTests
{
    private const string Key = "correct horse battery staple";
    private static readonly string KeyHash = DialogAuthorisation.Hash(Key);

    private static int? Run(string[] args, string? policyHash, string? environmentKey, out string[] remaining)
    {
        var result = DialogAuthorisation.Enforce(ref args, () => policyHash, _ => environmentKey);
        remaining = args;
        return result;
    }

    [Fact]
    public void HashIsLowerCaseHexSha256OfTheTrimmedKey()
    {
        Assert.Equal("2c26b46b68ffc68ff99b453c1d30413413422d706483bfa0f98a5e886266e7ae", DialogAuthorisation.Hash("foo"));
        Assert.Equal(DialogAuthorisation.Hash("foo"), DialogAuthorisation.Hash("  foo \r\n"));
    }

    [Fact]
    public void NoPolicyHashRunsWithoutAKey()
    {
        Assert.Null(Run(new[] { "--title", "t" }, null, null, out _));
        Assert.Null(Run(new[] { "--title", "t" }, "", null, out _));
    }

    [Fact]
    public void RightKeyInTheEnvironmentRuns()
        => Assert.Null(Run(new[] { "--title", "t" }, KeyHash, Key, out _));

    [Fact]
    public void RightKeyAsAnArgumentRunsAndIsRemoved()
    {
        Assert.Null(Run(new[] { "--title", "t", "--key", Key }, KeyHash, null, out var remaining));
        Assert.Equal(new[] { "--title", "t" }, remaining);

        Assert.Null(Run(new[] { $"--key={Key}", "--title", "t" }, KeyHash, null, out remaining));
        Assert.Equal(new[] { "--title", "t" }, remaining);
    }

    [Fact]
    public void PolicyHashIsComparedCaseInsensitively()
        => Assert.Null(Run(Array.Empty<string>(), "  " + KeyHash.ToUpperInvariant() + " ", Key, out _));

    [Fact]
    public void EnvironmentWinsOverArgument()
    {
        Assert.Null(Run(new[] { "--key", "wrong" }, KeyHash, Key, out _));
        Assert.Equal(DialogAuthorisation.RejectedExitCode, Run(new[] { "--key", Key }, KeyHash, "wrong", out _));
    }

    [Fact]
    public void MissingKeyIsRefusedWithExitCode30()
        => Assert.Equal(30, Run(new[] { "--title", "t" }, KeyHash, null, out _));

    [Fact]
    public void WrongKeyIsRefusedWithExitCode30()
    {
        Assert.Equal(30, Run(Array.Empty<string>(), KeyHash, "wrong", out _));
        Assert.Equal(30, Run(new[] { "--key", "wrong" }, KeyHash, null, out _));
        Assert.Equal(30, Run(new[] { "--key" }, KeyHash, "   ", out _));
    }

    [Fact]
    public void ArgumentsCannotSupplyTheHash()
    {
        // Only policy sets the hash: an AuthorisationKey on the command line is just an unknown flag,
        // and a key with no policy hash set is simply not needed.
        Assert.Null(Run(new[] { "--AuthorisationKey", KeyHash, "--key", "anything" }, null, null, out var remaining));
        Assert.Equal(new[] { "--AuthorisationKey", KeyHash }, remaining);
    }

    [Fact]
    public void RealPolicyReaderIgnoresNonPolicySources()
    {
        // The reader looks only at HKLM\SOFTWARE\Policies\csharpDialog. A value written to the
        // user's own hive, which needs no elevation, must never turn enforcement on.
        if (!OperatingSystem.IsWindows()) return;
        using var user = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\csharpDialog");
        var hadValue = user.GetValue(DialogAuthorisation.ValueName);
        var machineHash = DialogAuthorisation.ReadPolicyHash();
        try
        {
            user.SetValue(DialogAuthorisation.ValueName, KeyHash);
            Assert.Equal(machineHash, DialogAuthorisation.ReadPolicyHash());
        }
        finally
        {
            if (hadValue is null) user.DeleteValue(DialogAuthorisation.ValueName, throwOnMissingValue: false);
            else user.SetValue(DialogAuthorisation.ValueName, hadValue);
        }
    }
}

using csharpDialog.Core;
using csharpDialog.Core.Notifications;
using csharpDialog.Core.Services;

namespace CsharpDialog.Core.Tests;

public class LogLineClassifierTests
{
    [Theory]
    [InlineData("[2026-10-06 09:12:44] ERROR Unhandled error -- IOException: boom", LogLineLevel.Error)]
    [InlineData("[2026-10-06 09:12:44] WARN  Could not start Cimian monitoring", LogLineLevel.Warning)]
    [InlineData("[2026-10-06 09:12:44] DEBUG tick", LogLineLevel.Debug)]
    [InlineData("[2026-10-06 09:12:44] INFO  Exiting with code 0", LogLineLevel.Default)]
    [InlineData("[2026-10-06 09:12:44] INFO  csharpdialog 1.0.0.0 starting: --window", LogLineLevel.Header)]
    [InlineData("[DEBUG] ProgressDialogWindow created", LogLineLevel.Debug)]
    [InlineData("Error: file not found", LogLineLevel.Error)]
    [InlineData("Warning: Could not start Cimian monitoring.", LogLineLevel.Warning)]
    [InlineData("Result: button1", LogLineLevel.Success)]
    [InlineData("", LogLineLevel.Default)]
    public void ClassifiesFileLogAndStdoutForms(string line, LogLineLevel expected)
        => Assert.Equal(expected, LogLineClassifier.Classify(line));

    [Fact]
    public void LevelTokenMustBeWholeWord()
        => Assert.Equal(LogLineLevel.Default, LogLineClassifier.Classify("[2026-10-06 09:12:44] INFO  WARNINGS cleared"));

    [Fact]
    public void FileLogFormatIsRecognised()
    {
        var line = FileLog.FormatLine(new DateTime(2026, 10, 6, 9, 0, 0), "WARN", "x");
        Assert.Equal(LogLineLevel.Warning, LogLineClassifier.Classify(line));
    }
}

public class TestDialogPresetsTests
{
    [Fact]
    public void OffersInfoProgressAndAlert()
        => Assert.Equal(["info", "progress", "alert"], TestDialogPresets.All.Select(p => p.Id));

    [Theory]
    [InlineData("info")]
    [InlineData("progress")]
    [InlineData("alert")]
    public void EveryPresetOpensAWindowAndTimesOut(string id)
    {
        var config = CommandLineParser.ParseArguments(TestDialogPresets.Find(id)!.Arguments.ToArray());
        Assert.True(config.Metadata.ContainsKey("WindowMode"));
        Assert.Equal(TestDialogPresets.TimeoutSeconds, config.Timeout);
        Assert.False(string.IsNullOrWhiteSpace(config.Title));
        Assert.False(string.IsNullOrWhiteSpace(config.Message));
    }

    [Fact]
    public void ProgressPresetShowsABarAndList()
    {
        var config = CommandLineParser.ParseArguments(TestDialogPresets.Progress.Arguments.ToArray());
        Assert.True(config.ShowProgressBar);
        Assert.Equal(40, config.ProgressValue);
        Assert.Equal(3, config.ListItems.Count);
    }

    [Fact]
    public void AlertPresetIsTopmostWithTwoButtons()
    {
        var config = CommandLineParser.ParseArguments(TestDialogPresets.Alert.Arguments.ToArray());
        Assert.True(config.Topmost);
        Assert.Equal(2, config.Buttons.Count);
    }

    [Fact]
    public void CommandLineQuotesOnlyWhereNeeded()
        => Assert.Equal("--title \"Two words\" --window", TestDialogPresets.ToCommandLine(["--title", "Two words", "--window"]));

    [Fact]
    public void FindIsCaseInsensitiveAndNullForUnknown()
    {
        Assert.Same(TestDialogPresets.Alert, TestDialogPresets.Find("ALERT"));
        Assert.Null(TestDialogPresets.Find("nope"));
    }
}

public class InstallPathsTests
{
    [Fact]
    public void PrefersTheCliBesideTheGui()
    {
        var dir = Directory.CreateTempSubdirectory("csd-gui-").FullName;
        try
        {
            var beside = Path.Combine(dir, InstallPaths.CliExecutableName);
            File.WriteAllText(beside, "");
            Assert.Equal(beside, InstallPaths.FindCli(dir));
            Assert.Equal(beside, InstallPaths.CliCandidates(dir)[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void InstallDirectoryIsUnchanged()
        => Assert.EndsWith(@"\csharpDialog", InstallPaths.InstallDirectory);

    [Fact]
    public void LogDirectoryIsTheFileLogFolder()
        => Assert.Equal(Path.GetDirectoryName(FileLog.LogPath), InstallPaths.LogDirectory);
}

public class LogFileLocatorTests
{
    [Fact]
    public void ListsActiveLogThenGenerationsAndSkipsMissing()
    {
        var dir = Directory.CreateTempSubdirectory("csd-logs-").FullName;
        try
        {
            var active = Path.Combine(dir, "csharpdialog.log");
            File.WriteAllText(active, "a");
            File.WriteAllText(active + ".2", "bb");
            File.WriteAllText(active + ".1", "c");

            var files = LogFileLocator.List(active);

            Assert.Equal(["csharpdialog.log", "csharpdialog.log.1", "csharpdialog.log.2"], files.Select(f => f.Name));
            Assert.Equal(2, files[2].SizeBytes);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void EmptyWhenNothingExists()
        => Assert.Empty(LogFileLocator.List(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "csharpdialog.log")));
}

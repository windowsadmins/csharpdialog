using System.Diagnostics;
using System.Runtime.InteropServices;
using csharpDialog.Core.Services;
using Xunit;

namespace CsharpDialog.Core.Tests;

[Collection("FileLog")]
public sealed class SafeLogFileTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("csd-logs-");
    private readonly DirectoryInfo _outside = Directory.CreateTempSubdirectory("csd-outside-");

    public void Dispose()
    {
        foreach (var dir in new[] { _root, _outside })
        {
            try { dir.Delete(recursive: true); } catch { }
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLinkW(string fileName, string existingFileName, IntPtr securityAttributes);

    /// <summary>A junction, which needs no privilege, unlike a symbolic link.</summary>
    private static void CreateJunction(string link, string target)
    {
        using var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
        {
            CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true
        })!;
        mklink.WaitForExit();
        Assert.Equal(0, mklink.ExitCode);
    }

    [Fact]
    public void AppendsToAPlainFile()
    {
        var path = Path.Combine(_root.FullName, "csharpdialog.log");
        File.WriteAllText(path, "first\n");

        using (var writer = SafeLogFile.OpenAppend(path))
            writer.WriteLine("second");

        Assert.Equal("first\nsecond" + Environment.NewLine, File.ReadAllText(path));
    }

    [Fact]
    public void JunctionedDayDirectoryIsRemovedNotFollowed()
    {
        var secret = Path.Combine(_outside.FullName, "keep.txt");
        File.WriteAllText(secret, "untouched");
        var day = Path.Combine(_root.FullName, "logs", "2026-10-06");
        Directory.CreateDirectory(Path.GetDirectoryName(day)!);
        CreateJunction(day, _outside.FullName);
        var log = Path.Combine(day, "csharpdialog.log");

        var notes = SafeLogFile.RemoveLinks(_root.FullName, log);

        Assert.Single(notes);
        Assert.False(Directory.Exists(day));
        Assert.Equal("untouched", File.ReadAllText(secret));
        Directory.CreateDirectory(day);
        using (var writer = SafeLogFile.OpenAppend(log))
            writer.WriteLine("entry");
        Assert.False(File.Exists(Path.Combine(_outside.FullName, "csharpdialog.log")));
    }

    [Fact]
    public void JunctionAtTheRootIsRemoved()
    {
        var root = Path.Combine(_root.FullName, "ManagedNotifications");
        CreateJunction(root, _outside.FullName);

        var notes = SafeLogFile.RemoveLinks(root, Path.Combine(root, "logs", "csharpdialog.log"));

        Assert.Single(notes);
        Assert.False(Directory.Exists(root));
        Assert.True(_outside.Exists);
    }

    [Fact]
    public void HardLinkedLogIsReplacedNotWrittenThrough()
    {
        var target = Path.Combine(_outside.FullName, "target.txt");
        File.WriteAllText(target, "untouched");
        var path = Path.Combine(_root.FullName, "events.jsonl");
        Assert.True(CreateHardLinkW(path, target, IntPtr.Zero));
        var notes = new List<string>();

        using (var writer = SafeLogFile.OpenAppend(path, notes))
            writer.WriteLine("entry");

        Assert.Equal("untouched", File.ReadAllText(target));
        Assert.Equal("entry" + Environment.NewLine, File.ReadAllText(path));
        Assert.Single(notes);
    }

    [Fact]
    public void SymbolicLinkedLogIsReplacedNotWrittenThrough()
    {
        var target = Path.Combine(_outside.FullName, "target.txt");
        File.WriteAllText(target, "untouched");
        var path = Path.Combine(_root.FullName, "csharpdialog.log");
        try
        {
            File.CreateSymbolicLink(path, target);
        }
        catch (IOException)
        {
            // Creating a symbolic link needs Developer Mode or elevation. The junction and
            // hard-link cases cover the same code without either.
            return;
        }
        var notes = new List<string>();

        using (var writer = SafeLogFile.OpenAppend(path, notes))
            writer.WriteLine("entry");

        Assert.Equal("untouched", File.ReadAllText(target));
        Assert.False(new FileInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint));
        Assert.Single(notes);
    }

    [Fact]
    public void FileLogDoesNotWriteThroughAHardLink()
    {
        var target = Path.Combine(_outside.FullName, "target.txt");
        File.WriteAllText(target, "untouched");
        var logs = Path.Combine(_root.FullName, "ManagedNotifications", "logs");
        Directory.CreateDirectory(logs);
        var path = Path.Combine(logs, "csharpdialog.log");
        Assert.True(CreateHardLinkW(path, target, IntPtr.Zero));

        var previous = FileLog.LogPath;
        FileLog.LogPath = path;
        try
        {
            FileLog.Info("entry");
        }
        finally
        {
            FileLog.LogPath = previous;
        }

        Assert.Equal("untouched", File.ReadAllText(target));
        Assert.Contains("entry", File.ReadAllText(path));
    }

    [Fact]
    public void FileLogRemovesAJunctionedLogsFolder()
    {
        var root = Path.Combine(_root.FullName, "ManagedNotifications");
        Directory.CreateDirectory(root);
        var logs = Path.Combine(root, "logs");
        CreateJunction(logs, _outside.FullName);

        var previous = FileLog.LogPath;
        FileLog.LogPath = Path.Combine(logs, "csharpdialog.log");
        try
        {
            FileLog.Info("entry");
        }
        finally
        {
            FileLog.LogPath = previous;
        }

        Assert.False(File.Exists(Path.Combine(_outside.FullName, "csharpdialog.log")));
        Assert.False(new DirectoryInfo(logs).Attributes.HasFlag(FileAttributes.ReparsePoint));
        Assert.Contains("entry", File.ReadAllText(Path.Combine(logs, "csharpdialog.log")));
    }
}

namespace csharpDialog.Core.Services;

/// <summary>
/// Cimian's on-disk layout, resolved from the standard folders rather than a hardcoded drive.
/// Mirrors the CimianPaths class in the Cimian repository: the client keeps its data under
/// %ProgramData%\ManagedInstalls and its binaries under %ProgramFiles%\Cimian.
/// </summary>
internal static class CimianPaths
{
    /// <summary>%ProgramData%\ManagedInstalls</summary>
    public static readonly string ManagedInstallsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ManagedInstalls");

    /// <summary>%ProgramFiles%\Cimian</summary>
    public static readonly string CimianInstallDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "Cimian");

    public static readonly string ManifestsDir = Path.Combine(ManagedInstallsRoot, "manifests");
    public static readonly string LogsDir = Path.Combine(ManagedInstallsRoot, "logs");
    public static readonly string ReportsDir = Path.Combine(ManagedInstallsRoot, "reports");

    /// <summary>The current run's log, truncated at the start of every run.</summary>
    public static readonly string ReportRunLog = Path.Combine(ReportsDir, "run.log");

    /// <summary>Present for the life of a bootstrap run; removed when it finishes.</summary>
    public static readonly string BootstrapFlagFile = Path.Combine(ManagedInstallsRoot, ".cimian.bootstrap");

    public static readonly string ManagedSoftwareUpdateExe = Path.Combine(CimianInstallDir, "managedsoftwareupdate.exe");
}

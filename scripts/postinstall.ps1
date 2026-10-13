<#
.SYNOPSIS
    Post-installation script for the csharpDialog package.

.DESCRIPTION
    Runs after the payload is copied to C:\Program Files\csharpDialog. It puts the install
    folder on the machine PATH, gives Managed Notifications Dialog a Start menu entry for
    every user, and locks down %ProgramData%\ManagedNotifications.

    This file is the one copy. Everything that packages csharpDialog reads it from here:
    cimipkg, which reads scripts/ out of the repository as checked out at the release tag
    and builds the MSI, and build.ps1, which stages it into the .pkg it builds.

    Each step is independent and never fails the install: a missing shortcut or ACL is
    reported and the script still exits 0.
#>

$installDir = 'C:\Program Files\csharpDialog'
$guiExe = Join-Path $installDir 'Managed Notifications Dialog.exe'
$shortcutPath = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Managed Notifications Dialog.lnk'

# Machine PATH, so `dialog` resolves in a new shell.
try {
    $machinePath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    $entries = @($machinePath -split ';' | Where-Object { $_ })
    if ($entries | Where-Object { $_.TrimEnd('\') -ieq $installDir }) {
        Write-Host "[csharpDialog] $installDir already on the machine PATH"
    } else {
        [Environment]::SetEnvironmentVariable('PATH', (($entries + $installDir) -join ';'), 'Machine')
        Write-Host "[csharpDialog] Added $installDir to the machine PATH"
    }
} catch {
    Write-Host "[csharpDialog] WARNING: could not update the machine PATH: $($_.Exception.Message)"
}

# Start menu entry for every user. Save() overwrites, so a reinstall refreshes it.
if (Test-Path -LiteralPath $guiExe) {
    try {
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $guiExe
        $shortcut.WorkingDirectory = $installDir
        $shortcut.IconLocation = "$guiExe,0"
        $shortcut.Description = 'Show test dialogs and read csharpDialog logs'
        $shortcut.Save()
        Write-Host "[csharpDialog] Start menu shortcut: $shortcutPath"
    } catch {
        Write-Host "[csharpDialog] WARNING: could not create the Start menu shortcut: $($_.Exception.Message)"
    }
} else {
    Write-Host "[csharpDialog] Managed Notifications Dialog.exe not in this package; no Start menu shortcut"
}

# %ProgramData%\ManagedNotifications: SYSTEM and Administrators full control, Users read,
# inheritance off. Only the logs subfolder lets users write, because dialog.exe runs in
# user context and appends to its log there.
try {
    $dataRoot = Join-Path $env:ProgramData 'ManagedNotifications'
    $logsDir = Join-Path $dataRoot 'logs'
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
    $acls = @(
        @($dataRoot, 'D:PAI(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;OICI;0x1200a9;;;BU)'),
        @($logsDir, 'D:PAI(A;OICI;FA;;;SY)(A;OICI;FA;;;BA)(A;OICI;0x1301bf;;;BU)')
    )
    foreach ($entry in $acls) {
        $security = New-Object System.Security.AccessControl.DirectorySecurity
        $security.SetSecurityDescriptorSddlForm($entry[1], 'Access')
        Set-Acl -Path $entry[0] -AclObject $security
    }
    Write-Host "[csharpDialog] Locked down $dataRoot; users may write only in logs"
} catch {
    Write-Host "[csharpDialog] WARNING: could not set the ManagedNotifications ACLs: $($_.Exception.Message)"
}

exit 0

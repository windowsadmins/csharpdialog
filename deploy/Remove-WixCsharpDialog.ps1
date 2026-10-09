<#
.SYNOPSIS
    Removes csharpDialog installs left by the WiX-built MSIs.

.DESCRIPTION
    The WiX-built csharpDialog MSIs generated a new UpgradeCode for every release and had
    no MajorUpgrade, so each release installed beside the previous ones instead of
    replacing them. A machine can carry several csharpDialog entries in Programs and
    Features, all sharing C:\Program Files\csharpDialog.

    This finds every Windows Installer product named "csharpDialog" whose publisher is
    "windowsadmins" (the WiX builds' Manufacturer) and uninstalls it. cimipkg-built
    csharpDialog packages carry the publisher "Windows Admins Open Source" and are never
    touched.

    Run it before installing the first cimipkg-built release. A WiX uninstall deletes the
    files in C:\Program Files\csharpDialog, the machine PATH entry and the Start menu
    shortcut, all of which the cimipkg package also uses, so running it afterwards leaves
    csharpDialog broken until the cimipkg package is installed again. sbin-installer
    already removes same-named products before it installs, in that order, so machines
    that receive csharpDialog through it need nothing more.

.PARAMETER WhatIf
    List what would be removed without removing it.

.EXAMPLE
    .\Remove-WixCsharpDialog.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$uninstallKeys = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
)

$wixInstalls = @(Get-ItemProperty $uninstallKeys -ErrorAction SilentlyContinue | Where-Object {
    $_.DisplayName -eq 'csharpDialog' -and
    $_.Publisher -eq 'windowsadmins' -and
    $_.WindowsInstaller -eq 1 -and
    $_.PSChildName -match '^\{[0-9A-Fa-f-]{36}\}$'
})

if ($wixInstalls.Count -eq 0) {
    Write-Output 'No WiX-built csharpDialog installs found.'
    exit 0
}

$failed = 0
foreach ($install in $wixInstalls) {
    $productCode = $install.PSChildName
    $label = "csharpDialog $($install.DisplayVersion) $productCode"
    if (-not $PSCmdlet.ShouldProcess($label, 'Uninstall')) {
        continue
    }

    $process = Start-Process msiexec.exe -ArgumentList "/x $productCode /qn REBOOT=ReallySuppress" -Wait -PassThru
    switch ($process.ExitCode) {
        0 { Write-Output "Removed $label" }
        3010 { Write-Output "Removed $label (restart required)" }
        1605 { Write-Output "Already gone: $label" }
        default {
            Write-Warning "Could not remove $label (msiexec exit $($process.ExitCode))"
            $failed++
        }
    }
}

exit ([int]($failed -gt 0))

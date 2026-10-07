<#
.SYNOPSIS
    Uninstall script for the csharpDialog package.

.DESCRIPTION
    Undoes what postinstall.ps1 added outside the install folder: the all-users Start menu
    shortcut and the machine PATH entry. %ProgramData%\ManagedNotifications is left in place
    so the logs survive, as the MSI does. Never fails the uninstall.
#>

$installDir = 'C:\Program Files\csharpDialog'
$shortcutPath = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Managed Notifications Dialog.lnk'

try {
    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath -Force
        Write-Host "[csharpDialog] Removed Start menu shortcut: $shortcutPath"
    }
} catch {
    Write-Host "[csharpDialog] WARNING: could not remove the Start menu shortcut: $($_.Exception.Message)"
}

try {
    $machinePath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')
    $entries = @($machinePath -split ';' | Where-Object { $_ })
    $kept = @($entries | Where-Object { $_.TrimEnd('\') -ine $installDir })
    if ($kept.Count -ne $entries.Count) {
        [Environment]::SetEnvironmentVariable('PATH', ($kept -join ';'), 'Machine')
        Write-Host "[csharpDialog] Removed $installDir from the machine PATH"
    }
} catch {
    Write-Host "[csharpDialog] WARNING: could not update the machine PATH: $($_.Exception.Message)"
}

exit 0

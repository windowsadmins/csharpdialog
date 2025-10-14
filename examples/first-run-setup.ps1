#Requires -Version 5.1
<#
.SYNOPSIS
    First-run device setup wizard
.DESCRIPTION
    Complete first-run experience for new device provisioning
.EXAMPLE
    .\first-run-setup.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"
$commandFile = "$env:TEMP\first-run.txt"

Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Starting first-run device setup..."

# Launch in kiosk mode - user cannot skip
$process = Start-Process $dialog -ArgumentList @(
    "--kiosk",
    "--commandfile", "`"$commandFile`"",
    "--title", "Setting Up Your Device",
    "--message", "Please wait while we configure your new device. This is a one-time setup process.",
    "--progressbar"
) -PassThru -NoNewWindow

Start-Sleep -Seconds 2

# Setup stages
$stages = @(
    @{
        Name = "Windows Configuration"
        Tasks = @(
            "Applying Windows settings",
            "Configuring Windows Update",
            "Setting regional options",
            "Configuring power settings"
        )
    },
    @{
        Name = "Corporate Policies"
        Tasks = @(
            "Joining domain",
            "Applying group policies",
            "Configuring compliance policies"
        )
    },
    @{
        Name = "Essential Software"
        Tasks = @(
            "Installing Microsoft 365",
            "Installing security tools",
            "Installing productivity apps"
        )
    },
    @{
        Name = "Security Configuration"
        Tasks = @(
            "Enabling BitLocker",
            "Installing certificates",
            "Configuring antivirus",
            "Enabling firewall"
        )
    },
    @{
        Name = "Finalization"
        Tasks = @(
            "Creating user profile",
            "Mapping network resources",
            "Cleaning up temporary files"
        )
    }
)

$totalStages = $stages.Count
$currentStage = 0

foreach ($stage in $stages) {
    $currentStage++
    $stagePercent = [math]::Round(($currentStage / $totalStages) * 100)
    
    Add-Content $commandFile "listitem: add, title: $($stage.Name), status: pending" -Encoding UTF8
    Add-Content $commandFile "progresstext: Stage $currentStage of ${totalStages}: $($stage.Name)..." -Encoding UTF8
    
    foreach ($task in $stage.Tasks) {
        Write-Host "  - $task"
        Add-Content $commandFile "listitem: update, title: $($stage.Name), statustext: $task..." -Encoding UTF8
        Start-Sleep -Seconds 3
    }
    
    Add-Content $commandFile "listitem: update, title: $($stage.Name), status: success, statustext: Complete" -Encoding UTF8
    Add-Content $commandFile "progress: $stagePercent" -Encoding UTF8
}

# Complete
Add-Content $commandFile "title: Setup Complete!" -Encoding UTF8
Add-Content $commandFile "message: Your device is ready! Please restart to complete the setup." -Encoding UTF8
Add-Content $commandFile "progresstext: First-run setup completed successfully" -Encoding UTF8
Start-Sleep -Seconds 5

Add-Content $commandFile "quit" -Encoding UTF8

Wait-Process -Id $process.Id -Timeout 15 -ErrorAction SilentlyContinue
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "`nFirst-run setup completed. Device is ready for use."

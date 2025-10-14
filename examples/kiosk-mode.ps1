#Requires -Version 5.1
<#
.SYNOPSIS
    Kiosk mode demonstration
.DESCRIPTION
    Shows kiosk mode - fullscreen with blur, user cannot close until script completes
.EXAMPLE
    .\kiosk-mode.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"
$commandFile = "$env:TEMP\kiosk-commands.txt"

# Clean up
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Launching kiosk mode dialog..."
Write-Host "User will NOT be able to close this until we send the 'quit' command"

# Launch in kiosk mode
$process = Start-Process $dialog -ArgumentList @(
    "--kiosk",
    "--commandfile", "`"$commandFile`"",
    "--title", "Critical System Configuration",
    "--message", "Your device is being configured. Please do not turn off your computer.",
    "--progressbar"
) -PassThru -NoNewWindow

Start-Sleep -Seconds 2

# Simulate critical process
$steps = @(
    @{Name = "Configuring Security"; Duration = 3},
    @{Name = "Installing Certificates"; Duration = 2},
    @{Name = "Applying Group Policies"; Duration = 3},
    @{Name = "Finalizing Setup"; Duration = 2}
)

$totalSteps = $steps.Count
for ($i = 0; $i -lt $totalSteps; $i++) {
    $step = $steps[$i]
    $percent = [math]::Round((($i + 1) / $totalSteps) * 100)
    
    Add-Content $commandFile "listitem: add, title: $($step.Name), status: pending, statustext: In progress..." -Encoding UTF8
    Add-Content $commandFile "progress: $percent" -Encoding UTF8
    Add-Content $commandFile "progresstext: $($step.Name)... ($($i + 1) of $totalSteps)" -Encoding UTF8
    
    Start-Sleep -Seconds $step.Duration
    
    Add-Content $commandFile "listitem: update, title: $($step.Name), status: success, statustext: Complete" -Encoding UTF8
}

# Complete
Add-Content $commandFile "message: Configuration complete! Your device is ready." -Encoding UTF8
Add-Content $commandFile "progresstext: All tasks completed successfully" -Encoding UTF8
Start-Sleep -Seconds 2

# Close dialog (this is the ONLY way to close kiosk mode)
Write-Host "Sending quit command to close kiosk dialog..."
Add-Content $commandFile "quit" -Encoding UTF8

Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Kiosk mode demo completed"

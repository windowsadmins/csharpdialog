#Requires -Version 5.1
<#
.SYNOPSIS
    Maintenance mode notification
.DESCRIPTION
    Displays a maintenance window with countdown and system status
.EXAMPLE
    .\maintenance-mode.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"
$commandFile = "$env:TEMP\maintenance.txt"

Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Entering maintenance mode..."

# Launch fullscreen maintenance dialog
$process = Start-Process $dialog -ArgumentList @(
    "--fullscreen",
    "--commandfile", "`"$commandFile`"",
    "--title", "System Maintenance in Progress",
    "--message", "Critical system maintenance is being performed. Please do not power off your device.",
    "--progressbar"
) -PassThru -NoNewWindow

Start-Sleep -Seconds 2

# Maintenance tasks
$tasks = @(
    @{Name = "Disk Cleanup"; Duration = 5},
    @{Name = "Database Optimization"; Duration = 4},
    @{Name = "Security Scan"; Duration = 6},
    @{Name = "System Updates"; Duration = 5},
    @{Name = "Cache Clear"; Duration = 3}
)

$totalTasks = $tasks.Count
$completed = 0

foreach ($task in $tasks) {
    $completed++
    $percent = [math]::Round(($completed / $totalTasks) * 100)
    
    Add-Content $commandFile "listitem: add, title: $($task.Name), status: pending, statustext: Running..." -Encoding UTF8
    Add-Content $commandFile "progresstext: Performing $($task.Name)... ($completed of $totalTasks)" -Encoding UTF8
    Add-Content $commandFile "progress: $percent" -Encoding UTF8
    
    Start-Sleep -Seconds $task.Duration
    
    Add-Content $commandFile "listitem: update, title: $($task.Name), status: success, statustext: Complete" -Encoding UTF8
}

# Countdown to exit
for ($i = 10; $i -gt 0; $i--) {
    Add-Content $commandFile "message: Maintenance complete! System will return to normal in $i seconds..." -Encoding UTF8
    Add-Content $commandFile "progresstext: Returning to normal operation..." -Encoding UTF8
    Start-Sleep -Seconds 1
}

Add-Content $commandFile "quit" -Encoding UTF8

Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Maintenance completed successfully"

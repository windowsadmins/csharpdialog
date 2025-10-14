#Requires -Version 5.1
<#
.SYNOPSIS
    Software update notification and installer
.DESCRIPTION
    Notifies user of available updates and performs installation
.EXAMPLE
    .\update-notification.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

# Check for updates (simulated)
$updates = @(
    @{Name = "Windows Security Update KB5001234"; Size = "250 MB"; Critical = $true},
    @{Name = "Microsoft Office Update"; Size = "180 MB"; Critical = $false},
    @{Name = "Adobe Acrobat Update"; Size = "95 MB"; Critical = $false}
)

$criticalCount = ($updates | Where-Object {$_.Critical}).Count
$totalSize = ($updates | ForEach-Object {[int]($_.Size -replace '[^\d]','')}) | Measure-Object -Sum | Select-Object -ExpandProperty Sum

$message = if ($criticalCount -gt 0) {
    "Your device has $criticalCount critical update(s) available ($totalSize MB total). Install now?"
} else {
    "Your device has $($updates.Count) update(s) available ($totalSize MB total). Install now?"
}

# Show update prompt
& $dialog `
    --title "Software Updates Available" `
    --message $message `
    --listitem "Windows Security Update KB5001234,wait" `
    --listitem "Microsoft Office Update,wait" `
    --listitem "Adobe Acrobat Update,wait" `
    --button1text "Install Now" `
    --button2text "Remind Me Later"

if ($LASTEXITCODE -eq 0) {
    Write-Host "User chose to install updates"
    
    # Launch update installer
    $commandFile = "$env:TEMP\updates.txt"
    Remove-Item $commandFile -Force -ErrorAction SilentlyContinue
    
    $process = Start-Process $dialog -ArgumentList @(
        "--commandfile", "`"$commandFile`"",
        "--title", "Installing Updates",
        "--message", "Installing updates. Your device may restart when complete.",
        "--progressbar"
    ) -PassThru -NoNewWindow
    
    Start-Sleep -Seconds 2
    
    # Pre-populate list
    foreach ($update in $updates) {
        Add-Content $commandFile "listitem: add, title: $($update.Name), status: wait, statustext: Queued" -Encoding UTF8
    }
    
    # Install each update
    $total = $updates.Count
    for ($i = 0; $i -lt $total; $i++) {
        $update = $updates[$i]
        $percent = [math]::Round((($i + 1) / $total) * 100)
        
        Add-Content $commandFile "listitem: update, title: $($update.Name), status: pending, statustext: Installing..." -Encoding UTF8
        Add-Content $commandFile "progresstext: Installing $($update.Name)..." -Encoding UTF8
        
        # Simulate download and install
        Start-Sleep -Seconds 4
        
        Add-Content $commandFile "listitem: update, title: $($update.Name), status: success, statustext: Installed" -Encoding UTF8
        Add-Content $commandFile "progress: $percent" -Encoding UTF8
    }
    
    Add-Content $commandFile "message: All updates installed successfully!" -Encoding UTF8
    Add-Content $commandFile "progresstext: Updates complete" -Encoding UTF8
    Start-Sleep -Seconds 2
    
    Add-Content $commandFile "quit" -Encoding UTF8
    
    Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
    Remove-Item $commandFile -Force -ErrorAction SilentlyContinue
    
    Write-Host "Updates installed successfully"
} else {
    Write-Host "User postponed updates"
}

#Requires -Version 5.1
<#
.SYNOPSIS
    Command file demonstration with live updates
.DESCRIPTION
    Shows how to use command file for real-time updates to the dialog
.EXAMPLE
    .\command-file-demo.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"
$commandFile = "$env:TEMP\dialog-commands.txt"

# Clean up any existing command file
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

# Launch dialog with command file monitoring
$process = Start-Process $dialog -ArgumentList @(
    "--title", "Live Update Demo",
    "--message", "Watch as this dialog updates in real-time...",
    "--progressbar",
    "--commandfile", "`"$commandFile`""
) -PassThru -NoNewWindow

# Wait for dialog to initialize
Start-Sleep -Seconds 2

Write-Host "Sending updates to dialog..."

# Add initial items
Add-Content $commandFile "listitem: add, title: Step 1, status: pending, statustext: Starting..." -Encoding UTF8
Add-Content $commandFile "listitem: add, title: Step 2, status: wait, statustext: Waiting..." -Encoding UTF8
Add-Content $commandFile "listitem: add, title: Step 3, status: wait, statustext: Waiting..." -Encoding UTF8
Start-Sleep -Seconds 2

# Update Step 1
Add-Content $commandFile "progress: 33" -Encoding UTF8
Add-Content $commandFile "progresstext: Processing step 1..." -Encoding UTF8
Add-Content $commandFile "listitem: update, title: Step 1, status: success, statustext: Complete!" -Encoding UTF8
Start-Sleep -Seconds 2

# Update Step 2
Add-Content $commandFile "progress: 66" -Encoding UTF8
Add-Content $commandFile "progresstext: Processing step 2..." -Encoding UTF8
Add-Content $commandFile "listitem: update, title: Step 2, status: pending, statustext: In progress..." -Encoding UTF8
Start-Sleep -Seconds 2
Add-Content $commandFile "listitem: update, title: Step 2, status: success, statustext: Complete!" -Encoding UTF8

# Update Step 3
Add-Content $commandFile "progress: 100" -Encoding UTF8
Add-Content $commandFile "progresstext: Finishing up..." -Encoding UTF8
Add-Content $commandFile "listitem: update, title: Step 3, status: success, statustext: Complete!" -Encoding UTF8
Add-Content $commandFile "message: All steps completed successfully!" -Encoding UTF8
Start-Sleep -Seconds 2

# Close dialog
Add-Content $commandFile "quit" -Encoding UTF8

# Wait for process to exit
Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue

# Cleanup
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Demo completed!"

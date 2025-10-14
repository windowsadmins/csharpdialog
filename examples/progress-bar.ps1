#Requires -Version 5.1
<#
.SYNOPSIS
    Progress bar demonstration
.DESCRIPTION
    Shows how to use a static progress bar with percentage
.EXAMPLE
    .\progress-bar.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

# Display dialog with progress bar at 65%
& $dialog `
    --title "Processing" `
    --message "Processing your request. Please wait..." `
    --progressbar `
    --progress 65 `
    --progresstext "Step 3 of 5 complete" `
    --button1text "Cancel"

Write-Host "Dialog result: $LASTEXITCODE"

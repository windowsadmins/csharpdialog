#Requires -Version 5.1
<#
.SYNOPSIS
    List items with status indicators
.DESCRIPTION
    Demonstrates displaying multiple list items with different status states
.EXAMPLE
    .\list-items.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

# Display list with various status indicators
& $dialog `
    --title "System Check" `
    --message "Checking system components..." `
    --listitem "Windows Updates,success" `
    --listitem "Antivirus Status,success" `
    --listitem "Disk Space,wait" `
    --listitem "Network Connection,pending" `
    --listitem "Firewall,fail" `
    --progressbar `
    --progress 60 `
    --button1text "Close"

Write-Host "Dialog closed"

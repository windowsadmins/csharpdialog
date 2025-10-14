#Requires -Version 5.1
<#
.SYNOPSIS
    Fullscreen mode with blurred background
.DESCRIPTION
    Demonstrates fullscreen mode with Windows 11 backdrop blur effect
.EXAMPLE
    .\fullscreen-mode.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

# Display dialog in fullscreen with blur
& $dialog `
    --fullscreen `
    --title "Important System Update" `
    --message "Your device requires important updates. This will only take a few minutes." `
    --listitem "Security Patches,pending" `
    --listitem "Feature Updates,wait" `
    --listitem "Driver Updates,wait" `
    --progressbar `
    --progress 25 `
    --progresstext "Downloading updates..." `
    --button1text "Continue" `
    --button2text "Postpone"

if ($LASTEXITCODE -eq 0) {
    Write-Host "User clicked Continue"
} else {
    Write-Host "User clicked Postpone"
}

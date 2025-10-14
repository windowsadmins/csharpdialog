#Requires -Version 5.1
<#
.SYNOPSIS
    Button actions demonstration
.DESCRIPTION
    Shows how to use multiple buttons and handle user responses
.EXAMPLE
    .\button-actions.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

# Display dialog with two buttons
& $dialog `
    --title "Confirm Action" `
    --message "Would you like to proceed with the system update?" `
    --button1text "Update Now" `
    --button2text "Cancel"

# Handle user response
switch ($LASTEXITCODE) {
    0 {
        Write-Host "User clicked: Update Now"
        Write-Host "Starting update process..."
        # Add update logic here
    }
    2 {
        Write-Host "User clicked: Cancel"
        Write-Host "Update cancelled by user"
    }
    default {
        Write-Host "Dialog closed with code: $LASTEXITCODE"
    }
}

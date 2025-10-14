#Requires -Version 5.1
<#
.SYNOPSIS
    Basic message dialog example
.DESCRIPTION
    Demonstrates the simplest use case - displaying a message with a single button
.EXAMPLE
    .\basic-message.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

# Simple informational message
& $dialog `
    --title "Welcome!" `
    --message "Welcome to csharpDialog. This is a basic message dialog with a single button." `
    --button1text "OK"

Write-Host "Dialog closed with result: $LASTEXITCODE"

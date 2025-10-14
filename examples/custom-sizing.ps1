#Requires -Version 5.1
<#
.SYNOPSIS
    Custom window sizing demonstration
.DESCRIPTION
    Shows how to customize window width and height
.EXAMPLE
    .\custom-sizing.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"

Write-Host "Demonstrating different window sizes...`n"

# Small dialog
Write-Host "1. Compact dialog (600x400)..."
& $dialog `
    --title "Compact Dialog" `
    --message "This is a small, compact dialog window." `
    --width 600 `
    --height 400 `
    --button1text "Next"

# Default tall dialog (new default height is 1000)
Write-Host "2. Default tall dialog (750x1000)..."
& $dialog `
    --title "Tall Dialog" `
    --message "This uses the new default height of 1000px - perfect for list items!" `
    --listitem "Item 1,success" `
    --listitem "Item 2,success" `
    --listitem "Item 3,pending" `
    --listitem "Item 4,wait" `
    --listitem "Item 5,wait" `
    --listitem "Item 6,wait" `
    --listitem "Item 7,wait" `
    --listitem "Item 8,wait" `
    --button1text "Next"

# Wide dialog
Write-Host "3. Wide dialog (1200x800)..."
& $dialog `
    --title "Wide Dialog" `
    --message "This is a wider dialog useful for displaying more content horizontally." `
    --width 1200 `
    --height 800 `
    --progressbar `
    --progress 75 `
    --button1text "Close"

Write-Host "`nWindow sizing demo complete!"

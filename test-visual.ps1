# Simple Visual Test for csharpDialog WPF
# Quick way to test different visual dialogs

$wpfDll = "src\CsharpDialog.WPF\bin\Debug\net9.0-windows\CsharpDialog.WPF.dll"

Write-Host "csharpDialog Visual Test Menu" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Welcome Dialog"
Write-Host "2. Confirmation Dialog (Yes/No)"
Write-Host "3. Installation Dialog"
Write-Host "4. Warning Dialog"
Write-Host "5. Information Dialog"
Write-Host "6. Custom Styled Dialog"
Write-Host "0. Exit"
Write-Host ""

do {
    $choice = Read-Host "Select a dialog to test (0-6)"
    
    switch ($choice) {
        "1" {
            Write-Host "Launching Welcome Dialog..." -ForegroundColor Green
            & dotnet $wpfDll --title "Welcome" --message "Welcome to csharpDialog! This is the visual WPF version."
        }
        "2" {
            Write-Host "Launching Confirmation Dialog..." -ForegroundColor Green
            & dotnet $wpfDll --title "Confirm Action" --message "Do you want to continue?" --button1 "Yes" --button2 "No"
        }
        "3" {
            Write-Host "Launching Installation Dialog..." -ForegroundColor Green
            & dotnet $wpfDll --title "Install Software" --message "Install Microsoft Office 365?`n`nThis will download and install the latest version." --button1 "Install" --button2 "Cancel"
        }
        "4" {
            Write-Host "Launching Warning Dialog..." -ForegroundColor Green
            & dotnet $wpfDll --title "System Warning" --message "System will restart in 10 minutes for updates.`n`nSave your work now!" --button1 "OK" --button2 "Postpone"
        }
        "5" {
            Write-Host "Launching Information Dialog..." -ForegroundColor Green
            & dotnet $wpfDll --title "Backup Complete" --message "Backup completed successfully!`n`nFiles: 1,234`nSize: 2.5 GB" --button1 "View Log" --button2 "Close"
        }
        "6" {
            Write-Host "Launching Custom Styled Dialog..." -ForegroundColor Green
            & dotnet $wpfDll --title "Custom Style" --message "This dialog has custom styling!" --fontsize "16" --width "450" --height "250"
        }
        "0" {
            Write-Host "Exiting..." -ForegroundColor Yellow
            break
        }
        default {
            Write-Host "Invalid choice. Please select 0-6." -ForegroundColor Red
        }
    }
    
    if ($choice -ne "0") {
        Write-Host "Dialog closed. Return code: $LASTEXITCODE" -ForegroundColor Gray
        Write-Host ""
    }
    
} while ($choice -ne "0")

Write-Host "Visual test complete!" -ForegroundColor Green

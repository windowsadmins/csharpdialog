# csharpDialog WPF GUI Demo Script
# This script demonstrates the visual WPF dialogs

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  csharpDialog WPF GUI Visual Demos     " -ForegroundColor Cyan  
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$wpfDll = Join-Path $PSScriptRoot "src\CsharpDialog.WPF\bin\Debug\net9.0-windows\CsharpDialog.WPF.dll"

if (!(Test-Path $wpfDll)) {
    Write-Host "❌ WPF application not found. Building..." -ForegroundColor Red
    & dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
}

function Show-VisualDialog {
    param(
        [string]$Description,
        [string[]]$Arguments
    )
    
    Write-Host "🖼️  $Description" -ForegroundColor Green
    Write-Host "   Command: dotnet csharpDialog.WPF.dll $($Arguments -join ' ')" -ForegroundColor Gray
    Write-Host "   Press any key when dialog is closed to continue..." -ForegroundColor Yellow
    
    Start-Process -FilePath "dotnet" -ArgumentList @($wpfDll) + $Arguments -Wait -NoNewWindow
    Read-Host "   Dialog closed. Press Enter for next demo"
    Write-Host ""
}

Write-Host "🚀 Starting WPF Visual Dialog demonstrations..." -ForegroundColor Green
Write-Host ""
Write-Host "Note: Each dialog will open in a separate window." -ForegroundColor Cyan
Write-Host "      Close each dialog to proceed to the next one." -ForegroundColor Cyan
Write-Host ""

# Demo 1: Basic Welcome Dialog
Show-VisualDialog "Welcome Dialog" @("--title", "Welcome to csharpDialog", "--message", "This is the WPF GUI version of csharpDialog! You can see the visual interface with proper Windows styling.")

# Demo 2: Confirmation Dialog
Show-VisualDialog "Confirmation Dialog" @("--title", "Confirm Action", "--message", "Do you want to proceed with this installation?`n`nThis will install Office 365 on your computer.", "--button1", "Install", "--button2", "Cancel")

# Demo 3: Styled Dialog
Show-VisualDialog "Styled Dialog" @("--title", "Custom Styling", "--message", "This dialog demonstrates custom colors and fonts.", "--backgroundcolor", "#f0f8ff", "--textcolor", "#2c3e50", "--fontsize", "14")

# Demo 4: Administrative Warning
Show-VisualDialog "System Warning" @("--title", "System Maintenance Required", "--message", "Your computer will restart in 10 minutes for important updates.`n`nPlease save your work immediately.", "--button1", "OK", "--button2", "Postpone", "--topmost")

# Demo 5: Information Dialog
Show-VisualDialog "Information Dialog" @("--title", "Backup Complete", "--message", "Your data backup has completed successfully!`n`nFiles backed up: 1,234`nTotal size: 2.5 GB`nLocation: \\server\backups\", "--button1", "View Details", "--button2", "Close")

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "        WPF GUI Demo Complete!           " -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Visual Features Demonstrated:" -ForegroundColor White
Write-Host "   • Native Windows dialog styling" -ForegroundColor Green
Write-Host "   • Custom titles and messages" -ForegroundColor Green
Write-Host "   • Multiple button configurations" -ForegroundColor Green
Write-Host "   • Color and font customization" -ForegroundColor Green
Write-Host "   • Multi-line text support" -ForegroundColor Green
Write-Host "   • Window positioning (topmost)" -ForegroundColor Green
Write-Host ""
Write-Host "🎯 Perfect for:" -ForegroundColor Yellow
Write-Host "   • User notifications" 
Write-Host "   • Installation confirmations"
Write-Host "   • System maintenance alerts"
Write-Host "   • Deployment automation"
Write-Host "   • Administrative scripts"
Write-Host ""

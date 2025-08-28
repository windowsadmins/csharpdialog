# csharpDialog Demo Script
# This script demonstrates the key features of csharpDialog

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  csharpDialog - Windows Port of swiftDialog Demo   " -ForegroundColor Cyan  
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

$dllPath = Join-Path $PSScriptRoot "src\CsharpDialog.CLI\bin\Debug\net9.0\CsharpDialog.CLI.dll"

if (!(Test-Path $dllPath)) {
    Write-Host "❌ csharpDialog not found. Building project..." -ForegroundColor Red
    & dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful!" -ForegroundColor Green
}

Write-Host "🚀 Starting csharpDialog demonstrations..." -ForegroundColor Green
Write-Host ""

# Demo 1: Basic Information Dialog
Write-Host "Demo 1: Basic Information Dialog" -ForegroundColor Yellow
Write-Host "Command: --title 'Welcome' --message 'Welcome to csharpDialog!'"
echo "1" | & dotnet $dllPath --title "Welcome" --message "Welcome to csharpDialog! This is the console fallback mode."
Write-Host ""

# Demo 2: Confirmation Dialog
Write-Host "Demo 2: Confirmation Dialog (selecting 'Yes')" -ForegroundColor Yellow
Write-Host "Command: --title 'Confirm' --message 'Do you want to continue?' --button1 'Yes' --button2 'No'"
echo "1" | & dotnet $dllPath --title "Confirm" --message "Do you want to continue with the installation?" --button1 "Yes" --button2 "No"
Write-Host ""

# Demo 3: Confirmation Dialog - No
Write-Host "Demo 3: Confirmation Dialog (selecting 'No')" -ForegroundColor Yellow
Write-Host "Command: Same as above, but selecting option 2"
echo "2" | & dotnet $dllPath --title "Confirm" --message "Are you sure you want to delete this file?" --button1 "Delete" --button2 "Cancel"
Write-Host ""

# Demo 4: Administrative Script Example
Write-Host "Demo 4: Administrative Script Example" -ForegroundColor Yellow
Write-Host "Command: Administrative-style dialog"
echo "1" | & dotnet $dllPath --title "System Maintenance" --message "Windows Update will restart your computer in 5 minutes. Save your work now." --button1 "OK" --button2 "Postpone"
Write-Host ""

# Demo 5: Help Display
Write-Host "Demo 5: Help System" -ForegroundColor Yellow
Write-Host "Command: --help"
& dotnet $dllPath --help
Write-Host ""

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "                 Demo Complete!                     " -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Key Features Demonstrated:" -ForegroundColor White
Write-Host "✅ Basic dialogs with title and message" -ForegroundColor Green
Write-Host "✅ Custom button configurations" -ForegroundColor Green  
Write-Host "✅ Return values and exit codes" -ForegroundColor Green
Write-Host "✅ Console fallback mode (when GUI not available)" -ForegroundColor Green
Write-Host "✅ Command-line argument parsing" -ForegroundColor Green
Write-Host "✅ Help system" -ForegroundColor Green
Write-Host ""
Write-Host "Enterprise Features:" -ForegroundColor White
Write-Host "🔐 Code signing ready (EmilyCarrU Intune Windows Enterprise Certificate)" -ForegroundColor Cyan
Write-Host "🚀 PowerShell launcher for restricted environments" -ForegroundColor Cyan
Write-Host "📦 Multi-project architecture (Core, CLI, WPF)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "• Configure code signing for your environment"
Write-Host "• Test WPF GUI mode (requires signed binaries or execution policy changes)"
Write-Host "• Add custom icons, images, and styling"
Write-Host "• Integrate into your deployment scripts"
Write-Host ""
Write-Host "Usage Examples:" -ForegroundColor White
Write-Host '.\csharpdialog.ps1 --title "Deploy Software" --message "Install Office 365?" --button1 "Install" --button2 "Skip"'
Write-Host 'dotnet "src\CsharpDialog.CLI\bin\Debug\net9.0\CsharpDialog.CLI.dll" --title "Backup" --message "Backup completed successfully!"'
Write-Host ""

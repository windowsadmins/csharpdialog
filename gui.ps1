# WPF GUI Launcher for csharpDialog
# This script launches the visual WPF version of csharpDialog

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments = @()
)

Write-Host "🎨 Launching csharpDialog WPF GUI..." -ForegroundColor Cyan

$wpfDllPath = Join-Path $PSScriptRoot "src\CsharpDialog.WPF\bin\Debug\net9.0-windows\CsharpDialog.WPF.dll"
$wpfExePath = Join-Path $PSScriptRoot "src\CsharpDialog.WPF\bin\Debug\net9.0-windows\CsharpDialog.WPF.exe"

# Check if built
if (!(Test-Path $wpfDllPath)) {
    Write-Host "Building WPF application..." -ForegroundColor Yellow
    & dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        exit 1
    }
}

Write-Host "Attempting to launch WPF GUI..." -ForegroundColor Green

# Try different methods to launch the WPF app
try {
    # Method 1: Try running the DLL directly with dotnet
    Write-Host "Method 1: Running via dotnet (recommended for enterprise)" -ForegroundColor Yellow
    if ($Arguments.Count -gt 0) {
        & dotnet $wpfDllPath @Arguments
    } else {
        & dotnet $wpfDllPath --title "csharpDialog WPF" --message "Welcome to the visual version of csharpDialog!" --button1 "Awesome!" --button2 "Close"
    }
} catch {
    Write-Host "Method 1 failed: $($_.Exception.Message)" -ForegroundColor Red
    
    try {
        # Method 2: Try running the executable directly (may fail due to signing)
        Write-Host "Method 2: Attempting direct executable launch..." -ForegroundColor Yellow
        if (Test-Path $wpfExePath) {
            if ($Arguments.Count -gt 0) {
                & $wpfExePath @Arguments
            } else {
                & $wpfExePath --title "csharpDialog WPF" --message "Welcome to the visual version!" --button1 "Great!" --button2 "Exit"
            }
        } else {
            Write-Error "WPF executable not found at: $wpfExePath"
        }
    } catch {
        Write-Host "Method 2 failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "💡 Troubleshooting:" -ForegroundColor Yellow
        Write-Host "1. The executable may need to be signed with 'EmilyCarrU Intune Windows Enterprise Certificate'"
        Write-Host "2. You can try running: .\sign-build.ps1 -Sign"
        Write-Host "3. Or modify your execution policy temporarily"
        Write-Host "4. The console version works fine with: .\csharpdialog.ps1"
    }
}

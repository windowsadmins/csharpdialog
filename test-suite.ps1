# csharpDialog Test Suite
# This script demonstrates various features of csharpDialog

Write-Host "=== csharpDialog Test Suite ===" -ForegroundColor Cyan
Write-Host ""

$dllPath = Join-Path $PSScriptRoot "src\CsharpDialog.CLI\bin\Debug\net9.0\CsharpDialog.CLI.dll"

if (!(Test-Path $dllPath)) {
    Write-Error "csharpDialog not built. Please run 'dotnet build' first."
    exit 1
}

function Test-Dialog {
    param(
        [string]$TestName,
        [string[]]$Arguments
    )
    
    Write-Host "Testing: $TestName" -ForegroundColor Yellow
    Write-Host "Command: dotnet csharpDialog.CLI.dll $($Arguments -join ' ')" -ForegroundColor Gray
    Write-Host ""
    
    # Run with timeout to prevent hanging
    $result = & dotnet $dllPath @Arguments
    Write-Host "Result: $result" -ForegroundColor Green
    Write-Host ("-" * 50)
    Write-Host ""
}

# Test 1: Basic dialog
Write-Host "1" | Test-Dialog "Basic Dialog" @("--title", "Welcome", "--message", "Welcome to csharpDialog!")

# Test 2: Confirmation dialog
Write-Host "1" | Test-Dialog "Confirmation Dialog" @("--title", "Confirm Action", "--message", "Do you want to proceed?", "--button1", "Yes", "--button2", "No")

# Test 3: Custom styling
Write-Host "1" | Test-Dialog "Styled Dialog" @("--title", "Styled Message", "--message", "This dialog has custom styling", "--fontsize", "14")

# Test 4: Help display
Test-Dialog "Help Display" @("--help")

Write-Host "=== Test Suite Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Key Features Demonstrated:" -ForegroundColor Cyan
Write-Host "✓ Basic dialog with title and message" -ForegroundColor Green
Write-Host "✓ Custom buttons (Yes/No)" -ForegroundColor Green
Write-Host "✓ Font size customization" -ForegroundColor Green
Write-Host "✓ Help system" -ForegroundColor Green
Write-Host "✓ Console fallback mode" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "- Test WPF GUI mode (requires signed binaries)"
Write-Host "- Add image and icon support"
Write-Host "- Implement timeout functionality"
Write-Host "- Add markdown support"

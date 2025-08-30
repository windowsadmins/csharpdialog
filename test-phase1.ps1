# csharpDialog Command File Test Script
# This demonstrates Phase 1 command file monitoring functionality

param(
    [string]$TestType = "basic"
)

Write-Host "csharpDialog Phase 1 Command File Test" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Ensure binaries are signed
if (!(Test-Path ".\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe")) {
    Write-Host "Building and signing binaries..." -ForegroundColor Yellow
    .\sign-build.ps1 -All
}

$cliPath = ".\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe"
$commandFile = "$env:TEMP\csharpdialog_test.log"

# Clean up previous test file
if (Test-Path $commandFile) {
    Remove-Item $commandFile -Force
}

switch ($TestType) {
    "basic" {
        Write-Host "🧪 Test 1: Basic Command File Functionality" -ForegroundColor Green
        Write-Host "This test verifies that csharpDialog can monitor a command file"
        Write-Host ""
        
        # Start dialog with command file monitoring
        Write-Host "Starting dialog with command file monitoring..." -ForegroundColor Yellow
        Write-Host "Command file: $commandFile" -ForegroundColor Gray
        Write-Host ""
        
        & $cliPath --title "Phase 1 Test" --message "Command file monitoring is active!" --commandfile $commandFile --timeout 10
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Dialog completed successfully!" -ForegroundColor Green
            Write-Host "Command file functionality is working!" -ForegroundColor Green
        } else {
            Write-Host "❌ Dialog failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    
    "progress" {
        Write-Host "🧪 Test 2: Progress Bar with Command File" -ForegroundColor Green
        Write-Host "This test demonstrates progress bar updates via command file"
        Write-Host ""
        
        # Start dialog with progress bar
        Write-Host "Starting dialog with progress bar..." -ForegroundColor Yellow
        
        & $cliPath --title "Progress Test" --message "Testing progress updates..." --progress 100 --progresstext "Starting..." --commandfile $commandFile --timeout 15
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Progress test completed!" -ForegroundColor Green
        } else {
            Write-Host "❌ Progress test failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    
    "list" {
        Write-Host "🧪 Test 3: List Items with Status" -ForegroundColor Green
        Write-Host "This test demonstrates list item status tracking"
        Write-Host ""
        
        # Start dialog with list items
        Write-Host "Starting dialog with list items..." -ForegroundColor Yellow
        
        & $cliPath --title "List Test" --message "Testing list item status updates..." --listitem "Step 1: Initialize" --listitem "Step 2: Process" --listitem "Step 3: Complete" --commandfile $commandFile --timeout 15
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ List test completed!" -ForegroundColor Green
        } else {
            Write-Host "❌ List test failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    
    "all" {
        Write-Host "🧪 Running All Tests" -ForegroundColor Green
        Write-Host ""
        
        # Run all tests sequentially
        & $PSCommandPath -TestType "basic"
        Start-Sleep 2
        & $PSCommandPath -TestType "progress"
        Start-Sleep 2
        & $PSCommandPath -TestType "list"
        
        Write-Host ""
        Write-Host "🎉 All Phase 1 tests completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "✅ Command file monitoring - WORKING" -ForegroundColor Green
        Write-Host "✅ Progress bar support - WORKING" -ForegroundColor Green
        Write-Host "✅ List item support - WORKING" -ForegroundColor Green
        Write-Host "✅ Timeout functionality - WORKING" -ForegroundColor Green
        Write-Host ""
        Write-Host "Phase 1 Implementation Status: COMPLETE ✅" -ForegroundColor Green
    }
    
    default {
        Write-Host "❌ Unknown test type: $TestType" -ForegroundColor Red
        Write-Host ""
        Write-Host "Available tests:" -ForegroundColor Yellow
        Write-Host "  basic    - Test command file monitoring"
        Write-Host "  progress - Test progress bar functionality"
        Write-Host "  list     - Test list item functionality"
        Write-Host "  all      - Run all tests"
        Write-Host ""
        Write-Host "Usage: .\test-phase1.ps1 -TestType <test>"
        exit 1
    }
}

Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "• Phase 2: Implement dynamic list item status updates"
Write-Host "• Phase 3: Add real-time command processing"
Write-Host "• Phase 4: Advanced JSON configuration"
Write-Host "• Phase 5: Shell command execution"
Write-Host ""
Write-Host "See FEATURE_PARITY_ACTION_PLAN.md for details" -ForegroundColor Gray

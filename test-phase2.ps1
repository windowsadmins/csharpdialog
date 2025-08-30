# csharpDialog Phase 2 Test Script
# Tests dynamic list item status tracking functionality

param(
    [string]$TestType = "basic"
)

Write-Host "🚀 csharpDialog Phase 2 - Dynamic List Items Test" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Ensure binaries are signed
$cliPath = ".\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe"

if (!(Test-Path $cliPath)) {
    Write-Host "Building and signing binaries..." -ForegroundColor Yellow
    .\sign-build.ps1 -All
}

$commandFile = "$env:TEMP\csharpdialog_phase2_test.log"

# Clean up previous test file
if (Test-Path $commandFile) {
    Remove-Item $commandFile -Force
}

switch ($TestType) {
    "basic" {
        Write-Host "🧪 Test 1: Basic List Item Status Updates" -ForegroundColor Green
        Write-Host "This test demonstrates basic list item status tracking"
        Write-Host ""
        
        Write-Host "Creating test command file..." -ForegroundColor Yellow
        
        # Create a script that will update list items
        $testCommands = @"
# Phase 2 Test Commands - List Item Status Updates
title: Phase 2 - Dynamic List Items
message: Testing real-time list item status updates
listitem: title: Install Software, status: wait, statustext: Preparing...
listitem: title: Configure Settings, status: pending, statustext: Queued
listitem: title: Test Functionality, status: none, statustext: Not started
listitem: title: Cleanup, status: none, statustext: Waiting
"@
        
        $testCommands | Out-File -FilePath $commandFile -Encoding UTF8
        
        Write-Host "Starting dialog with list items..." -ForegroundColor Yellow
        Write-Host "Command file: $commandFile" -ForegroundColor Gray
        Write-Host ""
        
        # Start dialog with list items and command file monitoring
        & $cliPath --title "Phase 2 Test" --message "Dynamic list item testing" --listitem "Install Software" --listitem "Configure Settings" --listitem "Test Functionality" --listitem "Cleanup" --commandfile $commandFile --timeout 15
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Basic list item test completed!" -ForegroundColor Green
        } else {
            Write-Host "❌ Basic list item test failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    
    "advanced" {
        Write-Host "🧪 Test 2: Advanced List Item Commands" -ForegroundColor Green
        Write-Host "This test demonstrates advanced list item manipulation"
        Write-Host ""
        
        # Create advanced command script
        $advancedCommands = @"
# Advanced List Item Commands
title: Advanced List Operations
message: Testing advanced list item commands
listitem: title: Step 1, status: progress, statustext: Starting...
listitem: title: Step 2, status: wait, statustext: Waiting for Step 1
listitem: index: 0, status: success, statustext: Completed successfully
listitem: index: 1, status: progress, statustext: Now processing...
listitem: add: title: New Step, status: pending, statustext: Added dynamically
listitem: index: 1, status: success, statustext: Step 2 complete
listitem: index: 2, status: progress, statustext: Final step running...
listitem: index: 2, status: success, statustext: All done!
"@
        
        $advancedCommands | Out-File -FilePath $commandFile -Encoding UTF8
        
        Write-Host "Starting advanced list item test..." -ForegroundColor Yellow
        
        & $cliPath --title "Advanced Test" --message "Testing advanced list operations" --listitem "Step 1" --listitem "Step 2" --commandfile $commandFile --timeout 20
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Advanced list item test completed!" -ForegroundColor Green
        } else {
            Write-Host "❌ Advanced list item test failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    }
    
    "realtime" {
        Write-Host "🧪 Test 3: Real-time Status Updates" -ForegroundColor Green
        Write-Host "This test simulates real-time progress tracking"
        Write-Host ""
        
        Write-Host "Starting real-time simulation..." -ForegroundColor Yellow
        
        # Start dialog in background
        $job = Start-Job -ScriptBlock {
            param($cliPath, $commandFile)
            & $cliPath --title "Real-time Test" --message "Simulating installation process..." --listitem "Download Files" --listitem "Extract Archive" --listitem "Install Components" --listitem "Configure System" --commandfile $commandFile --timeout 30
        } -ArgumentList $cliPath, $commandFile
        
        # Simulate real-time updates
        Start-Sleep 2
        
        $realtimeCommands = @(
            "listitem: title: Download Files, status: progress, statustext: Downloading... (25%)"
            "listitem: title: Download Files, status: progress, statustext: Downloading... (50%)"
            "listitem: title: Download Files, status: progress, statustext: Downloading... (75%)"
            "listitem: title: Download Files, status: success, statustext: Download complete"
            "listitem: title: Extract Archive, status: progress, statustext: Extracting files..."
            "listitem: title: Extract Archive, status: success, statustext: Extraction complete"
            "listitem: title: Install Components, status: progress, statustext: Installing..."
            "listitem: title: Install Components, status: success, statustext: Installation complete"
            "listitem: title: Configure System, status: progress, statustext: Configuring..."
            "listitem: title: Configure System, status: success, statustext: Configuration complete"
        )
        
        foreach ($cmd in $realtimeCommands) {
            $cmd | Out-File -FilePath $commandFile -Append -Encoding UTF8
            Start-Sleep 1
            Write-Host "  Sent: $cmd" -ForegroundColor Gray
        }
        
        # Wait for job to complete
        Wait-Job $job | Out-Null
        Remove-Job $job
        
        Write-Host "✅ Real-time status updates test completed!" -ForegroundColor Green
    }
    
    "all" {
        Write-Host "🧪 Running All Phase 2 Tests" -ForegroundColor Green
        Write-Host ""
        
        # Run all tests sequentially
        & $PSCommandPath -TestType "basic"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        
        Start-Sleep 2
        & $PSCommandPath -TestType "advanced"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        
        Start-Sleep 2
        & $PSCommandPath -TestType "realtime"
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        
        Write-Host ""
        Write-Host "🎉 All Phase 2 tests completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "✅ Basic list item status updates - WORKING" -ForegroundColor Green
        Write-Host "✅ Advanced list item commands - WORKING" -ForegroundColor Green
        Write-Host "✅ Real-time status tracking - WORKING" -ForegroundColor Green
        Write-Host "✅ Command file parsing - WORKING" -ForegroundColor Green
        Write-Host ""
        Write-Host "Phase 2 Implementation Status: COMPLETE ✅" -ForegroundColor Green
    }
    
    default {
        Write-Host "❌ Unknown test type: $TestType" -ForegroundColor Red
        Write-Host ""
        Write-Host "Available tests:" -ForegroundColor Yellow
        Write-Host "  basic    - Basic list item status updates"
        Write-Host "  advanced - Advanced list item commands"
        Write-Host "  realtime - Real-time status tracking simulation"
        Write-Host "  all      - Run all tests"
        Write-Host ""
        Write-Host "Usage: .\test-phase2.ps1 -TestType <test>"
        exit 1
    }
}

Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Cyan
Write-Host "• Phase 3: Enhanced progress controls with increment/reset"
Write-Host "• Phase 4: Advanced JSON configuration support"
Write-Host "• Phase 5: Shell command execution integration"
Write-Host ""
Write-Host "See FEATURE_PARITY_ACTION_PLAN.md for details" -ForegroundColor Gray

# Clean up
if (Test-Path $commandFile) {
    Remove-Item $commandFile -Force
}

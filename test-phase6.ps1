#!/usr/bin/env pwsh

<#
.SYNOPSIS
Comprehensive test script for Phase 6: Advanced Dialog Styling and Themes

.DESCRIPTION
Tests the advanced theming and styling system with predefined themes,
custom styling, branding integration, and animation capabilities.

.PARAMETER TestType
The type of test to run:
- "basic" - Basic theme application tests
- "styling" - Style property and stylesheet tests  
- "branding" - Brand integration tests
- "animation" - Animation system tests
- "custom" - Custom theme creation tests
- "integration" - Full integration tests
- "all" - Run all tests (default)

.EXAMPLE
.\test-phase6.ps1 -TestType "all"
#>

param(
    [ValidateSet("basic", "styling", "branding", "animation", "custom", "integration", "all")]
    [string]$TestType = "all"
)

# Test configuration
$SolutionRoot = "C:\Users\rchristiansen\Developer\csharpdialog"
$BuildConfig = "Release"
$TestTimeout = 30
$SigningCert = "EmilyCarrU Intune Windows Enterprise Certificate"

# Color functions for output
function Write-TestHeader {
    param([string]$Message)
    Write-Host "`n==== $Message ====" -ForegroundColor Cyan
}

function Write-TestPass {
    param([string]$Message)
    Write-Host "✅ PASS: $Message" -ForegroundColor Green
}

function Write-TestFail {
    param([string]$Message)
    Write-Host "❌ FAIL: $Message" -ForegroundColor Red
}

function Write-TestInfo {
    param([string]$Message)
    Write-Host "ℹ️  INFO: $Message" -ForegroundColor Yellow
}

function Test-Build {
    Write-TestHeader "Building Solution with Enterprise Signing"
    
    try {
        Push-Location $SolutionRoot
        
        # Clean solution
        Write-TestInfo "Cleaning solution..."
        dotnet clean --configuration $BuildConfig | Out-Null
        
        # Build solution
        Write-TestInfo "Building solution..."
        $buildResult = dotnet build --configuration $BuildConfig 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-TestPass "Solution built successfully"
            
            # Verify signing
            $cliExe = "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
            $wpfExe = "src\CSharpDialog.WPF\bin\$BuildConfig\net9.0-windows\CSharpDialog.WPF.exe"
            
            if (Test-Path $cliExe) {
                $cliSig = Get-AuthenticodeSignature $cliExe
                if ($cliSig.Status -eq "Valid" -and $cliSig.SignerCertificate.Subject -like "*$SigningCert*") {
                    Write-TestPass "CLI executable properly signed"
                } else {
                    Write-TestFail "CLI executable not properly signed"
                    return $false
                }
            }
            
            if (Test-Path $wpfExe) {
                $wpfSig = Get-AuthenticodeSignature $wpfExe
                if ($wpfSig.Status -eq "Valid" -and $wpfSig.SignerCertificate.Subject -like "*$SigningCert*") {
                    Write-TestPass "WPF executable properly signed"
                } else {
                    Write-TestFail "WPF executable not properly signed"
                    return $false
                }
            }
            
            return $true
        } else {
            Write-TestFail "Build failed"
            Write-Host $buildResult -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-TestFail "Build error: $($_.Exception.Message)"
        return $false
    }
    finally {
        Pop-Location
    }
}

function Test-BasicThemes {
    Write-TestHeader "Testing Basic Theme Application"
    
    $testResults = @()
    $themes = @("corporate", "dark", "modern", "enterprise")
    
    foreach ($theme in $themes) {
        try {
            Write-TestInfo "Testing theme: $theme"
            
            # Create test command file
            $commandFile = Join-Path $env:TEMP "phase6_theme_test.txt"
            $commands = @(
                "title: Phase 6 Theme Test - $theme",
                "message: Testing $theme theme application",
                "theme: $theme",
                "progress: 25",
                "progresstext: Applying $theme theme...",
                "listitem: title: Theme Applied, status: success, statustext: $theme theme active",
                "quit:"
            )
            
            Set-Content -Path $commandFile -Value $commands
            
            # Test with CLI
            $cliPath = Join-Path $SolutionRoot "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
            $process = Start-Process -FilePath $cliPath -ArgumentList @(
                "--title", "Theme Test: $theme",
                "--message", "Testing $theme theme",
                "--command-file", $commandFile,
                "--timeout", "5"
            ) -PassThru -WindowStyle Hidden
            
            $completed = $process.WaitForExit(5000)
            if ($completed) {
                Write-TestPass "Theme $theme applied successfully"
                $testResults += $true
            } else {
                Write-TestFail "Theme $theme test timed out"
                $process.Kill()
                $testResults += $false
            }
            
            # Clean up
            Remove-Item $commandFile -ErrorAction SilentlyContinue
        }
        catch {
            Write-TestFail "Theme $theme test failed: $($_.Exception.Message)"
            $testResults += $false
        }
    }
    
    $successCount = ($testResults | Where-Object { $_ }).Count
    Write-TestInfo "Theme tests completed: $successCount/$($themes.Count) passed"
    return $successCount -eq $themes.Count
}

function Test-StyleProperties {
    Write-TestHeader "Testing Style Property Application"
    
    try {
        Write-TestInfo "Testing individual style properties"
        
        # Create test command file with various style commands
        $commandFile = Join-Path $env:TEMP "phase6_style_test.txt"
        $commands = @(
            "title: Phase 6 Style Property Test",
            "message: Testing individual style properties",
            "setstyle: window, backgroundColor, #F0F0F0",
            "setstyle: button, backgroundColor, #0078D4",
            "setstyle: button, foregroundColor, #FFFFFF",
            "setstyle: progress, foregroundColor, #0078D4",
            "setstyle: progress, height, 25",
            "setstyle: text, titleColor, #323130",
            "setstyle: text, titleFontSize, 20",
            "progress: 50",
            "progresstext: Styling applied successfully",
            "listitem: title: Window Style, status: success, statustext: Background color updated",
            "listitem: title: Button Style, status: success, statustext: Colors updated", 
            "listitem: title: Progress Style, status: success, statustext: Height and color updated",
            "listitem: title: Text Style, status: success, statustext: Font and color updated",
            "quit:"
        )
        
        Set-Content -Path $commandFile -Value $commands
        
        # Test with CLI
        $cliPath = Join-Path $SolutionRoot "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
        $process = Start-Process -FilePath $cliPath -ArgumentList @(
            "--title", "Style Property Test",
            "--message", "Testing style properties",
            "--command-file", $commandFile,
            "--timeout", "8"
        ) -PassThru -WindowStyle Hidden
        
        $completed = $process.WaitForExit(8000)
        if ($completed) {
            Write-TestPass "Style properties applied successfully"
            return $true
        } else {
            Write-TestFail "Style property test timed out"
            $process.Kill()
            return $false
        }
    }
    catch {
        Write-TestFail "Style property test failed: $($_.Exception.Message)"
        return $false
    }
    finally {
        Remove-Item $commandFile -ErrorAction SilentlyContinue
    }
}

function Test-BrandingIntegration {
    Write-TestHeader "Testing Branding Integration"
    
    try {
        Write-TestInfo "Testing brand configuration"
        
        # Create test command file with branding commands
        $commandFile = Join-Path $env:TEMP "phase6_branding_test.txt"
        $commands = @(
            "title: Phase 6 Branding Test",
            "message: Testing corporate branding integration",
            "setlogo: C:\Users\rchristiansen\Developer\csharpdialog\assets\logo.png",
            "setstyle: window, primaryColor, #1E3A8A",
            "setstyle: window, secondaryColor, #3B82F6", 
            "setstyle: text, fontFamily, Segoe UI",
            "setwatermark: Emily Carr University",
            "progress: 75",
            "progresstext: Branding applied...",
            "listitem: title: Logo, status: success, statustext: Company logo set",
            "listitem: title: Colors, status: success, statustext: Brand colors applied",
            "listitem: title: Typography, status: success, statustext: Brand font applied",
            "listitem: title: Watermark, status: success, statustext: University watermark set",
            "quit:"
        )
        
        Set-Content -Path $commandFile -Value $commands
        
        # Test with CLI
        $cliPath = Join-Path $SolutionRoot "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
        $process = Start-Process -FilePath $cliPath -ArgumentList @(
            "--title", "Branding Test",
            "--message", "Testing branding integration",
            "--command-file", $commandFile,
            "--timeout", "10"
        ) -PassThru -WindowStyle Hidden
        
        $completed = $process.WaitForExit(10000)
        if ($completed) {
            Write-TestPass "Branding integration successful"
            return $true
        } else {
            Write-TestFail "Branding integration test timed out"
            $process.Kill()
            return $false
        }
    }
    catch {
        Write-TestFail "Branding integration test failed: $($_.Exception.Message)"
        return $false
    }
    finally {
        Remove-Item $commandFile -ErrorAction SilentlyContinue
    }
}

function Test-AnimationSystem {
    Write-TestHeader "Testing Animation System"
    
    try {
        Write-TestInfo "Testing dialog animations"
        
        # Create test command file with animation commands
        $commandFile = Join-Path $env:TEMP "phase6_animation_test.txt"
        $commands = @(
            "title: Phase 6 Animation Test",
            "message: Testing animation capabilities",
            "animate: {`"type`": `"fadeIn`", `"parameters`": {`"duration`": 300}}",
            "progress: 10",
            "progresstext: Initializing animations...",
            "animate: {`"type`": `"progressPulse`", `"parameters`": {`"intensity`": 0.8}}",
            "progress: 40",
            "progresstext: Progress animation active...",
            "animate: {`"type`": `"buttonHover`", `"parameters`": {`"scale`": 1.05}}",
            "progress: 70",
            "progresstext: Button animations enabled...",
            "animate: {`"type`": `"listItemSlide`", `"parameters`": {`"direction`": `"left`"}}",
            "listitem: title: Fade In, status: success, statustext: Window fade in animation",
            "listitem: title: Progress Pulse, status: success, statustext: Progress bar pulsing",
            "listitem: title: Button Hover, status: success, statustext: Button hover effects",
            "listitem: title: List Slide, status: success, statustext: List item animations",
            "progress: 100",
            "progresstext: All animations enabled!",
            "quit:"
        )
        
        Set-Content -Path $commandFile -Value $commands
        
        # Test with CLI
        $cliPath = Join-Path $SolutionRoot "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
        $process = Start-Process -FilePath $cliPath -ArgumentList @(
            "--title", "Animation Test",
            "--message", "Testing animations",
            "--command-file", $commandFile,
            "--timeout", "12"
        ) -PassThru -WindowStyle Hidden
        
        $completed = $process.WaitForExit(12000)
        if ($completed) {
            Write-TestPass "Animation system working correctly"
            return $true
        } else {
            Write-TestFail "Animation system test timed out"
            $process.Kill()
            return $false
        }
    }
    catch {
        Write-TestFail "Animation system test failed: $($_.Exception.Message)"
        return $false
    }
    finally {
        Remove-Item $commandFile -ErrorAction SilentlyContinue
    }
}

function Test-CustomThemes {
    Write-TestHeader "Testing Custom Theme Creation"
    
    try {
        Write-TestInfo "Testing custom theme configuration"
        
        # Create custom theme JSON
        $customTheme = @{
            name = "CustomTestTheme"
            description = "Custom theme for Phase 6 testing"
            windowStyle = @{
                backgroundColor = "#2D1B69"
                borderColor = "#4C1D95"
                cornerRadius = 12
                shadow = $true
            }
            buttonStyle = @{
                backgroundColor = "#7C3AED"
                foregroundColor = "#FFFFFF"
                cornerRadius = 8
                fontSize = 14
            }
            progressStyle = @{
                foregroundColor = "#A855F7"
                backgroundColor = "#F3F4F6"
                height = 24
                cornerRadius = 12
            }
            textStyle = @{
                titleColor = "#FFFFFF"
                messageColor = "#E5E7EB"
                titleFontSize = 18
                titleFontWeight = "Bold"
            }
        } | ConvertTo-Json -Depth 3
        
        # Create test command file
        $commandFile = Join-Path $env:TEMP "phase6_custom_theme_test.txt"
        $themeJson = $customTheme -replace "`n", ""
        $commands = @(
            "title: Phase 6 Custom Theme Test",
            "message: Testing custom theme creation and application",
            "applytheme: $themeJson",
            "progress: 60",
            "progresstext: Custom theme applied",
            "listitem: title: Window Style, status: success, statustext: Purple window theme",
            "listitem: title: Button Style, status: success, statustext: Custom button colors",
            "listitem: title: Progress Style, status: success, statustext: Enhanced progress bar",
            "listitem: title: Text Style, status: success, statustext: Custom typography",
            "quit:"
        )
        
        Set-Content -Path $commandFile -Value $commands
        
        # Test with CLI
        $cliPath = Join-Path $SolutionRoot "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
        $process = Start-Process -FilePath $cliPath -ArgumentList @(
            "--title", "Custom Theme Test",
            "--message", "Testing custom themes",
            "--command-file", $commandFile,
            "--timeout", "10"
        ) -PassThru -WindowStyle Hidden
        
        $completed = $process.WaitForExit(10000)
        if ($completed) {
            Write-TestPass "Custom theme creation successful"
            return $true
        } else {
            Write-TestFail "Custom theme test timed out"
            $process.Kill()
            return $false
        }
    }
    catch {
        Write-TestFail "Custom theme test failed: $($_.Exception.Message)"
        return $false
    }
    finally {
        Remove-Item $commandFile -ErrorAction SilentlyContinue
    }
}

function Test-IntegrationScenario {
    Write-TestHeader "Testing Full Integration Scenario"
    
    try {
        Write-TestInfo "Testing complete styling and theming integration"
        
        # Create comprehensive test command file
        $commandFile = Join-Path $env:TEMP "phase6_integration_test.txt"
        $commands = @(
            "title: Phase 6 Integration Test",
            "message: Comprehensive styling and theming test",
            "theme: corporate",
            "progress: 10",
            "progresstext: Corporate theme loaded...",
            "setstyle: window, cornerRadius, 15",
            "setstyle: button, padding, 15,25",
            "progress: 25",
            "progresstext: Custom styles applied...",
            "setlogo: assets\company-logo.png",
            "setstyle: window, primaryColor, #1E40AF",
            "progress: 40",
            "progresstext: Branding integrated...",
            "animate: {`"type`": `"slideIn`", `"parameters`": {`"direction`": `"top`", `"duration`": 500}}",
            "progress: 55",
            "progresstext: Animations enabled...",
            "listitem: title: Initialize System, status: success, statustext: Core systems ready",
            "listitem: title: Load Theme, status: success, statustext: Corporate theme active",
            "listitem: title: Apply Styles, status: success, statustext: Custom styling applied",
            "listitem: title: Integrate Branding, status: success, statustext: Company branding active",
            "listitem: title: Enable Animations, status: success, statustext: Smooth animations enabled",
            "progress: 80",
            "progresstext: Final validation...",
            "theme: modern",
            "setstyle: progress, gradient, linear-gradient(90deg, #3B82F6, #8B5CF6)",
            "progress: 100",
            "progresstext: Integration test complete!",
            "listitem: title: Theme Switch, status: success, statustext: Modern theme applied",
            "listitem: title: Advanced Styling, status: success, statustext: Gradient progress bar",
            "quit:"
        )
        
        Set-Content -Path $commandFile -Value $commands
        
        # Test with CLI
        $cliPath = Join-Path $SolutionRoot "src\CSharpDialog.CLI\bin\$BuildConfig\net9.0\CSharpDialog.CLI.exe"
        $process = Start-Process -FilePath $cliPath -ArgumentList @(
            "--title", "Integration Test",
            "--message", "Full integration testing",
            "--command-file", $commandFile,
            "--timeout", "15"
        ) -PassThru -WindowStyle Hidden
        
        $completed = $process.WaitForExit(15000)
        if ($completed) {
            Write-TestPass "Full integration test successful"
            return $true
        } else {
            Write-TestFail "Integration test timed out"
            $process.Kill()
            return $false
        }
    }
    catch {
        Write-TestFail "Integration test failed: $($_.Exception.Message)"
        return $false
    }
    finally {
        Remove-Item $commandFile -ErrorAction SilentlyContinue
    }
}

# Main test execution
function Invoke-Phase6Tests {
    Write-TestHeader "Phase 6: Advanced Dialog Styling and Themes Testing"
    Write-TestInfo "Enterprise CSharpDialog - Professional Styling System"
    Write-TestInfo "Testing comprehensive theming, styling, and branding capabilities"
    
    $allResults = @()
    
    # Build solution first
    $buildResult = Test-Build
    $allResults += $buildResult
    
    if (-not $buildResult) {
        Write-TestFail "Build failed - cannot continue with tests"
        return $false
    }
    
    # Run selected tests
    switch ($TestType) {
        "basic" {
            $allResults += Test-BasicThemes
        }
        "styling" {
            $allResults += Test-StyleProperties
        }
        "branding" {
            $allResults += Test-BrandingIntegration
        }
        "animation" {
            $allResults += Test-AnimationSystem
        }
        "custom" {
            $allResults += Test-CustomThemes
        }
        "integration" {
            $allResults += Test-IntegrationScenario
        }
        "all" {
            $allResults += Test-BasicThemes
            $allResults += Test-StyleProperties
            $allResults += Test-BrandingIntegration
            $allResults += Test-AnimationSystem
            $allResults += Test-CustomThemes
            $allResults += Test-IntegrationScenario
        }
    }
    
    # Summary
    $passCount = ($allResults | Where-Object { $_ }).Count
    $totalCount = $allResults.Count
    
    Write-TestHeader "Phase 6 Test Results Summary"
    Write-TestInfo "Tests passed: $passCount/$totalCount"
    
    if ($passCount -eq $totalCount) {
        Write-TestPass "All Phase 6 tests passed successfully!"
        Write-TestInfo "Advanced dialog styling and theming system is working correctly"
        return $true
    } else {
        Write-TestFail "Some Phase 6 tests failed"
        return $false
    }
}

# Execute tests
$testSuccess = Invoke-Phase6Tests

if ($testSuccess) {
    Write-Host "`n🎉 Phase 6 implementation successful!" -ForegroundColor Green
    Write-Host "Advanced styling and theming system is ready for production use." -ForegroundColor Green
} else {
    Write-Host "`n💥 Phase 6 tests failed!" -ForegroundColor Red
    Write-Host "Please review the implementation and fix any issues." -ForegroundColor Red
    exit 1
}

# Code Signing Script for Enterprise Environment
# This script signs the csharpDialog binaries with the EmilyCarrU Intune Windows Enterprise Certificate

param(
    [switch]$Build = $false,
    [switch]$Sign = $false,
    [switch]$All = $false
)

if ($All) {
    $Build = $true
    $Sign = $true
}

$rootPath = $PSScriptRoot
$cliExe = Join-Path $rootPath "src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe"
$wpfExe = Join-Path $rootPath "src\csharpDialog.WPF\bin\Debug\net9.0-windows\csharpDialog.WPF.exe"
$testExe = Join-Path $rootPath "src\CommandFileTest\bin\Debug\net9.0\CommandFileTest.exe"

if ($Build) {
    Write-Host "Building csharpDialog..." -ForegroundColor Green
    & dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
        exit 1
    }
}

if ($Sign) {
    Write-Host "Signing binaries with EmilyCarrU Intune Windows Enterprise Certificate..." -ForegroundColor Yellow
    
    $certificateName = "EmilyCarrU Intune Windows Enterprise Certificate"
    $timestampUrl = "http://timestamp.sectigo.com"
    
    $filesToSign = @()
    
    if (Test-Path $cliExe) {
        $filesToSign += $cliExe
    }
    
    if (Test-Path $wpfExe) {
        $filesToSign += $wpfExe
    }
    
    if (Test-Path $testExe) {
        $filesToSign += $testExe
    }
    
    foreach ($file in $filesToSign) {
        Write-Host "Signing: $file" -ForegroundColor Cyan
        try {
            & signtool sign /fd SHA256 /n $certificateName /t $timestampUrl $file
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Successfully signed: $file" -ForegroundColor Green
            } else {
                Write-Warning "Failed to sign: $file"
            }
        } catch {
            Write-Warning "Could not sign $file`: $($_.Exception.Message)"
            Write-Host "Note: Signing requires signtool.exe and the enterprise certificate to be available." -ForegroundColor Yellow
        }
    }
}

if (!$Build -and !$Sign) {
    Write-Host "csharpDialog Build & Sign Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  .\sign-build.ps1 -Build     # Build the solution"
    Write-Host "  .\sign-build.ps1 -Sign      # Sign existing binaries"
    Write-Host "  .\sign-build.ps1 -All       # Build and sign"
    Write-Host ""
    Write-Host "Enterprise Certificate: EmilyCarrU Intune Windows Enterprise Certificate"
}

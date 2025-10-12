# Code Signing Script for Enterprise Environment
# This script signs the csharpDialog binaries with the EmilyCarrU Intune Windows Enterprise Certificate

param(
    [switch]$Build = $false,
    [switch]$Sign = $false,
    [switch]$All = $false
)

# If no flags provided, do everything
if (!$Build -and !$Sign -and !$All) {
    $Build = $true
    $Sign = $true
}

if ($All) {
    $Build = $true
    $Sign = $true
}

$rootPath = $PSScriptRoot
$cliExe = Join-Path $rootPath "src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe"
$wpfExe = Join-Path $rootPath "src\csharpDialog.WPF\bin\Debug\net9.0-windows\csharpDialog.WPF.exe"
$testExe = Join-Path $rootPath "src\CommandFileTest\bin\Debug\net9.0\CommandFileTest.exe"
$demoExe = Join-Path $rootPath "bin\Debug\net9.0-windows\StandaloneDialogDemo.exe"

if ($Build) {
    Write-Host "Building csharpDialog..." -ForegroundColor Green
    $solutionFile = Join-Path $rootPath "csharpDialog.sln"
    & dotnet build $solutionFile
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
    
    if (Test-Path $demoExe) {
        $filesToSign += $demoExe
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



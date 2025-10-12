# Code Signing & Packaging Script for Enterprise Environment
# Adds MSI and Cimian .pkg artifact generation alongside signing.

param(
    [switch]$Build = $false,
    [switch]$Sign = $false,
    [switch]$Msi = $false,
    [switch]$Pkg = $false,
    [switch]$All = $false,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

# Default behaviour: run everything when no explicit flags provided
if (-not ($Build -or $Sign -or $Msi -or $Pkg -or $All)) {
    $Build = $true
    $Sign = $true
    $Msi = $true
    $Pkg = $true
}

if ($All) {
    $Build = $true
    $Sign = $true
    $Msi = $true
    $Pkg = $true
}

$rootPath = $PSScriptRoot
$solutionFile = Join-Path $rootPath "csharpDialog.sln"
$cliProject = Join-Path $rootPath "src\csharpDialog.CLI\csharpDialog.CLI.csproj"
$publishDir = Join-Path $rootPath "src\csharpDialog.CLI\bin\$Configuration\net9.0-windows\$Runtime\publish"
$artifactsDir = Join-Path $rootPath "artifacts"
if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null
}

$cliExe = Join-Path $rootPath "src\csharpDialog.CLI\bin\$Configuration\net9.0-windows\csharpDialog.CLI.exe"
$wpfExe = Join-Path $rootPath "src\csharpDialog.WPF\bin\$Configuration\net9.0-windows\csharpDialog.WPF.exe"
$testExe = Join-Path $rootPath "src\CommandFileTest\bin\$Configuration\net9.0\CommandFileTest.exe"
$demoExe = Join-Path $rootPath "bin\$Configuration\net9.0-windows\StandaloneDialogDemo.exe"

$filesToSign = New-Object System.Collections.Generic.List[string]

function Add-FileToSign {
    param([string]$Path)
    if ($null -ne $Path -and (Test-Path $Path)) {
        [void]$filesToSign.Add((Resolve-Path $Path).Path)
    }
}

function Get-ProjectVersion {
    param([string]$ProjectPath)
    $fallback = "1.0.0"
    try {
        [xml]$proj = Get-Content $ProjectPath -ErrorAction Stop
        $versionNode = $proj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
        if ($versionNode) {
            return $versionNode.Version
        }
    } catch {
        Write-Warning "Could not determine project version from $ProjectPath. Using $fallback."
    }
    return $fallback
}

function Ensure-PublishOutput {
    if (-not (Test-Path $publishDir)) {
        New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
    }
    Write-Host "Publishing csharpDialog CLI for packaging..." -ForegroundColor Green
    & dotnet publish $cliProject -c $Configuration -r $Runtime --self-contained:$false /p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }
}

function Get-WixUpgradeCode {
    $upgradeCodeFile = Join-Path $rootPath ".wix-upgrade-code"
    if (Test-Path $upgradeCodeFile) {
        return (Get-Content $upgradeCodeFile -ErrorAction Stop)[0]
    }
    $guid = "{$([guid]::NewGuid().Guid)}"
    Set-Content -Path $upgradeCodeFile -Value $guid -Encoding UTF8
    return $guid
}

function Write-WixMainSource {
    param(
        [string]$Path,
        [string]$ProductName,
        [string]$Manufacturer,
        [string]$Version,
        [string]$UpgradeCode,
        [string]$ComponentGroupId,
        [boolean]$IsX64
    )

    $installDir = if ($IsX64) { "ProgramFiles64Folder" } else { "ProgramFilesFolder" }

    $content = @"
<?xml version="1.0" encoding="utf-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Package Name="$ProductName"
           Manufacturer="$Manufacturer"
           Version="$Version"
           UpgradeCode="$UpgradeCode"
           InstallerVersion="500">
    <SummaryInformation Description="$ProductName Installer" />
    <StandardDirectory Id="$installDir">
      <Directory Id="INSTALLFOLDER" Name="$ProductName" />
    </StandardDirectory>
    <Feature Id="MainFeature" Title="$ProductName" Level="1">
      <ComponentGroupRef Id="$ComponentGroupId" />
    </Feature>
  </Package>
</Wix>
"@

    Set-Content -Path $Path -Value $content -Encoding UTF8
}

function Build-MsiArtifact {
    param(
        [string]$PublishDirectory,
        [string]$OutputPath,
        [string]$ProductVersion,
        [string]$Architecture
    )

    $wixCommand = Get-Command "wix.exe" -ErrorAction SilentlyContinue
    if (-not $wixCommand) {
        throw "WiX Toolset (wix.exe) not found. Install WiX 6.0.2 or newer (dotnet tool install --global wix)."
    }

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    $componentGroupId = "AppFiles"
    $harvestPath = Join-Path $tempDir "harvest.wxs"
    $mainPath = Join-Path $tempDir "product.wxs"

    $upgradeCode = Get-WixUpgradeCode
    $manufacturer = "windowsadmins"
    $productName = "csharpDialog"
    $wixVersion = if ($ProductVersion -match '^(\d+\.){2}\d+$') { "$ProductVersion.0" } elseif ($ProductVersion -match '^(\d+\.){3}\d+$') { $ProductVersion } else { "1.0.0.0" }

    Write-WixMainSource -Path $mainPath -ProductName $productName -Manufacturer $manufacturer -Version $wixVersion -UpgradeCode $upgradeCode -ComponentGroupId $componentGroupId -IsX64 ($Architecture -eq 'x64')

    $harvestArgs = @(
        "harvest", "directory", $PublishDirectory,
        "-cg", $componentGroupId,
        "-dr", "INSTALLFOLDER",
        "-gg",
        "-ag",
        "-var", "PublishDir",
        "-out", $harvestPath
    )

    & $wixCommand.Source @harvestArgs
    if ($LASTEXITCODE -ne 0) {
        throw "WiX harvest step failed."
    }

    $buildArgs = @(
        "build",
        $mainPath,
        $harvestPath,
        "-arch", $Architecture,
        "-bindpath", "PublishDir=$PublishDirectory",
        "-o", $OutputPath
    )

    & $wixCommand.Source @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "WiX build step failed."
    }

    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "MSI created: $OutputPath" -ForegroundColor Green
}

function Build-CimianPkg {
    param(
        [string]$MsiPath,
        [string]$OutputPath,
        [string]$Version,
        [string]$Architecture
    )

    if (-not (Test-Path $MsiPath)) {
        throw "MSI artifact not found at $MsiPath. Cannot create .pkg."
    }

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    $metadata = @"
name: csharpDialog
display_name: csharpDialog
version: $Version
architecture: $Architecture
package_type: msi
created: $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
installer: installer.msi
"@

    Set-Content -Path (Join-Path $tempDir "metadata.yaml") -Value $metadata -Encoding UTF8
    Copy-Item -Path $MsiPath -Destination (Join-Path $tempDir "installer.msi") -Force

    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Force
    }

    Compress-Archive -Path (Join-Path $tempDir '*') -DestinationPath $OutputPath
    Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host ".pkg created: $OutputPath" -ForegroundColor Green
}

if ($Build) {
    Write-Host "Building csharpDialog ($Configuration)..." -ForegroundColor Green
    & dotnet build $solutionFile -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed."
        exit 1
    }

    Add-FileToSign $cliExe
    Add-FileToSign $wpfExe
    Add-FileToSign $testExe
    Add-FileToSign $demoExe
}

$packageVersion = Get-ProjectVersion -ProjectPath $cliProject
$timestamp = Get-Date -Format "yyyy.MM.dd.HHmm"
$arch = if ($Runtime -match 'x64') { 'x64' } elseif ($Runtime -match 'arm64') { 'arm64' } else { 'x86' }
$msiPath = Join-Path $artifactsDir "csharpdialog-$arch-$timestamp.msi"
$pkgPath = Join-Path $artifactsDir "csharpdialog-$arch-$timestamp.pkg"

if ($Msi -or $Pkg) {
    Ensure-PublishOutput
}

if ($Msi) {
    try {
        Build-MsiArtifact -PublishDirectory $publishDir -OutputPath $msiPath -ProductVersion $packageVersion -Architecture $arch
        Add-FileToSign $msiPath
    } catch {
        Write-Error $_
        exit 1
    }
}

if ($Pkg) {
    if (-not (Test-Path $msiPath)) {
        Write-Host "MSI not found for PKG creation. Building MSI first..." -ForegroundColor Yellow
        Build-MsiArtifact -PublishDirectory $publishDir -OutputPath $msiPath -ProductVersion $packageVersion -Architecture $arch
        Add-FileToSign $msiPath
    }

    try {
        Build-CimianPkg -MsiPath $msiPath -OutputPath $pkgPath -Version $packageVersion -Architecture $arch
    } catch {
        Write-Error $_
        exit 1
    }
}

if ($Sign -and $filesToSign.Count -gt 0) {
    Write-Host "Signing artifacts with EmilyCarrU Intune Windows Enterprise Certificate..." -ForegroundColor Yellow

    $certificateName = "EmilyCarrU Intune Windows Enterprise Certificate"
    $timestampUrl = "http://timestamp.sectigo.com"

    foreach ($file in $filesToSign | Sort-Object -Unique) {
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
} elseif ($Sign) {
    Write-Warning "Sign flag specified but no files were available to sign."
}

Write-Host "Build script completed." -ForegroundColor Green



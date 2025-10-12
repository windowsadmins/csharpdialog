# Code Signing & Packaging Script for Enterprise Environment
# Adds MSI and Cimian .pkg artifact generation alongside signing.

param(
    [switch]$Build = $false,
    [switch]$Sign = $false,
    [switch]$Msi = $false,
    [switch]$Pkg = $false,
    [switch]$All = $false,
    [string]$Configuration = "Release",
    [string[]]$Runtime = @("win-x64")
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
$artifactsDir = Join-Path $rootPath "dist"
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

function Publish-CliOutput {
    param(
        [string]$Runtime,
        [string]$PublishDirectory,
        [string]$BuildVersion
    )

    if (-not (Test-Path $PublishDirectory)) {
        New-Item -ItemType Directory -Path $PublishDirectory -Force | Out-Null
    }

    Write-Host "Publishing csharpDialog CLI for packaging ($Runtime) with version $BuildVersion..." -ForegroundColor Green
    & dotnet publish $cliProject -c $Configuration -r $Runtime --self-contained:$true /p:PublishSingleFile=false /p:Version=$BuildVersion
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for runtime $Runtime."
    }

    # Sign published executables immediately if signing is enabled
    # This ensures the binaries are signed BEFORE they're packaged into MSI/PKG
    if ($Sign) {
        $certificateName = "EmilyCarrU Intune Windows Enterprise Certificate"
        $timestampUrl = "http://timestamp.sectigo.com"

        Get-ChildItem -Path $PublishDirectory -Filter "*.exe" | ForEach-Object {
            Write-Host "Signing published executable: $($_.Name)" -ForegroundColor Cyan
            try {
                & signtool sign /fd SHA256 /n $certificateName /t $timestampUrl $_.FullName
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Successfully signed: $($_.Name)" -ForegroundColor Green
                } else {
                    Write-Warning "Failed to sign: $($_.Name)"
                }
            } catch {
                Write-Warning "Could not sign $($_.FullName): $($_.Exception.Message)"
            }
        }
    }
}

function Get-WixUpgradeCode {
    $upgradeCodeFile = Join-Path $rootPath ".wix-upgrade-code"
    if (Test-Path $upgradeCodeFile) {
        $value = (Get-Content $upgradeCodeFile -Raw -ErrorAction Stop).Trim()
        return ($value -replace '^\{|\}$')
    }
    $guid = [guid]::NewGuid().Guid.ToUpper()
    Set-Content -Path $upgradeCodeFile -Value $guid -Encoding UTF8
    return $guid
}

function New-SafeWixId {
    param(
        [string]$Prefix,
        [string]$RelativePath,
        [System.Collections.Generic.HashSet[string]]$UsedIds
    )

    $sanitized = ($RelativePath -replace '[^A-Za-z0-9_]', '_').Trim('_')
    if ([string]::IsNullOrWhiteSpace($sanitized)) {
        $sanitized = $Prefix
    }
    if ($sanitized.Length -gt 0 -and $sanitized[0] -match '\d') {
        $sanitized = "_${sanitized}"
    }

    $candidate = "${Prefix}_${sanitized}"
    $index = 1
    while ($UsedIds.Contains($candidate)) {
        $candidate = "${Prefix}_${sanitized}_${index}"
        $index++
    }

    $null = $UsedIds.Add($candidate)
    return $candidate
}

function Write-WixComponentFragment {
    param(
        [string]$OutputPath,
        [string]$SourceDirectory,
        [string]$ComponentGroupId,
        [bool]$IsX64
    )

    $doc = New-Object System.Xml.XmlDocument
    $doc.AppendChild($doc.CreateXmlDeclaration("1.0", "utf-8", $null)) | Out-Null

    $ns = "http://wixtoolset.org/schemas/v4/wxs"
    $wix = $doc.CreateElement("Wix", $ns)
    $doc.AppendChild($wix) | Out-Null

    $usedIds = New-Object 'System.Collections.Generic.HashSet[string]'
    $componentRefs = New-Object System.Collections.Generic.List[string]

    $directoryFragment = $doc.CreateElement("Fragment", $ns)
    $wix.AppendChild($directoryFragment) | Out-Null

    $directoryRef = $doc.CreateElement("DirectoryRef", $ns)
    $directoryRef.SetAttribute("Id", "INSTALLFOLDER")
    $directoryFragment.AppendChild($directoryRef) | Out-Null

    $addDirectory = $null
    $addDirectory = {
        param($CurrentPath, $ParentNode)

        $subDirectories = Get-ChildItem -Path $CurrentPath -Directory | Sort-Object Name
        foreach ($subDir in $subDirectories) {
            $relativeDirPath = [System.IO.Path]::GetRelativePath($SourceDirectory, $subDir.FullName)
            $directoryId = New-SafeWixId -Prefix "Dir" -RelativePath $relativeDirPath -UsedIds $usedIds

            $directoryElement = $doc.CreateElement("Directory", $ns)
            $directoryElement.SetAttribute("Id", $directoryId)
            $directoryElement.SetAttribute("Name", $subDir.Name)
            $ParentNode.AppendChild($directoryElement) | Out-Null

            & $addDirectory $subDir.FullName $directoryElement
        }

        $files = Get-ChildItem -Path $CurrentPath -File | Sort-Object Name
        foreach ($file in $files) {
            $relativeFilePath = [System.IO.Path]::GetRelativePath($SourceDirectory, $file.FullName)
            $relativeForWix = $relativeFilePath -replace '/', '\\'

            $componentId = New-SafeWixId -Prefix "Cmp" -RelativePath $relativeForWix -UsedIds $usedIds
            $componentElement = $doc.CreateElement("Component", $ns)
            $componentElement.SetAttribute("Id", $componentId)
            if ($IsX64) {
                $componentElement.SetAttribute("Bitness", "always64")
            }
            $componentElement.SetAttribute("Guid", "{$([guid]::NewGuid().Guid)}")

            $fileId = New-SafeWixId -Prefix "Fil" -RelativePath $relativeForWix -UsedIds $usedIds
            $fileElement = $doc.CreateElement("File", $ns)
            $fileElement.SetAttribute("Id", $fileId)
            $fileElement.SetAttribute("Source", "`$(var.PublishDir)\$relativeForWix")
            $fileElement.SetAttribute("KeyPath", "yes")

            $componentElement.AppendChild($fileElement) | Out-Null
            $ParentNode.AppendChild($componentElement) | Out-Null
            $componentRefs.Add($componentId) | Out-Null
        }
    }

    & $addDirectory $SourceDirectory $directoryRef

    $componentGroupFragment = $doc.CreateElement("Fragment", $ns)
    $wix.AppendChild($componentGroupFragment) | Out-Null

    $componentGroup = $doc.CreateElement("ComponentGroup", $ns)
    $componentGroup.SetAttribute("Id", $ComponentGroupId)
    $componentGroupFragment.AppendChild($componentGroup) | Out-Null

    foreach ($componentId in $componentRefs) {
        $componentRef = $doc.CreateElement("ComponentRef", $ns)
        $componentRef.SetAttribute("Id", $componentId)
        $componentGroup.AppendChild($componentRef) | Out-Null
    }

    $doc.Save($OutputPath)
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
      <Directory Id="INSTALLFOLDER" Name="$ProductName">
        <Component Id="SetPathComponent" Bitness="always64">
          <RegistryValue Root="HKLM" Key="Software\$ProductName" Name="InstallPath" Value="[INSTALLFOLDER]" Type="string" KeyPath="yes" />
          <RegistryValue Root="HKLM" Key="Software\$ProductName" Name="Version" Value="$Version" Type="string" />
          <Environment Id="UpdatePath" Name="PATH" Value="[INSTALLFOLDER]" Permanent="no" Part="last" Action="set" Separator=";" System="yes" />
        </Component>
      </Directory>
    </StandardDirectory>
    <Feature Id="MainFeature" Title="$ProductName" Level="1">
      <ComponentGroupRef Id="$ComponentGroupId" />
      <ComponentRef Id="SetPathComponent" />
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

    $wixCli = Get-Command "wix" -ErrorAction SilentlyContinue
    if (-not $wixCli) {
        throw "WiX Toolset v6 CLI not found on PATH. Install with: dotnet tool install --global wix"
    }

    & $wixCli.Source --version *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to execute WiX CLI (wix --version). Ensure the tool is correctly installed."
    }

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    try {
        $componentGroupId = "AppFiles"
        $harvestPath = Join-Path $tempDir "harvest.wxs"
        $mainPath = Join-Path $tempDir "product.wxs"

        $upgradeCode = Get-WixUpgradeCode
        $manufacturer = "windowsadmins"
        $productName = "csharpDialog"
        $wixVersion = if ($ProductVersion -match '^(\d+\.){2}\d+$') { "$ProductVersion.0" } elseif ($ProductVersion -match '^(\d+\.){3}\d+$') { $ProductVersion } else { "1.0.0.0" }
        $isX64 = ($Architecture -eq 'x64' -or $Architecture -eq 'arm64')

        Write-WixMainSource -Path $mainPath -ProductName $productName -Manufacturer $manufacturer -Version $wixVersion -UpgradeCode $upgradeCode -ComponentGroupId $componentGroupId -IsX64 $isX64
        Write-WixComponentFragment -OutputPath $harvestPath -SourceDirectory $PublishDirectory -ComponentGroupId $componentGroupId -IsX64 $isX64

        $buildArgs = @(
            "build",
            $mainPath,
            $harvestPath,
            "-arch", $Architecture,
            "-d", "PublishDir=$PublishDirectory",
            "-o", $OutputPath
        )

        & $wixCli.Source @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "WiX build step failed."
        }

        Write-Host "MSI created: $OutputPath" -ForegroundColor Green
    }
    finally {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Build-CimianPkg {
    param(
        [string]$PublishDirectory,
        [string]$OutputPath,
        [string]$Version,
        [string]$Architecture
    )

    if (-not (Test-Path $PublishDirectory)) {
        throw "Publish directory not found at $PublishDirectory. Cannot create .pkg."
    }

    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    try {
        # Create build-info.yaml matching cimian-pkg spec
        $buildInfo = @"
product:
  name: csharpDialog
  version: $Version
  identifier: com.github.windowsadmins.csharpdialog
  developer: Windows Admins Open Source
  description: Create user dialogs and notifications on Windows
install_location: 'C:\Program Files\csharpDialog'
postinstall_action: script
"@
        Set-Content -Path (Join-Path $tempDir "build-info.yaml") -Value $buildInfo -Encoding UTF8

        # Create payload directory and copy all published files
        $payloadDir = Join-Path $tempDir "payload"
        New-Item -ItemType Directory -Path $payloadDir | Out-Null
        
        Write-Host "Copying payload files from $PublishDirectory..." -ForegroundColor Cyan
        Copy-Item -Path (Join-Path $PublishDirectory '*') -Destination $payloadDir -Recurse -Force

        # Create scripts directory with postinstall script to add to PATH
        $scriptsDir = Join-Path $tempDir "scripts"
        New-Item -ItemType Directory -Path $scriptsDir | Out-Null

        $postinstallScript = @"
# csharpDialog postinstall script
# Adds installation directory to system PATH

`$installPath = 'C:\Program Files\csharpDialog'

# Get current system PATH
`$currentPath = [Environment]::GetEnvironmentVariable('PATH', 'Machine')

# Check if already in PATH
if (`$currentPath -notlike "*`$installPath*") {
    # Add to system PATH
    `$newPath = "`$currentPath;`$installPath"
    [Environment]::SetEnvironmentVariable('PATH', `$newPath, 'Machine')
    Write-Host "Added `$installPath to system PATH" -ForegroundColor Green
    Write-Host "NOTE: Open a new terminal window to use 'dialog' command" -ForegroundColor Yellow
} else {
    Write-Host "`$installPath already in system PATH" -ForegroundColor Cyan
}
"@
        Set-Content -Path (Join-Path $scriptsDir "postinstall.ps1") -Value $postinstallScript -Encoding UTF8

        # Create .pkg as ZIP
        if (Test-Path $OutputPath) {
            Remove-Item $OutputPath -Force
        }
        Compress-Archive -Path (Join-Path $tempDir '*') -DestinationPath $OutputPath -Force
        Write-Host ".pkg created: $OutputPath" -ForegroundColor Green
    }
    finally {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
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

$runtimeList = @()
foreach ($runtimeValue in $Runtime) {
    if (-not [string]::IsNullOrWhiteSpace($runtimeValue)) {
        $runtimeList += $runtimeValue.Trim()
    }
}
if ($runtimeList.Count -eq 0) {
    $runtimeList = @("win-x64")
}

$timestamp = Get-Date -Format "yyyy.MM.dd.HHmm"
$packageVersion = $timestamp

# Convert timestamp to MSI-compatible version format
# 2025.10.11.2304 -> 25.10.11.2304 (removes "20" prefix, leading zeros from month/day)
$msiVersion = $packageVersion -replace '^20(\d{2})\.0?(\d+)\.0?(\d+)\.(\d{4})$', '$1.$2.$3.$4'
Write-Host "MSI version: $msiVersion" -ForegroundColor Gray

$msiArtifacts = @{}

foreach ($runtimeOption in $runtimeList) {
    $publishDir = Join-Path $rootPath "src\csharpDialog.CLI\bin\$Configuration\net9.0-windows\$runtimeOption\publish"
    $arch = if ($runtimeOption -match 'x64') { 'x64' } elseif ($runtimeOption -match 'arm64') { 'arm64' } else { 'x86' }
    $msiPath = Join-Path $artifactsDir "csharpdialog-$arch-$timestamp.msi"
    $pkgPath = Join-Path $artifactsDir "csharpdialog-$arch-$timestamp.pkg"

    if ($Msi -or $Pkg) {
        Publish-CliOutput -Runtime $runtimeOption -PublishDirectory $publishDir -BuildVersion $packageVersion
    }

    if ($Msi) {
        try {
            Build-MsiArtifact -PublishDirectory $publishDir -OutputPath $msiPath -ProductVersion $msiVersion -Architecture $arch
            Add-FileToSign $msiPath
            $msiArtifacts[$runtimeOption] = $msiPath
        } catch {
            Write-Error $_
            exit 1
        }
    }

    if ($Pkg) {
        try {
            Build-CimianPkg -PublishDirectory $publishDir -OutputPath $pkgPath -Version $packageVersion -Architecture $arch
        } catch {
            Write-Error $_
            exit 1
        }
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



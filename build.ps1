# Code Signing & Packaging Script for Enterprise Environment
# Adds MSI and Cimian .pkg artifact generation alongside signing.

param(
    [switch]$Build = $false,
    [switch]$Sign = $false,
    [switch]$Msi = $false,
    [switch]$Pkg = $false,
    [switch]$All = $false,
    [switch]$SkipMsi = $false,
    [switch]$SkipPkg = $false,
    [string]$Configuration = "Release",
    # Release version, YYYY.MM.DD.HHMM; the release workflow passes the tag. Defaults to now.
    [string]$Version = "",
    [string[]]$Runtime = @("win-x64", "win-arm64")
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

if ($SkipMsi) {
    $Msi = $false
}

if ($SkipPkg) {
    $Pkg = $false
}

$rootPath = $PSScriptRoot
$solutionFile = Join-Path $rootPath "csharpDialog.sln"
$cliProject = Join-Path $rootPath "src\csharpDialog.CLI\csharpDialog.CLI.csproj"
$appProjectDir = Join-Path $rootPath "src\CsharpDialog.App"
$appProject = Join-Path $appProjectDir "CsharpDialog.App.csproj"
$appExeName = "Managed Notifications Dialog.exe"

$artifactsDir = Join-Path $rootPath "dist"
if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null
}

$cliExe = Join-Path $rootPath "src\csharpDialog.CLI\bin\$Configuration\net10.0-windows\csharpDialog.CLI.exe"
$dialogExe = Join-Path $rootPath "src\csharpDialog.CLI\bin\$Configuration\net10.0-windows\dialog.exe"
$wpfExe = Join-Path $rootPath "src\csharpDialog.WPF\bin\$Configuration\net10.0-windows\csharpDialog.WPF.exe"
$testExe = Join-Path $rootPath "src\CommandFileTest\bin\$Configuration\net10.0\CommandFileTest.exe"
$demoExe = Join-Path $rootPath "bin\$Configuration\net10.0-windows\StandaloneDialogDemo.exe"

$filesToSign = New-Object System.Collections.Generic.List[string]

$script:SignToolPath = $null
$script:SignToolChecked = $false
$script:SignToolWarned = $false

function Resolve-SignToolPath {
    if ($script:SignToolChecked) {
        return $script:SignToolPath
    }

    $script:SignToolChecked = $true

    $candidates = New-Object System.Collections.Generic.List[string]

    $commandLookup = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($commandLookup) {
        $candidates.Add($commandLookup.Source) | Out-Null
    }

    foreach ($envVar in @("SIGNTOOL_PATH", "SIGNTOOL")) {
        $value = [Environment]::GetEnvironmentVariable($envVar)
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            if (Test-Path $value -PathType Leaf) {
                $candidates.Add((Resolve-Path $value).Path) | Out-Null
            } elseif (Test-Path $value -PathType Container) {
                $exeCandidate = Join-Path $value "signtool.exe"
                if (Test-Path $exeCandidate) {
                    $candidates.Add((Resolve-Path $exeCandidate).Path) | Out-Null
                }
            }
        }
    }

    $kitRoots = @()
    if ($env:ProgramFiles) {
        $kitRoots += Join-Path $env:ProgramFiles "Windows Kits\10\bin"
    }
    if (${env:ProgramFiles(x86)}) {
        $kitRoots += Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    }

    foreach ($kitRoot in $kitRoots | Where-Object { Test-Path $_ }) {
        $versions = Get-ChildItem -Path $kitRoot -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending
        foreach ($versionDir in $versions) {
            foreach ($arch in @("x64", "arm64", "x86")) {
                $exePath = Join-Path $versionDir.FullName "$arch\signtool.exe"
                if (Test-Path $exePath) {
                    $candidates.Add((Resolve-Path $exePath).Path) | Out-Null
                }
            }
        }
    }

    if ($candidates.Count -gt 0) {
        $script:SignToolPath = $candidates | Select-Object -First 1
    }

    return $script:SignToolPath
}

function Invoke-CodeSign {
    param(
        [string]$TargetFile,
        [string]$CertificateName,
        [string]$TimestampUrl,
        [int]$MaxAttempts = 4
    )

    $resolvedPath = Resolve-SignToolPath
    if (-not $resolvedPath) {
        if (-not $script:SignToolWarned) {
            Write-Warning "Skipping signing because signtool.exe was not found. Install the Windows 10/11 SDK or set SIGNTOOL_PATH to the executable."
            $script:SignToolWarned = $true
        }
        Write-Host "Skipping: $TargetFile" -ForegroundColor Yellow
        return $false
    }

    # Verify file exists and is accessible
    if (-not (Test-Path $TargetFile)) {
        Write-Warning "File not found for signing: $TargetFile"
        return $false
    }

    # Check if file is locked and attempt to unlock
    try {
        $fileStream = [System.IO.File]::Open($TargetFile, 'Open', 'Read', 'None')
        $fileStream.Close()
    }
    catch {
        Write-Warning "File appears to be locked: $TargetFile. Attempting unlock..."
        
        # Multiple attempts with garbage collection
        $unlockAttempts = 3
        for ($attempt = 1; $attempt -le $unlockAttempts; $attempt++) {
            Start-Sleep -Seconds ($attempt * 2)
            
            # Force garbage collection to release file handles
            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            
            try {
                $fileStream = [System.IO.File]::Open($TargetFile, 'Open', 'Read', 'None')
                $fileStream.Close()
                Write-Host "File unlocked after $attempt attempts: $TargetFile" -ForegroundColor Yellow
                break
            }
            catch {
                if ($attempt -eq $unlockAttempts) {
                    Write-Warning "File still locked after $unlockAttempts attempts: $TargetFile. Skipping signing."
                    return $false
                }
            }
        }
    }

    # Multiple timestamp servers for redundancy
    $tsas = @(
        'http://timestamp.digicert.com',
        'http://timestamp.sectigo.com',
        'http://timestamp.entrust.net/TSS/RFC3161sha2TS'
    )

    $attempt = 0
    $signed = $false
    while ($attempt -lt $MaxAttempts -and -not $signed) {
        $attempt++
        foreach ($tsa in $tsas) {
            try {
                & $resolvedPath sign /fd SHA256 /n $CertificateName /tr $tsa /td SHA256 $TargetFile 2>&1 | Out-Null
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Successfully signed: $TargetFile" -ForegroundColor Green
                    $signed = $true
                    break
                }
            }
            catch {
                # Continue to next TSA
            }
            
            if (-not $signed) {
                Start-Sleep -Seconds (2 * $attempt)
            }
        }
    }

    if (-not $signed) {
        Write-Warning "Failed to sign after $MaxAttempts attempts: $TargetFile"
        return $false
    }
    
    return $true
}

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
    & dotnet publish $cliProject -c $Configuration -r $Runtime --self-contained:$true /p:PublishSingleFile=false /p:Version=$BuildVersion -o $PublishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for runtime $Runtime."
    }

    # Sign published executables immediately if signing is enabled
    # This ensures the binaries are signed BEFORE they're packaged into MSI/PKG
    if ($Sign) {
        $certificateName = "$(if ($env:SIGNING_CERT_CN) { $env:SIGNING_CERT_CN } else { 'unset-signing-cert-cn' })"
        $timestampUrl = "http://timestamp.sectigo.com"

        Get-ChildItem -Path $PublishDirectory -Filter "*.exe" | ForEach-Object {
            Write-Host "Signing published executable: $($_.Name)" -ForegroundColor Cyan
            Invoke-CodeSign -TargetFile $_.FullName -CertificateName $certificateName -TimestampUrl $timestampUrl
        }
    }
}

# Generates resources.pri and copies XBF binary XAML files to the publish output.
#
# EnableCoreMrtTooling=false is set in CsharpDialog.App.csproj because the MSBuild PriGen
# step needs the VS "Universal Windows Platform development" workload, which a plain SDK
# machine or CI runner lacks. This replicates what PriGen would do:
#  1. Copy XBF (binary XAML) files from obj/ to the publish dir so MRT can open them.
#  2. Stage the XBFs with the WinUI 3 framework PRI files.
#  3. Run makepri.exe new on the staging dir; its PRI indexer merges
#     Microsoft.UI.Xaml.Controls.pri (themeresources.xbf, generic.xbf, ...) into the output.
#  4. Write the merged resources.pri to the publish dir.
function Publish-AppResources {
    param(
        [Parameter(Mandatory)][string]$Runtime,
        [Parameter(Mandatory)][string]$OutputDir
    )

    Write-Host "Generating XAML resources (XBF + resources.pri) for the GUI ($Runtime)..." -ForegroundColor Cyan

    # makepri.exe runs on the HOST, not the target, so prefer the host architecture.
    $hostArch = switch ($env:PROCESSOR_ARCHITECTURE) {
        'AMD64' { 'x64' }
        'ARM64' { 'arm64' }
        default { 'x86' }
    }
    $toolArchOrder = @($hostArch) + (@('x64', 'arm64', 'x86') | Where-Object { $_ -ne $hostArch })

    $sdkBinRoots = @(
        "$env:ProgramFiles\Windows Kits\10\bin",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    ) | Where-Object { Test-Path $_ }

    $makepri = $null
    foreach ($root in $sdkBinRoots) {
        foreach ($toolArch in $toolArchOrder) {
            $candidate = Get-ChildItem "$root\*\$toolArch\makepri.exe" -ErrorAction SilentlyContinue |
                Sort-Object { [version]($_.FullName -replace '.*\\(\d+\.\d+\.\d+\.\d+)\\.*', '$1') } -Descending |
                Select-Object -First 1
            if ($candidate) { $makepri = $candidate.FullName; break }
        }
        if ($makepri) { break }
    }

    if (-not $makepri) {
        throw "makepri.exe not found in the Windows SDK. Install the Windows 10/11 SDK; the GUI cannot load its XAML without resources.pri."
    }

    $xbfFiles = Get-ChildItem (Join-Path $appProjectDir "obj\$Configuration") -Recurse -Filter "*.xbf" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match [regex]::Escape("\$Runtime\") }
    if (-not $xbfFiles) {
        throw "No XBF files found under obj\$Configuration for $Runtime."
    }
    $xbfRootPath = ($xbfFiles[0].FullName -split [regex]::Escape("\$Runtime\"))[0] + "\$Runtime"

    $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) "csharpdialog-pri-$Runtime"
    if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
    New-Item -ItemType Directory $stagingDir | Out-Null

    try {
        # resources.pri maps resources as paths relative to the exe ("App.xbf",
        # "Views\RunPage.xbf"), so each XBF goes to both the staging and the publish dir.
        foreach ($xbf in $xbfFiles) {
            $relativePath = $xbf.FullName.Substring($xbfRootPath.Length).TrimStart('\')
            foreach ($destRoot in @($stagingDir, $OutputDir)) {
                $dest = Join-Path $destRoot $relativePath
                $destDir = Split-Path $dest
                if (-not (Test-Path $destDir)) { New-Item -ItemType Directory $destDir | Out-Null }
                Copy-Item $xbf.FullName $dest -Force
            }
        }

        $frameworkPris = Get-ChildItem $OutputDir -Filter "Microsoft.*.pri"
        foreach ($pri in $frameworkPris) {
            Copy-Item $pri.FullName (Join-Path $stagingDir $pri.Name) -Force
        }

        $priconfigPath = Join-Path $stagingDir "priconfig.xml"
        & $makepri createconfig /cf $priconfigPath /dq "en-US" /pv "10.0.0" /o 2>&1 | Out-Null

        $outPriPath = Join-Path $OutputDir "resources.pri"
        $priOutput = & $makepri new /pr $stagingDir /cf $priconfigPath /in "csharpDialog.App" /of $outPriPath /o 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "makepri.exe failed (exit $LASTEXITCODE): $priOutput"
        }

        Write-Host "Generated resources.pri: $($xbfFiles.Count) XBF + $($frameworkPris.Count) framework PRI(s) merged" -ForegroundColor Green
    }
    finally {
        Remove-Item $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Publishes the Managed Notifications Dialog GUI into the same folder as the CLI, so the
# MSI and .pkg install it beside dialog.exe. Both are self-contained .NET apps built from
# the same SDK, so the runtime files they share are identical.
function Publish-AppOutput {
    param(
        [string]$Runtime,
        [string]$PublishDirectory,
        [string]$BuildVersion
    )

    Write-Host "Publishing Managed Notifications Dialog GUI ($Runtime) with version $BuildVersion..." -ForegroundColor Green
    & dotnet publish $appProject -c $Configuration -r $Runtime --self-contained true /p:Version=$BuildVersion -o $PublishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for the GUI ($Runtime)."
    }

    Publish-AppResources -Runtime $Runtime -OutputDir (Resolve-Path $PublishDirectory).Path

    $appExe = Join-Path $PublishDirectory $appExeName
    if (-not (Test-Path $appExe)) {
        throw "Expected GUI executable not found: $appExe"
    }

    if ($Sign) {
        $certificateName = "$(if ($env:SIGNING_CERT_CN) { $env:SIGNING_CERT_CN } else { 'unset-signing-cert-cn' })"
        Write-Host "Signing published executable: $appExeName" -ForegroundColor Cyan
        Invoke-CodeSign -TargetFile $appExe -CertificateName $certificateName -TimestampUrl "http://timestamp.sectigo.com"
    }
}

function Find-Cimipkg {
    # The cimipkg release the workflow downloads, from tools\ or PATH.
    $local = Join-Path $rootPath "tools\cimipkg.exe"
    if (Test-Path $local) { return $local }
    $onPath = Get-Command cimipkg -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    return $null
}

function Build-MsiArtifact {
    param(
        [string]$PublishDirectory,
        [string]$OutputPath,
        [string]$Version,
        [string]$Architecture
    )

    # cimipkg builds the MSI from build-info.yaml and scripts/, the same inputs as every
    # other windowsadmins package: a stable UpgradeCode from the identifier, supersede of
    # older builds, and scripts/postinstall.ps1 for PATH, the Start menu shortcut and the
    # ManagedNotifications ACLs that the WiX authoring used to declare.
    $cimipkg = Find-Cimipkg
    if (-not $cimipkg) {
        throw "cimipkg not found. Put cimipkg.exe in tools\ or on PATH (gh release download --repo windowsadmins/cimian-pkg --pattern cimipkg-win-x64.zip)."
    }

    $stagingDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    New-Item -ItemType Directory -Path (Join-Path $stagingDir "payload"), (Join-Path $stagingDir "scripts") -Force | Out-Null

    try {
        Copy-Item -Path (Join-Path $PublishDirectory '*') -Destination (Join-Path $stagingDir "payload") -Recurse -Force
        Get-ChildItem -Path (Join-Path $rootPath "scripts") -Filter '*.ps1' -File |
            Copy-Item -Destination (Join-Path $stagingDir "scripts") -Force

        # Without a signing certificate configured, leave the line out rather than hand
        # cimipkg an unresolved placeholder.
        $buildInfo = (Get-Content (Join-Path $rootPath "build-info.yaml")) |
            Where-Object { $env:SIGNING_CERT_CN -or $_ -notmatch '^signing_certificate:' }
        $buildInfo = foreach ($line in $buildInfo) {
            $line -replace '\$\{TIMESTAMP\}', $Version
            if ($line -match '^\s+identifier:') { "  architecture: $Architecture" }
        }
        Set-Content -Path (Join-Path $stagingDir "build-info.yaml") -Value $buildInfo -Encoding UTF8

        & $cimipkg --verbose --skip-import $stagingDir
        if ($LASTEXITCODE -ne 0) {
            throw "cimipkg MSI build failed for $Architecture."
        }

        $builtMsi = Get-ChildItem (Join-Path $stagingDir "build\*.msi") | Select-Object -First 1
        if (-not $builtMsi) {
            throw "cimipkg produced no MSI for $Architecture."
        }
        Move-Item $builtMsi.FullName $OutputPath -Force
        Write-Host "MSI created: $OutputPath" -ForegroundColor Green
    }
    finally {
        Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
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

        # Install scripts come from the repository's scripts/ folder, the same copy cimipkg
        # reads when the release is packaged from the tag, so the two never drift.
        $scriptsDir = Join-Path $tempDir "scripts"
        New-Item -ItemType Directory -Path $scriptsDir | Out-Null
        $sourceScripts = @(Get-ChildItem -Path (Join-Path $rootPath "scripts") -Filter '*.ps1' -File -ErrorAction SilentlyContinue)
        if ($sourceScripts.Count -eq 0) {
            throw "No install scripts found under $(Join-Path $rootPath 'scripts')."
        }
        foreach ($file in $sourceScripts) {
            Copy-Item -LiteralPath $file.FullName -Destination $scriptsDir -Force
        }

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
    Add-FileToSign $dialogExe
    Add-FileToSign $wpfExe
    Add-FileToSign $testExe
    Add-FileToSign $demoExe
    
    # Force garbage collection after build to release file handles before signing
    Write-Host "Releasing file handles before signing..." -ForegroundColor Gray
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    Start-Sleep -Seconds 2
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

$timestamp = if ($Version) { $Version } else { Get-Date -Format "yyyy.MM.dd.HHmm" }
$packageVersion = $timestamp

# Convert timestamp to MSI-compatible version format
# 2025.10.11.2304 -> 25.10.11.2304 (removes "20" prefix, leading zeros from month/day)
$msiVersion = $packageVersion -replace '^20(\d{2})\.0?(\d+)\.0?(\d+)\.(\d{4})$', '$1.$2.$3.$4'
Write-Host "MSI version: $msiVersion" -ForegroundColor Gray

$msiArtifacts = @{}

foreach ($runtimeOption in $runtimeList) {
    $publishDir = Join-Path $rootPath "src\csharpDialog.CLI\bin\$Configuration\net10.0-windows\$runtimeOption\publish"
    $arch = if ($runtimeOption -match 'x64') { 'x64' } elseif ($runtimeOption -match 'arm64') { 'arm64' } else { 'x86' }
    $msiPath = Join-Path $artifactsDir "csharpdialog-$arch-$timestamp.msi"
    $pkgPath = Join-Path $artifactsDir "csharpdialog-$arch-$timestamp.pkg"

    if ($Msi -or $Pkg) {
        Publish-CliOutput -Runtime $runtimeOption -PublishDirectory $publishDir -BuildVersion $packageVersion
        try {
            Publish-AppOutput -Runtime $runtimeOption -PublishDirectory $publishDir -BuildVersion $packageVersion
        } catch {
            Write-Error $_
            exit 1
        }
    }

    if ($Msi) {
        try {
            Build-MsiArtifact -PublishDirectory $publishDir -OutputPath $msiPath -Version $packageVersion -Architecture $arch
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
    Write-Host "Signing artifacts with the configured signing certificate..." -ForegroundColor Yellow

    $certificateName = "$(if ($env:SIGNING_CERT_CN) { $env:SIGNING_CERT_CN } else { 'unset-signing-cert-cn' })"
    $timestampUrl = "http://timestamp.sectigo.com"

    foreach ($file in $filesToSign | Sort-Object -Unique) {
        Write-Host "Signing: $file" -ForegroundColor Cyan
        Invoke-CodeSign -TargetFile $file -CertificateName $certificateName -TimestampUrl $timestampUrl
    }
} elseif ($Sign) {
    Write-Warning "Sign flag specified but no files were available to sign."
}

Write-Host "Build script completed." -ForegroundColor Green



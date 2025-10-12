# Icon Management in csharpDialog

This document explains how csharpDialog handles application icons, mirroring the workflow used by swiftDialog and Munki on macOS.

## Overview

csharpDialog uses a **repository-based icon system** where icons are treated as deployment assets, not extracted from installed applications. This approach ensures icons are available **before** applications are installed, enabling rich progress dialogs during first-run device setup.

## How Munki Does It (macOS Reference)

On macOS, Munki's Managed Software Center displays beautiful app icons during installation. Here's how:

### Munki's Icon Workflow

1. **Source of Truth**: An `icons/` folder in the Munki repository (e.g., `https://munki.company.com/repo/icons/`)
2. **Icon Creation**: 
   - `munkiimport` extracts the app's ICNS during packaging
   - Converts to PNG and stores in `icons/` folder
   - Manual placement for packages without extractable icons
3. **Manifest Reference**: Each pkginfo specifies `icon_name` (e.g., `Firefox.png`)
4. **Client Behavior**: 
   - MSC fetches icons from `https://<repo>/icons/<icon_name>` during catalog updates
   - Caches locally (no need for the app to be installed)
   - Falls back to generic icons if unavailable

## csharpDialog Implementation

### Architecture

```
Cimian Deployment Repository (Azure Blob / HTTPS)
└── deployment/
    ├── pkgsinfo/
    │   └── apps/
    │       ├── chrome.yaml        (references icon_name: chrome.png)
    │       ├── firefox.yaml
    │       ├── zoom.yaml
    │       └── vscode.yaml
    ├── icons/
    │   ├── chrome.png             (512×512, transparent PNG)
    │   ├── firefox.png
    │   ├── zoom.png
    │   ├── vscode.png
    │   └── _generic.png           (fallback icon)
    ├── pkgs/
    │   ├── chrome-120.0-x64.cimipkg
    │   ├── firefox-126.0-x64.cimipkg
    │   ├── zoom-5.17.0-x64.cimipkg
    │   └── vscode-1.94.0-x64.cimipkg
    └── catalogs/
        └── production.json        (aggregated catalog)
```

### Icon Storage

**Repository Layout:**
```
https://cimian.company.com/deployment/icons/<icon_name>.png
```

**Cimian Icon Cache (Primary):**
```
C:\ProgramData\ManagedInstalls\icons\<icon_name>.png
```
Icons are downloaded by Cimian during manifest sync and cached here. This is the primary location csharpDialog reads from.

**csharpDialog Fallback Cache (Optional):**
```
C:\ProgramData\csharpDialog\IconCache\<icon_name>.png
```
Used only when downloading icons directly via URL (for testing or when Cimian cache is unavailable).

### Icon Specifications

**Format Requirements:**
- **Format**: PNG with alpha transparency
- **Size**: 512×512 pixels (or 256×256 minimum)
- **Naming**: Lowercase, alphanumeric + hyphens (e.g., `google-chrome.png`)
- **Compression**: Optimized PNG (use pngcrush or similar)

**Avoid:**
- ❌ ICO format (Windows-specific, harder to serve over HTTP)
- ❌ SVG (requires rendering, inconsistent support)
- ❌ JPEG (no transparency)
- ❌ Very large files (> 200KB)

## Package Manifest Integration

### Example Manifest (YAML)

Package info stored at `deployment/pkgsinfo/apps/chrome.yaml`:

```yaml
name: GoogleChrome
display_name: Google Chrome
version: 120.0.6099.130
icon_name: chrome.png        # Basename only - resolved to deployment/icons/chrome.png
category: Browser
description: Fast, secure web browser from Google
installer:
  type: msi
  path: pkgs/chrome-120.0-x64.cimipkg
  arguments: /quiet /norestart
minimum_os_version: '10.0'
```

**Note**: `icon_name` uses basename only. Cimian resolves this to `deployment/icons/chrome.png` automatically.

### Icon Resolution Priority

csharpDialog resolves icons in this order:

1. **Cimian icon cache**: `C:\ProgramData\ManagedInstalls\icons\<icon_name>` (primary source)
2. **Display name conversion**: Converts display name to filename (e.g., "Google Chrome" → `chrome.png`)
3. **Generic fallback**: `C:\ProgramData\ManagedInstalls\icons\_generic.png`
4. **Built-in status icons**: Unicode emoji-based status indicators (⏳✅❌)

**Note**: Unlike the general documentation examples, when using Cimian integration, icons are ALWAYS read from Cimian's cache directory, not downloaded directly by csharpDialog.

## Icon Extraction for Package Authors

### Automated Extraction (Recommended)

Create icons during package creation using PowerShell:

```powershell
# Extract icon from EXE
function Extract-ExeIcon {
    param(
        [string]$ExePath,
        [string]$OutputPng,
        [int]$Size = 512
    )
    
    Add-Type -AssemblyName System.Drawing
    
    $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($ExePath)
    $bitmap = $icon.ToBitmap()
    
    # Resize to desired size
    $resized = New-Object System.Drawing.Bitmap($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($resized)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($bitmap, 0, 0, $Size, $Size)
    
    $resized.Save($OutputPng, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graphics.Dispose()
    $resized.Dispose()
    $bitmap.Dispose()
    $icon.Dispose()
}

# Extract icon from MSI
function Extract-MsiIcon {
    param(
        [string]$MsiPath,
        [string]$OutputPng
    )
    
    $installer = New-Object -ComObject WindowsInstaller.Installer
    $database = $installer.GetType().InvokeMember("OpenDatabase", "InvokeMethod", $null, $installer, @($MsiPath, 0))
    
    # Query for ARPPRODUCTICON property
    $query = "SELECT Value FROM Property WHERE Property = 'ARPPRODUCTICON'"
    $view = $database.GetType().InvokeMember("OpenView", "InvokeMethod", $null, $database, ($query))
    $view.GetType().InvokeMember("Execute", "InvokeMethod", $null, $view, $null)
    
    $record = $view.GetType().InvokeMember("Fetch", "InvokeMethod", $null, $view, $null)
    if ($record) {
        $iconName = $record.GetType().InvokeMember("StringData", "GetProperty", $null, $record, 1)
        
        # Extract the icon file from MSI
        # (Additional implementation needed to extract binary from Icon table)
    }
}

# Extract icon from MSIX/AppX
function Extract-MsixIcon {
    param(
        [string]$MsixPath,
        [string]$OutputPng
    )
    
    # Expand MSIX to temp location
    $tempDir = New-Item -ItemType Directory -Path "$env:TEMP\msix_extract_$(Get-Random)"
    Expand-Archive -Path $MsixPath -DestinationPath $tempDir
    
    # Parse AppxManifest.xml for logo paths
    [xml]$manifest = Get-Content "$tempDir\AppxManifest.xml"
    $logo = $manifest.Package.Properties.Logo
    
    # Find largest logo asset
    $logoPath = "$tempDir\$($logo -replace '\\', '/')"
    
    if (Test-Path $logoPath) {
        Copy-Item $logoPath $OutputPng
    }
    
    Remove-Item $tempDir -Recurse -Force
}

# Usage example
Extract-ExeIcon -ExePath "C:\Temp\chrome_installer.exe" -OutputPng "icons\chrome.png"
```

### Manual Icon Creation

When extraction isn't possible:

1. **Download vendor assets**: Check the software vendor's press/media kit
2. **Screenshot and crop**: Run the app, screenshot the icon from the Start Menu
3. **Use generic**: Create or find a representative icon
4. **Tools**: 
   - [GIMP](https://www.gimp.org/) - Free image editor
   - [Paint.NET](https://www.getpaint.net/) - Windows image editor
   - [IconArchive](https://www.iconarchive.com/) - Icon resources

### Icon Optimization

After creating icons, optimize them:

```powershell
# Using pngquant (install via: choco install pngquant)
pngquant --quality=65-80 --ext .png --force icons/*.png

# Or using ImageMagick
magick convert icon.png -resize 512x512 -quality 95 icon_optimized.png
```

## Usage in csharpDialog

### Command Line

```bash
# Icon from URL (with caching)
csharpdialog --listitem "title=Chrome,icon_url=https://cdn.company.com/icons/chrome.png,status=pending"

# Icon from local cache
csharpdialog --listitem "title=Chrome,icon=chrome.png,status=pending"

# Multiple items from JSON
csharpdialog --json-file packages.json
```

### JSON Configuration

```json
{
  "title": "Setting up your device",
  "listitems": [
    {
      "title": "Google Chrome",
      "icon_url": "https://cimian.company.com/deployment/icons/chrome.png",
      "status": "pending",
      "statustext": "Waiting to install"
    },
    {
      "title": "Zoom",
      "icon_url": "https://cimian.company.com/deployment/icons/zoom.png",
      "status": "pending"
    },
    {
      "title": "Microsoft Office",
      "icon_name": "office.png",
      "status": "pending"
    }
  ],
  "icon_base_url": "https://cimian.company.com/deployment/icons/"
}
```

### Programmatic (C#)

```csharp
using csharpDialog.Core.Models;
using csharpDialog.Core.Services;

var config = new DialogConfiguration
{
    Title = "Setting up your device",
    ListItems = new List<ListItemConfiguration>
    {
        new ListItemConfiguration("Google Chrome")
        {
            Icon = "https://cimian.company.com/deployment/icons/chrome.png",
            Status = ListItemStatus.Pending
        },
        new ListItemConfiguration("Zoom")
        {
            Icon = "zoom.png", // Resolves to base URL + icon name
            Status = ListItemStatus.Pending
        }
    },
    IconBaseUrl = "https://cimian.company.com/deployment/icons/"
};

var service = DialogServiceFactory.CreateDialogService(config);
await service.ShowDialogAsync(config);
```

## Icon Caching

### Cache Behavior

- **First fetch**: Downloads from URL, stores in local cache
- **Subsequent use**: Loads from cache (instant)
- **Cache duration**: 30 days (configurable)
- **Cache invalidation**: 
  - Manual: Delete `C:\ProgramData\csharpDialog\IconCache\`
  - Automatic: Old files cleaned up after 90 days

### Cache Management

```powershell
# Clear icon cache
Remove-Item "C:\ProgramData\csharpDialog\IconCache\*" -Recurse -Force

# Pre-populate cache (for offline scenarios)
$icons = @("chrome.png", "zoom.png", "office.png")
$baseUrl = "https://cimian.company.com/deployment/icons/"

foreach ($icon in $icons) {
    Invoke-WebRequest -Uri "$baseUrl$icon" -OutFile "C:\ProgramData\csharpDialog\IconCache\$icon"
}
```

## Cimian Integration

### Workflow

1. **Package creation**: Extract icon during package build
   ```powershell
   Extract-ExeIcon -ExePath "chrome.exe" -OutputPng "deployment/icons/chrome.png"
   ```

2. **Package info creation**: Reference icon by basename in YAML
   ```yaml
   # deployment/pkgsinfo/apps/chrome.yaml
   icon_name: chrome.png
   ```

3. **Upload to repository**: Both package and icon to deployment repo
   ```
   deployment/
     icons/chrome.png
     pkgsinfo/apps/chrome.yaml
     pkgs/chrome-120.0-x64.cimipkg
   ```

4. **Catalog generation**: Aggregate package info into catalog
   ```powershell
   New-CimianCatalog -OutputPath "deployment/catalogs/production.json"
   ```

5. **Client sync**: Cimian downloads manifest and icon cache
   ```
   C:\ProgramData\ManagedInstalls\
     manifests\staff.json
     icons\chrome.png       ← Downloaded by Cimian
   ```

6. **Dialog display**: csharpDialog reads icons from Cimian's cache
   ```powershell
   csharpdialog --cimian --firstrun
   ```

7. **Progress updates**: Status updates as Cimian installs packages

### Example Cimian Integration Script

```powershell
# Read Cimian manifest
$manifest = Get-Content "C:\ProgramData\ManagedInstalls\manifests\staff.json" | ConvertFrom-Json

# Build dialog configuration
$listItems = @()
foreach ($package in $manifest.managed_installs) {
    # Icon resolution: Cimian has already cached icons locally
    $iconPath = "C:\ProgramData\ManagedInstalls\icons\$($package.icon_name)"
    
    # Fallback to generic if specific icon missing
    if (-not (Test-Path $iconPath)) {
        $iconPath = "C:\ProgramData\ManagedInstalls\icons\_generic.png"
    }
    
    $listItems += @{
        title = $package.display_name
        icon = $iconPath        # Local path from Cimian's cache
        status = "pending"
    }
}

# Launch dialog
$json = @{
    title = "Setting up your device"
    message = "Please wait while we install your applications"
    listitems = $listItems
    commandfile = "C:\ProgramData\csharpDialog\cimian-progress.log"
} | ConvertTo-Json -Depth 10

$json | Out-File "C:\ProgramData\csharpDialog\config.json"

# Start dialog with Cimian monitoring
Start-Process csharpdialog -ArgumentList "--json-file C:\ProgramData\csharpDialog\config.json --cimian"

# Monitor Cimian log and update dialog
# (Implementation in separate monitoring script - see Cimian-Integration.md)
```

## Best Practices

### For Administrators

1. **Standardize icon sizes**: Use 512×512 consistently
2. **Optimize files**: Keep icons under 100KB each
3. **Use CDN**: Host icons on fast, reliable infrastructure
4. **Test offline**: Ensure graceful fallbacks when icons can't load
5. **Version control**: Track icon changes alongside packages
6. **Naming convention**: Use consistent, descriptive filenames

### For Package Authors

1. **Extract during packaging**: Automate icon extraction in build scripts
2. **Test rendering**: Verify icons look good on light and dark backgrounds
3. **Include metadata**: Document icon source and license in manifest
4. **Fallback category**: Specify category for generic icon fallback
5. **High quality**: Use official vendor assets when possible

### For End Users

Icons are managed automatically - no user intervention required!

## Troubleshooting

### Icon Not Displaying

1. **Check URL**: Verify icon URL is accessible
2. **Check cache**: Look in `C:\ProgramData\csharpDialog\IconCache\`
3. **Check format**: Ensure PNG format with transparency
4. **Check size**: Very large files may timeout
5. **Check permissions**: Ensure cache directory is writable

### Performance Issues

1. **Use local caching**: Pre-populate cache for offline scenarios
2. **Optimize PNGs**: Compress images to reduce download time
3. **Use CDN**: Serve icons from fast, geographically distributed servers
4. **Limit concurrent**: Download icons sequentially, not all at once

### Generic Icons Showing

This is expected behavior! Generic icons display when:
- Icon URL is unavailable
- Network is offline
- Icon file is corrupt
- No icon specified in manifest

## Related Documentation

- [Cimian Integration Guide](Cimian-Integration.md)
- [List Items Documentation](../README.md#list-items)
- [Command Line Options](../README.md#command-line-options)

## References

### Munki Icon Management
- [Munki Wiki - Icons](https://github.com/munki/munki/wiki/Icons)
- [munkiimport Icon Handling](https://github.com/munki/munki/wiki/munkiimport)

### Icon Resources
- [Icon Extraction Tools](https://www.nirsoft.net/utils/iconsext.html)
- [Free Icon Libraries](https://www.flaticon.com/)
- [Microsoft Fluent Icons](https://github.com/microsoft/fluentui-system-icons)

---

*For questions or improvements to this workflow, please open an issue on the [csharpDialog repository](https://github.com/windowsadmins/csharpdialog).*

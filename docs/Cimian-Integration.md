# Cimian Integration for First-Run Device Setup

This document explains how to use csharpDialog with Cimian for first-run device setup scenarios, similar to how swiftDialog is used with Munki on macOS.

## Overview

csharpDialog now supports monitoring Cimian's `managedsoftwareupdate` progress and displaying it in a user-friendly interface during first-login scenarios. This provides a seamless experience for users when their Windows device is being set up with essential software.

## Workflow

1. **Device Bootstrap**: New or reset Windows device runs through OOBE (Out-of-Box Experience)
2. **ESP Installation**: Cimian gets installed during the Enrollment Status Page (ESP)
3. **User Login**: User signs in with their Entra ID credentials and lands on desktop
4. **Auto-Launch**: csharpDialog automatically launches in fullscreen/kiosk mode
5. **Progress Display**: Shows real-time progress of Cimian software installations (Chrome, Zoom, PaperCut, etc.)
6. **Completion**: User cannot skip and must wait for completion before proceeding

## Setup Instructions

### Prerequisites

- Windows 10/11 device
- Cimian installed and configured
- .NET 9.0 runtime
- Administrative privileges for setup

### Installation

1. **Build csharpDialog**:
   ```bash
   git clone https://github.com/windowsadmins/csharpdialog.git
   cd csharpdialog
   dotnet build --configuration Release
   ```

2. **Run Setup Script**:
   ```powershell
   .\Setup-CimianFirstRun.ps1 -Install
   ```

   Or manually configure:
   ```powershell
   .\Setup-CimianFirstRun.ps1 -Configure
   ```

### Manual Configuration

If you prefer to configure manually:

1. **Copy binaries** to `C:\Program Files\csharpDialog\`
2. **Add registry entry** for auto-launch:
   ```reg
   [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run]
   "CSharpDialogFirstRun"="\"C:\\Program Files\\csharpDialog\\csharpDialog.CLI.exe\" --autolaunch"
   ```

3. **Set first-run markers** in registry:
   ```reg
   [HKEY_LOCAL_MACHINE\SOFTWARE\csharpDialog\FirstRun]
   "Enabled"=dword:00000001
   "InstallPath"="C:\\Program Files\\csharpDialog"
   ```

## Command Line Usage

### Basic First-Run Dialog

```bash
# Auto-detect first-run and launch appropriate dialog
csharpdialog --autolaunch

# Explicit first-run mode with Cimian monitoring
csharpdialog --firstrun --cimian --fullscreen

# Kiosk mode (cannot be closed by user)
csharpdialog --kiosk --cimian --title "Setting up your device"
```

### Advanced Configuration

```bash
# Full first-run setup with custom branding
csharpdialog --firstrun --cimian --fullscreen \
  --title "Welcome to Contoso" \
  --message "Please wait while we configure your device" \
  --backgroundcolor "#0078d4" \
  --textcolor "#ffffff"

# Progress monitoring only (no list items)
csharpdialog --cimian --progress --progresstext "Installing software..."

# Test mode (shows what would happen without Cimian)
csharpdialog --firstrun --fullscreen --listitem "Chrome" --listitem "Zoom"
```

## First-Run Detection

csharpDialog uses multiple methods to detect first-run scenarios:

### Detection Methods

1. **User Profile Age**: Checks if `ntuser.dat` was created recently (< 10 minutes)
2. **Registry Markers**: Looks for first-run flags in Windows and Cimian registry keys
3. **Bootstrap Files**: Checks for Cimian bootstrap completion markers
4. **System Boot Time**: Considers recent system boots as potential first-run indicators
5. **OOBE Status**: Detects if OOBE was recently completed

### Registry Locations

- `HKLM\SOFTWARE\Cimian\FirstRun` - Cimian first-run marker
- `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FirstRun` - Windows first-login
- `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\OOBE` - OOBE completion status

### File System Markers

- `C:\ProgramData\Cimian\bootstrap_complete` - Bootstrap completion
- `C:\ProgramData\Cimian\bootstrap_in_progress` - Active bootstrap
- `%USERPROFILE%\ntuser.dat` creation time - Profile age

## Cimian Integration

### Log Monitoring

csharpDialog monitors the Cimian log file at:
- `C:\ProgramData\Cimian\Logs\managedsoftwareupdate.log`

### Supported Log Patterns

The monitor looks for these log entry patterns:

```
Installing Chrome...                    # Installation started
Download progress: Chrome 45%          # Progress update
Installation complete: Chrome          # Success
Error installing: Zoom - Access denied # Failure
```

### Cimian Repository Layout

The Cimian repository follows a Munki-inspired structure:

```
/deployment
  /pkgsinfo
    /apps
      firefox.yaml        # Package metadata with icon_name
      chrome.yaml
      zoom.yaml
      vscode.yaml
  /icons
    firefox.png          # 512×512 PNG with transparency
    chrome.png
    zoom.png
    vscode.png
    _generic.png         # Fallback icon
  /pkgs
    firefox-126.0.1-x64.cimipkg
    chrome-120.0-x64.cimipkg
    zoom-5.17.0-x64.cimipkg
    vscode-1.94.0-x64.cimipkg
  /catalogs
    production.json      # Aggregated catalog
```

### Manifest Parsing

Expected manifest locations:
- `C:\ProgramData\Cimian\manifests\staff.yaml`
- `C:\ProgramData\Cimian\manifests\default.yaml`

Example package info (YAML):
```yaml
name: Chrome
display_name: Google Chrome
version: 120.0.6099.130
icon_name: chrome.png        # Referenced by basename, not full URL
category: Browser
description: Fast, secure web browser from Google
installer:
  type: msi
  path: pkgs/chrome-120.0-x64.cimipkg
  arguments: /quiet /norestart
```

Example manifest structure (JSON):
```json
{
  "managed_installs": [
    {
      "name": "Chrome",
      "display_name": "Google Chrome",
      "version": "latest",
      "icon_name": "chrome.png"
    },
    {
      "name": "Zoom",
      "display_name": "Zoom Client",
      "version": "latest",
      "icon_name": "zoom.png"
    }
  ]
}
```

**Note**: Icons are repository assets referenced by basename (`icon_name`), not full URLs. Icons are downloaded by Cimian during manifest sync and cached locally. See [Icon Management](Icon-Management.md) for details.

## User Interface Modes

### Fullscreen Mode

- Covers entire screen
- Corporate branding
- Progress bar and list of installations
- Admin escape hatch (Ctrl+Alt+Shift+X)

### Kiosk Mode

- Fullscreen with no escape
- User cannot close or minimize
- Designed for unattended deployment
- Only closes when installation completes

### Console Mode

- Fallback for systems without GUI
- Text-based progress display
- Suitable for server environments

## Troubleshooting

### Common Issues

1. **csharpDialog doesn't launch automatically**
   - Check registry entry exists
   - Verify executable path is correct
   - Ensure user has permissions

2. **First-run not detected**
   - Check profile creation time
   - Verify Cimian registry markers
   - Test with `csharpdialog --autolaunch`

3. **Cimian monitoring not working**
   - Verify Cimian is installed and running
   - Check log file exists and is accessible
   - Ensure managedsoftwareupdate process is active

### Testing

```powershell
# Test first-run detection
.\Setup-CimianFirstRun.ps1 -Test

# Test dialog manually
csharpdialog --firstrun --fullscreen --listitem "Test App"

# Test Cimian integration
csharpdialog --cimian --progress --progresstext "Testing..."
```

### Logging

Logs are written to:
- `C:\ProgramData\csharpDialog\Setup.log` - Setup script log
- Console output from csharpDialog executable

## Enterprise Deployment

### Group Policy

Create a GPO to deploy the registry settings:
1. Computer Configuration → Preferences → Windows Settings → Registry
2. Add the auto-launch registry entry
3. Link to appropriate OU

### SCCM/Intune

Package csharpDialog as an application:
1. Create application package
2. Include setup script in detection rules
3. Deploy to device collections

### PowerShell DSC

Example DSC configuration:
```powershell
Configuration CSharpDialogSetup {
    Registry AutoLaunch {
        Key = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
        ValueName = "CSharpDialogFirstRun"
        ValueData = "C:\Program Files\csharpDialog\csharpDialog.CLI.exe --autolaunch"
        Ensure = "Present"
    }
}
```

## Security Considerations

1. **Admin Escape**: Fullscreen mode includes admin escape sequence
2. **File Permissions**: Ensure proper ACLs on installation directory
3. **Registry Security**: First-run registry keys require admin access
4. **Code Signing**: Sign executables for enterprise environments

## Advanced Features

### Custom Branding

```bash
csharpdialog --firstrun --fullscreen \
  --backgroundcolor "#your-color" \
  --image "C:\Company\logo.png" \
  --fontfamily "Your Font"
```

### Progress Callbacks

Monitor progress programmatically:
```powershell
$process = Start-Process csharpdialog -ArgumentList "--firstrun --cimian" -PassThru
# Monitor process or use command file for updates
```

### Integration with Other Tools

- **PowerShell**: Call from login scripts
- **Task Scheduler**: Schedule for first login
- **Group Policy**: Deploy via startup scripts

## Best Practices

1. **Testing**: Always test in lab environment first
2. **Fallback**: Provide console mode fallback
3. **Monitoring**: Log deployment results
4. **User Communication**: Clear messaging about wait times
5. **Admin Access**: Provide escape mechanisms for IT staff

## Support and Troubleshooting

For issues or questions:
1. Check the logs in `C:\ProgramData\csharpDialog\`
2. Run test commands to isolate issues
3. Verify Cimian is working independently
4. Create GitHub issues for bugs or feature requests

## Application Icons

csharpDialog uses a repository-based icon system identical to Munki on macOS. Icons are:
- **Stored in deployment repository**: `deployment/icons/` directory
- **Referenced by basename**: Packages specify `icon_name: chrome.png`, not full URLs
- **Cached by Cimian**: Downloaded during manifest sync to `%ProgramData%\ManagedInstalls\icons\`
- **Available before installation**: Enables rich progress dialogs during first-run

### Icon Distribution Flow

```
┌──────────────────────────────────────────────────────────┐
│  Cimian Repository (Azure Blob / HTTPS)                 │
│                                                          │
│  deployment/icons/                                       │
│    ├── chrome.png       (512×512 PNG)                    │
│    ├── firefox.png                                       │
│    ├── zoom.png                                          │
│    └── _generic.png     (fallback)                       │
└──────────────────────────────────────────────────────────┘
                        ↓
            Cimian Client Sync (manifest update)
                        ↓
┌──────────────────────────────────────────────────────────┐
│  Windows Client                                          │
│                                                          │
│  C:\ProgramData\ManagedInstalls\icons\                   │
│    ├── chrome.png       (cached by Cimian)               │
│    ├── firefox.png                                       │
│    └── zoom.png                                          │
│                                                          │
│  csharpDialog reads from cached icons                    │
└──────────────────────────────────────────────────────────┘
```

### Quick Start

1. **Create icons folder** in your Cimian repository:
   ```
   deployment/
     icons/
       chrome.png        # 512×512 PNG with alpha transparency
       firefox.png
       zoom.png
       vscode.png
       _generic.png      # Fallback for missing icons
   ```

2. **Reference icons in pkgsinfo** (by basename only):
   ```yaml
   # deployment/pkgsinfo/apps/chrome.yaml
   name: Chrome
   display_name: Google Chrome
   icon_name: chrome.png    # Basename only, resolved to deployment/icons/
   ```

3. **Cimian downloads icons** during manifest sync:
   - Icons cached to `%ProgramData%\ManagedInstalls\icons\`
   - Refresh when manifest version changes

4. **csharpDialog displays icons** automatically:
   ```powershell
   # Icons read from Cimian's cache
   csharpdialog --cimian --firstrun
   ```

For complete details on icon extraction, optimization, and management, see [Icon Management Guide](Icon-Management.md).

## Related Documentation

- [Icon Management Guide](Icon-Management.md) - **How to manage application icons**
- [csharpDialog README](../README.md)
- [Cimian Documentation](https://github.com/windowsadmins/cimian)
- [swiftDialog (macOS equivalent)](https://github.com/swiftDialog/swiftDialog)
- [Munki Icon Workflow](https://github.com/munki/munki/wiki/Icons) - macOS reference implementation
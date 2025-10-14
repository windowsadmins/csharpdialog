# csharpDialog

A Windows port of [swiftDialog](https://github.com/swiftDialog/swiftDialog) written in C#. csharpDialog provides a powerful command-line utility for creating customizable dialog boxes and notifications on Windows.

## Overview

csharpDialog brings the elegant simplicity of macOS swiftDialog to Windows users, allowing system administrators, developers, and automation scripts to create rich, interactive dialog boxes with minimal effort.

## Features

- **Command-line Interface**: Fully customizable from command-line arguments
- **Rich UI Elements**: Support for custom titles, messages, buttons, icons, images, and videos
- **Multiple Display Modes**: WPF GUI dialogs and console fallback
- **Customizable Appearance**: Colors, fonts, window size, and positioning
- **Timeout Support**: Auto-close dialogs after specified time
- **Markdown Support**: Rich text formatting in messages (planned)
- **Scriptable**: Perfect for automation and administrative scripts
- **Cimian Integration**: First-run device setup with progress monitoring
- **Fullscreen/Kiosk Mode**: Uninterruptible progress display for device deployment
- **Real-time Progress**: Live updates from Cimian software installations

## Architecture

The project is structured as a multi-project solution:

- **csharpDialog.Core**: Shared library with dialog configuration and services
- **csharpDialog.CLI**: Command-line interface application
- **csharpDialog.WPF**: Windows Presentation Foundation GUI components

## Installation

### Prerequisites

- .NET 9.0 or later
- Windows 10/11

### Building from Source

```bash
git clone https://github.com/windowsadmins/csharpdialog.git
cd csharpdialog
dotnet build
```

### Enterprise Environment Setup

For enterprise environments with code signing requirements:

```powershell
# Build and sign with enterprise certificate
.\sign-build.ps1 -All

# Or build and sign separately
.\sign-build.ps1 -Build
.\sign-build.ps1 -Sign
```

**Note**: Signing requires the "EmilyCarrU Intune Windows Enterprise Certificate" to be available in the certificate store.

### Running in Restricted Environments

If executable files are blocked by security policies, use the PowerShell launcher:

```powershell
# Use the PowerShell launcher script
.\csharpdialog.ps1 --title "Hello" --message "World"

# Or run the DLL directly
dotnet "src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.dll" --title "Hello" --message "World"
```

### Usage

Basic usage:
```bash
# Simple message dialog
csharpdialog --title "Hello" --message "Welcome to csharpDialog!"

# Confirmation dialog
csharpdialog --title "Confirm Action" --message "Are you sure?" --button1 "Yes" --button2 "No"

# Custom styling
csharpdialog --title "Styled Dialog" --message "Custom colors and fonts" --backgroundcolor "#f0f0f0" --textcolor "#333333" --fontsize 14

# With timeout
csharpdialog --title "Auto-close" --message "This will close in 10 seconds" --timeout 10
```

## Command Line Options

### Basic Flags
| Option | Description | Example |
|--------|-------------|---------|
| `--title` | Window title | `--title "Hello World"` |
| `--message` | Main message content | `--message "Welcome to csharpDialog"` |
| `--button1text` | Primary button label | `--button1text "OK"` |
| `--button2text` | Secondary button label | `--button2text "Cancel"` |
| `--icon`, `-i` | Set dialog icon | `--icon "path/to/icon.png"` |
| `--help`, `-h` | Show help message | `--help` |

### Progress & Status
| Option | Description | Example |
|--------|-------------|---------|
| `--progressbar` | Enable progress bar | `--progressbar` |
| `--progress` | Set progress percentage (0-100) | `--progress 50` |
| `--progresstext` | Progress description text | `--progresstext "Step 2 of 4"` |

### List Items
| Option | Description | Example |
|--------|-------------|---------|
| `--listitem` | Add list item with status | `--listitem "Item Name,status"` |
| | Status values: `none`, `wait`, `pending`, `success`, `fail`, `error` | `--listitem "Chrome,success"` |

### Display Modes
| Option | Description | Example |
|--------|-------------|---------|
| `--fullscreen` | Fullscreen with blurred background | `--fullscreen` |
| `--kiosk` | Kiosk mode (fullscreen, cannot close) | `--kiosk` |
| `--width` | Set window width (default: 750) | `--width 800` |
| `--height` | Set window height (default: 1000) | `--height 1200` |
| `--centeronscreen` | Center dialog on screen | `--centeronscreen` |
| `--topmost` | Keep dialog on top | `--topmost` |

### Command File
| Option | Description | Example |
|--------|-------------|---------|
| `--commandfile` | Enable live updates via command file | `--commandfile "C:\temp\commands.txt"` |

### Styling (Legacy)
| Option | Description | Example |
|--------|-------------|---------|
| `--backgroundcolor` | Set background color | `--backgroundcolor "#ffffff"` |
| `--textcolor` | Set text color | `--textcolor "#000000"` |
| `--fontfamily` | Set font family | `--fontfamily "Arial"` |
| `--fontsize` | Set font size | `--fontsize 12` |
| `--timeout` | Auto-close after seconds | `--timeout 30` |

## Quick Reference

### Common Command Patterns

#### Basic Dialog
```powershell
dialog --title "Title" --message "Message" --button1text "OK"
```

#### Progress Dialog
```powershell
dialog --title "Processing" --message "Please wait..." --progressbar --progress 50 --progresstext "Step 2 of 4"
```

#### List Items
```powershell
dialog --title "Status" --listitem "Item 1,success" --listitem "Item 2,pending" --listitem "Item 3,wait"
```

#### Fullscreen with Blur
```powershell
dialog --fullscreen --title "Important" --message "Fullscreen with Windows 11 blur effect"
```

#### Kiosk Mode (Cannot Close)
```powershell
dialog --kiosk --commandfile "C:\temp\commands.txt" --title "Critical Process"
```

### Command File Syntax

Command files enable real-time updates to dialogs during long-running operations.

#### Add List Item
```
listitem: add, title: Item Name, status: pending, statustext: In progress...
```

#### Update List Item
```
listitem: update, title: Item Name, status: success, statustext: Complete!
```

#### Update Progress
```
progress: 75
progresstext: Processing step 3 of 4...
```

#### Update UI Elements
```
title: New Title
message: Updated message
button1text: Continue
```

#### Close Dialog
```
quit
```

### Status Values

- `none` - No indicator
- `wait` - Orange circle (waiting)
- `pending` - Blue spinning indicator (in progress)
- `success` - Green checkmark (completed)
- `fail` - Red X (failed)
- `error` - Red exclamation (error)

### Exit Codes

- `0` - Button 1 clicked (primary action)
- `2` - Button 2 clicked (secondary action)
- `1` - Error or window closed

### Tips & Best Practices

#### Using Command Files
Always start your command file process in a separate thread:
```powershell
$process = Start-Process dialog -ArgumentList @("--commandfile", $file) -PassThru -NoNewWindow
```

#### Fullscreen vs Kiosk
- **Fullscreen**: User can close, shows blurred background
- **Kiosk**: User CANNOT close, requires `quit` command, shows blurred background

#### List Item Updates
Match the `title` exactly when updating:
```powershell
Add-Content $file "listitem: add, title: My App, status: wait"
# Later...
Add-Content $file "listitem: update, title: My App, status: success"  # Must match "My App"
```

#### Progress Bar Updates
Set progress first, then progresstext:
```powershell
Add-Content $file "progress: 50"
Add-Content $file "progresstext: Halfway done..."
```

#### Window Sizing
Default height is 1000px (tall), default width is 750px:
```powershell
dialog --width 900 --height 1200  # Custom size
```

### Common Patterns

#### Installation Tracker
```powershell
# Start
Add-Content $file "listitem: add, title: App Name, status: pending"
Add-Content $file "progresstext: Installing App Name..."

# Complete
Add-Content $file "listitem: update, title: App Name, status: success"
Add-Content $file "progress: $percent"
```

#### Multi-Stage Process
```powershell
$stages = @("Stage 1", "Stage 2", "Stage 3")
foreach ($stage in $stages) {
    Add-Content $file "listitem: add, title: $stage, status: pending"
    # Do work...
    Add-Content $file "listitem: update, title: $stage, status: success"
}
```

#### Error Handling
```powershell
try {
    # Do something
    Add-Content $file "listitem: update, title: Task, status: success"
} catch {
    Add-Content $file "listitem: update, title: Task, status: error, statustext: $($_.Exception.Message)"
}
```

## Examples

The `examples` directory contains complete working scripts demonstrating various features:

### Basic Usage
- **basic-message.ps1** - Simple message dialog with title and button
- **progress-bar.ps1** - Progress bar with incremental updates
- **list-items.ps1** - Display list items with status indicators
- **button-actions.ps1** - Multiple buttons with different actions
- **custom-sizing.ps1** - Different window sizes

### Interactive Features
- **command-file-demo.ps1** - Live updates using command file

### Advanced Scenarios
- **fullscreen-mode.ps1** - Fullscreen with blurred background
- **kiosk-mode.ps1** - Locked fullscreen for critical operations
- **software-install.ps1** - Software installation progress tracker
- **onboarding-workflow.ps1** - Complete user onboarding experience

### Specific Use Cases
- **first-run-setup.ps1** - Initial device setup wizard
- **maintenance-mode.ps1** - System maintenance notification
- **policy-enforcement.ps1** - Compliance policy reminder
- **update-notification.ps1** - Software update prompt

### Running Examples

All examples assume csharpDialog is installed at `C:\Program Files\csharpDialog\dialog.exe`.

Run any example with PowerShell:
```powershell
.\examples\basic-message.ps1
```

Or with elevated privileges (for system-level operations):
```powershell
sudo pwsh .\examples\kiosk-mode.ps1
```

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Project Structure

```
csharpdialog/
├── src/
│   ├── csharpDialog.Core/       # Shared library
│   ├── csharpDialog.CLI/        # Command-line interface
│   └── csharpDialog.WPF/        # WPF GUI components
├── tests/                       # Unit tests (planned)
├── docs/                        # Documentation (planned)
└── README.md
```

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup

1. Clone the repository
2. Install .NET 9.0 SDK
3. Open in Visual Studio Code or Visual Studio
4. Run `dotnet restore` to restore packages

## Cimian First-Run Integration

csharpDialog now supports first-run device setup monitoring, similar to how swiftDialog works with Munki on macOS:

```bash
# Auto-launch for first-run scenarios
csharpdialog --autolaunch

# Fullscreen Cimian progress monitoring
csharpdialog --firstrun --cimian --fullscreen --title "Setting up your device"

# Kiosk mode (user cannot close)
csharpdialog --kiosk --cimian --title "Device Configuration"
```

**Workflow:**
1. New Windows device completes OOBE
2. User signs in with Entra ID
3. csharpDialog auto-launches in fullscreen
4. Shows real-time progress of Cimian software installations (Chrome, Zoom, PaperCut, etc.)
5. User waits for completion before proceeding

See [Cimian Integration Guide](docs/Cimian-Integration.md) for detailed setup instructions.

## Application Icons

csharpDialog supports displaying application icons in list items. Icons can be loaded from local file paths or URLs.

### Icon Support

- **Repository hosted**: Icons stored alongside packages in deployment repo
- **Cached locally**: Downloaded once, instant loading thereafter
- **Available before install**: Show app icons during installation progress
- **Automatic fallbacks**: Generic icons when specific icons unavailable

### Icon Usage

Icons are specified as the third parameter in list items:
```powershell
# With icon file path
dialog --listitem "Chrome,success,C:\ProgramData\ManagedInstalls\icons\chrome.png"

# Without icon
dialog --listitem "Chrome,success"
```

For complete documentation on icon management, extraction, and best practices, see [Icon Management Guide](docs/Icon-Management.md).

## Roadmap

- [x] Cimian progress monitoring and first-run detection
- [x] Fullscreen and kiosk mode support
- [x] Real-time progress bars and list item updates
- [x] Repository-based icon system with caching
- [ ] Advanced markdown support
- [ ] Form elements (text inputs, dropdowns)
- [ ] Sound notifications
- [ ] JSON configuration files
- [ ] PowerShell module
- [ ] MSI installer
- [ ] Unit tests
- [ ] Documentation site

## Inspiration

This project is inspired by and aims to provide Windows compatibility with the excellent [swiftDialog](https://github.com/swiftDialog/swiftDialog) project for macOS by [Bart Reardon](https://github.com/bartreardon).

## License

MIT License - see [LICENSE](LICENSE) for details.

## Support

- Create an [issue](https://github.com/windowsadmins/csharpdialog/issues) for bug reports or feature requests
- Join the discussion in [Discussions](https://github.com/windowsadmins/csharpdialog/discussions)

---

*csharpDialog - elegant dialog boxes for Windows*

# CsharpDialog

A Windows port of [swiftDialog](https://github.com/swiftDialog/swiftDialog) written in C#. CsharpDialog provides a powerful command-line utility for creating customizable dialog boxes and notifications on Windows.

## Overview

CsharpDialog brings the elegant simplicity of macOS swiftDialog to Windows users, allowing system administrators, developers, and automation scripts to create rich, interactive dialog boxes with minimal effort.

## Features

- **Command-line Interface**: Fully customizable from command-line arguments
- **Rich UI Elements**: Support for custom titles, messages, buttons, icons, images, and videos
- **Multiple Display Modes**: WPF GUI dialogs and console fallback
- **Customizable Appearance**: Colors, fonts, window size, and positioning
- **Timeout Support**: Auto-close dialogs after specified time
- **Markdown Support**: Rich text formatting in messages (planned)
- **Scriptable**: Perfect for automation and administrative scripts

## Architecture

The project is structured as a multi-project solution:

- **CsharpDialog.Core**: Shared library with dialog configuration and services
- **CsharpDialog.CLI**: Command-line interface application
- **CsharpDialog.WPF**: Windows Presentation Foundation GUI components

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

### Usage

Basic usage:
```bash
# Simple message dialog
csharpdialog --title "Hello" --message "Welcome to CsharpDialog!"

# Confirmation dialog
csharpdialog --title "Confirm Action" --message "Are you sure?" --button1 "Yes" --button2 "No"

# Custom styling
csharpdialog --title "Styled Dialog" --message "Custom colors and fonts" --backgroundcolor "#f0f0f0" --textcolor "#333333" --fontsize 14

# With timeout
csharpdialog --title "Auto-close" --message "This will close in 10 seconds" --timeout 10
```

## Command Line Options

| Option | Description | Example |
|--------|-------------|---------|
| `--title`, `-t` | Set dialog title | `--title "My Title"` |
| `--message`, `-m` | Set dialog message | `--message "Hello World"` |
| `--icon`, `-i` | Set dialog icon | `--icon "path/to/icon.png"` |
| `--button1` | Set first button text | `--button1 "OK"` |
| `--button2` | Set second button text | `--button2 "Cancel"` |
| `--timeout` | Auto-close after seconds | `--timeout 30` |
| `--width` | Set dialog width | `--width 500` |
| `--height` | Set dialog height | `--height 300` |
| `--centeronscreen` | Center dialog on screen | `--centeronscreen` |
| `--topmost` | Keep dialog on top | `--topmost` |
| `--backgroundcolor` | Set background color | `--backgroundcolor "#ffffff"` |
| `--textcolor` | Set text color | `--textcolor "#000000"` |
| `--fontfamily` | Set font family | `--fontfamily "Arial"` |
| `--fontsize` | Set font size | `--fontsize 12` |
| `--markdown` | Enable markdown in message | `--markdown` |
| `--image` | Display image | `--image "path/to/image.jpg"` |
| `--video` | Display video | `--video "path/to/video.mp4"` |
| `--help`, `-h` | Show help message | `--help` |

## Examples

### Administrative Scripts

```bash
# Software installation confirmation
csharpdialog --title "Software Installation" \
  --message "Install Microsoft Office 365?" \
  --icon "C:\Windows\System32\msiexec.exe" \
  --button1 "Install" \
  --button2 "Cancel" \
  --topmost

# System maintenance notification
csharpdialog --title "System Maintenance" \
  --message "System will restart in 5 minutes for updates." \
  --timeout 300 \
  --centeronscreen
```

### User Notifications

```bash
# Welcome message with custom branding
csharpdialog --title "Welcome to Company Portal" \
  --message "Please follow the setup instructions." \
  --image "C:\Company\logo.png" \
  --backgroundcolor "#0078d4" \
  --textcolor "#ffffff"
```

## Exit Codes

- `0`: Success (OK or first button clicked)
- `1`: Cancel or second button clicked
- `1`: Error occurred

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
│   ├── CsharpDialog.Core/       # Shared library
│   ├── CsharpDialog.CLI/        # Command-line interface
│   └── CsharpDialog.WPF/        # WPF GUI components
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

## Roadmap

- [ ] Advanced markdown support
- [ ] Form elements (text inputs, dropdowns)
- [ ] Progress bars
- [ ] List views
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

*CsharpDialog - Bringing elegant dialog boxes to Windows*

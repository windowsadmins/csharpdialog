# Testing csharpDialog with managedsoftwareupdate

This guide explains how to test csharpDialog alongside real `managedsoftwareupdate` runs, simulating the first-run device setup experience.

## Quick Start - Simulated Testing

The easiest way to test is using our simulator:

```powershell
# Fast test (quick for development)
.\Test-DialogWithSimulator.ps1 -Fast

# Realistic timing test
.\Test-DialogWithSimulator.ps1
```

This will:
1. Launch csharpDialog with command file monitoring
2. Simulate managedsoftwareupdate installing 5 applications
3. Show real-time progress updates in the dialog
4. Complete with all apps marked as "Installed"

## Testing with Real managedsoftwareupdate

### Prerequisites

- Cimian installed and configured
- csharpDialog built and ready
- Administrator privileges
- Active Cimian manifest with software to install

### Method 1: Manual Command File Integration

This method manually launches the dialog and monitors a command file that you update:

```powershell
# 1. Start the dialog with command file monitoring
$commandFile = "C:\ProgramData\csharpDialog\command.txt"

.\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe `
    --commandfile $commandFile `
    --title "Installing Company Applications" `
    --message "Please wait while we install required applications..." `
    --progressbar `
    --centeronscreen `
    --topmost `
    --width 600 `
    --height 500

# 2. In another PowerShell window, write commands to the file:
"title: Installing Applications" | Out-File $commandFile -Append
"listitem: add, title: Chrome, status: pending" | Out-File $commandFile -Append
"listitem: index: 0, status: wait, statustext: Installing..." | Out-File $commandFile -Append

# ... continue with more commands as software installs
```

### Method 2: Wrapper Script for managedsoftwareupdate

Create a wrapper that launches both the dialog and managedsoftwareupdate:

```powershell
# Launch-ManagedInstallWithDialog.ps1

$commandFile = "C:\ProgramData\csharpDialog\command.txt"

# Initialize command file
if (Test-Path $commandFile) { Remove-Item $commandFile -Force }
"# Command file for managedsoftwareupdate integration" | Out-File $commandFile

# Start the dialog
$dialogJob = Start-Job {
    param($exe, $file)
    & $exe --commandfile $file --title "Installing Software" --progressbar
} -ArgumentList "$PSScriptRoot\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe", $commandFile

Start-Sleep -Seconds 2

# Parse manifest and add list items
$manifest = Get-Content "C:\ProgramData\Cimian\manifests\staff.json" | ConvertFrom-Json
foreach ($app in $manifest.ManagedInstalls) {
    $title = $app.DisplayName ?? $app.Name
    "listitem: add, title: $title, status: pending" | Out-File $commandFile -Append
}

# Start managedsoftwareupdate
$msuProcess = Start-Process "C:\Program Files\Cimian\managedsoftwareupdate.exe" -PassThru -NoNewWindow

# Monitor log file and update dialog
$logFile = "C:\ProgramData\Cimian\Logs\managedsoftwareupdate.log"
$lastPosition = 0

while (!$msuProcess.HasExited) {
    if (Test-Path $logFile) {
        $content = Get-Content $logFile -Raw
        $newContent = $content.Substring($lastPosition)
        $lastPosition = $content.Length
        
        # Parse log for installation progress
        if ($newContent -match "Installing (\w+)") {
            $appName = $matches[1]
            "listitem: title: $appName, status: wait, statustext: Installing..." | Out-File $commandFile -Append
        }
        
        if ($newContent -match "Installation complete: (\w+)") {
            $appName = $matches[1]
            "listitem: title: $appName, status: success, statustext: Installed" | Out-File $commandFile -Append
        }
    }
    
    Start-Sleep -Seconds 1
}

# Completion
"progress: 100" | Out-File $commandFile -Append
"message: All applications installed successfully!" | Out-File $commandFile -Append

$dialogJob | Stop-Job
```

### Method 3: Full Integration with CimianMonitor

Use the built-in CimianMonitor service for automatic integration:

```powershell
# This uses the built-in Cimian integration
.\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe --cimian --firstrun --fullscreen
```

The CimianMonitor will:
- Detect the Cimian manifest location
- Parse installed software list
- Monitor `managedsoftwareupdate.log` for progress
- Automatically update the dialog UI
- Handle completion and errors

## Command File Format

The command file uses swiftDialog-compatible syntax:

### Initialization Commands
```
title: Installing Company Applications
message: Please wait while we install required applications...
progresstext: Preparing installation...
```

### List Item Commands
```
# Add a new list item
listitem: add, title: Microsoft Office 365, status: pending

# Update by index (0-based)
listitem: index: 0, status: wait, statustext: Installing...
listitem: index: 0, statustext: Installing... 45%
listitem: index: 0, status: success, statustext: Installed

# Update by title
listitem: title: Microsoft Office 365, status: success, statustext: Installed
```

### Progress Commands
```
progress: 0
progress: 50
progress: 100
progresstext: 3 of 5 applications installed
```

### Status Values
- `pending` - Gray circle, waiting to start
- `wait` - Orange circle with animation, in progress
- `success` - Green checkmark, completed successfully
- `fail` - Red X, installation failed
- `error` - Red warning, error occurred

### Button Commands
```
button1text: Continue
button1: enable
button1: disable
```

### Other Commands
```
message: Updated message text
title: Updated title
icon: C:\path\to\icon.ico
quit:
```

## Testing Checklist

- [ ] Dialog launches successfully
- [ ] Command file is created and monitored
- [ ] List items appear as pending initially
- [ ] Items update to "Installing..." when active
- [ ] Progress bar updates correctly
- [ ] Items show green checkmark when complete
- [ ] Overall progress percentage is accurate
- [ ] Final message displays properly
- [ ] Continue button enables when done
- [ ] Dialog closes with correct exit code

## Troubleshooting

### Dialog doesn't launch
```powershell
# Check if executable exists
Test-Path ".\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe"

# Try running directly
.\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe --help
```

### Command file not monitored
```powershell
# Verify command file path
$commandFile = "C:\ProgramData\csharpDialog\command.txt"
Test-Path $commandFile

# Check file permissions
Get-Acl $commandFile | Format-List

# Manually test by writing commands
"title: Test" | Out-File $commandFile -Append
```

### Dialog doesn't update
- Ensure commands are being written to the file
- Check that file writes are flushing (use `-Append`)
- Verify the dialog is still running (check process list)
- Look for errors in console output

### managedsoftwareupdate not found
```powershell
# Locate Cimian installation
Get-ChildItem "C:\Program Files\Cimian" -Recurse -Filter "managedsoftwareupdate.exe"

# Check if Cimian is installed
Test-Path "C:\Program Files\Cimian"
```

## Real-World Deployment

For actual first-run scenarios:

1. **Deploy via Intune/SCCM**: Package csharpDialog as an application
2. **Auto-launch at login**: Use registry Run key or scheduled task
3. **Monitor Cimian**: Let CimianMonitor handle the integration
4. **Lock down UI**: Use `--kiosk --fullscreen` for uninterrupted setup
5. **Branding**: Add `--backgroundcolor` and `--image` for company branding

Example deployment command:
```powershell
# First-run setup that auto-launches at login
"C:\Program Files\csharpDialog\csharpDialog.CLI.exe" --autolaunch --cimian --fullscreen --kiosk
```

## Performance Tips

- Command file monitoring uses minimal CPU (FileSystemWatcher)
- Log file parsing is efficient (reads only new lines)
- WPF updates are throttled to avoid UI lag
- Background monitoring runs on separate thread

## Advanced Testing

### Test Error Handling
```powershell
# Simulate a failed installation
"listitem: index: 2, status: fail, statustext: Installation failed" | Out-File $commandFile -Append
```

### Test Long-Running Installations
```powershell
# Modify simulator for longer times
.\Test-ManagedsoftwareupdateSimulator.ps1  # Uses realistic timing
```

### Test with Many Applications
```powershell
# Add more apps to the simulator script
# Edit Test-ManagedsoftwareupdateSimulator.ps1 $apps array
```

## Next Steps

1. Test with your actual Cimian manifest
2. Customize branding (colors, logo, fonts)
3. Create deployment package
4. Set up auto-launch mechanism
5. Test in production-like environment
6. Deploy to pilot group

## Support

For issues:
- Check logs in console output
- Review command file contents
- Test with simulator first
- Create GitHub issue with details

---

*Happy Testing! 🚀*

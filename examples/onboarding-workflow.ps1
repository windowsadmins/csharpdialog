#Requires -Version 5.1
<#
.SYNOPSIS
    Complete user onboarding workflow
.DESCRIPTION
    Demonstrates a full onboarding experience with multiple stages
.EXAMPLE
    .\onboarding-workflow.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"
$commandFile = "$env:TEMP\onboarding.txt"

Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Starting user onboarding workflow..."

# Launch in fullscreen for immersive experience
$process = Start-Process $dialog -ArgumentList @(
    "--fullscreen",
    "--commandfile", "`"$commandFile`"",
    "--title", "Welcome to Your New Device",
    "--message", "We're setting up your device with everything you need. This will take about 10 minutes.",
    "--progressbar",
    "--height", "1000"
) -PassThru -NoNewWindow

Start-Sleep -Seconds 2

# Stage 1: System Configuration
Add-Content $commandFile "listitem: add, title: System Configuration, status: pending" -Encoding UTF8
Add-Content $commandFile "listitem: add, title: Software Installation, status: wait" -Encoding UTF8
Add-Content $commandFile "listitem: add, title: Security Setup, status: wait" -Encoding UTF8
Add-Content $commandFile "listitem: add, title: User Profile, status: wait" -Encoding UTF8
Add-Content $commandFile "progresstext: Configuring system settings..." -Encoding UTF8

$tasks = @(
    "Applying group policies",
    "Configuring network settings",
    "Setting time zone",
    "Configuring display settings"
)

foreach ($task in $tasks) {
    Add-Content $commandFile "listitem: update, title: System Configuration, statustext: $task..." -Encoding UTF8
    Start-Sleep -Seconds 2
}

Add-Content $commandFile "listitem: update, title: System Configuration, status: success, statustext: Complete" -Encoding UTF8
Add-Content $commandFile "progress: 25" -Encoding UTF8

# Stage 2: Software Installation
Add-Content $commandFile "listitem: update, title: Software Installation, status: pending" -Encoding UTF8
Add-Content $commandFile "progresstext: Installing corporate software..." -Encoding UTF8

$apps = @("Microsoft Office", "Adobe Acrobat", "Google Chrome", "Zoom", "Microsoft Teams")
foreach ($app in $apps) {
    Add-Content $commandFile "listitem: update, title: Software Installation, statustext: Installing $app..." -Encoding UTF8
    Start-Sleep -Seconds 3
}

Add-Content $commandFile "listitem: update, title: Software Installation, status: success, statustext: Complete" -Encoding UTF8
Add-Content $commandFile "progress: 50" -Encoding UTF8

# Stage 3: Security Setup
Add-Content $commandFile "listitem: update, title: Security Setup, status: pending" -Encoding UTF8
Add-Content $commandFile "progresstext: Configuring security..." -Encoding UTF8

$secTasks = @(
    "Installing certificates",
    "Configuring BitLocker",
    "Enabling Windows Defender",
    "Configuring firewall"
)

foreach ($task in $secTasks) {
    Add-Content $commandFile "listitem: update, title: Security Setup, statustext: $task..." -Encoding UTF8
    Start-Sleep -Seconds 2
}

Add-Content $commandFile "listitem: update, title: Security Setup, status: success, statustext: Complete" -Encoding UTF8
Add-Content $commandFile "progress: 75" -Encoding UTF8

# Stage 4: User Profile
Add-Content $commandFile "listitem: update, title: User Profile, status: pending" -Encoding UTF8
Add-Content $commandFile "progresstext: Setting up your profile..." -Encoding UTF8

$profileTasks = @(
    "Creating user directories",
    "Mapping network drives",
    "Configuring email",
    "Setting up OneDrive"
)

foreach ($task in $profileTasks) {
    Add-Content $commandFile "listitem: update, title: User Profile, statustext: $task..." -Encoding UTF8
    Start-Sleep -Seconds 2
}

Add-Content $commandFile "listitem: update, title: User Profile, status: success, statustext: Complete" -Encoding UTF8
Add-Content $commandFile "progress: 100" -Encoding UTF8

# Complete
Add-Content $commandFile "title: Setup Complete!" -Encoding UTF8
Add-Content $commandFile "message: Your device is ready to use! You can now sign in and start working." -Encoding UTF8
Add-Content $commandFile "progresstext: Onboarding completed successfully" -Encoding UTF8
Start-Sleep -Seconds 4

Add-Content $commandFile "quit" -Encoding UTF8

Wait-Process -Id $process.Id -Timeout 15 -ErrorAction SilentlyContinue
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Onboarding workflow completed successfully"

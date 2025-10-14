#Requires -Version 5.1
<#
.SYNOPSIS
    Software installation progress tracker
.DESCRIPTION
    Demonstrates tracking software installation with real-time progress
.EXAMPLE
    .\software-install.ps1
#>

$dialog = "C:\Program Files\csharpDialog\dialog.exe"
$commandFile = "$env:TEMP\install-progress.txt"

Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

# Software packages to "install"
$packages = @(
    @{Name = "Microsoft Office"; Size = "3.2 GB"; Duration = 5},
    @{Name = "Adobe Acrobat"; Size = "650 MB"; Duration = 3},
    @{Name = "Google Chrome"; Size = "180 MB"; Duration = 2},
    @{Name = "Zoom"; Size = "120 MB"; Duration = 2},
    @{Name = "Microsoft Teams"; Size = "450 MB"; Duration = 3}
)

Write-Host "Starting software installation tracker..."

# Launch dialog
$process = Start-Process $dialog -ArgumentList @(
    "--commandfile", "`"$commandFile`"",
    "--title", "Installing Software",
    "--message", "Installing required applications. This may take several minutes...",
    "--progressbar",
    "--height", "900"
) -PassThru -NoNewWindow

Start-Sleep -Seconds 2

# Add all packages to list first
foreach ($pkg in $packages) {
    Add-Content $commandFile "listitem: add, title: $($pkg.Name), status: wait, statustext: Queued ($($pkg.Size))" -Encoding UTF8
}

# Install each package
$totalPkgs = $packages.Count
for ($i = 0; $i -lt $totalPkgs; $i++) {
    $pkg = $packages[$i]
    $percent = [math]::Round((($i + 1) / $totalPkgs) * 100)
    
    Write-Host "Installing $($pkg.Name)..."
    
    # Start installation
    Add-Content $commandFile "listitem: update, title: $($pkg.Name), status: pending, statustext: Installing..." -Encoding UTF8
    Add-Content $commandFile "progresstext: Installing $($pkg.Name) ($($i + 1) of $totalPkgs)..." -Encoding UTF8
    
    # Simulate installation time
    Start-Sleep -Seconds $pkg.Duration
    
    # Mark complete
    Add-Content $commandFile "listitem: update, title: $($pkg.Name), status: success, statustext: Installed" -Encoding UTF8
    Add-Content $commandFile "progress: $percent" -Encoding UTF8
}

# Finish
Add-Content $commandFile "message: All software has been installed successfully!" -Encoding UTF8
Add-Content $commandFile "progresstext: Installation complete" -Encoding UTF8
Start-Sleep -Seconds 3

Add-Content $commandFile "quit" -Encoding UTF8

Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
Remove-Item $commandFile -Force -ErrorAction SilentlyContinue

Write-Host "Software installation completed"

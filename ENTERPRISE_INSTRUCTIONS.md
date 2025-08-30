# Enterprise Environment Instructions - ALWAYS REFERENCE

## 🔒 CRITICAL SECURITY REQUIREMENT
**ALL BINARIES MUST BE SIGNED BEFORE EXECUTION**

This enterprise environment **DOES NOT ALLOW** unsigned binaries to execute. Any testing, building, or deployment MUST include code signing.

---

## Code Signing Requirements

### Certificate Information
- **Certificate Name**: `EmilyCarrU Intune Windows Enterprise Certificate`
- **Timestamp URL**: `http://timestamp.sectigo.com`
- **Tool Required**: `signtool.exe` (Windows SDK)

### Signing Script Location
**File**: `sign-build.ps1` (root directory)

### Usage Examples
```powershell
# Build and sign everything
.\sign-build.ps1 -All

# Build only (still need to sign before running)
.\sign-build.ps1 -Build

# Sign existing binaries
.\sign-build.ps1 -Sign
```

---

## Binary Locations (Default Debug Build)
- **CLI**: `src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe`
- **WPF**: `src\csharpDialog.WPF\bin\Debug\net9.0-windows\csharpDialog.WPF.exe`

---

## Development Workflow - MANDATORY STEPS

### For Any Code Changes:
1. **Build** the solution:
   ```powershell
   dotnet build
   ```

2. **Sign** the binaries:
   ```powershell
   .\sign-build.ps1 -Sign
   ```

3. **Then** run tests or execute binaries

### For Testing New Features:
1. **Build and Sign** in one step:
   ```powershell
   .\sign-build.ps1 -All
   ```

2. **Verify signing** (optional):
   ```powershell
   signtool verify /pa "path\to\binary.exe"
   ```

3. **Execute** tests or applications

### For Creating Test Applications:
- Any new test executables MUST be added to the signing script
- Update `$filesToSign` array in `sign-build.ps1`
- Always sign before attempting to run

---

## What Happens If You Don't Sign
- ❌ **Application will NOT execute**
- ❌ **Windows will block the binary**
- ❌ **Enterprise security policies prevent execution**
- ❌ **Tests will fail immediately**

---

## Quick Commands Reference

### Build & Sign Everything
```powershell
.\sign-build.ps1 -All
```

### Test After Signing
```powershell
# CLI test
.\src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe --help

# WPF test
.\src\csharpDialog.WPF\bin\Debug\net9.0-windows\csharpDialog.WPF.exe --title "Test"
```

### Verify Code Signature
```powershell
signtool verify /pa "src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe"
```

---

## For New Test Projects

### Adding to Signing Script
When creating new test executables (like CommandFileTest), add them to the signing script:

```powershell
# Add to $filesToSign array in sign-build.ps1
$testExe = Join-Path $rootPath "src\CommandFileTest\bin\Debug\net9.0\CommandFileTest.exe"
if (Test-Path $testExe) {
    $filesToSign += $testExe
}
```

### Complete Test Workflow
```powershell
# 1. Build the test project
dotnet build .\src\CommandFileTest\CommandFileTest.csproj

# 2. Sign the binary (update script first if needed)
.\sign-build.ps1 -Sign

# 3. Run the test
.\src\CommandFileTest\bin\Debug\net9.0\CommandFileTest.exe
```

---

## Environment Dependencies

### Required Tools
- ✅ **signtool.exe** (Windows SDK)
- ✅ **Enterprise certificate installed**
- ✅ **PowerShell execution policy allows scripts**
- ✅ **.NET 9.0 SDK**

### Certificate Verification
Check if certificate is available:
```powershell
Get-ChildItem -Path "Cert:\CurrentUser\My" | Where-Object {$_.Subject -like "*EmilyCarrU*"}
```

---

## Troubleshooting

### If Signing Fails
1. **Check certificate installation**
2. **Verify signtool.exe is in PATH**
3. **Run PowerShell as Administrator** (if needed)
4. **Check timestamp server accessibility**

### If Execution Still Fails After Signing
1. **Verify signature**:
   ```powershell
   signtool verify /pa "path\to\binary.exe"
   ```
2. **Check Windows Security policies**
3. **Try running as Administrator**

### Common Errors
- `'signtool' is not recognized` → Install Windows SDK
- `No certificates were found` → Certificate not installed
- `Access denied` → Run as Administrator

---

## IMPORTANT REMINDERS

### ⚠️ NEVER attempt to run unsigned binaries
### ⚠️ ALWAYS sign after building
### ⚠️ UPDATE signing script when adding new executables
### ⚠️ VERIFY signing worked before reporting issues

---

## File References
- **Signing Script**: `sign-build.ps1`
- **CLI Binary**: `src\csharpDialog.CLI\bin\Debug\net9.0\csharpDialog.CLI.exe`
- **WPF Binary**: `src\csharpDialog.WPF\bin\Debug\net9.0-windows\csharpDialog.WPF.exe`
- **This File**: `ENTERPRISE_INSTRUCTIONS.md`

---

*Last Updated: August 28, 2025*
*This document must be referenced for ALL development activities*

# Workspace Handoff – csharpDialog + Cimian

This note captures the current state of the csharpDialog → Cimian integration work so it’s easy to pick up in a combined workspace that includes both repositories.

## Current Status

- **csharpDialog build script**
  - Auto-discovers `signtool.exe`, falls back gracefully if the Windows SDK is missing.
  - New `-SkipMsi` and `-SkipPkg` switches allow packaging to be selectively disabled.
  - Default runtimes now cover both `win-x64` and `win-arm64`; each publish pass signs the binaries.
  - MSI generation is still disabled because WiX v6 CLI is not installed locally (see Next Steps).
- **Artifacts**
  - Latest builds generated `.pkg` archives for both architectures under `dist/`:
    - `csharpdialog-x64-2025.10.12.1132.pkg`
    - `csharpdialog-arm64-2025.10.12.1132.pkg`
  - Arm64 package was installed via `sudo installer` (Cimian tooling) and laid down files in `C:\Program Files\csharpDialog`.
  - Postinstall script successfully appended the install directory to the machine PATH (requires new shells to take effect).
- **Manual verification**
  - `dialog.exe` launches (runs the default prompt) when invoked directly: `& 'C:\Program Files\csharpDialog\dialog.exe' --version`.
  - `managedsoftwareupdate.exe --manifest ProvisioningStaff --checkonly` shows pending items (Teams, Zoom, FortiClient etc.).
  - Cimian version on the test machine: `2025.10.10.1224`.
- **Documentation**
  - `docs/Cimian-Integration.md` now includes a script-only manifest example that drives `managedsoftwareupdate` while feeding progress to csharpDialog and logging to the standard Cimian `/cache` and `/logs` locations.

## Immediate Next Steps (when both repos are open)

1. **Install WiX CLI if MSI packaging is required**
   ```powershell
   dotnet tool install --global wix
   ```
   - Update PATH or re-open the terminal so the `wix` command is available.
   - Re-run `build.ps1` without `-SkipMsi` to verify MSI creation.

2. **Create Cimian script-only item**
   - Copy the new script snippet from `docs/Cimian-Integration.md` into the Cimian repo, e.g. `deployment/pkgsinfo/scripts/csharpDialog-Progress.ps1`.
   - Add a corresponding script-only YAML entry (sample provided in the doc) that calls the script for the desired manifest (ProvisioningStaff).
   - Ensure the manifest references the script item so Cimian executes it before/while other installs run.

3. **End-to-end validation**
   - Open a fresh elevated PowerShell (PATH includes `C:\Program Files\csharpDialog`).
   - Trigger the script-only item via `managedsoftwareupdate` (e.g. `--manifest ProvisioningStaff --checkonly` followed by `--runall` or `--item`).
   - Confirm csharpDialog presents progress, updates statuses, and exits once Cimian finishes.
   - Review logs in `C:\ProgramData\ManagedInstalls\Logs\csharpDialog-progress-*.log`.

4. **Optional polish**
   - Adjust dialog messaging/branding (title, message, fullscreen/kiosk flags) after UI review.
   - Wire in icon support if the manifest items expose `icon_name` fields (see existing icon management doc).

## Useful Commands

```powershell
# Build and sign both runtimes, skip MSI and .pkg if desired
pwsh -NoProfile -File .\build.ps1 -SkipMsi -SkipPkg

# Build everything including packaging (requires WiX for MSI)
pwsh -NoProfile -File .\build.ps1

# Inspect installed binaries
Get-ChildItem "C:\Program Files\csharpDialog"

# Verify managedsoftwareupdate is reachable
& "C:\Program Files\Cimian\managedsoftwareupdate.exe" --version

# Simulate manifest analysis
& "C:\Program Files\Cimian\managedsoftwareupdate.exe" --manifest ProvisioningStaff --checkonly
```

## Repository Notes

- **csharpDialog**
  - Solution root: `csharpdialog/CsharpDialog.sln`.
  - CLI project: `src/csharpDialog.CLI`.
  - Build script: `build.ps1` (supports `-Build`, `-Sign`, `-Msi`, `-Pkg`, `-All`, `-SkipMsi`, `-SkipPkg`, `-Runtime`).

- **Cimian** (external repo to be opened in the new workspace)
  - Script-only packages typically live under `deployment/pkgsinfo/scripts/`.
  - Manifests reference script items via `installer: { type: script, path: ... }`.
  - Cimian client caches live under `C:\ProgramData\ManagedInstalls\` (logs, cache, icons).

## Open Questions

- Do we need MSI packaging on the build agent, or is `.pkg` sufficient for deployment pipelines?
- Should the script-only item enforce fullscreen/kiosk dialog usage, or leave that to manifest parameters?
- Are there additional manifests beyond `ProvisioningStaff` that should be monitored simultaneously?

Capture answers/decisions in this doc or the main integration doc so future iterations stay aligned.

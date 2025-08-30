# csharpDialog Feature Parity Action Plan

## Overview
This document outlines the implementation plan to achieve feature parity with swiftDialog, focusing on the advanced scripting capabilities and dynamic UI updates that are currently missing from csharpDialog.

## Analysis Summary
Based on comprehensive analysis of swiftDialog source code, the following critical features are missing:

### Critical Missing Features
1. **Command File Monitoring System** - Real-time script control
2. **Dynamic List Item Status Tracking** - Progress per item with status checks
3. **Interactive Progress Bar Controls** - Script-driven progress updates
4. **Advanced JSON Configuration** - Complex nested configurations
5. **Shell Command Execution** - Button actions and script integration

---

## Implementation Phases

### Phase 1: Command File Monitoring System (HIGH PRIORITY)
**Goal**: Implement real-time command file monitoring for script control

#### Tasks:
- [ ] **1.1 Create CommandFileMonitor Class**
  - Location: `src/csharpdialog.Core/Services/CommandFileMonitor.cs`
  - Use `FileSystemWatcher` for real-time monitoring
  - Default file: `%TEMP%\csharpdialog.log`
  - Thread-safe command processing

- [ ] **1.2 Implement Command Parser**
  - Location: `src/csharpdialog.Core/Services/CommandParser.cs`
  - Support basic commands:
    ```
    title: <text>
    message: <text>
    progress: <int>
    progresstext: <text>
    quit:
    ```

- [ ] **1.3 Integrate with Dialog Services**
  - Update `IDialogService` interface
  - Add command processing to WPF and CLI implementations
  - Real-time UI updates via data binding

- [ ] **1.4 Add Command Line Option**
  - `--commandfile <path>` option
  - Default to `%TEMP%\csharpdialog.log`
  - File creation and permission handling

#### Acceptance Criteria:
- External scripts can update dialog in real-time
- Command file monitoring starts automatically
- Basic commands (title, message, progress, quit) working
- Cross-platform file monitoring (Windows focus)

---

### Phase 2: Dynamic List Items (HIGH PRIORITY)
**Goal**: Implement dynamic list items with status tracking

#### Tasks:
- [ ] **2.1 Extend ListItemConfiguration**
  - Add `Status` property (wait, success, fail, error, pending, progress)
  - Add `StatusText` property for detailed status
  - Add `Icon` property for custom icons

- [ ] **2.2 Create Status Icon System**
  - Status icon resources (✓, ✗, ⏳, ⚠️, 🔄)
  - Icon mapping for each status type
  - Custom icon support via file paths

- [ ] **2.3 Implement List Update Commands**
  - Command syntax:
    ```
    listitem: title: <title>, status: <status>, statustext: <text>
    listitem: index: <index>, status: <status>, statustext: <text>
    listitem: add: title: <text>, status: <status>
    listitem: delete: index: <index>
    list: clear
    ```

- [ ] **2.4 Real-time List UI Updates**
  - ObservableCollection for list items
  - Data binding for status changes
  - Animation for status transitions

#### Acceptance Criteria:
- Scripts can update individual list items
- Status icons display correctly
- Real-time UI updates without flicker
- Support for add/delete operations

---

### Phase 3: Enhanced Progress Controls (MEDIUM PRIORITY)
**Goal**: Script-driven progress bar controls

#### Tasks:
- [ ] **3.1 Command-Driven Progress Updates**
  - Commands:
    ```
    progress: <percentage>
    progress: increment <value>
    progress: reset
    progresstext: <text>
    ```

- [ ] **3.2 Progress Text Integration**
  - Dynamic progress text updates
  - Text positioning below progress bar
  - Rich text support (optional)

- [ ] **3.3 Progress Animation**
  - Smooth progress transitions
  - Optional indeterminate mode
  - Progress completion callbacks

#### Acceptance Criteria:
- Scripts can control progress bar precisely
- Smooth animations for progress changes
- Progress text updates independently

---

### Phase 4: Advanced JSON Configuration (MEDIUM PRIORITY)
**Goal**: Comprehensive JSON configuration support

#### Tasks:
- [ ] **4.1 Enhanced JSON Schema**
  - Complex nested configurations
  - Array support for multiple items
  - Validation and error handling

- [ ] **4.2 JSON Command Support**
  - Load configuration from JSON file
  - Runtime JSON updates via command file
  - Merge JSON with command line options

- [ ] **4.3 Configuration Validation**
  - JSON schema validation
  - Error reporting for invalid configs
  - Default value handling

#### Acceptance Criteria:
- Complex dialogs definable via JSON
- Runtime configuration updates
- Robust error handling

---

### Phase 5: Shell Integration (LOWER PRIORITY)
**Goal**: Shell command execution and system integration

#### Tasks:
- [ ] **5.1 Button Action Execution**
  - Shell command execution for button actions
  - URL opening support
  - Security considerations

- [ ] **5.2 Environment Variable Support**
  - `CSHARPDIALOG_AUTH_KEY` environment variable
  - System information variables
  - Variable substitution in text

- [ ] **5.3 Process Integration**
  - Return codes for script automation
  - Standard output capture
  - Timeout handling

#### Acceptance Criteria:
- Buttons can execute shell commands
- Environment variables work correctly
- Secure execution with proper permissions

---

## Technical Architecture

### Core Components
```
csharpdialog.Core/
├── Services/
│   ├── ICommandFileMonitor.cs
│   ├── CommandFileMonitor.cs
│   ├── ICommandParser.cs
│   ├── CommandParser.cs
│   └── IShellExecutor.cs
├── Models/
│   ├── Command.cs
│   ├── ListItemStatus.cs
│   └── ProgressUpdate.cs
└── Extensions/
    └── DialogServiceExtensions.cs
```

### Command File Format
```
# Basic commands
title: Updated Dialog Title
message: Processing items...
progress: 50
progresstext: Step 2 of 4 complete

# List item commands
listitem: title: Install Software, status: success, statustext: Completed
listitem: title: Configure Settings, status: progress, statustext: In progress...
listitem: title: Restart Required, status: pending, statustext: Waiting

# Control commands
quit:
```

### Integration Points
- **WPF**: Real-time UI updates via data binding
- **CLI**: Status output to console
- **Core**: Shared command processing logic

---

## Testing Strategy

### Unit Tests
- Command parser validation
- File monitoring functionality
- Status update logic

### Integration Tests
- End-to-end command file processing
- UI update verification
- Cross-platform compatibility

### Manual Testing Scripts
- PowerShell test scripts for common scenarios
- Performance testing with large lists
- Error condition testing

---

## Success Metrics

### Functional Goals
- [ ] Scripts can control dialog in real-time
- [ ] List items update with status indicators
- [ ] Progress bars respond to script commands
- [ ] JSON configurations work as expected
- [ ] Shell integration functions securely

### Performance Goals
- [ ] Command processing < 100ms latency
- [ ] UI updates appear smooth (60fps)
- [ ] Memory usage remains stable during long operations
- [ ] File monitoring has minimal CPU impact

### Compatibility Goals
- [ ] Works on Windows 10/11
- [ ] PowerShell 5.1+ compatibility
- [ ] .NET 9.0 framework support
- [ ] Enterprise security compliance

---

## Implementation Timeline

| Phase | Duration | Dependencies | Priority |
|-------|----------|--------------|----------|
| Phase 1 | 2-3 weeks | None | Critical |
| Phase 2 | 2-3 weeks | Phase 1 | Critical |
| Phase 3 | 1-2 weeks | Phase 1 | Medium |
| Phase 4 | 2-3 weeks | Phase 1 | Medium |
| Phase 5 | 1-2 weeks | All phases | Low |

**Total Estimated Duration**: 8-13 weeks

---

## Next Steps

### Immediate Actions (This Week)
1. **Start Phase 1.1**: Create CommandFileMonitor class
2. **Set up testing framework**: Unit tests for command processing
3. **Design command syntax**: Finalize command format specification

### Week 2-3 Goals
1. **Complete Phase 1**: Basic command file monitoring
2. **Begin Phase 2**: List item status system
3. **Create demo scripts**: PowerShell examples for testing

### Month 1 Target
1. **Phases 1-2 Complete**: Core scripting functionality working
2. **Documentation**: Usage examples and API reference
3. **Community feedback**: Share progress and gather input

---

## References

### swiftDialog Command Reference
- Command file location: `/var/tmp/dialog.log`
- Status types: `wait`, `success`, `fail`, `error`, `pending`, `progress`
- Progress commands: `progress: <int>`, `progress: increment <value>`, `progress: reset`
- List commands: `listitem: title: <title>, status: <status>, statustext: <text>`

### Technical Resources
- .NET FileSystemWatcher documentation
- WPF data binding best practices
- PowerShell integration patterns
- Enterprise security guidelines

---

*This action plan will be updated as implementation progresses and requirements evolve.*

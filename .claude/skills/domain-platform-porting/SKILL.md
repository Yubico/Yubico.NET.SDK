---
name: platform-porting-template
description: Use when planning platform ports - reusable prompt template for Ralph Loop autonomous porting
---

# Platform Porting Template

Reusable template for creating Ralph Loop prompts that autonomously port platform-specific functionality across Windows, macOS, and Linux.

**Core principle:** Standardize Ralph Loop prompts for platform work so autonomous agents have clear reference implementations, critical patterns, and verification criteria.

## Use when

**Use this skill when:**
- Planning a Ralph Loop for porting device support (HID, NFC, Bluetooth)
- Adding new transport types to a new platform
- Need to guide an autonomous agent through multi-platform implementation
- Creating platform-specific P/Invoke or device connection code

**Don't use when:**
- Making ad-hoc platform changes (just use pinvoke-porting skill)
- Non-platform work (use domain-specific skills)
- One-off experiments (use workflow-experiment instead)

## Template Structure

Use this template by copying and customizing each section:

```markdown
# Port {Feature} to {Platform}

**Goal:** {One sentence describing what will be enabled}

**Platform Scope:** {Platform} only. See `docs/plans/{other-platforms}.md` for other platforms.

---

## ⚠️ CRITICAL POLICIES (READ FIRST)

### Hardware Tests
Try first, but if failure due to contention, Do NOT block on hardware operations. Create test structure, document requirements, proceed with build verification.

### Git Commits
Only commit files YOU create or modify. Do NOT use `git add .` or `git add -A`.
See `docs/COMMIT_GUIDELINES.md`.

---

## Reference Implementations

### Legacy C# SDK
- **Location:** `./legacy-develop/Yubico.Core/src/Yubico/Core/Devices/{Feature}/`
- **Key files:**
  - `{Platform}{Feature}Device.cs` - Device enumeration
  - `{Platform}{Feature}Connection.cs` - Connection implementation
- **P/Invoke:** `./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/{Platform}/`

### Current SDK
- **Existing P/Invoke:** `Yubico.YubiKit.Core/src/PlatformInterop/{Platform}/`
- **Existing interfaces:** `Yubico.YubiKit.Core/src/{Feature}/I{Feature}Device.cs`

---

## CLAUDE.md Guidelines Summary

**Memory Management:**
- ✅ Sync + ≤512 bytes → `Span<byte>` with `stackalloc`
- ✅ Sync + >512 bytes → `ArrayPool<byte>.Shared.Rent()`
- ✅ Async → `Memory<byte>` or `IMemoryOwner<byte>`

**Code Quality:**
- ✅ File-scoped namespaces
- ✅ `is null` / `is not null`
- ✅ Switch expressions
- ❌ No `#region` blocks
- ❌ No `.ToArray()` unless data must escape scope

**Build/Test:**
- `dotnet build.cs build`
- `dotnet build.cs test`

**Git:**
- Only commit YOUR files explicitly
- Never `git add .` or `git add -A`

---

## Critical Patterns to PRESERVE

From legacy - DO NOT modify these patterns:

1. **GCHandle Pinning** - Buffers in callbacks MUST be pinned
2. **Delegate Storage** - Callback delegates MUST be instance fields
3. **CFRunLoop Timeouts** - Preserve empirically tested timeout values
4. **{Platform-specific pattern}** - {Description and reason}

---

## Tasks

### Task 1: Port {Platform}{Feature}Device
**Reference:** `legacy-develop/.../Platform}{Feature}Device.cs`
**Create:** `Yubico.YubiKit.Core/src/{Feature}/{Platform}{Feature}Device.cs`

**Key implementation:**
- {Key point 1}
- {Key point 2}

**Success criteria:**
- [ ] Implements `I{Feature}Device`
- [ ] Build passes

**Commit:** `feat({feature}): add {Platform}{Feature}Device`

---

### Task 2: Port {Platform}{Feature}Connection
**Reference:** `legacy-develop/.../Platform}{Feature}Connection.cs`
**Create:** `Yubico.YubiKit.Core/src/{Feature}/{Platform}{Feature}Connection.cs`

**Critical patterns:**
- {Safety-critical pattern to preserve}

**Success criteria:**
- [ ] Implements `I{Feature}Connection`
- [ ] Build passes

**Commit:** `feat({feature}): add {Platform}{Feature}Connection`

---

### Task 3: Update Find{Feature}Devices
**Modify:** `Yubico.YubiKit.Core/src/{Feature}/Find{Feature}Devices.cs`

**Changes:**
```csharp
if (OperatingSystem.Is{Platform}())
    return FindAll{Platform}Async(cancellationToken);
```

**Commit:** `feat({feature}): add {Platform} support to Find{Feature}Devices`

---

### Task 4: Add Unit Tests
**Create:** `Yubico.YubiKit.Core.UnitTests/{Feature}/{Platform}{Feature}Tests.cs`

**Tests:**
- Device enumeration (mock or real)
- Connection creation
- Error handling

**Commit:** `test({feature}): add {Platform} unit tests`

---

### Task 5: Update Documentation
**Modify:** `docs/{feature}.md`

**Commit:** `docs({feature}): add {Platform} support documentation`

---

## Build & Test Commands

```bash
# Build
dotnet build.cs build

# Test all
dotnet build.cs test

# Test specific project
dotnet build.cs test --project {Feature}

# Test with filter
dotnet build.cs test --filter "FullyQualifiedName~{Platform}"
```

---

## Verification Checklist

- [ ] `dotnet build.cs build` exits with code 0
- [ ] `dotnet build.cs test` shows all tests passing
- [ ] New code has `[SupportedOSPlatform("{platform}")]` attribute
- [ ] No `#region` blocks
- [ ] Uses `is null` / `is not null`
- [ ] File-scoped namespaces
- [ ] Critical patterns preserved (GCHandle, delegates)

---

## Completion Requirements

**Only after ALL verification passes**, output:
```
<promise>DONE</promise>
```

**If any step fails:**
1. Read the error output
2. Fix the issue
3. Re-run the failing command
4. Do NOT output the promise until everything passes

---

## Hardware Test Policy

- Hardware tests MAY fail due to device state
- If a hardware test fails 2-3 times, document and skip
- Do NOT block completion on hardware tests
- Mark with `[Trait("RequiresHardware", "true")]`

---

## Example: Porting HID to Windows

For reference, here's a completed template instance for Windows HID support:

```markdown
# Port HID to Windows

**Goal:** Enable YubiKey FIDO2 and OTP via HID transport on Windows.

**Platform Scope:** Windows only. See `docs/plans/2026-01-09-add-hid-devices.md` for macOS.

...

## Reference Implementations

### Legacy C# SDK
- **Location:** `./legacy-develop/Yubico.Core/src/Yubico/Core/Devices/Hid/`
- **Key files:**
  - `WindowsHidDevice.cs` - SetupDi enumeration
  - `WindowsHidIOReportConnection.cs` - ReadFile/WriteFile
  - `WindowsHidFeatureReportConnection.cs` - HidD_GetFeature/SetFeature
- **P/Invoke:** `./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/Windows/`

...
```

## Notes

- Always reference existing implementations (PCSC, macOS HID) for consistency
- Platform-specific code goes in platform-named files
- Service layer code is platform-agnostic with runtime checks
- DI registration happens once per service, not per platform

## Related Skills

- `pinvoke-porting` - Detailed guide for P/Invoke porting patterns
- `agent-ralph-loop` - Launching autonomous agents with this template
- `workflow-plan` - Creating implementation plans

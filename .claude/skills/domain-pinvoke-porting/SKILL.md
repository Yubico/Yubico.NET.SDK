---
name: pinvoke-porting
description: Use when porting platform-specific P/Invoke code - legacy to modern C# with safety patterns
---

# Platform P/Invoke Porting

Specialized skill for porting legacy platform-specific P/Invoke code to modern C# SDK with safety and pattern updates.

**Core principle:** Platform interop must preserve safety-critical patterns (GCHandle pinning, delegate storage, CFRunLoop timeouts) while modernizing to file-scoped namespaces and C# 14 patterns.

## Use when

**Use this skill when:**
- Porting HID, NFC, Bluetooth, or device-specific P/Invoke from legacy SDK
- Adding platform interop for Windows/macOS/Linux
- Implementing platform-specific device connections (e.g., MacOSHidIOReportConnection)
- Moving code from `./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/` to modern SDK

**Don't use when:**
- Creating non-platform code (use standard refactoring instead)
- Adding business logic above platform layer (use domain-specific skills)
- Writing new protocols or specifications

## Identify Sources

```
┌─────────────────────────────────────────────────────────┐
│  REFERENCE LOCATIONS                                    │
│                                                         │
│  Legacy C# SDK:                                         │
│    ./legacy-develop/Yubico.Core/src/Yubico/Core/...    │
│    ./legacy-develop/Yubico.Core/src/Yubico/PlatformInterop/...│
│                                                         │
│  Java SDK (protocol logic):                             │
│    ../yubikit-android/                                  │
│                                                         │
│  Current SDK P/Invoke:                                  │
│    Yubico.YubiKit.Core/src/PlatformInterop/{Platform}/ │
└─────────────────────────────────────────────────────────┘
```

## Check Existing Infrastructure

Before creating new P/Invoke, verify what exists:

```bash
# Check existing platform interop
ls Yubico.YubiKit.Core/src/PlatformInterop/{MacOS,Windows,Linux}/

# Search for existing signatures
grep -r "DllImport" Yubico.YubiKit.Core/src/PlatformInterop/
```

**DO NOT duplicate existing P/Invoke signatures.**

## Critical Patterns to PRESERVE

These patterns are **safety-critical** and must NOT be changed:

### GCHandle Pinning for Callbacks

```csharp
// REQUIRED: Pin buffers passed to native callbacks
private GCHandle _bufferHandle;
private byte[] _buffer;

public void Initialize()
{
    _buffer = new byte[64];
    _bufferHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
}

public void Dispose()
{
    if (_bufferHandle.IsAllocated)
        _bufferHandle.Free();
}
```

### Delegate Field Storage (Prevent GC Collection)

```csharp
// REQUIRED: Store delegate as field to prevent GC collection
private readonly IOHIDReportCallback _callback;

public MyConnection()
{
    // Delegate MUST be stored as field, not inline lambda
    _callback = new IOHIDReportCallback(OnReport);
    NativeMethod(_callback); // Safe - delegate won't be collected
}

// ❌ WRONG - delegate may be collected before native code calls it:
// NativeMethod(new IOHIDReportCallback(OnReport));
```

### CFRunLoop/Event Loop Patterns

```csharp
// Preserve timeout values from legacy (empirically tested)
private const double RunLoopTimeout = 6.0; // seconds

// Pattern: Schedule -> Run -> Unschedule
IOHIDDeviceScheduleWithRunLoop(device, runLoop, mode);
try
{
    CFRunLoopRunInMode(mode, RunLoopTimeout, returnAfterSourceHandled: true);
}
finally
{
    IOHIDDeviceUnscheduleFromRunLoop(device, runLoop, mode);
}
```

## Modernization REQUIRED

These updates ARE required:

| Legacy Pattern | Modern Pattern | Reason |
|---|---|---|
| `namespace X.Y.Z { }` | `namespace X.Y.Z;` | File-scoped (C# 11+) |
| `#region ... #endregion` | Remove entirely | Modern IDE supports collapsing |
| `== null` / `!= null` | `is null` / `is not null` | Modern pattern |
| `if/else` chains | Switch expressions | Cleaner, more maintainable |
| Manual null checks | `ArgumentNullException.ThrowIfNull()` | Standard helper |
| `throw new ObjectDisposedException()` | `ObjectDisposedException.ThrowIf(_disposed, this)` | Standard helper |

## Platform Attributes

Always add platform support attributes:

```csharp
using System.Runtime.Versioning;

[SupportedOSPlatform("macos")]
public sealed class MacOSHidDevice : IHidDevice
{
    // ...
}

[SupportedOSPlatform("windows")]
public sealed class WindowsHidDevice : IHidDevice
{
    // ...
}

[SupportedOSPlatform("linux")]
public sealed class LinuxHidDevice : IHidDevice
{
    // ...
}
```

## Port Workflow

1. **Analyze legacy code** - Read the full legacy implementation
2. **Check existing P/Invoke** - Don't duplicate signatures
3. **Preserve safety patterns** - Keep GCHandle, delegates, timeouts
4. **Apply modernization** - File-scoped namespace, `is null`, switch expressions
5. **Add platform attributes** - `[SupportedOSPlatform(...)]`
6. **Verify build** - `dotnet build.cs build` must pass
7. **Commit carefully** - Only YOUR modified files

### Verification

After porting:

```bash
# Build to catch compilation errors
dotnet build.cs build

# Check for warnings
dotnet build.cs build 2>&1 | grep -i warning
```

## Example: Porting MacOSHidDevice

### Legacy Code
```csharp
// legacy-develop/.../MacOSHidDevice.cs
namespace Yubico.Core.Devices.Hid
{
    public class MacOSHidDevice : IHidDevice
    {
        #region Properties
        public int VendorId { get; }
        // ...
        #endregion

        public static IEnumerable<IHidDevice> GetList()
        {
            // IOKit enumeration...
        }
    }
}
```

### Modern Ported Code
```csharp
// Yubico.YubiKit.Core/src/Hid/MacOSHidDevice.cs
using System.Runtime.Versioning;

namespace Yubico.YubiKit.Core.Hid;

[SupportedOSPlatform("macos")]
public sealed class MacOSHidDevice : IHidDevice
{
    public int VendorId { get; }
    // ... (no #region)

    public static IReadOnlyList<IHidDevice> GetList()
    {
        // Same IOKit logic, modern return type
    }
}
```

## Commit Pattern

```bash
# Platform layer commits
git add Yubico.YubiKit.Core/src/Hid/MacOSHidDevice.cs
git commit -m "feat(hid): add MacOSHidDevice ported from legacy SDK"

git add Yubico.YubiKit.Core/src/Hid/MacOSHidIOReportConnection.cs
git commit -m "feat(hid): add MacOSHidIOReportConnection for FIDO HID"
```

## Verification Checklist

Before completing a platform port:

- [ ] Checked existing P/Invoke (no duplication)
- [ ] GCHandle pinning preserved for callbacks
- [ ] Delegate fields stored (not inline)
- [ ] CFRunLoop timeouts preserved
- [ ] File-scoped namespace
- [ ] No `#region` blocks
- [ ] `is null` / `is not null` syntax
- [ ] `[SupportedOSPlatform]` attribute added
- [ ] `ObjectDisposedException.ThrowIf` used
- [ ] Build passes with no errors
- [ ] Commit message follows convention

## Related Skills

- `domain-build` - Building and verifying the project
- `workflow-tdd` - Writing tests for new platform implementations
- `review-request` - Getting code review before merging

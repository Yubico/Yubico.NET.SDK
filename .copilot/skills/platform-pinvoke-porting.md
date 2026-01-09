# Platform P/Invoke Porting Skill

Specialized skill for porting legacy platform-specific P/Invoke code to modern C# SDK with safety and pattern updates.

## When to Use

Triggers:
- "Port {class} from legacy SDK"
- "Add platform interop for {feature}"
- "Implement {feature} for macOS/Windows/Linux"
- Porting HID, NFC, Bluetooth, or other platform-specific device code

## Workflow

### 1. Identify Sources

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

### 2. Check Existing Infrastructure

Before creating new P/Invoke, verify what exists:

```bash
# Check existing platform interop
ls Yubico.YubiKit.Core/src/PlatformInterop/{MacOS,Windows,Linux}/

# Search for existing signatures
grep -r "DllImport" Yubico.YubiKit.Core/src/PlatformInterop/
```

**DO NOT duplicate existing P/Invoke signatures.**

### 3. Critical Patterns to PRESERVE

These patterns are safety-critical and must NOT be changed:

#### GCHandle Pinning for Callbacks
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

#### Delegate Field Storage (Prevent GC Collection)
```csharp
// REQUIRED: Store delegate as field to prevent GC collection
private readonly IOHIDReportCallback _callback;

public MyConnection()
{
    // Delegate MUST be stored as field, not inline lambda
    _callback = new IOHIDReportCallback(OnReport);
    NativeMethod(_callback); // Safe - delegate won't be collected
}

// WRONG - delegate may be collected before native code calls it:
// NativeMethod(new IOHIDReportCallback(OnReport));
```

#### CFRunLoop/Event Loop Patterns
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

### 4. Modernization REQUIRED

These updates ARE required:

| Legacy Pattern | Modern Pattern |
|----------------|----------------|
| `namespace X.Y.Z { }` | `namespace X.Y.Z;` (file-scoped) |
| `#region ... #endregion` | Remove entirely |
| `== null` / `!= null` | `is null` / `is not null` |
| `if/else` chains | Switch expressions where appropriate |
| Manual null checks | `ArgumentNullException.ThrowIfNull()` |
| `throw new ObjectDisposedException()` | `ObjectDisposedException.ThrowIf(_disposed, this)` |

### 5. Platform Attributes

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

### 6. Verification

After porting:

```bash
# Build to catch compilation errors
dotnet build.cs build

# Check for warnings
dotnet build.cs build 2>&1 | grep -i warning
```

### 7. Commit Pattern

```bash
# Platform layer commits
git add Yubico.YubiKit.Core/src/Hid/MacOSHidDevice.cs
git commit -m "feat(hid): add MacOSHidDevice ported from legacy SDK"

git add Yubico.YubiKit.Core/src/Hid/MacOSHidIOReportConnection.cs
git commit -m "feat(hid): add MacOSHidIOReportConnection for FIDO HID"
```

## Example: Porting MacOSHidDevice

### Legacy Code Analysis
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

## Checklist

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

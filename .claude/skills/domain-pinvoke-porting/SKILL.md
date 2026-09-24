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
- Moving code from the legacy SDK's `Yubico.Core/src/Yubico/PlatformInterop/` to the modern SDK

**Don't use when:**
- Creating non-platform code (use standard refactoring instead)
- Adding business logic above platform layer (use domain-specific skills)
- Writing new protocols or specifications

## Identify Sources

Paths inside this repo are repo-relative. The legacy C# SDK and the Java SDK are **separate
checkouts**, not subdirectories of this repo — locate them rather than assuming a relative path.

| Source | Location |
|---|---|
| Current SDK P/Invoke declarations | `src/Core/src/Native/{Windows,MacOS,Linux,Desktop}/` |
| Current managed transport callers | `src/Core/src/Transports/{Hid,SmartCard}/` |
| Legacy C# SDK | sibling checkout, e.g. `~/Code/y/Yubico.NET.SDK-Legacy/Yubico.Core/src/Yubico/{Core,PlatformInterop}/` |
| Java SDK (protocol logic) | sibling checkout, e.g. `~/Code/y/yubikit-android/` |

```bash
# Confirm the legacy checkout before relying on it
ls -d ~/Code/y/Yubico.NET.SDK-Legacy/Yubico.Core/src/Yubico/PlatformInterop
```

## Check Existing Infrastructure

Before creating new P/Invoke, verify what exists:

```bash
# Check existing platform interop
ls src/Core/src/Native/{MacOS,Windows,Linux,Desktop}/

# Search for existing signatures (both attribute forms)
grep -rn "LibraryImport\|DllImport" --include="*.cs" src/Core/src/Native/
```

**DO NOT duplicate existing P/Invoke signatures.**

## Declare With `LibraryImport` — `DllImport` Is Wrong

Every **new** native entry point uses `[LibraryImport]` on an `internal static partial` method in a
`partial` class. `[DllImport]`/`extern` emits a runtime-generated IL stub: invisible to trimming and
Native AOT, invisible to the marshalling source generator, and it accepts non-blittable signatures
silently that then corrupt memory at runtime.

```csharp
// ❌ WRONG
[DllImport(Libraries.NativeShims, CharSet = CharSet.Ansi, EntryPoint = "Native_Foo", SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
internal static extern int Foo(string name, bool flag);

// ✅ RIGHT
[LibraryImport(Libraries.NativeShims, EntryPoint = "Native_Foo",
    StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
internal static partial int Foo(string name, [MarshalAs(UnmanagedType.U1)] bool flag);
```

`[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]` stays — it is required by the
analyzer settings and is a Windows security control.

`SYSLIB1054` (the analyzer that suggests this conversion) is informational and is **not** escalated
to an error in `.editorconfig`. A clean build does not prove you used `LibraryImport`.

When porting a legacy file, convert its declarations as part of the port. Do not carry
`[DllImport]` forward verbatim. Full conversion-trap table (`CharSet.Ansi`, `bool`, `SafeHandle`,
string returns): `src/Core/CLAUDE.md` § Platform Interop Pattern.

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

### Native Callbacks

Every declaration in this repo is `[LibraryImport]`, so the source-generated marshaller applies
everywhere — and it does not marshal C# delegate types at all. There are two ways through that, and
they are not interchangeable.

**Default: use a function pointer.** The source-generated marshaller does not
marshal C# delegate types at all. Declare the parameter as `delegate* unmanaged[Cdecl]<...>` and
point it at a `static` `[UnmanagedCallersOnly]` method. Per-instance state travels through the
native `context` pointer, not through a closure. There is no delegate to keep alive, so no `GCHandle`
for the callback itself.

```csharp
// See Native/MacOS/HidInput/HidInput.Interop.cs + Transports/Hid/MacOS/MacOSFidoHidConnection.cs
[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
private static void OnReport(nint context, nint report, nuint length) { /* ... */ }
```

**Escape hatch when the callback needs instance state and the native `context` is unusable:**
declare the parameter as `IntPtr`, keep a delegate in a field, and pass
`Marshal.GetFunctionPointerForDelegate(_field)`. A static `[UnmanagedCallersOnly]` method can only
recover `this` through the `context` pointer; where an existing API passes `IntPtr.Zero` there, the
closure is the only route. The delegate is GC-tracked and the function pointer dies with it, so the
field is load-bearing — an inline instance is collectable while native code still holds the thunk.

```csharp
// See Transports/Hid/MacOS/MacOSHidDeviceListener.cs and MacOSHidIOReportConnection.cs
private IOHIDDeviceCallback? _arrivedCallbackDelegate;

_arrivedCallbackDelegate = DeviceArrivedCallback;                 // field, not inline lambda
IOHIDManagerRegisterDeviceMatchingCallback(
    manager, Marshal.GetFunctionPointerForDelegate(_arrivedCallbackDelegate), IntPtr.Zero);

// ❌ WRONG - delegate collected before native code calls it:
// ...(manager, Marshal.GetFunctionPointerForDelegate(new IOHIDDeviceCallback(OnArrived)), IntPtr.Zero);
```

Buffer pinning (`GCHandle.Alloc(..., GCHandleType.Pinned)`) is required in **both** cases — that is
about the data, not the callback.

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
| `[DllImport]` + `static extern` | `[LibraryImport]` + `static partial` | Source-generated marshalling; trim/AOT safe |
| Delegate callback parameter | `delegate* unmanaged[Cdecl]<...>` + `[UnmanagedCallersOnly]` | `LibraryImport` cannot marshal delegates |

## Platform Attributes

Always add platform support attributes:

```csharp
using System.Runtime.Versioning;

[SupportedOSPlatform("macos")]
public sealed class MacOSHidInterface : IHidInterface
{
    // ...
}

[SupportedOSPlatform("windows")]
public sealed class WindowsHidInterface : IHidInterface
{
    // ...
}

[SupportedOSPlatform("linux")]
public sealed class LinuxHidInterface : IHidInterface
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
6. **Verify build** - `dotnet toolchain.cs build` must pass
7. **Commit carefully** - Only YOUR modified files

### Verification

After porting:

```bash
# Build to catch compilation errors
dotnet toolchain.cs build

# Check for warnings
dotnet toolchain.cs build 2>&1 | grep -i warning
```

## Example: Porting MacOSHidInterface

### Legacy Code
```csharp
// <legacy-checkout>/Yubico.Core/src/Yubico/Core/Devices/Hid/MacOSHidDevice.cs
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
// src/Core/src/Transports/Hid/MacOS/MacOSHidInterface.cs
using System.Runtime.Versioning;

namespace Yubico.YubiKit.Core.Transports.Hid.MacOS;

[SupportedOSPlatform("macos")]
public sealed class MacOSHidInterface : IHidInterface
{
    public int VendorId { get; }
    // ... (no #region)

    public static IReadOnlyList<IHidInterface> GetList()
    {
        // Same IOKit logic, modern return type
    }
}
```

## Commit Pattern

```bash
# Platform layer commits
git add src/Core/src/Transports/Hid/MacOS/MacOSHidInterface.cs
git commit -m "feat(hid): add MacOSHidInterface ported from legacy SDK"

git add src/Core/src/Transports/Hid/MacOS/MacOSHidIOReportConnection.cs
git commit -m "feat(hid): add MacOSHidIOReportConnection for FIDO HID"
```

## Verification Checklist

Before completing a platform port:

- [ ] Checked existing P/Invoke (no duplication)
- [ ] New declarations use `[LibraryImport]` + `static partial` — **no new `[DllImport]`**
- [ ] `[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]` present
- [ ] Marshalling explicit: `StringMarshalling`, `[MarshalAs]` on `bool`, `SafeHandle` kept
- [ ] Callbacks are `delegate* unmanaged[Cdecl]` + `[UnmanagedCallersOnly]`, or `IntPtr` + a stored delegate field
- [ ] GCHandle pinning preserved for buffers
- [ ] Delegate fields stored (not inline) wherever `Marshal.GetFunctionPointerForDelegate` is used
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

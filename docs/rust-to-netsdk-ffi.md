# Rust-to-.NET SDK FFI: Technical Handover

**Date:** 2026-03-17
**From:** Dennis (SDK team)
**For:** Rust team
**Branch:** `feature/fido2-scp-support`

---

## TL;DR

The .NET SDK now supports FIDO2 over SCP03/SCP11. On firmware 5.8+, this works over both USB CCID and NFC. On pre-5.8 firmware, NFC is required. To expose this to Rust, we'd compile a NativeAOT shared library (`.dll`/`.so`/`.dylib`) with `[UnmanagedCallersOnly]` C-ABI exports. Rust links against it and calls functions like `fido2_session_open_scp03()` and `fido2_send_command()` — the .NET SDK handles all SCP encryption transparently.

**However**, this approach ships a 15-30 MB .NET runtime in the native binary, requires per-platform builds, GCHandle lifecycle management, and NativeAOT trimming workarounds. If the Rust team only needs an SCP-encrypted APDU pipe (not the full FIDO2 command set), implementing SCP03 directly in Rust (~500 lines using `aes`/`cmac` crates + `pcsc`) is likely simpler, smaller, and easier to maintain. See the **Feasibility assessment** section for the full tradeoff analysis.

---

## What this enables

The .NET YubiKey SDK now supports FIDO2 sessions over SCP03 and SCP11 secure channels. This document describes how to expose that capability to Rust via a NativeAOT-compiled shared library.

The end result: Rust sends cleartext FIDO2/CTAP2 APDUs across an FFI boundary. The .NET SDK handles SCP channel establishment, encryption, and key management transparently. Rust never touches SCP directly.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│  Rust Application                                   │
│                                                     │
│  extern "C" {                                       │
│      fn fido2_session_open_scp03(...) -> i32;       │
│      fn fido2_send_command(...) -> i32;             │
│  }                                                  │
└──────────────────┬──────────────────────────────────┘
                   │ FFI (C ABI)
                   │
┌──────────────────▼──────────────────────────────────┐
│  Yubico.YubiKey.NativeInterop                       │
│  (NativeAOT shared library)                         │
│                                                     │
│  [UnmanagedCallersOnly] static methods              │
│  GCHandle-based opaque handles                      │
│  Error codes (no exceptions across FFI)             │
└──────────────────┬──────────────────────────────────┘
                   │ .NET project reference
                   │
┌──────────────────▼──────────────────────────────────┐
│  Yubico.YubiKey SDK                                 │
│                                                     │
│  Fido2Session(device, ScpKeyParameters)             │
│  ScpConnection → SCP03/SCP11 handshake              │
│  Encrypted APDU transport                           │
└─────────────────────────────────────────────────────┘
```

---

## Transport support for FIDO2+SCP

FIDO2 over SCP support depends on firmware version:

| Transport | Firmware | FIDO2 interface | SCP possible? |
|-----------|----------|----------------|---------------|
| USB, no SCP | All | HID FIDO | N/A (works) |
| USB, with SCP | Pre-5.8 | CCID — FIDO2 AID not on CCID | **No** |
| USB, with SCP | 5.8+ | CCID — FIDO2 AID registered on CCID | **Yes** |
| NFC, no SCP | All | SmartCard | N/A (works) |
| NFC, with SCP | All | SmartCard | **Yes** |

**Why:** SCP is a SmartCard-layer protocol (ISO 7816 secure messaging) that requires CCID. On pre-5.8 firmware, the YubiKey exposes FIDO2 only on the HID interface over USB — not on CCID. On firmware 5.8+, the FIDO2 applet is registered on both HID and CCID over USB, so SCP works over USB. Over NFC, there is only one interface (ISO 14443 / SmartCard), so all applets — including FIDO2 — are selectable on all firmware versions.

**Implication for Rust:** On firmware 5.8+, FIDO2+SCP works over USB — no NFC reader required. On pre-5.8 firmware, FIDO2+SCP requires NFC. The Rust application should check the firmware version and transport to determine SCP availability.

---

## .NET NativeInterop project

### Project setup

```xml
<!-- Yubico.YubiKey.NativeInterop/Yubico.YubiKey.NativeInterop.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RootNamespace>Yubico.YubiKey.NativeInterop</RootNamespace>
    <AssemblyName>yubico_yubikey</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Yubico.YubiKey/src/Yubico.YubiKey.csproj" />
  </ItemGroup>
</Project>
```

### Publishing per platform

```bash
# Windows (produces yubico_yubikey.dll)
dotnet publish -r win-x64 -c Release

# Linux (produces yubico_yubikey.so)
dotnet publish -r linux-x64 -c Release

# macOS Intel (produces yubico_yubikey.dylib)
dotnet publish -r osx-x64 -c Release

# macOS Apple Silicon (produces yubico_yubikey.dylib)
dotnet publish -r osx-arm64 -c Release
```

Output lands in `bin/Release/net8.0/<rid>/native/`.

### NativeAOT prerequisites

- .NET 8 SDK
- Platform-specific AOT toolchain:
  - **Windows:** Visual Studio with C++ workload (for `link.exe`)
  - **Linux:** `clang`, `zlib1g-dev`
  - **macOS:** Xcode command line tools

---

## FFI surface (C ABI)

### Design principles

| Principle | Implementation |
|-----------|---------------|
| No exceptions across FFI | Every function returns `int` error code. `0` = success. |
| Opaque handles | Managed objects are pinned via `GCHandle`, returned as `void*`. Rust never dereferences them. |
| Caller-allocated buffers | Rust allocates output buffers, passes `(ptr, len)`. Avoids cross-allocator issues. |
| No strings | Use byte arrays. UTF-8 where text is needed. |
| Separate functions per SCP variant | Simpler than passing discriminated unions across FFI. |

### Error codes

```c
#define YUBIKEY_OK                  0
#define YUBIKEY_ERR_NULL_HANDLE    -1
#define YUBIKEY_ERR_NOT_FOUND      -2
#define YUBIKEY_ERR_CONNECTION     -3
#define YUBIKEY_ERR_COMMAND        -4
#define YUBIKEY_ERR_BUFFER_SMALL   -5
#define YUBIKEY_ERR_INVALID_ARG    -6
```

### Proposed API

#### Device enumeration

```c
// Returns the number of connected YubiKeys, or 0 on error.
int yubikey_count_devices(void);

// Opens a handle to the YubiKey at the given index.
// *out_handle receives an opaque device handle.
int yubikey_open_device(int index, void** out_handle);

// Gets the serial number of the device.
int yubikey_device_serial(void* device_handle, int* out_serial);

// Releases a device handle. Safe to call with NULL.
void yubikey_close_device(void* device_handle);
```

#### FIDO2 session lifecycle

```c
// Opens a FIDO2 session without SCP (plain HID for USB, SmartCard for NFC).
int fido2_session_open(void* device_handle, void** out_session);

// Opens a FIDO2 session with SCP03 using default keys.
// Works over NFC (all firmware) and USB CCID (firmware 5.8+).
int fido2_session_open_scp03(void* device_handle, void** out_session);

// Opens a FIDO2 session with custom SCP03 keys.
// Each key pointer must reference exactly 16 bytes.
// Works over NFC (all firmware) and USB CCID (firmware 5.8+).
int fido2_session_open_scp03_custom(
    void* device_handle,
    const uint8_t* channel_mac_key,   // 16 bytes
    const uint8_t* channel_enc_key,   // 16 bytes
    const uint8_t* data_enc_key,      // 16 bytes
    void** out_session);

// Closes and disposes a FIDO2 session. Safe to call with NULL.
void fido2_session_close(void* session_handle);
```

#### Commands

```c
// Sends a raw APDU through the (optionally SCP-encrypted) channel.
// The APDU is cleartext — the SDK encrypts it if SCP is active.
// Response is written to out_buffer. *out_len receives actual length.
int fido2_send_command(
    void* session_handle,
    const uint8_t* apdu,        // input APDU bytes
    int apdu_len,
    uint8_t* out_buffer,        // caller-allocated response buffer
    int buffer_len,
    int* out_len);              // actual response length

// Gets authenticator info (FIDO2 GetInfo command).
// Writes a UTF-8 comma-separated list of supported versions.
int fido2_get_info_versions(
    void* session_handle,
    uint8_t* out_buffer,
    int buffer_len,
    int* out_len);
```

---

## C# implementation reference

### Handle management pattern

```csharp
// Pinning a managed object for FFI
var session = new Fido2Session(device, Scp03KeyParameters.DefaultKey);
var handle = GCHandle.Alloc(session);
*outSession = GCHandle.ToIntPtr(handle);   // → void* in Rust

// Recovering it later
var session = (Fido2Session)GCHandle.FromIntPtr(sessionHandle).Target!;

// Releasing it
var handle = GCHandle.FromIntPtr(sessionHandle);
if (handle.Target is IDisposable d) d.Dispose();
handle.Free();
```

### [UnmanagedCallersOnly] method pattern

```csharp
[UnmanagedCallersOnly(EntryPoint = "fido2_session_open_scp03")]
public static int OpenFido2SessionScp03(IntPtr deviceHandle, IntPtr* outSession)
{
    try
    {
        if (deviceHandle == IntPtr.Zero) return ERR_NULL_HANDLE;

        var device = (IYubiKeyDevice)GCHandle.FromIntPtr(deviceHandle).Target!;
        var session = new Fido2Session(device, Scp03KeyParameters.DefaultKey);

        var sessionHandle = GCHandle.Alloc(session);
        *outSession = GCHandle.ToIntPtr(sessionHandle);
        return OK;
    }
    catch
    {
        return ERR_CONNECTION_FAILED;
    }
}
```

### Key constraint: AllowUnsafeBlocks

`[UnmanagedCallersOnly]` methods with pointer parameters (`IntPtr*`, `byte*`) require `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in the `.csproj`.

---

## Rust consumption

### Linking

```toml
# Cargo.toml
[build-dependencies]
# If using bindgen to auto-generate from a C header:
bindgen = "0.71"

# Or declare the link manually:
# No extra deps needed — just extern "C" declarations
```

```rust
// build.rs (if needed)
fn main() {
    // Point to the NativeAOT output directory
    println!("cargo:rustc-link-search=native=path/to/native/output");
    println!("cargo:rustc-link-lib=dylib=yubico_yubikey");
}
```

### Rust bindings

```rust
use std::ffi::c_void;
use std::os::raw::c_int;

const OK: c_int = 0;
const ERR_BUFFER_SMALL: c_int = -5;

extern "C" {
    fn yubikey_count_devices() -> c_int;
    fn yubikey_open_device(index: c_int, out_handle: *mut *mut c_void) -> c_int;
    fn yubikey_close_device(handle: *mut c_void);

    fn fido2_session_open_scp03(
        device: *mut c_void,
        out_session: *mut *mut c_void,
    ) -> c_int;
    fn fido2_session_close(session: *mut c_void);

    fn fido2_send_command(
        session: *mut c_void,
        apdu: *const u8,
        apdu_len: c_int,
        out_buf: *mut u8,
        buf_len: c_int,
        out_len: *mut c_int,
    ) -> c_int;

    fn fido2_get_info_versions(
        session: *mut c_void,
        out_buf: *mut u8,
        buf_len: c_int,
        out_len: *mut c_int,
    ) -> c_int;
}
```

### Safe Rust wrapper (suggested)

```rust
use std::ptr;

pub struct YubiKeyDevice {
    handle: *mut c_void,
}

impl YubiKeyDevice {
    pub fn open(index: i32) -> Result<Self, i32> {
        let mut handle: *mut c_void = ptr::null_mut();
        let rc = unsafe { yubikey_open_device(index, &mut handle) };
        if rc != OK { return Err(rc); }
        Ok(Self { handle })
    }
}

impl Drop for YubiKeyDevice {
    fn drop(&mut self) {
        unsafe { yubikey_close_device(self.handle); }
    }
}

pub struct Fido2Session {
    handle: *mut c_void,
}

impl Fido2Session {
    /// Opens a FIDO2 session with SCP03 encryption.
    /// Works over NFC (all firmware) and USB CCID (firmware 5.8+).
    pub fn open_scp03(device: &YubiKeyDevice) -> Result<Self, i32> {
        let mut handle: *mut c_void = ptr::null_mut();
        let rc = unsafe { fido2_session_open_scp03(device.handle, &mut handle) };
        if rc != OK { return Err(rc); }
        Ok(Self { handle })
    }

    /// Sends a cleartext APDU. The .NET SDK encrypts it via SCP if active.
    pub fn send_command(&self, apdu: &[u8]) -> Result<Vec<u8>, i32> {
        let mut buf = vec![0u8; 4096];
        let mut out_len: c_int = 0;
        let rc = unsafe {
            fido2_send_command(
                self.handle,
                apdu.as_ptr(),
                apdu.len() as c_int,
                buf.as_mut_ptr(),
                buf.len() as c_int,
                &mut out_len,
            )
        };
        if rc != OK { return Err(rc); }
        buf.truncate(out_len as usize);
        Ok(buf)
    }
}

impl Drop for Fido2Session {
    fn drop(&mut self) {
        unsafe { fido2_session_close(self.handle); }
    }
}
```

### Usage example

```rust
fn main() -> Result<(), i32> {
    let count = unsafe { yubikey_count_devices() };
    println!("Found {} YubiKey(s)", count);

    let device = YubiKeyDevice::open(0)?;
    let session = Fido2Session::open_scp03(&device)?;

    // Send a CTAP2 GetInfo command (0x04) through the SCP-encrypted channel
    let get_info_cmd = [0x04];
    let response = session.send_command(&get_info_cmd)?;
    println!("Response: {} bytes", response.len());

    Ok(()) // Drop cleans up session and device handles
}
```

---

## Implementation plan

### Phase 1: Skeleton (Windows first)

1. Create `Yubico.YubiKey.NativeInterop` project with NativeAOT config
2. Implement device enumeration exports (`yubikey_count_devices`, `yubikey_open_device`, `yubikey_close_device`)
3. Implement `fido2_session_open` (plain, no SCP) and `fido2_session_close`
4. Publish for `win-x64`, verify Rust can link and call functions
5. Write a minimal Rust test that enumerates devices and opens/closes a session

### Phase 2: SCP support

1. Implement `fido2_session_open_scp03` and `fido2_session_open_scp03_custom`
2. Implement `fido2_send_command` (raw APDU passthrough)
3. Test over NFC or USB CCID (5.8+ firmware) with a YubiKey
4. Add SCP11 variants if needed

### Phase 3: Cross-platform

1. Publish for `linux-x64` and `osx-arm64`
2. Set up CI to produce all three native libraries
3. Package as a Rust crate with platform-specific lib selection

### Phase 4: Production hardening

1. Thread safety audit (one session per thread, or add locking)
2. Comprehensive error codes with `fido2_get_last_error` for detailed messages
3. Logging bridge (route .NET SDK logs to Rust's tracing/log)
4. Memory leak testing (ensure all GCHandles are freed)

---

## Known risks and considerations

| Risk | Mitigation |
|------|------------|
| **NativeAOT trims unused code** | The SDK uses reflection in some areas. May need `<TrimmerRootAssembly>` or `rd.xml` to preserve types. Test early. |
| **NativeAOT binary size** | Expect 15-30 MB for the shared library. The full .NET runtime is embedded. |
| **Thread safety** | `Fido2Session` is not thread-safe. Document that each session handle must be used from one thread at a time. |
| **GCHandle leaks** | If Rust crashes or doesn't call `_close` functions, managed objects leak. Consider a timeout/finalizer strategy. |
| **USB device access** | On Linux, requires udev rules for non-root access. On Windows, requires the correct smart card drivers for NFC readers. |
| **Transport compatibility** | FIDO2+SCP requires CCID: works over NFC (all firmware) and USB CCID (5.8+). On pre-5.8 firmware over USB, Rust code must handle graceful fallback. |

---

## Files to reference

| File | What it shows |
|------|---------------|
| `Yubico.YubiKey/src/Yubico/YubiKey/Fido2/Fido2Session.cs` | Constructor accepting `ScpKeyParameters`, XML docs with transport notes |
| `Yubico.YubiKey/src/Yubico/YubiKey/Scp/ScpConnection.cs` | How the SCP channel is established |
| `Yubico.YubiKey/src/Yubico/YubiKey/ConnectionFactory.cs` | How connections are routed (HID vs SmartCard) |
| `Yubico.YubiKey/src/Yubico/YubiKey/YubiKeyFeatureExtensions.cs` | Feature gate — FIDO2 in SCP03 capability check |
| `docs/findings-and-assumptions.md` | Full investigation of FIDO2+SCP transport constraints |

---

## Feasibility assessment

### What the FFI path actually buys you

Through all the NativeAOT machinery, the .NET SDK provides:

1. **SCP03 handshake** — INITIALIZE UPDATE + EXTERNAL AUTHENTICATE (2 APDUs, AES-128 key derivation)
2. **SCP11 handshake** — EC key agreement, certificate chain validation, session key derivation
3. **APDU encryption/MAC** — AES-CBC encryption + CMAC per the GlobalPlatform SCP spec
4. **SELECT FIDO2 AID** — a fixed 7-byte APDU
5. **Device enumeration** — PC/SC reader discovery

That's the entire value crossing the FFI boundary. Everything else (CTAP2 commands, credential management, PIN handling) happens in Rust anyway.

### Cost of the FFI path

| Cost | Detail |
|------|--------|
| **Binary size** | 15-30 MB native library (embeds .NET runtime + GC + JIT stubs) |
| **Build complexity** | NativeAOT requires MSVC (Windows), clang (Linux), Xcode (macOS) — per platform |
| **Trimming issues** | The SDK uses reflection (logging, DI, serialization). Expect `rd.xml` and `<TrimmerRootAssembly>` debugging. |
| **FFI surface maintenance** | Every SDK API change requires updating the interop layer and Rust bindings |
| **Debugging** | Errors are opaque integers. Stack traces don't cross the FFI boundary. |
| **GCHandle lifecycle** | Managed objects pinned via `GCHandle` leak if Rust doesn't call `_close`. No destructor safety net. |
| **Two runtimes** | Process hosts both the .NET GC and Rust's allocator. Memory behavior is less predictable. |

### Alternative: native Rust SCP implementation

SCP03 is a well-specified protocol (GlobalPlatform Card Specification, Amendment D):

| Component | Rust implementation | Complexity |
|-----------|-------------------|------------|
| Key derivation | `cmac` crate (AES-CMAC) | ~30 lines |
| Session encryption | `aes` crate (AES-128-CBC) | ~20 lines |
| MAC generation | `cmac` crate | ~20 lines |
| APDU wrapping | Prepend MAC, encrypt payload | ~40 lines |
| Handshake | INITIALIZE UPDATE + EXTERNAL AUTHENTICATE | ~100 lines |
| NFC communication | `pcsc` crate (PC/SC smart card) | ~50 lines |
| SELECT application | Fixed APDU construction | ~10 lines |
| **Total** | | **~300-500 lines** |

### Comparison

| | NativeAOT FFI | Native Rust SCP |
|---|---|---|
| **Time to implement** | 2-3 weeks | 1-2 weeks |
| **Binary size** | 15-30 MB | ~1 MB |
| **Runtime dependencies** | .NET 8 runtime (embedded) | None (static linking) |
| **Debugging** | Painful (cross-runtime) | Normal Rust tooling |
| **Maintenance burden** | Coupled to .NET SDK versions | Self-contained |
| **Platform builds** | 3 separate NativeAOT publishes | `cargo build` (cross-compile) |
| **SCP03 support** | Free (SDK has it) | ~500 lines of Rust |
| **SCP11 support** | Free (SDK has it) | Significant work (~2000 lines: EC key agreement, X.509 cert chains, multiple key slots) |
| **Full FIDO2 command set** | Free (SDK has it) | Must implement from scratch |

### Recommendation

**If you need SCP03 only:** Implement in Rust. The protocol is simple, the crypto crates are mature, and you avoid all FFI complexity. The `pcsc` crate handles NFC reader access.

**If you need SCP11:** Evaluate whether the EC key agreement and certificate handling justifies the FFI overhead. SCP11 is substantially more complex than SCP03. The .NET SDK has a battle-tested implementation.

**If you need the full FIDO2 command set (credential management, PIN/UV, attestation):** The FFI path makes sense — reimplementing all of FIDO2 in Rust is weeks of work and the SDK already has it.

**If you only need an encrypted APDU pipe:** You're shipping 30 MB of .NET runtime for something that's 500 lines of Rust. Don't do it.

---

## Questions for the Rust team

1. **APDU format:** Do you want to send raw ISO 7816 APDUs, or CTAP2 command bytes? The SDK can handle either — determines which `SendCommand` overload to expose.
2. **SCP11 support:** Do you need SCP11b in addition to SCP03? If so, the FFI surface needs a way to pass EC key material (public key, certificates).
3. **Key management:** Will SCP keys be hardcoded, loaded from config, or provisioned at runtime? Affects whether we need `fido2_session_open_scp03_custom` or just `_scp03` with defaults.
4. **Error detail:** Is the integer error code sufficient, or do you want a `fido2_get_last_error(buf, len)` function that returns the .NET exception message as UTF-8?
5. **Concurrency:** Will multiple Rust threads access sessions simultaneously? If so, we need to add locking in the interop layer.

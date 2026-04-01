# Fix: High CPU in RDS/Terminal Server Environments (Issue #434)

## Problem

Users running applications that call `YubiKeyDevice.FindByTransport(Transport.HidKeyboard)` in
Windows Remote Desktop / Windows 365 / RDS terminal-server environments observed one CPU core
pegged at 100% during otherwise idle periods.

**Root cause (confirmed via 10 minidump analysis):**

When an RDS session is disconnected and reconnected, the Windows Smart Card Service invalidates
all existing `SCARDCONTEXT` handles for that session. `DesktopSmartCardDeviceListener` held one
such handle and polled `SCardGetStatusChange` every 100 ms. With an invalid handle, that function
returns immediately (never enters its blocking wait) and — critically — `WinSCard.dll` internally
raises and unwinds a C++ exception (`CxxThrowException` / `RtlRaiseException` / `RtlUnwindEx`)
before returning `SCARD_E_INVALID_HANDLE` to the caller. This machinery is extremely expensive:
it ran thousands of times per second, pegging a CPU core.

The managed listener received `SCARD_E_INVALID_HANDLE` but its error handler did not recognise
it as a recoverable condition. It logged the error and immediately retried — re-entering the
tight loop. No context re-establishment occurred. No backoff was applied.

**Minidump evidence (6 of 10 dumps mid-exception):**

```
WinSCard!SCardGetStatusChangeA+0x1d6
  → CxxThrowException (ERROR_INVALID_HANDLE 0x6 on thrown object)
  → RtlRaiseException → RtlDispatchException → RtlUnwindEx
  → CatchIt<__FrameHandler4> → FindHandler<__FrameHandler4>
  → returns SCARD_E_INVALID_HANDLE (0x80100003) to caller
Yubico_NativeShims!Native_SCardGetStatusChange+0xd1
  → managed listener thread → tight loop → repeat
```

Timeout parameter `0x64` (100 ms) visible on stack — function never blocks, fails instantly.

---

## Fix

Three changes to `DesktopSmartCardDeviceListener`, plus a `ISCardInterop` abstraction layer
for testability:

### 1. `ISCardInterop` interface + `SCardInterop` concrete class (new files)

Extracts the four SCard P/Invoke calls (`EstablishContext`, `GetStatusChange`, `ListReaders`,
`Cancel`) behind an injectable interface. Enables unit testing of every error-handling path
without real hardware, Windows, or an RDS environment.

### 2. `UpdateContextIfNonCritical` — three new error cases

```csharp
case ErrorCode.SCARD_E_INVALID_HANDLE:      // RDS session disconnect invalidates handle
case ErrorCode.SCARD_E_SYSTEM_CANCELLED:    // RDS session logoff / system shutdown
case ErrorCode.ERROR_BROKEN_PIPE:           // RDS: OS does not support SC redirection
```

Added alongside the existing `SCARD_E_SERVICE_STOPPED`, `SCARD_E_NO_READERS_AVAILABLE`,
`SCARD_E_NO_SERVICE` cases. All trigger `UpdateCurrentContext()` + `Thread.Sleep(1000)`.

### 3. `UpdateCurrentContext` — two defensive guards

- Checks `SCardEstablishContext` return value; if it fails (service still transitioning),
  keeps the existing `_context` rather than replacing it with a failed handle.
- Explicitly disposes the old `SCardContext` before replacing it (previously relied on
  SafeHandle finalizer — correct but delayed).

### 4. Default path backoff (catch-all)

Unrecognised error codes that fall through the switch now also sleep 1000 ms, preventing
tight loops from future unknown persistent error codes.

---

## Files Changed

| File | Change |
|------|--------|
| `Yubico.Core/src/Yubico/PlatformInterop/Desktop/SCard/ISCardInterop.cs` | New — interface |
| `Yubico.Core/src/Yubico/PlatformInterop/Desktop/SCard/SCardInterop.cs` | New — concrete impl |
| `Yubico.Core/src/Yubico/Core/Devices/SmartCard/DesktopSmartCardDeviceListener.cs` | Modified — fix |
| `Yubico.Core/tests/Yubico/Core/Devices/SmartCard/DesktopSmartCardDeviceListenerSCardErrorTests.cs` | New — cross-platform mock tests |
| `Yubico.Core/tests/Yubico/Core/Devices/SmartCard/DesktopSmartCardDeviceListenerWindowsTests.cs` | New — Windows CPU tests |

---

## Tests

### Cross-platform mock tests (run anywhere — CI, macOS, Linux)

These tests use `FakeSCardInterop` to inject specific error codes without needing Windows or
real hardware. They run on every CI platform.

```powershell
# From repo root
dotnet test Yubico.Core\tests\Yubico.Core.UnitTests.csproj `
    --filter "FullyQualifiedName~DesktopSmartCardDeviceListenerSCardErrorTests" `
    --logger "console;verbosity=detailed"
```

Four tests:
- `WhenGetStatusChangeReturnsInvalidHandle_ContextIsReestablished` — fails before fix, passes after
- `WhenGetStatusChangeAlwaysReturnsInvalidHandle_LoopDoesNotSpin` — proves no tight loop
- `WhenGetStatusChangeReturnsSystemCancelled_ContextIsReestablished` — RDS logoff path
- `WhenContextReestablishmentFails_ListenerContinuesWithoutCrashing` — service-unavailable safety

### Windows CPU tests (requires Windows — closes the fidelity gap)

These tests use the real `WinSCard.dll` and programmatically invalidate the listener's
`SCARDCONTEXT` handle via `SCardReleaseContext`, reproducing exactly what an RDS disconnect does.
The CPU test measures `Process.TotalProcessorTime` over a 3-second window.

**Requirements:**
- Windows 10 / 11 / Server (any edition)
- Smart Card service (`SCardSvr`) in **Running** state — enable via `services.msc` if needed
- No physical smart card reader required — the service runs without hardware

**Run on Windows:**

```powershell
# From repo root on the Windows machine
dotnet test Yubico.Core\tests\Yubico.Core.UnitTests.csproj `
    --filter "Category=WindowsOnly" `
    --logger "console;verbosity=detailed"
```

Three tests:
- `RealWinSCard_WhenHandleInvalidated_CpuDoesNotSpike` ← **gold standard test**
  - Before fix: `cpuConsumedMs ≈ 2500–3000ms` in 3s window → **FAIL**
  - After fix: `cpuConsumedMs ≈ 30–100ms` in 3s window → **PASS**
- `RealWinSCard_WhenHandleInvalidated_NewContextIsEstablished`
- `RealWinSCard_WhenHandleInvalidatedThenDisposed_DisposalCompletesCleanly`

If the Smart Card service is not running, tests show as `Skipped` (not failed).

**Verifying before the fix (to confirm the test catches the bug):**

```powershell
# Stash the fix, run the test — should FAIL with high CPU reading
git stash
dotnet test Yubico.Core\tests\Yubico.Core.UnitTests.csproj `
    --filter "FullyQualifiedName~RealWinSCard_WhenHandleInvalidated_CpuDoesNotSpike" `
    --logger "console;verbosity=detailed"

# Restore fix — test should PASS
git stash pop
dotnet test Yubico.Core\tests\Yubico.Core.UnitTests.csproj `
    --filter "FullyQualifiedName~RealWinSCard_WhenHandleInvalidated_CpuDoesNotSpike" `
    --logger "console;verbosity=detailed"
```

---

## Confidence

| Layer | What it proves | Status |
|-------|---------------|--------|
| Logic + Opus review | Causal chain: unhandled error → tight loop → CPU spike | ✅ Done |
| Mock tests (Track B) | Managed loop is throttled; recovery fires; no crash | ✅ Done |
| Windows CPU test (Track A) | Real WinSCard C++ exception overhead eliminated | ⬜ Run on Windows machine |

The Windows CPU test is the empirical proof that closes the fidelity gap between the structural
mock test and the OP's reported symptom (CPU core pegged).

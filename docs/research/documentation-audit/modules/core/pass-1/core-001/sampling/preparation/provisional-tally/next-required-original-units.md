# Bounded next-required replacement whole units

**No content verdicts.** Pinned source `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`; these are only calculated shortfall repairs, not another full-category expansion. Read enclosing context as needed.

## `core-001:unit:df3667e8c83d1eb5`
`scripts/toolchain/Discovery.cs:36–45`; A XML-contracts; original rank 39.

````csharp


    /// <summary>"src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Yubico.YubiKit.Piv.UnitTests.csproj" -> "Piv.UnitTests".</summary>
    public static string ShortName(string projectPath)
    {
        var name = Path.GetFileNameWithoutExtension(projectPath);
        return name.StartsWith(Conventions.ProjectPrefix, StringComparison.Ordinal) && name.Length > Conventions.ProjectPrefix.Length
            ? name[Conventions.ProjectPrefix.Length..]
            : name;
    }
````

## `core-001:unit:78622499bfedf099`
`src/Core/src/Devices/YubiKeyDevice.cs:104–107`; A XML-contracts; original rank 40.

````csharp


    /// <inheritdoc />
    public DeviceInfo? CachedDeviceInfo => DeviceInfo;
````

## `core-001:unit:e1789c5c3edd2e8e`
`src/Core/src/Devices/DiscoveryReadSkippedException.cs:24–26`; A enum-values; original rank 9.

````csharp

    /// <summary>The device does not expose the internal discovery-only connection path.</summary>
    NoDiscoveryProvider,
````

## `core-001:unit:d27f5051f8d21199`
`src/Cli.Commands/src/Otp/OtpCommands.cs:505–572`; A implementation-state-and-internal-contracts; original rank 23.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, OtpStaticSettings settings, YkDeviceContext deviceContext)
    {
        var slot = OtpHelpers.ParseSlot(settings.Slot);
        var layout = OtpHelpers.ParseKeyboardLayout(settings.KeyboardLayout);
        string password;

        if (settings.Generate)
        {
            int length = settings.Length ?? OtpHelpers.MaxStaticPasswordLength;
            password = OtpHelpers.GenerateStaticPassword(length, layout);
            OutputHelpers.WriteKeyValue("Generated password", password);
        }
        else
        {
            if (string.IsNullOrEmpty(settings.Password))
            {
                OutputHelpers.WriteError("--password or --generate is required.");
                return ExitCode.GenericError;
            }

            password = settings.Password;
        }

        // The YubiKey emits HID scan codes, not characters; translate for the host's layout up front so an
        // untypeable character is reported before any prompt or device access.
        StaticPasswordSlotConfiguration config;
        try
        {
            config = OtpHelpers.CreateStaticPasswordConfiguration(password, layout);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            OutputHelpers.WriteError($"Password contains a character the {layout} keyboard layout cannot type: {ex.Message}");
            return ExitCode.GenericError;
        }

        byte[] accessCode = OtpHelpers.ParseHex(settings.AccessCode, "access-code") ?? [];

        try
        {
            if (!settings.Force)
            {
                if (!ConfirmationPrompts.ConfirmDangerous(
                        $"program slot {OtpHelpers.FormatSlot(slot)} with static password"))
                {
                    OutputHelpers.WriteInfo("Operation cancelled.");
                    return ExitCode.UserCancelled;
                }
            }

            await using var session = await deviceContext.Device.CreateYubiOtpSessionAsync(
                deviceContext.SessionOptions);

            await session.PutConfigurationAsync(slot, config, currentAccessCode: accessCode);

            OutputHelpers.WriteSuccess(
                $"Slot {OtpHelpers.FormatSlot(slot)} programmed with static password.");
            return ExitCode.Success;
        }
        finally
        {
            config.Dispose();
            CryptographicOperations.ZeroMemory(accessCode);
        }
    }
````

## `core-001:unit:77cd47f2db7ebab8`
`src/Core/src/ProductAtrs.cs:61–66`; A implementation-state-and-internal-contracts; original rank 24.

````csharp


    public static IList<AnswerToReset> UsbYubiKeys => new List<AnswerToReset>
    {
        YubiKeyNeoUsb, YubiKey4Usb, YubiKey5Usb
    };
````

## `core-001:unit:94abe99ff516c417`
`src/Cli.Commands/src/Fido/FidoCommands.cs:284–322`; A implementation-state-and-internal-contracts; original rank 25.

````csharp


    protected override async Task<int> ExecuteCommandAsync(
        CommandContext context, FidoVerifyPinSettings settings, YkDeviceContext deviceContext)
    {
        using var pin = PinPrompt.Resolve(settings.Pin, "PIN");
        if (pin is null)
        {
            OutputHelpers.WriteError("PIN is required.");
            return ExitCode.GenericError;
        }

        byte[]? pinToken = null;

        try
        {
            await using var session = await deviceContext.Device.CreateCtap2SessionAsync(
                deviceContext.SessionOptions);
            using var protocol = new PinUvAuthProtocolV2();
            using var clientPin = new ClientPin(session, protocol);

            pinToken = await clientPin.GetPinTokenAsync(pin.Memory);

            OutputHelpers.WriteSuccess("PIN is correct.");
            return ExitCode.Success;
        }
        catch (CtapException ex)
        {
            OutputHelpers.WriteError(FidoHelpers.MapCtapPinError(ex));
            return ExitCode.AuthenticationFailed;
        }
        finally
        {
            if (pinToken is not null)
            {
                CryptographicOperations.ZeroMemory(pinToken);
            }
        }
    }
````

## `core-001:unit:d7f277c73e9d67c6`
`scripts/toolchain/TestTargets.cs:276–282`; A implementation-state-and-internal-contracts; original rank 26.

````csharp


    static bool MtpFilterHasMatches(string projectPath, string[] positiveMtpFilterArgs) =>
        FilterHasMatches(
            projectPath,
            ["run", "--project", projectPath, "-c", Conventions.Configuration, "--", "--list-tests", .. positiveMtpFilterArgs],
            RunnerMarkers.MtpNoMatches);
````

## `core-001:unit:0389bce1986c0ea9`
`src/Cli.Commands/src/Management/ManagementConfigCommand.cs:75–78`; A implementation-state-and-internal-contracts; original rank 27.

````csharp


    private static string FormatDeviceFlags(DeviceFlags flags) =>
        flags == DeviceFlags.None ? "None" : flags.ToString();
````

## `core-001:unit:0b5f8163f489c670`
`docs/usage/device-discovery.md:283–325`; A narrative-examples-and-maps; original rank 17.

````markdown
### Several consumers: a host-owned singleton

Applications using `Microsoft.Extensions.Hosting` can register one initialized monitor. The SDK itself
does not depend on hosting. The three-line adapter below stops monitoring at graceful host shutdown;
`await using` also cleans up if host startup or execution fails. Register consumers after this adapter.
Their watch loops may still be enumerating when `StopAsync` disposes the monitor; those enumerations end
normally, so consumers need no extra coordination beyond handling the end of the stream.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yubico.YubiKit.Core.Devices;

await using var monitor = await YubiKeyManager.CreateMonitorAsync();
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(monitor); // Externally created instance: container does not own disposal.
builder.Services.AddHostedService<MonitorLifetime>();
// Register services that receive YubiKeyMonitor and call monitor.WatchAsync(theirStoppingToken).
using var host = builder.Build();
await host.RunAsync();

sealed class MonitorLifetime(YubiKeyMonitor monitor) : IHostedService
{
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => monitor.DisposeAsync().AsTask();
}
```

The factory starts listeners and awaits the loop's first publication. Empty success is ready; first-scan
failure fails fast and cleans up partial ownership. Its token cancels initialization only. After return,
the monitor runs even without subscribers until disposed. There is no pause/restart: create a new instance.

On first enumeration, capture and subscription registration are atomic. Replay does not occupy the
256-change subscriber buffer, but changes arriving during replay do. Overflow ends only that subscriber
with `InvalidOperationException`: clear its projection and re-enumerate the same monitor. Cancellation
or disposing a subscription does not stop a shared monitor. Consumer code never runs inline on publication.

Disposal is sequential: stop/dispose listeners, cancel and await the scan loop, retire discovery, complete
the feed. There is no disposal timeout or abandoned managed loop. In-flight scan waits are bounded by the
discovery budgets, not a hard total wall-clock limit; already-open native work keeps its lease until actual
teardown. Healthy streams can drain committed history and end normally; new enumerations after disposal
end empty. Disposal is idempotent and does not dispose caller-owned sessions or connections.


````

## `core-001:unit:353ef7aa94348bf2`
`src/Tests.Shared/CLAUDE.md:259–268`; A narrative-examples-and-maps; original rank 18.

````markdown
### Environment.Exit During Discovery

**Causes:**
- Allow list is empty or missing
- Cannot read appsettings.json

**Fix:**
- Add serial to `appsettings.json`
- Verify appsettings.json copied to output directory


````

## `core-001:unit:9220def02b369490`
`docs/usage/device-discovery.md:380–438`; A narrative-examples-and-maps; original rank 19.

````markdown
### Read-only scoped discovery hardware checks

From the repository root, explicitly select the read-only cases:

```bash
dotnet toolchain.cs -- test --integration --project Management.IntegrationTests --filter "(FullyQualifiedName~OwnedMonitorReadOnlyTests|FullyQualifiedName~ScopedDiscoveryReadOnlyTests)&Category!=RequiresUserPresence"
```

These tests live in the Management integration project only because they compare discovery metadata with
independent live `ManagementSession.GetDeviceInfoAsync` reads. Core itself needs no Management dependency.
Configure the existing `YubiKeyTests:AllowedSerialNumbers` allow list before running them. They exercise
**every attached authorized serial-bearing USB key**, not just the first `[WithYubiKey]` match, and do not
hard-code a key count or serial in the source.

The eight scoped cases cover SmartCard, FIDO, OTP, HID, SmartCard+OTP, SmartCard+FIDO, explicit All, and the default scope. They check
in-scope connection sets, serial-based `SameDeviceAs` correlation across YubiKeys from independent scoped calls, retained snapshots,
and cached/live serial, firmware, form factor, and USB/NFC capability agreement. Each exposed concrete
connection is opened, read, and asynchronously disposed before the next connection. The tests never reset,
write configuration or credentials, request touch, or require unplugging. Do **not** substitute a broad
Management integration run: other cases in that project change configuration or reset the key.

Run on idle serial-bearing USB fixtures, without another monitor/client probing the same keys. The scoped
tests do not start monitoring or use its cache; a full reference scan precedes each scoped test. Consequently this
hardware run does not prove that excluded backends were never called: the deterministic `FindYubiKeysScopeTests`
pin those boundaries, including the distinction between shared HID descriptor enumeration and excluded
interface protocol probes.

Metadata readiness is retried up to three scans with 250 ms backoff; live connection acquisition retries only
`ConnectionInUseException`, up to three attempts. The existing finite worker-saturation scan retry also applies.
Each case requests cancellation after 60 seconds; this is neither a hard completion bound nor a native-drain
guarantee. Missing expected interfaces or metadata remain failures, with diagnostic output; no fixture or
no supported authorized interface can skip a case. Check executed/skipped totals and resolve external
contention before treating a failure as an SDK bug.
For detailed firmware/count/retry output, temporarily enable xUnit's `showLiveOutput` in that project's runner
configuration. Do not commit machine-specific serials or hardware logs.

Connections to YubiKeys from disjoint scopes are **never** opened simultaneously in hardware tests. Overlapping
and disjoint ownership bounds, cancellation, and queued-read retirement are tested with fakes in
`FindYubiKeysScopeTests`; successful read-only hardware checks do not imply universal physical-key locking.

`OwnedMonitorReadOnlyTests` adds four public-API cases requiring **at least two attached authorized keys**;
fewer usable fixtures fails that hardware prerequisite rather than counting a one-key run as two-key coverage.
They read initial membership through `WatchAsync` replay up to `SnapshotComplete` after awaited creation
(no polling into readiness), checking both fixtures, retained membership/object identity, subscriber-local cancellation, factory-token
independence, disposal/stream completion, recreate, independent convenience selection, and predicate-based
null/throw no-match behavior without unplugging anything. They log fixture ordinals/interface support,
not machine serials. Snapshot metadata is compared with sequential live reads only **after disposing the
monitor**. Incomplete initial metadata/interfaces remain failures, not successful two-key observations.

`ScopedDiscoveryReadinessTests` also pins that a complete serial-less bystander does not prevent readiness,
whereas any YubiKey lacking metadata exhausts the bounded retries. Run that hardware-free regression explicitly
with `--integration --project Management.IntegrationTests --filter "FullyQualifiedName~ScopedDiscoveryReadinessTests"`.

**Attended acceptance remains a separate required pass gate for this lifecycle migration.** With a human
present, run the narrowly selected `DeviceEventHotPlugTests` and `HotPlugIdentityContractTests` protocols
for real removal/reinsertion and event/identity lifetime validation. Do not execute those cases unattended
or count unexecuted/skipped cases as acceptance. No UV/UP, touch, PIN/biometric prompt, reset, write, or manual
insertion/removal is part of the read-only filter above.


````

## `core-001:unit:287e6410db16f0e7`
`docs/TESTING.md:137–151`; A narrative-examples-and-maps; original rank 21.

````markdown
### xUnit v2 vs v3 Filter Differences

When running filtered tests **outside** the build script (ad-hoc debugging), syntax differs by xUnit version:

| Version | Detection | Filter Syntax |
|---------|-----------|---------------|
| xUnit v2 | No `UseMicrosoftTestingPlatformRunner` | `--filter "FullyQualifiedName~TestName"` |
| xUnit v3 | Has `UseMicrosoftTestingPlatformRunner` | `-m TestName` or `--method TestName` |

**Check version:** Look for `<PackageReference Include="xunit"` in the `.csproj`:
- `3.x.x` → xUnit v3 syntax
- `2.x.x` → xUnit v2 syntax

**Recommendation:** Use `dotnet toolchain.cs -- test --filter "..."` which handles this automatically.


````

## `core-001:unit:a9443056da14fe17`
`src/Core/src/Transports/Hid/Linux/LinuxFidoHidConnection.cs:51–70`; B2 XML-contracts; original rank 35.

````csharp


    /// <inheritdoc cref="IFidoHidConnection.SendAsync" />
    /// <remarks>
    ///     The owner copies all 64 payload bytes before publishing work to its worker, and clears that copy
    ///     before task completion. Native framing adds a zero report-ID byte. The caller retains its source
    ///     memory and must clear sensitive input after the task is terminal; the SDK never clears that source.
    ///     Cancellation before dispatch skips output; after dispatch the task waits for native completion.
    ///     The caller remains responsible for CTAPHID sequencing and must not interleave session or raw I/O.
    /// </remarks>
    /// <param name="packet">Exactly 64 caller-owned payload bytes, without a native report-ID prefix.</param>
    /// <param name="cancellationToken">Cancels output only before native dispatch.</param>
    /// <returns>A task terminal only after any dispatched output returns and its staging copy is cleared.</returns>
    public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return packet.Length != 64
            ? throw new ArgumentException("FIDO packets must contain exactly 64 bytes.", nameof(packet))
            : _owner.SendAsync(packet, cancellationToken);
    }
````

## `core-001:unit:b605ac9cbec57270`
`src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs:88–90`; B2 XML-contracts; original rank 36.

````csharp

    /// <summary>Performs one ioctl with borrowed request storage; Count is request-specific status or transfer count.</summary>
    LinuxIoResult Ioctl(LinuxFileSafeHandle handle, nuint request, Span<byte> buffer);
````

## `core-001:unit:42e5db3e323167cb`
`src/Core/src/Transports/Hid/Linux/LinuxFidoHidConnection.cs:1–14`; B2 XML-contracts; original rank 37.

````csharp
// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0 (the "License").

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;

namespace Yubico.YubiKit.Core.Transports.Hid.Linux;

/// <summary>Provides the built-in typed Linux FIDO connection over a hidraw native owner.</summary>
/// <remarks>
///     Selected by the Linux typed connection factory. Each connection owns a native lifetime with one
///     dedicated worker and reactor registration; the synchronous expert facade uses the same owner type,
///     not the same owner instance. Only one ordinary send or receive is admitted at a time.
/// </remarks>

````

## `core-001:unit:8ee0f42300c2c7e1`
`src/Core/src/Transports/Hid/Linux/LinuxHidDeviceListener.cs:336–342`; B2 XML-contracts; original rank 38.

````csharp


    /// <summary>
    /// Per-<see cref="Start"/> state: the event source, its listener thread, and the
    /// cancellation flag. Sessions are never reused; a session's thread is the sole owner of
    /// its source's disposal.
    /// </summary>

````

## `core-001:unit:94c5acf06136d521`
`src/Core/src/Transports/Hid/Linux/LinuxHidFeatureReportConnection.cs:80–92`; B2 XML-contracts; original rank 39.

````csharp


    /// <summary>Stops admission and blocks for the shared native shutdown outcome.</summary>
    /// <remarks>
    ///     Waits for any admitted GET/SET to return and checked close on the original worker. There is no drain
    ///     timeout; unproven release retains the native owner. Calling from the native worker is rejected.
    ///     Prefer <see cref="DisposeAsync" /> to avoid blocking the caller.
    /// </remarks>
    public void Dispose()
    {
        _owner.Dispose();
        GC.SuppressFinalize(this);
    }
````

## `core-001:unit:e6e22f9870456ae8`
`docs/research/linux-native-report-access.md:89–105`; B2 narrative-examples-and-maps; original rank 13.

````markdown
## Descriptor sizing limits

The size ioctl is bounded to 1..4095 before requesting the descriptor (the v6.12 hidraw ioctl
rejects lengths above HID_MAX_DESCRIPTOR_SIZE - 1), whose native struct is 4 + 4096 bytes.
A pool rental is temporary metadata, not report/secret staging. The existing parser's
last Input/Output item and default 64-byte behavior are retained for known descriptors, including
the pinned parsing vectors. Multiplication is widened and oversized report items are rejected.
Numbered report descriptors are outside this adapter's scope and fail explicitly. This is not
a generic HID parser: global PUSH/POP and collection aggregation are not implemented. Existing
truncation/default behavior is retained rather than claiming generic malformed-HID detection.
Bounded unnumbered non64 input/output sizes pass through to direct IO metadata/read capacity;
the parser's report-byte bound is 4096. Direct SET still uses caller length rather than requiring
it to match output metadata. OTP feature GET/metadata retains its established fixed 8 bytes.
Unavailable descriptor ioctls retain the old 64/64 FIDO fallback; invalid reported sizes fail.
The descriptor ioctl returns status, not a byte count; parsing is bounded by the requested struct
size, not a fabricated transfer count.


````

## `core-001:unit:792fc8aff569fa2e`
`docs/research/linux-fido-async-lifetime.md:7–31`; B2 narrative-examples-and-maps; original rank 17.

````markdown
## Exact seam and ownership

```csharp
internal static Task<LinuxFidoHidConnection> OpenAsync(
    string path, ILinuxHidReportApi reportApi, ILinuxHidReadReactor reactor,
    CancellationToken cancellationToken, IDisposable? physicalClaim = null);
```

`LinuxFidoHidConnection` implements `IFidoHidConnection` and `ITerminalWakeControl`. Its private
native lifetime is `LinuxFidoHidNativeOwner`, also used by `LinuxHidIOReportConnection`. The owner
roots itself with GCHandle before accepting the claim/acquiring native state. Facades are not
rooted by it: their finalizers can request nonblocking shutdown even while native state stays rooted.

One lazily started dedicated background worker owns open (`O_RDWR|O_NONBLOCK|O_CLOEXEC`), descriptor
ioctl, registration, activation wait, writes, actual quiescence wait, reference release and checked
close. There is one ordinary operation slot (send OR receive), no ordinary backlog, global
scheduler, command framework, replacement worker, operation Task.Run or cleanup pool job. The
opening caller's ExecutionContext does not flow to the worker. Cancellation during open waits for
native return and safe cleanup; a pre-canceled open never acquires native state.

The token is stored before Activation is observed, including failed published admissions. Failed
open cleanup preserves both the original and cleanup errors in an AggregateException. Every
published token stays attached to this same owner through real drain. An activation result,
terminal notification or failed removal wake never substitutes for Quiescence.


````

## `core-001:unit:283dfadc640419c8`
`src/Core/CLAUDE.md:218–240`; B2 narrative-examples-and-maps; original rank 24.

````markdown
### Linux OTP owner seam (L4)

`LinuxOtpHidConnection.OpenAsync(path, reportApi, cancellationToken, physicalClaim)` implements
`IOtpHidConnection` over a separate `LinuxOtpHidNativeOwner`. Rooting precedes native acquisition;
one context-suppressed dedicated worker owns open, feature GET/SET and checked close. Exactly one
ordinary raw call is admitted; there is no backlog, per-call Task.Run, replay or reactor involvement.
Send owns a copy before publication and clears it before task completion. Receive allocates eight
bytes on the worker, clears failed/partial output, and transfers successful storage to the caller.
Pre-dispatch cancellation submits nothing; after native dispatch cancellation cannot detach GET/SET.

Shutdown stops admission and shares one outcome across sync/async callers, after any admitted native
call returns. EINTR/EIO release the original claim with diagnostics; unknown release retains and
quarantines the rooted owner/claim. Unexpected worker failure stops admissions, faults actual pending
open/send/receive tasks, clears staging and attempts checked cleanup on that original worker. The
initial error survives successful cleanup. Signal retirement shares the setter lock. Finalization
only requests nonblocking, nonthrowing shutdown, even after worker failure/event retirement.

`LinuxHidFeatureReportConnection` is the synchronous expert facade over the same OTP owner:
metadata/GET remain8; direct SET retains supported caller lengths, rather than imposing typed8.
L1 still supplies native framing, exact-count checking and scoped zeroing. No factory/discovery
routing or adapter-registry rows change in L4. L5 routing is described below.
See [L4 handoff](../../docs/research/linux-otp-async-lifetime.md).


````

## `core-001:unit:a327c384b612ddf6`
`verification/LinuxHidRouteVerification/OperatorScenarios.cs:100–101`; B2 type-declarations; original rank 3.

````csharp
    private sealed class PresencePrompt(CancellationTokenSource? cancel) : IUserPresencePrompt
    {
````

## `core-001:unit:b63489aa81cdbaf2`
`src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs:71–76`; B3 XML-contracts; original rank 5.

````csharp


    /// <summary>
    ///     Closes the device. Does not release it; see <see cref="ReleaseCFObject" />.
    /// </summary>
    void CloseDevice(nint device);
````

## `core-001:unit:791a10be0b884998`
`CLAUDE.md:7–30`; B4 narrative-examples-and-maps; original rank 9.

````markdown
## Project Overview

`net10.0`, `LangVersion=14.0`, nullable reference types enabled throughout. Reference docs for new platform features live in `docs/net10/`. Use new .NET 10 library + C# 14 features actively.

**Modules** (all under `src/` with `Yubico.YubiKit.` prefix stripped from directory names; assembly/namespace/DLL names retain the prefix):

| Module | Purpose |
|---|---|
| `Core/` | Device management, connections, APDU pipeline, platform interop |
| `Management/` | Device info, capability detection, firmware version |
| `Piv/` | PIV smart-card functionality |
| `Ctap2/` | CTAP2 authenticator operations |
| `WebAuthn/` | Higher-level WebAuthn surface (delegates to CTAP2 — duplicate ZERO behavior) |
| `Oath/` | TOTP/HOTP |
| `YubiOtp/` | Yubico OTP |
| `OpenPgp/` | OpenPGP card |
| `SecurityDomain/` | SCP03 secure channel, key management |
| `YubiHsm/` | YubiHSM 2 integration |
| `PublicApi/` | Test-only cross-module public API conventions; no shipping assembly |
| `Cli.Shared/` | Shared CLI infra for example tools |
| `Tests.Shared/` | Multi-transport test harness |

**Platform interop** lives in `src/Core/src/Native/{Windows,MacOS,Linux,Desktop}/` with the P/Invoke declarations; the managed transport implementations that call them are in `src/Core/src/Transports/`. `UnmanagedDynamicLibrary` + `SafeLibraryHandle` manage native loading; `SdkPlatformInfo` detects runtime platform.


````

## `core-001:unit:3c6044a5e5f76ea2`
`docs/v1-to-v2-comparison.md:97–109`; B4 narrative-examples-and-maps; original rank 10.

````markdown
## Decided against

**A meta-package.** V1 was effectively one "install everything" dependency.
V2 won't add an equivalent bundle. Installing only the applets you use is the
supported path, because a catch-all package would give back exactly the
footprint savings the split is there to deliver.

**Synchronous applet facades or wrappers.** V2 does not provide synchronous
facades for applet operations. Migrating applet callers must adopt async flows
rather than blocking on SDK operations. This does not remove public synchronous
raw HID report methods, SmartCard transaction begin, or synchronous disposal;
see [raw access tiers](architecture/raw-access-tiers.md#retained-synchronous-compatibility-paths).


````

## `core-001:unit:41a13990682e5fd2`
`src/Core/PACKAGE_README.md:1–12`; B4 narrative-examples-and-maps; original rank 11.

````markdown
# Yubico.YubiKit.Core

Core is the foundation of the Yubico .NET SDK: device discovery, connections over SmartCard and HID, the
APDU and Secure Channel Protocol layers, and the shared types every application module builds on.
Most applications install an application package instead and get Core with it.

> ## Alpha - not for production
>
> This is a pre-release alpha. It is subject to change and has **not yet completed Yubico's formal
> security audit**. No security guarantees are made until that audit is complete. Packages are
> unsigned, and package names and namespaces may change. Provided for evaluation only.


````

## `core-001:unit:c2de2d6f26065229`
`src/Core/README.md:73–115`; B4 narrative-examples-and-maps; original rank 12.

````markdown
### Own continuous tracking

manager watch creates a producer; monitor watch subscribes to an existing one.

`YubiKeyMonitor` owns one continuous tracking lifetime: `WatchAsync` replays `DeviceAdded`, emits
`SnapshotComplete` (including empty discovery), then streams `DeviceAdded`, `DeviceRemoved`, and
`DeviceUpdated` changes. Maintain a consumer projection for cached membership; disposal ends tracking
without disposing application-owned sessions/connections, and later enumerations complete empty.

Create it with `await YubiKeyManager.CreateMonitorAsync(cancellationToken)` and dispose with `await using`.
Initialization registers listeners before the loop's single initial scan and waits for publication,
including successful empty discovery. Startup failure/cancellation cleans up the partial lifetime; the
creation token does not control the monitor after successful creation. The monitor performs all
initial, listener-triggered, and five-second fallback scans sequentially. There is no manual refresh or
stop/restart mode: dispose the monitor and create a new one to recover from terminal failure. There is no
process-global monitoring lifetime or device cache.

Transport listeners share payload-less `IDeviceChangeListener.Changed` hints, not physical-device truth.
The monitor reconciles scans; listener failure falls back to interval discovery. An enumeration timeout is
handled per path: one-shot `Find…Async` logs a warning and returns the other transport's results for that
call; a monitor never publishes a partial scan, so the timeout fails `CreateMonitorAsync` on the first scan
and, after initialization, keeps the previous snapshot until a later scan succeeds.

Share one host-owned monitor between long-lived components rather than letting each component take over
tracking. Short commands should normally use the one-shot Find family. A configuration editor can use a
monitor for selection, but must explicitly read Management for current configuration.

`YubiKeyMonitor.WatchAsync` supplies initial `DeviceAdded` records, `SnapshotComplete`, then ordered changes. On each
enumeration's first `MoveNextAsync`, it captures membership and subscribes at the same boundary as a
completed publication, so there is no snapshot/subscription gap. Initial replay does not occupy the
independent 256-change delta buffer; changes arriving while replay is consumed can still overflow it.
Overflow faults only that watcher: **clear its device projection and re-enumerate the same monitor's
`WatchAsync`** to rebuild it safely. Canceling a watcher does not stop monitoring. Disposal allows already
captured replay and published deltas to drain, but new enumerations complete empty.

Use the replaying stream to initialize a UI projection, or independent `FindAllAsync` for a one-shot lookup.
Disposal sequentially stops listeners, cancels and awaits the loop with no drain timeout, retires discovery,
and completes `DeviceChangeFeed`; native discovery budgets bound waits, while already-open native work
retains its leases until actual teardown. Already-published replay and changes may still drain.
Concurrent watch enumerations are safe, and disposal may overlap them: they end normally. Disposal must
not be called concurrently with itself. See the
[usage guide](../../docs/usage/device-discovery.md#device-monitoring) for picker and generic-host samples.


````

## `core-001:unit:0c27fcfbe45d9806`
`src/OpenPgp/src/OpenPgpSession.cs:255–276`; OpenPgp implementation-state-and-internal-contracts; original rank 9.

````csharp


    // ── Internal Helpers ──────────────────────────────────────────────

    private async Task<ReadOnlyMemory<byte>> GetDataCoreAsync(
        DataObject dataObject,
        CancellationToken cancellationToken)
    {
        var tag = (int)dataObject;
        var command = new ApduCommand
        {
            Cla = 0x00,
            Ins = (byte)Ins.GetData,
            P1 = (byte)(tag >> 8),
            P2 = (byte)(tag & 0xFF),
        };

        _logger.LogDebug("GET DATA for DO 0x{Tag:X4}", tag);
        var response = await TransmitWithResponseAsync(command, cancellationToken)
            .ConfigureAwait(false);
        return response.Data;
    }
````

## `core-001:unit:4264b15d39c60d99`
`src/OpenPgp/CLAUDE.md:20–22`; OpenPgp narrative-examples-and-maps; original rank 5.

````markdown
### Transport
SmartCard-only (`ISmartCardConnection`), over USB CCID or NFC. Applet SELECT and APDU send mechanics are routed through the module-local `OpenPgpBackend`; OpenPGP APDU construction stays in `OpenPgpSession`.


````

## `core-001:unit:e02a91097cd322b3`
`src/SecurityDomain/CLAUDE.md:30–38`; SecurityDomain narrative-examples-and-maps; original rank 5.

````markdown
### Memory and Crypto Safety

This module handles **highly sensitive cryptographic material**. Apply these rules strictly:

Follow the root memory rules: use `stackalloc` for synchronous buffers up to 512 bytes, pooled arrays for
larger synchronous buffers, and `Memory<byte>`/owned memory across awaits. Clear sensitive caller-owned
buffers in `finally`, dispose cryptographic objects, and use fixed-time comparisons for secret-derived
values. Log references and operation names, never key bytes, KCVs, or challenge/response payloads.


````

## `core-001:unit:876d6e14e704aec5`
`src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/Client/UserVerification/UvDecisionLogicTests.cs:1–30`; WebAuthn tests-and-helpers; original rank 25.

````csharp
// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Client.UserVerification;
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.UnitTests.Client.UserVerification;

/// <summary>
/// Truth table for whether a ceremony needs a PIN/UV auth token.
/// </summary>
/// <remarks>
/// The case worth protecting is <see cref="UserVerificationPreference.Discouraged"/> on a key that
/// has a PIN set. Verification must be skipped when no authenticator or permission rule requires it,
/// and <c>preferred</c> must degrade cleanly when verification is not configured.
/// </remarks>

````


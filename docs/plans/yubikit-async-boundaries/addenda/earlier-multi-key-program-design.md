# Earlier program design: multi-key execution and capacity

Status: preserved earlier multi-key program-design proposal, superseded for active
iteration planning by the single-key-first scope request on 2026-09-22. Independent
review passed for this proposal, but it was not approved or implemented. See the
[multi-key addendum](multi-key-capacity.md) for the deferred mechanisms and
the safety requirements that still apply to one key. The body below is retained;
do not dispatch it as the current single-key implementation plan.
Source base: `a7f2cae8c32ad6e0ada55e404f85442f6a266f6c`.

The [master plan](../../../../2026-09-21-yubikit-async-boundaries-ISA.md) retains acceptance
and evidence authority. This document preserves the earlier proposed contracts and file ownership,
not an additional acceptance registry. Declaration blocks are signature inventories,
not compilable implementations. Native contracts remain conditional on a verified
source/package pair and supported-platform evidence. Revised Gates 2 and 3 must select
the single-key contracts before Gate 4 orders implementation.

## Review decisions

1. Keep one internal asynchronous `IHidConnection`; do not split report modes into
   duplicate interfaces. Keep typed raw connections and raw sessions public.
2. Replace synchronous transaction acquisition with `BeginTransactionAsync` and an
   owned asynchronous-disposal scope. Retain synchronous disposal only as an explicit facade.
3. Separate local `Configure` from awaited protocol initialization. Make factory-failure
   cleanup asynchronous consistently across the eight direct applets and three raw sessions.
4. Use dedicated bounded native workers plus pre-acquisition cleanup reservations.
   All deferred cleanup must fit those reservations; no unbounded quarantine work list.
5. Release physical ownership on positive native release evidence, not a teardown
   task merely reaching a faulted state. Root native state independently of caller references.
6. Extend existing seams where they fit and add only the missing native submission/
   readiness seams. macOS dispatch ownership and report callbacks have distinct contracts.
7. Keep inventory, measurement and native-runtime verification in existing test/tooling
   projects; independently discover boundaries before joining them to a manifest.

## Fixture and capacity proposal

The user selected **the three currently attached keys** as the initial measurement set.
`system_profiler SPUSBDataType -detailLevel mini` observed these devices on 2026-09-22:

| Local fixture | Vendor/product | Location at observation | Reported bus version |
|---|---|---|---|
| K1 | `1050:0407` | `0x01114000` | `5.74` |
| K2 | `1050:0407` | `0x01113000` | `5.43` |
| K3 | `1050:0407` | `0x01111100` | `0.01` |

All three advertise the composite OTP/FIDO/CCID product name. These are descriptor
observations, not verified firmware, applet availability, or successful exchanges.
Locations can change after replug. Bind run-local aliases to verified device identity
before measurement; do not commit serial numbers. The factory ownership model permits
one live connection per physical key, not three simultaneous interface connections
per key. Direct raw/external implementations retain their documented responsibilities.

**Proposed calibration values, unmeasured and not yet frozen:**

| Resource | Proposed initial bound | Rationale / exhaustion |
|---|---|---|
| Interactive blocking work | 4 active, 16 queued | Exercise three keys concurrently with one spare worker; a full queue rejects before dispatch. |
| Discovery blocking work | 4 active, 16 queued | Use the existing four-slot count as a calibration seed; native-call capacity is not the same as the old whole-probe semaphore. |
| Metadata probe admission | 4 admitted workflows, 16 waiting | Bound the existing discovery orchestration separately from native calls; these are tasks, not dedicated native threads. |
| Cleanup execution | 2 active | One indefinitely occupied cleanup worker leaves one making progress; two occupied workers can exhaust progress. No slot is reclaimed before native return. |
| Cleanup reservations | 8 interactive-origin, 8 discovery-origin | Acquired before native resources; discovery cannot consume interactive credits. At most 16 outstanding release obligations, including live, ready, executing and quarantined owners. |
| Cleanup ready queue | 16 entries | One coalesced release job per reservation. Readiness admission cannot fail after native acquisition. No separate retry/backlog queue. |
| Persistent macOS input receiver | 256 owned reports per connection | A proposed burst allowance, subject to report-length/protocol-burst verification and measurement; overflow faults instead of dropping packets. |

These are real resource-admission limits: eight live or quarantined interactive owners
consume all eight interactive credits, so a ninth such open fails until a credit is
safely returned. Permanently unreleased owners can exhaust this process for its lifetime.
This is a proposed implementation capacity, not a claim to support only eight device
models or a measured scale guarantee. The fixture target is three keys, plus controlled saturation and
one-stalled-cleanup tests; faults are injected in isolated test processes, not by
deliberately hanging attached devices. Worker threads start lazily, remain background
threads, and are bounded process-wide. No unbounded admission waiters or replacement
threads are allowed. Existing persistent monitor/event threads are inventoried separately.

Before production changes, run the approved baseline, review these numbers and freeze
them with the user. No latency/allocation/idle regression thresholds are invented from
test-suite durations. Hardware/driver/system versions beyond this host remain pending;
these three USB fixtures do not supply an NFC-reader fixture or Windows/Linux evidence.

## Files

Paths are worktree-relative. `modify` means an existing file; `add` is a proposed file.
One foundation/integration engineer owns shared files. Platform engineers own their
platform rows only after the tested shared-contract handoff. A newly discovered public
dependency or file outside this list must return to the orchestrator before editing.

### Shared production and public surface

| Action | Files | Purpose |
|---|---|---|
| modify | `src/Core/src/Transports/Hid/IHidConnection.cs`, `IHidInterface.cs` in that directory | Internal asynchronous report/open contracts. |
| modify | `src/Core/src/Transports/Hid/FindHidInterfaces.cs`, `HidDeviceListener.cs`, `HidDescriptorInfo.cs`, `HidInterfaceType.cs`, `HidInterfaceClassifier.cs`, `HidDeviceRescanHint.cs`, `HidDeviceChangeKind.cs`, `HidReportType.cs` in that directory | Internalize low-level enumeration (both finder and IFindHidInterfaces), classification and listener types. |
| modify | `src/Core/src/DeviceListenerStatus.cs`, `src/Core/src/Devices/ConnectionTypeMapper.cs` | Close public dependencies on internal listener/classification types. |
| modify | `src/Core/src/Transports/SmartCard/ISmartCardDeviceListener.cs`, `DesktopSmartCardDeviceListener.cs` in that directory | Internal listener surface; keep public manager events. |
| modify | `src/Core/src/Transports/SmartCard/ISmartCardConnection.cs`, `UsbSmartCardConnection.cs`, `SmartCardConnectionFactory.cs`, `FindPcscDevices.cs` in that directory | Async transactions, bounded native lifecycle, partial-open cleanup and discovery reservation use. |
| add | `src/Core/src/Transports/SmartCard/ISmartCardTransaction.cs` | Public transaction lifetime contract. |
| modify | `src/Core/src/Abstractions/IProtocol.cs`, `src/Core/src/Protocols/Fido/Hid/IFidoHidProtocol.cs`, `FidoHidProtocol.cs` in the latter directory | Local configuration and explicit/lazy async initialization; remove the inherited redundant declaration. |
| modify | `src/Core/src/Protocols/Otp/Hid/OtpHidProtocol.cs`, `src/Core/src/Protocols/SmartCard/Apdu/PcscProtocol.cs`, `src/Core/src/Protocols/SmartCard/Scp/PcscProtocolScp.cs` | Same initialization contract; no-op where no work is needed, preserve guard/disposal checks. |
| modify | `src/Core/src/Protocols/Fido/Hid/FidoHidConnection.cs`, `src/Core/src/Protocols/Otp/Hid/OtpHidConnection.cs` | Await the report boundary and its asynchronous teardown; retain zeroing. |
| modify | `src/Core/src/Devices/YubiKeyDevice.cs`, `IYubiKeyConnectionSlot.cs`, `IDiscoveryConnectionProvider.cs`, `HidConnectionSlot.cs`, `PcscConnectionSlot.cs`, `RegisteredConnections.cs`, `DisposalGate.cs`, `DeviceConnectionRegistry.cs`, `ProtocolDeviceInfo.cs` in that directory | Thread opening context through factory/discovery entry points; arm release before open; add registry quarantine; bound existing metadata admission and replace async LongRunning scheduling. |
| modify | `src/Core/src/Devices/DiscoveryReadSkippedException.cs` | Distinguish native quarantine from ordinary transient interface contention. |
| add | `src/Core/src/Devices/NativeConnectionOpenContext.cs`, `IDeviceOwnershipLease.cs` | Internal opening context and typed existing registry registration contract, not a public factory parameter. |
| add | `src/Core/src/Native/NativeBlockingExecutor.cs`, `NativeCleanupReservation.cs` in that directory | Bounded work queues and reserved release obligations within the same execution owner. |
| add | `src/Core/src/Native/NativeExecutionContext.cs` | Thread-local owned-native callback/reactor/worker scope markers. |
| add | `src/Core/src/Devices/ConnectionLease.cs`, `ConnectionQuarantinedException.cs`, `NativeResourceCapacityException.cs` in that directory | Lease/release handshake and public typed failures. |
| modify | `src/Core/src/Utilities/ExchangeGuard.cs` | Reject synchronous drain from native worker/event contexts; preserve admission semantics. |
| modify | `src/Core/src/Sessions/ApplicationSession.cs`, `RawSmartCardSession.cs`, `RawFidoHidSession.cs`, `RawOtpHidSession.cs` in that directory | Await initialization/factory-failure cleanup; preserve connection ownership. |
| modify | `src/Management/src/ManagementSession.cs`, `src/Piv/src/PivSession.cs`, `src/Fido2/src/FidoSession.cs`, `src/Oath/src/OathSession.cs`, `src/YubiOtp/src/YubiOtpSession.cs`, `src/OpenPgp/src/OpenPgpSession.cs`, `src/SecurityDomain/src/SecurityDomainSession.cs`, `src/YubiHsm/src/HsmAuthSession.cs` | Await the common failure-cleanup helper; preserve applet wire logic and factory grammar. WebAuthn continues delegating through Fido2. |
| modify | `src/Core/src/PublicAPI.Unshipped.txt` | Exact retained/removed/new consumer signatures; protected helper change included. |

Internalize the existing `HidDeviceChangeKind.cs` and `HidReportType.cs` files;
do not invent new enum definitions. `ConnectionTypeMapper` must move with `HidInterfaceType`;
otherwise its public parameters would expose an internal type. Keep public `IDevice`,
`IConnection`, `ConnectionType`, manager device/event types and domain values.

Retain `IPcscDevice`, the `FindPcscDevices` producer, `ISmartCardConnectionFactory`,
`SmartCardConnectionFactory` and their required public dependencies: this is an existing
direct raw smart-card composition path. `UsbSmartCardConnection` and `SCARD_DISPOSITION`
are already internal and remain internal, including the implementation-only disposition
overload. No native enum or implementation type becomes newly public. No platform namespace
is blanket-internalized. Public device enumeration/events are served by the manager;
typed raw acquisition and external connection implementations remain supported.

### Platform files and native seam placement

| Owner | Existing files to modify | Proposed files to add |
|---|---|---|
| Windows | `src/Core/src/Native/Windows/Kernel32/Kernel32.Interop.cs`; `src/Core/src/Native/Windows/HidD/WindowsHidReportAccess.cs`, `IWindowsHidReportAccess.cs`; `src/Core/src/Transports/Hid/Windows/WindowsHidInterface.cs`, `WindowsHidDeviceListener.cs`, `WindowsHidIOReportConnection.cs`, `WindowsHidFeatureReportConnection.cs` | `src/Core/src/Native/Windows/HidD/IWindowsHidReportApi.cs`, `WindowsHidReportApi.cs`, `WindowsHidOverlappedOperation.cs` |
| macOS | `src/Core/src/Native/MacOS/IOKitFramework/IOKitHid.Interop.cs`; `src/Core/src/Transports/Hid/MacOS/IIOKitDeviceLifetime.cs`, `MacOSHidInterface.cs`, `MacOSHidDeviceListener.cs`, `MacOSHidIOReportConnection.cs`, `MacOSHidFeatureReportConnection.cs` | `src/Core/src/Native/MacOS/IOKitFramework/IOKitDispatch.Interop.cs`; `src/Core/src/Transports/Hid/MacOS/IIOKitDeviceEventOwner.cs`, `IOKitDeviceEventOwner.cs`, `IIOKitReportApi.cs`, `IOKitReportApi.cs`, `IOKitReportOperation.cs`, `HidReportReceiver.cs` |
| Linux | `src/Core/src/Native/Linux/Libc/Libc.Interop.cs`; `src/Core/src/Transports/Hid/Linux/LinuxHidInterface.cs`, `LinuxHidDeviceListener.cs`, `LinuxHidIOReportConnection.cs`, `LinuxHidFeatureReportConnection.cs` | `src/Core/src/Transports/Hid/Linux/ILinuxHidReportApi.cs`, `LinuxHidReportApi.cs`, `ILinuxHidReadReactor.cs`, `LinuxHidReadReactor.cs` |
| Smart card | `src/Core/src/Native/Desktop/SCard/SCardContext.cs`, `SCardCardHandle.cs`, `SCard.Interop.cs`; shared smart-card lifecycle files above | `src/Core/src/Transports/SmartCard/ISCardConnectionApi.cs`, `SCardConnectionApi.cs` |

No discovery-matching rewrite is included. Existing status-monitor seams retain their
scope. The new smart-card seam is a sibling of `ISCardApi`, not a silent enlargement
of the discovery/status interface. Windows tests can control submission below the
real `WindowsHidReportAccess`; a fake asynchronous `IWindowsHidReportAccess` alone is insufficient.

Native-lineage files, **conditional on verified source/base/package approval**:

- Add `Yubico.NativeShims/macos/hid_dispatch_owner.h` and `hid_dispatch_owner.m`.
- Modify `Yubico.NativeShims/CMakeLists.txt` for an Apple-only bridge target source,
  block support and existing IOKit/CoreFoundation dependencies.
- Add `Yubico.NativeShims/tests/expected_symbols.macos.txt`; adapt existing
  `tests/check_exports.sh` and `tests/check_exports.ps1` to platform-scoped export
  expectations while retaining the common `tests/expected_symbols.txt` contract.
- Add `Yubico.NativeShims/tests/async_boundary_contract_harness.c`,
  `async_boundary_test_exports.h`, `async_boundary_test_exports.c` and
  `expected_test_symbols.txt` in that directory. Test exports compile only into a
  separately named, non-packaged `Yubico.NativeShims.AsyncBoundaryTests` library.
- Modify that lineage's `.github/workflows/build-nativeshims.yml` to execute the
  declared native/export/consumer matrix and retain producer/package evidence.

The unrelated `nativeshims-static-core` checkout is read-only. No new package version
or producing revision is asserted here. Additive macOS exports must not break existing
smart-card/crypto consumers; the consuming package pin changes only after evidence.

### Tests, tooling, examples and documentation

Use existing projects; no new shipping or generic analyzer project.

- Add `verification/async-boundaries/manifest.json` as the one boundary-disposition,
  required-profile and capability input.
- Under `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AsyncBoundaries/`, add
  `AsyncBoundaryManifest.cs`, `AsyncBoundarySemanticChecker.cs`,
  `AsyncBoundaryInventoryTests.cs`, `AsyncBoundaryContractRegistry.cs`,
  `AsyncBoundaryProbe.cs`, `NativeBlockingExecutorAsyncBoundaryTests.cs`,
  `NativeCleanupReservationAsyncBoundaryTests.cs`, and `AsyncBoundaryNegativeFixtureTests.cs`.
- Add textual fixtures there under `Fixtures/`: `UnknownNativeImport.cs.txt`,
  `UnknownDynamicLookup.cs.txt`, `DirectBlockingWrapper.cs.txt`,
  `InterfaceBlockingWrapper.cs.txt`, `AsyncWorkerJob.cs.txt`,
  `TaskResultFalsePositive.cs.txt`, `StaleManagedSymbol.json`, `StaleNativeExport.json`.
- Add platform profile/test files under existing Core unit-test directories:
  `Transports/Hid/WindowsHidAsyncBoundaryTests.cs`, `MacOSHidAsyncBoundaryTests.cs`,
  `LinuxHidAsyncBoundaryTests.cs`; `Transports/SmartCard/PcscLifecycleAsyncBoundaryTests.cs`;
  `Devices/DisposalAsyncBoundaryTests.cs`; `Protocols/Fido/Hid/FidoCancellationAsyncBoundaryTests.cs`;
  `Protocols/Otp/Hid/OtpRecoveryAsyncBoundaryTests.cs`.
- Modify existing `Transports/Hid/WindowsHidConnectionOpenFailureTests.cs`,
  `MacOSHidConnectionLifetimeTests.cs` and `Devices/HidConnectionSlotTests.cs`
  for the deliberate seam/open contract changes; keep their cleanup assertions.
- Add `src/PublicApi/tests/Yubico.YubiKit.PublicApi.UnitTests/RawConnectionConsumerContractTests.cs`
  with the compiled consumer implementation fixture in that project; update the existing
  examples in `src/Core/README.md` and `docs/architecture/raw-access-tiers.md`.
  There is no `src/Core/examples` directory to extend.
- Add Core integration tests under `src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/`:
  `Transports/Hid/HidAsyncBoundaryIntegrationTests.cs`,
  `Transports/SmartCard/PcscAsyncBoundaryIntegrationTests.cs`,
  `Devices/AsyncBoundaryLifecycleIntegrationTests.cs`.
- Modify the existing benchmark `Program.cs` and project file under
  `benchmarks/Yubico.YubiKit.PerformanceBenchmarks/`; add
  `AsyncBoundaryMeasurementRunner.cs` and `AsyncBoundaryMeasurementRecord.cs`.
- Modify `verification/NativeAotVerification/Program.cs` and
  `Yubico.YubiKit.NativeAotVerification.csproj`; add `AsyncBoundaryNativeContracts.cs`
  and `PackagedConsumerVerification.cs` there.
- Modify `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Yubico.YubiKit.Core.UnitTests.csproj`
  and `Directory.Packages.props` for test-only `Microsoft.CodeAnalysis.CSharp.Workspaces`
  and `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.6.0 (matching existing Roslyn),
  plus `Microsoft.Build.Locator` 1.11.2. These are proposed fixed versions, not restored
  dependencies; the latter two versions were confirmed in NuGet's package indices.
- Modify `toolchain.cs`, `TOOLCHAIN.md`, `docs/TESTING.md`, `docs/NATIVE-AOT.md`,
  `docs/architecture/raw-access-tiers.md`, `docs/architecture/applet-public-api.md`,
  `docs/migration/v1-to-v2-gaps.md`, `docs/v2-highlights.md`, and `src/Core/CLAUDE.md`.
- Modify `.github/workflows/build.yml` and `.github/workflows/native-aot.yml` incrementally
  with each platform, not only at closure.

### Transaction consumer fixture file list

Mechanical transaction-implementation updates are also required in these existing fakes:

```text
src/Tests.Shared/RecordingSmartCardConnection.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ConnectionOwnershipContractTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionOwnershipTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceConnectionRegistryTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PhysicalYubiKeyTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PcscConnectionSlotTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/SessionTransportTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/ProtocolFactoryTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Apdu/PcscProtocolConcurrencyTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Apdu/PcscProtocolTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Protocols/SmartCard/Apdu/Fakes/FakeSmartCardConnection.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSmartCardSessionTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSessionYubiKeyExtensionsTests.cs
src/Core/tests/Yubico.YubiKit.Core.UnitTests/Sessions/RawSessionConstructionFailureTests.cs
src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathAuthenticationTests.cs
src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathSessionTests.cs
src/Management/tests/Yubico.YubiKit.Management.UnitTests/ManagementSessionTests.cs
src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/Scp03HandshakeFakeConnection.cs
src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/OpenPgpInvalidPinExceptionTests.cs
src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/FidoSessionTests.cs
src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/Authentication/PivPinOnlyModeSessionTests.cs
```

Update `Devices/ApplicationSessionScpTests.cs` in Core unit tests for the protected
failure-cleanup helper; update FIDO initialization/concurrency tests in place for the
explicit initialization call. Existing green regressions are not rewritten merely to
produce a red test. Other applet source changes require demonstrated contract impact.

Additional existing protocol fake files requiring InitializeAsync are Core unit tests
`Devices/DeviceInfoReaderTests.cs`, `Sessions/ApplicationSessionDisposalTests.cs`,
`Sessions/SessionConstructionGuardTests.cs`, and
`src/SecurityDomain/tests/Yubico.YubiKit.SecurityDomain.UnitTests/SecurityDomainSessionTests.cs`.
ManagementSessionTests and ApplicationSessionScpTests are already listed above.
Opening-context adaptations also cover Core unit tests under `Devices/`:
`DiscoveryIdentityReaderTests.cs`, `FindYubiKeysFaultInjectionTests.cs`,
`FindYubiKeysPidMergeTests.cs`, `HeldExceptionPropagationTests.cs`,
`DiscoverySingleFlightTests.cs` and the existing slot/registry tests. Fakes using the
non-openable slot default retain that behavior without creating native reservations.

## Types and signatures

### Internal report and initialization contracts

```csharp
internal interface IHidConnection : IConnection
{
    int InputReportSize { get; }
    int OutputReportSize { get; }
    Task SetReportAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken);
    Task<ReadOnlyMemory<byte>> GetReportAsync(CancellationToken cancellationToken);
}

internal interface IHidInterface : IDevice
{
    HidDescriptorInfo DescriptorInfo { get; }
    HidInterfaceType InterfaceType { get; }
    Task<IHidConnection> OpenIOReportsAsync(
        NativeConnectionOpenContext context, CancellationToken cancellationToken);
    Task<IHidConnection> OpenFeatureReportsAsync(
        NativeConnectionOpenContext context, CancellationToken cancellationToken);
}

internal interface IProtocol : IDisposable
{
    void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null);
    Task InitializeAsync(CancellationToken cancellationToken = default);
    void EnsureCanDisposeFromCurrentContext();
}

// ApplicationSession: replace the synchronous failure helper.
protected ValueTask DisposeAfterInitializationFailureAsync();
```

Report mode is selected by opening, not by a second interface. Returned memory has
stable owned contents; no view into a reused callback buffer. DisposeAsync remains
inherited through the existing connection lifetime contract. Typed public report
SendAsync/ReceiveAsync signatures remain unchanged. Configure becomes local-only;
FIDO does explicit initialization before session publication and preserves lazy raw
initialization. The smart-card and OTP implementations complete initialization locally;
authenticated decorators do not re-run handshakes. Preserve their disposal/admission checks.

All async factory catch paths await the common failure cleanup while preserving the
original initialization exception. The local Construct/binding-refusal path stays local:
constructors cannot acquire native resources before binding. A custom implementation's
blocking override is not silently made responsive by the library.

### Public transactions and errors

```csharp
// ISmartCardConnection: replace BeginTransaction; other members retained.
Task<ISmartCardTransaction> BeginTransactionAsync(
    CancellationToken cancellationToken = default);

public interface ISmartCardTransaction : IDisposable, IAsyncDisposable { }

// Internal UsbSmartCardConnection: implementation-only overload; enum remains internal.
internal Task<ISmartCardTransaction> BeginTransactionAsync(
    SCARD_DISPOSITION endDisposition, CancellationToken cancellationToken = default);

public sealed class NativeResourceCapacityException : InvalidOperationException
{
    public string Resource { get; }
    public int Capacity { get; }
    public NativeResourceCapacityException(string resource, int capacity);
}

public sealed class ConnectionQuarantinedException : InvalidOperationException
{
    public string InterfaceId { get; }
    public long Generation { get; }
    public string OperationName { get; }
    public ConnectionQuarantinedException(
        string interfaceId, long generation, string operationName,
        Exception? innerException = null);
}
```

There is no shipping applet transaction-acquisition caller to migrate; the only
shipping forwarding call is `RegisteredConnections.cs:43-44`. Chaining/secure-channel
protocols must keep their existing state guarantees, but this change does not add
transactions to them. The new scope defaults to LEAVE_CARD, ends exactly once, and
does not dispose the caller's connection. Scope disposal and connection disposal
share the same end state; neither repeats a possibly completed mutation.

Begin outcome linearization occurs after native return: a native failure wins over
cancellation; on success read caller cancellation once. If not cancelled, transfer
the scope to the caller even if cancellation arrives later. If cancelled, end the
newly acquired transaction in the same synchronous worker job before completing
cancelled. An end failure wins over cancellation and faults the connection; subsequent
ordered disconnect/release uses its existing cleanup reservation. No native command
is replayed. The current synchronous implementation already drains running begins;
this policy prevents abandonment in the new asynchronous path.

Other submitted raw operations return their observed native result; a token racing
after dispatch does not manufacture cancellation or rollback. Protocols retain their
separate cancellation/recovery policy. Queue cancellation before dispatch always
prevents submission. All borrowed input remains protected until public terminal completion.

### Execution and bounded release obligations

```csharp
internal enum NativeWorkClass { Interactive, Discovery }

internal readonly record struct NativeConnectionOpenContext(
    NativeWorkClass WorkClass, long Generation, NativeCleanupReservation? Cleanup);

// Additional/changed members of the existing internal slot/provider interfaces.
// Existing identity/type members are retained.
bool UsesNativeCleanupReservations { get; } // IYubiKeyConnectionSlot
Task<IConnection> OpenRawConnectionAsync(ConnectionType connection,
    NativeConnectionOpenContext context, CancellationToken cancellationToken);
bool SupportsNativeCleanupReservations(ConnectionType connection); // IDiscoveryConnectionProvider
Task<IConnection> ConnectForDiscoveryAsync(ConnectionType connection,
    NativeConnectionOpenContext context, CancellationToken cancellationToken);

// New internal overload on the existing public concrete SmartCardConnectionFactory.
internal Task<ISmartCardConnection> CreateAsync(IPcscDevice device,
    NativeConnectionOpenContext context, CancellationToken cancellationToken);

internal sealed class NativeBlockingExecutor
{
    internal static NativeBlockingExecutor Shared { get; }
    internal Task<T> RunAsync<T>(
        NativeWorkClass workClass, Func<T> nativeCall,
        CancellationToken cancellationToken);
    internal NativeCleanupReservation ReserveCleanup(
        NativeWorkClass origin, string interfaceId, long generation);
    internal static long NextGeneration();
}

internal static class NativeExecutionContext
{
    internal static bool IsNativeContext { get; }
    internal static IDisposable Enter();
}

internal readonly record struct NativeReleaseResult(bool Released, Exception? Error);

internal sealed class NativeCleanupReservation
{
    internal Task NativeReleased { get; }
    internal Task CleanupCompleted { get; }
    internal void ObserveQuarantine(Action<string, Exception?> observer);
    internal void Bind(object nativeState, Func<NativeReleaseResult> release);
    internal void MarkAcquisitionStarted();
    internal void MarkQuarantined(string operationName, Exception? error);
    internal void MarkReady();
    internal void ReturnUnused();
}

internal sealed class ConnectionLease
{
    internal ConnectionLease(
        IDeviceOwnershipLease registration, NativeCleanupReservation? reservation);
    internal Task Released { get; }
    internal void ArmRelease();
    internal void ObserveDisposalCompletion(Exception? error, IConnection connection);
}

internal interface IDeviceOwnershipLease : IDisposable
{
    long Generation { get; }
    void MarkQuarantined(string operationName, Exception? error, object nativeState);
}
// Changed internal registry members; existing lookup APIs retained.
ValueTask<IDeviceOwnershipLease> AcquireConnectionAsync(
    IReadOnlyCollection<string> interfaceIds, long generation, CancellationToken cancellationToken);
IDeviceOwnershipLease? TryAcquireDiscovery(string interfaceId, long generation,
    out DiscoveryReadSkipCause? skipCause);
ConnectionQuarantinedException? GetQuarantine(string interfaceId);
```

The opening context carries the proposed reservation explicitly. On the built-in
factory route, YubiKeyDevice.ConnectAsync allocates a generation, acquires the grouped
registry lease, reserves an Interactive cleanup credit, and arms ConnectionLease
**before** passing context to the slot and adapter. The decorators are created only
after open succeeds and accept that already-armed lease; they cannot arm it before open.
The actual adapter binds platform-native state before beginning native acquisition.
Every operation on that connection retains its origin WorkClass.

Discovery's ProtocolDeviceInfo path allocates a generation, takes its existing discovery
lease, reserves a Discovery credit and passes context through IDiscoveryConnectionProvider
and the same slots. It keeps the discovery lease until release evidence, not merely
until a budgeted read stops awaiting. Opening cancellation with no acquisition returns
the unused credit, successfully completes its no-resource release evidence, and releases
the registry claim. Acquired or uncertain state uses checked reserved cleanup instead.

An ordinary interactive acquisition may wait while a discovery attempt is still
within its existing read budget. When that budget expires, its result is abandoned,
or teardown faults while native release remains pending, the attempt's owner calls
MarkQuarantined on its still-held discovery lease. Under the registry lock, set the
generation's quarantine state and fault its discovery-release notification with
ConnectionQuarantinedException. This wakes already-waiting connection acquisitions;
new acquisitions check quarantine before entering the wait. **Do not release the
physical lease to wake callers.** Retain it until positive release evidence, when the
matching generation's quarantine clears. A race with safe release is a no-op on the
retired generation, never a quarantine of a replacement. Apply the existing read
budget through TimeProvider; do not infer that its expiry means the driver stopped.
Test this for grouped keys, where one discovery-held interface otherwise parks an
acquisition of all member interfaces. Track the attempt's lease in the existing
ProtocolDeviceInfo worker state so the budget observer can make this transition.

HID slots always support this internal tracking. PcscConnectionSlot supports it when
its factory is the built-in SmartCardConnectionFactory and calls the internal overload.
The public factory overload keeps its existing signature, self-reserves Interactive
credit and uses the same internal path without creating a physical registry lease.
Direct public consumers share Interactive capacity; no separate consumer reservation
is promised. A consumer-supplied ISmartCardConnectionFactory remains accepted but does
not claim built-in native tracking: its slot reports false and context.Cleanup is null.
Its documented factory-failure cleanup and successful DisposeAsync provide external
contract evidence, not native proof. ConnectionLease uses a null reservation for this
path: ObserveDisposalCompletion publishes external success only after disposal returns
successfully; on failure it faults Released and roots the external connection on the
registration. Failed external construction must leave no resources under that factory's
public contract; the library cannot track hidden handles it was never given.
A failed disposal retains/quarantines its registry
claim; no built-in cleanup worker invokes arbitrary consumer disposal code. Public
connection implementations are responsible for their own native lifetime and scheduling.

Generations are positive, process-wide monotonically allocated Int64 values from
NextGeneration at connection-attempt start; overflow rejects before acquisition and
values are never reused. The same value travels through the lease, context, reservation,
native operation and readiness registration. Linux compares both registration identity
and generation when a descriptor is reused. Release continuations hold the original
registration object and compare generation; no dictionary removal/recreation is used
as an identity shortcut.

The executor has two ordinary queues and a reserved cleanup-ready queue, all under
one owner. No public executor injection. `RunAsync` accepts no Action overload, so an
async-void delegate cannot bind. At entry reject Task/ValueTask result types before
dispatch. The semantic gate also rejects async lambdas/method groups, task-like result
erasure and worker bodies invoking asynchronous delegates. Unresolved relevant delegate
targets require review; this is not a claim that C# can prove arbitrary delegates synchronous.
Worker bodies that call Task/ValueTask awaiter GetResult, Task.Wait or Task.Result
are rejected as managed blocking, even when their delegate return type is synchronous.
No poison-overload maze or runtime reliance on preserved async-state-machine attributes.
Void native calls return a small synchronous status value through the same contract.

Queue insertion versus cancellation and dispatch has one locked/atomic winner.
Full queue returns a faulted task with NativeResourceCapacityException without native
submission; no second admission waiter list. The job retains its slot until native
return. Task completions use asynchronous continuations; workers invoke no app callback.
Cleanup admission uses ReserveCleanup, not another public RunAsync work class.
Every completion source in operation state, report reception, NativeReleased,
CleanupCompleted, ConnectionLease.Released, IOKitDeviceEventOwner.Quiescence and registry
notifications uses RunContinuationsAsynchronously. Native callbacks only record/copy
and publish; they do not invoke an async teardown method's synchronous prefix inline.

Reservation state is finite: Reserved → AcquisitionStarted → Ready → Running → Released,
with Quarantined as a diagnostic state while awaiting prerequisite completion, and
ReleaseFaulted as a retained terminal-failure state. Bind happens before acquisition;
allocate queue/callback bookkeeping before any native resource exists. ReserveCleanup
throws before open if the origin's credits are exhausted. MarkAcquisitionStarted runs
inside the admitted job immediately before the first native acquisition attempt, so a
blocked/partially successful native call cannot appear unused. ReturnUnused is valid
only before that transition; partial opening instead runs checked cleanup.

MarkReady is one-shot and only follows closure of native admission, terminal completion
of accepted operations, and callback/deregistration quiescence. One preallocated queue
entry per ticket makes readiness enqueue bounded and non-rejecting. Live/quarantined
not-ready owners consume a credit but no cleanup worker. There is no saturation retry
scanner, hung-call detector or detached native job. One hung cleanup consumes one
worker until real return; two may stall progress and eventually exhaust reservation credits.
For an already-open owner awaiting those workers, DisposeAsync returns promptly but
its task can remain pending indefinitely; the documented synchronous Dispose can block
indefinitely. There is no inferred stall timeout or early success/failure that releases
ownership. New acquisitions fail only on actual credit exhaustion. Diagnostics report
occupied workers and waiting release, not an invented “hung” classification.

The reservation roots **platform-local native state**, not the public connection or
session wrapper. Release closures and static callbacks must not capture those wrappers.
That state owns handles, pinned buffers, callback contexts and accepted-operation counts;
its positive NativeReleased task survives dropped caller references. A wrapper finalizer
may request the same nonblocking shutdown path, but cannot report completion or free
storage. Validate this reference direction explicitly so ordinary abandonment does
not accidentally prevent the wrapper's finalizer from requesting cleanup. SafeHandle
fallbacks remain classified; no finalizer can reclaim storage still rooted for native use.

NativeReleased succeeds only after platform-specific positive evidence establishes
release and storage can be reclaimed. NativeReleaseResult deliberately separates that
fact from an error: `(true, error)` permits physical release while CleanupCompleted
reports the cleanup error; `(false, error)` faults both tasks, retains credit/state and
marks the interface quarantined. No automatic ambiguous release retry. A merely late native
completion is different: it can satisfy prerequisites, run the one release job and
eventually succeed. Removal/replug is not proof. Admission/refusal and release are
generation-keyed; an old completion never releases a replacement's claim.
For `(true, error)`, DisposeAsync and its synchronous facade report that error after
physical/registry release has completed; they do not silently return success. The
connection may then be reopened because release was proven, not because the error
was ignored. All concurrent disposal callers observe the same recorded cleanup outcome.

DisposalGate keeps one-shot shared completion but loses automatic physical-lease
release authority. YubiKeyDevice/the discovery owner arm ConnectionLease before native acquisition;
RegisteredConnections receive it after opening. The release continuation runs only on
successful NativeReleased and completes Released
after the existing registry registration is disposed. A successful registered DisposeAsync
awaits Released, so immediate reopening cannot race the continuation. Faulted teardown
may return an error with ownership retained; later safe cleanup still releases it.
ObserveQuarantine is a single internal observer installed by ArmRelease, invoked after
releasing native-state locks; it marks the original registry generation and uses the
reservation itself as the retained-state root. It is not an application callback.
ObserveDisposalCompletion reports other teardown faults while NativeReleased remains
pending; it never turns a built-in successful Dispose return into native evidence.
ReleaseFaulted faults NativeReleased and Released, so waiters observe failure rather
than hanging on a success-only signal. This terminal checked-release failure is distinct
from still-pending native work, whose evidence task can later succeed. Registry-release
failure also faults Released and is never reported as successful disposal.
Failed pre-acquisition opening releases the unused credit and registry claim explicitly.
The existing InterfaceOwnership entries in DeviceConnectionRegistry hold the current
generation's quarantine diagnostic and root, set through IDeviceOwnershipLease for every
member of a grouped lease. Claim checks that state before ordinary in-use refusal and
throws ConnectionQuarantinedException. IsInUse/IsInterfaceInUse remain conservative true
for quarantine; add an internal diagnostic lookup for metadata/monitor reporting so
that a boolean use check does not hide the reason. Matching safe release clears only
that generation's diagnostic. For unregistered direct connections, the reservation
retains the diagnostic for disposal errors but introduces no physical-registry promise.
Add `DiscoveryReadSkipCause.NativeQuarantined` in DiscoveryReadSkippedException.cs.
TryAcquireDiscovery supplies the skip cause atomically with its refusal: quarantine
is distinct from InterfaceLeaseHeld. Metadata/monitor paths preserve that cause and
do not attempt native probing while quarantine remains. Later scans may check registry
state cheaply; a matching safe release permits a new probe under a fresh epoch. Do not
label permanent quarantine as ordinary transient contention or silently drop the device.

SafeHandle.Dispose returning is not checked native-release evidence. Add explicit
checked release to the smart-card handle/context owners, inspect native status, and
invalidate a SafeHandle only after platform-specific release evidence with no outstanding
native use. Unknown release state retains ownership rather than retrying blindly.
In particular, Linux close can release the descriptor even when it reports EINTR or
another error. After verified deregistration/drain, apply Linux's actual close semantics,
invalidate the old handle when release is established, report the error separately,
and never retry close on a possibly reused descriptor. EBADF/contradictory ownership is
a distinct fault to classify, not permission to close some newly reused descriptor.
NativeExecutionContext is a thread-local depth scope, entered/restored in every owned
blocking worker, Windows completion callback, macOS callback and Linux reactor callback.
It does not flow through asynchronous continuations. Synchronous disposal checks it
before changing disposal state. ExchangeGuard separately tracks a linked, immutable
AsyncLocal execution frame while running the admitted delegate; its close/drain and
each protocol's EnsureCanDisposeFromCurrentContext reject draining that same exchange
from its own execution context. ApplicationSession calls the protocol check before
BeginDisposal in both disposal forms. This detects prompt-callback self-drain without
rejecting a different caller disposing a genuinely active exchange. It cannot identify
arbitrary native threads created by consumer implementations, which retain their own
responsibilities. Shared-pool pressure tests cover managed continuation progress;
there is no guarantee against application-wide starvation.
Retire the execution frame when its exchange exits and compare its admission identity;
an inherited frame in a later child task cannot impersonate a new active exchange.

Finalizer changes are explicit: the two macOS report connections currently close/release
and free callback handles in their finalizers; persistent delivery invalidates that
assumption. They instead request shutdown of the separately rooted native state.
The two Linux report-connection finalizers must likewise respect reactor deregistration
and accepted native work. Inspect `WindowsHidDeviceListener`, `MacOSHidDeviceListener`,
`LinuxHidDeviceListener` and `DesktopSmartCardDeviceListener` finalizers as retained
listener-owner paths; clearing callbacks/unregistering/joining is not proof of unrelated
report completion. Smart-card SafeHandle fallback ReleaseHandle methods remain listed
separately, and new checked paths must prevent finalizer double release. The finalizer
test matrix covers each named owner, including partial construction and dropped wrappers.

### Platform seams

Windows extends the existing connection-facing `IWindowsHidReportAccess` with Task-returning input/
output methods and asynchronous disposal; synchronous feature methods remain under
bounded execution. The lower seam is synchronous submission, below the real owner:

```csharp
internal enum WindowsIoSubmission { Accepted, Rejected }
internal unsafe interface IWindowsHidReportApi
{
    SafeFileHandle OpenIoHandle(string devicePath);
    IWindowsHidOverlappedBinding Bind(SafeFileHandle handle);
    WindowsIoSubmission SubmitRead(SafeFileHandle handle, byte* buffer, uint length,
        NativeOverlapped* overlapped, out int error);
    WindowsIoSubmission SubmitWrite(SafeFileHandle handle, byte* buffer, uint length,
        NativeOverlapped* overlapped, out int error);
    bool Cancel(SafeFileHandle handle, NativeOverlapped* overlapped, out int error);
    bool Close(nint handle, out int error);
}
internal unsafe interface IWindowsHidOverlappedBinding : IDisposable
{
    NativeOverlapped* Allocate(IOCompletionCallback callback, object state, object buffer);
    void Free(NativeOverlapped* overlapped);
}
```

Use ThreadPoolBoundHandle with the default completion-notification policy; do not enable
skip-on-success notification modes. Accepted includes immediate OS success **and** pending
submission: both finish through their completion notification, never through an additional
manual success completion. Rejection promises no callback. Prepare roots before submission;
if completion races submission return, defer storage reclamation until both are observed.
CancelIoEx is only a request, including its already-completed/not-found race. Retain the
handle reference, owned report-ID-normalized buffer and overlapped storage until terminal
completion. Verify the actual policy in the Windows native harness, including Native AOT.
WindowsHidReportAccess remains the single handle owner; its OpenIOConnection delegates handle
creation to OpenIoHandle using read/write access plus FILE_FLAG_OVERLAPPED. Remove its
NULL-overlapped ReadFile/WriteFile calls on that handle rather than wrapping them.
Feature handles keep the separate zero-access synchronous mode. Teardown closes admission,
requests cancellation for terminal transport shutdown, observes every accepted operation's
completion, frees its operation storage, disposes ThreadPoolBoundHandle, then checked-closes
the SafeFileHandle. Closing the handle is not used as a substitute for observed completion.

macOS retains IIOKitDeviceLifetime for creation/open/close/property ownership; change
`CloseDevice(nint device)` to return the native `int` status rather than discard it,
and keep reference release distinct from device close. Do not broaden its scope into discovery. Add event
and report-submission siblings:

```csharp
internal interface IIOKitDeviceEventOwner : IAsyncDisposable
{
    void Activate();
    void Cancel();
    Task Quiescence { get; }
}
internal unsafe interface IIOKitReportApi
{
    int SubmitSetReport(nint device, uint reportType, nint reportId,
        byte* report, nint length, double nativeTimeout,
        delegate* unmanaged[Cdecl]<nint, int, nint, uint, uint, byte*, nint, void> callback,
        nint context);
    int SubmitGetReport(nint device, uint reportType, nint reportId,
        byte* report, nint* length, double nativeTimeout,
        delegate* unmanaged[Cdecl]<nint, int, nint, uint, uint, byte*, nint, void> callback,
        nint context);
}
```

Input uses persistent event ownership and a bounded receiver. Separate feature GET,
feature SET and output SET direction decisions: callback operation if its contract is
verified, otherwise existing synchronous native calls on bounded workers. Register
and root before activation. Input memory is copied before callback return. The native
callback performs no public callback or task continuation inline.

For accepted report requests, context, device and buffers survive the terminal report
callback. Immediate callback-before-submit-return is supported defensively; rejection
retains no storage and produces no callback. A caller cancellation is not a per-report
native abort. **Teardown order:** stop admission → drain accepted GET/SET operations →
request dispatch cancellation → observe event quiescence → checked close → destroy
event owner → reclaim buffers/context and release the lease. Do not cancel event
delivery first if pending reports depend on it to complete. Never-completing reports
retain ownership; they are not converted into a successful timeout release.

Installed Apple headers describe the report timeout as milliseconds, while published
older source multiplies the supplied CFTimeInterval by 1000. Leave nativeTimeout as
an explicitly native-valued argument until a native probe resolves this discrepancy.
No callback direction is enabled in production with an invented timeout conversion.

Conditional native bridge declarations, fixed-width types and cdecl callbacks only:

```c
typedef struct Native_IOHIDDispatchOwner Native_IOHIDDispatchOwner;
typedef void (*Native_IOHIDInputReportCallback)(void *context, int32_t status,
    uint32_t report_type, uint32_t report_id, const uint8_t *report, intptr_t length);
typedef void (*Native_IOHIDRemovalCallback)(void *context, int32_t status);
typedef void (*Native_IOHIDQuiescenceCallback)(void *context);

uint32_t Native_IOHIDDispatchOwner_GetVersion(void);
int32_t Native_IOHIDDispatchOwner_Create(void *borrowed_device,
    intptr_t maximum_input_report_length,
    Native_IOHIDInputReportCallback input_callback,
    Native_IOHIDRemovalCallback removal_callback,
    Native_IOHIDQuiescenceCallback quiescence_callback,
    void *context, Native_IOHIDDispatchOwner **owner);
void Native_IOHIDDispatchOwner_Activate(Native_IOHIDDispatchOwner *owner);
void Native_IOHIDDispatchOwner_Cancel(Native_IOHIDDispatchOwner *owner);
void Native_IOHIDDispatchOwner_Destroy(Native_IOHIDDispatchOwner *owner);
```

Version 1 denotes this proposed contract, not an existing export. Create retains the
borrowed device, owns its serial queue/input buffer and installs callbacks without
activation. Success returns one owner and no callback before activation; rejection
sets owner null, retains nothing and invokes no callback. Prepare/validate allocations
before attaching device state so rejection can honor that contract. Activate may deliver
callbacks before returning. Cancel is idempotent and delivers exactly one quiescence
callback after the first effective cancel; it does not account for report GET/SET.
Destroy requires quiescence plus all report completion and releases owned references.
No blocks, IOKit typedefs or dispatch layouts cross this interface. Check availability/
version before opening; a missing bridge selects the approved owned-run-loop fallback
or deterministic initialization failure, never a mid-command backend switch.

Linux uses a report seam separate from the existing udev event seam:

```csharp
internal readonly record struct LinuxHidReadResult(int BytesRead, int Error, bool Removed);
internal interface ILinuxHidReportApi
{
    LinuxFileSafeHandle Open(string path, bool nonBlocking);
    LinuxHidReadResult TryRead(LinuxFileSafeHandle handle, Span<byte> destination);
    int Write(LinuxFileSafeHandle handle, ReadOnlySpan<byte> report);
    int GetFeature(LinuxFileSafeHandle handle, Span<byte> report);
    int SetFeature(LinuxFileSafeHandle handle, ReadOnlySpan<byte> report);
    NativeReleaseResult Close(int descriptor);
}
internal interface ILinuxHidReadReactor
{
    ILinuxHidReadRegistration Register(int descriptor, long generation,
        Action<long, short> onReady);
}
internal interface ILinuxHidReadRegistration : IAsyncDisposable
{
    long Generation { get; }
    void Signal();
}
```

Error retains the native error number; classify EAGAIN, EINTR, removal and terminal
failures explicitly. The reactor owns poll/eventfd, registration and generation checks;
reads are nonblocking and bookkeeping short. Writes/ioctls never execute on it.
Cancellation/registration/removal use wake signals, not periodic polling delays.
Registration DisposeAsync acknowledges deregistration before descriptor close; late
readiness from a reused descriptor cannot complete a new generation. Returned lengths,
numbered/unnumbered reports and error translation are tested, not normalized by guessing.

Smart-card native calls remain synchronous beneath a new sibling connection seam:

```csharp
internal interface ISCardConnectionApi
{
    uint EstablishContext(SCARD_SCOPE scope, out SCardContext context);
    uint Connect(SCardContext context, string readerName, SCARD_SHARE share,
        SCARD_PROTOCOL protocols, out SCardCardHandle card, out SCARD_PROTOCOL active);
    uint Reconnect(SCardCardHandle card, SCARD_SHARE share, SCARD_PROTOCOL protocols,
        SCARD_DISPOSITION disposition, out SCARD_PROTOCOL active);
    uint Transmit(SCardCardHandle card, SCARD_IO_REQUEST protocol,
        ReadOnlySpan<byte> command, Span<byte> response, out int received);
    uint BeginTransaction(SCardCardHandle card);
    uint EndTransaction(SCardCardHandle card, SCARD_DISPOSITION disposition);
    uint Disconnect(nint card, SCARD_DISPOSITION disposition);
    uint ReleaseContext(nint context);
}
```

Pin spans only inside the executing synchronous job, not before queue admission or
across arbitrary callbacks. Discovery/status cancellation retains its independent
context. Partial connection failures release established contexts through the same
reserved, checked lifecycle. Native status values remain distinct from caller cancellation.

### Inventory, profiles and measurement contracts

One manifest contains `scanScopes`, `boundaries`, `requiredRows` and `capabilities`.
Each boundary records stable ID, managed documentation-symbol ID, native export/callback,
platform/transport/operation/route, execution/admission/lifetime owners, pre/post-dispatch
cancellation, borrowed/native last-use events, recovery/reuse, shutdown acknowledgment,
separate caller deadline/native timeout/teardown watchdog, status/exception/review owner,
profile, and artifact references for each evidence grade. No boolean “verified” shortcut.
Native declarations/exports are independently enumerated from the approved native build.

Required rows explicitly enumerate actual routes, not a Cartesian product with silent
skips. Not-applicable needs a reason. The scanner uses MSBuildWorkspace at test/tool
runtime, not project references from Core tests to the applets. Register the selected
.NET toolchain with Microsoft.Build.Locator before loading MSBuild assemblies, open
the ten explicit shipping project paths with source project references (not metadata
substitution), and require non-null GetCompilationAsync results with no unresolved
workspace/project load failures. Record evaluated target frameworks, platform symbols,
included source counts and exclusions. Fail if an expected source/project is absent.
Repeat applicable platform evaluations in the three-system verification matrix; do
not assume one host evaluates every conditional native source path. Test-only dependency
loading is isolated from shipping assemblies and source-generated outputs are recorded.
The scanner then
recognizes actual Task/ValueTask blocking members and known native/execution boundaries,
then follows direct/interface dispatch through known shipping implementations. Missing
targets or relevant indirect dispatch become findings. Site location is metadata, not
stable identity. Recognize approved scheduling boundaries without interpreting an await
of a blocking native leaf as safe. Native import counts alone are not a reachability proof.

```csharp
internal interface IAsyncBoundaryContractProfile
{
    string RowId { get; }
    Task InvokeAsync(AsyncBoundaryProbe probe, ReadOnlyMemory<byte> input,
        CancellationToken cancellationToken);
    ValueTask DisposeTargetAsync();
    Task AssertReusableAsync(CancellationToken cancellationToken);
}
internal sealed class AsyncBoundaryProbe
{
    internal Task NativeSubmissionObserved { get; }
    internal Task NativeCompletionObserved { get; }
    internal Task RecoveryObserved { get; }
    internal void ReleaseDispatch();
    internal void CompleteNative(int status);
    internal void SignalRemoval();
    internal void AcknowledgeQuiescence();
}
```

Every applicable row has exactly one profile and all mandatory probes run with nonzero
counts. Reject duplicate/orphan profiles, missing rows and stale managed/native symbols.
The profile invokes the real production entry on a dedicated context-bearing thread;
an invocation-return event must precede deliberately released native completion.
Always unblock the fake in cleanup. Native-pointer last-use is measured by retained
buffer counters in the native harness, not by freeing storage and hoping for a crash.

Measurement mode goes into the existing benchmark host. Raw records include run/scenario,
baseline/final, shipping and harness revisions, native producer/package/hash, operating
system/runtime/reader/firmware, invocation/return/native/recovery/public-terminal timestamps,
active/pending peaks, allocation, process/thread counts, idle activity, outcome and reuse.
Write raw rows/environment under `artifacts/async-boundaries/<run-id>/`. Unknown baseline
native timestamps or future executor counts remain null with a reason: they cannot be
inferred from public duration. Use untouched shipping source for baseline runs; additions
are tooling-only. A seam-controlled delay is managed evidence, not a hardware measurement.

Keep the native verification host's default behavior. Add explicit native-contract and
packaged-consumer modes; package mode uses PackageReferences with no shipping ProjectReference
fallback and checks its assets graph. Test-only native helpers are separately named and
never included in release exports/packages. Run actual native paths under ahead-of-time
compilation for Windows x64, Linux x64 and macOS arm64, recording path counts.

## Call stack

| Flow | Proposed order and lifetime boundary |
|---|---|
| Factory opening | Public device connection factory → existing physical lease → reserve origin cleanup credit → adapter local state → bounded/native open → initialization → registered typed connection. Any partial failure cleans acquired resources before releasing its lease. |
| Applet creation | Existing extension/static CreateAsync → Construct/bind → applet probe → local Configure → awaited InitializeAsync → optional secure-channel setup → publish. Catch awaits DisposeAfterInitializationFailureAsync. |
| FIDO/OTP raw operation | Public typed SendAsync/ReceiveAsync → internal FidoHidConnection/OtpHidConnection → async IHidConnection → platform native operation. Borrowed send input cannot outlive terminal public completion; returned data owns its storage. |
| Smart-card transmit | Public typed TransmitAndReceiveAsync → native-operation admission → executor → synchronous seam with pinning inside the job → native return → result/zeroing. A protocol caller separately retains its logical exchange/recovery state. |
| Transaction begin | Public BeginTransactionAsync → bounded worker → native begin → fault or cancellation linearization → return owned scope or finish compensating end → public completion. No detached begin. |
| Disposal | Close admission → drain accepted operations/protocol as applicable → platform callback/deregistration proof → reserved checked release job → NativeReleased success → registry release → shared disposal completion. A fault never grants physical reuse without that proof. |
| Late completion | Original operation generation → its native state → readiness/quiescence → original reserved release job → original lease only. Native faults are not replay triggers. |
| Discovery | Existing identity/monitor orchestration → discovery worker/credit class for blocking native work → generation-scoped result → publication only if still current. Monitor status waits retain their separate owner/context. |

Keep DiscoveryWorkerAdmission as a **whole asynchronous metadata-probe** admission
owner, but bound its waiting list at 16 and retain four admitted probes. Its existing
TryAcquire remains nonblocking; AcquireAsync must reject a full waiting list with
NativeResourceCapacityException rather than accumulating semaphore waiters. This is
distinct from the executor's four currently executing synchronous native calls: one
probe spans several calls, native-completion awaits and recovery. Do not take the
metadata permit again inside each leaf or apply it to unrelated discovery enumeration.
Replace the async LongRunning launch with explicitly classified Task.Run asynchronous
orchestration under that permit; do not submit the asynchronous delegate to the native
executor. This keeps a synchronously blocking external discovery-provider prefix off
the initiating thread without claiming dedicated-thread ownership after an await.
Every shipped blocking native leaf still uses the bounded Discovery execution class.
The scanner records this managed orchestration exception with its finite admission
owner and checks that shipping native calls below it use their approved boundary.
Capacity refusal is a recorded deferred/unavailable background-discovery result under
existing epoch/retry ownership, never false device absence. Retain deadline detachment
only where the discovery owner retains buffers, completion observation and release
evidence. Direct enumeration's saturation exception and background probing's deferral
must be tested separately, including existing synchronous-prefix/single-flight fixtures.

FIDO cancellation stays after a received keepalive and before the next read. No new
duplex requirement. If no more keepalives arrive, cancellation alone cannot terminate
the read or establish safe reuse. Overflow/removal/callback faults have explicit terminal
transport dispositions; they never masquerade as a successfully drained response.

## Test plan

Write contract → integration → end-to-end → focused unit tests, with the thinnest actual
adapter route first at Gate 4. New behavior must have a demonstrated red probe; a missing
type causing compilation failure alone does not prove responsiveness. Existing green
regressions and negative-fixture tests are retained without artificially forcing them red.

| Level / proposed test | What it proves; baseline/red condition |
|---|---|
| Contract: `HidOpen_WithheldNativeOpen_ReturnsBeforeRelease` | Existing factory opens synchronously; invoke the old route for the baseline ordering failure. |
| Contract: `ReportReadAndWrite_WithheldNativeCompletion_ReturnBeforeRelease` | Existing FIDO/OTP wrappers block before returning a task. Parameterize actual adapter routes. |
| Contract: `TransactionBegin_WithheldNativeBegin_ReturnsBeforeRelease` | Baseline synchronous begin blocks; proposed public async begin must return first. |
| Contract: `ConfigureAndInitialize_KeepNativeWaitOutOfConfigure` | Baseline FIDO Configure waits synchronously. Initialization is separately awaited. |
| Contract: `DisposeAsync_WithheldNativeRelease_ReturnsBeforeRelease` | Detect wrapper paths that currently enter synchronous disposal. |
| Contract: `ExternalRawConnection_CompilesAndSupportsBothSessionAndRawUse` | Compile/run the new transaction/public surface with an external implementation; not only internal friends. |
| Contract: `CancelBeforeDispatch_SubmitsNothing` | Queue cancellation wins before the job's dispatch linearization. |
| Contract: `BorrowedInput_NoAccessAfterPublicCompletion` | Instrument managed memory and native retained-buffer counters through the production adapter. |
| Contract: `ImmediatePendingRejectedAndRemoval_PublishOneTerminalResult` | Controlled races cover native submission-return versus callback and teardown. |
| Contract: `QueuedAndActiveWork_NeverExceedBounds` | Current incidental pool offloading supplies no such accounting; saturation includes all waiting admissions. |
| Contract: `DiscoverySaturation_PreservesInteractiveWorkersAndCleanupCredits` | Distinct execution and acquisition reservations remain usable. |
| Contract: `AsyncWorkerDelegate_IsRejectedBeforeDispatch` | Task/ValueTask results and semantic async/erased delegate fixtures fail; an async-void method cannot bind the API. |
| Contract: `ReservationExhaustion_PreventsNativeAcquisition` | No handle can be acquired without a bounded future release obligation. |
| Contract: `OneStalledCleanup_HealthyCleanupProgresses` | One worker remains usable; isolate the permanently blocked fake in a disposable process. |
| Contract: `TwoStalledCleanups_DoNotCreateWorkersOrUnboundedBacklog` | Bound reservations/ready entries, retain occupied slots, refuse new acquisition when credits exhaust. |
| Contract: `FaultedTeardown_DoesNotReleaseLeaseUntilNativeProof` | Current DisposalGate finally releases on failure; use controlled unresolved native work to demonstrate the red condition. |
| Contract: `DiscoveryBudgetExpiry_QuarantinesAndWakesConnectionWaiters` | A default-token grouped acquisition receives the typed quarantine error instead of waiting indefinitely after discovery's budget expires; the physical lease stays held. |
| Contract: `LateDiscoveryRelease_ClearsOnlyItsQuarantineAndPermitsFreshProbe` | Typed skip cause survives scans without native re-probing, then changes only after matching release evidence. |
| Contract: `LinuxCloseError_AppliesReleaseSemanticsWithoutRetryingReusedDescriptor` | Distinguish an error from unknown release; never double-close after EINTR/late write errors. Native status evidence and handle invalidation are separate assertions. |
| Contract: `SyncDisposeFromNativeOrOwnExchangeContext_IsRejectedBeforeStateChange` | Mark all platform callback/reactor scopes and self-drain contexts; a retired inherited frame cannot reject a different exchange. |
| Contract: `DroppedWrapper_NativeStateRemainsRootedButShutdownCanBeRequested` | Reference direction permits wrapper finalization without freeing pending native storage or losing cleanup. |
| Contract: `LateCompletion_ReleasesOnlyOriginalGeneration` | Removal/replug and callback races cannot release/complete a replacement. |
| Contract: `CancelledBegin_LateSuccessEndsBeforeCancellation` | New async policy returns no unowned transaction; native/cleanup faults outrank cancellation. |
| Contract: `ScopeAndConnectionDisposal_EndExactlyOnce` | Shared transaction end state, no duplicate end or hidden close of caller-owned connection. |
| Integration: Windows overlapped completion suite | Actual handle completion policy, CancelIoEx races, length validation and release ordering; fake success is not native proof. |
| Integration: Linux readiness suite | EAGAIN/EINTR, eventfd wake, deregistration, descriptor reuse and write/read isolation. |
| Integration: macOS event/report suite | Activation/cancel ordering, input copy, removal/overflow, report callbacks before/after submission return, drain-before-cancel, and separate quiescence. |
| Integration: `ReleasePackage_HasNoTestExports` | Inspect actual per-platform release artifact, not only source declarations. |
| End-to-end: K1–K3 non-destructive operation/reuse | Exact applet capability and firmware verified first; exercise available report/smart-card paths. Touch/removal/contended-transaction scenarios are explicit operator-assisted runs. |
| End-to-end: packaged and ahead-of-time consumer | Load pinned packages and execute required native paths on all three recurring target systems with nonzero counts. |
| Unit: inventory negative fixtures | Unknown import/dynamic lookup, blocking direct/interface wrapper, stale symbol/export and empty scope fail; unrelated Result property is not misclassified. |
| Unit: `RestoredSynchronousReportWait_FailsResponsivenessProbe` | Permanent harness sensitivity test; a completed Task signature cannot mask the deliberately restored blocking body. |
| Regression | Existing exchange/refusal, prompt pairing, secure-channel/chaining, OTP recovery, zeroing and applet byte-level suites stay green after each adapter/refit. |

Native report timeout units, GET versus SET viability and repeated macOS input/output
interaction need real native/hardware probes before enabling callback directions. No
fixture absence becomes a skipped pass. Disruptive/reset tests are not authorized by
the user's selection of three attached keys; use existing hardware-test categorization.

Existing verification commands (new AsyncBoundary tests do not exist yet):

```sh
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~AsyncBoundary"
dotnet toolchain.cs -- test --project PublicApi --filter "FullyQualifiedName~RawConnectionConsumerContract"
dotnet toolchain.cs -- test --project Core
dotnet toolchain.cs -- test --project PublicApi
dotnet toolchain.cs -- resilience --fast
dotnet toolchain.cs native-aot-contract-qa
dotnet toolchain.cs docs-qa
```

Use separate invocations or supported `&` filters, not `|`. Proposed new toolchain
targets are `async-boundary-inventory`, `async-boundary-measure --measurement-args`,
and `packaged-consumer-qa --package-version --rid`; they do not exist yet. Do not add a
redundant wrapper target for the existing focused test command. The existing
native-aot-contract-qa remains static validation, not runtime evidence.

## Least confident decisions and hard prerequisites

1. **Pre-acquisition cleanup reservations:** the bound closes a real hidden-backlog
   risk, but introduces an explicit resource-admission limit and reference-direction
   obligations. Validate dropped-wrapper/finalizer behavior, exactly-once readiness,
   origin isolation and checked-release faults before wider adapter work. The proposed
   numeric table is calibration, not performance evidence or a frozen shipping default.
2. **Native readiness evidence:** supported operating-system floors, callback timeout
   units, report-direction viability and native producer provenance remain unresolved.
   Signatures are proposed; native changes cannot start merely because this document exists.
3. **Public closure:** removing low-level HID enumeration/listeners must be accompanied
   by manager/raw consumer examples and public-baseline checks. Retained direct smart-card
   construction must still reserve cleanup without gaining undocumented registry semantics.
4. **Failure precedence:** the new begin cancellation policy is explicit, but native
   error/cleanup status mapping and finalizer fallback deserve native tests; no error
   becomes positive quiescence evidence just because a task or SafeHandle Dispose returned.
5. **Measurement completeness:** untouched baseline source cannot expose every proposed
   counter. Approve trace-based collection or mark unavailable fields honestly; do not
   backfill invented data or claim all S0/ISC-62 obligations already satisfied.

Architecture must be reopened if these contracts require a new shipping subsystem,
unbounded admission, removal of a retained raw-access use case, or concurrent FIDO
send/read requirements. Missing native/hardware evidence blocks the applicable work
or acceptance, not unrelated managed design. All implementation remains behind Gate 4.

## Evidence used for this draft

- Current source and public baseline; completed read-only architect and engineer research.
- Local descriptor observation above; no applet operation, firmware query, touch,
  removal, native completion experiment or benchmark was run for this draft.
- Installed `MacOSX15.5.sdk/.../IOKit.framework/Versions/A/Headers/hid/IOHIDDevice.h`,
  lines 233–317 (dispatch lifecycle), 354–375 (input callback), 663–748 (report callbacks),
  and `IOHIDBase.h:82–99` (report callback types). Header availability is not deployed proof.
- [Apple IOHIDFamily source at 777ccd9698845aadf711e32d843c8c9b777431d9](https://github.com/apple-oss-distributions/IOHIDFamily/blob/777ccd9698845aadf711e32d843c8c9b777431d9/IOHIDLib/IOHIDDeviceClass.m)
  and the older [IOKitUser-1445.40.1 report implementation](https://opensource.apple.com/source/IOKitUser/IOKitUser-1445.40.1/hid.subproj/IOHIDDevice.c.auto.html)
  distinguish report callbacks from dispatch lifetime and expose the timeout discrepancy.
- Current Microsoft guidance on [overlapped allocation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadpoolboundhandle.allocatenativeoverlapped?view=net-10.0),
  [freeing operation storage](https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadpoolboundhandle.freenativeoverlapped?view=net-10.0),
  and [cancelling pending operations](https://learn.microsoft.com/en-us/windows/win32/fileio/canceling-pending-i-o-operations).
  These are documentation inputs, not Windows runtime verification.
- [Linux close(2)](https://man7.org/linux/man-pages/man2/close.2.html): descriptor
  release and error reporting are distinct; retrying a failed close can hit a reused descriptor.

Abbreviations: API — application programming interface; HID — human interface device;
FIDO — Fast Identity Online; OTP — one-time password; CCID — chip card interface device;
USB — Universal Serial Bus; NFC — near-field communication; AOT — ahead-of-time compilation;
ISC — ideal state criterion. IOKit is Apple's device-driver framework name.

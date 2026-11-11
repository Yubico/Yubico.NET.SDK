# B1 portable connection and native-lifetime investigation

**Outcome:** all 49 primary files (9,747 lines), 1,078 primary semantic units and 126 assigned shared units have completed source/assertion dispositions. Twelve local candidate groups are ready for coordinator triage. The initial portable ownership vocabulary is available for B2/B3/B4/B5. Product source, central coverage, inherited registries, test assertions, API snapshots, index and refs remain untouched by B1.

Source baseline: `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. The record-only HEAD observed while serializing dispositions was `01a114d4e251e0f52b92f73393fbe1dca5d5c24e`; every read product path still matched the pinned Git content with explicit LF normalization, and every inventory raw/range hash matched. This metadata advance does not rebase the audit source.

## Evidence and coverage

| Record | Purpose |
| --- | --- |
| [Unit dispositions](investigation/B1/unit-dispositions.json) | Every original unit ID, complete range, original/current source and range SHA-256, read/disposition/review/validation fields |
| [File identities](investigation/B1/file-identities.json) | Primary/shared source identities, pinned Git blobs, raw/LF equality and exact read scope |
| [Coverage summary](investigation/B1/coverage-summary.json) | 1,204 units: 1,104 read-clean, 99 read-candidate, one read-exclusion |
| [Complete file evidence](investigation/B1/file-evidence.md) | Distinct implementation/assertion context for all 49 files, shared sections and narrow dependencies |
| [Local candidates](investigation/B1/candidates.md) | CORE-B1-C001–C012, separate documentation/runtime/policy/qualification facets and unchanged-path reopening maps |
| [Portable ownership states](investigation/B1/ownership-states.md) | Early vocabulary and native-claim handoff to later packets |
| [Original sources](investigation/B1/sources/register.md) | Exact PC/SC, .NET 10, Windows API and bounded NIST evidence, actors and limits |
| [Source fetch manifest](investigation/B1/sources/manifest.json) | Original URLs, access times, hashes and the preserved NIST HTTP 403 outcome |
| [Record verification and exact sample IDs](investigation/B1/verification.md) | Fresh ID/path/hash reconciliation, coordinate corrections and non-boilerplate clean nominees |

`READ-CLEAN` means the complete unit's documentation and implicit contract were checked in its scoped source/assertion context. It supplies neither an implementation acceptance nor fresh test/hardware execution. Independent review, compilation and platform verification remain pending. The one explicit shared exclusion is the ownership guide's platform-notes unit: portable no-fallback is checked here, while macOS seize/Windows access/elevation claims need B3/B4 native qualification. Binary diagram text-coordinate hashes reproduce the inventory's lossy administrative decoding; the raw binary hash and inspected render remain authoritative.

B1 read root AGENTS/CLAUDE, Core CLAUDE/README, nested Core test instructions, Tests.Shared and PublicApi instructions, campaign plan/inventory/dispatch/coverage/source ownership, workflow/style and SDK house style. The 126 shared units retain their unique research reader and future **SHARED** editor. Complete CLI transport getters were checked through the common CLI base/selector; whole applet operations remain later boundary work.

## Current ownership model

| Resource or state | Owner and acquisition | Completion/release evidence | Consequential limit |
| --- | --- | --- | --- |
| Physical known-member registration | YubiKeyDevice acquires sorted/deduplicated member IDs before native open | Native built-in owner releases the exact token after proof; wrappers use their configured failure policy | Process-local overlapping IDs; disjoint scoped views remain independent even with matching serial/topology |
| Discovery interface lease | SharedDeviceInfoRead claims one interface after PID/native admission | Transferred lease owns PID admission through actual native release | Outer timeout, cancellation or evidence retirement ends a wait, not existing native ownership |
| PC/SC context/card handle | PcscConnectionNativeState roots itself before acquisition; PcscNativeResources holds SafeHandles | One worker checks disconnect and context-release statuses before invalidation/disposal and token release | Failure retains rooted native state and claim; no safe retry or replacement worker is fabricated |
| Ordinary PC/SC work | One admitted native work item, no ordinary backlog | Token checked before invocation; task published after native borrow ends | Cancellation after dispatch may still return success and has no completion deadline |
| Transaction state | Separate Beginning/Active/EndRequested/Ended/EndFailed state on same owner | Scope/shutdown coalesce end, then shutdown disconnects/releases | Scope end failure currently logs and closes connection; connection disposal later reports it |
| SmartCard command input | Caller retains source memory | Actual raw transmit task becomes terminal after native use | An external WaitAsync timeout is a different task and gives no permission to clear input |
| SmartCard receive staging/result | Facade rents cleared pool output, copies exact returned bytes into independent result | Pool returned/cleared in finally after native return/copy | Successful returned bytes belong to caller; classify their contents before cleanup |
| Logical exchange | One protocol's ExchangeGuard, shared by base PC/SC and SCP decorator | Guard exits after complete exchange; disposal closes then drains it | Per-exchange exclusion does not serialize a multi-exchange public workflow or raw interleaving |
| Session binding | ConnectionSessionGuard identity table attaches after construction and before initialization | Exact holder detaches on disposal/failure | A borrowed connection's bookkeeping release does not establish protocol/device recovery |
| Session connection | Caller owns a directly supplied connection; convenience factory transfers hidden ownership internally | Session disposes its protocol then an owned connection | Sessions have no finalizer recovery; callback/task reachability can keep connection owners alive |
| Listener context/thread | Listener creates separate context and thread | Cancel request plus actual thread exit is the relevant drain | Current timeout/restart/partial-start paths and finalizer commentary are C003/C004 candidates |

Use the explicit states **admitted**, **wait canceled**, **native pending**, **terminal**, **drained**, **release proven**, and **uncertain claim retained**. Name the task/resource at each transition. Caller input lifetime and synchronous/asynchronous disposal dispatch are separate dimensions.

### Release and failure traces

1. **Successful registered open:** YubiKeyDevice claims members → PC/SC slot transfers token → built-in factory constructs facade/roots native owner → lazy worker establishes context/connects → task returns connection. A later native sharing violation in strict Exclusive maps to ConnectionInUseException with an inner native cause; there is no Shared retry.
2. **Partial open failure:** InitializeAsync catches failure → awaits same owner's shutdown → checked cleanup releases original registration → original exception rethrows. Unproven partial cleanup instead throws UnrecoveredConnectionException and retains quarantine. External factories communicate this distinction explicitly.
3. **Admitted raw transmit:** caller memory remains borrowed → worker calls synchronous native transmit → native returns → ordinary slot clears/task publishes → facade copies response and clears pool output. Caller cancellation after dispatch neither detaches native access nor automatically poisons a successful return.
4. **Dispose while native work is held:** admission closes → same shutdown task remains pending → worker finishes accepted call → ends transaction once if needed → disconnect → context release → registration release. Native/transaction error reporting and release proof remain distinct. A transaction-end error can coexist with successful native release/reopen; disconnect/context failure retains claim.
5. **Discovery budget expires:** bounded caller task times out → tracker marks active lease unrecovered → new public acquisition receives typed refusal. The underlying read owns its resources. Successful late native cleanup releases discovery lease, clears temporary unrecovered state and permits reopen; failure retains claim and PID admission.
6. **Protocol/session disposal:** protocol guard closes and drains one exchange; borrowed connection survives healthy sequential use. Active FIDO terminal wake may make it unsuitable for reuse; dispose/reopen after uncertainty. Multi-command methods and applet caches/firmware remain B5/boundary responsibility.

## Candidate priorities for triage

- **Source-backed resource/cadence paths:** C002 enumeration context cleanup starts after early exits; C003 listener UNAWARE success path can return immediately; C004 partial listener allocations/start/timeout restart; C009 post-open slot logger failure bypasses cleanup or releases external registration early.
- **Documentation/assertion precision:** C001 NIST/PIV application-state inference and requested-versus-observed power-off; C005 registered HID compatibility failure release; C006 scope end-error reporting; C007 stale strict-sharing/Linux wake and universal liveness wording; C008 public callback ownership/CLI device preference; C011 absent fake API names.
- **Contract decisions without an authorized runtime change:** C010 ambient ExecutionContext retention on PC/SC worker; C012 discovery-first/member-later wait precedence behind “immediate”/“atomic” ownership wording. C005/C006 also carry explicit future policy choices.

The detailed candidate record names exact current paths/lines, observed behavior, intended uncertainty, original-source support, actual test oracles, affected public guide/XML/example/internal-test/migration surfaces and precise reopening triggers. Candidate-associated product files remain unchanged and require explicit editor/reviewer disposition even when the existing passage is retained. The PNG/SVG and already-accurate Core sharing migration text are useful unchanged dependency examples. No canonical `CORE-001`, `CORE-DOC-*` or `CORE-IMP-*` ID was allocated.

## Inherited IDs retain their exact scopes

| Original run-qualified ID | B1 current evidence and disposition |
| --- | --- |
| `audit-v2-20261022-20261031:#18` / YESDK-1626 | **Resolved source for exact reader-name scope.** CreateMany frees prior allocations when allocation fails; finder finally clears/frees its owning array after status success, native status error or managed exception. Full allocation/free/managed-copy assertions were read in PcscReaderNameOwnershipTests. Commit `013f3587dc5ac130eff2702f5cba7e209d024e86` is the pinned-history fix. The distinct earlier context lifetime gap C002 and listener allocation gaps C004 stay separate. No fresh native leak result is claimed. |
| `audit-v2-20261022-20261031:#22` / YESDK-1630 | **Changed scoped policy; physical gap remains.** Strict Exclusive defaults at every built-in constructor layer; explicit fallback retries only on sharing violation; Shared direct and factory snapshot/override assertions are present. Requested UNPOWER is checked under fakes. C001 narrows normative/physical commentary; source policy is not cross-process PIN-state proof. |
| `piv-001:PIVCORE-001` | Same mechanism as #22 for portable Core policy; maintain distinct provenance. PIV-specific selected-application/PIN/cache/handle behavior belongs to the later PIV boundary and user-deferred physical lane. |
| `fixes-wave-001:CORE-001` | **Changed architecture, unresolved physical cause.** The original two-key serial 9681620 abandoned `hid:/dev/hidraw7:0001` read and single-key OATH success remain historical evidence. Registry/SharedDeviceInfoRead currently preserve claim through native release; fake PC/SC timeout/late-release evidence is a different transport and scenario. No matching current two-key physical capture explains that failure. A/B2 receive this dependency without reassigning its original ID or declaring closure. |

The original registry's 100 provenance rows/70 groups remain untouched. APDU response-owner/TLV questions stay B6, effective firmware/body gates stay B5, and PIV physical tests remain deferred. The reported incoming 4,442 unit executions, including 2,065 Core, and 197 overlapping resilience executions are integration evidence only. They are not new B1 executions or whole-Core physical audit evidence. Frozen campaign/integration/user artifacts remain unchanged.

## Complete lifetime examples for later validation

These are proposed composable consumer examples with explicit typed prerequisites. They were checked against current source by inspection and remain **VALID-NOT-CHECKED** for compilation. The coordinator should compile them in the existing validation foundation, rather than create a new generic host.

### Borrow a connection and keep callback work inside its lifetime

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

static async Task WithRawAsync(
    IYubiKey device,
    Func<RawSmartCardSession, CancellationToken, Task> action,
    CancellationToken cancellationToken)
{
    await using ISmartCardConnection connection =
        await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
    await using RawSmartCardSession session =
        await RawSmartCardSession.CreateAsync(connection, cancellationToken);

    // The returned task must represent every callback operation that uses session.
    await action(session, cancellationToken);
}
```

The callback must return/await every task using the session and unregister callbacks before its task completes. An awaited callback can still schedule detached work that outlives that await; the helper cannot infer or drain it. Keep external prompt/services alive through their last actual callback and dispose them only in their application-owned scope. Session disposal owns neither a caller-created connection nor the caller's service.

### Give a generic factory result explicit ownership

```csharp
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

static Task<OwnedRaw> OpenRawAsync(IYubiKey device, CancellationToken token) =>
    device.CreateSessionOverTransportAsync(
        ConnectionType.SmartCard,
        async (opened, ct) =>
        {
            var connection = (ISmartCardConnection)opened;
            RawSmartCardSession session = await RawSmartCardSession.CreateAsync(connection, ct);
            return new OwnedRaw(connection, session);
        },
        token);

sealed class OwnedRaw(ISmartCardConnection connection, RawSmartCardSession session) : IAsyncDisposable
{
    public RawSmartCardSession Session { get; } = session;

    // This small example has one owner and one disposal caller; it is not a shared-disposal wrapper.
    public async ValueTask DisposeAsync()
    {
        try
        {
            await Session.DisposeAsync();
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
```

Use `await using OwnedRaw owned = await OpenRawAsync(device, cancellationToken);` in the enclosing task. If session creation throws, the public helper disposes the connection; after success, the explicit result disposes both in the right order. Returning only the borrowed RawSmartCardSession from that delegate would leave its hidden connection without a deterministic owner.

### Clear borrowed input after the actual operation, including failure

```csharp
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Transports.SmartCard;

static async Task<ReadOnlyMemory<byte>> SendOwnedCommandAsync(
    ISmartCardConnection borrowedConnection,
    byte[] ownedCommand,
    CancellationToken cancellationToken)
{
    try
    {
        return await borrowedConnection.TransmitAndReceiveAsync(ownedCommand, cancellationToken);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(ownedCommand);
    }
}
```

Here the method is explicitly given ownership of `ownedCommand`, while the connection remains borrowed. A caller layering `WaitAsync` over this method may stop waiting early, but the method's own finally still runs only after its underlying transmit is terminal. Do not separately clear/reuse that same array at the outer wait boundary. The returned response has a separate caller-owned lifetime and may require its own clearing according to its contents.

## Verification recommendation for the coordinator

No build, test, formatter, native probe, device operation or subprocess test host ran in B1. Source fetches, original-PDF text extraction, Git reads and run-owned Python record checks were the only execution. One source extraction attempt lacked `bs4`; the original HTML remained retained and exact relevant passages were read directly. The direct NIST PDF request returned 403; one focused original-source search supplied its quoted clause, leaving independent original-PDF review outstanding.

### Managed regression lane

Use the current build/test skills and existing `dotnet toolchain.cs` workflow, one assigned run at a time. Set `TMPDIR`, `TMP`, `TEMP` and explicit logs/output to `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/B1` or the coordinator's home-backed run directory. `PcscIsolatedProbe` writes through Path.GetTempPath; preserve its child logs before toolchain retention overwrites them.

Recommended existing Core.UnitTests selectors: full class names for `DeviceConnectionRegistryTests`, `DeviceConnectionOwnershipTests`, `ConnectionOwnershipContractTests`, `SessionTransportTests`, `SmartCardSharingTests`, `PcscConnectionLifetimeTests`, `PcscDiscoveryLifetimeTests`, `PcscReaderNameOwnershipTests`, `SmartCardConnectionFactoryTests`, `PcscContextIsolationTests` and `DesktopSmartCardDeviceListenerSCardErrorTests`. Run PublicApi HID compatibility tests separately in their real project. Require actual nonzero executed counts and preserve each isolated probe's exact method, child marker, exit and one-test oracle. Existing listener disposal tests invoke the real PC/SC service despite their UnitTests location; do not silently label that selection no-native.

For selected Pass-2 work, add meaningful deterministic vectors before fixes:

1. Count valid context releases on every enumeration exit, including list error/empty/throw and duplicate-warning failure; distinguish SafeHandle IsClosed from actual release callback.
2. Have a fake implement UNAWARE's immediate-return semantics and a held subsequent wait; observe call cadence while no reader changes. Hold the old listener past shutdown budget, then test explicit restart policy without reusing mutable context/thread ownership. Inject failures after each name/array allocation.
3. Inject slot Information logger failure **after successful open**, for both built-in and external factories. Hold cleanup, verify original claim stays held and assert exact cleanup order/outcome.
4. Hold native EndTransaction itself, independently from transmit; check scope invocation returns a pending task and repeated end/dispose share one result. Preserve current end-error assertions until the maintainer selects a reporting change.
5. Exercise discovery-first `a`/live-connection `b` member acquisition and cancellation rollback. A nonsecret AsyncLocal sentinel can test the selected worker context-flow policy if authorized.

Every held fake/native resource must have a release gate in finally and actual drain before fake event/handle disposal. A watchdog completing or killing a child bounds the test harness; it does not prove cooperative SDK teardown.

### Native/platform and device lanes

| Lane | Meaningful check | Qualification retained |
| --- | --- | --- |
| Portable PC/SC under each deployed OS/resource manager | Capture version and wrapper/shim identity; context-specific listener cancellation; ordinary-reader quiet-loop status cadence; transaction begin/end/disconnect/context return statuses | Original Part 5, Windows API and pcsc-lite differ. Pin observed native behavior, not a generic promise. |
| Native context/resource errors | Reader/service removal/restart in an isolated supervised host; record admitted call, cancellation request, native return, transaction state, drain and checked release separately | No new owner/claim release while native work remains pending. Recovery/reset warnings need explicit dispose/reopen; retain uncertain claims. |
| Existing read-only USB PC/SC lifetime integration | Async transaction plus serial read/reopen and sync sequential transaction/read loops on an explicitly identified authorized fixture | Current tests choose one descending-serial candidate, so record the actual key/firmware/OS and operation count rather than all-device coverage. |
| Existing FIDO ownership integration | INIT nonce echo, same-view refusal and reopen | No proof of hardware cross-interface grouping, cross-process exclusion, touch cancellation or applet state. B2/B3/B4 own their transport qualification. |
| Physical PIV/shared-handle state | Later approved two-process/same-card scenario with known PIN/key policy and observable cleanup | User-deferred. Include applet deselection, reset/power change, shared/exclusive/fallback and borrowed-session/connection lifetime. No execution requested now. |
| Historical serial 9681620 two-key discovery | Matching original attachment/PID/interface/read scenario and retained complete logs, when separately authorized | Current architecture and passing single-key/fake-PCSC tests do not explain the old native HID read. |
| Windows concurrency delta | Fresh source/evidence comparison only after user's ready handoff | No inspection or mutation of unfinished worktree/branches; current pin remains authoritative here. |

## Clean-unit sampling nominees

The coordinator retains the campaign's stratified 10%/minimum-three sampling requirement; these are concrete nominees, not a substitute for that denominator. Resolve exact original IDs/hashes from the ledger by path and label. Read their full methods/neighboring conditions when sampling.

| Nominee | Full-context reason to sample |
| --- | --- |
| `DisposalGate` type contract and Run/TearDownAsync | Shared completion, inline synchronous winner, policy-controlled failure release and callback error propagation; avoid equating failure completion with release. |
| `ISmartCardConnection.TransmitAndReceiveAsync` XML | Universal borrowed-input contract with explicit built-in cancellation/native-completion bound and raw/session separation. |
| `HidConnectionSlot.OpenRegisteredConnectionAsync` and transfer remarks | Exact opaque token forwarded before open, including pre-cancel; platform owners retain release responsibility. |
| `ISmartCardConnectionFactory` type contract | External factory must distinguish ordinary failure proof from explicit unrecovered partial acquisition; disposal exceptions remain separate. |
| `UsbSmartCardConnection.TransmitAndReceiveAsync` | Real pooled output lifetime, captured borrowed command, native result copy and cleared pool return under failure. |
| `SmartCardSharingTests.DefaultSharing_ExplicitOptInIsUsedByNewFactories` | Mutates process policy, constructs factory, restores strict policy before connect, then asserts snapshot-selected share calls and restores prior state in finally. |
| `PcscReaderNameOwnershipTests.FindAllAsync_Success_ReleasesEveryNameAndReturnsManagedCopies` | Alloc/free balance, pointers cleared, repeated cleanup, name/ATR copies and managed-context caveat. |
| `PcscContextIsolationTests.ListenerDisposal_CancelsOnlyMonitorContext_WhileConnectionTransmits` | Real production owner composition with distinct fake contexts, held native borrow and explicit post-release handle equality/counts. |
| `PcscDiscoveryLifetimeTests.BuiltInPcscDiscovery_BudgetExpiryQuarantinesThenLateReleaseAllowsConnection` | Outer timeout versus underlying work, temporary unrecovered state, late release/reopen; release gate in finally. |
| PublicApi HID compatibility adapter tests and `ExerciseReportsAsync` | Actual adapter objects with reflective construction only; copy/zero oracle and fake received bytes, no native claim. |
| Core README §PC/SC sharing policy and migration | Startup policy snapshot, direct-factory bypass, conservative re-discovery after contention and explicit physical qualification are already useful; review unchanged dependencies alongside C001/C007. |

## Handoff

B2/B3/B4 should consume the state vocabulary and keep exact root/handle/reference/claim owners through each native outcome. B5 should retain per-exchange versus multi-command boundaries and borrowed session recovery conditions. B6 receives the shared fake example correction dependency and retains APDU response/TLV/SCP ownership. A receives enumeration/listener/discovery coordination candidates without central-record edits. C receives the ambient/logging dependency and remains sole owner of shared pinned Python evidence.

Platform stages should bind the common PC/SC shim's uint/status, handle widths, reader-state layout and string encoding to the actual native package source/version. Linux libc fd int width and bool/integer marshalling belong to B2's applicable APIs; no pointer-width or one-byte Boolean rule is generalized from this portable seam. SCard.Interop currently decodes reader names as ASCII while connect uses UTF-8 through the shim; B4 should check non-ASCII reader identity preservation as a narrow native-wrapper question. macOS/Linux/Windows cancellation and successful unpower semantics retain their original-source/version qualifications.

B1 investigation records are ready for triage and independent review. Documentation changes, runtime fixes, consumer compilation, native/hardware acceptance and inherited physical closure remain separate future gates.

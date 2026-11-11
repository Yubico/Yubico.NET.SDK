# B1 local candidates and reopening map

Every ID here is local to this investigation. Astra assigns canonical IDs during triage. These groups contain overlapping documentation, runtime, policy and qualification observations; twelve groups are not a claim of twelve independent implementation bugs. No product file was edited and no SDK experiment was run.

Paths beginning `src/Core/src/` below are current shipping source. Test names refer to assertion reads at source pin `fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef`. `unit-dispositions.json` maps affected ranges to original unit IDs and hashes. All candidate-associated product paths remain unchanged in B1; the later editor/reviewer must disposition them explicitly.

## CORE-B1-C001 — qualify the PIV state rationale and power-off oracle

**Documentation:** candidate correction. **Runtime:** sharing policy already changed; no new runtime fix selected. **Policy:** retain strict Exclusive default and explicit coexistence opt-ins. **Qualification:** physical PIV/process/OS state remains open under `audit-v2-20261022-20261031:#22` and `piv-001:PIVCORE-001`.

`SmartCardSharing.cs:22–31` infers that any handle can use a PIN=ONCE key until reset from NIST SP 800-73-5 Part 2 §2.4.2. The original clause defines PIV PIN **application** security status and its loss on application deselection, separately from Global PIN. It does not specify PC/SC handle/process isolation or the YubiKey PIN=ONCE policy. `PcscNativeResources.cs:118` says power-off ensures card-wide state cannot outlive this connection. `SmartCardSharingTests.cs:276–288` says the card is powered off so state cannot outlive the session; its only final assertion checks the fake's supplied `UNPOWER_CARD` argument. A borrowed session ends before its connection does, so even the named lifetime is too broad.

Supported direction: explain that strict PC/SC exclusivity limits other resource-manager clients, shared modes accept shared application state, and built-in release **requests** power-off and checks return statuses. Explain PIV application status with its actual selection conditions; reserve device-wide claims for measured vendor/platform evidence. `DefaultSharing` is initially Exclusive, while `default(SmartCardSharing)` is the zero-valued fallback enum member; docs should continue naming the factory policy explicitly.

**Original evidence:** `sources/register.md` (NIST original-PDF extraction; direct PDF blocked), PC/SC Part 5 §3.2.5.2 and Windows disconnect parameter/status documentation. **Actual oracles:** mode arrays, `IsExclusive`, native call counts, factory snapshot tests and requested release disposition. They establish managed selection and arguments, rather than a real competing process or physical power-state change.

**Required unchanged paths:** `SmartCardSharing.cs`, `PcscNativeResources.cs`, `SmartCardConnectionFactory.cs`, `ISmartCardConnection.cs`, `SmartCardSharingTests.cs`, Core README §PC/SC sharing policy, Core CLAUDE §Connection Factory Pattern; PIV README/CLAUDE/API state explanations and examples remain the later PIV boundary's review. **Migration:** preserve explicit Shared/fallback startup opt-in and snapshot timing. **Reopen trigger:** a sharing default, disconnect disposition, card-state statement or measured isolation result changes. Obtain the actual cited NIST PDF independently before approving the normative rewrite.

## CORE-B1-C002 — enumeration context cleanup starts too late

**Documentation:** candidate contract addition/qualification. **Runtime:** source-confirmed missing deterministic cleanup paths; Pass 2. **Policy:** deterministic enumeration ownership is the expected resource-management direction, rather than an authorized fix. **Qualification:** no native leak measurement.

`FindPcscDevices.FindAll`, lines 98–114, calls `SCardListReaders`, returns for non-success/empty results, deduplicates names and can log a warning before entering the `try/finally` at line 115. A successfully established context on those exits is left to SafeHandle finalization. A throwing list call, allocation or logger also bypasses the finalizer block. The ordinary no-readers path can repeat in every scan. In contrast, the recognized-USB return at line 134 occurs **inside** the finally and disposes its context.

The fix for `audit-v2-20261022-20261031:#18` owns reader-name allocation after line 136 and remains correct for that exact scope. This separate context gap must not undo that scoped resolution or silently broaden the old ID.

`FindPcscDevicesTests` often supplies an invalid default `SCardContext`; `PcscReaderNameOwnershipTests` checks `Context.IsClosed` only on paths that enter the finally. .NET 10 SafeHandle's managed closed state also does not prove a native ReleaseHandle succeeded. Public `IFindPcscDevices.FindAllAsync` currently supplies no useful XML describing its one-shot results, generic/NFC status probing or cancellation boundary: cancellation is checked before worker admission; once launched it awaits the worker without detaching. Higher-level finder budgets bound a different wait.

**Required unchanged paths:** `FindPcscDevices.cs`, `ISCardApi.cs`, `SCardContext.cs`, `SCard.Interop.cs`, `FindPcscDevicesTests.cs`, `PcscReaderNameOwnershipTests.cs`; Core discovery/ownership guides and module instructions where enumeration completion is explained. A owns discovery narrative integration; B4 owns native wrappers. **Reopen trigger:** move context ownership to cover every post-establishment exit, change unavailable-service/list-error policy, or change caller-wait behavior. Add counting-context fakes for non-success, empty and throwing ListReaders, warning failure and successful integrated-reader paths before physical leak qualification.

## CORE-B1-C003 — listener success path can repeatedly request immediate status

**Documentation:** candidate correction of timing rationale. **Runtime:** source/API-supported busy-loop candidate; no execution. **Policy:** maintain bounded failure cadence and event-hint role. **Qualification:** exact OS/native-shim behavior and call cadence need a bounded native lane.

`DesktopSmartCardDeviceListener.cs:318–332` creates new states for every wait and never preserves `EventState` as a later `CurrentState`. `SCARD_READER_STATE.Create` sets CurrentState to zero. PC/SC Part 5 §3.2.4 and pcsc-lite's original API define all-zero UNAWARE as an immediate status response. On successful reader listing/status calls the loop reaches its next iteration without a sleep. `WaitForPnpChange` also recreates zero PnP state; its concrete behavior requires OS qualification. The one-second parameter is a maximum wait and supplies no minimum delay for the ordinary-reader success path.

`DesktopSmartCardDeviceListenerSCardErrorTests` pins a single backoff before retry/reestablishment on **failure**; it supplies no quiet-success cadence oracle. The integration quiet-period test counts callbacks over 500 ms, rather than native call count or CPU consumption.

**Required unchanged paths:** listener source, `SCardReaderStates.cs`, `SCardState.cs`, `SCard.Interop.cs`, listener error/disposal/integration tests; Core listener/concurrency guidance and discovery guide timing statements. **Reopen trigger:** persistent state or explicit cadence enforcement changes; verify stable reader/PnP conditions independently on Windows, Linux and macOS. A consumes the listener-hint implications; B4 rechecks native layout/shim behavior. Avoid a portable CPU-rate guarantee based on fakes alone.

## CORE-B1-C004 — listener partial lifetimes and finalizer commentary need a precise bound

**Documentation:** candidate correction. **Runtime:** partial allocation and timeout/restart ownership candidates, Pass 2. **Policy:** maintain deterministic owner shutdown and avoid freeing active native resources. **Qualification:** no finalizer or held-native experiment executed.

- `WaitForPnpChange` allocates its name before a native call and frees it afterward without finally (`281–307`). A managed/native seam exception loses that allocation.
- `WaitForStatusChange` allocates ordinary names, then the combined array and PnP name **before** its finally (`318–326`). A later allocation failure leaves earlier owned names outside cleanup. The corrected `CreateMany` helper handles failure **within** its own allocation loop; it cannot protect subsequent caller allocations.
- `Start` records a context and catches startup/baseline failures without disposing partial state (`80–109`). Retry after Error can replace that context. `StopListening` clears `_listenerThread` before joining; on its eight-second timeout it clears `_context` and returns. `Start` can subsequently reset `_shouldStop` and install a new thread/context while the old thread is alive; the old loop reads shared mutable fields. Thus “bounded leak” describes one stop attempt, not a process-wide fixed resource bound or safe restart generation.
- The finalizer directly disposes `_context`; its XML says it ensures cleanup. A running ThreadStart retains the listener, so ordinary abandonment need not make it finalizable. `Finalizer_DoesNotThrowException` catches creation/start exceptions and only asserts that the GC calls returned without a caught exception. It neither observes finalizer entry nor proves context release/thread exit. A real exception on the finalizer thread would not be an ordinary exception caught around `GC.Collect`.

`PcscContextIsolationTests` is a useful fake oracle: cancel/release handles equal the monitor context, the held connection context remains distinct and unreleased, and transmit completes after the test opens its gate. That establishes call routing under the fake's cancel behavior. It does not establish a native timeout, all shutdown races or finalizer safety.

**Required unchanged paths:** listener source, listener disposal/error/integration tests, `PcscContextIsolationTests.cs`, native context/reader helpers, Core instructions and listener-facing API remarks. **Migration:** direct callers should set callbacks before Start, dispose explicitly and avoid lifecycle calls inside callbacks; monitor consumers keep the monitor as owner. **Reopen trigger:** atomic shutdown/restart-generation policy, actual late thread drain, partial-start cleanup or allocation ownership changes. Proposed tests must own a release gate in finally and observe actual old-thread termination before disposing fake events; the current tests must not be used as broad finalizer acceptance.

## CORE-B1-C005 — preserve the registered HID failure-release distinction

**Documentation:** candidate correction. **Runtime:** existing compatibility behavior, decision deferred. **Policy:** B4/maintainer must decide any future tightening for Windows/custom HID. **Qualification:** external/native behavior remains implementation-specific.

`RegisteredConnections.cs:21–26` promises teardown followed by lease release and finished teardown for every disposal call. SmartCard wrappers use `releaseLeaseOnFailure: false` plus quarantine; FIDO/OTP wrappers use the default `true` and release even if inner disposal throws. `DeviceConnectionRegistryTests.RegisteredFidoHidConnection_ExternalDisposeFailure_KeepsPriorLeaseBehavior` explicitly asserts `ReleaseCount == 1` after the exception. Built-in macOS/Linux slots bypass these wrappers and give the token to their native owner.

Supported direction: describe shared disposal **outcome**, preserve successful-teardown ordering, and name the failure-release policy for each route. A faulted disposal task is not generic native release proof. `DisposalGate` itself documents its configurable policy correctly; it is a useful unchanged dependency.

**Required unchanged paths:** registered wrappers, `DisposalGate.cs`, `DeviceConnectionRegistryTests.cs`, `HidConnectionSlot.cs`, `YubiKeyDevice.cs`, ownership/raw-tier guides and platform handoffs. **Reopen trigger:** Windows/custom HID gains checked release or wrapper policy changes. Shared editor should qualify `connection-ownership-and-contention.md:109–113`; B4 owns any native conclusion. No broad native HID audit was performed here.

## CORE-B1-C006 — transaction end completion and error reporting are distinct

**Documentation:** candidate contract clarification. **Runtime:** observed behavior; product decision if failure should be thrown earlier. **Policy:** scope error reporting needs maintainer adjudication. **Qualification:** native transaction/reset behavior remains OS-specific.

`PcscConnectionNativeState.EndTransaction` captures exceptions or status failure, sets EndFailed and closes the connection, but completes `EndCompletion` with a uint. `TransactionScope.Dispose/DisposeAsync` calls `reportEndFailure` on non-success. `UsbSmartCardConnection` supplies a Debug logger callback. Thus built-in scope disposal can return normally after native end **failed**, while the later connection disposal reports the stored error and checked resource cleanup still releases the claim if proven.

`AsyncTransaction_FailedAsyncEnd_RefusesFurtherWorkAndShutdownReportsFailure` awaits scope disposal without expecting a throw, then expects ObjectDisposedException on transmit and SCardException on connection shutdown. `EndFailure_StillReleasesNativeResourcesAndPhysicalClaim` asserts end/disconnect/context counts and successful reopen after the shutdown error. These are coherent, specific oracles. The title `AsyncScopeEndAwaitsNativeEnd...` actually holds a transmit before end; its fake end returns immediately, so that assertion does not independently hold native EndTransaction itself.

`ISmartCardConnection.cs:45–68` should explain the built-in outcome and asynchronous runtime cast pattern, while keeping custom default synchronous dispatch explicit. A transaction does not exclude other callers using the **same connection** between raw calls. Caller workflow serialization remains necessary. Windows' documented idle transaction reset and current SCARD_W_RESET_CARD semantics are platform conditions, not new portable SDK promises.

**Required unchanged paths:** interface, native owner, Usb facade, lifetime tests, Core README transaction example, raw-tier/ownership guides. **Migration:** retain IDisposable signature and conditional IAsyncDisposable disposal; any API/error policy change requires separate implementation authorization. **Reopen trigger:** scope disposal starts throwing native errors, transaction error state/recovery changes, or a new bounded held-end oracle is added.

## CORE-B1-C007 — reconcile active shared ownership claims with current routes

**Documentation:** candidate correction. **Runtime:** no new fix proposed here. **Policy:** keep known-member/process bounds and per-exchange exclusion. **Qualification:** later platform/session/wire packets must confirm their own tests.

`connection-ownership-and-contention.md:9–10` still suggests native PC/SC sharing violations ordinarily surface as SCardException. The built-in strict Exclusive route maps exactly that status to ConnectionInUseException with an inner SCardException; explicit Shared failures/custom providers can differ. `195–199` describes active disposal wake as macOS-only and says other built-ins receive no wake. Current `FidoHidProtocol` invokes `ITerminalWakeControl` and Linux FIDO implements that seam too; the raw-tier guide already says macOS/Linux. Reconcile this with B2/B5, rather than auditing all platform implementations from B1.

`Known bounds` says hotplug failure must be bounded and must not strand the lease. Current safety deliberately retains uncertain claims and permits indefinitely pending native work. State which managed waits are bounded, name unresolved native ownership and explain recovery rather than implying universal bounded teardown. `DeviceConnectionRegistry` introductory disposal wording and registered-wrapper wording need the same success/failure distinction.

Keep historical async plans historical: the old addendum's `DisposalGate` failure-release statement describes its then-current source. It must not be silently rewritten as an active contract. Test comments in `ApplicationSessionScpTests` and `ConnectionOwnershipContractTests` contain upstream/phase/Rust/Python comparisons; retain the local rationale but replace unsupported current-version authority with direct behavior. C's pinned Python register can settle a retained comparison later; no duplicate checkout was created.

**Required unchanged paths:** active ownership guide, raw-tier guide, Core README/CLAUDE, registry/wrappers, `ApplicationSession.cs` lifetime remarks (B5), the two named tests; historical plan sections retain provenance. Diagram SVG/PNG currently represent the happy ownership route consistently; treat them as candidate-associated unchanged simplifications, not complete error-state models. **Reopen trigger:** mapping, wake routing, claim quarantine or workflow guard boundaries change. The shared editor owns all shared-file edits.

## CORE-B1-C008 — make public callback ownership and CLI selection commentary actionable

**Documentation:** candidate enhancement/correction. **Runtime:** source behavior retained. **Policy:** caller ownership on success is required for the generic helper. **Qualification:** consumer-shape compilation remains assigned validation work.

`YubiKeyConnectionExtensions.CreateSessionOverTransportAsync<TResult>` disposes its created connection when the delegate fails. On success it returns TResult unchanged and never disposes the connection; built-in callers explicitly call internal `OwnConnection`. Public external delegates cannot call that internal method. XML should require the delegate's successful result to own/expose/dispose the connection and provide a complete ownership-bearing result example; simply constructing a borrowed session would leak the hidden connection. `SessionTransportTests...Succeeds_LeavesConnectionOwnedByResult` returns the connection itself and checks it was left open, rather than asserting an arbitrary result automatically owns it.

The assigned `YkCommandBase.AppletTransports` XML says the first type is auto-selected. Actual DeviceSelector uses the first type to **prefer a device** in noninteractive selection, while YkCommandBase forwards only the explicit requested transport into PreferredConnection. Explain device eligibility/preference separately from subsequent applet transport choice. The transport arrays match Core's documented defaults for the selected getter scope; whole applet command behavior belongs to later boundary packets.

**Required unchanged paths:** connection extensions, SessionTransportTests, CLI base, DeviceSelector and its tests (A), relevant raw-session/owned-result examples, Core ownership guide. **Reopen trigger:** public ownership-transfer capability or helper success/failure semantics change. The final report supplies a complete callback/result pattern for later compilation; it is not a product edit.

## CORE-B1-C009 — post-open slot logging can escape cleanup ownership

**Documentation:** candidate qualification of failure cleanup promises. **Runtime:** source-backed exceptional-path candidate, Pass 2. **Policy:** logger failure must preserve native ownership until actual release. **Qualification:** no injected log experiment executed.

`PcscConnectionSlot.OpenRegisteredConnectionAsync:68–74` awaits an already initialized registered connection, then logs Information before returning it. A throwing logger exits without disposing that returned local. YubiKeyDevice has already transferred registration ownership and deliberately avoids releasing it. Best-effort facade finalization is the remaining possible recovery, with no bound. In the **external factory** branch (`77–95`), an Information logger exception after successful open reaches the catch that releases registration without disposing the raw connection; that can permit reopen while the external resource is still owned. `OpenRawConnectionAsync:52–56` has the analogous undisposed local after logging failure.

`SmartCardConnectionFactoryTests` covers logger **creation before rooting** and Debug log failures inside `UsbSmartCardConnection.InitializeAsync`, whose catch drains native cleanup. Those assertions do not cover the slot's post-open Information logger. `SessionTransportTests` proves a cleanup-logger failure preserves an original factory failure, which is a separate call site.

**Required unchanged paths:** PC/SC slot, native factory/facade, YubiKeyDevice ownership catch, logging helpers (C), factory/lifetime/transport tests and factory ownership XML. **Reopen trigger:** post-open logger handling/cleanup changes; test both registered built-in and external-factory routes with a valid fake opened handle, assert cleanup order and no early claim release, and retain cleanup failures separately from the original logger exception.

## CORE-B1-C010 — PC/SC worker ambient context retention needs a scoped decision

**Documentation:** record only until intent is settled. **Runtime:** source-backed retention mechanism; no secret-retention finding claimed. **Policy:** ask whether PC/SC owner workers should suppress caller ExecutionContext as the newer HID owners do. **Qualification:** no runtime probe.

`PcscConnectionNativeState.EnsureWorker:228–239` uses normal Thread.Start; .NET 10 v10.0.0 Thread source captures ExecutionContext. A long-lived/rooted native owner can therefore keep ambient AsyncLocal state reachable independently of the caller's awaited method lifetime. This is separate from the safe lifetime of the explicitly borrowed command buffer. Existing worker tests assert one managed thread ID and native-call ordering; they do not assert ambient context isolation.

**Required unchanged paths:** native owner, Usb facade, lifetime tests, context-aware logging/presence ownership commentary and B2/B3 owner comparisons. **Reopen trigger:** approved suppression/isolation policy or a documented reason to preserve flow. A bounded nonsecret AsyncLocal sentinel experiment may later distinguish captured state from caller input; retain platform and full API context and avoid recording actual secrets.

## CORE-B1-C011 — fake connection examples use absent method/property names

**Documentation:** directly supported correction. **Runtime:** no change. **Policy:** use the actual narrow fake rather than inventing a second abstraction. **Qualification:** source binding checked by inspection; current compilation remains unrun.

Core CLAUDE §Faking Connections and Core tests CLAUDE §FakeSmartCardConnection call `QueueResponse` and inspect `SentCommands`. The actual fake declares `EnqueueResponse` and `TransmittedCommands` (`Protocols/SmartCard/Apdu/Fakes/FakeSmartCardConnection.cs:31–34`). Its transmission method copies commands and throws if no response is queued. Update both complete examples with current names, actual imports/internal test context and explicit connection/protocol disposal.

**Required unchanged paths:** both assigned shared sections, the actual fake (B6), `PcscProtocol.cs` (B6) and relevant test project context. The fake's DisposeAsync is currently a no-op with a genuine TODO; preserve it for B6's disposition, rather than claiming async disposal-count evidence from that fake. **Reopen trigger:** fake API or ownership behavior changes. Shared editor integrates duplicate examples once and the coordinator compiles them in the actual test context.

## CORE-B1-C012 — whole-scope immediate refusal has a discovery-wait precedence bound

**Documentation:** candidate precision improvement. **Runtime:** current sequential acquisition, decision if stronger immediate refusal is required. **Policy:** preserve sorted acquisition/rollback and waiting-connection priority. **Qualification:** source trace only.

`DeviceConnectionRegistry.AcquireConnectionAsync:110–114` acquires members sequentially. Concrete trace: discovery holds `a`; a live connection holds `b`; a new connection asks for `[a,b]`. It waits for discovery on `a` before inspecting the already-held `b`. Consequently the remarks saying any known-member conflict is refused **immediately** need this local condition, and “atomic” should mean a logical registration with rollback rather than an indivisible dictionary-wide transaction.

Existing `AcquireConnection_LaterMemberHeld_RollsBackEarlierClaims` has a free first member, and `AcquireConnection_DiscoveryOnMember_CancellationRollsBack...` has a held **later discovery** member. Neither asserts the discovery-first/live-later trace. The implementation correctly sorts/deduplicates IDs and prevents double successful overlapping ownership; this candidate does not allege two live winners or deadlock.

**Required unchanged paths:** registry XML, `DeviceConnectionRegistryTests.cs`, ownership architecture §One physical device and §Discovery coordination, Core concurrency guide, IYubiKey.ConnectAsync XML (A). **Reopen trigger:** full-scope preflight/admission policy or wait precedence changes. A deterministic test should hold a discovery `a` and connection `b`, observe the wait, cancel it and prove no native open and complete rollback; any stricter refusal requirement is a separate runtime decision.

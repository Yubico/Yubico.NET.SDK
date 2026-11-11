# Bounded Linux managed acceptance — L6a

Recorded 2026-11-11. Scope: managed public journeys, finite registry links, and portable PC/SC
release evidence over accepted L0–L5. **No shipping source, public API, package, native host, device,
system configuration, master ISA or orchestrator status changes.** This is not native Linux/AOT/hardware
acceptance or global closure. Context7 `/dotnet/docs` confirmed wait-only `Task.WaitAsync` semantics
and asynchronous TCS continuations; no native-cancellation wrapper was introduced.

## Independent gates

* `AdapterContractRegistry` / `adapter-contracts.v1.json`: **43 required operation rows / 76 runnable
  method links**, up from 27/49. The **16 new Linux rows / 27 links** cover typed FIDO/OTP
  open/send/receive/dispose (8), and direct synchronous IO/feature open/get/set/dispose (8).
  Production owner and declared symbol resolution is separate from required role/test resolution.
  Linux exclusion is replaced only for these rows; Windows HID remains deferred/rejected.
* Existing macOS, listener and portable PC/SC requirements are preserved. Missing Linux operation,
  missing/empty role, wrong role, stale owner/test and duplicate-profile checks fail closed. Sixteen
  explicit Linux row-deletion cases test the independently required list, including every profile role.
* `PublicSyncBoundaryRegistry`: **24 required public reachability entries**, up from 16; eight new
  retained Linux synchronous paths map public interfaces independently to Linux implementation owners.
  Eight explicit deletion cases prevent silent shrinkage. These rows link reachability and existing
  behavioral evidence; they do not execute profiles recursively or attest native release.
* No shipping boundary site changed. The reviewed source manifest remains **219 sites / 132 imports /
  37 waits / 18 scheduling sites**, documented/outstanding. No source-site status was promoted.

`LinuxDirectFacadeAcceptanceTests` adds 6 concrete behaviors: partial IO-open metadata failure, feature
open failure, held IO write, held feature SET, IO true-quiescence disposal, and held feature close with
shared disposal. Existing direct-facade tests supply descriptor-sized IO and fixed-eight-byte feature
GET/metadata evidence. Direct SET preserves expert lengths. These direct opens still take no grouped claim.

## Public applet journeys

Both new suites construct actual `LinuxHidInterface` → `HidConnectionSlot` → `YubiKeyDevice` routes;
native payload/readiness fakes sit **below the production Linux owners**, not at `IFidoHidConnection`
or `IOtpHidConnection`. There is no new framework or production scheduling seam.

`Ctap2SessionLinuxCrossLayerTests` (**3 cases**):

* Unwrapped invocation returns a pending creation task while native INIT output is held. A held
  GetInfo response also keeps the public operation pending. Two borrowed sessions perform real framed
  INIT/GetInfo sequentially without closing the connection; only connection disposal closes it.
* Public MakeCredential sends the complete multi-report request before duplicate touch keepalives.
  The prompt cancels; exactly one CANCEL follows the full request. The terminal response continuation
  is withheld: the operation and terminal presence remain pending and overlap/physical reopen fail.
  After drain, one Cancelled resolution references the original request, then correlated-channel
  GetInfo succeeds. Exact command sequence: `86 90 90 00 91 90`.
* An extension-owned session disposed during pending GetInfo terminalizes the public operation,
  while disposal and the physical claim wait for true reactor quiescence. Reopen succeeds only after
  close. Terminal wake can yield ODE on an admitted reader or IOException on the next admission;
  both are explicit supported test outcomes, not a native-release signal.

`YubiOtpSessionLinuxCrossLayerTests` (**4 cases**):

* Held first feature GET leaves public creation pending; status gives firmware 5.4.3 and fake slot
  flags, serial 12345678 decodes through the public API, and two borrowed sessions retain the connection.
* Public HMAC challenge-response cancellation begins only on the fake device's touch-wait flag.
  The abort SET is held; overlap is refused and terminal presence remains unresolved until it returns.
  One Cancelled resolution precedes successful public read-only serial recovery. Ten frame reports
  plus one abort/completion reset per exchange are checked, including command/sequence/CRC bytes.
* Successful and corrupted synthetic HMAC response CRCs resolve once as Completed and Failed,
  respectively, proving applet CRC validation—not the transport—owns terminal outcome.
* Fixed fake response vectors use independent complemented CRCs: serial `00 BC 61 4E C8 04`,
  synthetic HMAC bytes `00..13` followed by `15 43`. No real key, slot provisioning or device operation.
  Captured synthetic challenge reports are cleared at fake disposal.

`OtpHidProtocol.Configure` is deliberately unchanged. It retains a classified synchronous internal
initialization wait. Public YubiOTP (and source-reviewed Management OTP) awaits ReadStatus first, so
Configure sees initialized state. The public YubiOTP queue rejects extra hidden Configure I/O. This is
not proof that an arbitrary uninitialized internal Configure call is nonblocking.

## Portable PC/SC gap

`ControlledSCardConnectionApi` can now hold Disconnect and ReleaseContext independently.
`PcscConnectionLifetimeTests.LiveRegisteredConnection_*` (**3 cases**) uses real public registered
connections, not discovery-failure-only surrogates. Disposal and competing public opens stay pending/
refused at each held release stage. Concurrent and repeated disposal share the same outcome, call each
native release at most once, and successful reopen waits for both returns on the original worker.

Isolated `PcscIsolatedProbe` children check failed live context release and failed live disconnect:
shared original failure, no release retry, surviving weak native-handle references after dropping the
test's strong facade reference/GC, and continued `UnrecoveredConnectionException`. A disconnect failure
correctly stops before context release and retains both resources; context failure follows a proven
card disconnect. Facade collection itself is not promised/claimed. **No PC/SC production bug reproduced;
no production PC/SC change.** These are portable managed tests, not libpcsclite/driver acceptance.

## Verification

Release, toolchain wrappers only. Initial L6a runs below: exit0, RESULT PASSED, warnings0, no skipped cases.
The subsequent quality correction and its fresh verification are recorded separately below.
Each run ID names `artifacts/toolchain/runs/<id>/` containing `run.log`, `result.json`, and step logs.
Available outputs are archived at `/tmp/opencode/linux-async-l6a-evidence/<id>/` because toolchain
retains only five runs. These temporary logs are not committed.

| Scope | Run ID | Executed / failed / skipped |
|---|---|---|
| Full solution `dotnet toolchain.cs build` | `20261111-040629-530-build` | 0 warnings / errors |
| Core selected boundary/lifetime/routing regressions | `20261111-040632-539-test` | 872 / 0 / 0 |
| Core registry/direct-facade/live-PCSC focus | `20261111-040518-877-test` | 55 / 0 / 0 |
| Full Ctap2 unit suite | `20261111-040239-796-test` | 495 / 0 / 0 |
| Full YubiOtp unit suite | `20261111-040245-370-test` | 205 / 0 / 0 |
| Full Management unit suite | `20261111-040701-320-test` | 96 / 0 / 0 |
| Full WebAuthn unit suite | `20261111-040310-604-test` | 371 / 0 / 0 |
| Full PublicApi conventions | `20261111-040315-684-test` | 25 / 0 / 0 |
| `dotnet toolchain.cs complexity` | `20261111-040703-450-complexity` | 172 changed shipping methods within thresholds (accepted L1–L5; none added by L6a) |

The six initial nonoverlapping regression selections total **2064 cases**, including **40 new cases**
(24 registry deletion cases, 6 direct facades, 3 PC/SC, 3 CTAP2, 4 YubiOTP). The focused 55 reruns overlap.
Commands for full modules are `dotnet toolchain.cs -- test --project <module>`. Exact Core selection:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxHidRoutingTests|FullyQualifiedName~MacOS|FullyQualifiedName~Pcsc|FullyQualifiedName~DeviceConnection|FullyQualifiedName~ConnectionOwnership|FullyQualifiedName~Discovery|FullyQualifiedName~FindYubiKeys|FullyQualifiedName~CompositeDevice|FullyQualifiedName~YubiKeyDevice|FullyQualifiedName~HidConnectionSlot|FullyQualifiedName~RawCtap|FullyQualifiedName~RawOtp|FullyQualifiedName~BoundaryInventory|FullyQualifiedName~Linux"
```

Registry RED `20261111-035412-806-test`: 46 executed/26 failed/0 skipped, warnings0, missing Linux rows
and old count expectations. Initial GREEN `20261111-035522-956-test`: 52/0/0, later strengthened to 76
links and three live PC/SC tests. Early raw logs aged out before archiving; observed counts are recorded,
not claimed as archived files. Intermediate failures corrected test imports, terminal-wake race
expectations, the intentional PC/SC stop-on-disconnect-failure path, and an unsupported facade-GC
assertion. There was no shipping fix. One early pair of test invocations overlapped and emitted the
toolchain collision advisory; final runs were serialized.

Scoped formatting and `--verify-no-changes` pass (exit0) on all 9 edited/new C# test files.
The existing duplicate-PublicAPI workspace-load
warning remains a formatting-tool warning, not a compiler/test warning. `git diff --check` passes.
`codemapper .` remains unavailable (exit127). No runtime loop changed, so resilience-fast was not rerun;
L5's 184 passing cases are historical accepted evidence, not a new L6a run. No integration/hardware tests
were executed; full solution compilation includes integration projects.

## Quality follow-up: witnessed synchronous disposal

Review identified a false-green possibility in the two direct-facade disposal tests: an incomplete
`Task.Factory.StartNew(connection.Dispose, ...)` did not prove the synchronous caller had started.
The existing async withheld-native probe cannot establish entry into a synchronous disposal wait.

`LinuxDirectFacadeAcceptanceTests` now uses a small nested test-only `HeldSynchronousDisposeProbe`:

* After the real async caller reaches held native quiescence/close, a dedicated thread invokes the
  synchronous Dispose. Volatile invoking/returned markers bracket only that invocation. There are
  no probe waits, signals, context dispatch or task scheduling inside that interval.
* The observer must witness that thread in `WaitSleepJoin` with invocation active and return absent,
  then observe another 200ms of nonreturn while the native gate remains held. The 5s watchdog bounds
  failure observation; neither duration is a performance or disposal-latency claim. Thread state is
  diagnostic evidence, not the synchronization mechanism: the existing native gate controls release.
  Context7 `/dotnet/docs` explicitly distinguishes a thread-start request from actual execution and
  cautions against using ThreadState itself as synchronization.
* `finally` always releases the native gate and drains both async and synchronous callers, using
  finite uncancelled cleanup waits and joining the dedicated caller. Existing one-close and IO
  reference-release assertions remain. No production instrumentation or shipping change was added.
* Two negative-control cases exercise these same held-native journeys with a deliberately broken
  immediate-return synchronous Dispose surrogate. The witness rejects return, not just a timeout.
  Before adding the expected-exception assertion, the control was actually run RED: both cases
  failed with `Synchronous Dispose returned before a blocking wait was witnessed while native cleanup
  was held.` The final tests require that specific rejection; this is not merely an unverified
  negative assertion that happened to pass.

Fresh results (toolchain Release, warnings0, no skipped cases):

| Scope | Run ID | Result |
|---|---|---|
| Unwrapped immediate-return controls (deliberate RED, exit1) | `20261111-041340-334-test` | 2 executed / 2 failed / 0 skipped; early-return witness failures |
| Direct facade suite, including wrapped controls | `20261111-041403-459-test` | 8 executed / 0 failed / 0 skipped |
| Final registry/direct-facade/live-PCSC selection (includes added nonreturn observation) | `20261111-041504-177-test` | 57 executed / 0 failed / 0 skipped |
| Full solution build | `20261111-041509-505-build` | 0 warnings / errors |

Exact final test filter is
`FullyQualifiedName~AdapterContractRegistryTests|FullyQualifiedName~PublicSyncBoundaryRegistryTests|FullyQualifiedName~LinuxDirectFacadeAcceptanceTests|FullyQualifiedName~LiveRegisteredConnection`
with `dotnet toolchain.cs -- test --project Core --filter ...`; the RED filter is
`FullyQualifiedName~SynchronousDisposeWitness_ImmediateReturnControlIsRejected` before wrapping its
expected failure. Logs including raw RED output are archived under
`/tmp/opencode/linux-async-l6a-quality-evidence/<run-id>/` (`run.log`, `result.json`, step logs).
The suite now has 8 direct-facade cases, and L6a has 42 new cases including the two new controls.
Initial broad regression counts above remain historical; the broad suites were not rerun for this
test-only correction. Registry row/link counts and shipping source inventory are unchanged.
Only the direct-facade test file and this evidence document changed in the quality follow-up.
Scoped formatting/verification and `git diff --check` pass; formatting retains the existing workspace-load warning.

## L6b handoff / limits

No native/AOT/hardwarehost implementation or execution was attempted. L6b retains selected-key/native
ABI/AOT/hardware evidence, measurement/verification-host work and any native support-contract updates.
Use the existing L1 report seam, L2 activation/quiescence split, L3/L4 checked owners and L5 claim-transfer
route unchanged; L6a introduces no shipping interface for the next context. Managed fakes do not prove
kernel/hidraw behavior, permissions, musl support, hardware cancellation/removal/reopen, starvation,
performance/resource budgets, or whole-Linux readiness. Hung native work/broken wake can remain pending
indefinitely. Windows/custom HID and custom SmartCard limits remain documented; neither registry is a
whole-SDK call graph. No subagents, commits, pushes, device operations, or system changes were performed.

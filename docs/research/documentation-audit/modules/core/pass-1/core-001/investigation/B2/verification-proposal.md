# Proposed validation lanes — prepare only

No command below was executed by B2. The coordinator assigns and serializes compiler/test/device work.
Use the repository build/test skills and existing validation foundation; no new generic host is needed.
Route TMPDIR/TMP/TEMP and explicit output to `/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/B2`.
The literal `/tmp/opencode` preferences in existing Linux ABI/Python tests need a separately owned
home-backed adaptation before those particular selections under the current quota constraint.
Preserve original tests and frozen evidence; a path override is not evidence that they already ran.

## Linux current-release matrix

| Lane | Meaningful observation / exact existing entry | Missing gate or prerequisite |
| --- | --- | --- |
| Source preservation | Compare every affected source body/directive, API and native signature with pin; compiler input identity from coordinator foundation | All B2 product bodies stay unchanged. Current checked compiler execution is coordinator work. |
| API examples | Compile complete typed/raw owned scopes and successful-result buffer cleanup in actual net10 referenced context, with full imports and cancellation | VALID-NOT-CHECKED until actual compiler result; no compile result infers hardware behavior. |
| ABI and close | Core UnitTests LinuxNativeAbiTests, LinuxDescriptorCloseTests, LinuxHidReportApiTests; retain exact reflection signatures, actual libc counts/errno, isolated fd0 and no double-close | Only Linux cases actually call libc. Synthetic close-error rows establish classification, not induced kernel EINTR/EIO. Record runtime/kernel/libc/architecture. |
| FIDO resource safety | LinuxFidoHidConnectionTests, LinuxFidoWorkerFailureTests, LinuxHidReadReactorTests (both partial files) | Record activation, callback start/end, reader terminality, actual quiescence, reference release and checked close individually. Native pending may outlive wait cancellation. |
| OTP resource safety | LinuxOtpHidConnectionTests, LinuxOtpWorkerFailureTests, LinuxOtpRecoveryTests | Hold actual fake GET/SET independently from close. Distinguish cancellation dispatch, native return, cleared staging, reset acknowledgment and caller-owned output. B5 validates full wire semantics. |
| Public routing/discovery | LinuxHidRoutingTests and LinuxDiscoveryAdmissionTests, with actual slots/registry/owners and fake native seams | Record known member set, selected route, PID admission transfer, temporary versus retained quarantine, late exact-token release and reopen. A owns cache/epoch policy vectors. |
| Synchronous expert facades | LinuxDirectFacadeAcceptanceTests and LinuxReportConnectionTests | Witness the synchronous caller inside its actual blocked invocation; immediate-return negative must be rejected. Finally drains both callers before fake event disposal. No UI latency guarantee. |
| Listener mechanics | LinuxHidDeviceListenerFaultInjectionTests and LinuxUdevHidEventSourceTests | Fake policy cadence/error exit differs from real libudev wake/stop. Add an isolated held-past-join session probe only if separately authorized. Preserve listener callback lifecycle constraints. |
| Real native host JIT/AOT | Existing standalone LinuxHidRouteVerification --native and two exact sensitivity controls; same source/effective inputs and package/artifact binding | Actual current publish plus execution is required. 38 exact native names/runtime and expected negative exit/prefix/cleanup; watchdog is failure. Do not silently use retained old output as new execution. |
| Verifier contract tests | Existing test_evidence_gates.py against an explicitly selected retained/current capture | 31 recorded integration tests include real MSBuild contexts in some Python cases. Need assigned compiler lane and home-backed temp handling; no generic “Python-only/no compiler” label. |
| Packaging C004 | Isolated clean Ubuntu noble runtime-package environment, record `.so.1` versus `.so` presence and resolution | Static package evidence is strong, fresh load unexecuted. No library installation/configuration on user's host. B4 coordinates stable-soname/loading policy. |
| Permission C001 | Inspect distribution-provided rules and resulting permissions for selected fixture, choose desktop ACL versus service group | No world-writable fallback or system rule change without its separately scoped authorization. Read-only inspection/permission setup is distinct from report exchange. |
| Selected-key read-only physical | Existing --list then --fido/--otp/--raw/--expert-io/--expert-feature/--smartcard, positive matching serial in JIT and AOT | Record exact authorized serial/configuration, interfaces, raw/effective firmware and source/input manifest. Direct tier requires unique interface association. Compare to upstream serial 103 only within that historical scope. |
| Attended Selection/cancel/removal | Existing separately gated OperatorScenarios; capture request, native pending, cancel request, terminal result/presence, drain/close, observed absence/replug and reopen separately | Human coordination required; no execution now. A canceled wait or killed child supplies no native drain proof. |
| Old two-key blocker | Matching serial 9681620/key 125 attachment/PID/interface/identity-read trace with old evidence retained | A/OATH retain original CORE-001 identity and missing cause. New serial 103 or single-key passes do not resolve it. |
| Broad applet qualification | CTAP2/WebAuthn real UP/UV ceremonies and relevant applet transport/readiness/recovery lanes | Future boundary-owned plan. Current GetInfo is read-only transport smoke; PIV physical work stays user-deferred. |
| Performance/resources | Representative input/output/ioctl/open/close timing, invocation/terminal/drain/release phases, idle CPU/allocation and worker telemetry | No approved budgets or comparable pre-change physical baseline. Whole-process allocations/call counts and held seam time need their original labels. |

Use full class/project selectors rather than a blanket `~Linux` as the sole evidence of what ran. Require
actual nonzero executions and preserve early failure/blocked/negative outcomes. The existing resilience
fast gate belongs to later authorized runtime-loop work, not this source-only documentation return.

## Candidate-specific experiments, all unexecuted

1. **B2-E01/C005 assertion-failure teardown:** inject an assertion failure after held native open enters,
   retain both invocation and typed result, release in finally and dispose that result even on failure.
   Observe original worker exit/close count/root/claim independently; preserve the primary assertion if
   cleanup also faults. Repeat held-send and discovery late-cleanup shapes. Use nonsecret marker data.
2. **B2-E02/C004 deployment:** minimal clean runtime-only package profile versus explicit unversioned-entry
   control, without hardware. Record name-resolution and classified unavailable-library discovery outcome.
   Any native-name fix needs its own API/body-preserving documentation re-review afterward.
3. **B2-E03 listener lifecycle:** isolated fake source ignores initial shutdown wake and releases only from
   test-owned finally. Stop past join budget, restart with independent source, then release old wait and
   observe no old callback/status/resource corruption of new session. No arbitrary thread abort or
   cross-thread native close. Child watchdog bounds test damage, not SDK cooperation.
4. **A handoff vectors:** refer A-E01 (TLV disposal/hash stability), A-E04 (metadata PID validity), A-E06
   (successful omitted-serial caching). Use original A IDs and source units; do not create duplicate B2
   finding identities or bypass discovery admission to force an expected result.

## Proposed composable example for later compilation

This explicitly owns the typed connection and borrows it for one raw session. `action` is application
logic whose returned task must represent all use of that session. The outer application keeps prompt/
credential services alive through actual callback completion. The example sends no arbitrary command
and requests no device execution during this investigation.

```csharp
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Sessions;

static async Task WithRawCtapAsync(
    IYubiKey device,
    Func<RawCtapHidSession, CancellationToken, Task> action,
    CancellationToken cancellationToken)
{
    await using IFidoHidConnection connection =
        await device.ConnectAsync<IFidoHidConnection>(cancellationToken);
    await using RawCtapHidSession session =
        await RawCtapHidSession.CreateAsync(connection, cancellationToken);
    await action(session, cancellationToken);
}
```

Initialization of this raw FIDO channel is lazy. The caller implements a complete protocol operation
inside `action` and awaits all work before returning. Raw session disposal precedes connection disposal;
active terminal wake/drain can still take an unbounded native lifetime. An external WaitAsync may stop
the application waiting without completing this method's owned cleanup. Successful sensitive output
has a separate caller-owned lifetime. B5 owns operation-specific framing/recovery examples.

## Other platforms

- **B3 macOS:** independently inspect native acknowledgment, callback-copy ownership, seized-device
  semantics, checked close and direct six-second read detachment. B2 Linux EOF/indefinite input and
  EINTR/EIO close policy supply no macOS result. MacOS source units remain B3-owned, including the shared
  linked SmartCardScenario's platform-host inclusion.
- **B4 Windows/support:** independently inspect native CCID/HID concurrency, handles/overlapped I/O,
  access and registered-wrapper failure policy. Consume C004's loader/package dependency and B1's
  native-shim ABI notes. Continue pinned source until the user supplies the ready Windows handoff;
  no unfinished worktree was fetched or read here.
- **musl/other Linux architectures:** glibc `libc.so.6`, constants and ioctl ABI in this source need an
  explicit separate support decision plus real native builds/execution. x64 evidence does not establish
  endian/ABI support on every Linux host.
- **PIV physical:** remains deferred by the user. Native connection lifetime success does not establish
  PIN/application state isolation, touch policy or cryptographic operation coverage.

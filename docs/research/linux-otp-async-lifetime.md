# Linux OTP async lifetime — L4

Scope: the internal typed OTP connection and a separate small native lifetime owner, plus the
existing synchronous feature-report expert facade over that owner. Accepted L0/L1/L2/L3 layers
remain separate. No typed factory/discovery routing or registry claim-transfer integration (L5),
adapter-profile rows, physical hidraw execution, native-AOT or platform-wide acceptance (L6).

## Exact seam and ownership

```csharp
internal static Task<LinuxOtpHidConnection> OpenAsync(
    string path, ILinuxHidReportApi reportApi, CancellationToken cancellationToken,
    IDisposable? physicalClaim = null);
```

`LinuxOtpHidConnection : IOtpHidConnection` has FeatureReportSize8 and Type HidOtp. Its private
`LinuxOtpHidNativeOwner` also backs `LinuxHidFeatureReportConnection`. The owner roots itself with
GCHandle before accepting the claim/acquiring native state; it does not root a facade. One dedicated
background thread starts only for open, without flowing the opening caller's ExecutionContext.
That same worker performs `open(O_RDWR|O_CLOEXEC)`, feature GET/SET and checked close. There is one
ordinary operation slot, no queue/backlog, per-call Task.Run, replacement worker, generic command
framework or reuse of the FIDO reactor owner. No feature ioctl runs on the read reactor.

Construction/thread-start failure before native acquisition releases only the original supplied
claim; failed claim release retains the root where an owner exists. Open exceptions/cancellation
await owner cleanup and preserve initial and cleanup failures. Cancellation while open is held
cannot release the claim before a later acquired descriptor is checked-closed. A fake L1 returning
an invalid SafeHandle is rejected: dispose that no-descriptor handle without native close, then
release the claim. It cannot be mistaken for an acquired descriptor requiring release evidence.

## Reports and cancellation

* Typed Send requires exactly8 bytes. Under the admission lock it owns a copy before escaping to
  the worker. Dispatch checks cancellation, invokes SetFeature at most once and clears the owned
  copy before the actual public TCS task becomes terminal, on both success and failure.
* Receive allocates8 bytes on the worker, invokes GetFeature exactly once and transfers successful
  storage to its caller. Failed/partial staging is cleared before task failure. L1's borrowed seam
  guarantees exact native transfer validation; the owner does not synthesize a count or pad data.
* Entry/pre-dispatch cancellation submits no feature call. Once submitted, GET/SET remains pending
  through native return even after token cancellation; successful native completion may succeed.
  There is no cancellation detach, suffix retry, replay or uncertain-mutation retry.
* All TCS continuations run asynchronously. Pending operation TCS and owned staging are tracked
  independently of the dispatch delegate, including after the delegate is removed for dispatch.
* The direct expert facade stays synchronous: constructor/Get/Set block over the same owner.
  Metadata and GET remain8. Direct SET preserves arbitrary supported caller length (0..16382),
  not typed8. L1 adds report ID zero and exact framed transfers: OTP8/native9; direct SET native
  length is caller length+1. L1 clears stack/pool framing in finally. No direct input-IO L3 code
  or public IHidInterface/IHidConnection/IOtpHidConnection signature changes.

## Shutdown, unexpected failure and finalization

Shutdown closes admission under the owner lock and returns one shared completion/outcome. An
accepted GET/SET runs to native return and public task terminality before checked close on the
original worker. Sync disposal drains that same outcome, refusing worker-thread self-drain.
No timeout or cross-thread close manufactures native abort or release proof.

Only L1 Released=true permits original-claim release/root removal. Linux EINTR/EIO prove descriptor
release while preserving a close diagnostic. EBADF, unknown evidence, close exceptions or claim
release failure retain/root/quarantine the original lifetime. Actual registry claims use the
existing TryMarkUnrecovered hook. Repeated disposal cannot retry native close or release a successor.

An unexpected loop error stops admission, faults pending actual open/send/receive tasks and clears
owned staging under the same lock. The surviving original worker performs checked cleanup; proven
release still releases the claim, but disposal retains the initial loop error, aggregated with
cleanup diagnostics. Failed/interrupted cleanup remains quarantined, with no replacement worker.
The event is retired under the setter/admission lock only after stopping is visible. A later
Dispose/finalizer request therefore never Sets a disposed event. Facade finalizers only request
shutdown with a nonthrowing backstop; collecting the facade cannot unroot unknown native ownership.

## No-hardware evidence and exact inventory

`LinuxOtpHidConnectionTests` drives the production owner under fake L1. L0 WithheldNativeProbe
asserts invocation return with pending task during held open/GET/SET/checked close. Other cases
exercise pre-dispatch cancellation, submitted cancellation and overlap refusal, concurrent sync/
async disposal, late canceled-open cleanup, invalid/failed open, partial-GET clearing, owned-output
isolation/clearing, claim-release failure, EINTR/EIO diagnostics and real registry quarantine.

`LinuxOtpWorkerFailureTests` interrupts the actual idle worker and queued open/GET/SET under the
real owner admission lock, not a substitute scheduler. It asserts actual pending task terminality,
cleared queued output, original-worker cleanup, first-error preservation, held-close drain and
released/unproven-close branches. An isolated child using existing PcscIsolatedProbe collects the
facade during held close, proves nonblocking finalization, then proves unknown release remains rooted.

`LinuxOtpRecoveryTests` runs OtpHidProtocol over this production owner. After ten accepted frame
reports, a held touch-wait GET prevents cancellation detachment. Protocol cancellation then sends
an acknowledged dummy reset before reuse; the next read-only capabilities frame returns a correlated
CRC-valid response and performs its completion reset. No device provisioning/operation occurs.
Existing L1 framing/short-transfer tests and OTP/macOS protocol regressions remain independent.

The reviewed source inventory is **219 sites /132 imports /37 waits /18 scheduling sites**. L4
removes the direct feature facade's one Task.Run disposal site, adds two owner waits, three direct
facade waits and two mutually exclusive context-start sites for its one native worker. Entries stay
`documented` + `outstanding`, not adapter or platform acceptance. L6 registry/profile rows are untouched.

## L5 handoff and remaining limits

L5 must select this internal OpenAsync from the typed Linux OTP factory and transfer real grouped
connection/discovery claims before acquisition. Direct expert opens still acquire no implicit grouped
claim. No public `ConnectAsync<IOtpHidConnection>` responsiveness claim follows from L4 alone.
Preserve this owner and exact original claim through late open cancellation and checked close; do
not wrap it in a per-operation worker or release a registration solely because DisposeAsync faulted.

The report seam remains synchronous and borrowed. A genuinely hung feature ioctl/open/close can
keep the sole worker and shutdown pending indefinitely. There is no native abort guarantee, hardware
latency bound, starvation benchmark, musl claim, selected-key reopen measurement or native-AOT run.
Thread construction/start failure is handled in production but not fault-injected with an extra
production scheduler seam. Fake tests do not establish device/kernel timing or whole-Linux safety.

## Verification record (2026-11-11)

All build/test commands used the repository toolchain, Release configuration. No integration/device
tests, device discovery, provisioning, system changes, commits or pushes were performed. Context7
was consulted for .NET TCS continuation scheduling and cancellation semantics; its cancellable-wait
wrapper was deliberately not adopted for submitted native feature calls.

Initial RED: `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxOtpHidConnectionTests"`,
run `20261111-032552-955-test`, exit1, 11 executed/11 failed/0 skipped, warnings0. The first behavioral
suite used a temporary reflection lookup so the missing Linux OTP type failed at runtime rather
than compilation. After implementation it calls the real typed OpenAsync directly. An earlier
attempt had a missing test namespace import and was corrected before this RED result. The inventory
also rejected the seven unclassified/new sites plus the removed stale Task.Run entry before review.

Final results (all exit0, RESULT PASSED, warnings0):

| Command | Run under `artifacts/toolchain/runs/` | Result |
|---|---|---|
| `dotnet toolchain.cs build` | `20261111-033451-813-build` | Full solution compiled, 0 warnings |
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxOtp"` | `20261111-033513-723-test` | 36 executed, 0 failed, 0 skipped |
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~Linux\|FullyQualifiedName~BoundaryInventory\|FullyQualifiedName~OtpHidProtocol\|FullyQualifiedName~MacOSOtp"` | `20261111-033458-705-test` | 375 executed, 0 failed, 0 skipped |
| `dotnet toolchain.cs -- resilience --fast` | `20261111-033521-545-resilience` | 184 executed, 0 failed, 0 skipped |
| `dotnet toolchain.cs complexity` | `20261111-033539-158-complexity` | All162 changed shipping methods within thresholds; includes accepted prior layers |

Each directory contains `run.log`, `result.json` and step logs (`01-build.log` or
`01-test-Core.UnitTests.log`). Toolchain retention is five runs; available final/prior logs were also
copied locally to `/tmp/opencode/linux-async-l4-evidence/`. That temporary archive is not committed.

Scoped `dotnet format Yubico.YubiKit.sln --no-restore --include <eight L4 edited/new C# files>
--verify-no-changes` exits0 after correcting final-newline style. It still emits the pre-existing
workspace-load warning noted by L0 (duplicate PublicAPI workspace documents), not a compiler/test
warning. `git diff --check` passes. `codemapper .` remains unavailable (exit127). Accepted L0–L3
changes and concurrent master ISA/status work were preserved; no L5/L6 routing/profile work started.

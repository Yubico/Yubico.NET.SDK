# Linux public HID factory and discovery routing — L5

Recorded 2026-11-11. Scope: typed public connection creation, discovery lease transfer, and Core
raw-session integration over the accepted L1/L2/L3/L4 native seams. This is managed, fake-native
integration evidence, **not** Linux hardware/native-AOT acceptance or an L6 adapter-profile claim.

## Routing and APIs

No public signatures changed. The production route is:

```text
LinuxHidInterface (enumerated descriptor/path)
  -> HidConnectionSlot
  -> YubiKeyDevice.ConnectAsync<IFidoHidConnection / IOtpHidConnection>
  -> acquire every known physical member's registry claim
  -> LinuxFidoHidConnection.OpenAsync / LinuxOtpHidConnection.OpenAsync
  -> existing L3/L4 native owner accepts that exact claim before native acquisition
```

`HidConnectionSlot.OwnsNativeLifetime` replaces the macOS-specific FIDO/OTP capability names. It
means built-in **macOS or Linux** HID whose native lifetime accepts the claim before open. The
constructor now accepts optional `ILinuxHidReportApi? linuxReportApi` and
`ILinuxHidReadReactor? linuxReadReactor`, alongside the existing macOS seams. Production defaults
are `LinuxHidReportApi.Instance` and, for FIDO only, `LinuxHidReadReactor.Shared`. There is no new
provider hierarchy, scheduler, global admission pool, or transport scan rewrite.

`LinuxHidInterface(HidDescriptorInfo)` is internal rather than private so tests can create the real
interface without enumeration or opening a device node. Its direct `ConnectToIOReports` and
`ConnectToFeatureReports` implementations are unchanged synchronous expert facades.

Typed Linux connections bypass `FidoHidConnection` / `OtpHidConnection` synchronous report adapters.
Windows/custom HID keeps that fallback and its registered wrappers. Existing macOS, built-in PC/SC,
external SmartCard, session attachment and transport-selection policies are preserved. Transport
failures do not trigger fallback. Requested transports must still match slots; mismatched discovery
requests submit no native open and release their untransferred discovery lease.

## Claim, timeout and disposal contract

* Public connect claims all known stable physical members first, including PC/SC siblings, not just
  the selected HID member. Same-PID paths alone never create an association. Existing conservative
  merger evidence, registry priority and one-connection/one-session rules remain authoritative.
* The capability is checked before invoking open. On transfer, the native owner becomes the sole
  release authority, including pre-canceled, failed and late-completing opens. No registered HID
  wrapper is placed around that owner, and an outer exception does not release its claim twice.
* `ProtocolDeviceInfo` selects the same owned HID slot for a direct slot and for a published
  composite provider. It acquires the selected interface's discovery lease, performs its existing
  final supersession check, marks transfer **before** invoking the owner's open, and releases in
  `finally` only if no transfer occurred. An open exception is not evidence that no native owner exists.
* Evidence budgets use the existing `Task.WaitAsync` wait-only model. Timeout/cancellation does not
  abort native calls or release claims. Abandoned discovery is marked unrecovered; public reconnect
  is refused until actual late cleanup releases the original registration. Epoch retirement still
  prevents late metadata from populating replacement evidence. It does not kill an admitted owner.
* L3 shutdown waits for real reactor quiescence/reference release, then checked close. L4 waits for
  admitted feature work, then checked close. Hung native work or broken wake can remain pending
  indefinitely; neither a timeout nor another worker supplies release proof.
* Linux EINTR/EIO close outcomes prove release but retain a `PlatformApiException` diagnostic for
  disposal. That diagnostic **does not** quarantine an already-released original claim. EBADF or
  other unknown release retains/quarantines ownership, and subsequent public/discovery acquisition
  throws `UnrecoveredConnectionException`. Repeated disposal shares the owner's outcome and does
  not retry close. Canceled/failed open plus failed cleanup preserves both errors in an
  `AggregateException`; only the owner decides whether the original claim was released.
* Late cleanup/repeated disposal/stale reactor generations cannot release a replacement's claim.
  Direct synchronous expert opens still acquire no implicit grouped physical claim.

Context7 `/dotnet/docs` was consulted for `Task.WaitAsync` timeout/cancellation semantics: it ends
only the wait, not the underlying operation. No new cancellable wrapper was added around native work.

## Tests and source inventory

`src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/LinuxHidRoutingTests.cs` contains **36** executed
cases using actual Linux interfaces/slots, `YubiKeyDevice`, the real process registry, L3/L4 owners,
and narrowly injected fake native/report/reactor seams. It reuses L0 `WithheldNativeProbe` and L3/L4
fakes rather than introducing a scheduler substitute. Coverage includes:

* Invocation returns a pending task while native open/checked close is held, for FIDO and OTP.
* Every grouped member is claimed before acquisition, including a nonselected PC/SC member;
  sibling transports are refused without native dispatch.
* Pre-canceled open submits zero native calls and releases acquired claims; canceled held open
  drains before release and preserves unrecovered cleanup errors. Failed open with proven cleanup
  permits retry; unknown close quarantines every member and is not hidden by a wrapper's finally.
* Both released diagnostic branches (EINTR/EIO), shared disposal errors, held quiescence with failed
  wake, exact-original-claim late release, stale callback and replacement admission behavior.
* Direct-slot and composite discovery timeout during held open, report-call timeout through held
  close, and unknown close through both provider shapes. Reconnect is refused until real cleanup.
* A real `FindYubiKeys` scan with a held Linux metadata call, retirement, late successful serial333,
  replacement serial999 and a subsequent cache hit showing only replacement evidence was retained.
* A same-PID two-key/four-interface Linux scan: real FIDO/OTP metadata framing and CRC produce
  serial111/222 groups, each contains only matching paths, and subsequent typed routing remains
  distinct. Independent same-PID standalone interfaces can also be opened simultaneously.
* Public `RawCtapHidSession` and `RawOtpHidSession` creation, one-live-session refusal, idle borrowed
  disposal, subsequent lazy native initialization/exchange, and sequential reuse without closing the
  connection or releasing its registry claim. FIDO performs a second real framed INIT after reuse;
  OTP performs ten frames and the completion reset. These are not Ctap2/public applet journey tests.

Source inventory remains **219 sites / 132 imports / 37 waits / 18 scheduling sites**. No boundary
invocations were added/removed by L5. The two exact fallback identities in
`BoundaryInventory/core-boundaries.v1.json` remain
`OpenRawConnectionAsync#2D6CDB08BA90` and `OpenRawConnectionAsync#ABE529EA2920`; their evidence/rationale
now accurately says **Windows/custom fallback**, with Linux migrated to the L3/L4 owners. Statuses
remain `documented` + `outstanding`. Adapter profiles and public synchronous-registry rows are untouched.

Changed shipping files: `Devices/HidConnectionSlot.cs`, `Devices/YubiKeyDevice.cs`,
`Devices/ProtocolDeviceInfo.cs`, `Transports/Hid/Linux/LinuxHidInterface.cs` under `src/Core/src/`.
Also changed: the focused test file above, the two inventory rationales, Core `CLAUDE.md`/`README.md`,
and this handoff. Accepted L0–L4 work was preserved; master ISA/status files remain orchestrator-owned.

## Verification

All commands use the repository toolchain in Release, with nonzero executed counts and warnings0.
Initial RED: `20261111-034112-655-test`, 19 executed/19 failed/0 skipped, exit1. The initial test
helper used reflection to detect the absent injection seam, failing before any device-node access.
After implementing the seam/routing it calls the constructors directly. Initial GREEN:
`20261111-034135-861-test`, 19 executed/0 failed. Later cases extended the same behavioral suite.
An intermediate test listing failed because this Core project does not reference NSubstitute;
the helper was replaced with two tiny enumerator fakes, without dependency/project changes.

Final results (all exit0, RESULT PASSED, warnings0):

| Command / scope | Run under `artifacts/toolchain/runs/` | Result |
|---|---|---|
| `dotnet toolchain.cs build` | `20261111-034904-111-build` | Full solution, 0 warnings / errors |
| `dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxHidRoutingTests"` | `20261111-034911-000-test` | 36 executed, 0 failed / skipped |
| Core targeted regression selection below | `20261111-034918-727-test` | 839 executed, 0 failed / skipped |
| `dotnet toolchain.cs -- resilience --fast` | `20261111-034949-405-resilience` | 184 executed, 0 failed / skipped |
| `dotnet toolchain.cs complexity` | `20261111-035007-562-complexity` | All172 changed shipping methods within thresholds (includes accepted L1–L4) |

Exact regression command:

```bash
dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~LinuxHidRoutingTests|FullyQualifiedName~MacOS|FullyQualifiedName~Pcsc|FullyQualifiedName~DeviceConnection|FullyQualifiedName~ConnectionOwnership|FullyQualifiedName~Discovery|FullyQualifiedName~FindYubiKeys|FullyQualifiedName~CompositeDevice|FullyQualifiedName~YubiKeyDevice|FullyQualifiedName~HidConnectionSlot|FullyQualifiedName~RawCtap|FullyQualifiedName~RawOtp|FullyQualifiedName~BoundaryInventory|FullyQualifiedName~Linux"
```

Scoped `dotnet format Yubico.YubiKit.sln --no-restore --include` on the four edited shipping files
and the new test file, followed by `--verify-no-changes`, exits0. It emits the existing workspace-load
warning (duplicate PublicAPI documents noted by L0), not a compiler/test warning. `git diff --check`
passes. `codemapper .` remains unavailable (exit127), so source was inspected directly.

Toolchain retains only five runs; available outputs are archived locally under
`/tmp/opencode/linux-async-l5-evidence/` (not committed).
Each run directory includes `run.log`, `result.json` and step logs. Early RED logs have aged out of
toolchain retention; the observed counts above are preserved here, not claimed as archived raw files.

## L6 handoff / deliberately not claimed

No subagents, commits, pushes, device operations, hardware discovery, provisioning, system changes,
or native-AOT execution were performed. Full-solution compilation includes integration projects but
does not execute them. The new route uses fake native APIs; it is not hidraw/kernel/hardware timing,
musl support, starvation/resource benchmarking, whole-Linux acceptance, or a release recommendation.

L6 still owns Ctap2/public applet journeys, adapter profile acceptance, public synchronous-registry
accounting, selected-key/native/hardware runs and remaining acceptance documentation. Existing owner
source comments that refer historically to L5 do not imply L6 acceptance; this document and Core docs
record the now-migrated route.

`OtpHidProtocol.Configure` has an existing synchronous initialization wait. It was **not changed**:
normal Management OTP initialization awaits `OtpBackend.ReadStatusVersionAsync` / `ReadStatusAsync` before
`ConfigureProtocolAsync`; YubiOTP similarly awaits `HidBackend.ReadInitialStateAsync` / `ReadStatusAsync`
first. Raw OTP sessions retain lazy initialization. Source inspection does not establish a newly
introduced regression or an applet journey result; L6 should assess the actual public applet path.

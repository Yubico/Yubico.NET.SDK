# Bounded YubiOTP hardware probe preparation

**Prepared only; no build, test, hardware, formatter, commit or agent execution.** Sol,
2026-11-04. This assignment owns only `validation/hardware-probe/` and this plan. The
coordinator owns the currently running integration suite, source acceptance, compilation,
device execution and final evidence interpretation. Do not start a probe while that suite
or another toolchain/device operation is running.

## Authorization, isolation and real test context

The user authorized dedicated serial **103**, reported firmware **5.8.0.beta.0**; both slots
were initially empty. Each probe uses the real Tests.Shared allow-list/discovery machinery,
a `Serial103Filter`, two metadata serial assertions, and a fresh explicit-HID serial read
returning 103 before its first configuration write. There is no new approval/config gate or
automatic allow-list edit. Missing matching hardware can be reported skipped by the genuine
harness; it is not evidence that a lane executed.

No fixture provisions or resets anything. Each lane refuses to start if a fresh HID snapshot
finds slot 2 occupied, records slot 1's initial configured state, and then owns its temporary
slot-2 writes, including an ambiguous failed write. This protects evidence from a previous
failed cleanup without adding per-mutation authorization. The coordinator must reconcile any
occupied slot before retrying. Unknown original slot secrets cannot be restored.

The host is
`validation/hardware-probe/tests/Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests/src/Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests.csproj`.
Its unique selector is **`AuditOtp001.Hardware.IntegrationTests`**. Both `tests/` and `src/`
path components are intentional: the repository toolchain independently discovers buildable
projects and integration projects by those components. No new project has a competing
`YubiOtp` selector substring in its filename. Do not select the whole campaign by a broad
`AuditOtp001` match when building/running this host.

The host imports canonical root `Directory.Build.props` and `Directory.Build.targets`, uses
root central package versions and the actual YubiOtp integration project's xUnit-v2/VSTest
package set, and directly references real YubiOtp, Core and Tests.Shared projects. Core is a
normal build dependency only: preparation consulted its public API snapshot, **not Core
implementation**. Prompt recording uses the public `IUserPresencePrompt`, not a protocol
mock/stub. No production source, tests, snapshots or lifecycle policy is changed. Warnings
remain errors and analyzers stay enabled.

Probe intermediates, binaries, VSTest results and compiler-input capture go only below
`/tmp/opencode/yubiotp-001-hardware-probe`. Referenced SDK/shared projects keep their existing
canonical build context. Actual compiler input paths, resolved references, analyzers,
analyzer configs, command-line args and evaluated policy are captured by host MSBuild
targets. They must be retained after the coordinator's build, not inferred from this plan.

## Four independent lanes

All tests use real public SDK methods and only program/delete **slot 2**. There is no slot-1,
NDEF, scanmap, PIN, protection or global application reset call. A known raw-static scan-code
configuration has AppendCr disabled and is **never triggered**. Nobody should touch the key
while any probe is running; prompts explicitly say **DO NOT TOUCH**.

| Test method | Preparation/actions | Actual lane assertions and evidence |
| --- | --- | --- |
| `StatusMatrix_ReportsFreshFlags_AndMeasuresSmartCardHmac` | HID programs raw static, non-touch short-mode HMAC and RequireTouch short-mode HMAC in turn. After each write, separate fresh explicit HID and SmartCard sessions read status, each disposed before the next. Non-touch HMAC is calculated on HID and attempted on SmartCard with a recording prompt. | Six fresh configured snapshots must show slot 2 configured and slot 1's configured state unchanged. Touch indicators, raw/effective firmware and LED flag are logged without preassuming their interpretation or equality between transports. HID must return exactly 20 API bytes matching host HMAC, with no prompt events. SmartCard is a **capability measurement**: either a 20-byte host-matched HMAC, or exactly `ApduException` status **6985** classified as device rejection. Its exact type/status/header and observed request/terminal events are recorded; any unexpected exception/status or mismatched response fails. If a prompt occurs it must have one matching terminal (`Completed` for HMAC, `Failed` for rejection), application YubiOTP, slot Two and terminal token None. No callback is treated as proof of physical touch. |
| `HidCancel_AfterDeviceWaiting_ResolvesCancelledOnce_AndReopens` | Programs RequireTouch HMAC, reopens HID with prompt recorder, starts calculation, waits at most 10 seconds for `DeviceWaiting`, then cancels its linked caller token without touching. Drains that same operation before disposing its session. | Requires cancellation exception while the separate 25-second watchdog is still live, exactly one DeviceWaiting request and one same-context `Cancelled` terminal with token None. Records UTC prompt times, time to cancellation, drain time and exact exception type/HResult. Missing DeviceWaiting, earlier completion, wrong outcome, duplicate terminal or watchdog cancellation fails. Fresh cleanup and known-answer non-touch reopen/reprovision/recovery follow even after an assertion failure, if cleanup succeeded. |
| `HidNaturalTimeout_WithoutTouch_ResolvesTimedOutOnce_AndReopens` | Programs RequireTouch HMAC, reopens HID and deliberately never touches. Uses an outer token with up to 65 seconds, not the existing successful-touch test's 30-second caller CTS. | Requires the module-identified `OtpHidTouchTimeoutException` name, with the outer token **not** cancelled, one DeviceWaiting request and one same-context `TimedOut` terminal with token None. Records actual elapsed time and exception type/HResult. Outer cancellation, `Failed`, unexpected success or missing prompt is failure, not timeout evidence. Fresh cleanup and non-touch reopen known-answer recovery are attempted. No exact modern timeout duration is assumed from Core internals. |
| `AesTicket_DecryptsSixByteChallenge_AndHasVendorCrcResidual` | Programs known 16-byte AES key through `YubicoOtpChallengeResponseSlotConfiguration`, explicitly non-touch HID. Sends the same known six-byte challenge twice. | Each API response must be exactly 16 bytes. .NET AES ECB/no padding decrypts into a 16-byte stack buffer. Its first six bytes must equal the challenge (this config's UID is zero), and an independent bitwise CRC16 over all 16 decrypted bytes must have residual F0B8. Repeated-ticket equality is recorded only; nondeterminism is **not** an assertion. Prompt events must be absent. No ticket, plaintext, key, challenge-derived response or access code is logged. |

The optional access-code retain/change/remove lane is deliberately **omitted** to keep this
assignment bounded. It needs additional authenticated-write/rejection cases and cleanup with
potentially ambiguous new/current codes. This omission is not evidence for OTP-C001/C004.

## Timeout and cleanup model

The timeout test's 65-second outer allowance substantially exceeds the **legacy manual's**
15-second description (§4.3); the actual 5.8 beta device behavior is measured, not inferred to
be exactly 15 seconds. Every lane shares an **80-second cooperative total budget** starting
with fresh initial OTP status, so setup, cleanup and recovery consume the same finite budget.
Each failed lane disposes its calculation/programming session before the wrapper cleans up.

Cleanup is independent of the operation's cancelled token. It tries a fresh explicit HID
session, then a fresh explicit SmartCard session if necessary, with at most six seconds per
attempt and the remaining total budget. Each attempt reads fresh status, deletes slot 2
**only if configured**, disposes that session, and verifies empty slot 2 plus unchanged slot-1
configured state in another fresh explicit HID session. Empty slots are not blindly deleted.
All actions use `CreateYubiOtpSessionAsync`/`DeleteSlotAsync`; any protocol reset/dummy handling
remains inside the SDK APIs. No hand-coded feature reports, raw APDU, reset emulation or Core
implementation assumption is introduced. Failed cleanup attempts are logged separately;
cleanup success after fallback is labeled recovery rather than silently erasing the attempt.

Cancel/timeout lanes then reopen and program a separate known non-touch HMAC, require a
host-matched response, and clean it up again. Recovery material is cleared even on failure.
Original test errors and cleanup/recovery errors are preserved together, never converted into
a passing expected-error result. A cleanup failure stops recovery and must stop coordinator
progression until the readable slot state is inspected.

`probe.runsettings` sets a **90,000 ms VSTest test-session limit**, one process and temporary
results directory. The project selects that file through `RunSettingsFilePath`; the actual
execution must confirm it is effective. Run **one lane per filtered invocation**, not all four
in one process. The 90-second limit bounds the test session, not compilation/toolchain startup.
It is a last-resort failure bound, **not a cleanup guarantee**: an unresponsive native operation
or killed host can prevent managed finally blocks. If a host is aborted, do not report cleanup
success; the coordinator must reopen/read status and explicitly finish slot-2 cleanup. No
power cycle or touch is requested automatically.

## Verifier and source provenance

`validation/hardware-probe/provenance.json` is generated by `prepare-provenance.py` and records
the complete probe source/config hashes, original project/config/API/template hashes and
exact vendor passages. The helper also parses Python/XML and checks the unique discovery
layout; it never compiles C#, discovers/runs tests or opens hardware. Original provenance
is exclusive-create; do not overwrite it after fixes. Retain the initial version and make a
new revision record if the coordinator changes source or if later module editing changes
the input hashes. Compiler capture provides the actual final compilation context separately.

The independently implemented CRC is based on the original vendor **YubiKey Manual 3.3,
17 September 2014**, §§6.1.1, 6.1.6 and 6.3, independently read from the campaign's retained
text. §6.3 specifies initial FFFF, reflected polynomial 8408, and residual F0B8 over data
including CRC. It is not a call to the SDK's HID `ChecksumUtils`. The original vendor PDF URL
and SHA-256 are retained with the quoted passages. These historical passages supply a
verifier hypothesis tested against this device, not blanket 5.x conformance or certification.

Current API/framework checks used Context7 for .NET AES `DecryptEcb(ReadOnlySpan<byte>,
Span<byte>, PaddingMode)` and xUnit-v2 serialization/runner configuration; Microsoft Learn's
VSTest runsettings documentation confirms `RunSettingsFilePath` and millisecond
`TestSessionTimeout`. URLs are retained in provenance. No SDK library/framework code was
replaced with hand-written stubs.

## Proposed coordinator commands — not executed during preparation

From repository root, after the current suite finishes and the coordinator accepts source:

```bash
RUN=docs/research/documentation-audit/modules/yubiotp/pass-1/yubiotp-001
PROBE="$RUN/validation/hardware-probe"

python3 "$PROBE/prepare-provenance.py" verify
python3 "$RUN/validation/run-command.py" hardware-probe-v1-build dotnet toolchain.cs -- build --project AuditOtp001.Hardware.IntegrationTests --clean
python3 "$PROBE/capture-compiler.py" v1-build

python3 "$RUN/validation/run-command.py" hardware-probe-v1-status dotnet toolchain.cs -- test --integration --project AuditOtp001.Hardware.IntegrationTests --filter "FullyQualifiedName~StatusMatrix_ReportsFreshFlags_AndMeasuresSmartCardHmac"
python3 "$RUN/validation/run-command.py" hardware-probe-v1-aes dotnet toolchain.cs -- test --integration --project AuditOtp001.Hardware.IntegrationTests --filter "FullyQualifiedName~AesTicket_DecryptsSixByteChallenge_AndHasVendorCrcResidual"
python3 "$RUN/validation/run-command.py" hardware-probe-v1-cancel dotnet toolchain.cs -- test --integration --project AuditOtp001.Hardware.IntegrationTests --filter "FullyQualifiedName~HidCancel_AfterDeviceWaiting_ResolvesCancelledOnce_AndReopens"
python3 "$RUN/validation/run-command.py" hardware-probe-v1-timeout dotnet toolchain.cs -- test --integration --project AuditOtp001.Hardware.IntegrationTests --filter "FullyQualifiedName~HidNaturalTimeout_WithoutTouch_ResolvesTimedOutOnce_AndReopens"
```

These are separate, serial commands, **not** a batch authorization to continue after a failed
lane/cleanup. Do not use `--smoke` (these probes deliberately carry Slow). Successful physical
touch is neither needed nor wanted, so these tests do not carry RequiresUserPresence.
Assembly/collection parallelism is disabled both in source and the actual runner JSON.
Check RESULT/exit status, zero warnings, exactly one executed and zero skipped per lane,
original failures, observed transport/status/prompt events, cleanup and recovery separately.
SmartCard 6985 remains **capability-rejected**, even if its measurement test passes.

Every toolchain run is retained before rotation by the existing wrapper. The compiler-capture
helper copies only six named text outputs and hashes their actual source/reference/analyzer
dependencies, never DLL/PDB/cache files. Test invocations can rebuild: capture their compiler
inputs with a new unique evidence name if source/dependency context changed. Retain non-text
VSTest outputs only by hashes/explicit exclusions if needed; do not copy binaries or caches
into this run. Final fresh slot state and any missing timing/cleanup evidence remain the
coordinator's report responsibility.

**Readiness:** complete proposed source/configuration and provenance are prepared. C# source
acceptance, compiler/analyzer success, effective VSTest bound and all four hardware outcomes
remain **pending**. No behavior, capability, cancellation, timeout, CRC or cleanup result is
claimed by this preparation.

Static preparation inspection was executed without dotnet/device access and exited 0:
`validation/hardware-probe/preparation-static.log` retains its command/output. It reconciled
eight probe source/config records, 28 original input hashes, vendor passages, Python/XML
syntax, the canonical package/runner settings and exactly one discoverable unique host.
This verifies declared configuration only, not evaluated compiler or VSTest behavior.

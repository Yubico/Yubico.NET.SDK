# Older-key discovery blocker

## Observations

- With both authorized YubiKey5 devices attached, the protocol worktree's shared discovery found two devices but excluded serial9681620 before OATH execution.
- Exception: `UnrecoveredConnectionException` for `hid:/dev/hidraw7:0001`; inner cause: a bounded discovery read was abandoned while native ownership remained active.
- The alpha key125 completed its OATH cases. A separate SmartCard-pinned attempt still hit the guard for9681620.
- Core source explains why selecting SmartCard alone does not avoid this: `FindYubiKeys` enumerates all interfaces and performs identity reads before applying the requested transport filter. Multi-device PID disambiguation can trigger those reads. Registry ownership is cleared only when the native discovery lease actually releases.
- On request, the user unplugged125. Read-only `ykman list` then showed only9681620, firmware5.4.3.
- On coordinator branch361567eb, the existing `CredentialLifecycle_PutListCalculateDelete_Succeeds` integration case passed: **1 executed, zero skipped/failures/warnings**, using9681620 alone.

## Evidence

- Two-key run: `/tmp/opencode/oath-protocol-evidence/20261102-220858-877-test/01-test-Oath.IntegrationTests.log`, lines19–35.
- SmartCard-pinned attempt: `/tmp/opencode/oath-protocol-evidence/20261102-221033-774-test/01-test-Oath.IntegrationTests.log`.
- Single-key command: `dotnet toolchain.cs -- test --integration --project Oath.IntegrationTests --smoke --filter 'FullyQualifiedName~CredentialLifecycle_PutListCalculateDelete_Succeeds'`.
- Single-key result: `artifacts/toolchain/runs/20261102-223809-745-test/`, coordinator repository.
- Relevant Core source: FindYubiKeys identity/metadata discovery; DiscoveryIdentityReader bounded reads; ProtocolDeviceInfo.DiscoveryReadLeaseTracker; DeviceConnectionRegistry.InterfaceOwnership release/recovery.

## Interpretation and limits

The older key can perform the existing OATH lifecycle when tested alone. The blocker is associated with the two-key discovery path, not demonstrated unsupported OATH firmware. The exact reason the HID identity read does not finish within budget is not established; parallel-interface interference and a transport-specific read problem must not be conflated without further evidence.

The ownership guard is functioning as designed by refusing access while native release is unproven. No guard removal, quarantine clearing, configuration workaround, or Core refactor was performed. Corrected protocol tests subsequently passed on9681620 alone (`20261102-223955-624-test`): two live and two offline cases, with newer-device rows skipped. They also passed on125 alone after the user swapped devices (`20261102-230114-130-test`). Track broader two-key discovery diagnosis separately from the OATH implementation fixes.

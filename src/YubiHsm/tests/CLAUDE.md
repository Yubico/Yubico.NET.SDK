# CLAUDE.md - YubiHSM Tests

This file provides guidance for the YubiHSM module test infrastructure.

## Required Reading

Read [`docs/TESTING.md`](../../../docs/TESTING.md) for runner requirements. Use `dotnet toolchain.cs test`
to select the appropriate xUnit runner.

## Test Projects

- `Yubico.YubiKit.YubiHsm.UnitTests` - Unit tests for YubiHSM module
- `Yubico.YubiKit.YubiHsm.IntegrationTests` - Integration tests requiring a YubiKey with the YubiHSM Auth applet and firmware 5.4.3+

`WithHsmAuthSessionAsync` resets the applet before the body by default and closes its sessions/connection
afterward. The body can leave credentials or a rotated management key in persistent state; arrange explicit
final reset/readback for a hardware run. Its SCP parameters apply to the body session, while the initial
reset session uses the supplied protocol configuration alone.

Keep comments tied to assertions. Length/metadata checks establish their stated shape only. The two
touch-tagged cases inspect the stored flag; physical CALCULATE/touch/cancellation needs separate evidence.
The asymmetric dummy-input case checks exact `ApduException` and nonzero status with several invalid
inputs. Use independently checked handshake material and a real HSM for interoperability qualification.

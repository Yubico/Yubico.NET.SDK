# 26e PIV retry-limit change resets the PIN and PUK (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (sub-item of #26; the audit lists #26 as one row) |
| Our verdict | Confirmed. The gap is documentation. Yubico documents the reset. |
| Our severity | HIGH for the documentation gap. Callers assume the old PIN still works after the call, and it does not. |
| Root cause | Device behaviour. The command resets the PIN and PUK. The SDK's public docs omit that. |
| Fix group | A (documentation only). An acknowledgement parameter would be a B or C decision. |
| Evidence | Static (Yubico docs, specs, SDK code). No unit test, because a fake transport cannot show an on-card reset. Hardware not run. |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). `IPivSession.cs`, `PivMetadataProtocol.cs`, and the `SetPinAttemptsAsync` wrapper in `PivSession.cs` (lines 426-431) are not in the diff. |

## What the audit says

The audit says the public docs describe changing retry limits without warning that PIN and PUK are reset:

> "An authorized caller attempting to tighten retry limits can therefore unintentionally restore known default credentials."

Abbreviations: PIN is personal identification number. PUK is PIN unblocking key. PIV is Personal Identity Verification. APDU is application protocol data unit. INS is the APDU instruction byte.

## What is right

- The device resets both secrets. Yubico's PIV extensions spec says so: "Both PIN and PUK will be reset to default values when this is executed." ([Yubico extensions](https://developers.yubico.com/PIV/Introduction/Yubico_extensions.html), SET PIN RETRIES).
- The SDK's own internal remark knows this. "This command also resets PIN and PUK to defaults." ([PivMetadataProtocol.cs#L436-L437](../../../src/Piv/src/Metadata/PivMetadataProtocol.cs#L436-L437)).
- The SDK sends the Yubico command as specified (INS `0xFA`, [PivMetadataProtocol.cs#L464-L466](../../../src/Piv/src/Metadata/PivMetadataProtocol.cs#L464-L466)).
- The default values are published. The technical manual lists "PIN: 123456" and "PUK: 12345678" ([YubiKey Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html), Default Values). So a reset restores known credentials.

## What is wrong or imprecise

- The public XML doc is incomplete. `SetPinAttemptsAsync` says only "Set PIN and PUK retry limits." ([IPivSession.cs#L158-L164](../../../src/Piv/src/IPivSession.cs#L158-L164)). The warning exists only in an internal remark, which users do not see.
- The reset is not limited to a change. Yubico's docs describe it as unconditional. They do not say the reset is skipped when the limits stay the same, so a call that sets the default limits also resets the credentials. The audit's word "tighten" understates this.
- Changing the retry limits resets both credentials in one command ([PivMetadataProtocol.cs#L464-L466](../../../src/Piv/src/Metadata/PivMetadataProtocol.cs#L464-L466)). Restoring non-default credentials takes further commands, so the complete sequence is not atomic. Yubico's docs say: "If you don't want to leave the PIN and PUK as the default values, follow this command with the Change reference data command." A failure after the first command, and before the credentials are restored, leaves the defaults in place.
- The SDK's own test comments already say the call resets the PIN ("SetPinAttemptsAsync resets PIN to default, so re-verify", [PivPinRetryTests.cs#L133](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPinRetryTests.cs#L133)). That knowledge sits in test comments, not in the public docs.
- The SDK's guard checks only management-key authentication ([PivMetadataProtocol.cs#L449-L452](../../../src/Piv/src/Metadata/PivMetadataProtocol.cs#L449-L452)). The SDK's integration tests say the call needs both: "SetPinAttemptsAsync requires BOTH PIN verification AND management key auth" ([PivPukTests.cs#L150](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPukTests.cs#L150)). The production code does not check the PIN, and the public docs do not mention it. Yubico's docs say the PIN must also be verified, and the Yubico extensions spec says "PIN has to be validated". Not run on hardware: whether the card rejects the call without a verified PIN, on any firmware.
- The existing hardware tests do not show the reset. They verify the PIN before the call, but no test changes the PIN, so the default PIN still works afterwards whether or not the card reset it.

## Why it matters

- Who can trigger it: code that calls `SetPinAttemptsAsync`, usually provisioning or admin tooling. The caller must hold the management key and the PIN.
- Impact: after the call, the PIN is `123456` and the PUK is `12345678`. Anyone with the key who knows the defaults can verify the PIN, use PIN-protected keys, or unblock with the PUK. The application's stored PIN stops working, which can lock out its own flows.
- Preconditions: management-key and PIN authentication by the caller. Physical or logical access to the key later.

## Specification

- Yubico PIV extensions, SET PIN RETRIES. Source: [Yubico extensions](https://developers.yubico.com/PIV/Introduction/Yubico_extensions.html).
  > "Set the PIN retries for PIN and PUK. Both PIN and PUK will be reset to default values when this is executed. For this authentication in management mode is required and PIN has to be validated."

- Yubico SDK user manual, PIV commands, Set PIN retries. Source: [PIV commands](https://docs.yubico.com/yesdk/users-manual/application-piv/commands.html). Availability: "All YubiKeys with the PIV application."
  > "Note also that this will reset the PIN and PUK to the default values."

  > "Before the YubiKey can set the PIN retries, the caller must have authenticated the management key and verified the PIN."

- YubiKey Technical Manual, Default Values. Source: [YubiKey Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html).
  > "PIN: 123456" and "PUK: 12345678"

- SDK contract. `SetPinAttemptsAsync` is documented as "Set PIN and PUK retry limits." ([IPivSession.cs#L158-L164](../../../src/Piv/src/IPivSession.cs#L158-L164)).

## Canonical Python reference

Python is correct here. The same device behaviour is documented on the method.

- The docstring says the reset happens, and that the caller must authenticate the management key and verify the PIN ([yubikit/piv.py#L1046-L1055](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1046-L1055)):
  ```python
  def set_pin_attempts(self, pin_attempts: int, puk_attempts: int) -> None:
      """Set PIN retries for PIN and PUK.

      Both PIN and PUK will be reset to default values when this is executed.

      Requires authentication with management key and PIN verification.
  ```
- The SDK's gap is the missing public warning and the missing PIN check, not the device behaviour.

## Sibling SDKs (context only)

Not checked for this sub-item.

## Reproduction

- Unit: none. A fake transport can record the APDU but cannot show a reset of card state. The verification report reached the same conclusion.
- Existing hardware tests: [PivPinRetryTests.SetPinAttempts_CustomLimit_EnforcedDuringFailedAttempts](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPinRetryTests.cs#L115) and [PivPukTests.SetPinAttemptsAsync_CustomLimit_EnforcesLimit](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPukTests.cs#L145). They check the retry counts only. Neither changes the PIN, so neither shows the reset.
- Hardware: **not run.** A controlled check on a test key: change the PIN with `ChangePinAsync`, confirm `GetPinMetadataAsync().IsDefault` is false, call `SetPinAttemptsAsync`, then confirm `IsDefault` is true again. The `IsDefault` flag is already read in [PivPinRetryTests.cs#L175-L183](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivPinRetryTests.cs#L175-L183).

## Proposed fix

- Recommendation (group A, documentation):
  1. Add a `<remarks>` section to `IPivSession.SetPinAttemptsAsync` and to the matching facade method on `PivSession` ([PivSession.cs#L426-L431](../../../src/Piv/src/PivSession.cs#L426-L431)). It should say: "Changing retry limits resets both credentials. The PIN is reset to 123456 and the PUK to 12345678. Restoring non-default credentials requires subsequent commands; the complete sequence is not atomic."
  2. State the PIN requirement as well as the management-key requirement.
  3. Move the existing internal remark ([PivMetadataProtocol.cs#L436-L438](../../../src/Piv/src/Metadata/PivMetadataProtocol.cs#L436-L438)) to the public docs, or point to it.
- Options considered:
  - (a) Documentation only (recommended for now). No code change and no breaking change.
  - (b) Require an explicit acknowledgement parameter, such as `resetsCredentials: true`. If it replaces the signature, it is breaking. An overload is additive.
  - (c) Provide a combined helper that sets the retries and then changes the PIN and PUK. It needs defined failure and recovery behaviour. It must not claim atomicity.
- API impact: (a) none. (b) additive if an overload, breaking if it replaces the signature. (c) additive.
- Proving test: a documentation check. For the behaviour, a manual hardware check on a test key, as described above.
- Depends on / interacts with: #22 (PIN state is card-wide, which applies here too).
- Open questions:
  1. Should the SDK check PIN verification before the call? It cannot read the card's PIN state today. It tracks only management-key authentication per session ([PivSession.cs#L54](../../../src/Piv/src/PivSession.cs#L54)).
  2. Is an acknowledgement parameter worth a breaking change in 2.0? The verification report recommends documentation now, and leaves the parameter as a 2.0 decision.

## Check it yourself

- Read the public contract: [IPivSession.cs#L158-L164](../../../src/Piv/src/IPivSession.cs#L158-L164).
- Read the internal remark and the management-key-only guard: [PivMetadataProtocol.cs#L433-L472](../../../src/Piv/src/Metadata/PivMetadataProtocol.cs#L433-L472).
- Read the Python docstring: [yubikit/piv.py#L1046-L1061](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1046-L1061).
- Read the Yubico extensions spec: [SET PIN RETRIES](https://developers.yubico.com/PIV/Introduction/Yubico_extensions.html).
- Existing hardware tests (test keys only; they reset the PIV application):
  ```bash
  dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests --filter "FullyQualifiedName~SetPinAttempts"
  ```

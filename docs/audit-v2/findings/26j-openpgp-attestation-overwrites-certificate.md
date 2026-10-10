# 26j OpenPGP attestation overwrites the cardholder certificate (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | By design (needs docs) |
| Our severity | MED (documentation footgun) |
| Root cause | Device behaviour. The SDK does not warn about it. |
| Fix group | A (documentation only) |
| Evidence | vendor documentation. No unit or hardware test: a recorded response cannot show a device-side write, and a hardware run would overwrite a certificate on a card. |
| Since the audit | `IOpenPgpSession.cs` is unchanged. `AttestKeyAsync` changed only in its user-presence context call ([OpenPgpSession.Keys.cs:157-158](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L157-L158) at the base). The attestation command and the read-back are unchanged. |

## What the audit says

The SDK's attestation method generates an attestation certificate, and the device writes it into the cardholder certificate slot for the attested key. The method does not warn about this, and it does not back up the certificate that was there before.

> "A diagnostic operation can therefore replace an enrolled certificate even if the subsequent read fails; the private key is not deleted."

Abbreviations: OpenPGP is the card application and message format. SIG is the signature key slot. DO is data object, the card's tagged storage item. GET_ATTESTATION is the card command that produces the attestation statement. PIN is personal identification number. SDK is software development kit.

## What is right

- **The device overwrites the slot.** Yubico's documentation says: "When an attestation statement is generated, it is placed in the OpenPGP cardholder certificate slot for the attested key. For example, if the signature key is attested, the attestation statement is placed in the SIG cardholder certificate slot (index 2 of DO 0x7F21). Any data currently stored in the cardholder certificate slot is overwritten." (see Specification.)
- **The SDK knows the write happens.** The code comment says GET_ATTESTATION "generates the attestation cert and writes it to the certificate slot for the key" ([OpenPgpSession.Keys.cs:153-155](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L153-L155)). The read-back is in the same slot ([OpenPgpSession.Keys.cs:165](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L165)).
- **The write happens before the read.** So the old certificate is gone even if the read then fails. The audit is right about that ordering.
- **The public documentation is silent.** `IOpenPgpSession.AttestKeyAsync` says only that it "Gets an attestation certificate for the specified key slot" and lists the exceptions ([IOpenPgpSession.cs:241-251](../../../src/OpenPgp/src/IOpenPgpSession.cs#L241-L251)). It does not mention the overwrite.

## What is wrong or imprecise

- **"By design" is correct, but the device is the designer.** The SDK sends one command and reads the result. Nothing in the SDK's code causes a wrong write. The gap is the missing warning in the SDK's documentation.
- **The SDK does not preserve the old certificate.** Nothing in the method saves the previous certificate. Restoring it is not automatic. [verification-report.md](../verification-report.md) says restoring may need admin authorization; that is not verified here.
- **The SDK documentation omits a second requirement.** yubikey-manager's documentation for the same operation says the call "Requires User PIN verification" ([openpgp.py:1699-1705](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L1699-L1705)). The SDK's documentation does not say so. Not verified on a device.

## Why it matters

- **Who triggers it.** An application that calls `AttestKeyAsync` on a key whose cardholder certificate slot holds a certificate it still needs. For example, a certificate issued by an internal CA for signing, or a certificate used for mail or TLS client authentication.
- **Impact.** The certificate is lost, with no error and no backup. The private key is not deleted, so the key still works, but the certificate that applications rely on is gone until someone re-issues it.
- **Preconditions.** Firmware 5.2.0 or later, an on-device-generated key eligible for attestation, successful authorization, and a certificate already stored for that key. Most applications that call attestation as a check do not expect a side effect.

## Specification

> "An attestation certificate can only be generated if the attested key has been generated on device." (Yubico, OpenPGP Attestation, "Implementation")

> "When an attestation statement is generated, it is placed in the OpenPGP cardholder certificate slot for the attested key. ... Any data currently stored in the cardholder certificate slot is overwritten." (Yubico, OpenPGP Attestation, "Implementation")

Source: [Yubico OpenPGP attestation](https://developers.yubico.com/PGP/Attestation.html), section "Implementation". The ellipsis omits the SIG example, which is quoted in full under "What is right".

> "Upon successful completion (SW 0x9000), the Attestation Statement is written to the corresponding Cardholder Certificate slot. The Attestation Statement can be retrieved via the normal GET DATA instruction to DO tag 0x7F21." (Yubico, OpenPGP Attestation, "Generate Attestation (Instruction)")

The instruction is CLA `0x80`, INS `0xFB`, with P1 `0x01` (SIG), `0x02` (DEC) or `0x03` (AUT). The SDK sends the same bytes ([Ins.cs:37](../../../src/OpenPgp/src/Ins.cs#L37) for INS `0xFB`; the key reference is passed as P1 at [OpenPgpSession.Keys.cs:156](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L156)).

The OpenPGP card specification does not define attestation, so Yubico's page is the governing document.

## Canonical Python reference

- **Python documents the side effect.** The `attest_key` docstring says: "The certificate is written to the certificate slot for the key, and its content is returned. Requires User PIN verification." ([yubikit/openpgp.py:1699-1713](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L1699-L1713)). Python does not add a backup or a warning in code.
- Python's behaviour is the same as the SDK's. The gap is the same in both: the side effect is documented in the docstring, and the SDK's docstring does not say it.

## Sibling SDKs (context only)

- The Android SDK's `attestKey` has the same documentation as Python: the certificate "is written to the certificate slot for the key" and "Requires User PIN verification" ([OpenPgpSession.java:1330-1336](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/openpgp/src/main/java/com/yubico/yubikit/openpgp/OpenPgpSession.java#L1330-L1336)).

## Reproduction

- Unit: none. A recorded response cannot show the device-side write. The existing AuditV2 tests do not cover attestation.
- Hardware: not run. It would overwrite the certificate on the test card. A dedicated test key with no certificate in the slot would be needed to test anything useful.

## Proposed fix

- **Recommendation (documentation only).**
  1. Add a warning to `IOpenPgpSession.AttestKeyAsync` ([IOpenPgpSession.cs:241-254](../../../src/OpenPgp/src/IOpenPgpSession.cs#L241-L254)): the attestation statement replaces any certificate in the cardholder certificate slot for that key, and the old certificate is not kept.
  2. Add the same warning, and the PIN requirement, to the OpenPGP usage documentation in [src/OpenPgp/README.md](../../../src/OpenPgp/README.md).
- **Options considered.**
  - Option 1 (recommended): documentation only, as above. No behaviour change.
  - Option 2: read the existing certificate with `GetCertificateAsync` before the attestation, and expose it to the caller. Useful, but not atomic: the device write still happens, and a failure between the two calls leaves the caller with a copy that may not be restorable. Do not promise a restore.
  - Option 3: require an explicit acknowledgement parameter (for example, `allowOverwrite: true`). Clear, but a breaking signature change for callers that already attest.
- **API impact.** None for Option 1. Option 3 is breaking.
- **Proving test.** A documentation review. A dedicated-key test is possible only on a card with no certificate in the slot.
- **Depends on / interacts with.** None. Independent of the other OpenPGP findings.
- **Open questions for the maintainer.** Documentation only now, or an acknowledgement parameter in the next breaking release?

## Check it yourself

No command. Compare the two documents:

- Yubico, [OpenPGP Attestation](https://developers.yubico.com/PGP/Attestation.html), sections "Implementation" and "Generate Attestation (Instruction)".
- [OpenPgpSession.Keys.cs:153-165](../../../src/OpenPgp/src/OpenPgpSession.Keys.cs#L153-L165) (the write and the read-back) and [IOpenPgpSession.cs:241-254](../../../src/OpenPgp/src/IOpenPgpSession.cs#L241-L254) (the public documentation, which has no warning).
- `attest_key` in `yubikit/openpgp.py` lines 1699-1713 in the Python reference.

# 24 Ed25519 PIV signing alters the caller's message (YESDK-1632)

| | |
| --- | --- |
| Audit severity | MED/HIGH |
| Our verdict | Confirmed. |
| Our severity | HIGH. The signature comes back with no error, but it does not verify against the message the caller passed. |
| Root cause | SDK only. ykman and yubikit-android pass the Ed25519 message unchanged. |
| Fix group | A (no-brainer). Pass the message through unchanged, and reject lengths above a measured cap. |
| Evidence | Unit (APDU content, two failing repros). Hardware (OpenSSL verification of the signature fails for a 10-byte message). Static. |
| Since the audit | `PivSession.cs` changed in `SignOrDecryptAsync`, but only the user-presence arguments. `PivCryptographicOperations.cs` (`PrepareDataForCrypto`) and `IPivSession.cs` are unchanged. The defect and the fix point are the same at current yubikit. |

## What the audit says

The audit says both `SignOrDecryptAsync` overloads change the caller's message before signing:

> "Both `SignOrDecryptAsync` overloads truncate messages longer than 32 bytes and append zeros to shorter messages before sending them to the YubiKey. This contradicts the API's promise to sign the supplied message directly."

Abbreviations: EdDSA is Edwards-curve digital signature algorithm. Ed25519 is the EdDSA curve variant used here. APDU is application protocol data unit. OpenSSL is the open-source crypto library used as an independent verifier in the hardware test. PIV is Personal Identity Verification, the smart-card application.

## What is right

- The code truncates or pads the message to 32 bytes:
  - Ed25519 expected length is 32 ([PivCryptographicOperations.cs#L404](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L404)).
  - Longer input is cut to its first 32 bytes ([L436-L440](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L436-L440)).
  - Shorter input is copied to the start of a 32-byte zero array, so zeros are appended ([L441-L447](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L441-L447)).
  - The result is sent as TLV `0x81` ([L106-L110](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L106-L110)).
- Both public overloads reach that code. The explicit-algorithm overload calls it through [PivSession.cs#L486-L503](../../../src/Piv/src/PivSession.cs#L486-L503), and the metadata overload through [PivSession.cs#L508-L543](../../../src/Piv/src/PivSession.cs#L508-L543). Both end at [PivSession.cs#L819-L829](../../../src/Piv/src/PivSession.cs#L819-L829).
- The API promise is in the XML doc: "EdDSA: Sign message directly" ([IPivSession.cs#L255](../../../src/Piv/src/IPivSession.cs#L255)).
- The unit repro failed in the recorded run. The APDU carries 38 bytes of data for both 10-byte and 64-byte messages. The repro expects 16 and 70 ([PivAuditReproTests.cs#L11-L36](../../../src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs#L11-L36)).
- Hardware: the signature for a 10-byte message fails OpenSSL's PureEd25519 verification against the slot public key ([hardware-results.md](../evidence/hardware-results.md), #24 section).

## What is wrong or imprecise

- The existing hardware test misses the bug. `SignOrDecryptAsync_Ed25519_ProducesSignature` signs `"test data"` (9 bytes, so it is padded). It asserts only the signature length ([PivCryptoTests.cs#L121](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs#L121)). Its own comment says it "verifies signature format only" ([L126-L128](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/PivCryptoTests.cs#L126-L128)). It passes while the signature is wrong.
- The hardware record names only the 10-byte case. The test loops over 10 and 64 bytes and stops at the first failed assertion, so the 64-byte case was not reached on hardware.
- Ed25519 is deterministic (RFC 8032 §8.2: "EdDSA signatures are deterministic."). Identical card inputs therefore give identical signatures. A message and its zero-padded form to 32 bytes produce the same signature. This follows from the code path. It was not tested on hardware.
- The audit says "Different long messages sharing their first 32 bytes produce identical signatures". The code path supports this, and the same reasoning applies. It was not tested on hardware.
- The audit does not mention a maximum message length. We did not find one in the Yubico PIV command pages or the technical manual. The real limit is unknown per firmware. It may depend on the APDU transport (short or extended APDUs). The SDK reports extended APDUs only on USB ([UsbSmartCardConnection.cs#L298-L299](../../../src/Core/src/Transports/SmartCard/UsbSmartCardConnection.cs#L298-L299)).

## Why it matters

- Who is affected: any application that signs with an Ed25519 PIV key through `SignOrDecryptAsync`. The technical manual lists Ed25519 (0xE0) for firmware 5.7.4 and 5.8.x.
- Impact: the caller gets a signature with no exception. It does not verify against the message the caller holds, so the failure appears later, at verification time.
- Security impact: the signature covers only the first 32 bytes (or the padded form). Bytes after 32 are not bound to the signature. Messages that differ only by trailing zeros get the same signature.
- Preconditions: none beyond calling the API with an Ed25519 key. No attacker is needed for the functional failure.

## Specification

- RFC 8032 §5.1.6 (Sign). Source: [RFC 8032](https://www.rfc-editor.org/rfc/rfc8032#section-5.1.6).
  > "The inputs to the signing procedure is the private key, a 32-octet string, and a message M of arbitrary size."

  The same section adds that Ed25519ph is a different scheme with a flag and an optional context:
  > "For Ed25519ctx and Ed25519ph, there is additionally a context C of at most 255 octets and a flag F, 0 for Ed25519ctx and 1 for Ed25519ph."

- SDK contract. The XML doc for `SignOrDecryptAsync` says "EdDSA: Sign message directly" ([IPivSession.cs#L255](../../../src/Piv/src/IPivSession.cs#L255)).
- YubiKey Technical Manual, supported algorithms. The row "Ed25519/x25519 (0xe0)" is marked as supported on firmware 5.8.x and 5.7.4. Source: [YubiKey Technical Manual](https://docs.yubico.com/hardware/yubikey/yk-tech-manual/yk5-apps-piv.html), "Supported Algorithms".

| Source | Clause | Requirement for an Ed25519 message |
| --- | --- | --- |
| RFC 8032 | §5.1.6 | Signs the message M as given, of arbitrary length |
| SDK | `IPivSession.cs` remark | Sign the message directly |
| Yubico docs checked | PIV commands, technical manual | No message-size limit documented. The cap is unknown per firmware |

## Canonical Python reference

Python is correct here.

- `_pad_message` returns Ed25519 messages unchanged ([yubikit/piv.py#L546-L548](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L546-L548)):
  ```python
  def _pad_message(key_type, message, hash_algorithm, padding):
      if key_type in (KEY_TYPE.ED25519, KEY_TYPE.X25519):
          return message
  ```
- `sign()` sends that value unchanged as TLV `0x81` ([yubikit/piv.py#L1159-L1160](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1159-L1160), [L1476-L1493](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1476-L1493)).
- Python sets no cap on message length. The device or the transport decides.

## Sibling SDKs (context only)

- yubikit-android `rawSignOrDecrypt` passes Ed25519 payloads unchanged ([PivSession.java#L399-L400](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java#L399-L400)).

## Reproduction

- Unit: [PivAuditReproTests.YESDK1632_Ed25519ChallengeContainsEntireMessage](../../../src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs#L11-L36), cases 10 and 64. It compares the whole APDU. **Recorded result: both cases fail.**
- Hardware: [PivAuditHardwareReproTests.YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages](../../../src/Piv/tests/Yubico.YubiKit.Piv.IntegrationTests/AuditV2/PivAuditHardwareReproTests.cs#L76-L94). It needs an `openssl` 3 binary on PATH. **Recorded result: the 10-byte signature fails OpenSSL's PureEd25519 verification** ("Signature Verification Failure"). The 64-byte case was not reached. The hardware record does not name the test key used.

## Proposed fix

- Recommendation (group A):
  1. For Ed25519, pass the caller's message to TLV `0x81` unchanged. Do not truncate or pad it. The branch belongs before the ECC digest sizing in `PrepareDataForCrypto` ([PivCryptographicOperations.cs#L394-L448](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L394-L448)).
  2. Reject messages longer than a documented maximum with `ArgumentException`. This matches the RSA rule in #26f.
  3. Set the maximum by measuring it on the 5.7.x and 5.8.x firmware. Until it is measured, document that the limit is device- and transport-dependent.
  4. Do not pre-hash the message. Ed25519ph is a different scheme (see Specification).
- Options considered:
  - (a) Pass through, and reject above a measured cap (recommended). Correct for every accepted length.
  - (b) Pass through, and reject above 255 bytes, a conservative short-APDU cap. Simple, but it refuses messages the USB transport could carry.
  - (c) Pre-hash. Rejected. It changes the algorithm and the signature.
- API impact: none for accepted lengths. Signatures that were returned for non-32-byte messages were wrong, and they will change. Oversize messages get a new `ArgumentException`.
- Proving test:
  - The two existing unit APDU repros. They should pass after the fix.
  - The hardware repro for both lengths, with OpenSSL.
  - A new rejection test for a length above the cap.
  - Strengthen `SignOrDecryptAsync_Ed25519_ProducesSignature` to verify the signature against the original message. It currently checks only the length.
- Depends on / interacts with: #26f. Both change `PrepareDataForCrypto`. Fix them together.
- Open questions:
  1. What is the exact cap per firmware, and does it depend on short or extended APDUs?
  2. Should oversize input be rejected with `ArgumentException` (as proposed) or with a new exception type?

## Check it yourself

- Unit repros (no hardware):
  ```bash
  dotnet toolchain.cs -- test --project Piv --filter "FullyQualifiedName~YESDK1632"
  ```
- Hardware repro (test keys only; it resets the PIV application and needs `openssl` on PATH):
  ```bash
  dotnet toolchain.cs -- test --integration --project Piv.IntegrationTests --filter "FullyQualifiedName~YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages"
  ```
- Spec: [RFC 8032 §5.1.6](https://www.rfc-editor.org/rfc/rfc8032#section-5.1.6).
- Python: [yubikit/piv.py#L546-L548](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L546-L548) and [L1135-L1160](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1135-L1160).

# 26f PIV input formatting alters short EC digests and truncates oversized RSA blocks (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (sub-item of #26; the audit lists #26 as one row) |
| Our verdict | Confirmed for EC right-padding and RSA truncation. The Ed25519 part is tracked under #24. |
| Our severity | MED |
| Root cause | SDK only. ykman does not right-pad. yubikit-android and yubikit-swift left-pad. |
| Fix group | B (small API decision): left-pad short EC digests, or reject them. Reject oversized RSA input in either case. |
| Evidence | Unit (two failing repros). Static (FIPS 186-5, Python, Android, Swift, legacy docs). No hardware for EC or RSA. |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). `PivCryptographicOperations.cs` is not in the diff. |

## What the audit says

The audit says the SDK changes caller input before the private-key operation. For ECDSA it adds trailing zeros, and for RSA it drops leading bytes:

> "Short digests are padded with **trailing zeros**, changing their numerical value. For example, a 32-byte SHA-256 digest used with P-384 becomes digest || 16 zero bytes. Integer-preserving padding would prepend zeros instead."

Abbreviations: ECDSA is elliptic curve digital signature algorithm. EC is elliptic curve. RSA is Rivest–Shamir–Adleman. FIPS is Federal Information Processing Standard. PKCS#1 is the Public-Key Cryptography Standards #1 RSA encoding. APDU is application protocol data unit. Ed25519 is the Edwards-curve signature variant covered by #24.

## What is right

- Short EC digests are padded at the end. The code allocates the curve length, copies the digest to offset 0, and returns it ([PivCryptographicOperations.cs#L441-L447](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L441-L447)). The comment on line 443 says the path "shouldn't normally happen". The padding itself is at [L444-L446](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L444-L446).
- Oversized RSA input loses its leading bytes. The code keeps the last `expectedLength` bytes ([PivCryptographicOperations.cs#L424-L428](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L424-L428)). No error is raised.
- Unit repros (recorded result: both fail):
  - [YESDK1634_ShortEcDigestIsLeftPaddedNotRightPadded](../../../src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs#L38-L52). For the two-byte digest `A1 B2`, the APDU carries `A1 B2` followed by 30 zero bytes. The test expects 30 zeros, then `A1 B2`.
  - [YESDK1634_OversizedRsaBlockIsRejectedRatherThanSilentlyTruncated](../../../src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs#L54-L65). A 129-byte block for RSA-1024 is accepted and no `ArgumentException` is thrown.
- Long EC digests are cut from the right, and that is correct. The code keeps the leftmost bytes ([L436-L440](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L436-L440)). FIPS 186-5 does the same for byte-aligned curves (see Specification).

## What is wrong or imprecise

- The bug is only in the short-digest branch. The audit does not flag long EC digests. FIPS keeps the leftmost bits of a long digest, as the SDK does. The audit already says integer-preserving padding would prepend zeros, which is the fix.
- The XML doc for RSA says "RSA: PKCS#1 v1.5 padding for both sign and decrypt" ([IPivSession.cs#L253](../../../src/Piv/src/IPivSession.cs#L253)). The SDK does not add PKCS#1 padding. It only resizes. The caller must supply a correctly formatted block. The legacy .NET SDK documents the same rule ([AuthenticateSignCommand.cs#L194-L200](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.YubiKey/src/Yubico/YubiKey/Piv/Commands/AuthenticateSignCommand.cs#L194-L200)). The SDK's wording reads as if it pads.
- The ECDSA contract says only "Sign hash directly (caller must hash data)" ([IPivSession.cs#L254](../../../src/Piv/src/IPivSession.cs#L254)). It does not say what happens to a digest of the wrong length.
- Python does not right-pad. Its EC branch intends to left-pad, but it uses `byte_len // 8` as the width ([yubikit/piv.py#L556-L558](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L556-L558)). For P-256 that width is 4, and for P-384 it is 6. So digests of 4 bytes or more pass through unchanged. That is a latent Python bug. It does not change the value sent, if the card reads the APDU data as a big-endian integer. We did not test that on hardware.
- The card's handling of a short digest is not documented. Yubico's docs say the digest "must be 256 bits (32 bytes) or shorter". They do not describe the padding. The claim that a shorter digest is read as a big-endian integer is an inference. It is not tested here.

## Why it matters

- Who is affected: applications that call `SignOrDecryptAsync` with an EC digest shorter than the curve size. Examples are a SHA-1 digest on P-256, or a truncated digest. Applications that pass an RSA block longer than the modulus are also affected.
- Impact, EC: the card signs a different number from the one the caller intended. Standard verification of the caller's digest fails. No exception is thrown.
- Impact, RSA: distinct oversized inputs that share their trailing bytes produce the same signing operation. The leading bytes are ignored.
- Preconditions: a non-canonical digest or block length from the caller. No attacker is needed for the functional failure. Any security impact depends on the caller relying on the digest-to-signature mapping.

## Specification

- FIPS 186-5 §6.4.1 (ECDSA Signature Generation Algorithm), Inputs item 3. Source: [FIPS 186-5](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.186-5.pdf).
  > "Approved hash function or XOF with output length of hashlen bits and a security design strength that is the same as or greater than the security strength of the key pair"

  A short input is therefore not an approved digest for this algorithm.

- FIPS 186-5 §6.4.1, Process steps 1-2. Our text extraction drops the ceiling brackets around `log2(n)`, so the quote uses an ellipsis there.
  > "2. Derive the integer e from H as follows: a. If len(n) ≥ hashlen, set E = H. Otherwise, set E equal to the leftmost ... bits of H. b. Convert the bit string E to the integer e as specified in Appendix B.2.1."

- FIPS 186-5, Appendix B.2.1 (Conversion of a Bit String to an Integer).
  > "Note that the first bit of a sequence corresponds to the most significant bit of the corresponding integer, and the last bit corresponds to the least significant bit."

  Read together, these mean that a digest is an integer with its first byte most significant. Padding on the left keeps the value. Padding on the right changes it. For a digest that is longer than the order, FIPS keeps the leftmost bits. That matches the SDK's truncation.

- Yubico PIV commands, Authenticate: sign. Source: [PIV commands](https://docs.yubico.com/yesdk/users-manual/application-piv/commands.html).
  > "For ECC signatures, simply provide the digest. No DER encoding, just the digest. If the key is EccP256, the digest must be 256 bits (32 bytes) or shorter."

- SDK contract. The ECDSA remark says "Sign hash directly (caller must hash data)" ([IPivSession.cs#L254](../../../src/Piv/src/IPivSession.cs#L254)).

## Canonical Python reference

Python does not have this issue. It does not right-pad, and it does not reject. There is a latent width bug, described above.

- EC branch of `_pad_message`, with the bug on the `rjust` line ([yubikit/piv.py#L549-L559](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L549-L559)):
  ```python
  byte_len = key_type.bit_len // 8
  if len(hashed) < byte_len:
      return hashed.rjust(byte_len // 8, b"\0")
  return hashed[:byte_len]
  ```
- `bit_len` for `ECCP256` is 256, so `byte_len` is 32, and `byte_len // 8` is 4 ([yubikit/piv.py#L122-L129](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L122-L129)).
- RSA branch: `_pad_message` builds the PKCS#1 block itself, from the message, using a dummy key. It never receives a raw short or long block ([yubikit/piv.py#L560-L567](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L560-L567)).
- So Python never right-pads. For EC, it passes digests of 4 bytes or more through unchanged, because of the width bug. It never rejects a short EC digest.

Every sibling that changes a short digest pads it on the left. None pads on the right.

## Sibling SDKs (context only)

- yubikit-android left-pads short EC and RSA inputs, truncates long EC digests, and rejects oversized RSA input ([PivSession.java#L399-L411](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java#L399-L411)). Its oversize rule is at [L405-L406](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java#L405-L406).
- yubikit-swift left-pads short ECDSA digests (`Data(count: keySize - hash.count) + hash`, [PIVDataFormatter.swift#L67-L74](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/PIV/PIVDataFormatter.swift#L67-L74)). A unit test checks the leading zeros ([PIVDataFormatterTests.swift#L119-L132](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/UnitTests/PIVDataFormatterTests.swift#L119-L132)).
- The legacy .NET SDK tells the caller to prepend `00` bytes to a short ECC digest ([AuthenticateSignCommand.cs#L223-L226](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.YubiKey/src/Yubico/YubiKey/Piv/Commands/AuthenticateSignCommand.cs#L223-L226)). The caller formats the data, and the SDK does not.

## Reproduction

- Unit: [YESDK1634_ShortEcDigestIsLeftPaddedNotRightPadded](../../../src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs#L38-L52) and [YESDK1634_OversizedRsaBlockIsRejectedRatherThanSilentlyTruncated](../../../src/Piv/tests/Yubico.YubiKit.Piv.UnitTests/AuditV2/PivAuditReproTests.cs#L54-L65). **Recorded result: both fail.** The EC repro saw `[161, 178, 0, ...]` instead of the left-padded form. The RSA repro saw no exception.
- Hardware: not run for EC or RSA. The Ed25519 hardware result is under #24.

## Proposed fix

- Recommendation (group B). Left-pad a short EC digest to the curve's byte length, in `PrepareDataForCrypto` ([PivCryptographicOperations.cs#L441-L447](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L441-L447)). Reject an RSA block longer than the modulus with `ArgumentException`, before any APDU is sent. Keep the leftmost truncation for long EC digests. Keep the left-padding for short RSA blocks.
- Options considered:
  - (a) Left-pad short EC digests (recommended). It keeps the integer value. It matches FIPS integer semantics, yubikit-android, yubikit-swift, and the legacy docs. It still accepts short digests that FIPS does not approve, but they are encoded correctly.
  - (b) If choosing stricter handling, reject short digests while retaining specified truncation of longer digests. Exact curve-byte length is an additional API restriction, not a FIPS requirement. FIPS takes the leftmost bits of a longer digest, so SHA-512 with P-256 is valid input. This option breaks callers that pass SHA-1 to P-256 or other short digests, unless a new explicit option allows them.
  - (c) Keep right-padding. Rejected. It changes the value.
- API impact: with (a), inputs that were encoded wrongly now produce correct signatures. With (b), short EC digests throw `ArgumentException`. For both, oversized RSA input now throws `ArgumentException` instead of being truncated.
- Proving test:
  - The two existing unit repros, which should pass after the fix.
  - Add a case for a 20-byte SHA-1 digest on P-256. The APDU should carry 12 zero bytes, then the digest.
  - Add a case for a 129-byte block on RSA-1024 that is rejected before any APDU.
- Depends on / interacts with: #24. Both change `PrepareDataForCrypto`. Fix them together.
- Open questions:
  1. Accept short EC digests with left-padding (a), or reject short EC digests while keeping truncation of longer ones (b)? The verification report recommends (a).
  2. Should the card's behaviour with short digests be measured on hardware before the fix? It would confirm the integer reading, but the left-padding fix does not depend on it.

## Check it yourself

- Unit repros (no hardware):
  ```bash
  dotnet toolchain.cs -- test --project Piv --filter "FullyQualifiedName~YESDK1634"
  ```
- Spec: FIPS 186-5 §6.4.1 (steps 1-2 and Inputs item 3) and Appendix B.2.1 in the [FIPS 186-5 PDF](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.186-5.pdf).
- Python: [yubikit/piv.py#L546-L567](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L546-L567).
- SDK: [PivCryptographicOperations.cs#L394-L448](../../../src/Piv/src/Cryptography/PivCryptographicOperations.cs#L394-L448).

# 27 Mismatched public and private key components are accepted (YESDK-1635)

| | |
| --- | --- |
| Audit severity | LOW/MED |
| Our verdict | Confirmed, for both EC and RSA PKCS#8 import. |
| Our severity | MED. This is a trust-boundary failure. The wrapper reports a public key that does not belong to the private key. It does not expose the private key. |
| Root cause | SDK only. Python's cryptography loader rejects these inputs (tested with cryptography 50.0.2). |
| Fix group | C (design). The EC check has to avoid the D-only runtime import flagged in #17. The number of RSA checks is a decision. |
| Evidence | Unit (two failing repros). Static (decoders, RFCs). Local experiment with cryptography, on locally generated keys. No hardware. |
| Since the audit | Unchanged at current yubikit (`df1ec06d`). The decoders, the wrappers, and the PIV key encoder are not in the diff from `fdfcd6fd`. |

## What the audit says

The audit says the SDK reports a public key without checking that it belongs to the private key:

> "the SDK tells the application, that B is the public half of private A, without checking that this is true."

Abbreviations: EC is elliptic curve. RSA is Rivest–Shamir–Adleman. PKCS#8 is the Public-Key Cryptography Standards #8 private-key format. D is the EC private scalar. Q is the EC public point. n, p, q are the RSA modulus and its primes. dP, dQ, qInv are the RSA CRT (Chinese remainder theorem) parameters. PIV is Personal Identity Verification.

## What is right

- The EC decoder checks only the point's format. It requires the `0x04` prefix and the exact length ([AsnUtilities.cs#L60-L75](../../../src/Core/src/Cryptography/AsnUtilities.cs#L60-L75)). The format check runs at [AsnPrivateKeyDecoder.cs#L258](../../../src/Core/src/Cryptography/AsnPrivateKeyDecoder.cs#L258). The point is then copied into the result without any check against D ([AsnPrivateKeyDecoder.cs#L280-L285](../../../src/Core/src/Cryptography/AsnPrivateKeyDecoder.cs#L280-L285)). The format check does not test that the point is on the curve.
- The RSA decoder reads n, e, d, p, q, dP, dQ and qInv ([AsnPrivateKeyDecoder.cs#L338-L345](../../../src/Core/src/Cryptography/AsnPrivateKeyDecoder.cs#L338-L345)). Normalization only pads widths ([RSAParametersExtensions.cs#L56-L90](../../../src/Core/src/Cryptography/RSAParametersExtensions.cs#L56-L90)). Nothing checks n = p·q or the CRT relations. The `RSAPrivateKey` constructor validates nothing beyond the normalization ([RSAPrivateKey.cs#L54-L62](../../../src/Core/src/Cryptography/RSAPrivateKey.cs#L54-L62)).
- The two repro tests failed in the recorded run: [YESDK1635_RsaPkcs8RejectsModulusUnrelatedToPrivatePrimes](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreAuditReproTests.cs#L13-L69) and [YESDK1635_EcPkcs8RejectsPublicPointFromDifferentPrivateScalar](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreAuditReproTests.cs#L71-L117). Both report "No exception was thrown".
- The PIV import sends only D. The EC encoder uses `parameters.D` and nothing else ([PivKeyProtocol.cs#L279-L293](../../../src/Piv/src/Keys/PivKeyProtocol.cs#L279-L293)). The card derives its own public key. So the audit's sequence is right: the card keeps private A and derives public A, while the SDK reports public B.

## What is wrong or imprecise

- The verification report says yubikey-manager imports private values "without checking supplied public Q". That is true of yubikit's `put_key`. It is not true of the ykman CLI. The CLI loads the key with cryptography before it calls `put_key`, and cryptography rejects both mismatches. Python validates on load. The SDK does not. See [Canonical Python reference](#canonical-python-reference).
- RFC 8017 Appendix A.1.2 defines dP = d mod (p−1). Together with §3.2's relation for e and d, this satisfies §3.2's CRT-exponent relation. These are consistent checks, not alternative definitions. §3.2 requires e·d ≡ 1 (mod λ(n)), and §3.1 defines λ(n) = LCM(p−1, q−1) for two primes. Since p−1 divides λ(n), e·(d mod (p−1)) ≡ 1 (mod p−1). Cryptography accepts `d mod (p − 1)` (see Canonical Python reference).
- Platform behaviour is not established by us. [dotnet/runtime#27830](https://github.com/dotnet/runtime/issues/27830) records that OpenSSL rejects a mismatched Q, while Windows accepted it and re-derived Q from D. The issue is closed as completed (2025). We did not test current behaviour on macOS or Windows. The verification report says the same.
- The verification report's review correction is right. A portable check of Q = D·G that uses a D-only runtime import reintroduces the #17 path for every EC PKCS#8 import.

## Why it matters

- Who is affected: any application that loads a PKCS#8 EC or RSA private key with the SDK's decoders (`CreateFromPkcs8`), and then uses the wrapper's public parameters. Examples are registering the key, building a certificate, or verifying with it.
- Preconditions: the PKCS#8 bytes are wrong or come from an untrusted source, such as a corrupted file or a file from another party.
- Impact: the application can register, certify, or trust a public key that does not match the private key. The private key is not exposed. Malformed input is accepted without any error.
- PIV import: the card derives its own public key from the imported D. The SDK may report a different one. An application that registers the SDK's public key then fails to verify the card's signatures.

## Specification

- RFC 5915 §3 (ECPrivateKey). Source: [RFC 5915 §3](https://www.rfc-editor.org/rfc/rfc5915#section-3).
  > "publicKey contains the elliptic curve public key associated with the private key in question."

  > "Given the private key and the parameters, the public key can always be recomputed; this field exists as a convenience to the consumer."

- RFC 5915 §2. Source: [RFC 5915 §2](https://www.rfc-editor.org/rfc/rfc5915#section-2).
  > "When an EC public key is included in the distributed PrivateKeyInfo, the publicKey field in ECPrivateKey is used."

  Read together: the field must hold the public key of the private key. Recomputing it from D is always possible. The RFC does not tell a consumer to check it.

- RFC 8017 §3.2 (RSA Private Key). Source: [RFC 8017 §3.2](https://www.rfc-editor.org/rfc/rfc8017#section-3.2).
  > "In a valid RSA private key with the first representation, the RSA modulus n is the same as in the corresponding RSA public key and is the product of u distinct odd primes r_i, i = 1, 2, ..., u, where u >= 2."

  > "...the CRT coefficient qInv is a positive integer less than p satisfying q * qInv == 1 (mod p)."

  > "The RSA private exponent d is a positive integer less than n satisfying e * d == 1 (mod \lambda(n))," with §3.1 defining "\lambda(n) = LCM(r_1 - 1, ..., r_u - 1)."

- RFC 8017 Appendix A.1.2 (RSA Private Key Syntax). Source: [RFC 8017 Appendix A.1.2](https://www.rfc-editor.org/rfc/rfc8017#appendix-A.1.2).
  > "exponent1 is d mod (p - 1)."

  > "coefficient is the CRT coefficient q^(-1) mod p."

- SDK contract. None of the decoders' XML docs promises that the public parameters match the private key. The audit's contract argument rests on the wrapper's public properties.

## Canonical Python reference

Python is correct here, through cryptography.

- yubikit's `put_key` does not compare the public key with D. It derives the key type from `private_key.public_key()` ([yubikit/piv.py#L1340](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1340)). It sends the RSA CRT values ([L1343-L1355](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1343-L1355)) or the EC private value only ([L1366](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1366)).
- The ykman CLI loads the key first, with cryptography's PEM and DER loaders ([ykman/util.py#L66-L106](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/util.py#L66-L106)). Then it calls `put_key` ([ykman/_cli/piv.py#L752-L774](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/piv.py#L752-L774)). The check happens in the loader.
- Local experiment, not in the repository. We built PKCS#8 keys by hand, with the RFC 5915 layout inside a PKCS#8 wrapper. Then we loaded them with cryptography 50.0.2, the version in the ykman 5.9.2 Python environment:
  - EC P-256, D from key A and the public point from key B: `ValueError: Invalid key`.
  - EC P-256, D + 1 with Q from key A: `ValueError: Invalid key`.
  - RSA, modulus from key B and primes from key A: `ValueError: Invalid private key`.
  - RSA, dP one greater than d mod (p − 1): `ValueError: Invalid private key`.
  - Controls with matching keys load. The control with `dmp1 = d mod (p − 1)` loads.
  - Other cryptography versions were not tested.

  The loader call is:
  ```python
  from cryptography.hazmat.primitives import serialization
  # der: PKCS#8 EC key whose D belongs to key A and whose public point belongs to key B
  serialization.load_der_private_key(der, password=None)
  # raises ValueError: Invalid key
  ```

## Sibling SDKs (context only)

- yubikit-android's `putKey` takes private values only, through `PrivateKeyValues`. The import has no public component to check ([PivSession.java#L1164-L1205](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/piv/src/main/java/com/yubico/yubikit/piv/PivSession.java#L1164-L1205)).

## Reproduction

- Unit: [YESDK1635_RsaPkcs8RejectsModulusUnrelatedToPrivatePrimes](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreAuditReproTests.cs#L13-L69) and [YESDK1635_EcPkcs8RejectsPublicPointFromDifferentPrivateScalar](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreAuditReproTests.cs#L71-L117). **Recorded result: both fail with "No exception was thrown".**
- Hardware: not applicable. The keys are generated locally.
- Local experiment: see [Canonical Python reference](#canonical-python-reference).

## Proposed fix

- Recommendation (group C). Validate in the decoder, before any wrapper is created, for both key types.
  1. RSA, with managed arithmetic and no runtime import. Check that n = p·q, dP = d mod (p−1), dQ = d mod (q−1), qInv·q ≡ 1 (mod p), and e·d ≡ 1 (mod lcm(p−1, q−1)). Reject failures with `CryptographicException`.
  2. EC. Check that the embedded Q equals D·G on the named curve. The options below say how.
  3. Keep the existing format checks. If option (b) is not used, add an on-curve check for Q.
- Options considered, EC:
  - (a) Managed scalar multiplication. Compute D·G in managed code and compare. Pros: no runtime involvement. Cons: a new cryptographic implementation. It must be constant time, because D is secret. High review and maintenance cost. Not recommended.
  - (b) Runtime sign-and-verify probe (recommended, pending tests). Import D with the embedded Q, sign a fixed digest, then verify that signature with a public-only key built from the same Q. Reject on any exception or failed verification. The signature is made with D, so it verifies only if Q = D·G. This never imports a D-only key, so it does not extend the #17 path. Cons: it depends on the runtime signing with D and not with Q, and on each platform's behaviour, which is not yet tested on macOS, Windows, or Linux.
  - (c) Derive Q from D with `ECDsa.Create` on a D-only parameter set. Pros: simplest. Cons: this is the path flagged in #17, and it would extend that residual to every EC PKCS#8 import. Not recommended.
  - (d) Accept and document. Not acceptable. It leaves the trust boundary open.
- Options considered, RSA:
  - (i) All the checks listed above (recommended).
  - (ii) Minimum checks, n = p·q only. Weaker. A key with a wrong dP would still load.
- API impact: malformed keys that were accepted now throw `CryptographicException`. This is an intended tightening. Valid keys and signatures do not change.
- Proving test:
  - The two existing repros, which should pass after the fix.
  - Negative vectors: dP off by one; qInv wrong; Q from another scalar; n from another key; a point that is not on the curve (new).
  - Positive controls: the existing decoder tests and `PrivateKeyDecodingZeroingTests` must still pass.
- Depends on / interacts with: #17. Option (c) extends the residual, and option (b) avoids the D-only import.
- Open questions:
  1. For RSA: all checks (recommended), or the minimum?
  2. For EC: accept option (b), subject to tests on macOS, Windows, and Linux?
  3. Should caller-supplied `CreateFromParameters()` input get the same validation, or only the PKCS#8 decoder? The caller controls that input, so the answer changes the contract.

## Check it yourself

- Unit repros (no hardware; both failed in the recorded run):
  ```bash
  dotnet toolchain.cs -- test --project Core --filter "FullyQualifiedName~YESDK1635"
  ```
- Read the RFC text: [RFC 5915 §2-§3](https://www.rfc-editor.org/rfc/rfc5915#section-3), [RFC 8017 §3.2](https://www.rfc-editor.org/rfc/rfc8017#section-3.2), and [RFC 8017 Appendix A.1.2](https://www.rfc-editor.org/rfc/rfc8017#appendix-A.1.2).
- Read the Python path: [ykman/util.py#L66-L106](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/util.py#L66-L106) and [yubikit/piv.py#L1323-L1372](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/piv.py#L1323-L1372).
- Local experiment: build a PKCS#8 EC key whose D and public point come from different keys, then call cryptography's `load_der_private_key`. Record the cryptography version. The same approach works for RSA with a modulus from a different key.

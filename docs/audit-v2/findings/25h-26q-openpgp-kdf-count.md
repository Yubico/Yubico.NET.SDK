# 25h and 26q OpenPGP KDF iteration count handling (YESDK-1633 and YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (25h, audit row 25); MED (26q, audit row 26) |
| Our verdict | Confirmed (both) |
| Our severity | MED (both) |
| Root cause | SDK and Python share it (short counts). The sign wrap of counts from 0x80000000 is SDK only. |
| Fix group | C (real design decision: compatibility with cards already provisioned) |
| Evidence | unit test; Python reference run; specification. No hardware run. |
| Since the audit | `src/OpenPgp/src/Kdf.cs` is unchanged at current yubikit. The fix sites are unchanged. |

## What the audit says

The KDF (key derivation function) setting on an OpenPGP card holds an iteration count. The SDK reads it as a signed 32-bit integer, so large values become negative. The hashing loop also hashes only as many bytes as the count, so a short count can leave the PIN out of the derived value.

> "`KdfIterSaltedS2k.Process()` concatenates the salt and PIN. `DoProcess()` then hashes exactly `IterationCount` bytes, even when that count is too small to include the complete PIN."

Abbreviations: KDF is key derivation function. OpenPGP is the message format and card application. SDK is software development kit. PIN is personal identification number. S2K is string-to-key, the derivation method. DO is data object, the card's tagged storage item. RFC is Request for Comments. GET DATA and VERIFY are card commands.

## What is right

- **Short counts drop PIN bytes.** `DoProcess` splits the count into whole copies of the input and a trailing part ([Kdf.cs:238](../../../src/OpenPgp/src/Kdf.cs#L238)). A count below the input length gives zero whole copies. Only `data[..count]` is hashed ([Kdf.cs:252-255](../../../src/OpenPgp/src/Kdf.cs#L252-L255)). With an 8-byte salt and a 6-digit PIN:
  - count 8 hashes only the salt;
  - count 9 hashes the salt and the first PIN byte.
  - Unit test `YESDK1633_ShortKdfCountStillHashesCompletePin` (counts 8 and 9) fails today with "Collections differ".
- **The sign wrap.** `ParseData` casts the unsigned count with `(int)` ([Kdf.cs:323](../../../src/OpenPgp/src/Kdf.cs#L323)). A count of `0x80000000` becomes `int.MinValue`. `Math.DivRem` then gives a negative quotient, the loop does nothing, and `trailing > 0` is false ([Kdf.cs:247-255](../../../src/OpenPgp/src/Kdf.cs#L247-L255)). By reading the code, the output is SHA-256 of the empty input, whatever the PIN. Unit test `YESDK1633_UnsignedKdfCountDoesNotWrapNegative` fails today with "No exception was thrown". This was not run to get the output bytes.
- **The Python reference confirms the short-count rule.** The Python `_do_process` gives the same result. Run for this document with the local yubikey-manager clone at the pinned commit: count 8 gives SHA-256 of the salt; count 9 gives SHA-256 of the salt and the first PIN byte; count 14 (one full copy) gives SHA-256 of salt plus PIN.

## What is wrong or imprecise

- **25h is a sign wrap, not truncation.** The audit's title for 25h says the count "truncation" removes PIN input. For counts from `0x80000000` upward, nothing is hashed at all, so the derived value does not depend on the PIN. The short-count rule (26q) is the separate truncation.
- **The XML documentation cites the older RFC 4880, which contains the same minimum-input rule. Its algorithm description incorrectly omits that rule.** [Kdf.cs:103](../../../src/OpenPgp/src/Kdf.cs#L103) cites "RFC 4880 §3.7.1.3". RFC 4880 states the rule: "The one exception is that if the octet count is less than the size of the salt plus passphrase, the full salt plus passphrase will be hashed even though that is greater than the octet count." (RFC 4880, section 3.7.1.3, [rfc4880.txt](https://www.rfc-editor.org/rfc/rfc4880.txt)). RFC 9580 obsoletes RFC 4880 (its header lists "Obsoletes: 4880"), and section 3.7.1.3 is the same section in RFC 9580. The OpenPGP card specification 3.4.1 also names RFC 4880 for the S2K function (section 4.3.2). The remarks at [Kdf.cs:107-119](../../../src/OpenPgp/src/Kdf.cs#L107-L119) describe the divmod algorithm without the rule, so the documentation describes the non-conforming behaviour.
- **The count has a different encoding on the card than in RFC 9580.** RFC 9580 codes the count in one octet with a formula. The card's KDF-DO carries a 4-byte field named "Iteration count (long integer)", which the SDK and yubikey-manager both treat as a byte count. The RFC's coded-count formula does not apply to the card directly. The truncation rule is the part that applies, and the card specification does not restate it.
- **The card does not evaluate the KDF.** The card spec says the KDF-DO "is not evaluated by any card command, the functionality is handled completely by the terminal application." The stored PIN value is whatever the terminal wrote when it provisioned the card. This is why the compatibility question below exists.
- **Python does not wrap, but it is not safe either.** Python reads the count as unsigned, so `0x80000000` means about 2^31 bytes of hashing. Not timed.

## Why it matters

- **Who controls the input.** The KDF-DO is read from the card with GET DATA (tag `F9`). The card, or a device that presents itself as one, supplies the count. The card already controls its own VERIFY result, so the security effect is small. The effects that matter are on the user's PIN and on the host.
- **Short counts.** The value sent to VERIFY no longer depends on every PIN byte. For a count of 9, PINs that share a first digit produce the same value. The check becomes weaker than the card intends.
- **Compatibility, which is the real issue.** The terminal's derived value must match what the card stored at provisioning. A card provisioned with an RFC-conforming tool and a short count fails VERIFY today, and each failed VERIFY uses a retry. A card provisioned with the SDK rule, or with ykman, passes today and would fail after the fix. The KDF-DO has no marker for which rule was used, so the SDK cannot tell the two apart. Typical counts are far above salt plus PIN length: the yubikey-manager default is `0x780000` ([openpgp.py:771](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L771)), and the card spec's example is `0x000186A0` (100 000). Short counts are therefore rare. The SDK cannot count how many exist.
- **Host work.** A count up to `0x7FFFFFFF` forces up to about 2 GiB of SHA-256 input for each PIN operation. For a 14-byte input that is roughly 1.5 × 10^8 update calls. Not measured.

## Specification

> "The input is truncated to the octet count, except if the octet count is less than the initial size of the salt plus passphrase. That is, at least one copy of the full salt plus passphrase will be provided as input to each hash context regardless of the octet count." (RFC 9580, section 3.7.1.3)

Source: [RFC 9580, section 3.7.1.3](https://www.rfc-editor.org/rfc/rfc9580.html#section-3.7.1.3).

> "The KDF-DO has the following format and shall be evaluated by the terminal software, if present" (OpenPGP card specification 3.4.1, section 4.3.2). Tag `83 04` holds the "Iteration count (long integer)".

> "The content of the KDF-DO is not evaluated by any card command, the functionality is handled completely by the terminal application." (OpenPGP card specification 3.4.1, section 4.3.2)

Source: [OpenPGP card application 3.4.1](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf), section 4.3.2 (pages 18-19 of the PDF).

| Clause | RFC 9580, section 3.7.1.3 | OpenPGP card 3.4.1, section 4.3.2 | Notes |
| --- | --- | --- | --- |
| Count encoding | 1-octet coded value with a formula | 4-byte long integer (tag 83) | Different encodings; the truncation rule still applies by analogy |
| Input shorter than salt plus PIN | At least one full copy | Not restated; the terminal computes the value | The SDK violates the RFC rule |
| Who computes | Implementation | Terminal application | The card compares the value it stored |

## Canonical Python reference

- Python has the same short-count behaviour. This is a candidate for the divergence ledger. `_do_process` divides by the input length and hashes the trailing bytes, as the SDK does ([openpgp.py:756-765](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L756-L765)). The comment there reads: "Although the field is called "iteration count", it's actually the number of bytes to be passed to the hash function, which is called only once. Go figure!"
- Python reads the count as unsigned ([openpgp.py:793](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/openpgp.py#L793)), so it does not wrap. That part is correct in Python.
- Deliberate? The byte-count meaning is deliberate. The short-count rule is not discussed. It came in with KDF PIN support in commit `4bea80dc` (2020, "opgp: add support for KDF PINs", resolves #279). The commit message does not mention counts shorter than salt plus PIN.

## Sibling SDKs (context only)

- Android has the same divmod in `Kdf.doProcess` ([Kdf.java:152-162](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/openpgp/src/main/java/com/yubico/yubikit/openpgp/Kdf.java#L152-L162)). Its parser reads the count with `BigInteger.intValue()` ([Kdf.java:131](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/openpgp/src/main/java/com/yubico/yubikit/openpgp/Kdf.java#L131)), so it wraps the same way. What happens next was not traced.
- No OpenPGP KDF implementation was found in the Swift SDK.

## Reproduction

- Unit: [YESDK1633_ShortKdfCountStillHashesCompletePin](../../../src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs#L30-L54), counts 8 and 9. Asserts that the digest equals SHA-256 of salt plus PIN. **Result today (2026-10-10): both fail, "Collections differ".**
- Unit: [YESDK1633_UnsignedKdfCountDoesNotWrapNegative](../../../src/OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/AuditV2/OpenPgpAuditReproTests.cs#L56-L67). Asserts that `Process` rejects `0x80000000` with `ArgumentException`. **Result today: fails, "No exception was thrown".**
- Hardware: not run. Provisioning a card with a different KDF count would change the PIN on the test key.

## Proposed fix

- **Recommendation.**
  1. Decode the count as unsigned in `ParseData` ([Kdf.cs:319-323](../../../src/OpenPgp/src/Kdf.cs#L319-L323)) and reject counts above a documented maximum. The maximum is a maintainer decision. The yubikey-manager default (`0x780000`) is a reference point: a bound with clear headroom above it is reasonable.
  2. In `DoProcess` ([Kdf.cs:236-258](../../../src/OpenPgp/src/Kdf.cs#L236-L258)), hash at least one full input: use `Math.Max(iterationCount, data.Length)` before the divmod. Counts below the input length then hash the full input once. Counts of 0 are covered by the same change.
  3. Correct the XML remarks ([Kdf.cs:102-121](../../../src/OpenPgp/src/Kdf.cs#L102-L121)) to cite RFC 9580 section 3.7.1.3 and to state the rule.
- **Options for cards already provisioned with short counts** (the open decision):
  - Option A (recommended in the verification report): follow the RFC, document the change, and warn that cards with short counts may need their PIN set again. Cards provisioned with the old rule stop verifying after the fix.
  - Option B: keep the old rule behind an explicit session option and default to the RFC rule. Cards provisioned with the old short-count rule need the legacy option; cards provisioned with a conforming tool use the default.
  - Option C: throw a specific exception before VERIFY when the count is shorter than salt plus PIN. No retry is used up, and no value is silently wrong. Both populations are blocked until the caller decides.
- **API impact.** `IterationCount` stays an `int`, but parsing becomes checked. Derived output changes for counts below the input length. Counts above the new bound are rejected. The existing test expects `ArgumentException`; the exception type for an out-of-range count should be chosen to match.
- **Proving tests.** The two existing tests. Add: a count equal to the input length; a count of 0; the largest accepted count and the first rejected count.
- **Depends on / interacts with.** 25h and 26q share the fix. `Kdf.Process` is used by the OpenPGP PIN path, which needs no other change.
- **Open questions for the maintainer.** The maximum count. Option A, B or C. Whether to log a warning when a short count is seen.

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project OpenPgp --filter "FullyQualifiedName~YESDK1633"
```

Read in this order:

- [Kdf.cs:236-258](../../../src/OpenPgp/src/Kdf.cs#L236-L258) (`DoProcess`) and [Kdf.cs:319-323](../../../src/OpenPgp/src/Kdf.cs#L319-L323) (`ParseData`).
- RFC 9580 section 3.7.1.3, and card specification 3.4.1 section 4.3.2 (page 18 of the PDF).
- `yubikit/openpgp.py` lines 756-765 and 789-807 in the Python reference.

To run the Python reference on a short count, install `cryptography` in a virtual environment and run `KdfIterSaltedS2k._do_process(HASH_ALGORITHM.SHA256, 8, salt + pin)` with the pinned clone on `PYTHONPATH`. Compare the output with `hashlib.sha256(salt)`.

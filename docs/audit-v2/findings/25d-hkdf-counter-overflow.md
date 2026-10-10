# 25d HKDF block counter wraps at the maximum output length (YESDK-1633)

| | |
| --- | --- |
| Audit severity | MED (item 25d of the MED row 25) |
| Our verdict | Confirmed, with a correction. The length limit is already enforced. The defect is a counter that wraps for valid lengths 8129 to 8160. |
| Our severity | INFO-LOW |
| Root cause | Replace with runtime API (`System.Security.Cryptography.HKDF.DeriveKey`) |
| Fix group | A (no-brainer) |
| Evidence | unit test (run today, compared with the .NET HKDF output), static (RFC 5869, .NET documentation) |
| Since the audit | `HkdfUtilities.cs` is identical at `df1ec06d`. The caller, `ArkgPrimitivesOpenSsl.cs`, changed only in its P/Invoke declarations. The two HKDF call sites are still at lines 259 and 279. |

Abbreviations used in this file: API is the application programming interface. HKDF is the HMAC-based key derivation function (RFC 5869). HMAC is the hash-based message authentication code. P-256 is the NIST elliptic curve with 256-bit coordinates. RFC is Request for Comments. SDK is the software development kit. ykman is the Yubico command-line tool, written in Python.

## What the audit says

The audit says that the HKDF block counter overflows and produces a negative output offset.

> "HKDF block-counter overflow produces a negative output offset"

## What is right

- The block counter is a `byte`. The loop is `for (byte index = 1; index <= numberOfBlocks; index++)` ([HkdfUtilities.cs:69](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L69)). For 255 blocks, which is every output length from 8129 to 8160, the counter reaches 255. The next increment wraps it to 0, and the loop body runs once more with index 0.
- The block offset is then `(index - 1) * 32 = -32` ([HkdfUtilities.cs:85](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L85)). The copy into the output span fails with `ArgumentOutOfRangeException` ([HkdfUtilities.cs:89](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L89)).
- The repro shows it. Repro: [CoreNumericBoundaryAuditReproTests.YESDK1633_HkdfMaximumRfcLength_MatchesBclHkdf](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L83-L96), lengths 8129 and 8160. Result today: failed in both cases with `ArgumentOutOfRangeException`.

## What is wrong or imprecise

- **Correction to the source record.** The verification report says the audit claims the RFC 5869 maximum is not enforced, and that the claim is wrong. The audit row does not make that claim. Its item for HKDF names only the counter overflow and lines 69 and 85 ([original audit, row 25](../original-audit.md)). Either way, the check exists. `if (length > 255 * Sha256HashByteLength)` rejects lengths above 8160 ([HkdfUtilities.cs:45-48](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L45-L48)). The guard test passes today: [CoreNumericBoundaryAuditReproTests.YESDK1633_HkdfAboveRfcMaximum_Rejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L98-L103).
- The defect does not return wrong key material. It throws for valid input in the 8129 to 8160 range.
- No current caller reaches it. The two callers request 32 bytes ([ArkgPrimitivesOpenSsl.cs:259](../../../src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs#L259)) and the length of the P-256 shared secret, which is 32 bytes ([ArkgPrimitivesOpenSsl.cs:279](../../../src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs#L279); the secret is allocated at [line 140](../../../src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs#L140)).

## Why it matters

- **Who can trigger it:** nobody in the current code. A future caller that requests a length from 8129 to 8160 gets an exception instead of key material.
- **Severity:** INFO-LOW. The class is internal, and the failure is loud.

## Specification

RFC 5869, section 2.3 (Step 2: Expand):

> "L length of output keying material in octets (<= 255*HashLen)"

> "N = ceil(L/HashLen)"

> "(where the constant concatenated to the end of each T(n) is a single octet.)"

Source: [RFC 5869](https://www.rfc-editor.org/rfc/rfc5869), section 2.3.

RFC 5869, section 2.2 (Step 1: Extract) sets the salt default:

> "if not provided, it is set to a string of HashLen zeros."

The .NET documentation for the array overload of `HKDF.DeriveKey` says the same thing for the salt:

> "If not provided, it defaults to a byte array of the same length as the output of the specified hash algorithm."

Source: [Microsoft Learn, HKDF.DeriveKey](https://learn.microsoft.com/dotnet/api/system.security.cryptography.hkdf.derivekey), checked today. The span overload's section has no such sentence, so we checked its behaviour instead (see Proposed fix). For SHA-256 the default is 32 zero bytes, which is what `HkdfExtract` uses ([HkdfUtilities.cs:56](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L56)).

## Canonical Python reference

**Python has no equivalent.** yubikit and ykman contain no HKDF implementation. Our comparison reference is the .NET implementation, which the test uses.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CoreNumericBoundaryAuditReproTests.YESDK1633_HkdfMaximumRfcLength_MatchesBclHkdf](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L83-L96), cases 8129 and 8160. Compares the output with `HKDF.DeriveKey`. Result today: failed, `ArgumentOutOfRangeException` in both cases.
- Unit: [CoreNumericBoundaryAuditReproTests.YESDK1633_HkdfAboveRfcMaximum_Rejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L98-L103), guard. Result today: passed.
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** Replace the hand-written extract and expand code in `HkdfUtilities` ([HkdfUtilities.cs:54-95](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L54-L95)). Allocate `byte[length]`, call `HKDF.DeriveKey(HashAlgorithmName.SHA256, inputKeyMaterial, output.AsSpan(), salt, contextInfo)`, and return the output. That is the span overload, whose documented signature takes an output span. The array overload takes a `byte[]` input key and an integer length, but no span input. No overload takes spans together with an integer length. In both overloads, an empty salt gives HashLen zero bytes. We checked this against RFC 5869 test case 3 (empty salt and info), and at 8129 bytes the span overload matches the array overload with a 32-byte zero salt. Or inline the runtime call at the two call sites ([ArkgPrimitivesOpenSsl.cs:259, 279](../../../src/Core/src/Cryptography/ArkgPrimitivesOpenSsl.cs#L259)) and delete `HkdfUtilities.cs`. The target framework is net10.0 ([Directory.Build.props:35](../../../Directory.Build.props#L35)), so the API is available.
- **Options considered.**
  - **(a) Runtime HKDF (recommended).** The root cause is a duplicate of a runtime API, so the fix is to remove the copy.
  - **(b) Minimal fix.** Make the counter an `int`. Smaller diff, but it keeps the hand-written code.
- **Open point, not in the audit.** The current code does not clear its intermediate secret buffers. These are the pseudorandom key ([HkdfUtilities.cs:50](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L50)), the HMAC input and the block output ([lines 72, 81, 91](../../../src/Core/src/Cryptography/HkdfUtilities.cs#L72)). [CLAUDE.md, line 59](../../../CLAUDE.md#L59) requires zeroing sensitive data. Whichever option is chosen, check the intermediate buffers. We did not inspect the runtime's internal buffers.
- **API impact.** None. The class is internal.
- **Proving test.** The existing repro compares the output with the runtime. It passes after either option.
- **Depends on / interacts with.** Item 25c uses the same "replace with runtime API" fix.
- **Open questions for the maintainer.** Runtime call (a), or the minimal loop fix (b)? We recommend (a).

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1633_Hkdf"`, from the worktree root.
- Read RFC 5869 section 2.3 at the [RFC link](https://www.rfc-editor.org/rfc/rfc5869).
- Read `src/Core/src/Cryptography/HkdfUtilities.cs` lines 45-48 and 69-89.

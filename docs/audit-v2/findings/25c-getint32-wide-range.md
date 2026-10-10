# 25c RandomNumberGenerator.GetInt32 wrapper returns a constant for wide ranges (YESDK-1633)

| | |
| --- | --- |
| Audit severity | MED (item 25c of the MED row 25) |
| Our verdict | Confirmed |
| Our severity | LOW |
| Root cause | Replace with runtime API (`System.Security.Cryptography.RandomNumberGenerator.GetInt32`) |
| Fix group | A (no-brainer) |
| Evidence | unit test (run today), static (code, .NET documentation) |
| Since the audit | Unchanged. `RandomNumberGeneratorExt.cs` is identical at `df1ec06d`. The methods are still in `PublicAPI.Unshipped.txt` (lines 265 and 1028-1030 at `df1ec06d`). |

Abbreviations used in this file: API is the application programming interface. HKDF is the HMAC-based key derivation function. RNG is the random number generator. SDK is the software development kit. XML is Extensible Markup Language; the XML documentation comments are part of the SDK's contract text. ykman is the Yubico command-line tool, written in Python.

## What the audit says

The audit says that the random number generator (RNG) extension returns deterministic values for large ranges.

> "Large RNG intervals return deterministic values without randomness"

## What is right

- For a range wider than `int.MaxValue`, the rejection loop never runs. The code sets `result = int.MaxValue` and then loops `while (result > range)` ([RandomNumberGeneratorExt.cs:61-62](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L61-L62)). When `range` is at least `int.MaxValue`, the condition is false and the loop body never runs. The method returns `(int)result + fromInclusive` ([RandomNumberGeneratorExt.cs:68](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L68)). That value is fixed for a given range, and no random bytes are drawn.
- The repro shows it. Repro: [CoreNumericBoundaryAuditReproTests.YESDK1633_GetInt32WideRange_ConsumesRandomness](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L62-L75), three cases. Result today: failed in all three. For `[int.MinValue, int.MaxValue)` and `[int.MinValue, 1)` the message is "GetInt32 returned -1 without drawing any random bytes." For `[-1, int.MaxValue)` it is "GetInt32 returned 2147483646 without drawing any random bytes."
- The file header says the code is adapted from the .NET runtime ([RandomNumberGeneratorExt.cs:15-19](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L15-L19)). The runtime documents a discard-and-retry strategy (see Specification). The port's loop condition does not give that behaviour for wide ranges, and that is the cause.

## What is wrong or imprecise

- Not in the audit: two exception messages are garbled. In `GetInt32`, the message is a string literal with a `+` inside the format string, so the text is wrong and the arguments are unused ([RandomNumberGeneratorExt.cs:46-50](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L46-L50)). `GetByte` has the same problem ([RandomNumberGeneratorExt.cs:102-104](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L102-L104)).
- The audit describes the method as public API, which is correct. Internal use is safe. The only call to `GetInt32` in the SDK source is inside `GetByte`, and `GetByte` rejects ranges above 256 ([RandomNumberGeneratorExt.cs:98-100](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L98-L100)). The defect is reachable only by external callers of the extension.

## Why it matters

- **Who can trigger it:** an application that calls the extension with a range of width 2^31 or more (for example, the full `int` range). The application gets a fixed value and no error.
- **Impact:** depends on the caller. If the application uses the value as a key, nonce or random index, the value is predictable. The SDK's own code does not do that.

## Specification

No protocol specification covers this. The SDK contract is the XML summary "Gets a random 32-bit signed int." ([RandomNumberGeneratorExt.cs:32](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L32)). It describes the range as `[fromInclusive, toExclusive)`.

The .NET documentation for the runtime method `RandomNumberGenerator.GetInt32(Int32, Int32)` says:

> "Generates a random integer between a specified inclusive lower bound and a specified exclusive upper bound using a cryptographically strong random number generator."

> "This method uses a discard-and-retry strategy to avoid the low value bias that a simple modular arithmetic operation would produce."

> "Negative values are permitted for both fromInclusive and toExclusive."

Source: [Microsoft Learn, RandomNumberGenerator.GetInt32](https://learn.microsoft.com/dotnet/api/system.security.cryptography.randomnumbergenerator.getint32), checked today.

The SDK's own crypto guide uses the runtime RNG directly ([docs/CRYPTO-APIS.md, line 23](../../CRYPTO-APIS.md#L23): `RandomNumberGenerator.Fill(random);`).

## Canonical Python reference

**Python has no equivalent.** We searched yubikit and ykman for HKDF, `randbelow` and `SystemRandom`. The only match is `random.SystemRandom()` in the static password generator ([ykman/otp.py#L66](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/otp.py#L66)). Python has no wide-range helper to compare with.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CoreNumericBoundaryAuditReproTests.YESDK1633_GetInt32WideRange_ConsumesRandomness](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs#L62-L75). Asserts that the value is in range and that random bytes were drawn. Result today: failed, all three cases (see above).
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** Delete the wrapper ([RandomNumberGeneratorExt.cs:38-69](../../../src/Core/src/Cryptography/RandomNumberGeneratorExt.cs#L38-L69)). Call `RandomNumberGenerator.GetInt32(fromInclusive, toExclusive)` at each call site. The other members of the same type depend on the wrapper. `GetByte` can call `RandomNumberGenerator.GetInt32(0, 256)`, and `Fill` can call `RandomNumberGenerator.Fill`. Or delete them too. Remove the entries from [PublicAPI.Unshipped.txt](../../../src/Core/src/PublicAPI.Unshipped.txt) (lines 228 and 988-990).
- **Options considered.**
  - **(a) Delete and use the runtime (recommended).** There is no SDK behaviour to keep.
  - **(b) Keep the wrapper and fix the loop** (`do { ... } while (result > range)`), and fix the two messages. Smallest diff, but it keeps a copy of code that already has the defect.
- **API impact.** (a) removes unshipped public members. That is not a break for shipped users, because the members are not in a shipped release. (b) None.
- **Proving test.** (b): the existing repro passes. (a): the repro is removed with the wrapper. The runtime method has its own tests.
- **Depends on / interacts with.** Item 25d uses the same "replace with runtime API" fix.
- **Open questions for the maintainer.** Remove or fix? (We recommend removal.)

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1633_GetInt32"`, from the worktree root.
- Read `src/Core/src/Cryptography/RandomNumberGeneratorExt.cs` lines 38-69.
- Read the Microsoft Learn page linked above, section "Remarks" for `GetInt32(Int32, Int32)`.

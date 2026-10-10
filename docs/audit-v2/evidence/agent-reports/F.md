# Audit v2 verification: agent F (Core, Cli.Commands)

Worktree: `<repo root>` @ fdfcd6fd. Relative to the audit commit 65e36386, the only production changes under `src/Core` and `src/Cli.Commands` are in `FidoCommands.cs` and `OpenPgpInfoCommand.cs`, which none of these findings touch. None of the audit line numbers below have moved.

Runtime facts I established myself (.NET SDK 10.0.401, runtime 10.0.12, macOS arm64):
- A negative `stackalloc` size is **not** a catchable `OverflowException`. The process prints `Stack overflow.` and exits with code 134 (SIGABRT). I saw this in `scratch-F/app.cs` (`dotnet run app.cs` gives `exit=134`) and again in the Release-built test assembly. So `catch (Exception)` cannot recover. Tests that hit it run in a child process (see `ChildProcessTestRunner`).
- With stdin redirected, `Console.KeyAvailable` throws `InvalidOperationException` ("...console input has been redirected from a file. Try Console.In.Peek."). Scratch: `scratch-F/keyavail/app.cs` run as `echo 123456 | dotnet run app.cs`.

---

### #23 Short SCP response crashes the host process (YESDK-1631) - audit severity MED
- Verdict: **CONFIRMED** | My severity: MED (agree). Any party on the wire can kill the host process with a 1-byte response, before any MAC check. An attacker in the path is exactly the threat SCP exists to defend against (malicious reader, NFC relay).
- Location at HEAD: `src/Core/src/Protocols/SmartCard/Scp/ScpState.cs:192` (`msgLength = data.Length - 8 + 2`), `:198-200` (`stackalloc byte[msgLength]`), `:202` (`data[..^8]`). Call site: `src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs:121-123`. No drift.
- Spec / contract: "No R-MAC shall be generated and no protection shall be applied to a response that includes an error status word: in this case only the status word shall be returned in the response. All status words except '9000' and warning status words (i.e. '62xx' and '63xx') shall be interpreted as error status words. [...] The R-MAC is made of the first 8 bytes (in S8 mode) or full 16 bytes (in S16 mode) of the CMAC computed on the message made of the MAC chaining value, the response data field (if present) and the status bytes." — GlobalPlatform Card Spec v2.3 Amd D (SCP03) v1.1.2 §6.2.5. So a protected response always ends in at least 8 R-MAC bytes. A non-empty data field shorter than 8 bytes cannot be valid.
- Reference implementations:
  - ykman `yubikit/core/smartcard/scp.py:223-230`: `data[:-8]` / `data[-8:]` slicing, so short data fails the constant-time compare and raises `BadResponseError("Wrong MAC")`. Safe.
  - yubikit-android `core/.../scp/ScpState.java:170-172`: `ByteBuffer.allocate(data.length - 8 + 2)` throws `IllegalArgumentException`. Wrong type, but catchable.
  - yubikit-swift `SCP/SCPState.swift:77`: `data.prefix(data.count - 8)` with a negative count traps. Same crash class, not verified at runtime.
  - Legacy .NET v1 `Yubico.YubiKey/src/Yubico/YubiKey/Scp/Helpers/ChannelMac.cs:96-99`: `if (response.Length < 8) throw new SecureChannelException(InsufficientResponseLengthToVerifyRmac)`. This is the correct behaviour, and v2 regressed from it.
- Evidence (call-site conditions): `ScpProcessor` calls `Unmac` whenever `response.Data.Length > 0`, **whatever the SW is**. An error SW that comes with data takes the same path. How the result depends on data length:

  | Data length | `msgLength` | Outcome today |
  |---|---|---|
  | 1..5 | -6..-1 | negative `stackalloc`, process terminated (SIGABRT 134) |
  | 6..7 | 0..1 | `data[..^8]` throws `ArgumentOutOfRangeException`, which the `catch (Exception)` at `:223` wraps as `NotSupportedException("Cryptography provider does not support AESCMAC")`. Misleading type and message. |
  | 0 | n/a | `Unmac` is skipped |
  | ≥ 8 | ≥ 2 | normal path |

- Similar arithmetic elsewhere in SCP (all catchable, none crash the process):
  - `ScpState.Decrypt` (`ScpState.cs:121-139`): only reached after a valid R-MAC. Ciphertext that is not a multiple of 16 gives a raw `CryptographicException` from `DecryptCbc`, not `BadResponseException`. The padding loop `for (i = len-1; i > 0; i--)` never looks at index 0, so a valid block whose plaintext is empty (`80 00..00`) is rejected as "Bad padding". ykman `scp.py:239-243` accepts it. §6.2.7 says no encryption is applied when there is no data, so a conforming card never sends this block. Informational.
  - `ScpState.Scp03InitAsync` (`ScpState.Scp03.cs:47-51`): slices `responseData[..10]`, `[10..13]`, `[13..21]` and `[21..29]` with no length check. A short INITIALIZE UPDATE response gives `ArgumentOutOfRangeException` instead of `BadResponseException`.
  - `ScpState.Scp11InitAsync` (`ScpState.Scp11.cs:117-118`): reads `tlvs[0]` and `tlvs[1]` with no count check. Same class of problem.
- Related observation, outside this finding and not a regression: a bare `9000` with empty data skips R-MAC verification completely (`ScpProcessor.cs:121`). §6.2.5 requires an R-MAC even when there is no data field. ykman (`smartcard/__init__.py:314`), yubikit-android (`ScpProcessor.java:63`) and yubikit-swift (`SmartCardInterface.swift:155`) all behave the same way, so this is consistent Yubico reference behaviour. It deserves a separate design decision.
- Repro: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs`
  - `YESDK1631_ShortResponseDataBelowRmac_DoesNotTerminateProcess` runs `YESDK1631_ChildBody_OneToFiveByteResponse_ThrowsBadResponse` in a child process. It **FAILED as expected** with "Child test process exited with 134 (134 = SIGABRT). Output head: Stack overflow."
  - `YESDK1631_SixOrSevenByteResponse_ThrowsBadResponse(6|7)` **FAILED as expected** with "Expected BadResponseException, Actual NotSupportedException: Cryptography provider does not support AESCMAC".
  - Both drive the real `ScpProcessor` + `ScpState` with a `FakeApduProcessor` (`ApduFormatterShort`) that returns an N-byte body plus `9000`.
- Hardware repro: none needed.
- Remediation: at the top of `ScpState.Unmac`, add `if (data.Length < MacLength) throw new BadResponseException("Response too short to contain R-MAC");` before any arithmetic. For defence in depth, also narrow the `catch (Exception)` at `:223` so non-provider errors are not relabelled as `NotSupportedException`. In the same change, add length guards to `Scp03InitAsync` (require ≥ 29 bytes), `Scp11InitAsync` (require ≥ 2 TLVs), and `Decrypt` (require a non-zero multiple of 16, and include i = 0 in the padding scan). All of these should throw `BadResponseException`.
- API impact: none. Internal types only. The exception type changes from crash/`NotSupportedException` to `BadResponseException`.
- Proving test: the three tests above pass. The child test's exit code becomes 0.
- Depends on / interacts with: none.
- Open decisions:
  1. Should `Unmac` be skipped for error SWs (per §6.2.5), or kept as is? Recommendation: keep calling `Unmac` when data is present. A card must not attach data to an error SW, so failing the MAC check is correct.
  2. The empty-data `9000` R-MAC gap above. Recommendation: raise it as a separate ticket and check firmware behaviour first.

---

### #25 Numeric-boundary issues (YESDK-1633) - audit severity MED

#### a) `--length` unchecked before `stackalloc` (Cli.Commands)
- Verdict: **CONFIRMED** | My severity: LOW. The input is a local CLI argument (self-DoS), and the Cli.Commands project is excluded internal tooling.
- Location at HEAD: `src/Cli.Commands/src/Otp/OtpCommands.cs:203` (`stackalloc byte[length]`). The caller at `:480-481` passes `settings.Length ?? 38` with no check. The option at `:92-94` is documented "Length of generated password (1-38)."
- Spec / contract: the SDK's own option description, "(1-38)". The 38-character limit is the YubiKey static-password maximum; ykman `yubikit/yubiotp.py` rejects longer values.
- Evidence: a negative value terminates the process (stack overflow). A very large value also overflows the stack. 0 returns an empty password. 39 or more returns a password longer than the documented maximum.
- Repro: `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs`
  - `YESDK1633_GenerateStaticPasswordOutOfDocumentedRange_Throws(0|39)` **FAILED as expected** with "No exception was thrown".
  - `YESDK1633_GenerateStaticPasswordNegativeLength_DoesNotTerminateProcess` runs the body in a child process. It **FAILED as expected** with "Child test process exited with 134 ... Stack overflow."
- Remediation: add `ArgumentOutOfRangeException.ThrowIfLessThan(length, 1); ThrowIfGreaterThan(length, 38);` to `GenerateStaticPassword`. Optionally also add a Spectre `Validate()` override on `OtpStaticSettings` for a friendly error message.
- API impact: none. Internal tooling.
- Unrelated observation, not verified: `OtpStaticCommand` passes `Encoding.UTF8.GetBytes(password)` as `scanCodes` (`:483`, `:494`). That is ASCII, not HID scan codes. Worth a separate look.

#### b) DeviceInfoReader page counter wraps and the loop never ends
- Verdict: **CONFIRMED** | My severity: LOW-MED. A malicious or faulty device hangs discovery (`ProtocolDeviceInfo.cs:463`) or `ManagementSession` (`:140`, `:200`), and only the caller's token stops it.
- Location at HEAD: `src/Core/src/Devices/DeviceInfoReader.cs:61` (`byte page`), `:88` (`remainingPages = moreData.Value.Span[^1]`), `:101` (`++page`). No drift.
- Spec / contract: no public spec for the YubiKey management READ CONFIG paging. Contract: the page index travels as the single P1 byte (`:148`, or a one-byte FIDO/OTP payload), so only 256 distinct pages exist. Core `CLAUDE.md` "Listener and Native Retry Loops" also requires loops to exit or back off on every failure path.
- Reference implementations:
  - ykman `yubikit/management.py:647-663` uses the same count-reset semantics, but `page` is a Python int, so it never wraps. On SmartCard, P1 = 256 makes `struct.pack(">BBBB", ...)` fail (`core/smartcard/__init__.py:158`), so the loop ends with an exception. The OTP and FIDO backends (`management.py:500`, `:592`) send `int2bytes(page)` and have no local bound.
  - yubikit-android `ManagementSession.java:373-389` only continues while the value is exactly `01`, and has no page bound.
  - yubikit-swift `ManagementSession.swift:77,91` uses `UInt8 page += 1`, which traps on overflow. That terminates the loop, by crashing.
  - Legacy v1 `GetDeviceInfoHelper.cs:39-58` does `(byte)page++`, which wraps just like v2.
  - None of these has an explicit bound. v2 is the case that never terminates.
- Evidence: a fake that returns `03 10 01 01` for every page. The reader asked for pages 0..255 and then page 0 again. The fake's 600-request guard stopped it.
- Repro: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs:YESDK1633_DeviceInfoAlwaysMoreData_StopsWithBadResponseWithinPageSpace` **FAILED as expected** with "Expected BadResponseException, got InvalidOperationException after 600 page requests (first repeated page index: request #256 asked for page 0 again)." The test uses a 10 s linked `CancellationTokenSource` plus the fake's request cap, so it cannot hang.
- Remediation: before `++page`, add `if (remainingPages > 0 && page == byte.MaxValue) throw new BadResponseException("Device info exceeds 256 pages");`. Optionally use a much smaller cap: real devices return 1-2 pages, so a limit like 16 would work.
- API impact: none.
- Open decision: the bound. Options are 256 (the protocol limit) or a small constant such as 16. I recommend 256 plus a test, because it is the only bound the protocol itself justifies.

#### c) `RandomNumberGeneratorExt.GetInt32` returns a constant for wide ranges
- Verdict: **CONFIRMED** | My severity: LOW. This is public API, but no SDK code calls it with a wide range: the only internal caller is `GetByte`, whose range is ≤ 256.
- Location at HEAD: `src/Core/src/Cryptography/RandomNumberGeneratorExt.cs:61-67`. `uint result = int.MaxValue; while (result > range)` means the loop body never runs when `range >= int.MaxValue`.
- Contract: the XML doc says "Gets a random 32-bit signed int" in `[fromInclusive, toExclusive)`. The file header says the code was adopted from dotnet/runtime `RandomNumberGenerator.GetInt32`, which uses `do { ... } while (result > range)`. The port broke that. Separately, the `ArithmeticException` message is a garbled string literal: `"ExceptionMessages.ValueMustBeBetweenXandY," + int.MinValue`, with unused format arguments.
- Repro: `CoreNumericBoundaryAuditReproTests.cs:YESDK1633_GetInt32WideRange_ConsumesRandomness` **FAILED as expected** for all 3 cases. For example, "GetInt32 returned -1 without drawing any random bytes" for `[int.MinValue, int.MaxValue)`, and "returned 2147483646" for `[-1, int.MaxValue)`.
- Remediation: delete `RandomNumberGeneratorExt`. The BCL has `RandomNumberGenerator.GetInt32(int, int)` and `RandomNumberGenerator.Fill(Span<byte>)`, and the type is only in `PublicAPI.Unshipped.txt`, so removal is pre-release. The alternative is to switch to `do/while` and fix the exception.
- API impact: removal is breaking only against unshipped API. The fix alone would be none.
- Open decision: remove or fix. I recommend removing it, since it is a wrapper with no benefit.

#### d) HkdfUtilities
- Verdict: **CONFIRMED-WITH-CORRECTION** | My severity: INFO/LOW. The audit's claim that L > 255*HashLen is not rejected is **wrong**: `HkdfUtilities.cs:45-48` rejects it, and that check has been there since 80ee372f1 (2025-12-09, before the audit). The counter overflow and negative offset are real, but they affect valid lengths 8129..8160 (N = 255). There, `for (byte index = 1; index <= 255; index++)` wraps to 0, `blockOffset = -32` (`:85`), and `AsSpan(-32)` throws `ArgumentOutOfRangeException`.
- Location at HEAD: `src/Core/src/Cryptography/HkdfUtilities.cs:69`, `:85`.
- Spec: "L length of output keying material in octets (<= 255*HashLen) [...] N = ceil(L/HashLen) [...] (where the constant concatenated to the end of each T(n) is a single octet.)" — RFC 5869 §2.3. So L = 8160 is valid and must succeed.
- Callers (reachability): only `ArkgPrimitivesOpenSsl.cs:259` (L = 32) and `:279` (L = `kPrime.Length`, a curve coordinate of about 32 bytes). **A large L cannot be reached.**
- Repro:
  - `CoreNumericBoundaryAuditReproTests.cs:YESDK1633_HkdfMaximumRfcLength_MatchesBclHkdf(8129|8160)` **FAILED as expected** with `ArgumentOutOfRangeException`.
  - Guard `YESDK1633_HkdfAboveRfcMaximum_Rejected` passes, which shows the §2.3 bound is enforced.
- Remediation: replace the class body with `System.Security.Cryptography.HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, length, salt, info)`. It is in the BCL since .NET 5, and an empty salt means HashLen zero bytes per the RFC, matching the current `HkdfExtract`. Or inline that call at the two Arkg call sites and delete `HkdfUtilities`. The minimal fix is an `int` loop counter. The test compares against BCL `HKDF`, so it also proves the replacement is equivalent.
- API impact: none (internal).

#### e) + #20: BER-TLV long-form length decoding (YESDK-1633 / YESDK-1628, #20 audit severity LOW)
- Verdict: **CONFIRMED-WITH-CORRECTION** | My severity: LOW. The audit's example `84 FF FF FF FF` is **already rejected** today: the length decodes to -1 and `buffer[..-1]` throws `ArgumentOutOfRangeException`, which is an `ArgumentException` subtype and the parser's error family. The real acceptance bug is 5+ byte length forms. `85 01 00 00 00 01` overflows the 32-bit accumulator to 1, and the TLV is accepted silently. #20 is confirmed as stated: `5A 81` and `5A 82 01` throw `IndexOutOfRangeException`.
- Location at HEAD: `src/Core/src/Utilities/Tlv.cs:232-241` (long-form loop, `buffer[0]` read without a bounds check, no `lengthLn` limit), `:244`.
- Spec: "b) bits 7 to 1 shall encode the number of subsequent octets in the length octets [...] c) the value 11111111₂ shall not be used. [...] shall be the encoding of an unsigned binary integer equal to the number of octets in the contents octets" — ITU-T X.690 §8.1.3.5. The value must be an unsigned count that fits in the buffer. Contract: every other truncation in `ParseData` throws `ArgumentException("Insufficient data for ...")`.
- Reference implementations:
  - ykman `yubikit/core/__init__.py:273-300` uses arbitrary-precision ints and maps `IndexError` to `ValueError("Invalid encoding of tag/length")`.
  - yubikit-android `core/.../util/Tlv.java:128-133` has the same int overflow pattern (not exercised).
- Repro (`CoreNumericBoundaryAuditReproTests.cs`):
  - `YESDK1633_TlvFiveByteLengthOverflow_Rejected` **FAILED as expected** with "No exception was thrown".
  - `YESDK1628_TlvTruncatedLongFormLength_ThrowsParseError([5A 81] | [5A 82 01])` **FAILED as expected** with "Actual: IndexOutOfRangeException". It covers both `Tlv.Create` and `TlvHelper.DecodeList`.
  - Guard `YESDK1633_TlvFourByteLengthNegative_Rejected` passes (not reproduced).
- Remediation: in the long-form branch, reject `lengthLn > 4` (or > 3; the YubiKey never needs more than `82`) and `buffer.Length < lengthLn` with `ArgumentException("Insufficient data for length")`. Accumulate in `uint`/`long` and reject values above `int.MaxValue` or above `buffer.Length`, with a proper message.
- API impact: none. Invalid inputs change from an unhandled exception type, or silent acceptance, to `ArgumentException`.
- Depends on / interacts with: device-info decoding (#21 / YESDK-1629) runs through `TlvHelper.DecodeList`, so `DeviceInfoReader` may want to wrap `ArgumentException` as `BadResponseException`.
- Open decision: max length-octet count, 4 or 3. I recommend 4 (the ISO 7816-4 maximum is `84`).

#### f) `TlvHelper.EncodeDictionary` underallocates
- Verdict: **CONFIRMED** | My severity: LOW. It is public API. The internal callers are not affected: `SecurityDomainTlvEncoding.cs:50` has 1-byte values, and `DeviceConfig.cs:116` has small values with 1-byte tags.
- Location at HEAD: `src/Core/src/Utilities/TlvHelper.cs:238` (estimate `2 + value.Length`, which assumes a 1-byte tag and a 1-byte length), `:250` (copy that fails).
- Evidence: the failure only shows when the estimate lands exactly on an `ArrayPool` bucket size, because `Rent` rounds up to a power of two. That makes it data-dependent and easy to miss.
- Repro: `CoreNumericBoundaryAuditReproTests.cs:YESDK1633_EncodeDictionaryLongTagOrLength_EncodesAllBytes` fails for (0x7F49, 126 B), where the estimate is 128 and the actual size 129, and for (0x53, 254 B), where the estimate is 256 and the actual size 257. Both **FAILED as expected** with "ArgumentException: Destination is too short."
- Remediation: build the `Tlv`s first and sum `TotalLength`, reusing the `EncodeList` approach (`TlvHelper.cs:112-132` already sizes exactly). Or use a worst-case estimate of `2 (tag) + 5 (length) + value.Length` per entry.
- API impact: none.

---

### #26 API footguns: credential-reader items (YESDK-1634) - audit severity MED

#### g) Redirected input bypasses `CredentialReaderOptions` policy
- Verdict: **CONFIRMED** | My severity: LOW-MED. Automation accepts PINs that interactive mode rejects, and the device then rejects them or burns a retry.
- Location at HEAD: `src/Core/src/Credentials/ConsoleCredentialReader.cs:75-77` (branch), `:125-145` (`ReadNonInteractive` passes the line straight to `ConvertToResult`). Policy is defined at `CredentialReaderOptions.cs:44-63` and `ForPin` at `:87-94` ("6-8 numeric digits"). No drift.
- Contract: `ForPin()` XML doc: "Creates options configured for PIN input (6-8 numeric digits)." `MinLength`, `MaxLength` and `CharacterFilter` are documented as the allowed lengths and characters, with no mention of an interactive-only restriction.
- Seam: `IConsoleInputSource` / `MockConsoleInput` (internal) makes this testable. No Console dependency.
- Repro: `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs:YESDK1634_NonInteractivePinViolatingPolicy_IsRejected("abcdef"|"123"|"123456789")` **FAILED as expected**. All three returned a `DisposableArrayPoolBuffer` (length 6, 3 and 9) instead of null.
- Additional finding: `ReadCredentialWithConfirmation` (`:81-117`) ignores `IsInteractive` and always uses the key-by-key path. With a real redirected stdin, `Console.KeyAvailable` throws `InvalidOperationException` (confirmed by the scratch program above). The confirmation API is therefore unusable in automation.
- Remediation: after reading the line in `ReadNonInteractive`, enforce `MinLength`/`MaxLength` (counted in chars before any hex separator stripping, consistent with interactive mode) and reject any character that fails `CharacterFilter` or is a control character. Return null with a console message, as `ParseHex` does. Route the confirmation path through the same interactive/non-interactive switch.
- API impact: behavioural only. Invalid redirected input now returns null.
- Open decision: reject or filter. Interactive mode silently drops filtered characters and truncates at MaxLength, but for a line that already exists, rejecting is safer. I recommend rejecting.

#### h) Confirmation path strands the first credential buffer
- Verdict: **CONFIRMED** | My severity: LOW.
- Location at HEAD: `src/Core/src/Credentials/ConsoleCredentialReader.cs:87-100`. `first` is held across `_console.Write(options.ConfirmPrompt)` and the second `ReadCredentialCore(...)` with no try/finally.
- Contract: class remarks at `ConsoleCredentialReader.cs:34-37`: "Returns IMemoryOwner<byte> that zeros memory on disposal [...] Clears intermediate buffers on all code paths".
- Repro: `CoreCredentialReaderAuditReproTests.cs:YESDK1634_ConfirmationCancelled_FirstCredentialIsCleared` **FAILED as expected** with "First credential buffer was not cleared: 3132333435360000" ("123456" is still in the pooled array). How the test works:
  - It seeds this thread's `ArrayPool<byte>.Shared` bucket with a sentinel array, so the first credential lands in a known array. A precondition assert guards that assumption.
  - The fake console cancels the token when the confirm prompt is written.
- Remediation: wrap everything after the first read in `try { ... } catch { first.Dispose(); throw; }`, or use a `success` flag with `finally`. Dispose `second` the same way.
- API impact: none.

---

## Files added
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/ChildProcessTestRunner.cs`: re-runs a single test of this assembly in a child process (`--filter-method` plus an env marker). The child bodies show as *skipped* in normal runs.
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreScpAuditReproTests.cs` (#23)
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreNumericBoundaryAuditReproTests.cs` (#25 b-f, #20)
- `src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs` (#26 g, h)
- `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/ChildProcessTestRunner.cs` (copy, different namespace)
- `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs` (#25 a)
- Scratch programs (outside repo): `.../audit-v2/scratch-F/app.cs` and `.../audit-v2/scratch-F/keyavail/app.cs`

No production code, csproj or config changes. Nothing committed.

## Commands
Unit repros:
```
dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
dotnet toolchain.cs -- test --project Cli.Commands.UnitTests --filter "FullyQualifiedName~AuditV2&Category!=RequiresHardware&Category!=RequiresUserPresence"
```
Observed:
- Core.UnitTests: `total: 21, failed: 18, succeeded: 2, skipped: 1`. The 2 passes are the guards `YESDK1633_HkdfAboveRfcMaximum_Rejected` and `YESDK1633_TlvFourByteLengthNegative_Rejected`. The skip is the child body.
- Cli.Commands.UnitTests: `total: 4, failed: 3, succeeded: 0, skipped: 1`.

Hardware repros: none. Every item reproduces without hardware.

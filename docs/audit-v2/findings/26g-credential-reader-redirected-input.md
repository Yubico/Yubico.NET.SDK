# 26g Credential reader accepts redirected input that violates its policy (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (item g of the MED row 26) |
| Our verdict | Confirmed |
| Our severity | LOW-MED |
| Root cause | SDK only |
| Fix group | B (small API decision: reject or filter, and how to report it) |
| Evidence | unit test (run today), static (code, XML documentation, CLAUDE.md) |
| Since the audit | Unchanged. `ConsoleCredentialReader.cs`, `CredentialReaderOptions.cs` and `IConsoleInputSource.cs` are identical at `df1ec06d`. |

Abbreviations used in this file: API is the application programming interface. CI is continuous integration. PIN is the personal identification number. SDK is the software development kit. XML is Extensible Markup Language; the XML documentation comments are part of the SDK's contract text. ykman is the Yubico command-line tool, written in Python.

## What the audit says

The audit says that redirected input skips the checks in the reader's options, so input that the interactive mode rejects is accepted.

> "sends redirected input directly to conversion without those checks"

## What is right

- In the non-interactive branch, `ReadNonInteractive` passes the line straight to `ConvertToResult` ([ConsoleCredentialReader.cs:138](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L138)). It does not check `MinLength`, `MaxLength` or `CharacterFilter`.
- The interactive path does check them. It drops filtered characters ([lines 198-206](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L198-L206)), stops at `MaxLength` ([lines 208-211](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L208-L211)), and rejects an entry that is too short ([line 170](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L170)).
- `ForPin()` sets `MinLength = 6`, `MaxLength = 8` and `CharacterFilter = char.IsAsciiDigit` ([CredentialReaderOptions.cs:87-94](../../../src/Core/src/Credentials/CredentialReaderOptions.cs#L87-L94)).
- The repro shows it. Repro: [CoreCredentialReaderAuditReproTests.YESDK1634_NonInteractivePinViolatingPolicy_IsRejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs#L29-L43), with `abcdef`, `123` and `123456789`. Result today: failed in all three. The reader returned a credential buffer of length 6, 3 and 9.

## What is wrong or imprecise

- "Redirected" is not the full condition. The non-interactive branch runs when either input or output is redirected. `IsInteractive` requires both to be attached to a console ([IConsoleInputSource.cs:64](../../../src/Core/src/Credentials/IConsoleInputSource.cs#L64)). A program that redirects only its output also skips the policy checks.
- Hex mode is partly covered. `ParseHex` checks the digits and the expected byte length ([ConsoleCredentialReader.cs:253-288](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L253-L288)). It does not apply the configured length bounds or the character filter.
- The audit says that automation "accepts input that interactive mode rejects." That is correct.

## Why it matters

- **Who can trigger it:** an application or script that uses the same options (for example `ForPin()`) with redirected input. Batch or CI PIN entry is one example.
- **Effect:** input that the interactive mode would reject is returned as a credential. The caller then sends it to the device. A wrong PIN counts against the device's retry counter. That is device behaviour, which we did not test here. The non-interactive path also prints no "too short" message.
- **Severity:** LOW-MED. The bad value reaches the device, and it can consume a retry. Nothing secret leaks.

## Specification

No protocol specification governs this. The SDK contract is the XML documentation.

- `CredentialReaderOptions.ForPin()`: "Creates options configured for PIN input (6-8 numeric digits)." ([CredentialReaderOptions.cs:85](../../../src/Core/src/Credentials/CredentialReaderOptions.cs#L85))
- `MinLength`: "Gets or sets the minimum allowed credential length." ([CredentialReaderOptions.cs:45-46](../../../src/Core/src/Credentials/CredentialReaderOptions.cs#L45-L46))
- `MaxLength`: "Gets or sets the maximum allowed credential length." ([CredentialReaderOptions.cs:50](../../../src/Core/src/Credentials/CredentialReaderOptions.cs#L50))
- `CharacterFilter`: "Gets or sets a filter function that determines which characters are allowed." ([CredentialReaderOptions.cs:60-61](../../../src/Core/src/Credentials/CredentialReaderOptions.cs#L60-L61))
- The reader's own remarks say only that non-interactive input is not masked: "Non-interactive mode: Input is not masked (warning displayed)" ([ConsoleCredentialReader.cs:45](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L45)). They do not say that the policy is relaxed.

## Canonical Python reference

**Python has no equivalent in yubikit.** The reader is an SDK helper. ykman's prompts use Click. We did not compare their policy handling with this reader.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CoreCredentialReaderAuditReproTests.YESDK1634_NonInteractivePinViolatingPolicy_IsRejected](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs#L29-L43). Feeds one line through a mock console with `IsInteractive = false` and asserts `null`. Result today: failed for all three inputs.
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** In `ReadNonInteractive` ([ConsoleCredentialReader.cs:125-145](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L125-L145)), apply the same policy as the interactive path before conversion:
  - enforce `MinLength` and `MaxLength`, counted in characters;
  - reject a character that fails `CharacterFilter` or that is a control character;
  - return `null` and print a message, as `ParseHex` already does ([lines 270-271](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L270-L271), [279-280](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L279-L280), [286-287](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L286-L287)).
- **Options considered.**
  - **(1) Reject (recommended).** The reader never returns input that the interactive mode would reject, and it never changes what the user typed.
  - **(2) Filter or truncate, as the interactive mode does.** Interactive mode drops extra characters ([lines 208-211](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L208-L211)). Doing the same here would silently change the input. A nine-digit PIN would become an eight-digit PIN, and the device would get a different PIN from the one that was typed.
- **Open question: null or an exception.** `null` already means "cancelled" (Escape, [lines 181-184](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L181-L184)) and "no input" ([lines 131-133](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L131-L133)). If invalid input also returns `null`, a caller cannot tell the two apart. The existing `ParseHex` pattern returns `null`. We recommend keeping that pattern for consistency. The alternative is a distinct exception for invalid input.
- **API impact.** Behavioural. Invalid redirected input now returns `null` (or throws, if the alternative is chosen) instead of a buffer.
- **Proving test.** The existing repro passes after the change. Add a positive control: a valid eight-digit PIN, redirected, still returns a credential.
- **Depends on / interacts with.** Item 26h (same class, different method). N3 (confirmation with redirected input) takes the same branch decision.
- **Open questions for the maintainer.** Reject (recommended) or filter? `null` or an exception for invalid input?

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1634_NonInteractive"`, from the worktree root.
- Read `src/Core/src/Credentials/ConsoleCredentialReader.cs` lines 71-78 and 125-145.
- Read `src/Core/src/Credentials/CredentialReaderOptions.cs` lines 84-94.

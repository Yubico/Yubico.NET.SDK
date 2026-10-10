# 26h Credential reader confirmation strands the first credential buffer (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (item h of the MED row 26) |
| Our verdict | Confirmed |
| Our severity | LOW |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test (run today), static (code, XML documentation, CLAUDE.md) |
| Since the audit | Unchanged. `ConsoleCredentialReader.cs` is identical at `df1ec06d`. |

Abbreviations used in this file: API is the application programming interface. CLI is the command-line interface. PIN is the personal identification number. SDK is the software development kit. XML is Extensible Markup Language; the XML documentation comments are part of the SDK's contract text.

## What the audit says

The audit says that if the confirmation step is cancelled or throws, the first credential is never disposed, so its bytes stay in memory and the caller gets no owner to dispose.

> "Cancellation during confirmation, or an exception writing its prompt, skips `first.Dispose(),` leaving credential bytes uncleared and returning no owner for the caller to dispose."

## What is right

- The first credential is held while the confirmation prompt is written ([ConsoleCredentialReader.cs:94](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L94)) and the second entry is read ([line 95](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L95)). No `try` or `finally` covers that window. The explicit disposals at [lines 98](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L98) and [108](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L108) cover only the "no second entry" and "entries do not match" branches.
- The repro shows the leak. Repro: [CoreCredentialReaderAuditReproTests.YESDK1634_ConfirmationCancelled_FirstCredentialIsCleared](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs#L51-L76). The repro seeds the shared pool so that it knows which array holds the first credential. The cancellation fires when the confirmation prompt is written. Result today: failed. The message was "First credential buffer was not cleared: 3132333435360000". The first six bytes, `123456`, are still in the array.
- The class remarks make the promise that the code breaks: "Clears intermediate buffers on all code paths" ([ConsoleCredentialReader.cs:37](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L37)).

## What is wrong or imprecise

- The audit cites lines 87 to 95. The unprotected window is lines 87 to 116. The success path returns `first` ([line 116](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L116)), so the leak happens only on exceptional paths: cancellation, and an exception from the prompt write or from the second read.
- The second entry has the same pattern. It is disposed on the mismatch and success paths, but not if an exception is thrown while it is held. The window is small, because only the comparison runs before its disposal. The verification report recommends guarding both entries, and we agree.
- "Returning no owner for the caller to dispose" is correct. The method throws, so no owner is returned.

## Why it matters

- **Who can trigger it:** an application that calls `ReadCredentialWithConfirmation`. That is public Core API. The example tools use it (for example, [PivTool's PIN prompt](../../../src/Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs#L133)). The production CLI in `Cli.Commands` does not call it.
- **Precondition:** cancellation during the confirmation step, or an exception from the console.
- **Effect:** the entered secret stays uncleared in an abandoned heap array until the garbage collector reclaims it. Only `Dispose` zeroes the array and returns it to the pool ([DisposableArrayPoolBuffer.cs:74-84](../../../src/Core/src/Utilities/DisposableArrayPoolBuffer.cs#L74-L84)). This failure path never reaches `Dispose`, so the array is not returned, and ordinary pool rentals cannot obtain it through this path. The demonstrated exposure is uncleared process memory, not pool reuse.
- **Severity:** LOW.

## Specification

No protocol specification governs this. The contract is the reader's class remarks ([ConsoleCredentialReader.cs:33-37](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L33-L37)):

- "Returns IMemoryOwner{T} that zeros memory on disposal"
- "Clears intermediate buffers on all code paths"

The root [CLAUDE.md](../../../CLAUDE.md) gives two rules that apply:

- "ALWAYS zero sensitive data: `CryptographicOperations.ZeroMemory()`" ([line 59](../../../CLAUDE.md#L59))
- "NEVER forget to return `ArrayPool` buffers (use try/finally)" ([line 54](../../../CLAUDE.md#L54))

## Canonical Python reference

**Python has no equivalent.** Python strings are immutable, so the secret cannot be cleared in the same way. There is no Python helper with this contract to compare with.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CoreCredentialReaderAuditReproTests.YESDK1634_ConfirmationCancelled_FirstCredentialIsCleared](../../../src/Core/tests/Yubico.YubiKit.Core.UnitTests/AuditV2/CoreCredentialReaderAuditReproTests.cs#L51-L76). Asserts that the array is all zeros after the cancelled confirmation. Result today: failed, as above.
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** Restructure `ReadCredentialWithConfirmation` ([ConsoleCredentialReader.cs:81-117](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L81-L117)) so that `first` is disposed on every path except the success return, and `second` is disposed on every path. Two shapes work:
  - a `try` and `catch` that disposes `first` and rethrows;
  - a `success` flag and a `finally` that disposes `first` unless it is returned, and disposes `second` always.

  We recommend the `finally` shape. It covers the success path too, and it keeps the two entries together.
- **API impact.** None.
- **Proving test.** The existing repro passes after the change. Add a second case where the prompt write throws instead of cancelling, as the audit describes.
- **Depends on / interacts with.** Items 26g and N3 change the same reader class. N3 changes which branch the confirmation uses. Coordinate the three edits.
- **Open questions for the maintainer.** None.

## Check it yourself

- `dotnet toolchain.cs -- test --project Core.UnitTests --filter "FullyQualifiedName~YESDK1634_ConfirmationCancelled"`, from the worktree root.
- Read `src/Core/src/Credentials/ConsoleCredentialReader.cs` lines 81-117.

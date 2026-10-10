# 25a CLI static password length unchecked (YESDK-1633)

| | |
| --- | --- |
| Audit severity | MED (item 25a of the MED row 25) |
| Our verdict | Confirmed |
| Our severity | LOW |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test (negative value runs in a child process), static (code, option text, Python read) |
| Since the audit | Unchanged. `OtpCommands.cs` is identical at `df1ec06d`. |

Abbreviations used in this file: API is the application programming interface. CLI is the command-line interface. NuGet is the .NET package manager. SDK is the software development kit. XML is Extensible Markup Language; the XML documentation comments are part of the SDK's contract text. ykman is the Yubico command-line tool, written in Python.

## What the audit says

The audit says that a negative `--length` value reaches a `stackalloc` in the CLI and aborts the process.

> "Unchecked negative password length causes a fatal stack allocation"

## What is right

- `GenerateStaticPassword(int length)` passes `length` to `stackalloc` with no check ([OtpCommands.cs:201-203](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L201-L203)).
- The caller passes `settings.Length ?? 38` straight through ([OtpCommands.cs:480-481](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L480-L481)).
- The CLI's own option text says "Length of generated password (1-38)." ([OtpCommands.cs:92-94](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L92-L94)). The value 38 is the static-password size. It is the sum of the fixed, UID and key fields (16 + 6 + 16 bytes, [YubiOtpConstants.cs:33](../../../src/YubiOtp/src/YubiOtpConstants.cs#L33)).
- A negative length crashes the process. Repro: [CliOtpAuditReproTests.YESDK1633_GenerateStaticPasswordNegativeLength_DoesNotTerminateProcess](../../../src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs#L35-L48). The child process exited with 134 and printed `Stack overflow.` Result today: failed as expected.
- Values 0 and 39 are not rejected. Repro: [CliOtpAuditReproTests.YESDK1633_GenerateStaticPasswordOutOfDocumentedRange_Throws](../../../src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs#L25-L30). Result today: failed, "No exception was thrown" for both values.

## What is wrong or imprecise

- The audit calls the negative case "fatal". That is confirmed. The audit does not name a value. We did not run very large positive values, so the claim that they overflow the stack is not verified here.
- A value of 39 is not rejected. The generator returns 39 characters, and the CLI prints them ([OtpCommands.cs:482](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L482)). Only then does the slot constructor reject the 39-byte scan code array ([StaticPasswordSlotConfiguration.cs:97-102](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L97-L102)). That happens after the confirmation prompt and the session open ([OtpCommands.cs:500-513](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L500-L513)). The user sees a password, then an error.

## Why it matters

- **Who can trigger it:** the person running the CLI, or a script that builds the command line from untrusted input.
- **Effect:** the CLI process aborts with no error message.
- **Reach:** the CLI project is not packable (`IsPackable` is false in [Yubico.YubiKit.Cli.Commands.csproj](../../../src/Cli.Commands/src/Yubico.YubiKit.Cli.Commands.csproj#L6)). SDK users who take the NuGet packages do not get this code. The impact is limited to the CLI tool.

## Specification

The contract is the CLI's own option text, quoted above. There is no protocol specification for the CLI option. The static-password size comes from the slot format. The XML documentation of the byte-array constructor's `scanCodes` parameter says: "The keyboard scan codes representing the password (up to 38 bytes)." ([StaticPasswordSlotConfiguration.cs:80](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L80)).

## Canonical Python reference

**Python is correct here.** ykman declares the option as `click.IntRange(1, 38)`. Click rejects 0, 39 and negative values before the generator runs.

[ykman/_cli/otp.py, lines 535-543](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/otp.py#L535-L543) at `4ca60f7`:

```python
@click.option(
    "-l",
    "--length",
    metavar="LENGTH",
    type=click.IntRange(1, 38),
    default=38,
```

Typed passwords longer than 38 characters are rejected as well ([ykman/_cli/otp.py#L568](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/otp.py#L568)). The generator is a loop over `random.SystemRandom().choice` ([ykman/otp.py#L54-L67](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/otp.py#L54-L67)). It has no fixed-size allocation.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: [CliOtpAuditReproTests.YESDK1633_GenerateStaticPasswordOutOfDocumentedRange_Throws](../../../src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs#L25-L30), cases 0 and 39. Asserts `ArgumentOutOfRangeException`. Result today: failed, "No exception was thrown".
- Unit: [CliOtpAuditReproTests.YESDK1633_GenerateStaticPasswordNegativeLength_DoesNotTerminateProcess](../../../src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/AuditV2/CliOtpAuditReproTests.cs#L35-L48). Runs the body in a child process. Result today: failed, child exit 134 with `Stack overflow.`
- Hardware: not applicable.

## Proposed fix

- **Recommendation.** At the top of `OtpHelpers.GenerateStaticPassword` ([OtpCommands.cs:201](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L201)), before the `stackalloc`, add:

  ```csharp
  ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
  ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 38);
  ```

  `YubiOtpConstants` is `internal` ([YubiOtpConstants.cs:20](../../../src/YubiOtp/src/YubiOtpConstants.cs#L20)), so the CLI needs its own constant or a shared public one.
- **Optional.** Add a friendly validation on `OtpStaticSettings.Length` ([OtpCommands.cs:92-94](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L92-L94)), so the user sees a message before the confirmation prompt. If you do this, keep the method guard as well. The guard must also run before the password is printed.
- **Options considered.** Method guard only (the minimum), or method guard plus option validation (the usual CLI pattern).
- **API impact.** None. The CLI is not packaged.
- **Proving test.** The existing repros pass after the guard. The child-process body covers the negative case.
- **Depends on / interacts with.** N3 changes the same generate path (the password encoding). Edit both in one pass.
- **Open questions for the maintainer.** Add the friendly option validation, or keep only the method guard?

## Check it yourself

- `dotnet toolchain.cs -- test --project Cli.Commands.UnitTests --filter "FullyQualifiedName~YESDK1633"`, from the worktree root.
- Read `src/Cli.Commands/src/Otp/OtpCommands.cs` lines 92-94, 201-214, and 480-483.
- Read `ykman/_cli/otp.py` lines 535-543 and 568 at `4ca60f7`.

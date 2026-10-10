# N3 CLI static password encoding and redirected-input confirmation (no Jira ticket)

| | |
| --- | --- |
| Audit severity | Not in the audit. New finding from our verification. |
| Our verdict | Confirmed (two defects) |
| Our severity | LOW |
| Root cause | SDK only |
| Fix group | A (no-brainer). The default keyboard layout is a small decision, listed under open questions. |
| Evidence | static (code, Python read), a throwaway program that reproduced the redirected-input exception (run today, not committed). No repro test exists for either defect. |
| Since the audit | Static-password path unchanged: `OtpCommands.cs` and `StaticPasswordSlotConfiguration.cs` are identical at `df1ec06d`. `YubiOtpSession.cs` changed only in its user-presence context, not in the configuration write. `ConsoleCredentialReader.cs` is unchanged. |

## What the audit says

The audit does not cover this item. Our verification found two defects in the CLI and the credential reader. The first is that the static password slot receives the UTF-8 bytes of the password as if they were HID scan codes. The second is that the credential confirmation step cannot run with redirected input.

Abbreviations used here: HID is the human interface device class (keyboard usage codes). CLI is the command-line interface. ModHex is the Yubico 16-letter keyboard layout. ASCII is the American Standard Code for Information Interchange. UTF-8 is the 8-bit Unicode Transformation Format. API is the application programming interface. SDK is the software development kit. XML is Extensible Markup Language; the XML documentation comments are part of the SDK's contract text. ykman is the Yubico command-line tool, written in Python.

## What is right

- **The CLI sends UTF-8 bytes as scan codes.** The static command uses `Encoding.UTF8.GetBytes(...)` for a typed password ([OtpCommands.cs:493](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L493)) and for a generated one ([line 483](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L483)). It passes the result to the byte constructor ([line 513](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L513)). That constructor copies the bytes into the slot unchanged ([StaticPasswordSlotConfiguration.cs:85-88](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L85-L88) and the copy in `Initialize`, lines 90-138).
- **The SDK already has the correct constructor.** `StaticPasswordSlotConfiguration(string password, KeyboardLayout keyboardLayout)` translates each character to its HID code for the layout ([StaticPasswordSlotConfiguration.cs:53-56](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L53-L56), `ToScanCodes` at [lines 143-173](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L143-L173)). It rejects characters the layout does not support.
- **The `--keyboard-layout` option is never read.** It is declared ([OtpCommands.cs:96-98](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L96-L98)), but no code in the static command uses it. A search of the command project finds only the declaration.
- **Confirmation ignores the input mode.** `ReadCredentialWithConfirmation` always uses the key-by-key path, and that path polls `Console.KeyAvailable` ([ConsoleCredentialReader.cs:86-95](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L86-L95), [line 158](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L158)). `RealConsoleInput.KeyAvailable` returns `Console.KeyAvailable` ([IConsoleInputSource.cs:73](../../../src/Core/src/Credentials/IConsoleInputSource.cs#L73)).
- **The redirected-input exception is real.** In a throwaway program (outside the repository), `Console.KeyAvailable` with redirected stdin threw `System.InvalidOperationException: Cannot see if a key has been pressed when either application does not have a console or when console input has been redirected from a file. Try Console.In.Peek.` The program is in the "Check it yourself" section.

## What is wrong or imprecise

- **Defect 1: the programmed keys differ from the typed text.** The CLI reports success after programming ([OtpCommands.cs:517-518](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L517-L518)), so the user gets no warning. For example, the ModHex table gives the letter `c` the HID code `0x06` ([HidCodeTranslator.ModHex.cs:26](../../../src/Core/src/Transports/Hid/Keyboard/HidCodeTranslator.ModHex.cs#L26)). The CLI sends `0x63`, the ASCII value of `c`. Those are different keys. The generated password has the same problem: the generator uses ModHex letters ([OtpCommands.cs:161](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L161)), which the CLI then encodes as ASCII.
- **Defect 1: the default layout is inconsistent.** The help text says the default is US ([OtpCommands.cs:97](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L97)). ykman defaults to MODHEX ([ykman/_cli/otp.py#L548](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/otp.py#L548)). The SDK's `KeyboardLayout` enum has ModHex as its zero value ([KeyboardLayout.cs:20](../../../src/Core/src/Transports/Hid/Keyboard/KeyboardLayout.cs#L20)).
- **Defect 2 is not a production CLI defect.** Our report says that the CLI's confirmation is unusable with redirected stdin. The production CLI does not call `ReadCredentialWithConfirmation`. Its `--force` confirmation uses `ConfirmationPrompts.ConfirmDangerous`, which uses Spectre.Console ([ConfirmationPrompts.cs:28](../../../src/Cli.Shared/src/Output/ConfirmationPrompts.cs#L28)). We did not test whether that works with redirected stdin. The callers of the reader's confirmation are the example tools: [PivTool](../../../src/Piv/examples/PivTool/Cli/Prompts/PinPrompt.cs#L133), [OathTool](../../../src/Oath/examples/OathTool/Commands/AccessCommand.cs#L84) and [FidoTool](../../../src/Fido2/examples/FidoTool/Cli/Prompts/FidoPinHelper.cs#L54). So defect 2 affects SDK API users and the example tools.

## Why it matters

- **Defect 1.** Anyone who programs a static password with `otp static`, typed or generated, gets a slot that types different characters from the text they gave. The CLI says the slot was programmed. The error shows only when the password is typed into a field. It is a silent functional error, not a security exposure.
- **Defect 2.** An application that calls `ReadCredentialWithConfirmation` with redirected input gets `InvalidOperationException` on the first key poll. The confirmation feature cannot be used in automation at all.

## Specification

No protocol specification governs either defect. The contracts are the XML documentation and the reader's remarks.

- The string constructor: "Initializes a new static password configuration from a human-readable password, translating each character to a HID scan code for the given keyboard layout." ([StaticPasswordSlotConfiguration.cs:34-35](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L34-L35))
- The byte constructor's parameter: "The keyboard scan codes representing the password (up to 38 bytes)." ([StaticPasswordSlotConfiguration.cs:80](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L80))
- The reader's remarks describe both modes: "with fallback to unmasked line-based input when running non-interactively" ([ConsoleCredentialReader.cs:28-29](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L28-L29)). The reader is meant to work in non-interactive mode. Only the masking changes.

## Canonical Python reference

- **Defect 1: Python is correct here.** The yubikit library takes scan codes as bytes ([yubikit/yubiotp.py, lines 463-474](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L463-L474)). The ykman CLI translates the password first, with the chosen layout ([ykman/_cli/otp.py, lines 576-585](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/otp.py#L576-L585)):

  ```python
  password = generate_static_pw(length, keyboard_layout)
  ...
  scan_codes = encode(password, keyboard_layout)
  ...
  StaticPasswordSlotConfiguration(scan_codes).append_cr(enter),
  ```

  `encode` maps each character through the layout's table ([ykman/scancodes/__init__.py, lines 44-48](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/scancodes/__init__.py#L44-L48)). Generated passwords use only the layout's characters ([ykman/otp.py, lines 54-67](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/otp.py#L54-L67)). The default layout is MODHEX ([otp.py line 548](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/otp.py#L548)).
- **Defect 2: Python has no equivalent.** yubikit has no reader with a confirmation mode.

## Sibling SDKs (context only)

Not checked for this item.

## Reproduction

- Unit: none. Neither defect has a repro test in the repository.
- Throwaway program (run today, not committed): the program in "Check it yourself" printed the `InvalidOperationException` quoted above.
- Hardware: not run. We did not program a slot on a test key.

## Proposed fix

- **Defect 1 (group A).** In `OtpStaticCommand` ([OtpCommands.cs:476-494](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L476-L494), slot construction at [line 513](../../../src/Cli.Commands/src/Otp/OtpCommands.cs#L513)):
  - Parse `--keyboard-layout` into a `KeyboardLayout` value.
  - For a typed password, use `new StaticPasswordSlotConfiguration(password, layout)`.
  - For `--generate`, generate from the layout's characters (or from ModHex), and use the same constructor.
  - Report an unsupported character as an error. The constructor already throws for one ([StaticPasswordSlotConfiguration.cs:49-52](../../../src/YubiOtp/src/StaticPasswordSlotConfiguration.cs#L49-L52)).
  - Keep the byte constructor for callers that really have scan codes.
- **Defect 1: the default layout (a decision).** Options: keep the documented default, US. Or match ykman, MODHEX. We recommend keeping US, because the help text already promises it. The maintainer should confirm.
- **Defect 1: compatibility.** A slot programmed by the old CLI keeps the old codes. Reprogramming with the fixed CLI gives different output for the same text. Note this in the change.
- **Defect 2 (group A).** In `ReadCredentialWithConfirmation` ([ConsoleCredentialReader.cs:81-117](../../../src/Core/src/Credentials/ConsoleCredentialReader.cs#L81-L117)), when `_console.IsInteractive` is false, read two lines through the non-interactive path, with the policy checks from item 26g. Compare them with `FixedTimeEquals`, as the interactive path does. Keep the key-by-key path for interactive use. Coordinate this with items 26g and 26h, which change the same class.
- **API impact.**
  - Defect 1: CLI behaviour changes for typed and generated passwords. Previously programmed slots are not changed.
  - Defect 2: no `InvalidOperationException` with redirected input. The method reads two lines.
- **Proving test.**
  - Defect 1: a unit test that the static command's scan codes equal the output of `StaticPasswordSlotConfiguration(password, layout)` for a known string. For example, the first ModHex letter `c` must give `0x06`.
  - Defect 2: a unit test with `MockConsoleInput { IsInteractive = false }` and two lines. Matching lines return a buffer. Mismatched lines return `null`.
- **Depends on / interacts with.** Items 26g and 26h (same reader), and item 25a (same generate path in the CLI).
- **Open questions for the maintainer.** The default keyboard layout: US (as the help says) or MODHEX (as ykman does)?

## Check it yourself

- Throwaway program, outside the repository. Create `app.cs` with this content, then run `echo 123456 | dotnet run app.cs`. Expect `System.InvalidOperationException`.

  ```csharp
  using System;

  try
  {
      Console.WriteLine(Console.KeyAvailable);
  }
  catch (Exception e)
  {
      Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
  }
  ```

- Read `src/Cli.Commands/src/Otp/OtpCommands.cs` lines 476-518, and lines 96-98 for the option.
- Read `src/YubiOtp/src/StaticPasswordSlotConfiguration.cs` lines 53-56 and 85-88.
- Read `src/Core/src/Transports/Hid/Keyboard/HidCodeTranslator.ModHex.cs` line 26.
- Read `ykman/_cli/otp.py` lines 548 and 576-585 at `4ca60f7`.

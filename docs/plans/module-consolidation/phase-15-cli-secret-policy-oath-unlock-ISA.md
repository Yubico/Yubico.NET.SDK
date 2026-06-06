# Phase 15 ISA: CLI Secret Policy + OATH Unlock Migration

## Problem

OATH CLI unlock currently accepts passwords as `string` values from `--password` or from `PinPrompt.PromptForPin(...)`, then converts the string with `Encoding.UTF8.GetBytes(...)`. The derived key is zeroed, but the original password string and intermediate UTF-8 array are not owned through a disposable secret lifecycle.

Command-line password arguments are inherently exposed through shell history, process inspection, and automation logs. Phase 15 must decide the program-wide argv secret policy before more CLI credential families migrate.

## Scope

- Preserve `--password <PASSWORD>` for compatibility.
- Emit a minimal warning when an OATH unlock password is supplied through argv.
- Warning text must say argv passwords are inherently insecure and intended only for testing or demos.
- Warning text must provide an interactive-prompt example without claiming the prompt is secure.
- Warning text must go to stderr, not stdout, so OATH account/code output remains scriptable.
- Do not use current `OutputHelpers.WriteWarning(...)` or `WriteError(...)` for the argv warning because those helpers write to stdout via `AnsiConsole.MarkupLine(...)`.
- Migrate exactly one path: OATH session unlock in `OathHelpers.UnlockIfNeededAsync(...)`.
- Switch prompted OATH unlock from `PinPrompt.PromptForPin(...)` to `PinPrompt.PromptForCredential(...)` so owned bytes are zeroed after use.
- The prompted unlock label/newline moving to stderr is accepted because it follows `SecureCredential.Prompt(...)` behavior and avoids stdout pollution.
- Convert argv compatibility input through `SecureCredential.FromUtf8String(...)` at the unlock boundary so the UTF-8 byte copy is owned and zeroed, while explicitly acknowledging the original `string` cannot be zeroed.
- Perform the empty-password prompt fallback before `SecureCredential.FromUtf8String(...)` because `FromUtf8String` rejects empty input.
- Do not leave any ad-hoc `Encoding.UTF8.GetBytes(password)` copy at the OATH unlock boundary.
- Keep derived OATH access keys zeroed in `finally`.
- Dispose the `SecureCredential` in the unlock method so both prompted and argv-derived owned byte buffers are zeroed on success and failure.
- Add only a narrow internal test seam if needed to inject prompted `SecureCredential` input into `UnlockIfNeededAsync(...)`; do not introduce a broad prompt abstraction.

## Out Of Scope

- No removal of `--password`.
- No broad CLI parser/session helper refactor.
- No OATH password change/new-password migration.
- No OATH credential secret or otpauth URI migration.
- No HSM Auth, OpenPGP, PIV, FIDO, or SCP password migration.
- No keyring/remember-password implementation.
- No public SDK API change.

## Approved Policy

Program-wide CLI secret policy for migrated paths:

- Plaintext argv secrets are compatibility behavior only.
- The CLI may keep existing argv secret options, but migrated paths must warn when they are used.
- New or migrated interactive secret paths should prefer owned byte lifetimes through `SecureCredential` or an equivalent disposable byte owner.
- Do not claim an input path is secure. Prefer wording such as "prefer the interactive prompt" and "avoid placing secrets directly in command arguments."
- The warning and owned-byte migration do not remove unavoidable plaintext copies in OS argv or Spectre.Console.Cli settings objects.

Approved warning shape:

```text
Warning: Passing passwords on the command line is inherently insecure and intended only for testing or demos. Prefer the interactive prompt, for example: yk oath accounts list
```

## Goal

OATH unlock remains compatible with existing `--password` usage, warns clearly when argv secrets are used, and uses disposable owned credential bytes for prompted input.

## Criteria

- [ ] ISC-15.1: `--password <PASSWORD>` still unlocks OATH sessions for existing commands.
- [ ] ISC-15.2: argv password unlock emits one minimal warning at the OATH unlock boundary.
- [ ] ISC-15.3: warning says command-line passwords are inherently insecure and intended only for testing or demos.
- [ ] ISC-15.4: warning provides an interactive-prompt example.
- [ ] ISC-15.4a: warning does not call any alternative secure or safe.
- [ ] ISC-15.5: prompted OATH unlock uses `SecureCredential`/owned bytes rather than `string`.
- [ ] ISC-15.5a: prompted OATH unlock calls `PinPrompt.PromptForCredential(...)`, not `PinPrompt.PromptForPin(...)`.
- [ ] ISC-15.6: the derived OATH access key is zeroed in `finally` on success and failure.
- [ ] ISC-15.7: argv compatibility input is converted to an owned credential only at the unlock boundary, with explicit acknowledgement that the original `string` cannot be zeroed.
- [ ] ISC-15.7a: the argv-derived UTF-8 byte copy is owned by `SecureCredential` and zeroed on dispose.
- [ ] ISC-15.7b: warning detection is based on non-empty password on method entry, before any prompt fallback, so prompted users and empty values routed to prompt do not receive argv warnings.
- [ ] ISC-15.7c: warning text is a single source constant used by implementation and tests.
- [ ] ISC-15.7d: warning is written directly to stderr, for example with `Console.Error.WriteLine(...)`, and does not use current stdout-based `OutputHelpers` methods.
- [ ] ISC-15.7e: warning example matches the registered CLI application/verb path: `yk oath accounts list`.
- [ ] ISC-15.7f: implementation keeps using `IOathSession.DeriveKey(ReadOnlyMemory<byte>)` and does not add new OATH SDK API overloads.
- [ ] ISC-15.7g: the owned `SecureCredential` is disposed in the unlock method on success and failure.
- [ ] ISC-15.7h: if prompted unlock tests need injection, the test seam is narrow and internal to OATH helper testing.
- [ ] ISC-15.7i: empty password values follow the prompt fallback before `SecureCredential.FromUtf8String(...)` can throw.
- [ ] ISC-15.7j: no `Encoding.UTF8.GetBytes(password)` copy remains at the OATH unlock boundary.
- [ ] ISC-15.7k: argv warning is emitted exactly once per `UnlockIfNeededAsync(...)` invocation that receives a non-empty argv password.
- [ ] ISC-15.8: migration is limited to OATH unlock; password change, new-password, credential secrets, otpauth URI, and other modules remain unchanged.
- [ ] ISC-15.9: tests cover argv warning behavior and prompted/owned credential unlock behavior where feasible without hardware.
- [ ] ISC-15.10: verification uses focused CLI/OATH build and unit test commands only; no hardware integration is required.
- [ ] ISC-15.11: no operation-specific command classes or broad helper layers are introduced.

## Likely Files

- `src/Cli.Commands/src/Oath/OathHelpers.cs`
- `src/Cli.Commands/src/Oath/OathCommands.cs` only if call sites need a signature adjustment
- `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Oath/OathHelpersTests.cs`
- `src/Cli.Shared/src/Output/SecureCredential.cs` only if a small test seam is needed
- `src/Cli.Shared/tests/Yubico.YubiKit.Cli.Shared.UnitTests/Output/SecureCredentialTests.cs` only if the prompt/zeroing seam changes

## Test Strategy

- Unit test an argv password unlock against a fake `IOathSession`, asserting validation succeeds and warning output includes the approved language.
- Unit test the argv warning is emitted to stderr and is not emitted for prompted/owned credential input.
- Unit test stdout is not polluted by the argv warning.
- Unit test empty password input follows the prompt/owned-credential branch and does not emit the argv warning.
- Unit test the warning text comes from a single source constant.
- Unit test the warning text does not include `secure` or `safe` as a claim about the alternative.
- Unit test a prompted/owned-credential unlock path if it can be done without broad console plumbing.
- Unit test wrong password/failure path still zeros derived key where observable through the fake session.
- Prefer tests on `OathHelpers` over command parser tests because this phase owns the unlock boundary, not parser behavior.

## Planned Verification

- `dotnet toolchain.cs -- build --project Cli.Commands`
- `dotnet toolchain.cs -- build --project Cli.Shared`
- `dotnet toolchain.cs -- test --project Cli.Commands --filter "FullyQualifiedName~OathHelpersTests"`
- `dotnet toolchain.cs -- test --project Cli.Shared --filter "FullyQualifiedName~SecureCredentialTests"` if `Cli.Shared` changes
- `dotnet format --verify-no-changes --include <Phase 15 touched files>`
- `git diff --check`

## Integration Scope

No hardware integration tests are planned for Phase 15. The phase changes CLI input handling and unlock-boundary memory ownership. Hardware OATH unlock tests would require a password-protected applet state and could mutate or depend on persistent state, so they are out of agent-runnable scope.

## Review Plan

- Run Cato against this ISA before implementation.
- Run DevTeam implementation/review loop after Cato findings are incorporated.
- Cross-vendor reviewer route must use Vertex Opus 4.8 because the active primary is OpenAI GPT-5.5.

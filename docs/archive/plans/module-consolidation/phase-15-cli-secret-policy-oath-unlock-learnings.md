# Phase 15 Learnings: CLI Secret Policy + OATH Unlock Migration

Use this note as the handoff record for Phase 15 of module consolidation.

## Phase Summary

- Branch: `yubikit-consolidation`
- Scope: preserve OATH `--password` compatibility, warn on argv password use, and migrate OATH unlock password bytes to `SecureCredential`
- Phase ISA: `docs/plans/module-consolidation/phase-15-cli-secret-policy-oath-unlock-ISA.md`
- Human approval: preserve `--password <PASSWORD>` for compatibility, but warn that it is inherently insecure and intended only for testing or demos
- Source files changed: OATH CLI helper and Cli.Commands friend-assembly declaration for tests
- Test files changed: OATH CLI helper unit tests
- Integration tests: skipped; hardware OATH unlock would require password-protected applet state and is not agent-runnable in this phase
- Result: implementation verified by focused builds, focused tests, formatting, whitespace check, Cato plan audit, and DevTeam cross-vendor review
- Commit: recorded by the Phase 15 commit containing this learning note
- `/Ping` status: pending

## What Changed

- `OathHelpers.UnlockIfNeededAsync(IOathSession, string?)` keeps its public signature for existing call sites.
- Non-empty argv password input now emits a minimal warning directly to `Console.Error`.
- Warning text is centralized in `OathHelpers.ArgvPasswordWarning` and includes the registered example command `yk oath accounts list`.
- Empty or null password input goes through the prompted path and does not emit the argv warning.
- Prompted OATH unlock now uses `PinPrompt.PromptForCredential(...)`, which returns `SecureCredential` owned UTF-8 bytes instead of a `string`.
- Argv compatibility input is converted at the unlock boundary through `SecureCredential.FromUtf8String(...)` so the UTF-8 copy is owned and zeroed, while the original argv string remains unzeroable.
- `IOathSession.DeriveKey(ReadOnlyMemory<byte>)` receives `SecureCredential.Memory`; no OATH SDK API overload was added.
- The derived OATH access key is zeroed in `finally`.
- The owned `SecureCredential` is disposed in `finally` on success and failure.
- A narrow internal overload accepts `Func<SecureCredential>` for unit tests; no broad prompt abstraction was introduced.

## Why This Shape

- Removing `--password` would be a CLI behavior break, so the phase preserves compatibility and makes the risk visible.
- The warning belongs at the OATH unlock boundary because all relevant OATH account operations already funnel through `UnlockIfNeededAsync(...)`.
- `Console.Error.WriteLine(...)` is used instead of `OutputHelpers.WriteWarning(...)` because current `OutputHelpers` methods write to stdout via `AnsiConsole.MarkupLine(...)`, and OATH list/code stdout should remain scriptable.
- `SecureCredential.FromUtf8String(...)` cannot make argv safe, but it does eliminate the previous unowned `Encoding.UTF8.GetBytes(password)` copy at the unlock boundary.
- The narrow internal seam keeps tests deterministic without turning prompting into a broad CLI service layer.

## Verification Evidence

- Branch check command: `git status --short --branch`
- Branch check result: `## yubikit-consolidation`; unrelated untracked `src/Core/src/YubiKey/Weird stuff:.md` was present and left unstaged.
- RED command: `dotnet toolchain.cs -- test --project Cli.Commands --filter "FullyQualifiedName~OathHelpersTests"`
- RED result: failed before production changes because `ArgvPasswordWarning` and prompt-injection overload did not exist.
- Build command: `dotnet toolchain.cs -- build --project Cli.Commands`
- Build result: passed, 0 warnings, 0 errors.
- Build command: `dotnet toolchain.cs -- build --project Cli.Shared`
- Build result: passed, 0 warnings, 0 errors.
- Focused unit command: `dotnet toolchain.cs -- test --project Cli.Commands --filter "FullyQualifiedName~OathHelpersTests"`
- Focused unit result: passed, 5 succeeded.
- Focused shared unit command: `dotnet toolchain.cs -- test --project Cli.Shared --filter "FullyQualifiedName~SecureCredentialTests"`
- Focused shared unit result: passed, 5 succeeded.
- Format command: `dotnet format --verify-no-changes --include src/Cli.Commands/src/Oath/OathHelpers.cs src/Cli.Commands/src/Properties/AssemblyInfo.cs src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Oath/OathHelpersTests.cs`
- Format result: passed after `dotnet format --include ...` normalized final-newline style.
- Whitespace command: `git diff --check`
- Whitespace result: passed.

## Integration Lifecycle

- Hardware target: not used.
- Management preflight: not applicable.
- Integration scope was read-only: not applicable.
- Tests run: none.
- Tests skipped: OATH hardware unlock checks.
- Skip reason: meaningful hardware coverage would require a password-protected OATH applet state and could depend on or mutate persistent state.
- Persistent state changed: no.
- Destructive tests skipped completely: yes.
- Reset/cleanup performed: none.

## Review Evidence

- Cato route: Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`.
- Cato outputs:
- `/tmp/opencode/cato-phase15-plan-audit-r2.jsonl`
- `/tmp/opencode/cato-phase15-plan-audit-r3.jsonl`
- `/tmp/opencode/cato-phase15-plan-audit-r4.jsonl`
- `/tmp/opencode/cato-phase15-plan-audit-r5.jsonl`
- Cato findings incorporated: argv UTF-8 copy must be owned/zeroed; warning must go to stderr; warning must only fire for non-empty entry password; warning text should be single-source; example command must match registered CLI path; prompt should switch to `PinPrompt.PromptForCredential(...)`; no ad-hoc `Encoding.UTF8.GetBytes(password)` copy should remain.
- DevTeam reviewer route: Vertex Opus 4.8 via `google-vertex-anthropic/claude-opus-4-8@default`.
- DevTeam output: `/tmp/opencode/devteam-phase15-review.jsonl`
- DevTeam verdict: pass.
- DevTeam info findings: optional empty-prompt UX message, prompted buffer zeroing test hardening, unrelated untracked Windows-illegal scratch file.
- Findings fixed after review: added explicit prompted `SecureCredential` buffer zeroing assertions.
- Findings deferred: empty-prompt UX wording remains acceptable because the migrated path still returns failure and Phase 15 did not own prompt UX taxonomy.

## Deferred Future Improvements

- Consider a future CLI-wide input-policy phase for non-argv automation input paths. Do not call those alternatives secure; frame them as avoiding direct argv secrets.
- Consider moving one-off stderr warning behavior into a small CLI.Shared helper only after at least one more credential family needs the same behavior.
- Consider whether `SecureCredential` should expose a safer test-only owned-buffer inspection hook instead of reflection in tests, but avoid adding that unless more tests need it.

## Cross-Module Implications

- Modules likely affected later: PIV, FIDO, OpenPGP, HSM Auth, SCP credential-file password handling, and any CLI path accepting secrets through argv.
- Next module should copy: preserve compatibility first, warn at the narrow boundary, use owned bytes for migrated prompted input, and avoid overclaiming security.
- Next module should avoid: broad parser refactors, claiming prompt/stdin is secure, or treating argv string zeroing as possible in C#.
- Potential API compatibility concern: none for OATH SDK APIs; CLI.Commands public helper signature was preserved.

## Generalization Check

- Pattern classification: candidate for one more CLI credential-family trial.
- Reusable lesson: for C# CLI secrets, distinguish unfixable argv/string exposure from fixable owned byte-copy hygiene.
- Not promoted to shared code: stderr warning helper and prompt injection remain local because only OATH unlock has migrated under this policy.

## Compact Summary

- Goal: warn on OATH argv passwords and migrate OATH unlock bytes to owned disposable credentials.
- Files changed: OATH CLI helper, Cli.Commands friend assembly, OATH helper tests, Phase 15 ISA, this learning note.
- Final pattern: preserve `--password`, warn on stderr, use `SecureCredential` for prompted and argv-derived UTF-8 copies, zero key and credential in `finally`.
- Rejected approaches: removing `--password`, broad CLI parser refactor, claiming any alternative is secure.
- Tests passed: Cli.Commands focused OATH helper tests and Cli.Shared SecureCredential tests.
- Integration lifecycle: skipped; hardware unlock state is persistent-state-dependent.
- Shared/Core candidates: no promotion this phase; warning helper may be revisited after another module migrates.
- House-style update needed: none now.
- Next phase recommendation: Phase 16 API And Package Compatibility Checkpoint.
- Learning note path: `docs/plans/module-consolidation/phase-15-cli-secret-policy-oath-unlock-learnings.md`
- Commit: recorded by Phase 15 commit.
- `/Ping` status: pending

# Wave2 bounded OATH input fixes — review handoff

## State and boundaries

- Exclusive worktree: `/tmp/opencode/yubikit-oath-fix-inputs`.
- Branch: `yubikit-oath-fix-inputs`; HEAD remains `c90dced180de7975895771b32fcc91b9df752591`.
- Session moved to the exclusive worktree before reading/editing repository files.
- No commits, pushes, delegation, hardware operations, audit-ledger changes, root solution/project wiring, shared harness changes, or shared Markdown changes.
- Ready for coordinator review. Full-solution gates and independent review remain coordinator work.

## Exact repository files

New:

1. `src/Core/src/Utilities/Base32.cs`
2. `src/Core/tests/Yubico.YubiKit.Core.UnitTests/Utilities/Base32Tests.cs`
3. `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathInputTests.cs`

Modified:

4. `src/Core/src/PublicAPI.Unshipped.txt`
5. `src/Oath/src/CredentialData.cs`
6. `src/Oath/src/Credential.cs` — request formatting only; response parsing, equality and ordering unchanged.
7. `src/Oath/src/OathSession.cs` — precisely one added `credentialData.ValidateRequest()` before ID/secret processing in PUT; no reset/auth/calculation edits. Rename uses the strengthened existing shared formatter without changing its wire flow.
8. `src/Oath/examples/OathTool/Commands/AccountsCommand.cs` — decoder only.
9. `src/Cli.Commands/src/Oath/OathHelpers.cs` — decoder only.
10. `src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/OathToolCancellationTests.cs` — adds decoder/boundary tests and local fake capture, no shared harness edits.
11. `src/Cli.Commands/tests/Yubico.YubiKit.Cli.Commands.UnitTests/Oath/OathHelpersTests.cs`

`OathCommands.cs` has no final diff: its existing call to `OathHelpers.Base32Decode` now rejects absent material at that OATH boundary. All other validation after credential construction is enclosed by existing disposal scopes.

## Decisions and evidence

### IMP005 / remaining IMP004

- One small Core decoder, public static `Base32`, with only `Decode(ReadOnlySpan<char>) -> byte[]` and `Decode(ReadOnlySpan<char>, Span<byte>) -> int`. API snapshot updated; no IVT or encoding framework.
- All three previous decoder implementations now delegate to Core.
- Exact RFC4648 `f`, `fo`, `foo`, `foob`, `fooba`, `foobar` vectors, padded/unpadded, plus empty generic input, ASCII case and literal spaces.
- Valid terminal symbol counts modulo eight: 0, 2, 4, 5, 7. Supplied padding must be complete (0, 6, 4, 3, 1 respectively); impossible lengths, all-padding, extra/missing padding, embedded padding, invalid late ASCII/non-ASCII characters, tab and newline rejected.
- Preserves permissive unused pad bits (`MZ` decodes to `f`); no new canonical-pad-bit policy.
- No uppercase/space-removal/padding string copies. Generic empty input remains valid, while URI factory, PUT request, OathTool decoder boundary, and Cli.Commands OATH decoder boundary reject absent secret material. Nonempty one-byte secrets are accepted and existing 14-byte padding is retained; no entropy requirement introduced.
- Span overload clears the entire caller-visible destination on any decode failure, including late invalid characters/padding and too-small output after a written prefix. Success preserves unused destination bytes. The array overload allocates escaping owned storage and delegates to that cleanup-owning span overload.
- Runtime sensitivity check temporarily removed destination clearing, producing 14 real cleanup assertion failures; clearing restored and full Core green. This is supplementary regression-test discrimination, not a claim that upstream cleanup was absent.
- SDK URI non-secret numeric/identity validation happens before decoding. Temporary decoded bytes remain in `finally`; private-copy/borrowed-view/PUT seed ownership (IMP003) unchanged.

### IMP006

- Formatter rejects empty names, empty non-null issuer, nonpositive TOTP periods, total IDs exceeding 64 UTF-8 bytes, or metadata changed by parsing the formatted bytes through the existing host convention.
- Period formatting uses invariant culture.
- No escaping invented, no blanket colon/slash restriction. Leading-colon names without issuer, colon-containing names under an ordinary issuer, and numeric-slash HOTP names remain representable. Exact 64-byte rename IDs and 66-byte UTF-8 rejection tested.
- HOTP ignores period (including default 30 and negative period); comparison expects the parser's HOTP period zero, not the supplied ignored value.
- Incoming raw-ID parser and invalid-period omission policy are untouched.
- Fake wire tests assert invalid PUT/rename performs SELECT only, and invalid PUT does not change caller bytes or borrowed owned seed. Successful rename leaves original identity unchanged.

### IMP007

- PUT rejects digits outside 6..8 before int-to-byte encoding; raw seven-digit support preserved.
- TOTP period must be positive; HOTP counter must be nonnegative. Irrelevant counter/period retains existing ignored behavior.
- Public Counter remains int with documented supported range 0..Int32.MaxValue. Exact max IMF bytes `7F FF FF FF` verified; zero/missing HOTP counter still omits IMF/defaults zero.
- URI issuer query precedence and existing URI extensions preserved. No broader DEC003 conformance change.

## Source comparison

- Read actual `origin/develop:Yubico.Core/src/Yubico/Core/Buffers/Base32.cs` via `git show`: its explicitly permissive padding comment is not reused as the new shape contract.
- Read Python sibling `/home/rupicola/Code/y/yubikey-manager/yubikit/oath.py`, including `parse_b32_key`, URI parsing, identity formatting, PUT and rename. Python normalizes case/spaces then uses base64.b32decode; PUT uses key type/digits then secret, raw touch property, big-endian four-byte IMF. The bounded implementation preserves existing C# wire ordering, int API range and ignored-field policies rather than incidentally porting Python behavior wholesale.

## RED / GREEN commands and durable logs

All commands ran in the exclusive worktree and used the toolchain wrapper. Each evidence directory contains complete copied run directories (`run.log`, `result.json`, step logs), archived before the five-run retention policy could remove required evidence.

| Phase | Exact command | Result | Durable run directory |
|---|---|---|---|
| Behavioral RED | `dotnet toolchain.cs -- test --project Oath` | 375 executed, 40 failed, 0 skipped; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-initial/20261103-000001-524-test/` |
| Behavioral RED | `dotnet toolchain.cs -- test --project Cli.Commands` | 39 executed, 6 failed, 1 existing skip; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-initial/20261103-000020-459-test/` |
| Missing API checkpoint, NOT behavioral RED | `dotnet toolchain.cs -- test --project Core --filter FullyQualifiedName~Base32Tests` | CS0103: Core Base32 does not yet exist; no tests executed | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-initial/20261103-000051-437-test/` |
| First codec GREEN | `dotnet toolchain.cs -- test --project Core --filter FullyQualifiedName~Base32Tests` | 23 executed, 0 failed/skipped; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-initial/20261103-000141-463-test/` |
| Empty OATH boundary RED | `dotnet toolchain.cs -- test --project Cli.Commands` | 41 executed, 2 failed, 1 existing skip; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-boundaries/20261103-000251-284-test/` |
| Cleanup sensitivity RED | `dotnet toolchain.cs -- test --project Core --filter FullyQualifiedName~Base32Tests` | 23 executed, 14 cleanup failures, 0 skipped; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-boundaries/20261103-000319-887-test/` |
| Final GREEN | `dotnet toolchain.cs -- test --project Core` | 1,739 executed, 0 failed, 4 existing skips; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-final/20261103-000618-145-test/` |
| Final GREEN | `dotnet toolchain.cs -- test --project PublicApi` | 24 executed, 0 failed/skipped; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-final/20261103-000651-663-test/` |
| Final GREEN | `dotnet toolchain.cs -- test --project Oath` | 386 executed, 0 failed/skipped; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-final/20261103-000752-668-test/` |
| Final GREEN | `dotnet toolchain.cs -- test --project Cli.Commands` | 42 executed, 0 failed, 1 existing skip; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-final/20261103-000758-337-test/` |
| Complexity | `dotnet toolchain.cs complexity` | 0 new/worsened violations; ParseUri unchanged existing 19/14; warnings 0 | `/tmp/opencode/yubikit-oath-fix-inputs-evidence-final/20261103-000807-254-complexity/` |

Final totals: **2,191 executed, zero failures, five existing skips**. No new skipped/placeholder tests. Existing Core skips are Windows listener, two SCP11 cases and an audit child-body case; Cli.Commands skip is an existing audit child-body case.

Scoped `dotnet format Yubico.YubiKit.sln --include <the ten edited/new C# files>` applied repository EOF conventions. The final same-scoped `--verify-no-changes --verbosity diagnostic` exited zero and reported `Formatted 0 of 1371 files`; full diagnostic output is `/tmp/opencode/yubikit-oath-fix-inputs-format.log`. Earlier format application emitted generic workspace-load warning text, but final diagnostic verification contains no warning. `git diff --check` passes. No root project or solution changes.

## Documentation and policy followups for coordinator

- Update OATH README, IOathSession PUT/rename XML, and contributor docs to explain outgoing identity round-trip checks and complete 64-byte UTF-8 ID budget, not blanket colon restrictions.
- Update Base32 conformance/support prose: ASCII case/literal spaces, optional but complete padding, terminal-length rejection, permissive pad bits, and empty generic codec versus nonempty OATH material distinction.
- Add Core README/CLAUDE mention of the small shared decoder and caller-output cleanup contract; no generic encoding architecture implied.
- Update numeric and ParseUri usage/migration notes: 6..8 digits, positive TOTP period, nonnegative Int32 HOTP counter; unsupported wider counter API remains a separate proposal.
- Owned CredentialData XML has been updated. Decoder and public Core XML describe output ownership/cleanup.
- DEC003 remains open beyond this bounded scope: missing counter default, issuer precedence and extra representable colons remain preserved. A future canonical nonzero-pad-bit rejection policy, uint/wider counter API and DEC004 padding provenance are not resolved here.
- Full solution build/test gates, independent review and any authorized hardware checks remain coordinator work. This worker performed no hardware operations and makes no device-firmware acceptance claim.

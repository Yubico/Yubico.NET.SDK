# Implementation wave 1

Baseline: `361567ebf6d1507afb474f960f94efe92ae9b802`.
User approved autonomous work using the house style, the Python sibling SDK for firmware checks, and both attached test keys. See `../../implementation-plan.md`.

| Work | Model/session | Isolated branch/worktree |
| --- | --- | --- |
| CLI help, option arity, cancellation (IMP-012/013/002) | Sol6.1 `ses_e61eb4c7fffe2aIFZMwz1BiU4Z` | `yubikit-oath-fix-cli`, `/tmp/opencode/yubikit-oath-fix-cli` |
| Required response parsing and sensitive cleanup (IMP-001/008/009/011/014), selected hardware evidence | Sol6.1 `ses_e61eb4c25ffePGHfPbnG7wRzDE` | `yubikit-oath-fix-protocol`, `/tmp/opencode/yubikit-oath-fix-protocol` |

Both workers received exact ownership boundaries, current repository guidance, the house style, existing findings, TDD/build/test requirements, and a fresh-review checkpoint. Neither may commit/push or recursively delegate. The CLI worker has no hardware access; the protocol worker exclusively owns device operations during this wave. Parent coordination does not access devices while it runs.

Tests must establish expected red behavior before fixes. CLI regression tests use fake execution/bootstrap boundaries, never the defective real reset-help path. Protocol tests use realistic response bytes and existing Core types; hardware evidence records each executed firmware group. No broad architecture refactor is authorized. Ledger updates stay with the coordinator.

## Inventory and reference evidence

- `ykman list` and `ykman --device <serial> info` confirmed125 (5.8.0.alpha.2, Enhanced PIN) and9681620 (5.4.3), both OATH/CCID enabled.
- The committed shared test allow list already includes both serials; no config change was made.
- Python clone is clean at4ca60f706af930459138d8dc0f0f953480e1c7a4; `git rev-parse --is-shallow-repository` is true. Exact source reference lines are recorded in the implementation plan.
- Parent performed only read-only device inventory before dispatch. Subsequent mutation/testing belongs to the protocol worker and must be reported with actual results.

## Case-specific legacy lookup

The coordinator investigated DEC-004's14-byte threshold while implementation work ran. A targeted official-doc search did not explain its origin. `origin/develop` legacy `Yubico.YubiKey/src/Yubico/YubiKey/Oath/Commands/PutCommand.cs` defines `MinimalSecretLength=14` at39 and allocates `2+Math.Max(secretDecoded.Length, MinimalSecretLength)` at90. This corroborates longstanding SDK padding, not the hardware rationale. OTP-slot20-byte requirements were excluded because they concern another application. No broad v1-doc crawl or legacy command architecture was adopted.

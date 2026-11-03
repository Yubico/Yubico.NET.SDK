# Resume checkpoint — 2026-11-03

**Paused at the user's request. No agent jobs remain active. Do not restart work until the user asks to resume.**

## Integrated state

- Repository: `/home/rupicola/Code/y/Yubico.Yubikit.NET`.
- Branch: `yubikit-docs-audit`.
- HEAD: `244df5b1` — reviewed Linux PC/SC documentation.
- Prior integrated implementation: `c90dced1`, containing CLI fix `618adbec` and protocol fix `c79e4c1e`.
- Documentation checkpoint: `7c4153f8`; upstream import: `361567eb`.
- Nothing pushed. Main tracked working tree is clean. Four original untracked user files are untouched; their names are in `state.json`. Do not stage or overwrite them.
- Wave1 closes IMP-001/002/008/009/011/012/013/014 within documented bounds; IMP-003 was closed by upstream. DOC-001 is now completed in244df5b1.
- Integrated code gates:3,828 unit tests passed, five skipped, zero warnings; OATH311. Integration smoke on125:20 executed, five skipped. Revised protocol cases passed separately on5.4.3 and5.8.0.alpha.2. PC/SC documentation has separate peer review and passing docs QA; it changed no runtime source.

## Preserved, unreviewed wave2 work

### Inputs

- Worktree: `/tmp/opencode/yubikit-oath-fix-inputs`.
- Branch: `yubikit-oath-fix-inputs`, base `c90dced1`.
- Author session: `ses_e61838f92ffeeW2F8q9uSMwwKC`.
- 11 changed/new files; not committed or integrated, no independent review yet.
- Shared Core Base32 decoder; three callers migrated; failed-output clearing; empty OATH material rejected; outgoing identity round trips/64-byte limit; numeric request validation. URI extension policy and wider counter API intentionally unchanged.
- Author evidence: Core1,739/OATH386/Cli.Commands42/PublicApi24 passed, zero warnings. Five existing skips across these suites. No hardware run. Missing-public-API compilation checkpoint is explicitly not counted as behavioral RED.
- Detailed handoff: `inputs-handoff.md`; snapshots/complete patch: `inputs/files/`, `inputs/wip.patch`; archived RED/GREEN evidence in `inputs-evidence-*`.

### Lifecycle/time

- Worktree: `/tmp/opencode/yubikit-oath-fix-lifecycle`.
- Branch: `yubikit-oath-fix-lifecycle`, base `c90dced1`.
- Author session: `ses_e61838f51ffeNTj26Cr00wiwSD`.
- 10 changed/new files; not committed or integrated, no independent review yet.
- Nonnegative/overflow-safe timestamp windows; post-successful-RESET refresh failure invalidates the session using Core lifecycle helpers; device-HMAC mismatch becomes BadResponseException and device-rejection wording is neutral.
- Author evidence:344 OATH units passed; OATH projects built warning-free; focused integration5passed/4skipped (three live cases on125 plus two offline cleanup cases). No new9681620 evidence.
- Detailed handoff: `lifecycle-handoff.md`; snapshots/complete patch: `lifecycle/files/`, `lifecycle/wip.patch`; archived logs in `lifecycle-evidence/` and related directories.

## Decisions and leads requiring attention

1. **SCP reset recovery remains incomplete.** Current SessionCreationOptions borrows SCP parameters for initialization. Retaining opaque/disposable private keys would silently extend caller lifetimes. Author proposes fresh borrowed reset parameters and pre-transmission refusal of SCP reset without recovery material; this API proposal is not implemented or approved.
2. **SCP test failure is not proven absent hardware support.** Default C# initialization returned6A88 before RESET. Last source check found C# Default references KID1/KVNFF, whereas the pinned Python default uses KID1/KVN0 and INITIALIZE UPDATE P2=0. The C# wire construction/P2 and alternate-reference behavior have not yet been compared. Check these before attributing failure solely to unavailable credentials or requesting secret material.
3. **DEC-001 is only partially improved in WIP.** Device-HMAC mismatch classification changes, but terminal invalidation after that authentication failure and compatibility enum policy remain open.
4. **CORE-001 remains:** with both keys attached, a bounded HID identity read for9681620 was abandoned and the ownership guard blocked its later use. The same key works alone. Do not bypass the guard; exact Core root cause is not established.
5. Raw-key scope, URI extensions, padding history, and detailed HOTP semantics retain their ledger status. Do not close them from unrelated green test counts.

## Hardware and reference context

- Authorized test keys:125 (5.8.0.alpha.2, SDK normalizes to5.8.0) and9681620 (5.4.3). Both already in committed shared allow list.
- Last verified arrangement:125 connected alone;9681620 unplugged at our request. Recheck on resume, do not assume this remains current.
- User permits OATH testing; shared harness, finite cleanup budgets, and human coordination for touch/UV/power-cycle still apply. No Security Domain configuration/key reset was authorized or performed during this work.
- Python clone: `/home/rupicola/Code/y/yubikey-manager`, clean shallow revision `4ca60f706af930459138d8dc0f0f953480e1c7a4`.
- User direction: use `docs/SDK-HOUSE-STYLE.md`, Python firmware policies, and legacy docs only for specific questions. Preserve flat protocol flow and clear ownership.

## Recommended resume order

1. Verify `state.json`, current Git status and worktree contents. Both WIP patches were checked against their recorded base using temporary indexes; file hashes were verified.
2. Request independent review of the two WIP implementations against their explicit scope and handoffs. Include docs/API ownership and lifecycle/security boundaries, not just test counts.
3. Resolve review findings with RED/GREEN evidence; settle SCP reset API/lifetime separately before claiming that path fixed.
4. Reconcile dependent docs. The input task deliberately leaves shared README/guide updates as handoff work; preserve the PC/SC link introduced in244df5b1 when merging its older base.
5. Run full integration gates on the combined state and hardware cases actually supported by the available setup, then commit/integrate. No push without direction.

If temporary worktrees are lost, create fresh worktrees at the recorded base under new names and apply the corresponding `wip.patch`; exact changed files also exist in `files/`. Do not reset existing branches or delete unfamiliar work to restore the checkpoint.

# OATH follow-up implementation plan

**Current status:** The bounded OATH follow-up is integrated through `76a8ae52`, including reviewed inputs, terminal reset, timestamp/error corrections, and documentation-impact re-review. Combined offline and applicable hardware gates passed with recorded limits. See [wave2 completion](runs/fixes-wave-002/completion.md). Remaining meaningful engineering questions are deferred under the documentation-first scope.

**Scope constraint:** This OATH implementation pass is an explicit exception to a documentation-first campaign. Finish the approved OATH work and re-review its documentation. Defer meaningful behavior changes elsewhere—including cross-application reset consistency—to a separate implementation pass with linked documentation follow-up. Small fixes must remain bounded; no automatic expansion into shared-Core or other applet redesign.

**Goal:** Resolve the confirmed OATH implementation concerns, update dependent contracts, and verify device behavior where hardware evidence is necessary.

**Architecture:** Preserve the flat session/APDU flow in `docs/SDK-HOUSE-STYLE.md`. Prefer existing Core primitives, local parsers and explicit `finally` cleanup; no operation-command hierarchy or general-purpose audit framework. Isolate independent changes in worktrees, review before integration, and rerun affected documentation checks after behavior changes.

**Tech stack:** C#14/.NET10, repository toolchain/xUnit harness, local Python YubiKey Manager reference, two allow-listed physical YubiKeys.

## User direction

- Buffer ownership follows the attached/current SDK house style and root security rules.
- Firmware checks may follow the Python sibling SDK. Record the reference revision and distinguish SDK policy from independently established earliest hardware support.
- The two attached keys are authorized for testing. Existing repository hardware allow-list and presence/timing conventions still apply.
- Consult v1 documentation at `https://docs.yubico.com/yesdk/users-manual/intro.html` only for a specific unresolved question; do not eagerly treat legacy API shape as v2 design.
- Keep check-ins between implementation, independent review, integration, and verification.

## Baseline and resources

- Branch baseline: `361567ebf6d1507afb474f960f94efe92ae9b802` (`yubikit-docs-audit`, after upstream reconciliation).
- Python clone: `/home/rupicola/Code/y/yubikey-manager`, clean main at `4ca60f706af930459138d8dc0f0f953480e1c7a4`. The clone's current history is shallow; `git blame` cannot establish the original rationale from this snapshot alone.
- Key125: YubiKey5 NFC Enhanced PIN, firmware **5.8.0.alpha.2**, OATH/CCID enabled.
- Key9681620: YubiKey5 NFC, firmware **5.4.3**, OATH/CCID enabled.
- Both serials already occur in committed `src/Tests.Shared/appsettings.json`. No authorization/configuration changes are needed.
- Read-only `ykman list` and per-device `info` confirmed inventory. No OATH mutation has been performed at this preparation stage.

## Reference conclusions already established

At the pinned Python revision, `yubikit/oath.py`:

- Lines279–282 reject SCP on YubiKey5 versions `5.0.0 <= version < 5.6.3`; rename requires5.3.1 at438.
- Full calculation returns the value after its digit header (`resp[1:]`,463–480).
- Device verification mismatch raises `BadResponseError` (333–349).
- Reset reselects and re-establishes SCP if retained parameters exist (316–324).
- The storage padding threshold is14 bytes; the snapshot establishes practice, not its original hardware rationale.

These are reference evidence, not permission to copy Python memory ownership, parser weaknesses, or integer behavior into C# without checking applicability.

## Wave 1 — independent fixes

### A. CLI dispatch and cancellation

- Worktree: `/tmp/opencode/yubikit-oath-fix-cli`.
- Branch: `yubikit-oath-fix-cli`.
- Findings: IMP-012, IMP-013, IMP-002.
- Ownership: `src/Oath/examples/OathTool/**`; narrowly scoped new CLI regression tests and unit-project wiring if necessary. No SDK session/parser changes, Base32 rewrite, shared-harness changes, or physical-device use.
- Prove red before implementation: supported help cannot invoke discovery/mutation, boolean flags do not consume positionals, and cancellation reaches operations or a proper user-visible CLI cancellation result with cleanup.
- Tests must use fake dispatch/bootstrap boundaries. Never invoke the defective real `reset --help --force` path to demonstrate red while physical devices are attached.
- Keep helpers local and small. Do not introduce a general CLI framework or a new SDK command layer.

### B. Protocol responses, cleanup, and initial device evidence

- Worktree: `/tmp/opencode/yubikit-oath-fix-protocol`.
- Branch: `yubikit-oath-fix-protocol`.
- Findings: IMP-001, IMP-008, IMP-009, IMP-011, IMP-014. Gather device evidence for DEC-001/006 where feasible; avoid deciding unsupported semantics by assumption.
- Ownership: OATH session/backend response paths and related unit/integration tests. No CLI, CredentialData/Base32, public scalar-type changes, or general Core refactor.
- Prove red with protocol-shaped fake responses and observable cleanup where practical; then make minimal fixes.
- Preserve valid extension handling. Validate required metadata and response structure without inventing salt lengths or hard-coding one digest algorithm. Keep raw command bytes and cleanup visible.
- This worker exclusively owns hardware use during the wave. Use only the two authorized keys via shared test helpers, serializing device operations. Coordinate before any touch/presence-dependent run.
- Record which device/firmware executed each case. A default WithYubiKey row binds to one matching device, not all devices; explicitly arrange coverage of the available firmware groups.
- Prioritize correct full-HMAC known vectors and normal OATH lifecycle on both keys. Use non-touch cases for unattended characterization; a skipped test is not evidence.

## Later waves

After wave1 review/integration, assign non-overlapping work for:

1. Base32 shape across SDK/OathTool/Cli.Commands and meaningful remaining failure-cleanup regressions (IMP-005/004).
2. Creation ID/numeric boundaries (IMP-006/007), preserving explicit distinction between required correctness and unsettled URI-extension policy.
3. Reset invalidation/recovery (IMP-010), timestamp bounds (DEC-005), and remaining device-backed authentication/HOTP details.
4. Targeted Linux PC/SC guidance and outstanding provenance/contract questions.

IMP-003 remains closed by upstream; do not reimplement it. DEC-002 raw-key API scope and DEC-003 URI extension policy require a focused recommendation if implementation choices materially broaden or restrict their contract. The user's Python-firmware direction supplies the support-policy reference for DEC-007; record exact applicability rather than claiming hardware history was proved.

## Verification and integration

- Workers load TDD/build/test skills and use the repository wrappers. Observe expected red failures before implementing; do not create guard-only or skipped placeholder tests.
- Run only affected tests during development; record commands, result blocks, warning counts and real device execution.
- Fresh Sol review checks code, tests, ownership, failure paths and house style; Astra adjudicates security/protocol uncertainty.
- Workers do not commit/push until review and coordinator integration direction. Preserve unrelated work and keep each branch's file ownership explicit.
- At integration, run full required gates warning-free and applicable OATH hardware checks. Re-review documentation whose supporting behavior changed.
- Update the issue register with actual implementation/test evidence, not intention or test counts alone.

# Wave2 independent review — round1

## Input validation

Reviewer: fresh Sol6.1 `ses_e5ed45777ffeAylwNNavvyvzmi`, read-only source and archived-evidence review againstc90dced1 including new files. Assessment: **Needs fixes**, one Important issue, no Critical findings.

- Public span Base32 decoding does not define/check overlapping source and destination. A byte destination beginning at offset8 of UTF16 `MZXW6YTB` overwrites unread input, then failure cleanup clears part of that borrowed input. Reviewer supplied a concrete trace; coordinator inspected the actual read/write/catch flow and accepted the finding.
- Chosen correction: reject overlapping byte ranges before mutation/cleanup admission; document unchanged storage for that rejection. Disjoint decode failures retain full destination clearing. RED/GREEN tests must establish these distinct ownership outcomes.
- Existing tests/source support RFC terminal forms, shared decoding, outgoing ID round trips, supported numeric ranges and no-wire invalid request checks. Private abandoned array/ParseUri cleanup is supported by delegation/finally, not direct observation of those private arrays. Do not overstate the span cleanup test evidence.
- Minor valuable gaps: exact64byte Unicode success, isolated surrogate rejection, explicit30/demo ambiguity, and independent original-value assertions for caller/seed preservation.
- Planned doc reconciliation: input/rename XML, Oath README/CLAUDE ID budget and round trips, Core decoder/cleanup docs, requireTouch ParseUri exception XML and CLI seven-digit text.

Correction worker: Sol6.1 `ses_e5ed1baf8ffe6rXcSd9j07efvx`, inputs worktree only, no hardware/commits/delegation. Documentation scope excludes lifecycle/reset/time/auth behavior. Re-review required after corrections.

## Lifecycle/time

Fresh Sol6.1 `ses_e5ed3ee3effeKEyjKnp7jR2e25` returned Needs fixes: introduced hardware-test double-disposal can replay teardown failure and mask the primary operation error; the known preexisting SCP-reset stale readiness/authentication gap remains open. Timestamp arithmetic and sequential non-SCP refresh-failure invalidation passed source review.

Correction worker `ses_e5ed0d2b6ffeDK472Vbn3dECpx` removed the duplicate disposal owner, enclosed the full acquired lifetime, and covered operation/teardown/cleanup error precedence with seven offline cases using real session ownership. Archived REDs caught replay and secondary-cleanup masking; reported344unit+9offlineintegration passed,zero warnings. SCP diagnostics now describe6A88 cause as unresolved; neutral exception summary corrected. No SCP runtime/API behavior changed.

Both input and lifecycle correction passes are now in independent re-review with their original reviewers. Input worker reports17totalfiles, Core1743/Oath394/Cli42/PublicApi24 passed,zero warnings; exact logs under `/tmp/opencode/yubikit-oath-fix-inputs-review-*`. Lifecycle logs under `/tmp/opencode/oath-lifecycle-correction-*`. These are author results pending re-review and combined gates.

## Independent SCP probe

Sol6.1 `ses_e5ed23b4fffe5Q8yS1qxXKha0z` owns hardware and a new isolated worktree `/tmp/opencode/yubikit-oath-scp-reference-probe`, branch `yubikit-oath-scp-reference-probe` at244df5b1. Scope is SELECT OATH and INITIALIZE UPDATE for KVNFF/00 in fresh sessions, without external authentication or destructive operations. Record only public header/status/length/selected-version metadata, never cryptograms. All other agents have no hardware access. See `scp-reference-investigation.md` for source evidence and limits.

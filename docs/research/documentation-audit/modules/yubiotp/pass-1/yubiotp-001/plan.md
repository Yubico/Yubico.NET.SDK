# YubiOTP Pass-1 Documentation Improvement Plan

**Goal:** Enable consumers to complete YubiOTP tasks with accurate, useful documentation, examples and contracts.

**Architecture:** One coherent module investigation, fresh editing, independent content/citation review,
coordinator sampling/validation and fresh final evidence review. Record implementation findings for the
campaign's second sweep and Core dependencies for the final Core cycle.

**Tech Stack:** .NET 10 / C# 14; SmartCard and OTP HID; YubiOTP slot configuration/challenge-response/NDEF.

## Authority and baseline

User approved this plan and execution after explicitly authorizing any tests, including integration and
one-off tests, against the connected YubiKey. Readable device identity/state and test effects must be
established before mutation; touch/power-cycle timing requires human coordination. Hardware results are
device/firmware/transport/OS scoped. Slot secrets are not generally readable for restoration.

Branch `yubikit-docs-audit`, published baseline `95e5ccaeda33954c21f2a38058d4f66b5f208787`.
Preserve four pre-existing untracked files: docs/research/audit-v2-closeout-review.md,
docs/research/audit-v2-execution.md, docs/research/audit-v2-jira-traceability.md, mise.toml.
Root/module/test instructions, campaign workflow.md and records/style.md govern. The campaign-local
plan follows the established run layout. Planning scout `ses_e5a0d87c8ffeEKQMdvqYe8cToR` was read-only;
its preliminary counts must be independently reconciled in execution.

## Steps and ownership

1. **Baseline/inventory — Astra.** Record all63 tracked module paths and preserved user/archive hashes;
   inventory active shared-document references narrowly. Preliminary surface:24 public types/193 snapshot
   entries, including generated/duplicate/protected entries, not193 callable methods.
2. **Investigation — fresh Sol6.1.** Read complete semantic units across all23 library C#,14 OtpTool C#,
   9unit/5integration C#,4Markdown,4projects,2snapshots,2runner files. Cover public contracts, guides,
   internal/test/example explanations, unfinished markers and meaningful specifications. Nominate clean
   semantic units. Report candidate IDs, local implementation evidence, intent gaps and doc dependencies.
3. **Citations/triage — Sol/Astra.** Exact title/revision/status/section/actor/normative strength/feature
   applicability. Original vendor/standards passages; Context7 for API/framework contracts. Initial
   research plus one focused follow-up, then precise gap or decision. Astra adjudicates consequential
   claims, assigns OTP-IMP-* / OTP-DEP-* IDs and documentation-reopening triggers.
4. **Fresh editor — Sol6.1.** One owner for accepted module docs/comments and explicitly assigned shared
   sections. Improve task flow and local XML together using approved voice. Preserve useful rationale.
   No runtime behavior, assertion, API snapshot or lifecycle-policy changes. Classify full snippets and
   retain typed prerequisites, complete resource scopes and change-to-finding mapping.
5. **Independent content/citation review — fresh Sol6.1.** Complete affected sections, original local
   source and consequential original citations. Required factual/usefulness changes versus optional
   preferences; editor corrects, reviewer rechecks. Bind acceptance to exact hashes.
6. **Sampling/reconciliation — Astra.** At least10% eligible clean units, minimum3 where available across
   API/narrative/internal-test categories. Expand category after material miss. Remove edited-overlap
   nominations from final clean counts; reconcile all claim dependencies after late evidence.
7. **Validation/hardware — Astra.** Serialize toolchain and device operations. Compile complete meaningful
   examples in genuine consumer/OtpTool/test references and analyzer contexts, retain original bodies,
   wrappers, Csc membership/provenance and failures. Scope format to actual solution files, prove token/
   directive preservation and explicitly account harmless import changes. Compare all API snapshots.
   Run clean full-solution build/unit tests, docs/package QA, architecture and complexity gates with zero
   compiler/analyzer warnings. Inspect integration setup/effects, identify connected authorized device,
   run applicable YubiOTP integration tests and focused probes, coordinate touch if needed, retain actual
   transport/firmware/result/cleanup evidence. Feed consequential hardware findings back into docs/review.
8. **Final evidence review — fresh Sol6.1.** Independently reconcile coverage, findings, review/correction
   hashes, sampling, compiler and hardware provenance, original failures, preserved archives and current
   status. Compact record review, not a repeat whole-module investigation.
9. **Publication — Astra.** Resolve required record findings, freeze hash/size manifest, explicitly stage
   owned paths, commit/push campaign branch, verify remote HEAD, publish separate external checkpoint.

## YubiOTP-specific focus

Slot replacement/update/access-code/protection semantics; mode-specific flags and configuration ownership;
HMAC input versus64-byte transmitted payload versus device-hashed message (empty/trailing-zero/63/64-byte
boundaries/collisions); Yubico OTP AES challenge-response distinction; firmware/status gates and NEO
initialization; SmartCard programming sequence versus HID CRC; touch/timeout/cancellation/response
transfer; NDEF URI/text UTF8 limits and null/empty/disable semantics; actual OtpTool and test effects.

## Scope and records

Whole-module Pass1 first; Pass2 waits until all remaining modules finish. Core remains final; no Core
investigation or dedicated boundary-review assignment. Record precise inherited questions. External
link checks use target existence, not Core/other-module document content. Ordinary solution compilation
and test infrastructure use do not constitute Core audit. Breaking release; backward API compatibility
does not constrain eventual Pass2 decisions. Hardware authorization does not authorize runtime fixes.

Run records include plan/status/inventory/coverage, findings+dependency map, focused sources, edits and
reviews/corrections, full validation/hardware evidence, closeout and manifest. Related logical records
may share files. Prior finalized archives stay immutable; generated binary/cache duplicates are excluded
explicitly. Preserve command outputs before five-run toolchain rotation; checkpoint before pauses.

Sol model availability confirmed: `github-copilot/gpt-6.1-sol`. Astra owns phase transitions and integration.
One investigator/editor is proportional; specialist assignment only for consequential unresolved issues.

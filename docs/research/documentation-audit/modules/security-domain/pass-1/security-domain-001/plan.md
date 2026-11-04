# Security Domain Pass-1 Documentation Improvement Plan

**Goal:** Help consumers complete Security Domain tasks using accurate, understandable contracts, examples and explanations.

**Architecture:** Audit the whole module in one coherent investigation, then fresh editing and independent review. Keep unresolved Core dependencies for Core's final campaign cycle; retain behavioral findings for Pass 2.

**Tech Stack:** .NET 10 / C# 14; Security Domain APDU/TLV operations; GlobalPlatform and YubiKey documentation.

## Authority and baseline

- User approved execution on 2026-11-04, with proportionate analysis and no Core project involvement or dedicated boundary review. Core is the final target, with per-applet boundary reviews then.
- Branch `yubikit-docs-audit`; baseline and verified GitHub HEAD `b73efe3813b429b4b8eb2a7e819a6ad7be699d47`.
- Tracked tree clean at baseline. Preserve untracked `docs/research/audit-v2-closeout-review.md`, `docs/research/audit-v2-execution.md`, `docs/research/audit-v2-jira-traceability.md`, and `mise.toml`.
- Root `CLAUDE.md`, module/test instructions, campaign `workflow.md`, and `records/style.md` govern. This campaign-local plan follows the canonical run layout rather than the generic plan skill's location.
- Prior finalized archive contents remain immutable. No runtime fixes, test assertion changes, API redesign or live hardware work in this run.

## Task 1 — inventory and investigation

Sol 6.1 investigator owns `investigation.md`, `coverage.json`, and focused `evidence/` records. Read all 38 tracked module paths (11 library C#, 15 test C#, five Markdown, two API snapshots, three project files, two runner files), reconciled in `inventory.json`. Inventory public families/snapshot entries and meaningful clean semantic units. Inspect active external documentation only for Security Domain-specific sections/examples; historical records and other modules are excluded.

Read complete source/test units. Verify meaningful specification claims against exact title/revision/status/section and vendor applicability. Use Context7 for library/API documentation; original standards/vendor sources for protocol claims. Preserve focused excerpts, URLs, access date and failed access attempts. After initial research plus one targeted follow-up, record the precise gap instead of open-ended investigation.

Output candidate IDs, concrete documentation corrections, pass-2 findings and Core dependency questions. Do not read or edit Core implementation. Source can establish module-local behavior; inherited/Core behavior that cannot be established locally remains an explicit dependency. Test commentary must follow setup/assertions/cleanup. Treat source/test names as clues, not proof.

## Task 2 — triage and fresh editing

Astra adjudicates consequential candidates and records stable `SD-IMP-*` / `SD-DEP-*` IDs and exact affected-document re-review triggers in `findings.md`. A fresh Sol editor owns module content and accepted active external documentation, using `patch`; source executable tokens, assertions, API snapshots and runtime/project configuration remain preserved. Improve task flow, local API contracts and comments together. Retain useful rationale and conditions.

One editor in the primary checkout avoids shared ownership and unnecessary worktrees. Save finding-to-edit mapping and classify every example as complete, fragment, sketch or historical. Preserve full scope/cleanup in compiled examples. Coordinator alone edits campaign/run status.

## Task 3 — independent review and sampling

Fresh Sol reviewer checks full affected sections against module source and original consequential citations. Required corrections are re-reviewed; optional preferences do not cause churn. Review module-level consistency, not cross-module boundaries. Astra samples at least 10% of eligible clean units, minimum three spanning API, narrative/examples and internal/test comments where available; expand a category on a material miss.

## Task 4 — validation

Inventory all final C# example blocks and compile meaningful current tasks in appropriate real reference/analyzer contexts. Use collision-free `AuditSd001*` host names outside normal project discovery. Do not execute example hosts. Retain source, wrappers, diagnostics and compiler provenance; exclude generated runtime/cache duplicates explicitly.

Apply scoped formatter to edited C# and prove baseline/final declaration/body/directive preservation. Any formatter syntax change requires explicit accounting and fresh evidence. Verify API snapshots unchanged. Run full warning-free solution build/unit tests and applicable documentation/package, architecture, complexity and link gates via repository toolchain and skills; serialize toolchain runs. Record pre-existing failures and limits accurately. Integration/hardware is not executed in this documentation cycle.

## Task 5 — closeout and publication

Reconcile every inventory disposition, finding/correction, review and final hash; current status has one authority. Preserve commands/results, failed attempts, original evidence and reusable validation sources. Fresh final review can combine content follow-up and compact record reconciliation. Commit explicit owned paths, push campaign branch, verify remote HEAD, then record publication in a separate checkpoint outside the frozen run manifest.

Completion is documentation-only. YubiOTP is next; PIV/Core remain last and implementation returns wait for the second sweep.

## Actor and task-packet record

Model discovery on 2026-11-04 confirmed `github-copilot/gpt-6.1-sol` (active). Astra coordinates; Sol investigates, edits and independently reviews in fresh sessions. This plan supplies shared scope/instructions; each assignment and result is recorded in `status.md` and its phase report. No concurrent content editing, builds or device operations.

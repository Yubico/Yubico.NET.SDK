# PIV Pass 1 Delegation Plan

**Goal:** Use the same fresh-owner Sol 6.1 workflow as preceding applets, with Astra coordination and exact edit boundaries.

**Architecture:** Dispatch is gated by [the main plan](2026-11-05-piv-docs-pass-1.md). Independent preparation may run alongside investigation after release; editing follows triage, content review follows editing, and fresh evidence review follows validation.

**Tech Stack:** Repository documentation and C# source, original specifications, Python YubiKey Manager references, repository validation tools.

---

## Dispatch gate and model resolution

No audit agents are dispatched while PR #745 is unmerged. User release was supplied on 2026-11-05 after planning: proceed once the plan and wait are ready. The watcher reports upstream state and wakes Astra after verified merge; Astra establishes the integrated baseline before dispatch. At execution, discover available model IDs and select the user's requested **Sol 6.1** for most investigation, preparation, editing and fresh independent reviews; Astra remains coordinator. If unavailable, report the blocker rather than silently substitute a model. Do not guess provider IDs from historical sessions.

Existing PIV implementation-agent work is a separate ownership domain. Preserve its branch/worktree and pending edits. Only the merged, integrated baseline becomes this run's source of truth.

## Mandatory assignment preamble

Include these instructions in every packet, with concrete post-merge SHA, run path and owned files filled in:

> Read root/module/test instructions and `docs/research/documentation-audit/workflow.md`, `docs/research/documentation-audit/records/style.md`, and `docs/SDK-HOUSE-STYLE.md`. Use direct, calm, precise and approachable prose. Prefer affirmative task-oriented wording: actual behavior and actor first, useful rationale and local conditions preserved. Reserve negatives for consequential limitations or likely misconceptions, paired with supported actions. Avoid repeated “does not,” “cannot,” “NOT,” and “X, not Y.” Preserve complete resource scopes and optimize comprehension rather than word count.
>
> Work only from the recorded post-PR-745 baseline. Investigate complete semantic units and actual assertions. Source implementation, sibling code and specifications have distinct evidentiary roles. Verify exact original revision/status/section/actor/strength/applicability. Pin Python YubiKey Manager source/tests/history for uncertain behavior and firmware provenance. Use bounded research before escalating precise gaps.
>
> Stay PIV-focused. Record Core dependencies with paths/questions for the final Core cycle. Preserve current runtime behavior and original failed attempts. Do not edit another owner's files, perform unassigned builds/tests/hardware, commit, or recursively delegate. Report exact paths/hashes, outcome limits and unresolved questions.

## Ownership and phase packets

| Assignment | Owner | Deliverable and boundary |
| --- | --- | --- |
| Baseline/integration/status | Astra | PR merge/ancestry gate, branch/worktree ownership, prior manifests/user files, source baseline, authoritative status and decisions |
| Whole-module investigation | Fresh Sol | Coverage, candidates, original citations and pinned Python history; all PIV public/internal/guide/example/test units; proposed hardware effect matrix. Report-only, no product edits or execution |
| Validation preparation | Independent Sol | Collision-free audit utilities and compiler-capture/extraction preparation, with provenance from previous final tools. Own designated validation paths only; no speculative final snippet extraction before accepted prose |
| Focused probe preparation, if justified | Independent Sol | Narrow source-checked state/readback helpers and meaningful probes for adjudicated questions. No device access or execution; preserve attestation and secret lifetimes; own distinct probe paths |
| Fresh documentation editing | Fresh Sol, different from investigator | Accepted module/shared prose, comments and descriptive metadata; complete task examples. One owner per file, preserve bodies/assertions/API/directives; report all identities and changed claim dependencies |
| Independent content/citation review | Fresh Sol, different from editor/investigator | Complete changed units, original consequential clauses, actual assertions, resource scopes and approved voice. Required versus optional findings; no product edits |
| Corrections and re-review | Editor, then reviewer | Exact required corrections and independent corrected-hash acceptance, preserving original reports |
| Final example extraction | Validation owner | Full accepted/formatted bodies, complete real tool, explicit dispositions, typed wrappers and real prerequisites. Preparation only; Astra compiles and immediately captures |
| Sampling/execution/integration | Astra | Clean-unit sampling, claim adjudication, serial builds/tests/hardware, human timing, failure analysis, final restoration and evidence reconciliation |
| Fresh final evidence review | New Sol session | Raw results, coverage/acceptance hashes, complete examples/compiler identities, failed-versus-successful outcomes, hardware scope, final state and archive preservation. Own review report only |

## Parallelism and handoffs

- After release, whole-module investigation and tooling preparation can run concurrently in disjoint record paths. Keep hardware planning with the investigator until enough source evidence exists for bounded probe preparation.
- Investigator hands Astra complete coverage and candidates. Astra adjudicates before assigning the fresh editor.
- Extract final examples only after review corrections and formatting are stable. Early host skeletons are preparation, never evidence of final examples compiling.
- Serialize all builds/tests in a checkout and all access to a physical device. No subagent independently performs device cleanup while a coordinator operation is active.
- Notify the editor/reviewer of late source or hardware evidence with exact scope and original failure pointers. Reopen the dependency map instead of silently adjusting a public claim.
- For a required probe adjustment, preserve the original source/context/result, state the changed hypothesis/setup, and capture the new version. A successful reference operation never overwrites an SDK failure.
- Every handoff contains baseline, owned paths, complete/read/excluded counts, exact hashes, required next action, pending gates and qualifications. Use proportionate logical records rather than reproducing a large module's bureaucracy.

## Coordinator completion checks

Confirm no assigned jobs or device operations remain active. Reconcile every reviewed path and clean-sample denominator after corrections. Preserve all original user files and prior frozen runs. Complete documentation, engineering backlog and device qualification statuses separately. Commit/push only after fresh final evidence acceptance and applicable gates; record publication outside the frozen archive.

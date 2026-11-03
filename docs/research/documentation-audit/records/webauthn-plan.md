# WebAuthn documentation and comment audit plan

**Goal:** Give SDK consumers clean, usable, truthful documentation that communicates WebAuthn's practical value, while making the module's internal explanatory comments accurate and free of misleading development residue.

**Status:** Documentation cycle complete on 2026-11-03 from baseline `e353b49afa15d1d104fce70bbf1e1abc59ddb132` on `yubikit-docs-audit`. All reviews and final gates pass. Committed/pushed `9399b94bdc6fe2786903b1b6e58808537aa8c94e`, remote verified. See `runs/webauthn-001/closeout.md` and `status.md` for coverage and deferred work.

**Authority:** This is the canonical WebAuthn cycle plan. It supersedes the earlier public-only next-cycle proposal. Use [style.md](style.md) for the approved voice and [roles.md](roles.md) for dispatch mechanics. Core is deferred; deciding its eventual audit scope is not part of this cycle.

## 1. Outcomes and boundaries

Public documentation has first priority. Readers should understand:

1. What the module enables and when to choose it over a lower-level API.
2. What the SDK handles and what the application or relying-party server must provide.
3. The prerequisites and a complete path through registration and authentication.
4. What results mean, how to handle expected failures, and who owns resources and secret buffers.
5. Which device, transport, platform, or standards qualifications affect the task.

Internal comments are an explicit second audit target, not merely incidental evidence. Inspect their accuracy,
rationale, specification citations, and unfinished-work language throughout the module. Their purpose is to help
the next reader understand real constraints, not to preserve a coding agent's development narrative.

The cycle remains documentation-first. The OATH implementation waves were an explicit exception. Meaningful
API, lifecycle, security-policy, protocol, and platform behavior changes go into a separate deferred fix pass,
with a documented trigger for re-reviewing affected prose. A small, local, unambiguous bug may be fixed under the
existing user allowance, with its own bounded diff and appropriate tests; it must not turn into a redesign.

### Included material

- Public API XML and the complete externally accessible WebAuthn surface: classes, interfaces, records/structs,
  enums, delegates, constructors, members, and supported extension points.
- `src/WebAuthn/README.md`, `PACKAGE_README.md`, relevant consumer examples, and current WebAuthn usage/migration
  claims elsewhere in the repository.
- Internal/private comments and XML, including implementation, extension pipeline, backend, and helper comments
  under `src/WebAuthn/src/`.
- Explanatory comments under WebAuthn examples and tests, including stale fixture descriptions and development
  markers. Test implementation is evidence; rewriting the test architecture is not a deliverable.
- Module/test contributor guidance when it explains audited behavior or would otherwise contradict accepted
  changes. Mandatory repository instructions must always be read.
- WebAuthn/CTAP and related standard references wherever they support the included public or internal claims.

### Dependencies and exclusions

- Read Core, CTAP2, and other dependencies only far enough to establish the WebAuthn claim. Record cross-module
  findings for later; do not launch a dependency-module audit or fix program.
- Historical reports/plans remain historical. Identify active misuse rather than rewriting old observations to
  describe current behavior.
- Generated output is reviewed through its source inputs. Copyright/license boilerplate and compiler-generated
  record members do not require artificial semantic findings or repetitive new documentation.
- No operating-system switching or platform qualification campaign is scheduled. Core remains deferred without
  a newly prescribed scope.

## 2. Improvements carried forward from OATH

| OATH lesson | WebAuthn workflow adjustment |
| --- | --- |
| Useful documentation work grew into substantial engineering | Classify findings before editing; defer meaningful behavior changes and keep their documentation dependencies explicit |
| Independent reviewers found real omissions | Use fresh editors and reviewers, original evidence, and a required correction/re-review loop |
| Code changes invalidated neighboring and external prose | Maintain a claim-to-document dependency map and run a final cross-document consistency pass |
| Plausible comments and fake responses can repeat the same misconception | Check substantive claims against actual code paths and primary specifications, not tests or model agreement alone |
| Historical and current statements were easy to confuse | Record revision and document purpose; do not apply a finding to another module or a historical snapshot |
| Repeated research and broad context add overhead | Cache focused evidence and send each worker only its assignment, required guidance, and relevant dependencies |
| Validation counts can obscure gaps | Label source review, compilation, test execution, platform/device execution, and unverified behavior separately |
| Formatting and merge corrections can invalidate a final check | Validate the actual final diff, with explicit scope for formatter and documentation checks |

## 3. Coverage and assignment model

This is one whole-WebAuthn cycle, not a sequence of partial module audits. The coordinator creates two linked
coverage views: public API/user-documentation units and internal comment-bearing files/regions. Searches find
candidates; contextual semantic reading establishes coverage, including material with no search hits.

Use `src/WebAuthn/src/PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` together with actual declarations to
inventory effective public accessibility. A public member in an internal type is not automatically exported.
Inventory the current tree before recording counts; OATH's counts and coverage do not transfer to WebAuthn.

Organize task packets around coherent areas, with their related public docs, implementation comments, tests, and
specification claims kept together:

| Area | Reader/task focus and internal evidence |
| --- | --- |
| Purpose, construction, and lifetime | Module value, client/CTAP2/server boundaries, origin/options, ownership and disposal |
| Registration | Creation options, exclusion behavior, verification/presence, registration responses and attestation |
| Authentication and selection | Request options, zero/one/multiple matches, lazy selection, results and completion timing |
| PIN/UV, cancellation, and failures | Direct versus prompted secrets, retries, token lifetime, cleanup, error mapping and admission boundaries |
| Extensions and public data types | Inputs/outputs, optional values, capabilities, experimental versus standard behavior, adapters and pipeline comments |
| Cross-document reconciliation | Entry points, migration/comparison pages, shared public guidance, example comments, and unassigned residual comments |

The coordinator assigns every in-scope unit and tracks overlaps. Start with at most two independent investigators;
combine closely coupled areas if separating them would duplicate investigation. One editor owns a file at a time.

## 4. Internal-comment and unfinished-work review

Mechanical candidates include TODO/FIXME/HACK/XXX, placeholders, stubs, phase/milestone markers, "not implemented",
"for now", "later", "temporary", future promises, unfinished sentences, and agent-like narration. Search code
signals such as `NotImplementedException` only to find relevant claims; a search hit is not proof of a defect.

For each candidate, inspect the surrounding code and relevant call paths, then choose a disposition:

| Finding | Disposition |
| --- | --- |
| Work is complete; the marker or future promise is obsolete | Remove or rewrite the stale comment |
| Genuine unfinished work or behavioral defect | Record deferred engineering and affected docs; retain an accurate limitation where readers need it |
| Deliberate supported limitation or non-obvious constraint | Explain present behavior and useful rationale; cite a decision/specification when established |
| Development narration, duplicated code narration, or abandoned phase scaffolding | Remove when it conveys no current meaning |
| Legitimate protocol phases or useful algorithm steps | Preserve them; distinguish protocol terminology from project-plan residue |
| Uncertain meaning or contradictory evidence | Investigate within the effort bound, then record unresolved status without inventing intent |

The objective is no misleading unfinished-work residue, not a zero-TODO quota. Never erase evidence of a real
defect to make the module look complete. When replacing a marker, ensure its actionable finding survives in the
ledger; use a real issue reference if one exists, and never invent an issue link. Do not make useful shipped
comments depend on inaccessible agent notes.

## 5. Specification-reference verification

For each substantive cited or implicit standards claim:

1. Identify the actor and layer: WebAuthn client, relying party/server, CTAP client, or authenticator.
2. Identify the applicable specification family, level/version, publication status, exact section/anchor, and
   relevant optional feature or transport conditions. Related RFCs and vendor extensions are checked as needed.
3. Read the actual normative text and enough surrounding context to interpret it. Distinguish MUST/SHOULD/MAY
   requirements from examples, notes, recommendations, and implementation choices.
4. Trace the nearby implementation through a concrete input/output or state transition where the claim depends
   on algorithm order, bytes, flags, ownership, or error behavior. Tests corroborate this but are not the standard.
5. Classify the outcome: correct citation and claim; incorrect citation; incorrect explanation; implementation
   mismatch; deliberate extension/deviation with evidence; or unresolved applicability.
6. Correct the reference and prose only when supported. Do not substitute the newest specification automatically,
   silently change a supported standards target, or weaken a normative reference to disguise a code defect.

Maintain a compact citation/claim index with: finding/claim ID, file/symbol and source revision, specification
title/version/status, canonical URL and section, actor/conditions, relevant excerpt or paraphrase, implementation
evidence, disposition, and dependent documentation. For moving URLs, record access date and a stable dated
reference where available. Verify that the link target supports the specific sentence, not merely the topic.

Use Context7 for library/API documentation and focused primary W3C/FIDO/RFC/vendor sources for normative claims.
Read exact local sibling-SDK source only when it answers a concrete question; it is corroboration, not proof of
conformance. Do not eagerly crawl entire standards or sibling SDKs.

## 6. Execution stages and checkpoints

### A. Baseline and inventory — coordinator

- Read root/module/test instructions, the approved voice, and this plan. Verify current branch/source state and
  preserve unrelated user work; historical checkpoint hashes are not a fresh baseline.
- Build the linked public/comment inventories and reader journeys, using the existing active-doc discovery
  mechanism while explicitly recording supplemental XML/package/migration/comment coverage.
- Assign owners and initialize findings, citation index, dependencies, and verification status.
- Check in with the user on the concrete scope and task packets before substantive editing.

### B. Read-only investigation — Sol 6.1

- Read complete assigned semantic units; investigate correctness, usefulness, comment residue, and citations.
- Return findings with original evidence and proposed disposition, plus material read/clean/unresolved.
- Do not edit during investigation or treat a test name/green count as proof of its claim.

### C. Triage — Astra, with user steering for consequential intent

- Deduplicate root causes, separate documentation corrections from engineering, and accept supported edits.
- Defer meaningful behavior changes with ownership and documentation re-review triggers. Known uncertainty must
  also be represented truthfully where it affects user guidance; a ledger entry alone does not fix misleading prose.
- After initial investigation and one targeted follow-up, escalate a consequential unresolved question or record
  the gap. Avoid an open-ended detour into device/platform debugging.
- Report accepted documentation work and deferred items at this meaningful stage boundary.

### D. Factual and editorial editing — fresh Sol 6.1

- Edit accepted documentation/comment findings only, respecting file ownership and surrounding context.
- Prioritize purpose, complete reader paths, responsibilities, useful examples, and precise contracts. Internal
  comments explain constraints and intent; they should not become mini audit reports.
- Reconcile all known dependent public/internal claims, links, tables, and example comments. Preserve qualifiers
  and useful rationale; optimize comprehension rather than word count.
- Return a finding-to-change mapping and any new question for triage.

### E. Independent review and sampling — fresh Sol, then Astra

- Verify edits against original source/spec evidence, and review full affected sections for reader-task fit.
- Explicitly check table associations, absolute claims versus sibling exceptions, actor/layer confusion,
  specification version drift, TODO disposition, and cross-document contradictions.
- Separate required factual corrections from optional preferences; fix and re-review the affected scope.
- Astra reviews consequential claims and a recorded sample of clean units: initially 10%, minimum three where
  available, spanning public XML, narrative/examples, and internal comments. A material miss expands the affected
  category rather than silently weakening coverage. This is a quality check, not statistical certification.

### F. Validation, integration, and closeout — coordinator

- Use repository wrappers and relevant documentation checks on the final reviewed changes. Compile complete
  runnable examples where practical; identify application-supplied placeholders and unexecuted ceremonies.
- Follow required build/test/formatting gates without adding tests that merely mirror prose or repeatedly running
  broad suites absent a change, failure, or unresolved concern. Meaningful new regression tests belong to actual
  bug-fix work, not a comment-cleanup quota.
- Linux is the available runtime environment. Use existing evidence for other platforms and record gaps; no
  Windows/macOS execution is inferred. Coordinate touch, PIN/UV, or device-state operations only when a specific
  documentation claim needs them. There is no automatic hardware campaign.
- Verify the final dependency map after integration, including user guides, migration/error guidance, internal
  comments, and citations. Any later code fix reopens only the affected claims plus a targeted sample.
- Report public-documentation improvements, comment/citation corrections, coverage, checks, and deferred work.
  Capture a short retrospective and check in before selecting the next module.

## 7. Artifacts, ownership, and definition of done

Keep artifacts together under the established audit workspace, using a `runs/webauthn-001/` run directory when
execution begins. Logical artifacts: coverage manifest, findings/deferred-work ledger, citation index, dependency
map, review/validation record, and concise closeout. These may share files if that reduces duplication. Do not
create empty reports or speculative finding counts during planning.

Each worker receives only its task packet, mandatory instructions, relevant evidence, and style reference.
Resolve the user's selected Sol 6.1 model before dispatch; Astra owns progress, tracking, adjudication, and
integration. The user steers at meaningful decisions rather than having to prompt every phase transition.

Completion requires:

- The complete selected public/user-documentation and internal-comment inventory has a recorded disposition.
- Accepted edits pass independent review and applicable checks; specification claims are backed by the cited
  requirements or accurately qualified, with no knowingly misleading claim left as a purported contract.
- Unfinished-work markers have meaningful dispositions; real limitations and deferred bugs remain traceable.
- Examples communicate actual supported tasks and responsibilities without unsupported functionality promises.
- Deferred engineering names the documentation affected and when to re-review it.
- Verification limitations are explicit. Completion does not mean complete WebAuthn/CTAP conformance, every OS
  tested, or all SDK defects fixed.

This public-first, comment-inclusive workflow is the default for subsequent non-Core modules. Core is deferred;
its scope will be decided separately when the user wants to return to it.

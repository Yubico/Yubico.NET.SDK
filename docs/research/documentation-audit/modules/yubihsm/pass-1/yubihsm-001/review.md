# YubiHSM Pass 1 — independent content and citation review

Reviewer: fresh GPT-6.1 Sol session, 2026-11-04. Baseline
`a74792facb657413e98223e059876d8b213ffff3`, primary checkout.

**Verdict: accept the semantic content of the 35 authored product paths at
`evidence/review-path-manifest.json`.** Required product factual/usefulness/style corrections: **zero**
at this checkpoint. The two record-reconciliation actions below remain coordinator-owned. Compilation,
preservation/analyzer gates, independent clean-unit sampling, late hardware reconciliation and final
record/publication review retain their separate acceptance steps.

## Scope and evidence identities

Read root/module/test instructions, testing guidance, campaign workflow/style, RUN plan, findings,
investigation, coverage, editorial handoff, cryptogram follow-up and its source/input evidence. Reviewed
the entire authored diff against the named baseline and the complete affected semantic units: all eight
edited library C# files, all eleven edited example C# files and all seven edited test/helper C# files,
including implementations, resource scopes and assertions. Read all four module Markdown files and the
five shared files' YubiHSM semantic units with enclosing context as needed. Core and other-module source,
API snapshots and documentation remained outside investigation; linked external repository targets received
existence checks only.

| Surface | Accepted paths | Scope |
| --- | ---: | --- |
| Library C# | 8 | Backend, algorithm/credential/retry types, session/interface, extensions and SessionKeys; complete implementations alongside commentary |
| Example C# | 11 | CLI command parsing/enrollment/access, selection, calculation menu, five operations and entry point; complete affected functions/files |
| Test/helper C# | 7 | Both integration classes/helper and four unit suites; actual setup/body/assertions/finally |
| Module Markdown | 4 | README, package README, module CLAUDE and test CLAUDE |
| Shared Markdown/YAML | 5 | YubiHSM migration guide/map/gaps/comparison and user-interaction sections only |

The path manifest lists every exact accepted path/current SHA-256/baseline SHA-256, editor-hash match and
scope. All 35 match the latest editor handoff hashes. It separately lists the other 26 tracked module
paths as **excluded from authored-content acceptance**, with baseline/current identities. All 26 retain
the baseline Git text; four project files have CRLF/LF working-tree normalization differences in raw bytes.
Shared-file full hashes identify the reviewed revision; acceptance applies to the assigned sections.
`evidence/review-product.diff` preserves this exact 35-path diff. This review supplies no clean-unit
sampling verdict for the excluded paths.

`evidence/review-input-manifest.json` identifies read records, raw logs and retained probe/oracle snapshots.
`evidence/review-snippet-manifest.json` independently identifies all six full C# README bodies, with the
same body hashes as the editor inventory. `evidence/review-link-check.json` records 17 local Markdown
target checks, all resolving. Final hashes should be rechecked after any subsequent edit.

## Required record reconciliation

These actions correct evidence/status records rather than reopening an accepted product edit.

1. **R1 — Reject the mistaken test-guide link subfinding in current triage/coverage.**
   `findings.md` currently calls for fixing a confirmed relative link; `coverage.json` still describes
   `tests/CLAUDE.md` as having a broken link. From `src/YubiHsm/tests/`, three parents reach the repository
   root: `../../../docs/TESTING.md` resolves to the existing root testing guide. The proposed four-parent
   alternative leaves the repository. The editor retained the correct target. Record this subfinding as
   rejected with the independent check pointer; preserve the investigator's original observation as a
   labeled historical error. The remaining C011 ownership/test-infrastructure changes stand independently.
2. **R2 — Publish a current checkpoint before final record freeze.** The opening status text still says
   no hardware operation has run, and its last progress entry says the first v2 lane is executing.
   Preserve those checkpoints as history and add an authoritative current summary distinguishing the
   failed v1 parser probe, failed v2 cancellation expectation, passing omission-only retry/password probes,
   passing v3 measured-6982/fresh-recovery probe, and pending human-touch/live-HSM qualification. Update
   current hardware/coverage dispositions together. The original cryptogram investigation's unverified
   retry-restoration statement belongs to its supplied-cryptogram checkpoint; the separate v2 omission
   result now establishes 8→7→8 for that narrower branch.

## Content adjudication

| Claim group | Independent assessment |
| --- | --- |
| Enrollment and secret roles, C003 | The guide leads with enrolling matching symmetric keys on both devices, separate HSM object ID/YubiKey label mapping, domains/capabilities and partial-enrollment reconciliation. Typed provisioning completes protected recovery storage before the generated buffer is cleared. Borrowed management/password inputs retain caller lifetimes. Derived and credential passwords have distinct purposes. |
| Management-key rotation, C003/C006 | The durable typed persistence callback runs before PUT. Replacement clearing covers later failures, the current key stays caller-owned, and uncertain outcomes require reconciliation. The guide preserves destructive-reset recovery consequences. |
| Handshake actors and typed sketch, C004 | The host supplies its actual challenge; HSM challenge/cryptogram come from the HSM through the connector. Context ordering is exact. The complete adapter interface/method assigns handshake completion, protected state, partial-construction cleanup, asynchronous disposal and retained-copy clearing to the application. Owned returned arrays/context/host challenge are cleared in finally; keys are disposed before enclosing resources. |
| Asymmetric identity, C004/C007 | PK-OCE exported for enrollment is distinct from ephemeral EPK-OCE and separately trusted HSM static public key. GET_CHALLENGE shape/point validation and external HSM inputs have explicit integration responsibilities. The dummy test commentary identifies the credential public key's misuse as the HSM argument and the several invalid inputs. |
| Retry/error policy, C001/C002 | Source-qualified success restoration, credential deletion and management blocking agree with S2. Actual helper users expose typed 63Cx failures; GET_CHALLENGE uses the ordinary APDU path. Single attempts and application-owned retry decisions remain explicit. |
| Version policy, C005 | SELECT's three-byte value, fallback, override and reset-retained policy agree with local implementation. Feature XML distinguishes the obsolete alias. Device metadata, ykman display and SDK-effective version stay separate evidence. Production/development capability extrapolation is removed. |
| Ownership, C006 | SessionKeys copies/owns three arrays; previous spans see cleared storage and later getters throw. Parse inputs and additional copies have separate owners. PBKDF2 copy wording is restricted to the helper's explicit input handling. Direct factory borrowing and extension-created connection ownership agree with local calls and the recording ownership assertion. |
| Presence/cancellation, C007 | Conditional LIST, known/unknown/missing policy mappings, cancellation propagation, prompt ownership and non-reentrant callbacks agree with local flow. Updated module/shared text tells callers to await terminal drain/cleanup and follow the actual result. It promises no status code, native-interruption timing or touch-timeout interpretation. |
| Open metadata, C008 | Unknown algorithm identifiers and nullable touch are preserved; extension properties reject unknown algorithms. Record equality uses all properties while ordering uses ordinal case-insensitive labels. DTO construction stays separate from enrollment validation. |
| Diagnostic sample, C009 | Tool comments accurately identify exposed long-lived/session keys, visible/argument strings, partial pre-finally cleanup coverage, symmetric-only dispatch and absence of a configured prompt. Operator HSM enrollment/recovery tasks remain explicit. Runtime/help strings stay outside this Pass-1 edit. |
| Test explanations, C010/C011 | Length/metadata assertions retain their narrow meaning. GET_CHALLENGE only checks nonempty in the named integration case. Exact ApduException/nonzero-SW rejection establishes combined invalid-input rejection, with subclass mismatch described correctly. Both touch-tagged cases check stored policy only. Password-change recorder comments match selected header/tags; PBKDF2 recomputation uses the same primitive. Helper reset-before/resource-disposal/persistent-state distinctions agree with its complete body. |
| Shared consistency, C012 | Current YubiHSM statements distinguish implemented SDK surfaces from target-app/HSM qualification, retain external provisioning/handshake duties and scope retry policy correctly. The hybrid gaps section labels its current clarification while retaining prior history. Unrelated shared parse debt and other-module claims remain outside acceptance. |

The approved affirmative task-oriented voice is satisfied. Procedures name the actor and supported action;
remaining negative statements explain consequential compatibility, authentication, lifetime or assertion
limits. Generic-string absence and cryptogram-omission facts remain useful security qualifications.
Optional preference-only rewrites: **none requested**. Preserve accepted wording rather than introduce
editorial churn.

## Original citations and cryptogram limitation

`evidence/review-citations.md` records independent original passages, titles/status, actor, strength and
feature applicability for S1–S5 and the cryptogram follow-up. Original-response manifests retain URLs,
access date, exact bytes and SHA-256. S4/current Python, historical Python and pinned C/header bytes match
the earlier captures. Original commit metadata independently confirms the 2023 initial parser date and
S4/S5 revision dates. The hardware oracle's S5 repository revision yields byte-identical `core.py` to the
investigator's pinned file.

Accept the strict-48/public response-format limitation in the symmetric interface, README, SessionKeys,
module guidance and diagnostic calculation comments. The supplied-cryptogram path remains explicitly
qualified; the typed HSM sketch demonstrates intended integration responsibilities beside the current
compatibility caveat. Python's historical 56-byte handling/current slicing and C's deliberate suppression
of an 8-byte symmetric cryptogram/strict-48 check support the stated implementation comparison. Their
different tail names/comments leave actual suffix meaning and firmware range open. The public wording
assigns peer verification and handshake completion to the application and offers cryptogram omission
only for isolated key-calculation checks.

Preserve GAP-SPEC-1 and HSM-IMP-008. Descriptive manuals and vendor host encoders support these scoped
statements; versioned normative response framing, a firmware boundary and real-HSM interoperability
require further evidence. A subsequent raw-region probe reopens the listed cryptogram-dependent units
for focused re-review, rather than automatically changing a historical parser observation into universal
firmware behavior.

## Hardware evidence reconciliation at this checkpoint

This reviewer read existing logs and probe/oracle source; the coordinator performed all device operations.
The recorded environment is serial 103, USB SmartCard, Omarchy, metadata `5.8.0.beta.0`, ykman app display
`5.8.0`, and SDK-effective session `0.0.1`. Retain each observation's actor and provenance.

- The initial existing integration batch logs **11 passed**, with the assertion limits described above.
- V1 KDF/retry logs four successful CMAC primitive vectors, three independent key matches with cryptogram
  omitted, typed wrong-password 63C7/seven retries, then strict-48 parsing failure on 56 bytes. The retained
  v1 source binds that failing call to the supplied independent laboratory card cryptogram. Its later
  key-content/retry-restoration assertions were unreached. Finally reset/fresh readback passed.
- V2 omission-only KDF/retry logs **one passed**, three-key matches and credential retries **8→7→8**.
  User and administrator password-transition probes log **one passed** with both replacement passwords
  yielding independent three-key matches. Their cryptogram-bearing qualification remains separate.
- V2 cancellation logs cancellation at **1,009 ms**, drain at **14,308 ms**, CALCULATE **6982**, one
  `Failed` resolution and a live body token. Its expected OCE/6985 alternatives failed; recovery stayed
  unreached. Finally reset/fresh readback passed. Preserve this failure.
- V3's retained terminal log now records cancellation at **1,011 ms**, drain at **14,270 ms**, 6982/Failed,
  then fresh owned-session recovery **without an intervening reset**, an independent non-touch three-key
  match, and successful final reset/readback. It logs **one passed** for that measured-outcome/recovery
  scope. Native admission, cancellation effect and touch-timeout interpretation remain unproven.
- Successful human-touch calculation remains pending at this checkpoint. Actual HSM/connector enrollment,
  authenticated interoperability and other device/firmware/platform combinations retain separate gaps.

## Validation and reopening boundary

All six complete README C# bodies are semantically reviewed and independently hashed. Compile the actual
full interface/type/method bodies in the stated wrapper, with real SDK references/analyzers and all cleanup
scopes intact. Compilation remains coordinator-owned and pending separately. The actual 22-source example
tool also requires its genuine-project validation. Reviewer operations were limited to reads, original-source
fetches, identity/link/diff checks, and this report/evidence creation; no product/tool edits, builds, tests,
hardware commands, formatter, commits or agents were performed.

Changes to accepted hashes, raw 56-byte region identification, successful physical-touch evidence or
changed error/ownership/version policy require focused dependency reconciliation. Retain the failed probes
and superseded record checkpoints, attach exact new hashes, and keep documentation acceptance separate
from implementation completion and hardware qualification.

# Security Domain security-domain-001 closeout

## Outcome

Pass-1 documentation content, independent source/citation review and final verification are complete
against `b73efe3813b429b4b8eb2a7e819a6ad7be699d47` on `yubikit-docs-audit`. Fresh independent
[final record review](review-closeout.md) passed with no required corrections. The coordinator finalizes
the archive manifest and publication after this checkpoint. The external
[Security Domain handoff](../../../../security-domain.md) records the eventual commit/remote checkpoint.

## Coverage and improvements

- All **38 module paths** fully investigated and dispositioned: 11 library C#, 15 test C#, five Markdown,
  two API snapshots, three project files and two runner files.
- **Eight public families / 96 API snapshot entries** reconciled, with generated record/extension members
  grouped rather than padded with prose. All 16 concrete operations inherit explicit local interface
  documentation; factory, feature and DTO contracts are explained.
- **26 authored paths** independently accepted: 23 module paths and three narrowly scoped shared-document
  sections. Fifteen module paths remain byte-identical to baseline. No generated diagram edits.
- Practical guidance now distinguishes first SCP03 import, additional/custom replacement, fresh
  authentication before old-key deletion, deletion filters, certificate/provisioning roles, SCP11b host
  authority limits and draft-qualified SCP11c command restrictions.
- Reset guidance explains bounded attempts, partial mutation, missing factory-state verification and
  current reset-option loss without making those defects intended contracts.
- Public parsing/result/exception/ownership descriptions and test explanations match local evidence.
  Contributor examples use current signatures, real typed prerequisites and complete resource scopes.

## Review and corrections

Sol 6.1 investigated, edited in a fresh session, and independently reviewed in another fresh session;
Astra coordinated triage, selected checks, sampling, validation and integration.

All required content corrections are independently accepted in review-followup.md: draft subsection
7.5.1, thumbprint comparison scope, human-facing testing link and explicit draft §4.6 SCP11c command
restrictions. The investigator's proposed four-parent testing link was rejected; the original three-parent
path was correct. Original reports, source excerpts, failed attempts and superseding errata remain usable.

Final clean sampling: **3/9 eligible units**, across API, narrative/example and test-comment categories.
Three edited-overlap nominations were conservatively excluded; one earlier pre-edit package sample is
supplemental and uncounted. No material sample miss occurred.

## Validation

- Clean full solution build; **3998 unit tests passed**, including **50 Security Domain cases**; five
  existing suite skips, zero compiler/analyzer warnings or test failures.
- Documentation/package QA, architecture and changed-method complexity gates passed.
- Ten meaningful example groups compiled in two genuine SDK/analyzer contexts; all current blocks/wrappers
  and 407 compiler reference/analyzer identities reconciled after final compilation. No host was executed.
- Eighteen C# paths passed strict pre-format token/directive preservation. Final formatter applied two
  explicitly accounted ordinary namespace-import removals in one file plus trailing-newline normalization;
  all other syntax and prose are preserved. All 20 API snapshot files remain byte-identical.
- Final formatter passed. Workspace duplicate-source loading diagnostics are retained separately from
  the warning-free compiler/analyzer results. Initial formatter, transform-recorder and docs-QA failures
  remain preserved with exact corrections.

See [validation](validation.md), [snippet validation](snippet-validation.md), [reviewed identities](reviewed-files.json)
and [final coverage](coverage-final.json). Generated runtime/cache duplicates are not Git artifacts;
meaningful source/config/command/result evidence and dependency identities are retained.

The final `record-manifest.json` enumerates all retained run artifacts with exact byte counts and SHA-256,
excluding itself. Campaign-local attributes preserve validation/evidence bytes, including their original
line endings. Earlier finalized archives (784 OATH/WebAuthn, 402 Management, 534 CTAP records) and original
user work passed fresh preservation checks and independent final verification.

## Deferred work and scope

The 13 initial candidate groups map to **eight deferred engineering/verification groups** and **five Core
dependency questions** in [findings](findings.md), with precise documentation reopening triggers. No
runtime fix, assertion change, API redesign, implementation completion or hardware qualification is claimed.
Final GP amendments/profile applicability remain unverified where originals were inaccessible after
bounded follow-up; the accessible draft remains explicitly distinguished from final releases.

Core is the final target; dedicated per-applet boundary reviews wait until then. A review link checker
incidentally read Core README headings, disclosed in corrections.md; no Core behavior investigation or
source edit followed. Ordinary whole-solution verification and dependency compilation do not constitute
a Core documentation audit. No live integration test, SCP handshake, key mutation or reset was run.

DOC-002 and the unrelated existing shared-guide anchor remain outside this cycle's narrow edits. Earlier
campaign archives and four original untracked files are preserved. The inherited OATH SCP03 initialization
failure did not reach authentication/reset and is not Security Domain reset evidence.

## Next target and retrospective

Next approved module: **YubiOTP Pass 1**. PIV/Core remain for the final part of the first sweep, Core last;
implementation returns remain deferred until the second sweep.

The compact one-investigator/one-editor/one-reviewer structure suited this module. Keep whole-task
compilation, independent consequential citation checks and assertion-based comment review. Trust neither
plausible section numbers nor path arithmetic without checking; count formatter transformations from
actual before/after content, not diagnostic-line counts. Reconcile current status and evidence at each
transition while preserving checkpoint history.

# YubiHSM Pass 1 — bounded final-content and late-evidence follow-up

GPT-6.1 Sol, original independent content reviewer, 2026-11-04.
Baseline `a74792facb657413e98223e059876d8b213ffff3`.

**PASS for this follow-up scope. R1 and R2 are resolved. Required product corrections: zero.
Required record corrections identified by this follow-up: zero.** The original 35-path semantic
acceptance carries through the verified formatter transforms to the exact final hashes in
`reviewed-files.json`. Repository-run completion and documentation/example compilation remain separate,
pending coordinator gates; this report certifies neither those gates nor overall closeout.

## Accepted-content → formatter → final binding

`evidence/review-final-bindings.json` independently binds all 35 original accepted identities to current
final bytes. The original `evidence/review-path-manifest.json` remains byte-identical to its prior review
verification identity. All 26 full-source entries in `validation/pre-format-content.json` hash to the
original accepted C# files. All 26 formatter before/after hashes agree with those captures and the current
files. The nine Markdown/shared paths retain the exact accepted bytes, with shared acceptance restricted
to the previously assigned YubiHSM sections.

An independent full-content comparison confirms that the only accepted-to-final C# changes are:

- EOF newline normalization across the 26 files;
- import-group blank removal and ordering in `HsmAuthRetryExceptionTests.cs`;
- two named namespace-import removals in `src/YubiHsm/src/IYubiKeyExtensions.cs`;
- one `Yubico.YubiKit.Core.Protocols.SmartCard.Apdu` import removal in each of
  `HsmAuthSessionTests.cs` and `HsmAuthSessionByteLevelTests.cs`.

The named import removals total **four across three files**. Complete non-import source, including
comments, strings, executable bodies and directives, is preserved. The readable exact transform is saved
in `evidence/review-final-format.diff`. No editorial rewrite was introduced.

Read `pre-format-tokens.log` and `final-tokens-corrected.log` in full. Their **26** per-file SHA-256
identities match the accepted/pre-format and final files respectively. The former records zero token/
directive failures; the latter records zero import-accounted failures, identifying exactly the four
removals and distinguishing the normalized import syntax from strict token identity. The retained API
log reports **20 snapshot files, zero failures**. This follow-up reads that result as preservation evidence;
it performs no other-module API/source investigation.

The first final-token run ended with `KeyNotFoundException`/exit -6 at source-capture JSON parsing. Its
retained initial manifest has the identity printed by that failed log. The corrected recorder adds the
required schema fields and checks formatter exit status; independent comparison confirms that all
original paths, source identities, removals and transform facts survive unchanged. The corrected manifest,
source-capture/transform bundle hashes and formatter-log hashes match their actual retained bytes. The
corrected checker run binds that manifest and exits zero. Preserve both attempts as evidence of a recorder
schema correction, rather than describing the original run as passing.

`format-final.log` records the scoped verify-no-changes command and exit zero, alongside a workspace-load
warning. This report accepts the exact content transform; final repository/compiler/analyzer warning
qualification stays with the coordinator's remaining gates.

## R1 and R2 adjudication

**R1 resolved:** `findings.md` explicitly rejects the proposed test-guide link correction and points to
the independent check; `coverage.json` marks the rejected link subfinding. `corrections.md` preserves the
adjudication and the investigator's historical error. The accepted test CLAUDE retains the correct
three-parent target. The remaining C011 changes retain their original acceptance.

**R2 resolved:** `status.md` now leads with an authoritative current checkpoint and labels preceding
progression entries as history. `hardware/results.md` reconciles all 13 existing methods, the four final
scoped probes, the two original failed expectations, final cleanup and remaining verification dimensions.
The original review's pending-human-touch checkpoint is superseded by this follow-up. Earlier investigation,
triage and probe-review checkpoints remain historical; the current status/results govern present hardware
claims. The scoped omission-only retry result stays separate from the failed supplied-cryptogram branch.

## Late hardware and cleanup evidence

Read the complete retained v3 touch and two-method metadata logs, both cleanup logs, original/corrected
cleanup scripts, final device-info output and current hardware results. Previously reviewed v3 cancellation
and original failed probes retain their existing dispositions. The coordinator performed the hardware
operations; this reviewer performed read-only evidence and identity checks.

| Evidence | Accepted measured scope and documentation impact |
| --- | --- |
| `hardware-v3-touch-20261104-133147-709-test` | **One passed.** The coordinator/user record supplies human readiness and actual touch. The raw log records serial 103, SmartCard, SDK-effective 0.0.1, one PolicyRequires request, one Completed resolution with token None, independent three-key equality and successful final reset/readback. The reviewed assertion source checks the same context instance and exact lifecycle counts. This CALCULATE omits the optional cryptogram. It supports the existing touch/prompt explanation on this device while preserving the cryptogram-bearing qualification. |
| `integration-touch-metadata-20261104-133206-476-test` | **Two passed.** Together with the original 11-method batch, all 13 existing methods passed. These two methods inspect stored touch metadata; their comments correctly retain that narrower scope beside the separate real-touch calculation evidence. |
| Original independent cleanup | **Failed helper validation, exit 1.** The retained original script passes a nine-byte Python byte password. The log shows rejection for requiring 16 bytes, and source places reset in the exception handler. The log supplies no completed default-key usability assertion for this attempt. Preserve the failure. |
| Corrected independent cleanup | **Passed, exit 0.** The only script change pads that fixed laboratory password explicitly to 16 bytes. Reset, fresh connection, default-key PUT/LIST/DELETE and another fresh connection establish zero credentials, eight management retries and usable default management key. Direct vendor-Python session version is 0.0.1. This is applet-state/usability evidence, with Python buffer/string lifetime outside the .NET ownership assurances. |
| Final public metadata | Independently compared the complete initial/final `ykman info` output excluding capture time: serial/product/firmware/interfaces/application metadata match. The authorized reset erased the original credential; this comparison establishes readable metadata consistency and the cleanup state, rather than restoration of its original private material. |

The v3 measured-6982/Failed plus fresh-session recovery result remains a successful measurement/recovery
check. The v2 OCE/6985 expectation remains failed. Neither result identifies native admission, interruption
timing or a specific touch-timeout cause. Current module/shared cancellation prose correctly tells callers
to await the terminal result and cleanup, then follow that result for recovery.

## Final prose qualifications and bounded research decision

The accepted public prose remains accurate after these results. The touch test adds recorded-device
support for the existing behavior; it supplies no reason to weaken the cryptogram-bearing parser caveat,
claim general firmware interoperability or change the two metadata-test descriptions. Default-key cleanup
supports the reset/recovery explanation while keeping destructive reset and caller/owned-copy lifetimes
explicit. Device metadata 5.8.0.beta.0, ykman's displayed app version 5.8.0 and direct Python/SDK session
version 0.0.1 remain separate observations.

Accept the coordinator's bounded Pass-1 decision to stop without a raw 56-byte region probe. The actual
SDK failure and independently checked vendor-source history support the present consumer-facing
compatibility limitation. HSM-IMP-008 retains response-layout identification, variant acceptance and
firmware applicability as Pass-2 verification/contract work. Actual HSM/connector enrollment and
authenticated interoperability remain separate outstanding dimensions. The typed HSM sketch preserves
peer verification, handshake completion, resource disposal and secret ownership responsibilities;
cryptogram omission continues to establish isolated key calculation only.

## Durable evidence and remaining gates

- `evidence/review-final-bindings.json`: all 35 accepted/final hashes, full-source transform checks and
  provenance-bundle checks.
- `evidence/review-final-format.diff`: exact accepted-to-final C# changes.
- `evidence/review-final-evidence.json`: identities of all focused records/logs/scripts/source captures
  used here, plus independent log/hash/schema/metadata checks.

The six complete README C# bodies and actual 22-source example project retain their separate compilation
gate. The final repository run remains outside this follow-up's verdict. Retain the original semantic
review, failed evidence and accepted hash list; append later gate results with their own exact identities.
Reviewer ownership is limited to this report and `evidence/review-final-*`. No product/tool edits, builds,
tests, hardware commands, formatter runs, agents or commits were performed.

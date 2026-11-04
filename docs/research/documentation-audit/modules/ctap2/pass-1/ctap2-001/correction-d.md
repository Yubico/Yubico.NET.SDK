# D review corrections — RD-R1 and optional RD-O1

**Date:** 2026-11-04. **Editor:** GPT-6.1 Sol, original D editor responding to fresh review.
**Worktree:** `/tmp/opencode/ctap2-001-edit-d`; HEAD remains
`5b09a9b6942ded3913b5c9340c3e126617e61a82`.
**Status:** corrections applied and locally checked; fresh follow-up review pending.
No source outside this D worktree was edited. Only this record and the additive
[edit-d.md](edit-d.md) mapping/hash update were written in PRIMARY. No builds, tests,
hardware, compiler/format probes, commits, integration or delegation.

## Verified feedback and changes

Read [review-d.md](review-d.md) completely and checked the actual source before editing,
following the already-loaded receive-review and verify guidance. No new protocol claim
or standards research is needed for these two source/assertion observations.

### RD-R1 — required; applied

Exact path:
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs`.

Read the complete `EnrollCaptureNextSampleAsync_WithPoorQuality_ReturnsStatus` test,
lines 205–227, before and after correction. It supplies a single reply with count 3,
without an earlier count, and asserts only `LastSampleStatus == TooFast`. The old
“Remaining doesn't decrease” comment therefore claimed an untested transition.

At line 213, replaced only the comment with:

> `Supplied count; this test asserts only the decoded TooFast status.`

Encoded integer **3**, status, method name, mock setup, invocation and assertion remain
unchanged. Mapping: D-15 / CTAP-DOC-010/012 / existing CTAP-IMP-022 proof-scope work.
No test defect was repaired or new implementation bug counted.

### RD-O1 — optional; accepted as one-line clarification

Exact path:
`src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementExtendedTests.cs`.

Verified actual `EnumerateCredentialsAsync(rpIdHash)` calls, count >=2 and both-created-ID
presence assertions; this is credential enumeration for one RP, not RP enumeration.
At line 29, the summary now reads:

> `Checks updated display name and presence of two created IDs in credential enumeration for one RP.`

No other text or executable content in this file changed since review. Mapping:
D-13 / CTAP-DOC-012; no new scope or behavior.

## Reviewed-to-corrected byte hashes

SHA-256 over exact worktree file bytes:

| Exact path | Reviewed hash | Corrected hash |
| --- | --- | --- |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.UnitTests/BioEnrollment/FingerprintBioEnrollmentTests.cs` | `0c8aa16392483fc9b4bf59ed0a5685964cacd74783a9dfcf088e6e2d932ca8a4` | `942b123fa87c844f45e41f7719b7b55ae873fefad67f963c91de6e0819dafd8a` |
| `src/Ctap2/tests/Yubico.YubiKit.Ctap2.IntegrationTests/Ctap2CredentialManagementExtendedTests.cs` | `a0dc31337bc40de9a268ef4aa6e7ed5352588de1deb67d9f623df73c2589c8b0` | `77908964a69218454942a89bdf6ac216998e71ce46f4e79e89c9c4abfe3fe390` |

The other **18 files match review-d.md's reviewed hashes exactly**; their acceptance is
not expanded here. The two changed semantic units need follow-up review. D08's rejected
runtime-ordering inference and D14's correct CTAP 2.1 legacy force-change status remain
unchanged and accepted for their already-reviewed scope. Genuine deferred work remains.

## Local checks and limits

Ran `git rev-parse HEAD`, then read-only Python (`python3 - <<'PY' ... PY`) using the
20-row reviewed SHA-256 manifest. For each changed hash, reversing precisely its one
comment substitution reproduced the reviewed hash; every other hash was identical.
The same check compared baseline/comment-stripped executable tokens (preserving string
and character literals), imports/assertions and directives across all 20 files, parsed
all contiguous XML-comment blocks structurally, and asserted exact baseline-relative
changed-path equality. It invoked `git diff --check` and read `git diff --stat`.

**Result:** exit **0**; exactly **two comment substitutions** since review, other **18
files byte-identical**, all **20 executable/directive sequences preserved**, **129 XML
blocks parsed**, `git diff --check` exit **0**. The complete corrected RD-R1 test and
RD-O1 summary were re-read afterward. This is text/XML verification, not compiler/API/
cref validation, a passing unit suite or device qualification.

Current baseline-relative diff (`git diff --no-ext-diff --no-color`, UTF-8 text) SHA-256:
`17cd7c64500e8052c9fcee1efdba31d89d340e36e90828442ef0c53398c5b250`.
Stat: **20 files, 502 insertions, 207 deletions**, including earlier approved documentation
edits and terminal-newline whitespace. No source edits followed these checks.

Snippet classifications, typed prerequisites and cross-owner dependencies remain those
in edit-d.md. No example/code block was added; only the two explanatory lines changed.
Ready for fresh follow-up review; no independent re-review PASS is claimed by the editor.

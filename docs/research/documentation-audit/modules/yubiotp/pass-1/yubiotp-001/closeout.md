# YubiOTP yubiotp-001 closeout

## Outcome at this checkpoint

Pass1 authored content, independent original-source review/correction/hardware-impact acceptance,
source preservation, complete-example compilation and final repository gates are complete from baseline
`95e5ccaeda33954c21f2a38058d4f66b5f208787` on `yubikit-docs-audit`.
Fresh [final record review](review-closeout.md) passed with no required corrections. Archive freeze and
publication follow. External [YubiOTP handoff](../../../../yubiotp.md)
will carry publication commit/verified remote hash without rewriting this run's eventual frozen archive.

## Scope and improvement

All**63tracked module paths** fully investigated:23libraryC#,14OtpToolC#,9unit/5integrationC#,4Markdown,
4projects,2API snapshots,2runner files. **24effective-public types/193snapshot entries** reconciled,
including18protected subclass entries and generated/extension duplicates; not193callable operations.

Independent review accepted**50authored paths**:45module paths and5narrow shared-document sections.
Eighteen module paths remain byte-identical; all42editedC#files preserve executable bodies/directives.
Public interface/concrete contracts, configuration flags/defaults/ownership, task guides, all sample/
test explanations and meaningful specifications were in scope. Generated/historical exclusions and
active shared-reference dispositions remain explicit in coverage records.

Reader improvements include current/new access-code retention and complete update masks; raw public IDs;
HMAC input/wire/hashed-message distinctions and verifier enrollment; AES ticket/verifier responsibility;
cached keyboard trigger versus actual challenge touch; firmware/transport conditions; NDEF byte limits;
copied/borrowed secret ownership; HOTP IMF/scan-map/CLI implementation limitations; assertion-based tests.
The voice preserves useful conditions/rationale and practical alternatives without presenting unfixed
defects as intended contracts. No SDK behavior, assertion, runtime help string or API redesign changed.

## Investigation, reviews and corrections

Sol6.1 completed a fresh whole-module investigation, fresh documentation editing, independent content/
original-citation review and correction/hardware follow-up. A separate Sol tooling session prepared
validation hosts; Astra owned triage, source sampling, execution, correction adjudication and integration.

18candidate groups map to**12deferred engineering/verification groups** and**5record-only Core dependencies**
with source anchors and documentation-reopening triggers. IMF divisor65536versus16, scan-map38versus45,
preflight secret-copy cleanup, SmartCard trigger inference and OtpTool UTF8/raw-scan-code mismatch remain
engineering findings. Passing unrelated device tests does not close them.

Three required review corrections were applied/re-reviewed: serial exclusion source attribution,
YubiOtpCommand's actual protected try/finally path, and verifier enrollment before generated HMAC key
erasure. Reviewer's ancillary sources.md correction request was rejected and withdrawn because that
passage already correctly described merged flags. No optional editorial churn.

Fresh consequential source retrieval matched ten originals/publisher pages. Manual3.3 remains legacy
vendor evidence; personalization1.20.0 and manager5.8.0 are vendor implementations, not standards. NFC
URI/Text1.0 originals via identified mirror have distinct publication-status evidence; failed Manual3.4
access and PDF extraction warnings retained. One-device results never become all-firmware proof.

Final clean sampling:**3/21**, API/narrative/internal-test categories. Five edited-overlap nominations and
one runner-metadata nomination excluded conservatively; no material miss. Full range/hash reconciliation
and reviewer=>formatter=>final source identities are preserved.

## Verification

- Clean full-solution build and**3998unit tests**, including**201YubiOTP**, passed with0compiler/analyzer
  warnings/errors/failures and5existing suite skips. Docs73active/10package, architecture and complexity
  gates passed; OtpTool is part of the solution. No full-suite device test of unrelated applets.
- **All16existing YubiOTP integration methods** passed across15non-touch+1human-coordinated touch runs.
- **Four focused probes** passed: HID/SmartCard status matrix and USBCCIDHMAC6985classified rejection;
  AES ticket challenge/independentCRC; cancellation/Cancelled; natural14s timeout/TimedOut. New-session
  cleanup/recovery passed; final independent ykman readback shows both slots empty and accessible key.
- Hardware scope: serial103,metadata5.8.0.beta.0,rawOTP/effective0.0.1,Linux,USBHID/CCID. USBCCID rejection
  is not successfulHMAC. NFC output/readback, old-firmware matrix, keyboardIMF output and access-code
  transition probe remain unexecuted. NDEF test writes/disable request do not prove on-tap restoration.
- Pre-format42C#strict token/directive comparisons passed; formatter removed2named ordinary imports in
 1file, reordered/grouped1integration import and normalizedEOF. Exact transform and final Roslyn proof
  retained; all20repository API snapshot byte identities unchanged. Formatter workspace loader diagnostics
  are distinct from warning-free compilation. No broad syntax exception or analyzer suppression added.
- **19documentation groups** compiled in2genuine reference/analyzer contexts; full14source-fileOtpTool
  compiled with separate actualCsc capture. All624reference/analyzer identities, raw/wrapped bodies and
  current source/config membership checks passed. No documentation/example executable ran.

See [validation](validation.md), [hardware results](hardware/results.md), [review follow-up](review-followup.md),
[coverage](coverage-final.json), [sampling](sampling.md), [examples](snippet-validation.md) and [findings](findings.md).

## Preservation, limits and next sweep

Initial reader contention blocked0executed/15skipped before user released it; preserve original failure,
not only successful retry. Both formatter-accounting and compiler /embed-membership tool failures remain
with original sources and exact corrections. Prepared-only example revisionv1 is history, not passing
compilation evidence. Primary-source PDFs have exact exceptional identities; build binary/cache duplicates
are excluded rather than silently retained or claimed reproducible offline.

Prior784/402/534/164archives stay immutable and four original untracked files remain user-owned. Shared
test infrastructure was read for execution, and the probe preparer read CoreAPI snapshot signatures;
this is disclosed. No Core implementation audit/edit or dedicated per-applet boundary review occurred.
Existing DOC-002 migration-map parse defect outside narrow entries remains deferred. Final record review
has passed; manifest/remote backup must complete before claiming the cycle published. record-manifest.json
will freeze every retained artifact except itself with exact sizes/hashes, including three registered
primary-source PDFs. Its eventual verification/publication status belongs in the external handoff.

Continue Pass1 across remaining modules before implementation returns. PIV remains late; Core is last,
with per-applet boundary reviews then. Re-baseline stable findings before any Pass2 work. Backward API
compatibility is not a release goal; documentation completion and scoped device evidence are distinct
from implementation completion, full conformance and platform qualification.

Retain lessons: inspect every assertion/cleanup path, use original editions rather than plausible citations,
complete verifier enrollment/resource scopes in examples, distinguish status indicators from policy,
check actual transport/firmware and partial outcomes, and preserve failed attempts/current status together.

# YubiOTP corrections, execution limits and scope disclosures

## Current corrections

Independent content review requires three bounded corrections; editor follow-up assigned. Original
investigation/editing/preparation outputs retain their phase-specific meaning rather than being overwritten.

| ID | Adjudication and source evidence | Current disposition |
| --- | --- | --- |
| OTP-REV-001 | ExtendedFlag serial exclusions belong to personalization1.20.0, not Manual3.3; reviewer freshly retrieved original clauses. Retain correct Manual3.3 device-global merge explanation. | Product attribution correction accepted. Ancillary reviewer request to correct evidence/sources.md:23–24 rejected: those lines only state correctly attributed merged flags, not the disputed exclusions. Original record remains intact. |
| OTP-REV-002 | YubiOtpCommand secret allocations/parsing are inside its try; finally clears AES/privateID/accesscode. Astra independently checked complete relevant flow; the broad pre-try warning was copied to a safer path. | Correct local comment; retain genuine other-command exceptional cleanup findings under IMP003/010. |
| OTP-REV-003 | Generated HMAC key must be enrolled with verifier before only shown caller copy is erased, otherwise recipe provisions an unusable pair. | Add explicit application-dependent enrollment step/prose, keep complete cleanup and explicitHID. |

**Final content disposition:** all three corrections applied and independently accepted in
review-followup.md/evidence/review-final-content.json. Original47paths unchanged; three corrected paths
accepted. Reviewer withdrew the ancillary sources.md request. Hardware-impact review passed for the
recorded device scope; no additional public-document correction required.

Optional navigation/wording preferences are not required changes. Hardware-sensitive follow-up will
check all affected claims against actual results rather than blindly adding one-device facts everywhere.

## Execution/evidence corrections

- Initial integration run executed0/skipped15 due to exclusive SmartCard-reader contention, not absence
  of allow-list approval. User released reader; exact IntegrationTests selector retry passed15/15.
  Initial negative!~ filter also hit an unrelated MTP positive preflight; narrower project selector avoids
  that tooling defect without changing it. Full original logs retained.
- Three original PDFs were collected as specification/vendor evidence: Manual3.3 and NFC URI/Text RTD1.0.
  Initial utility preparation assumed text-only. Coordinator explicitly registered exact PDF size/hash/
  signature identities in validation/primary-binary-sources.json and narrowed manifest/record validators
  to allow only those primary sources. Build binaries/cache remain rejected; original utility provenance
  reflects preparation hashes, not falsely final tool identities.
- Hardware probe author consulted Core public API snapshot for compilation signatures, as disclosed in
  hardware/probe-plan.md. No Core implementation investigation/edit or dedicated boundary review occurred.
  Shared test-infrastructure instructions/provider were read narrowly for device authorization/execution.
  Do not claim zero Core-file reads across the run; future scoped preparation should use module examples
  and compiled references where sufficient.
- Device metadata names firmware5.8.0.beta.0; raw OTP/session version in probe is0.0.1. Keep both in all
  evidence rather than relabeling raw OTP version as production5.8.0. OTP-DEP-004 retains inherited policy.
- Initial formatter verify exited2 for EOF normalization/import order/unused namespaces. Actual apply
  removed exactly two ordinary imports from IYubiKeyExtensions and grouped one integration-test import,
  removing its intervening blank line. First transform recorder failed closed because it had not allowed
  that exact blank-line removal; original script/failure retained. Corrected recorder permits just this
  named transformation. Forty-two corrected sources passed strict pre-format Roslyn; final42passed with
  two explicit namespace-removal exceptions in one file, no declaration/body/directive changes. All20
  API snapshots unchanged. Review=>formatter=>final hashes are bound in reviewed-files.json.
- Nineteen documentation groups compiled warning-free in two contexts. Separate OtpTool evidence build
  failed during Clean with MSB3540 because the proposed output-path hook changed MSBuildProjectExtensionsPath
  after initial use. This is validation-host configuration, not an SDK/example compile failure; full solution
  already compiled OtpTool. Originalv2hook/log retained; preparer is simplifying to ordinary sample outputs
  with capture-only hook. Accepted consumer/annotation contexts do not need a repeat for this tool fix.

## Coordinator probe review

Astra read all632 lines of the proposed four-lane probe and its plan before hardware execution: serial103
guard, real public APIs, explicit transports, slot2-only mutation, independent HMAC/AES+CRC oracle,
separate cancellation/timeout expectations, owned-buffer cleanup and fresh-session readback/recovery.
Host builds warning-free; measurement accepting exactly6985 is classified capability-rejected, not a
successful SmartCard HMAC claim. Test host/runsettings/provenance are retained for final evidence review.

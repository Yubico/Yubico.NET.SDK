# OATH follow-up wave2 complete

## Integrated work

| Work | Implementation | Integration |
| --- | --- | --- |
| Shared Base32, request identity/numeric validation, cleanup | `8503fe2d` | `eba72a81` |
| Terminal reset, timestamp bounds, authentication provenance and caller migrations | `d74cdd44` | `57e0fa4d` |
| Focused documentation/migration reconciliation | `76a8ae52` | same commit |

The current OATH behavior changes are an explicit user-approved exception. The broader campaign returns to documentation-first work. Meaningful behavior changes elsewhere, including cross-application reset consistency, are deferred to a separate fix pass and documentation re-review.

## Review and verification

- Inputs: independent review, overlap correction, re-review Ready across17files.
- Lifecycle: independent review, teardown correction, user reset-policy decision, terminal implementation, fresh review Ready across20files.
- Documentation implications: fresh review identified three stale claims; all corrected and re-review PASS. Newer PC/SC link preserved.
- Combined full solution: **3,998 unit tests passed**, including436OATH;5existing skips;0warnings. Build, docs QA, architecture and complexity passed.
- Final XML/prose corrections: full build and docs QA passed,0warnings. Scoped merged-file formatting found one added EOF newline, corrected it, then verified0changes without warnings.
- Combined OATH non-user-presence integration: **32 passed,7skipped,0warnings** on125/5.8.0.alpha.2. This comprises19live and13offline cases. A final terminal-reset/time case passed and independent cleanup completed; separate postflight opens confirmed password disabled and zero accounts.
- Skips:3absentolder-firmware rows,3serial103-only rows,1SCPinitialization6A88beforeRESET. User-presence case excluded. Do not present these as live SCP verification or a multi-device success matrix.

## Remaining findings and limits

- All14identified IMP items now have scoped resolutions, with ownership/firmware/evidence qualifications retained in the register. DEC-005 timestamp policy is resolved; DEC-001 provenance improved but broader failure-state policy remains deferred.
- Raw-key policy, URI-extension decisions, padding provenance, detailed HOTP behavior and earliest firmware rationale remain recorded questions. No new runtime changes are authorized merely to resolve documentation uncertainty.
- CORE-001 two-key discovery remains open and deferred.
- Bounded SCP probe returned6A88 for bothFFand00 after OATHSELECT; no change to defaults was made. Deeper SCP diagnosis is deferred.
- **DOC-002:** a supplemental parse of `docs/migration/v1-to-v2-map.yml` exposed an existing YAML syntax failure atline101,column621 (unquoted colon in the device-stream note), reproduced unchanged inHEADbeforeedits. The changed OATH exception entry parses in isolation. Full-map YAML validity is therefore not claimed; record this for a documentation-only hygiene pass rather than silently expanding the OATH changes.

No push performed. Original untracked user files remain untouched. The diagnostic SCP worktree retains an uncommitted probe; it is not integrated or represented as a passing test. All dispatched workers have finished.

## Evidence

`inputs-full-gates/`, `lifecycle-full-gates/`, `combined-full-gates/`, `combined-doc-correction-gates/`, reviewed manifests, correction/terminal-reset evidence, `terminal-reset-hardware/`, final combined hardware archive, and independent review records in this directory.

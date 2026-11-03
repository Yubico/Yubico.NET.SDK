# Wave1 re-review and verification

## Independent reviews

- **CLI:** Ready, reviewer `ses_e61ddcc03ffejlYWhXv6K3x7me`. All six required issues closed: sanitized diagnostics, comparisons without arbitrary secret-string normalization, context-aware help, seven-digit raw forwarding, real async prompt cancellation, and shallow parser phases with explicit complexity assessment.
- **Protocol:** Ready, reviewer `ses_e61ddcc01ffeWOPOE7R1nS8zU4`. All four required issues closed: known malformed aggregate rejection, wire validation before naming omission, accurate structural/ownership contracts, and independent bounded test cleanup preserving primary failures.

Both reviews were read-only and inspected original source/tests and saved evidence, not merely author summaries. No remaining Critical/Important issue was reported within these bounded tasks. Broader auth, timestamp, reset, creation and marker-policy decisions remain open.

## Branch verification and commits

- CLI: full solution build, all3,774 executed unit tests, docs QA and applicable checks passed; five skips, zero failures/warnings. OATH unit scope257. Example parser complexity was manually assessed because the shipping tool excludes those methods.
- Protocol: full solution build, all3,738 executed unit tests, docs QA and complexity passed; five skips, zero failures/warnings. OATH unit scope221. Existing VALIDATE complexity is unchanged; no new/worse method requires action.
- Reviewed file manifests matched after these gates. Only the12 CLI and11 protocol files were staged explicitly.
- Commits: `618adbec` (CLI arguments/cancellation), `c79e4c1e` (response validation/cleanup).
- Main audit branch fast-forwarded to the CLI commit and has the non-conflicting protocol merge staged. Combined gates are running before the merge commit.

## Current-source hardware evidence

| Device | Run | Live cases | Offline cases | Skipped |
| --- | --- | --- | --- | --- |
| 9681620, firmware5.4.3, connected alone | `20261102-223955-624-test` | 2 passed | 2 passed | 2 absent newer-firmware rows |
| 125, physical firmware5.8.0.alpha.2; SDK reports5.8.0, connected alone | `20261102-230114-130-test` | 2 passed | 2 passed | 2 absent older-firmware rows |

Both runs passed with zero warnings. Live cases verified exact SHA1/SHA256/SHA512 full-HMAC vectors, credential lifecycle, protected SELECT shape, wrong-key status0x6A80 followed by successful correct-key validation, and aggregate marker behavior. Cleanup completed after the runs.

These are separate single-key runs. They do not prove simultaneous two-key discovery works or that individual touch-configured HOTP calculation needs no touch. The observed marker-to-SDK metadata mapping is not a physical touch-policy proof; DEC-006 remains open. See `discovery-blocker.md` for the separate Core issue.

## Evidence storage

Reviewed hashes: `cli-reviewed-content.json`, `protocol-reviewed-content.json`.
Archived red/green/format/hardware results: `cli-evidence/`, `protocol-evidence/`.
The initial protocol27-failure red run was observed by the author but pruned before archival; later parser and cleanup red runs are preserved. No reconstruction of missing logs is presented as original execution evidence.

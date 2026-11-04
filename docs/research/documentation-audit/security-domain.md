# Security Domain documentation handoff

## Current cycle

**Pass 1 is complete and published** as
[security-domain-001](modules/security-domain/pass-1/security-domain-001/plan.md),
on `yubikit-docs-audit`. Baseline:
`b73efe3813b429b4b8eb2a7e819a6ad7be699d47`. Content and complete run records:
**`7f7623595859b153a3c80d0e49bbdac60ecb43e4`**, committed/pushed and independently matched to GitHub's
branch HEAD on 2026-11-04. Fresh independent final record review passed with no required corrections.

The [frozen run status](modules/security-domain/pass-1/security-domain-001/status.md) preserves the
pre-publication checkpoint; this handoff supersedes its publication-pending next step. This publication-only
update changes no reviewed content, compiler inputs, evidence or run hashes.

The user approved a module-focused, proportionate cycle. Core project investigation and dedicated
cross-module boundary review are deferred until Core, the final target, when per-applet relationships
will be reviewed together. Record precise dependencies for that later work.

All 38 tracked module paths were investigated: eight public families / 96 API snapshot entries. Twenty-six
authored paths have independent acceptance; three final clean samples cover nine eligible units.
Ten meaningful example groups compiled across two contexts. Final clean solution gates passed 3998 unit
tests, including 50 Security Domain cases, with five existing skips and zero compiler/analyzer warnings.
Implementation and hardware verification remain separate from documentation completion.

The [run manifest](modules/security-domain/pass-1/security-domain-001/record-manifest.json) preserves
**164 text artifacts / 2,385,100 bytes**, excluding the manifest itself. Exact staged Git blob bytes were
checked against retained files before publication. Finalized earlier archives and four original untracked
files remain unchanged. No PR or merge was performed.

## Return-visit records

- [Closeout](modules/security-domain/pass-1/security-domain-001/closeout.md): scope, outcomes and remaining limits.
- [Findings](modules/security-domain/pass-1/security-domain-001/findings.md): SD-IMP-001–008 and SD-DEP-001–005,
  source observations, missing evidence and exact documentation reopening triggers.
- [Final coverage](modules/security-domain/pass-1/security-domain-001/coverage-final.json) and
  [reviewed hashes](modules/security-domain/pass-1/security-domain-001/reviewed-files.json): every module path
  disposition and exact authored-content acceptance/formatter mapping.
- [Validation](modules/security-domain/pass-1/security-domain-001/validation.md) and
  [examples](modules/security-domain/pass-1/security-domain-001/snippet-validation.md): actual commands,
  preserved inputs/results, failed attempts and limits.

All engineering groups remain deferred-pass-2. They include reset completion/options, deletion filters,
allowlist profile/validation, malformed responses, range/KCV validation, test assertions and sensitive
resource cleanup. The accessible SCP11 public-review draft is not final-profile evidence. Re-baseline
selected items before implementing; no live hardware test or device-state mutation occurred here.

## Inherited evidence limits

The [OATH handoff](oath.md) retains a live SCP03 attempt on serial 125 blocked at INITIALIZE UPDATE:
both `80 50 FF 00` and `80 50 00 00` returned `6A88`. EXTERNAL AUTHENTICATE and RESET were not reached.
That historical result is an unresolved initialization question, not evidence for Security Domain reset
behavior. Future device work must re-establish inventory/state and the exact authorized scope.

Next target after this cycle: YubiOTP Pass 1. Implementation returns wait until the campaign's second sweep.

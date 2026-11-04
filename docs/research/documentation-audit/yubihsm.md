# YubiHSM Auth documentation handoff

**Pass 1 is complete and published** as [yubihsm-001](modules/yubihsm/pass-1/yubihsm-001/plan.md).
Baseline: `a74792facb657413e98223e059876d8b213ffff3`. Documentation and complete evidence committed and
pushed as **`e053386a6afff23194f8874603625a2a13c07a00`** on `yubikit-docs-audit`; local/GitHub branch
HEAD matched on 2026-11-04. Fresh final evidence review passed with zero required corrections.

The [frozen status](modules/yubihsm/pass-1/yubihsm-001/status.md) preserves the prepublication checkpoint;
this external handoff supersedes its publication-pending next step. This publication-only checkpoint
updates the handoff and campaign index while preserving accepted product content and frozen run bytes.
The campaign branch remains published without a PR or merge.

Scope: whole YubiHSM Auth module, example tool, test explanations and relevant shared-document sections.
User selected YubiKey-only hardware verification. The approved voice emphasizes affirmative, task-oriented
explanations with explicit actors and useful qualifications. Engineering findings accumulate for Pass 2;
Core dependencies and dedicated boundary reviews remain scheduled for Core, the final target.

## Verified outcomes

- **56 module paths, eight public types and 83 API entries** reconciled; **35 authored paths** accepted.
- All 26 edited C# paths preserve executable bodies/directives, with four exact formatter-only import
  removals in three files. All 20 API snapshots unchanged. Final clean sampling: **3/17**.
- Clean solution gates: **3,998 unit tests**, including **91 YubiHSM**, five existing skips and zero
  compiler/analyzer warnings or failures. Documentation/package, architecture and complexity passed.
- **Eight documentation groups and full 22-source HsmAuthTool** compiled with actual Csc evidence;
  all **407 reference/analyzer identities** reconciled. Compilation preserved complete task/resource scopes.

All 13 existing integration methods and four final scoped probes passed. Original supplied-cryptogram
56-byte parser and cancellation-expectation failures remain explicit. Final independent client cleanup
verified zero credentials, eight retries and usable default management key on serial 103. SDK/direct
Python session version **0.0.1**, ykman app display **5.8.0**, device metadata **5.8.0.beta.0**;
USB SmartCard/Linux. The original asymmetric test credential was erased by the authorized reset.

The successful probes establish omission-only three-key calculation/retry restoration, user/admin
password transitions, measured `6982`/`Failed` followed by fresh-session recovery, and human touch with
`Completed` plus independent three-key equality. The failed cancellation expectation remains distinct
from the later measured recovery result; native interruption and timeout causality remain unverified.

## Return-visit records

- [Closeout](modules/yubihsm/pass-1/yubihsm-001/closeout.md): scope, completed gates and lessons.
- [Findings](modules/yubihsm/pass-1/yubihsm-001/findings.md): **eight deferred engineering groups and
  three Core dependencies**, with exact documentation-reopening triggers.
- [Cryptogram investigation](modules/yubihsm/pass-1/yubihsm-001/cryptogram-investigation.md): HSM-IMP-008,
  observed 56-byte response/48-byte parser mismatch, vendor history and remaining format questions.
- [Hardware results](modules/yubihsm/pass-1/yubihsm-001/hardware/results.md),
  [validation](modules/yubihsm/pass-1/yubihsm-001/validation.md), and
  [examples](modules/yubihsm/pass-1/yubihsm-001/snippet-validation.md): exact outcomes and limitations.
- [Final evidence review](modules/yubihsm/pass-1/yubihsm-001/review-closeout.md): independent acceptance.

Live HSM/connector authentication, raw 56-byte region identification, firmware applicability, NFC,
other platforms and Native AOT runtime qualification remain open. The supplied-cryptogram compatibility
limitation is documented; runtime work awaits the campaign's second sweep.

## Preservation and publication

The [manifest](modules/yubihsm/pass-1/yubihsm-001/record-manifest.json) freezes **477 text artifacts /
5,894,560 bytes**, excluding itself. All 515 content-commit paths were explicitly staged and matched
working-file/index bytes. Final manifest/path/size/hash, whitespace and previous archive/user checks passed.
All **2,317 earlier archived identities** and four original untracked user files remain unchanged.
The tracked tree was clean after the verified content push. Earlier failed probes, capture assumptions,
cleanup-helper and recorder-schema failures remain preserved alongside their scoped successors.

Continue the remaining module Pass-1 sweep before selecting implementation returns. PIV remains late;
Core remains last, including the dedicated per-applet boundary reviews. Re-baseline findings before fixes.

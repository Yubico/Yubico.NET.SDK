# YubiOTP documentation handoff

**Pass 1 is complete and published** as [yubiotp-001](modules/yubiotp/pass-1/yubiotp-001/plan.md).
Baseline: `95e5ccaeda33954c21f2a38058d4f66b5f208787`. Documentation and complete run records:
**`f4419397df7e6c25cef89c7ea01e1baacb35ce2a`**, committed/pushed on `yubikit-docs-audit` and matched
to GitHub's branch HEAD on 2026-11-04. Fresh independent final evidence review passed with no required
corrections. No PR or merge was performed.

The [frozen status](modules/yubiotp/pass-1/yubiotp-001/status.md) preserves the pre-publication checkpoint;
this external handoff supersedes its publication-pending next step. This publication-only checkpoint
changes no accepted product content, compiler input, hardware result or frozen evidence hash.

Scope: all 63 tracked module paths, effective public surface, guides, source/test/example comments,
meaningful specifications and narrowly selected active shared documentation. The user authorized
integration and focused one-off tests against the connected YubiKey; device/transport/firmware and
actual execution/cleanup evidence remain explicit.

## Verified outcomes

- All **63 module paths, 24 public types and 193 API entries** reconciled; 50 authored paths accepted.
- All 42 edited C# paths preserve behavior with two explicit formatter-only namespace removals;
  all 20 repository API snapshots unchanged. Final clean sampling: **3/21** eligible units.
- Clean full-solution gates: **3,998 unit tests**, including **201 YubiOTP**, five existing skips,
  zero compiler/analyzer warnings or failures. Documentation/package, architecture and complexity passed.
- All **16 existing integration methods plus four focused probes** passed on serial **103**, metadata
  firmware **5.8.0.beta.0**, raw/effective OTP **0.0.1**, Linux USB HID/CCID. Final slots were empty.
- **19 documentation groups and full OtpTool** compiled warning-free, with actual compiler inputs and
  624 reference/analyzer identities reconciled. No documentation/sample executable was run.

USB CCID HMAC returned `6985`: a measured rejection, not a successful calculation or universal transport
claim. NFC output/readback, older firmware, keyboard HOTP counter output and cross-platform execution
remain unverified. Cleanup establishes empty slots and readable metadata, not restoration of all device state.

## Return-visit records

- [Closeout](modules/yubiotp/pass-1/yubiotp-001/closeout.md): completed scope and limitations.
- [Findings](modules/yubiotp/pass-1/yubiotp-001/findings.md): **12 deferred engineering/verification groups
  and five Core questions**, with source evidence and exact documentation-reopening triggers.
- [Validation](modules/yubiotp/pass-1/yubiotp-001/validation.md),
  [hardware](modules/yubiotp/pass-1/yubiotp-001/hardware/results.md), and
  [examples](modules/yubiotp/pass-1/yubiotp-001/snippet-validation.md): commands, actual results and limits.
- [Final evidence review](modules/yubiotp/pass-1/yubiotp-001/review-closeout.md): independent acceptance.

The [run manifest](modules/yubiotp/pass-1/yubiotp-001/record-manifest.json) preserves **433 artifacts /
7,297,581 bytes** (430 text records and three primary-source PDFs), excluding itself. Initial staged
whitespace checking flagged ykman's original table padding in two hardware
logs. The existing campaign evidence-attribute convention now also covers hardware records; no frozen
log bytes or manifest entries were changed. Final staging passed: all 487 owned Git paths had exact
working-file/index bytes. Previous 1,884 archive records and four original untracked user files remain
unchanged; tracked tree was clean after the verified content push.

Implementation returns wait until the second sweep. Core investigation and dedicated per-applet boundary
reviews remain scheduled for Core, the final target. Earlier finalized campaign archives stay immutable.

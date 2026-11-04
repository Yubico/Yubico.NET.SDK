# YubiOTP documentation handoff

Pass1 content, independent content/citation/hardware-impact review and execution gates are complete as
[yubiotp-001](modules/yubiotp/pass-1/yubiotp-001/plan.md), from published campaign
baseline `95e5ccaeda33954c21f2a38058d4f66b5f208787` on `yubikit-docs-audit`.
See [current status](modules/yubiotp/pass-1/yubiotp-001/status.md).

Scope: all63 tracked module paths, effective public surface, guides, source/test/example comments,
meaningful specifications and narrowly selected active shared documentation. The user authorized
integration and focused one-off tests against the connected YubiKey; device/transport/firmware and
actual execution/cleanup evidence will remain explicit.

All63module paths/24public types/193API entries reconciled. Fifty authored paths accepted;42C#paths
preserve behavior with2explicit formatter-only namespace removals, all20APIs unchanged; clean sampling3/21.
Final clean solution gates passed3998unit tests including201YubiOTP,5existing skips,0warnings/failures.
All16existing integration methods+4focused probes passed onserial103,metadata5.8.0.beta.0/rawOTP0.0.1,
LinuxUSBHID/CCID; final slots empty. Nineteen documentation groups and fullOtpTool compiled warning-free.
Fresh final evidence review passed with no required corrections; publication pending.
[Closeout](modules/yubiotp/pass-1/yubiotp-001/closeout.md)
and [findings](modules/yubiotp/pass-1/yubiotp-001/findings.md) retain scope, limits and12deferred groups/5Core questions.

The run is frozen at433artifacts/7,297,581bytes (430text records and3primary-source PDFs), excluding its
manifest. Initial staged whitespace checking flagged ykman's original table padding in two hardware
logs. The existing campaign evidence-attribute convention now also covers hardware records; no frozen
log bytes or manifest entries were changed. Final staging/remote checks follow this correction.

Implementation returns wait until the second sweep. Core investigation and dedicated per-applet boundary
reviews remain scheduled for Core, the final target. Earlier finalized campaign archives stay immutable.

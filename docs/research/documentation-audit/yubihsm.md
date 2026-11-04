# YubiHSM Auth documentation handoff

Pass 1 content, independent reviews and execution gates are complete as
[yubihsm-001](modules/yubihsm/pass-1/yubihsm-001/plan.md), baseline
`a74792facb657413e98223e059876d8b213ffff3` on `yubikit-docs-audit`.
See [current status](modules/yubihsm/pass-1/yubihsm-001/status.md).

Scope: whole YubiHSM Auth module, example tool, test explanations and relevant shared-document sections.
User selected YubiKey-only hardware verification. The approved voice emphasizes affirmative, task-oriented
explanations with explicit actors and useful qualifications. Engineering findings accumulate for Pass 2;
Core dependencies and dedicated boundary reviews remain scheduled for Core, the final target.

Current scope: 56 module paths, eight public types/83 API entries, 35 authored paths accepted, 26 C#
paths with four explicitly accounted formatter-only import removals; all 20 API snapshots unchanged.
Clean sampling 3/17; clean solution gates passed 3,998 unit tests including 91 YubiHSM, five existing
skips and zero compiler/analyzer warnings/failures. Eight documentation groups and full 22-source
HsmAuthTool compiled with actual Csc evidence and 407 reference/analyzer identities reconciled.

All 13 existing integration methods and four final scoped probes passed. Original supplied-cryptogram
56-byte parser and cancellation-expectation failures remain explicit. Final independent client cleanup
verified zero credentials, eight retries and usable default management key on serial 103. SDK/direct
Python session version 0.0.1, ykman app display 5.8.0, device metadata 5.8.0.beta.0; USB SmartCard/Linux.

Fresh final record review passed with zero required corrections; publication remains pending.
[Closeout](modules/yubihsm/pass-1/yubihsm-001/closeout.md),
[findings](modules/yubihsm/pass-1/yubihsm-001/findings.md) and
[hardware results](modules/yubihsm/pass-1/yubihsm-001/hardware/results.md) preserve eight deferred
engineering groups, three Core dependencies and unverified HSM/firmware/platform dimensions.

# Bounded correction and hardware-impact re-review

2026-11-04 · same independent `github-copilot/gpt-6.1-sol` reviewer · baseline
`95e5ccaeda33954c21f2a38058d4f66b5f208787`.

## Verdict: PASS for content and the recorded hardware-impact scope

**OTP-REV-001–003 are resolved. No remaining required consumer-content correction was identified.**
All 50 authored paths are accepted at the exact hashes/lengths in
[`evidence/review-final-content.json`](evidence/review-final-content.json). Fresh hash checks confirm
the original 47 accepted paths are byte-identical to the first review and only the three corrections
changed. Reverse substitution of the editor's exact changes reconstructs each original reviewed hash.
This acceptance precedes final formatting/validation; any later byte changes require reconciliation.
For the five shared files, the whole-file hash detects drift but acceptance covers only the assigned
YubiOTP sections, not other applets' prose.

The original `review.md`, its manifests and original-source captures remain unchanged. This follow-up
supersedes its pending correction/hardware-review status, not its retained historical account. Builds,
compilation/analyzers, token/directive/API-snapshot gates, package/link QA, sampling and final evidence
closeout remain separate coordinator gates. I executed no build, test, formatter or device command.

## Correction acceptance and exact hashes

| Finding / path | Re-review result | Accepted SHA-256 |
|---|---|---|
| REV-001 · `src/YubiOtp/src/ExtendedFlag.cs` | Correctly attributes the two serial firmware exclusions to personalization v1.20.0, retaining legacy applicability and merged serial-flag explanation. | `6ac9449167e736fe03b5475eafdf1364cd49ce1319b7a4607271578e5ca9f7c1` |
| REV-002 · `src/YubiOtp/examples/OtpTool/Commands/YubiOtpCommand.cs` | Complete command still clears acquired AES/private-ID/access-code arrays in finally; corrected comment distinguishes immutable strings/parser diagnostics/captured output without importing another command's pre-try gap. | `e129bcfbfc3ff12f64eb5f0d4c5fe688b8bd63078f4f26d50d9dba542ff7c45d` |
| REV-003 · `src/YubiOtp/README.md` | HMAC setup is explicitly application-dependent, requires enrolling the same generated key with the verifier, and places the enrollment placeholder inside try before creation. Explicit HID, touch, configuration/session scopes and key cleanup are preserved. | `5ba4557b704e023e231e09208f584cdf8bbf1dc8753f8d437d8558ef2ff32eba` |

**Reviewer correction to the original REV-001 ancillary request:** I agree with the coordinator.
`evidence/sources.md:23–24` states only the device-global merged serial flags, correctly attributed to
Manual 3.3 §5.6. It does **not** assert the button-2.x or USB-serial-3.0/3.1 exclusions. My statement that
those record lines repeated the misattribution was mistaken; that request is withdrawn. No source-record
change is required. The actual `ExtendedFlag.cs` attribution correction remains valid and is accepted.

## Hardware evidence actually reviewed

Read `hardware/results.md`, the **full retained raw logs** for reader-released and successful-touch
existing integrations and all four status/AES/cancel/timeout probes, together with their toolchain result
logs. Also read the original reader-conflict raw failure, the complete 632-line probe source, its project/
runsettings, compiler membership/source identity, existing successful-touch test source and final/initial
ykman identity/slot logs. Existing test assertions and known-answer vectors were already reviewed in full
in the original review; their source remains unchanged at the recorded hashes. No tests were rerun.

The reviewed probe source is:

`validation/hardware-probe/tests/Yubico.YubiKit.AuditOtp001.Hardware.IntegrationTests/src/HardwareProbeTests.cs`

**SHA-256 `1a9dd85f2a0856d0eb61581624e38737c217266d619dc988b7dd3367b3e9bdf6`, 31,062 bytes.**
Fresh hash equals the actual v1 compiler capture's `compile-inputs.txt` identity, and that file is a
compiler member. The raw executions identify the same uniquely named probe assembly. Full source/log
identities and original review hash are retained in the final content manifest; no binary duplication
or new source/spec retrieval was necessary.

### Result reconciliation

- **Initial attempt:** raw failure is `ConnectionInUseException` during exclusive-reader initialization,
  followed by misleading generic authorization skips. It executed **0**, skipped 15, and is not operation
  failure/success evidence. It remains preserved separately from the retry.
- **Existing suite after release:** 15 executed / 0 failed / 0 skipped plus the nonoverlapping successful-
  touch invocation's 1 / 0 / 0 = **all 16 existing methods executed successfully**. Each retained toolchain
  result reports zero warnings and exit zero. The two HMAC known-answer methods exercise eight short-
  mode and four fixed-mode vectors internally; those twelve vectors are not twelve extra test methods.
- **Four targeted probes:** status, AES, cancellation and natural timeout each executed one test, with
  zero failures/skips and zero toolchain warnings. The status lane's passing classification includes an
  expected/accepted **device rejection branch**, not successful SmartCard HMAC.
- **Identity:** serial **103**, Linux/Omarchy, USB-A YubiKey 5 NFC Enhanced PIN; ykman metadata firmware
  **5.8.0.beta.0**. Probe logs separately show raw OTP **0.0.1** and session effective **0.0.1** over both
  explicit transports. These are not relabeled production 5.8.0 and are not an old-firmware compatibility
  test. The probe options select transport/prompt, with no firmware override.

## Claim-to-evidence hardware impact

| Claim family | What source plus retained execution establishes | Consumer-document disposition / limits |
|---|---|---|
| Cached trigger versus RequireTouch | Fresh sessions over explicit HID and USB SmartCard log static keyboard trigger **true**, non-touch HMAC **false**, touch-required HMAC **false**, each with slot 2 configured and slot 1 empty. The probe asserts configured/slot-1 stability; the trigger values are logged observations, not a pre-assumed matrix assertion. The separate touch test does observe a live HID wait for the touch-required configuration. | Supports ConfigState, session/CLAUDE/README, shared interaction and migration qualification: trigger is not challenge RequireTouch read-back. Does not repair or validate the SmartCard PolicyRequires heuristic for every slot/device. No documentation change needed. |
| HID HMAC marker and exact answers | Both executed known-answer methods compare host HMAC with all twelve expected messages, including short-mode empty/all-equal/final-marker and fixed64 behavior. HID non-touch probe also compares exact host output and asserts no prompts. | Current input/payload/hashed-message explanation is supported on this key; retaining target-firmware/empty64 qualification remains appropriate. No universal firmware promise, no claim of mode discovery. |
| USB SmartCard capability | Explicit USB SmartCard non-touch HMAC logs `ApduException`, **SW6985**, header `00 01 38 00`; the specific rejection is caught/classified. It emits zero request/terminal events. Zero events are consistent with cached non-trigger status, not proof the command succeeded or all SmartCard touch policy is correct. | Explicit HID USB recipes and lack-of-parity cautions remain correct. No conclusion about NFC, other keys, AES over CCID, or universal SmartCard rejection code. |
| AES ticket | Two HID responses are each asserted16 bytes, decrypted with known AES key/ECB/no padding, first six plaintext bytes compared to challenge, and full plaintext checked with **independent bitwise CRC16** (initFFFF, reflected8408, residualF0B8), not SDK ChecksumUtils. Ciphertexts differed in this run; no prompts. | Supports zero-private-ID challenge/ticket/CRC interpretation and “can differ” wording. Two samples do not prove entropy quality, full replay policy, all variable fields or another transport's support. SDK still performs no ticket verification. |
| Successful touch | Existing test requires initially empty slot2, explicitly selects HID, writes RequireTouch, observes exactly one DeviceWaiting request and matching-context Completed resolution with terminal tokenNone, compares exact host HMAC, then deletes slot2 and asserts cached empty. | Supports application-visible prompt lifecycle on this device. It does not promote printed trigger status to policy read-back. Fresh independent final state adds cleanup evidence beyond this test's cached assertion. |
| Deliberate cancellation | Probe waits for DeviceWaiting (observed after14ms), cancels operation token, **awaits that same operation** before disposal, observes OperationCanceledException drained at268ms and exactly one matching-context **Cancelled** resolution with tokenNone. Outer budget did not expire. | Supports cancellation/prompt outcomes, not an instantaneous-stop guarantee. No raw HID report recording was performed: this does **not** independently prove dummy-reset bytes/order, native drain internals, or all-path lifetime safety. README's inherited reset description remains a deferred Core dependency, not a new inference from these logs. |
| Natural no-touch timeout | The same awaited calculation raises the logged full `OtpHidTouchTimeoutException` type after**14067ms**, with outer65s token still live. Exactly one DeviceWaiting and matching-context **TimedOut**, tokenNone, are asserted. | Supports device/protocol touch-timeout versus outer caller cancellation and ordinary timeout distinctions. Measured14s is not a promised timeout interval. Declared90s VSTest bound was not deliberately exercised. SmartCard admitted-cancellation behavior was not tested. |
| Cleanup/recovery | All four lanes dispose their operation sessions, then use independent bounded fresh sessions to delete/read slot2 empty and check slot1 against its initial state. Cancellation/timeout additionally provision new non-touch HMAC, check exact answer/no prompts, then clean again. Independent final ykman logs show **both slots empty**, same identity/version/interfaces. | Establishes fresh-session operation and external client access after these runs, **not same-session reuse or universal native ownership**. Initial empty slots are restored at configured-state level; secrets/NDEF/global flags are not byte-for-byte restoration evidence. |

Rechecked current README HMAC/AES/NDEF/prompt/security sections and the unchanged dependent XML/module/
shared qualifications from the first review. There is no newly introduced run-specific success claim in
consumer material, and no existing claim needs to be broadened because the device run passed. The source-
supported current/new-code semantics and legacy flag caveats remain source-supported, not newly live-
verified. CLI limitations remain disclosed; runtime/help strings were not changed.

## Remaining limits, not unresolved content blockers

No NFC reader/on-tap read-back or verified NDEF disable; no full scan-map programming; no keyboard HOTP/
nonzero-IMF output capture; no access-code retain/change/remove probe; no old-firmware or cross-OS matrix;
no same-session post-cancellation/timeout reuse; no raw reset-byte capture or Core/native lifetime review.
Configured-bit and response-length-only tests retain their original evidential limits despite passing.
Runtime defects/deferred decisions are not closed by this documentation acceptance.

Only this follow-up, `evidence/review-followup-check.py` and generated `evidence/review-final-content.json`
were added in this follow-up. No product content, original review/captures, source/tests, coordinator
records or hardware evidence was edited. No agents, commits, formatter, test/build or hardware execution;
no Core/other-module source/docs read and no external target content validation.

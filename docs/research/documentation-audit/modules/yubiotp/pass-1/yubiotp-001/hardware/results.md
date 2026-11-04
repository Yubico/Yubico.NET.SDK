# YubiOTP hardware execution evidence

2026-11-04; Astra coordinator. Linux/Omarchy, .NET10.0.403/runtime10.0.14. Authorized serial**103**,
YubiKey5NFC Enhanced PIN USB-A, metadata firmware**5.8.0.beta.0**, USBOTP/FIDO/CCID enabled. Raw OTP
status and YubiOtpSession effective version report**0.0.1** on both transports; neither field is relabeled
as production5.8.0. User approved connected-device tests and later confirmed reader release and readiness
for successful physical touch. Every device/toolchain command was serialized.

## Existing integration suite

All16 existing module integration methods executed successfully across two nonoverlapping invocations:

| Retained validation log | Executed / skipped / warnings | Scope |
| --- | --- | --- |
| integration-initial.log | 0 /15 /0; FAILED no-tests | Pre-test exclusive-reader conflict; no module operation ran. Exact ConnectionInUseException retained, not an authorization failure. |
| integration-reader-released.log | 15 /0 /0; PASS | All methods except successful-touch. Includes read-only presence-traited inspector and both short/fixed HMAC known-answer tests with12 internal vectors. |
| integration-successful-touch.log | 1 /0 /0; PASS | Human confirmed ready, then touched while waiting; host HMAC comparison, exactly one DeviceWaiting/Completed lifecycle and slot2 cleanup. |

Bodies/effects/actual transport are cataloged in hardware-test-plan.md. SmartCard/default status/programming/
swap/NDEF write methods passed; explicit HID programming/challenge/protection methods passed. Attribute
transport filters alone are not proof of which session transport was used. NDEF test wrote then requested
disable but did not read NFC output; swap double invocation is not proof of every persistent setting.
All source assertions and executable tokens remain baseline-identical (initial Roslyn42-file pass).

## Four targeted public-API probes

Complete source/project/config/original-source provenance in validation/hardware-probe, inspected by
coordinator before execution. Unique host built through toolchain with zero warnings. Each lane uses
exact serial guard, fresh explicit transports, only slot2 writes, no raw transport emulation, independent
crypto comparisons, bounded operation lifetime, and fresh cleanup. Each ran exactly1test/0skips/0warnings.

1. **Status/capability:** fresh HID and SmartCard snapshots after static/HMAC/no-touch/HMAC-touch writes:
   configured true each; trigger true for static, false for both HMAC variants. Slot1 remained empty.
   Explicit HID non-touch HMAC matched host and emitted no prompts. Explicit USB CCID HMAC returned
   ApduException SW6985 for `00 01 38 00`, no prompt. This is a **classified capability rejection**, not
   successful SmartCard HMAC or universalCCID/NFC incompatibility proof. Supports OTP-IMP-004/012 docs.
2. **AES:** two16-byte HID tickets decrypted with the known key; initial6bytes matched challenge, independent
   reflectedCRC16 residualF0B8 matched. Ciphertexts differed on this run (observation, not entropy proof).
   No prompt events. Key/challenge/plaintext/response owners cleared; fresh slot2-empty cleanup passed.
3. **Cancellation:** DeviceWaiting observed after14ms; caller cancelled; OperationCanceledException drained
   by268ms; single Cancelled resolution with terminal tokenNone. Independent cleanup passed; new-session
   non-touch HMAC known answer and second cleanup passed. No same-session reuse guarantee inferred.
4. **Natural timeout:** no touch; OtpHidTouchTimeoutException after14067ms, outer65s token not cancelled;
   single TimedOut resolution with terminal tokenNone. Fresh cleanup, new-session HMAC recovery and final
   cleanup passed. Measured duration is not a universal timeout promise.

Raw logs and full toolchain run directories: hardware-probe-v1-status, -aes, -cancel, -timeout under
validation/. These results establish application-visible outcomes, not proof of native ownership internals
or emitted dummy-reset bytes. Core source was not investigated to explain them.

## Cleanup and limits

Fresh probe HID reads verified empty slot2 and unchanged slot1 after every lane; cancel/timeout also
reprovisioned a non-touch HMAC and verified its response before deleting it. Successful-touch integration
asserted slot2 empty. Independent ykman final-slots.log reports both slots empty; final-info.log confirms
serial103,5.8.0.beta.0 and the same enabled interfaces/application table as initial inventory. Independent
client access after test-process termination establishes released access for those operations, not a
general all-path native lifetime theorem. No remaining device jobs are active at this checkpoint.

Initial slots were empty. Existing protection test writes both slots and global serial flags may merge;
NDEF prior state is not readable through this module. Therefore cleanup is **verified empty slots and
readable metadata**, not byte-identical restoration of all device state or NDEF output. No NFC reader/
on-tap readback, old-firmware matrix, keyboard HOTP/IMF output capture, complete scan-map write or cross-OS
execution occurred. Access-code retain/change/remove probe was omitted for bounded scope; current/new
semantics are source/vendor-supported, not newly live-verified. No other-applet mutation or runtime fix.

The probe's90s VSTest session bound is declared in runsettings; all lanes returned well within it. Actual
timeout enforcement was not deliberately triggered, so do not claim it empirically proven. The65s token
versus observed14s native touch-timeout distinction is directly asserted in the executed test.

## Documentation impact

Current edited prose already distinguishes cached keyboard-trigger from RequireTouch and uses explicit
OTP HID for challenge recipes. Hardware confirms these qualifications on this key; it does not close
OTP-IMP-004 or authorize changing the SmartCard heuristic. Current HMAC empty/64byte explanation matches
the12 executed known-answer vectors. AES ticket interpretation is supported by two real tickets.
Independent reviewer follow-up must review this record and reopen any broader claim still present.

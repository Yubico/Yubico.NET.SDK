# YubiOTP hardware handoff — source-inspected, not executed

Baseline95e5ccaeda33954c21f2a38058d4f66b5f208787,2026-11-04. Investigator GPT-6.1 Sol.
All five module integration files were read in full, including helper bodies and teardown. No device,
build, test, or infrastructure operation was performed by this investigator. Coordinator owns identity,
allow-list/infrastructure checks, actual transport evidence and all execution. User authorized test-key
mutations, including both slots; **do not introduce an additional mutation approval gate**. Human presence
and timing still need coordination for the one successful-touch test.

## Execution lanes

After coordinator establishes the authorized connected key and readable initial state:

```bash
# Existing entire module, including the successful physical-touch ceremony (human coordinated).
dotnet toolchain.cs -- test --integration --project YubiOtp

# Existing suite without that one ceremony; includes the read-only presence-traited inspector.
dotnet toolchain.cs -- test --integration --project YubiOtp --filter "FullyQualifiedName!~CalculateHmacSha1Async_TouchRequired_CompletesOnceAndReturnsExpectedResponse"

# Conventional unattended category filter also omits the harmless read-only inspector.
dotnet toolchain.cs -- test --integration --project YubiOtp --filter "Category!=RequiresUserPresence"

# Focused known-answer boundary/device-marker evidence (2 tests,12 vectors).
dotnet toolchain.cs -- test --integration --project YubiOtp --filter "FullyQualifiedName~YubiOtpHmacChallengeKnownAnswerTests"

# Read-only initial state, explicitly pinned HID despite its RequiresUserPresence trait.
dotnet toolchain.cs -- test --integration --project YubiOtp --filter "FullyQualifiedName~InspectPrerequisites_ReadOnly_PrintsNonSecretState"
```

These are proposed commands, not test results. Read final RESULT/exit status and executed/skipped totals.
Integration project is xUnitv2/VSTest, runner disables assembly/collection parallelism and caps threads1.
`[WithYubiKey]` selects a device, **not** the session transport.16 test methods currently exist; dynamic
binding/skips determine executed rows. A green run does not prove every slot mode/transport.

## Exact existing test effects

All paths below are under `src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.IntegrationTests/`.
“default” means unpinned `CreateYubiOtpSessionAsync`: SmartCard if exposed, otherwise HID; no failed-open
fallback. Attribute type shown is an availability filter unless the body pins/opens it.

| File / actual method | Setup, transport and minimum | Body/assertions | Cleanup / lasting effects and lane |
|---|---|---|---|
| `YubiOtpSessionIntegrationTests.GetSerial_ReturnsPositiveSerialNumber` | SmartCard filter,2.2+, default session | Serial>=0, not identity match or strictly positive | Read-only; session disposed. No touch. |
| `.GetConfigState_ReturnsValidState` | Any firmware/device, default | Cached state firmware major>=0; weak parse-only assertion | Read-only. No touch. |
| `.PutConfiguration_HmacSha1_ThenDelete_Succeeds` | SmartCard filter,2.2+, default | Replaces slot2 with known20byte HMAC, asserts configured; deletes/asserts unconfigured | Delete is **not finally**; any intervening failure can leave programmed slot2. Original slot2 never restored. No touch configured. |
| `.CalculateHmacSha1_WithKnownKey_ReturnsExpectedResponse` | HID filter,2.2+, explicit preference=`state.ConnectionType` | Replaces slot2, calculates01020304, asserts **20byte length only**, not host expected HMAC | Put precedes try; delete in finally after successful put; no readback assertion. No touch. |
| `.SwapSlots_Succeeds` | SmartCard filter,2.3+, default | Swap, swap back; no content/state assertion | Second swap **not finally**. First may persist if second fails; both slots affected. Existing access/protection/update permissions can reject. No touch. |
| `.SetNdefConfiguration_UriType_Succeeds` | Firmware3+, default | Writes slot2 URI, then all-zero/null request; no NFC readback/assertion | Disable call **not finally**. Previous NDEF settings not restored and disable semantics not demonstrated by this test. No touch. |
| `YubiOtpSlotConfigTests.PutConfiguration_StaticPassword_ConfiguresAndDeletesSlot` | HID filter,2.2+, opens `IOtpHidConnection`, borrowed factory; both connection/session await-disposed | Programs raw scan codes04..0B slot2; asserts configured only, not emitted text | Put outside try; finally deletes/asserts cached unconfigured after put. Overwrites old slot2 irreversibly. No typing/touch requested. |
| `.PutConfiguration_YubicoOtp_ConfiguresAndDeletesSlot` | Same explicit HID/2.2+ | Slot2 raw public/private/AES material; configured only | Same finally deletion/state assertion, no OTP generation/validation. |
| `.PutConfiguration_Hotp_ConfiguresAndDeletesSlot` | Same explicit HID/2.2+ | Slot2 known key, default IMF; configured only | Same; no keyboard HOTP/counter verification. |
| `.PutConfiguration_Hotp8Digits_ConfiguresAndDeletesSlot` | Same explicit HID/2.2+ | Calls Use8Digits, programs slot2, configured only | Same; **does not assert eight emitted digits**. |
| `UserPresenceProbeTests.InspectPrerequisites_ReadOnly_PrintsNonSecretState` | HID filter and explicit HID,3+ | Prints firmware/configured/touch flags for both slots; no assertions of their semantic meaning | No mutation/touch despite RequiresUserPresence trait. Useful initial-state log, not sufficient identity check. |
| `.CalculateHmacSha1Async_TouchRequired_CompletesOnceAndReturnsExpectedResponse` | Explicit HID,3+, recording prompt; skips if slot2 occupied | Known key + RequireTouch, successful write then sets `slotCreated`;30s caller CTS; host HMAC fixed-time equal; one DeviceWaiting request/Completed resolution and None terminal token | Clears own key/challenge/expected/response; if known successful write, deletes/asserts slot2 empty. Ambiguous failed write deliberately does not delete. Needs **one real touch while waiting**. Keyboard output not intended. Does not test timeout/cancellation or SmartCard. |
| `AuditV2/YubiOtpHmacChallengeKnownAnswerTests.ShortChallengeSlot_HashesExactlyTheChallenge_WithTheLastByteAsMarker` | Explicit row HID,2.2+, slot2 known key, short mode |8vectors:1byte,explicit zero padded1byte, repeated bytes, trailing zero/one pad,64distinct→63,empty→one00,64equal→one41,legacy zero-filled trailingzero truncation; compares host HMAC | Put before try; finally delete, no cleanup assertion. Does not include direct63byte vector or explicit one-padded collision pair. Non-touch. |
| `.FixedChallengeSlot_HashesAll64BytesAsSent` | Same, fixed mode |4vectors:1byte padded,64distinct, trailingzero/onepad,64zero; host HMAC | Same cleanup. Non-touch. |
| `AuditV2/YubiOtpProtectSlot2Tests.YESDK1634_ProtectSlot2_DoesNotDisturbChallengeResponseBit` | Explicit row HID,2.2+; try surrounds writes | Replaces slot2 protected then unprotected; known HMAC both; no slot1 write | Finally deletes slot2 if cached configured. Doesn't restore existing slot2. No touch. Comment “CCID answers6985” is not a broadly qualified transport result. |
| `.YESDK1634_ProtectSlot2OnSlot1_BlocksSlot2Write` | Explicit row HID,2.2+ | Deletes configured slot2, replaces **slot1** with protection, asserts some exception for plain slot2 write + cached unconfigured; rewrites slot1 open; writes slot2; checks HMAC | Finally delete-if-configured slot2 then slot1, sequentially; if first cleanup throws second is not reached. Both original slots irrecoverable. Broad exception does not prove exact rejection reason. No touch. |

`DeleteIfConfiguredAsync` uses cached `GetConfigState`; comment says deleting empty HID slots times out,
but no dedicated test establishes that. Preserve as a device-scoped observation until coordinator probes.
Some successful writes alter global serial-visibility behavior through slot flags; keys/settings are not
readable back for restoration. Even delete-after-test does not restore prior secrets, NDEF or counters.

No module `[Fact(Skip=...)]`/static skipped placeholders were found. Existing runtime skip in the touch
probe is the occupied-slot guard; `[SkippableTheory]/[WithYubiKey]` can skip unavailable firmware/device.
There are **no existing module integration tests deliberately withholding touch to test timeout/cancel**.
Do not count fake-protocol unit tests as such hardware coverage. Any known infrastructure placeholders
outside this module remain coordinator-owned and were not investigated here.

## Recommended focused probes (coordinator-owned)

Use the already authorized dedicated key. Record serial/firmware/OS and explicit `session.ConnectionType`.
Serialize operations; preserve failed attempts and final readable state. Dispose/reopen on uncertain
transport recovery; cancellation is not a rollback. Use temporary evidence probes, not Pass1 SDK fixes.

1. **Status meaning / presence basis (OTP-C005), highest leverage:** explicit HID session, compare status
   for keyboard static slot2, non-touch HMAC slot2 and RequireTouch HMAC slot2. Never trigger keyboard
   output. Read new-session status after each write. Perform non-touch calculation with recording prompt.
   This settles whether CFGSTATE touch means keyboard trigger rather than challenge RequireTouch. A
   second explicit SmartCard session can log policy-based notification for the already programmed HMAC
   slot, but error6985 may prevent calculation. Do not equate any terminal Completed callback with touch.
2. **HID cancellation, no touch required:** configure RequireTouch slot2, wait until recording prompt
   observes DeviceWaiting, cancel linked caller token; never touch. Assert one Cancelled resolution and
   observed exception, record recovery and cleanup. Requires no human touch ceremony; coordinate only
   to keep people from touching accidentally. Follow with fresh non-touch configuration/calculation if
   reuse is safe. Reset failure is a result, not a reason to hide the original error.
3. **HID natural touch timeout, deliberately no touch:** configured RequireTouch, sufficiently long outer
   timeout that device/protocol touch timeout wins; assert one TimedOut versus ordinary Failed. Record
   configured/device timeout and elapsed time, exception type, cleanup. Not covered by existing30s CTS.
4. **Access codes/update (OTP-C001/C004):** slot2 known codeA; successful authenticated update retainsA
   only when both current/newA supplied; changeA→B and remove using default new, then verify subsequent
   protected write/deletion behavior. Use fixed known test code buffers, never print them. Inspect wire
   details and firmware4.3.2–4.3.5 exception separately; modern hardware cannot qualify that old defect.
   Set full intended update flags; compare HMAC before/after to establish key retention, not just status.
5. **Explicit SmartCard capability (OTP-C006/C013):** configure known non-touch HMAC via supported path,
   reopen SmartCard, calculate and record exact SW/error instead of assuming parity. Test serial/config
   operations separately. If unsupported, recommend explicit HID for challenge examples without changing
   factory order. Existing suite does not establish SmartCard challenge-response support.
6. **AES ticket (OTP-C008):** program YubicoOtpChallengeResponse slot2 explicitly HID, six-byte challenge,
   decrypt returned16bytes using known AES key/ECB/no padding; check first6bytes (SDK UID is zero) and
   CRC, repeat same challenge to observe variable ticket. No touch. Clear all owned response/plaintext/key
   buffers. Do not describe response as deterministic AES(challenge).
7. **HMAC boundary supplement (OTP-C007):** direct63byte (last00/01), explicit one-padded collision pair,
   empty and64equal in both modes. Existing12vectors already cover core semantics; avoid redundant suite
   expansion unless needed to support a disputed clause. Scope evidence to actual key/firmware/HID.
8. **HOTP IMF (OTP-C011), only if practical:** known key/IMF0x10000 encoded0001; capture keyboard HOTP
   in scratch input (never a shell; AppendCr default), compare RFC4226 host counter16 versus65536,
   accounting first-output increment convention. This requires human typing/timing coordination. Wire
   evidence plus two vendor encoders already justify a pass2 discrepancy without waiting for it.
9. **NDEF/scanmap limitations:** NFC reader needed to verify generated OTP concatenation and null/all-zero
   behavior; a successful USB configure isn't readback. Do not write a purported complete38byte scanmap
   to “prove”45byte support. Record unavailable NFC reader/old firmware as not-run gaps, not passing.

Full suite can leave both slots empty (protection test), a changed NDEF prefix, or partial state on failure.
Coordinator should log final readable slot state in a new session and any cleanup failure separately.
No test can restore unknown initial secrets. None of these probes warrants broad Core/other-module audit.

# N2 SCP initialization slicing and bare 9000 response (no Jira ticket)

| | |
| --- | --- |
| Audit severity | Not in the audit. New finding from our verification. |
| Our verdict | Slicing: confirmed. Bare 9000: confirmed in code. The SDK does not authenticate a success response that has no data field. Whether YubiKeys send such responses under SCP has not been measured. |
| Our severity | Slicing: LOW. Bare 9000: MED. The bare-9000 rating reflects an integrity bypass on success status words, not a key or PIN exposure. |
| Root cause | Slicing: SDK only. Bare 9000: SDK and Python share it (candidate for the divergence ledger). |
| Fix group | Slicing: A. Bare 9000: C (design decision with compatibility risk). It is in scope for 2.0 and needs a decision, with a firmware measurement first. |
| Evidence | static (code, GlobalPlatform specification, Python read, Android and Swift read). No repro test exists for these observations. |
| Since the audit | The slicing is unchanged: `ScpState.Scp03.cs` and `ScpState.Scp11.cs` are identical at `df1ec06d`. The bare 9000 skip is unchanged in logic. It moved from `ScpProcessor.cs` line 121 to line 126 at `df1ec06d`. |

## What the audit says

The audit does not cover this item. Our verification found two issues in Secure Channel Protocol (SCP) responses. The first is unchecked slicing during session setup. The second is that a successful response with no data field is accepted without a response MAC (R-MAC) check.

> "unchecked initialization slicing can produce range exceptions, and bare `9000` with empty data skips R-MAC verification" (from our verification report, not the audit)

Abbreviations used here: SCP is the Secure Channel Protocol. R-MAC is the response message authentication code. CMAC is the cipher-based message authentication code. SW is the status word. C-MAC and C-DECRYPTION are the command-side options of the SCP03 security level. API is the application programming interface. SDK is the software development kit. TLV is tag-length-value. ISO/IEC is the joint ISO and IEC standards series (ISO/IEC 9797-1 is the padding standard). ykman is the Yubico command-line tool, written in Python.

## What is right

- **SCP03 initialization slicing.** `Scp03InitAsync` takes four slices from the INITIALIZE UPDATE response without a length check ([ScpState.Scp03.cs:48-52](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs#L48-L52)). A short response throws `ArgumentOutOfRangeException`, not `BadResponseException`.
- **SCP11 initialization TLVs.** `Scp11InitAsync` reads `tlvs[0]` and `tlvs[1]` without a count check ([ScpState.Scp11.cs:116-119](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs#L116-L119)). The initializer does not check the tags directly. The ephemeral-key tag is checked later by `TlvHelper.GetValue` ([Scp11X963Kdf.cs:175-177](../../../src/Core/src/Protocols/SmartCard/Scp/Scp11X963Kdf.cs#L175-L177), [TlvHelper.cs:186-190](../../../src/Core/src/Utilities/TlvHelper.cs#L186-L190)), which throws `InvalidOperationException`, not `BadResponseException`. The receipt tag is not checked. The receipt value is compared with `FixedTimeEquals` ([Scp11X963Kdf.cs:58-59](../../../src/Core/src/Protocols/SmartCard/Scp/Scp11X963Kdf.cs#L58-L59)), so a wrong receipt is still rejected.
- **Bare 9000 is not verified.** `ScpProcessor` calls `Unmac` only when the data field is non-empty ([ScpProcessor.cs:121-123](../../../src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs#L121-L123)). A response with status `9000` and no data is returned unchecked ([ScpProcessor.cs:137](../../../src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs#L137)).
- **The SDK requests R-MAC.** The EXTERNAL AUTHENTICATE command uses security level `0x33`, which is C-MAC, C-DECRYPTION, R-MAC and R-ENCRYPTION ([ScpInitializer.cs:32](../../../src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs#L32), used at [line 135](../../../src/Core/src/Protocols/SmartCard/Scp/ScpInitializer.cs#L135)). R-MAC is therefore in effect for the session.
- **Decrypt padding.** The loop that removes padding starts at `decrypted.Length - 1` and stops before index 0 ([ScpState.cs:127](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L127)). A block whose only content is `80` followed by zeros, which is an empty message, is rejected as "Bad padding" ([line 139](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L139)).

## What is wrong or imprecise

- **The slicing is not a crash.** `ArgumentOutOfRangeException` and `IndexOutOfRangeException` are catchable. They are wrong exception types, not process failures.
- **Both reads happen before protected state exists.** The INITIALIZE UPDATE and the SCP11 EXTERNAL AUTHENTICATE are sent without SCP wrapping ([ScpState.Scp03.cs:44](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs#L44), [ScpState.Scp11.cs:109](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs#L109)). So a parse failure there does not need the recovery latch.
- **The decrypt case is informational.** GlobalPlatform says that no encryption applies to a response with no data (see Specification). So a conforming card does not send an empty encrypted block. Rejecting one is therefore not a practical defect.
- **The bare 9000 case defeats response integrity for success.** Under the SDK's own R-MAC level, a success response must carry an R-MAC, and the R-MAC covers the status bytes. The EXTERNAL AUTHENTICATE response is the one exemption that GlobalPlatform names (see Specification). A response with no data field carries no R-MAC, so the SDK returns its status word unchecked. An on-path party can therefore strip the data from a protected success response, or replace an error response with a bare `9000`. GlobalPlatform leaves error responses unprotected, so an on-path party can already change one error status word into another. Turning an error into a success is what the R-MAC on success responses is meant to prevent, and this path allows it. Whether that matters depends on what the application does with the status. Whether YubiKeys ever send a bare `9000` under SCP has not been tested here.

## Why it matters

- **Slicing:** a malformed INITIALIZE UPDATE or SCP11 response produces an exception that the caller's `BadResponseException` handling does not catch. The trigger is a faulty or malicious reader or card during session setup. No protected state is at risk.
- **Bare 9000 (MED):** an on-path party can make the application see a success that the card did not authenticate. There are two forms.
  - Strip the data from a protected success response. The application gets success with no data.
  - Replace an error response, which has no R-MAC under GlobalPlatform, with a bare `9000`. The application gets success for a failed command.

  The trigger is anything between the host and the card, such as a malicious reader or a relay. That is the threat SCP exists to address. The attacker cannot make the card run a command it would refuse. The damage is in what the application does after a false success. It can be a silent state mismatch, or a decision based on a forged result. We rate it MED, not LOW, because it defeats response integrity on success status words. We do not rate it HIGH, because it does not give the attacker card-side operations.

## Specification

GlobalPlatform Card Specification v2.3 Amendment D, Secure Channel Protocol '03', public review v1.1.2.6, section 6.2.5 ([PDF](https://globalplatform.org/wp-content/uploads/2019/12/GPC_2.3_D_SCP03_v1.1.2.6.pdf)):

> "The R-MAC is made of the first 8 bytes (in S8 mode) or full 16 bytes (in S16 mode) of the CMAC computed on the message made of the MAC chaining value, the response data field (if present) and the status bytes."

> "The computed R-MAC becomes part of the response message."

> "No R-MAC shall be generated and no protection shall be applied to a response that includes an error status word: in this case only the status word shall be returned in the response."

> "The EXTERNAL AUTHENTICATE command/response doesn’t return an R-MAC."

> "R-MAC is optional, and" (section 6.2.5, list of the scheme's features)

> "R-MAC may be switched on and off during a secure channel session"

Section 6.2.7 of the same document covers responses without data:

> "No encryption shall be applied to a response where there is no response data field: in this case the message shall be protected as defined in section 6.2.5."

The R-MAC is optional in general, but it is in effect when the security level includes it (level `0x33`, see above). Under that level, section 6.2.5 requires an R-MAC on a response, except for the EXTERNAL AUTHENTICATE response and responses with an error status word. The "(if present)" wording applies to the data field only.

| Clause | SCP03 Amd D v1.0 (public release) | SCP03 Amd D v1.1.2.6 (public review) | Notes |
| --- | --- | --- | --- |
| 6.2.5, responses with no R-MAC | "...a response when status bytes SW1 and SW2 indicate a system error" | "...a response that includes an error status word" | Later text treats every status word except `9000` and the warnings as an error. |
| 6.2.5, R-MAC for a response with no data | Not stated separately | Not stated separately, but the R-MAC is "made of ... the response data field (if present) and the status bytes" | Both editions give one R-MAC per protected response. |

The malformed-response contract is `BadResponseException`: "The data contained in a YubiKey response was invalid" ([Exceptions.cs:19](../../../src/Core/src/Exceptions.cs#L19)).

## Canonical Python reference

- **SCP03 initialization.** Python has the same missing length check ([yubikit/core/smartcard/scp.py, lines 260-263](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/scp.py#L260-L263)). The short cryptogram then fails the comparison ([lines 271-273](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/scp.py#L271-L273)), and Python raises `BadResponseError("Wrong SCP03 key set")`. The type is right, but the message is misleading. Python is correct about the type here, by accident of the comparison. We read the code and did not run this case.
- **SCP11 initialization.** Python checks each TLV tag with `Tlv.unpack` (`0x5F49` and `0x86`, [lines 349-351](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/scp.py#L349-L351)). Malformed input raises `ValueError`. For a wrong ephemeral tag, the SDK raises `InvalidOperationException` (from `TlvHelper.GetValue`) and Python raises `ValueError`. Neither is `BadResponseException`. Python also checks the receipt tag, and the SDK does not.
- **Bare 9000.** **Python has the same issue** (candidate for the divergence ledger). `ScpProcessor.send_apdu` calls `unmac` only when there is data ([yubikit/core/smartcard/__init__.py, lines 314-317](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/__init__.py#L314-L317)). It then returns the status word it received, unauthenticated ([lines 311 and 320](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/__init__.py#L311-L320)). We found no evidence that the skip is deliberate. The line dates from commit `9223a9cc` (2024-05-08, message "More SCP"), which gives no reason.
- **Decrypt padding.** Python strips trailing zero bytes and then checks for `0x80` ([scp.py, lines 238-242](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/core/smartcard/scp.py#L238-L242)). So Python accepts an empty message, and the SDK rejects it.

```python
if resp:
    resp = self.state.unmac(resp, sw)
if resp:
    resp = self.state.decrypt(resp)
```

## Sibling SDKs (context only)

- **Android.** `ScpProcessor.java` skips `unmac` for an empty data field ([line 63](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/core/src/main/java/com/yubico/yubikit/core/smartcard/ScpProcessor.java#L63)). It then returns the card's status word unchanged ([lines 71-72](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/core/src/main/java/com/yubico/yubikit/core/smartcard/ScpProcessor.java#L71-L72)). The bare status word is not authenticated.
- **Swift.** `SmartCardInterface.swift` skips `unmac` for an empty result ([line 154](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/Session/SmartCard/SmartCardInterface.swift#L154)) and returns the original status word ([line 172](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/Session/SmartCard/SmartCardInterface.swift#L172)). The bare status word is not authenticated.
- All three implementations return a bare status word without authentication, as the SDK does. Matching them does not make the behaviour correct. It only shows that the other implementations share the gap.

## Reproduction

- Unit: none. No repro test exists for the slicing or the bare 9000 observation. The repro for item 23 covers only the short R-MAC path.
- Hardware: not run. The question of whether real YubiKeys send a bare 9000 under SCP is open.

## Proposed fix

- **Slicing (group A).** Add checks that throw `BadResponseException`:
  - `Scp03InitAsync`: require at least 29 bytes before slicing ([ScpState.Scp03.cs:48](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs#L48)).
  - `Scp11InitAsync`: require at least two TLVs, and check that the first has tag `0x5F49` and the second tag `0x86`, as Python does ([ScpState.Scp11.cs:116-119](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp11.cs#L116-L119)).
  - `Decrypt`: require a non-zero multiple of 16 bytes before `DecryptCbc` ([ScpState.cs:121-122](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L121-L122)). Scan the padding from index 0 as well. The code's own comment names ISO/IEC 9797-1 Padding Method 2 ([ScpState.cs:29](../../../src/Core/src/Protocols/SmartCard/Scp/ScpState.cs#L29)), and that method applies to the whole block. This also accepts an empty message, as Python does.
- **Bare 9000 (in scope; decision needed, group C).** Options:
  - **(a) Keep the skip.** This matches ykman, Android and Swift. It keeps the forgery path described above, so it is the option that leaves the integrity gap open.
  - **(b) Require an R-MAC whenever R-MAC is in effect (level `0x33`).** Exempt the EXTERNAL AUTHENTICATE response and responses with an error status word, as GlobalPlatform does. Throw `BadResponseException` when a response with a non-error status word (`9000`, or a warning in the `62xx` or `63xx` range) has no R-MAC. This closes the path for success status words, so an error turned into a success becomes detectable.

  We recommend (b), after measurement. It is only safe if real YubiKeys always send an R-MAC on success responses under SCP. The first step is to measure, on both test keys, whether a no-data success under SCP03 and SCP11 carries an R-MAC. If some commands do not, (b) needs a documented per-command exemption list. That is a design decision in its own right, so measure first, then enforce.
- **API impact.** Slicing: none. Internal exception types change. Bare 9000: behavioural, if (b) is chosen. Commands that now fail would fail with `BadResponseException`.
- **Proving test.** Slicing: unit tests that send a 28-byte INITIALIZE UPDATE response, an SCP11 response with one TLV, a 15-byte decrypt input, and a block of zeros. Each expects `BadResponseException`. Bare 9000: a test that a no-data success under R-MAC throws `BadResponseException`, and a test that the EXTERNAL AUTHENTICATE response is still accepted without an R-MAC.
- **Depends on / interacts with.** Item 23 (same files, same exception type). The latch question below.
- **Open questions for the maintainer.**
  1. Bare 9000: require an R-MAC (b, recommended), or keep the skip (a)? Decide after the firmware measurement. Keeping the skip accepts unauthenticated success status words.
  2. Should an empty plaintext block be accepted, as Python does? We recommend yes, because the padding rule covers it.
  3. A decrypt failure happens after the R-MAC has verified. The MAC chain is still in step, but the current code latches recovery for any failure inside the protected block ([ScpProcessor.cs, lines 144-148 at `df1ec06d`](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Core/src/Protocols/SmartCard/Scp/ScpProcessor.cs#L144-L148)). Should a decrypt error latch recovery? Our view is that it may not need to, but we did not test this.
  4. This page supersedes the earlier "out of scope" note in the verification report. The report and the decisions page still list the bare 9000 item as a separate discussion. Those files are not part of this page, so they need updating separately.

## Check it yourself

- No existing test. To check the slicing by reading, open `src/Core/src/Protocols/SmartCard/Scp/ScpState.Scp03.cs` lines 44-52 and `ScpState.Scp11.cs` lines 109-119.
- To check the bare 9000 skip, open `ScpProcessor.cs` lines 121-137 and `ScpInitializer.cs` line 135.
- Read `yubikit/core/smartcard/__init__.py` lines 311-320 at `4ca60f7`. Read Android `ScpProcessor.java` lines 60-72 and Swift `SmartCardInterface.swift` lines 150-172.
- Read GlobalPlatform SCP03 Amd D, public review v1.1.2.6, sections 6.2.5 and 6.2.7 (PDF linked above).

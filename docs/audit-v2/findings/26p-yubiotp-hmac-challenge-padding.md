# 26p YubiOTP HMAC challenge padding is ambiguous (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction. The wire collision is real. The report's claim that the device output depends on the slot's mode is not supported. The audit did not mention the empty-challenge divergence or the second collision below. |
| Our severity | LOW-MED |
| Root cause | SDK and Python share it (the padding rule and the missing mode in the API) |
| Fix group | C (real API design: an explicit challenge mode). A documentation-only step can come first. |
| Evidence | unit test; comparison with the padding code in yubikey-manager and the Yubico documentation. No device test. |
| Since the audit | `YubiOtpSession.cs` changed (user-presence plumbing in `CalculateHmacSha1CoreAsync` and `CreateUserPresenceContext`). `PadHmacChallenge` (lines 426-444 at the base) is unchanged. `IYubiOtpSession.cs` and `HmacSha1SlotConfiguration.cs` are unchanged. |

## What the audit says

`CalculateHmacSha1Async` pads challenges shorter than 64 bytes. A short challenge and its explicitly padded form therefore produce the same device request. The API does not check the challenge length against the slot's configured mode, so callers that mix lengths can lose input separation.

> "a short challenge and its explicitly padded representation produce identical device requests, for example, 0x41 and 0x41 followed by 63 zero byte. The API does not enforce input lengths against the slot's configured short- or fixed-challenge mode."

Abbreviations: HMAC-SHA1 is hash-based message authentication code with SHA-1. OTP here is the YubiOTP application, which has two slots. The short-challenge mode is the device setting that allows challenges under 64 bytes. SDK is software development kit.

## What is right

- **The collision is real at the request level.** `PadHmacChallenge` copies the input and fills the rest with a pad byte ([YubiOtpSession.cs:430-444](../../../src/YubiOtp/src/YubiOtpSession.cs#L430-L444)). The pad byte is `0x01` when the short challenge ends in zero, and `0x00` otherwise ([YubiOtpSession.cs:437-438](../../../src/YubiOtp/src/YubiOtpSession.cs#L437-L438)). For `[0x41]` the pad is `0x00`, so the request is `41 00 ... 00`. For `[0x41, 0x00 × 63]` the input is already 64 bytes, and the request is the same. The unit test fails today: "Collections are equal", both `[65, 0, 0, 0, 0, ...]`.
- **The API does not record the mode.** `CalculateHmacSha1Async` accepts any length up to 64 ([YubiOtpSession.cs:359-364](../../../src/YubiOtp/src/YubiOtpSession.cs#L359-L364)), and its interface documentation mentions only "up to 64 bytes" ([IYubiOtpSession.cs:211](../../../src/YubiOtp/src/IYubiOtpSession.cs#L211)). The session does not know whether the slot is in short mode.
- **The SDK documents the padding rule.** `UseShortChallenge` says: "The challenge is padded to 64 bytes with a byte value that differs from the last data byte." ([HmacSha1SlotConfiguration.cs:61-68](../../../src/YubiOtp/src/HmacSha1SlotConfiguration.cs#L61-L68)). That is the rule `PadHmacChallenge` implements, but the session never says which mode applies.

## What is wrong or imprecise

- **The report's device-side claim is not supported.** The report says device-output equality "depends on slot configuration". For one slot, two calls that send the same 64 bytes get the same response, whichever mode the slot uses, because the response depends on the key and the bytes received. What the mode changes is whether the verifier expects the short or the fixed form. That is an API question, not a device difference.
- **A second collision.** The padding rule also maps a challenge that ends in `0x00` to the same request as the 64-byte value that spells out the padding. For example `[0x41, 0x00]` pads to `41 00 01 ... 01`, and so does `[0x41, 0x00, 0x01 × 62]`. yubikey-manager uses the same rule and has the same collision. The audit describes only the zero-padding case.
- **The empty challenge differs from yubikey-manager.** The SDK sets the pad byte from the last data byte, and for an empty input that byte defaults to `0`, so the pad is `0x01` ([YubiOtpSession.cs:437-438](../../../src/YubiOtp/src/YubiOtpSession.cs#L437-L438)). yubikey-manager's test for a trailing zero is `b"".endswith(b"\0")`, which is false, so it pads with `0x00` ([yubiotp.py:918-921](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L918-L921)). The two SDKs send different 64-byte requests for an empty challenge. Whether the device treats them differently was not tested.
- **The SDK has a mode switch, and the caller can use it.** `UseShortChallenge` sets or clears the short-challenge flag on the slot ([HmacSha1SlotConfiguration.cs:65-68](../../../src/YubiOtp/src/HmacSha1SlotConfiguration.cs#L65-L68)). The constructor sets it by default ([HmacSha1SlotConfiguration.cs:46-47](../../../src/YubiOtp/src/HmacSha1SlotConfiguration.cs#L46-L47)). So every HMAC slot the SDK configures is in short-challenge mode unless the caller turns it off. The missing piece is that the challenge call does not look at that choice.

## Why it matters

- **Who triggers it.** An application that sends challenges of mixed lengths to one slot, or that compares a stored challenge with one sent later. A challenge-response scheme in which the verifier uses one length and the application uses another is the typical case.
- **Impact.** Two different inputs that the application treats as different produce the same response. This matters only if the verifier distinguishes the lengths. The Yubico documentation says so explicitly: both sides "must agree on the length of the challenge".
- **No key exposure.** The HMAC itself is not weakened. The problem is loss of input separation at the API.
- **Preconditions.** Mixed challenge lengths for one slot, with a verifier that needs to tell them apart.

## Specification

> "The size of the challenge sent to the YubiKey with `UseChallenge()` must align with the slot's configuration. ... If the slot is configured for HMAC-SHA1, the challenge must be 64 bytes long. However, if the slot has been configured with `UseSmallChallenge()`, a challenge smaller than 64 bytes is acceptable." (Yubico .NET SDK user manual, challenge-response, "Note")

> "An HMAC-SHA1 challenge is 64 bytes by default. The YubiKey also supports a short challenge mode (`UseSmallChallenge()`) where challenges, which are sent to a YubiKey with `CalculateChallengeResponse()`, can be configured to be less than 64 bytes."

> "You can use challenges smaller than 64 bytes without setting the short challenge mode by padding the end of the challenge with zeros. Regardless, both sides of the operation must agree on the length of the challenge." (same manual, "How to program a slot with a challenge-response credential", "Short challenge mode for HMAC-SHA1")

Sources: [Yubico .NET SDK user manual, challenge-response](https://docs.yubico.com/yesdk/users-manual/application-otp/challenge-response.html) and [how to program a challenge-response credential](https://docs.yubico.com/yesdk/users-manual/application-otp/how-to-program-a-challenge-response-credential.html). These pages describe the legacy .NET SDK. They are SDK documentation, not device specifications.

The device flag is defined in the vendor header:

> `#define CFGFLAG_HMAC_LT64 0x04 /* Set when HMAC message is less than 64 bytes */`

Source: [yubikey-personalization, `ykcore/ykdef.h`](https://github.com/Yubico/yubikey-personalization/blob/master/ykcore/ykdef.h), master branch at the time of writing (not pinned).

## Canonical Python reference

- **Python has the same ambiguity.** `calculate_hmac_sha1` pads with `ljust(HMAC_CHALLENGE_SIZE, b"\1" if challenge.endswith(b"\0") else b"\0")` ([yubikit/yubiotp.py:918-921](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L918-L921)). It has no mode parameter. This is a candidate for the divergence ledger.
- **Python's slot setup sets the short-challenge flag by default.** `HmacSha1SlotConfiguration.__init__` sets `HMAC_LT64` ([yubiotp.py:382](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L382)), and `lt64()` turns it off ([yubiotp.py:391-393](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/yubiotp.py#L391-L393)). The caller can choose the mode at configuration time, as in the SDK, but the calculation call does not check it.
- **The empty challenge is the divergence described above.** Python pads an empty challenge with `0x00`; the SDK pads it with `0x01`.

## Sibling SDKs (context only)

- The Android SDK sets the short-challenge flag by default and documents the switch: "if false, all challenges must be exactly 64 bytes long (default: true)" ([HmacSha1SlotConfiguration.java:61](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/yubiotp/src/main/java/com/yubico/yubikit/yubiotp/HmacSha1SlotConfiguration.java#L61) and [line 83](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/yubiotp/src/main/java/com/yubico/yubikit/yubiotp/HmacSha1SlotConfiguration.java#L83)). The challenge call's padding was not compared.

## Reproduction

- Unit: [YESDK1634_DistinctChallengeLengthsDoNotHaveSameRequestBytes](../../../src/YubiOtp/tests/Yubico.YubiKit.YubiOtp.UnitTests/AuditV2/YubiOtpAuditReproTests.cs#L27-L37). Asserts that `[0x41]` and `[0x41, 0 × 63]` produce different requests. **Result today (2026-10-10): fails, "Collections are equal".**
- Hardware: none. The response depends on the device's handling of the padding, and that was not tested.

## Proposed fix

- **Recommendation (two steps).**
  1. Now: document the ambiguity on `CalculateHmacSha1Async` ([IYubiOtpSession.cs:207-217](../../../src/YubiOtp/src/IYubiOtpSession.cs#L207-L217)) and on `UseShortChallenge`. State that callers must use one length convention per slot, that a short challenge and its explicitly padded 64-byte form produce the same request, and that the device cannot tell them apart.
  2. Decide the empty challenge. Reject zero-length input with `ArgumentException`, and remove the divergence from yubikey-manager.
- **Options considered (for the API).**
  - Option A: documentation only, as in step 1. No behaviour change. The collision remains for callers who mix lengths.
  - Option B (recommended by the verification report): an explicit mode. A fixed mode requires exactly 64 bytes and sends them unchanged. A short mode pads and is documented as matching a slot with `UseShortChallenge` enabled. The mode-less method stays, with its limitation documented. Additive.
  - Option C: reject any challenge shorter than 64 bytes unless a mode is given. Stronger, but breaking for callers that use short challenges today.
- **API impact.** Option A none. Option B additive. Option C breaking. Rejecting empty input is a small behaviour change.
- **Proving test.** For Option B: fixed mode rejects `[0x41]`; short mode produces the padded request; the existing test changes to assert the fixed-mode rejection. For step 1: a documentation review and a test that the empty challenge is rejected.
- **Depends on / interacts with.** Independent of the other findings. `YubiOtpSession.cs` has user-presence changes near this code, so rebase first.
- **Open questions for the maintainer.** Option A now and B later, or B now? Reject the empty challenge, or match yubikey-manager's `0x00` padding? A device check of the empty-challenge response would settle the second question.

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project YubiOtp --filter "FullyQualifiedName~YESDK1634_DistinctChallengeLengths"
```

Read in this order:

- [YubiOtpSession.cs:426-444](../../../src/YubiOtp/src/YubiOtpSession.cs#L426-L444) (`PadHmacChallenge`) and [YubiOtpSession.cs:351-368](../../../src/YubiOtp/src/YubiOtpSession.cs#L351-L368) (the public method).
- [HmacSha1SlotConfiguration.cs:42-68](../../../src/YubiOtp/src/HmacSha1SlotConfiguration.cs#L42-L68) (the default short-challenge flag and `UseShortChallenge`).
- `calculate_hmac_sha1` in `yubikit/yubiotp.py` (lines 901-927) and the flag handling at lines 382 and 391-393, in the Python reference.

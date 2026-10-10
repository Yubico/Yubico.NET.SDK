# 26o OATH authentication retry zero seed is not reproduced (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Not reproduced |
| Our severity | None. No defect was found, so no severity applies. |
| Root cause | None. No defect. |
| Fix group | None |
| Evidence | passing counterexample unit test; code reading |
| Since the audit | `OathSession.cs` changed (user-presence plumbing in `CalculateAsync`, `CalculateCodeAsync` and `CreateUserPresenceContext`). The retry path (lines 757-825 at the base) is unchanged. |

## What the audit says

The OATH authentication retry is said to replace the caller's seed with zeros, send the zeros to the device, and then reuse them.

> "OATH retry sends an all-zero seed. Cleanup clears the caller's original seed; the SDK's authentication retry then reuses it."

Abbreviations: OATH is the Open Authentication one-time password standard. PBKDF2 is password-based key derivation function 2. HMAC is hash-based message authentication code. SDK is software development kit. YKOATH is the Yubico protocol for the OATH applet.

## What is right

- **The method zeroes a key buffer.** `AuthenticateAndRetryAsync` derives a key from the password, and that key is zeroed after validation ([OathSession.cs:775-776](../../../src/Oath/src/OathSession.cs#L775-L776) and [OathSession.cs:801-804](../../../src/Oath/src/OathSession.cs#L801-L804)). Zeroing that buffer is what the SDK's rules require.
- **The zeroing is documented.** The documentation of the internal helper `AuthenticateWithDerivedKeyAsync` says it "unconditionally zeroes" the key "whether validation succeeds or fails (e.g. with `WrongPassword`)" ([OathSession.cs:782-792](../../../src/Oath/src/OathSession.cs#L782-L792)). The code does that.
- **The audit is right that the derived key and the caller's password are different objects.** The caller's password is a borrowed `ReadOnlyMemory<byte>` from `passwordProvider` ([OathSession.cs:774](../../../src/Oath/src/OathSession.cs#L774)).

## What is wrong or imprecise

- **The buffer that is zeroed is the derived key, not the caller's seed.** `DeriveKey` returns a new array each time ([OathSession.cs:600-610](../../../src/Oath/src/OathSession.cs#L600-L610)). Only that array is zeroed ([OathSession.cs:803](../../../src/Oath/src/OathSession.cs#L803)). The caller's password is not zeroed by the SDK, and it should not be: the SDK does not own it.
- **The key is never sent.** `ValidateAsync` sends an HMAC of the device challenge under the key, plus a random client challenge ([OathSession.cs:628-641](../../../src/Oath/src/OathSession.cs#L628-L641)). No key bytes, and no zeros, reach the wire. The "all-zero seed" cannot be sent by this path.
- **The zeroed key is not reused.** The zeroing happens in the `finally` after `ValidateAsync` has finished ([OathSession.cs:799-803](../../../src/Oath/src/OathSession.cs#L799-L803)). The retry then calls the operation again ([OathSession.cs:778](../../../src/Oath/src/OathSession.cs#L778)). That call does not use the key. It relies on the session already being unlocked, which `ValidateAsync` sets ([OathSession.cs:662](../../../src/Oath/src/OathSession.cs#L662)).
- **The retry is not a second validation.** The audit's wording suggests the retry validates a zeroed key. The code validates once, and only the operation is retried after a successful validation.

## Why it matters

Nothing in this item is a defect, so there is no impact to fix. Two related observations are worth recording, but they are not part of this finding:

- The only test for this path covers the failure path (a wrong password). The success path, where validation works and the operation is retried, has no AuditV2 test.
- The audit's wording, "silently replaces the intended credential seed with zeros", could lead a maintainer to change correct code. The item should be closed, with the explanation kept in this file.

## Specification

> "The key to be set is expected to be a user-supplied UTF-8 encoded password passed through 1000 rounds of PBKDF2 with the ID from select used as salt." (YKOATH protocol, SET CODE instruction)

Source: [YKOATH protocol](https://developers.yubico.com/OATH/YKOATH_Protocol.html). The SDK's `DeriveKey` uses 1000 iterations and the SELECT ID as salt ([OathSession.cs:600-610](../../../src/Oath/src/OathSession.cs#L600-L610)). The derivation matches the protocol. The protocol does not describe a retry.

The SDK's own remark is the governing contract for this method:

> "Validates an already-derived access key and unconditionally zeroes it once the attempt completes, whether validation succeeds or fails (e.g. with `WrongPassword`)." ([OathSession.cs:782-786](../../../src/Oath/src/OathSession.cs#L782-L786))

## Canonical Python reference

- **Python derives the key the same way.** `derive_key` uses `pbkdf2_hmac("sha1", ..., salt, 1000, 16)` ([yubikit/oath.py:230-231](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L230-L231), called at [oath.py:326-331](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L326-L331)).
- **Python validates the same way.** `validate` sends an HMAC of the device challenge and checks the device's HMAC of a random challenge ([oath.py:333-351](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L333-L351)). The key is not sent.
- **Python has no retry helper.** The caller derives and validates. There is nothing to compare with the SDK's retry method.

## Sibling SDKs (context only)

Not checked for this item. The finding is closed by code reading in the .NET SDK.

## Reproduction

- Unit: [YESDK1634_RetryDoesNotZeroCallerPassword](../../../src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs#L81-L103). Expects the caller's password bytes to be unchanged after a failed validation, and exactly three commands (SELECT, LIST, VALIDATE). **Result today (2026-10-10): passes.** This is the only passing test in the Oath AuditV2 set. It is a counterexample, not a repro, and it covers the failure path only.
- Hardware: none.

## Proposed fix

None for this alleged finding. The audit's claim is not reproduced and the code is correct as written.

Optional, outside this finding: add a unit test for the success path. A test that a successful validation followed by a retry runs the operation once more and sends exactly one VALIDATE would cover what this item does not.

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project Oath --filter "FullyQualifiedName~YESDK1634_RetryDoesNotZeroCallerPassword"
```

Read in this order:

- [OathSession.cs:757-805](../../../src/Oath/src/OathSession.cs#L757-L805) (the retry method, the derived key, and the zeroing).
- [OathSession.cs:613-692](../../../src/Oath/src/OathSession.cs#L613-L692) (`ValidateAsync`, which uses the key locally and never sends it).
- `derive_key` and `validate` in `yubikit/oath.py` (lines 230-231 and 326-351) in the Python reference.

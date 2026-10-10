# 28 and 25i OATH period zero breaks batch calculation (YESDK-1636)

| | |
| --- | --- |
| Audit severity | MED/LOW (#28); MED (25i, audit row 25, which lists the Jira key as YESDK-1633) |
| Our verdict | Confirmed (#28); Confirmed, with a correction (25i, see "What is wrong or imprecise") |
| Our severity | MED |
| Root cause | SDK and Python share it |
| Fix group | A (no-brainer): reject a zero TOTP period at the parse boundaries. Open decision only for the batch policy (see "Proposed fix") |
| Evidence | unit test; hardware (no-touch run, recorded) |
| Since the audit | `src/Oath/src/OathSession.cs` changed (user-presence plumbing in `CalculateAsync`, `CalculateCodeAsync` and `CreateUserPresenceContext`). The batch loop and the division at line 450 are unchanged. `Credential.cs`, `CredentialData.cs` and `Code.cs` are unchanged. The fix needs a rebase onto the new `CreateUserPresenceContext` signature. |

## What the audit says

The SDK accepts an imported URI with `period=0`, stores it in the credential name, and then divides by zero when it calculates the batch. For #25i, the audit states the general case: invalid OATH periods abort batch code calculation.

> "The SDK accepts an imported URI with period=0. It stores that period in the credential's name, then divides by zero when calculating its OTP. CalculateAllAsync threw DivideByZeroException in two fresh sessions and returned no batch result."

Abbreviations: OATH is the Open Authentication one-time password standard. TOTP is time-based one-time password, and HOTP is HMAC-based one-time password. YKOATH is the Yubico protocol for the OATH applet. SDK is software development kit. URI is uniform resource identifier.

## What is right

- The root cause is correct. A zero period reaches an integer division by zero:
  - The period is written into the name as a `0/` prefix ([Credential.cs:112-115](../../../src/Oath/src/Credential.cs#L112-L115)).
  - The prefix is parsed back as period 0 ([Credential.cs:146-151](../../../src/Oath/src/Credential.cs#L146-L151)).
  - `CalculateAllAsync` treats any non-default TOTP period as a reason to recalculate one credential ([OathSession.cs:566-570](../../../src/Oath/src/OathSession.cs#L566-L570)).
  - That recalculation divides the timestamp by the period ([OathSession.cs:450](../../../src/Oath/src/OathSession.cs#L450)).
- The import path accepts the zero. `CredentialData.ParseUri` reads `period` with `int.Parse` and never checks it ([CredentialData.cs:161-164](../../../src/Oath/src/CredentialData.cs#L161-L164)). `PutCredentialAsync` then writes the prefixed name to the device without a check ([OathSession.cs:244-265](../../../src/Oath/src/OathSession.cs#L244-L265)).
- The batch uses a fixed 30-second counter for every credential, as the audit says ([OathSession.cs:527](../../../src/Oath/src/OathSession.cs#L527)). The device does not know the stored period either, so it returns a truncated code for the zero-period entry, as it does for every TOTP entry.
- The hardware run reproduced the batch failure. The valid credential in the same batch was not returned. See "Reproduction".

## What is wrong or imprecise

- **The existing guard does not help the batch.** `Code.ParseCode` already rejects a TOTP period of 0 or less ([Code.cs:63-66](../../../src/Oath/src/Code.cs#L63-L66)), but the batch never reaches it. The batch uses `Code.FormatCode` directly for default-period entries. Non-default periods go through `CalculateCodeAsync`, whose division happens before `Code.ParseCode`, at [OathSession.cs:450](../../../src/Oath/src/OathSession.cs#L450). `Code.FormatCode` also divides by the period for every TOTP credential ([Code.cs:89-93](../../../src/Oath/src/Code.cs#L89-L93)), so the check has to move to the parse boundary, not sit next to either division.
- **Negative periods do not abort the batch (25i correction).** The TOTP name pattern accepts only digits ([Credential.cs:36](../../../src/Oath/src/Credential.cs#L36)). A stored `-30/demo` is therefore read back as a 30-second credential named `-30/demo`. Nothing divides by it. Import still accepts `period=-30` and writes the name, so the import check should reject negatives too, but they are not a batch abort. This is by reading the pattern; it was not run.
- **Other period prefixes also abort the batch (25i scope).** `int.Parse` throws `OverflowException` for a prefix above `int.MaxValue`. `ParseCredentialId` calls it for every entry ([Credential.cs:149-151](../../../src/Oath/src/Credential.cs#L149-L151)), and `CalculateAllAsync` calls that ([OathSession.cs:561](../../../src/Oath/src/OathSession.cs#L561)). A name such as `99999999999/demo` would abort the batch the same way. The SDK cannot write such a name, but another OATH client can. Not run; by reading.
- **`\d` matches more than ASCII digits.** The same pattern uses `\d`, which matches Unicode decimal digits. `int.Parse` with the invariant culture rejects them with `FormatException`. This is also a batch abort, and it is also by reading only.
- **HOTP reports period 0 by design.** `ParseCredentialId` returns 0 for HOTP ([Credential.cs:163](../../../src/Oath/src/Credential.cs#L163), [Credential.cs:166](../../../src/Oath/src/Credential.cs#L166)). Any new validation must apply only to TOTP. `Credential.Period` is documented as "Only meaningful for TOTP credentials" ([Credential.cs:64-66](../../../src/Oath/src/Credential.cs#L64-L66)).
- **The hardware test history.** [agent-reports/G.md](../evidence/agent-reports/G.md) records the hardware test as not run. That is superseded by the recorded run in [hardware-results.md](../evidence/hardware-results.md). The first version of the test used the HOTP type byte (`0x11`). It was corrected to TOTP with SHA-1 (`0x21`) before the recorded run.

## Why it matters

- **Who can trigger it.** Anyone who can add an OATH credential to the device. YKOATH marks PUT as requiring authentication when a password is set, so the attacker needs the access code or an unprotected applet. A user importing a bad `otpauth://` URI with `period=0` triggers it without any attack.
- **Impact.** `CalculateAllAsync` throws `DivideByZeroException`. The application gets no codes for any credential on that device, not only the bad one. Single-credential calculation for the other credentials still works. The failure is loud, so no wrong codes are returned.
- **Why the import check matters.** The SDK stores the zero without complaint. The failure then appears later, on a different call, far from the cause.
- **No secret exposure.** Nothing in this path discloses key material.

## Specification

> "T = (Current Unix time - T0) / X, where the default floor function is used in the computation." (RFC 6238, section 4.2)

> "X represents the time step in seconds (default value X = 30 seconds) and is a system parameter." (RFC 6238, section 4.1)

Source: [RFC 6238, section 4.2](https://www.rfc-editor.org/rfc/rfc6238.html#section-4.2). The RFC never says that X must be positive, but X is the divisor, so zero has no meaning.

> "The period parameter defines a period that a TOTP code will be valid for, in seconds. The default value is 30." (Google Key URI Format, "Period")

Source: [Key URI Format](https://github.com/google/google-authenticator/wiki/Key-Uri-Format). The same page also says "Currently, the period parameter is ignored by the Google Authenticator implementations." The page does not say that the value must be positive.

> "Performs CALCULATE for all available credentials, returns name + response for TOTP and just name for HOTP and credentials requiring touch." (YKOATH, CALCULATE ALL)

Source: [YKOATH protocol](https://developers.yubico.com/OATH/YKOATH_Protocol.html), CALCULATE ALL instruction. The protocol does not define the name format. The `period/` prefix is a convention. The SDK, yubikey-manager, the Android SDK, the Swift SDK and the legacy .NET SDK all use it.

SDK contract. `IOathSession.CalculateAllAsync` says "Calculates codes for all credentials on the device. HOTP and touch-required credentials return `null` codes." ([IOathSession.cs:98-99](../../../src/Oath/src/IOathSession.cs#L98-L99)). The contract does not say what happens to a malformed entry. Option 2 below would blur the meaning of `null`.

Project rule. `CLAUDE.md` (Forward compatibility doctrine) says: "Malformed encodings, missing required fields, and security-critical protocol data remain strict." ([CLAUDE.md:142](../../../CLAUDE.md#L142)). That favours rejecting a bad period, and the batch policy question below is about how the batch stays strict and still returns the other entries.

| Clause | RFC 6238 | YKOATH | Notes |
| --- | --- | --- | --- |
| Time step X | 4.1 | not defined | Divisor; zero is undefined |
| T computation | 4.2 | CALCULATE ALL | Floor division by X |
| Period name prefix | not defined | not defined | Host convention shared by all five implementations |

## Canonical Python reference

Python has the same issue. This is a candidate for the divergence ledger.

- Import accepts zero: [yubikit/oath.py:143](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L143) (`period=int(params.get("period", DEFAULT_PERIOD))`).
- Name prefix and read-back: [oath.py:193](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L193) and [oath.py:209](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L209).
- Division: `_get_challenge` divides by the period ([oath.py:243-245](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L243-L245)). The batch recalculates non-default periods ([oath.py:524-529](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L524-L529)), so a zero period raises `ZeroDivisionError` and aborts the batch.
- The CLI `--period` option has no lower bound ([ykman/_cli/oath.py:471-477](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/ykman/_cli/oath.py#L471-L477)).
- Deliberate? No evidence. `git blame` puts the parse line in commit `677b91cd` (2020, "Implement yubikit OATH and use for CLI oath commands."). The recalculation branch dates from `1cb1006d` (2021, "Fix prompting for touch on non-standard period."). Neither message mentions zero.

## Sibling SDKs (context only)

- Android avoids the exception. For period 0 it sends an all-zero challenge and returns a code for counter 0 ([OathSession.java:506-507](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/oath/src/main/java/com/yubico/yubikit/oath/OathSession.java#L506-L507), [OathSession.java:730-731](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/oath/src/main/java/com/yubico/yubikit/oath/OathSession.java#L730-L731)). That hides the error instead of failing.
- Swift divides a `Double` by the period and converts it to `UInt64` ([OATHSession.swift:290](https://github.com/Yubico/yubikit-swift/blob/8cd5583a489a2f7ab7a1c7669518e68b26b6e475/YubiKit/YubiKit/OATH/OATHSession.swift#L290)). With period 0 that conversion of infinity is expected to trap. Not run.
- The legacy .NET SDK v1 models the period as an enum with `Undefined = 0` and 15, 30 and 60 ([CredentialPeriod.cs:20-26](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.YubiKey/src/Yubico/YubiKey/Oath/CredentialPeriod.cs#L20-L26)). Zero is not a divisor there.

## Reproduction

- Unit: [YESDK1636_MalformedPeriodDoesNotAbortNormalBatchEntry](../../../src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs#L26-L48). Asserts that a normal credential after a `0/demo` entry is still returned, and that one CALCULATE ALL is sent. **Result today (2026-10-10): fails with `DivideByZeroException`.**
- Unit: [YESDK1636_ParseUriRejectsZeroPeriod](../../../src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs#L16-L24). Asserts that `ParseUri` rejects `period=0` with `ArgumentException`. **Result today: fails, no exception is thrown.** This test is not listed in the verification report; it is in the same file.
- Hardware: [YESDK1636_RawPutZeroPeriodDoesNotAbortNormalBatch](../../../src/Oath/tests/Yubico.YubiKit.Oath.IntegrationTests/AuditV2/OathAuditReproTests.cs#L13-L54). Raw PUT of `0/audit-v2-invalid` and a valid credential, then `CalculateAllAsync`. Recorded in the no-touch phase of [hardware-results.md](../evidence/hardware-results.md): `CalculateAllAsync` threw `System.DivideByZeroException` and the valid credential was not returned. Cleanup by raw DELETE succeeded. The record does not name the test key used. Not rerun for this document.

## Proposed fix

- **Recommendation.**
  1. Reject a TOTP period of 0 or less at the parse boundaries: `CredentialData.ParseUri` (for `oathType == Totp`, [CredentialData.cs:161-164](../../../src/Oath/src/CredentialData.cs#L161-L164)) and `Credential.ParseCredentialId` ([Credential.cs:149-151](../../../src/Oath/src/Credential.cs#L149-L151)). Parse with `int.TryParse` (or a wider range check) using the invariant culture, and treat overflow, non-ASCII digits and non-positive values as malformed. Only TOTP is checked; HOTP keeps period 0.
  2. In `CalculateAllAsync`, skip a malformed entry with a diagnostic and keep the rest of the batch ([OathSession.cs:560-575](../../../src/Oath/src/OathSession.cs#L560-L575)). Log the entry's length, not its name. `CLAUDE.md` says to log lengths and IDs only ([CLAUDE.md:104](../../../CLAUDE.md#L104)).
  3. Keep a guard in `CalculateCodeAsync` before the division ([OathSession.cs:448-450](../../../src/Oath/src/OathSession.cs#L448-L450)) for locally built `Credential` objects. This is defensive; a caller can construct a zero-period TOTP `Credential` directly.
  4. Leave `Code.ParseCode` ([Code.cs:63-66](../../../src/Oath/src/Code.cs#L63-L66)) as a last line of defence, or remove it once the parse boundary rejects zero.
- **Options considered for the batch policy** (the only open decision):
  - Option 1, omit and log (recommended). No API change. Breaks the literal reading of "calculates codes for all credentials", but the malformed entry has no valid code to return.
  - Option 2, return the entry with a `null` code. No signature change, but `null` already means "HOTP or touch", so callers cannot tell them apart.
  - Option 3, return a per-entry error object. Clear, but a breaking change to the return type.
- **API impact.** `ParseUri` now throws `ArgumentException` for a TOTP period of 0 or less. That is a behaviour change for callers who relied on the zero being accepted. Batch behaviour changes only for malformed entries.
- **Proving tests.** The two existing unit tests, plus new ones for: HOTP with period 0 still parses; a batch with an overflowing prefix still returns the valid entries; a batch with a non-ASCII digit prefix still returns the valid entries. Then rerun the hardware test on an allow-listed key.
- **Depends on / interacts with.** Shares one root cause with #25i (the same change fixes both). `OathSession.cs` has user-presence changes in the same methods, so rebase first. The fix for #26k (device ID checks) touches `CalculateCodeAsync` too.
- **Open questions for the maintainer.** Option 1 or Option 2 for the batch policy. Should a malformed entry be logged at warning level or debug level?

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project Oath --filter "FullyQualifiedName~YESDK1636"
dotnet toolchain.cs -- test --integration --project Oath --filter "FullyQualifiedName~YESDK1636_RawPutZeroPeriodDoesNotAbortNormalBatch"
```

The hardware command needs an allow-listed device. It writes OATH state and deletes its own two credentials in a `finally` block. Do not run it while another process is using the device.

Read in this order:

- [OathSession.cs:448-450](../../../src/Oath/src/OathSession.cs#L448-L450) (the division) and [OathSession.cs:566-570](../../../src/Oath/src/OathSession.cs#L566-L570) (the recalculation branch).
- [Credential.cs:112-115](../../../src/Oath/src/Credential.cs#L112-L115) and [Credential.cs:146-151](../../../src/Oath/src/Credential.cs#L146-L151) (prefix write and parse).
- RFC 6238 section 4.2, and `yubikit/oath.py` lines 209 and 243-245 in the Python reference.

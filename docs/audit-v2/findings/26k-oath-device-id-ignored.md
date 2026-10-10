# 26k OATH operations ignore the credential's device identity (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only (Python checks device identity in one method and cannot check it in the raw-ID methods) |
| Fix group | B (small API decision: exception type and whether to check all operations or only the object-based ones) |
| Evidence | unit test (recorded commands) |
| Since the audit | `OathSession.cs` changed (user-presence plumbing in `CalculateAsync` and `CalculateCodeAsync`). The methods that need the check are the same, and the check goes at the top of each. `Credential.cs` is unchanged. |

## What the audit says

The `Credential` type is identified by its device ID together with its raw ID. The delete, rename and calculate operations send only the raw ID to the session's device, so a handle from another device can target a same-named credential on the current one.

> "Passing a handle from device A into an authorized session for device B therefore targets B's same-named credential instead of rejecting the device mismatch."

Abbreviations: OATH is the Open Authentication one-time password standard. SDK is software development kit. API is application programming interface. TLV is tag-length-value, the card's encoding.

## What is right

- **The type declares the identity.** The `Credential` remarks say that credentials "are uniquely identified by the combination of DeviceId and Id" ([Credential.cs:25-27](../../../src/Oath/src/Credential.cs#L25-L27)). `Equals` and `GetHashCode` use the device ID too ([Credential.cs:170-194](../../../src/Oath/src/Credential.cs#L170-L194)).
- **The operations do not check it.** The four operations send the raw ID without comparing device IDs:
  - Delete builds its name TLV from `credential.Id` ([OathSession.cs:341-343](../../../src/Oath/src/OathSession.cs#L341-L343)).
  - Rename uses the old ID and the new ID ([OathSession.cs:357-367](../../../src/Oath/src/OathSession.cs#L357-L367)).
  - `CalculateAsync` uses `credential.Id` ([OathSession.cs:393](../../../src/Oath/src/OathSession.cs#L393)).
  - `CalculateCodeAsync` uses `credential.Id` ([OathSession.cs:459](../../../src/Oath/src/OathSession.cs#L459)).
- **The session knows its device ID.** `DeviceId` is a public property on the session ([OathSession.cs:49](../../../src/Oath/src/OathSession.cs#L49)), so the comparison is available at every call.
- **The unit test reproduces the problem.** [YESDK1634_ForeignCredentialCannotTargetLocalDevice](../../../src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs#L50-L79) runs all four operations with a foreign credential whose ID matches a local one. It expects `ArgumentException` and no further commands. **Result today (2026-10-10): all four variants fail with "No exception was thrown".**

## What is wrong or imprecise

- **The gap is in the .NET object-based methods.** Python checks device identity in its object-based `calculate_code` method, which raises `ValueError` when the device IDs differ ([yubikit/oath.py:542-543](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L542-L543)). Python's raw-ID methods take bytes, so they cannot receive a foreign credential handle at all. The audit's claim is right for .NET. See the Python section.
- **The `CalculateAsync` impact is a wrong key, not only a wrong target.** The raw HMAC operation returns the response of the local same-named key. An application that verifies the response against the foreign device's key would get a valid-looking response from the wrong key. The audit does not describe this case.
- **The device ID changes on reset.** `DeviceId` is derived from the applet's salt ([OathSession.cs:167](../../../src/Oath/src/OathSession.cs#L167)) and is recomputed on `ResetAsync` ([OathSession.cs:594](../../../src/Oath/src/OathSession.cs#L594)). A credential obtained before a reset therefore fails the new check. Today it would reach the device and fail there with "no such object", since a reset removes all credentials. This is a small change in the error, not a new failure.

## Why it matters

- **Who triggers it.** An application that holds `Credential` objects from one device and runs an operation in a session on another. This happens when two YubiKeys are connected and an app keeps a list of credentials across devices. A common case: both keys hold an account named the same way, for example "GitHub:me".
- **Impact.** Delete and rename change or remove the local credential the user did not mean to touch. Calculate returns the local key's result as if it came from the intended credential. Both are silent.
- **Preconditions.** Two devices, a handle from one used with a session on the other, and a same-named credential on the second device. The operation must be authorized on the second device.
- **Not a key leak.** No secret moves to another party. The problem is the wrong object being changed.

## Specification

No protocol specification governs this. The YKOATH protocol has no device identity: it identifies credentials only by name. The device ID is an SDK concept, derived from the applet salt. The governing contract is the SDK's own remark:

> "Credentials are uniquely identified by the combination of DeviceId and Id." (`Credential` XML documentation, [Credential.cs:25-27](../../../src/Oath/src/Credential.cs#L25-L27), `cref` markup removed)

The Android SDK applies the same contract in its object-based methods (see Sibling SDKs).

## Canonical Python reference

- **Python checks device identity in its object-based `calculate_code` method.** It raises `ValueError` when the device IDs differ ([oath.py:542-543](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L542-L543)). Its raw-ID methods cannot express a foreign credential handle and have no equivalent identity check: `delete_credential` ([oath.py:482-488](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L482-L488)), `rename_credential` ([oath.py:428-445](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L428-L445)) and `calculate` ([oath.py:463-478](https://github.com/Yubico/yubikey-manager/blob/4ca60f706af930459138d8dc0f0f953480e1c7a4/yubikit/oath.py#L463-L478)) take raw bytes. Python is correct on the object-based path. The raw-ID methods are a different API shape with nothing to compare, so this is not a divergence candidate.

## Sibling SDKs (context only)

- Android checks the device identity in its object-based methods and throws `IllegalArgumentException`: `calculateCode` ([OathSession.java:500-503](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/oath/src/main/java/com/yubico/yubikit/oath/OathSession.java#L500-L503)), `deleteCredential(Credential)` ([OathSession.java:626-628](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/oath/src/main/java/com/yubico/yubikit/oath/OathSession.java#L626-L628)) and `renameCredential(Credential, ...)` ([OathSession.java:672-676](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/oath/src/main/java/com/yubico/yubikit/oath/OathSession.java#L672-L676)). This is the pattern the fix should follow.

## Reproduction

- Unit: [YESDK1634_ForeignCredentialCannotTargetLocalDevice](../../../src/Oath/tests/Yubico.YubiKit.Oath.UnitTests/AuditV2/OathAuditReproTests.cs#L50-L79), four variants (`delete`, `rename`, `calculate`, `code`). Asserts `ArgumentException` and no commands after the initial SELECT. **Result today: all four fail, "No exception was thrown".**
- Hardware: none. The recorded commands are enough to show the behaviour.

## Proposed fix

- **Recommendation.** At the top of `DeleteCredentialAsync`, `RenameCredentialAsync`, `CalculateAsync` and `CalculateCodeAsync` ([OathSession.cs:334-502](../../../src/Oath/src/OathSession.cs#L334-L502)), compare `credential.DeviceId` with `DeviceId` and throw `ArgumentException`. Do it before any TLV is built or any command is sent.
- **Options considered.**
  - Option 1 (recommended): check all four operations and use `ArgumentException`, which the existing test expects and which matches the parameter being wrong for this session.
  - Option 2: check only delete and rename, the operations that change state. Leaves the wrong-key calculation path open. Not recommended.
  - Option 3: `InvalidOperationException`, because the session is the thing that is wrong. The existing test expects `ArgumentException`, and the Android precedent is also an argument-type exception.
- **API impact.** Callers who pass a handle from another device now get an exception. No signature change.
- **Proving test.** The existing test (four variants, no commands). Add a positive control: a `Credential` from the same device still works.
- **Depends on / interacts with.** Shares the methods with the #28 change (`CalculateCodeAsync`). The user-presence plumbing in the same methods has changed, so rebase first.
- **Open questions for the maintainer.** Exception type (the recommendation is `ArgumentException`). Whether a stale handle after reset should get a dedicated message.

## Check it yourself

From the worktree root:

```bash
dotnet toolchain.cs -- test --project Oath --filter "FullyQualifiedName~YESDK1634_ForeignCredentialCannotTargetLocalDevice"
```

Read in this order:

- [Credential.cs:25-27](../../../src/Oath/src/Credential.cs#L25-L27) (the identity contract).
- [OathSession.cs:334-502](../../../src/Oath/src/OathSession.cs#L334-L502) (the four operations and where the check would go).
- `credential_id` and `device_id` handling in `yubikit/oath.py` lines 428-545 in the Python reference.

# 25g Nonpositive WebAuthn batch size (YESDK-1633)

| | |
| --- | --- |
| Audit severity | MED (sub-item of audit item #25) |
| Our verdict | Confirmed, with a correction |
| Our severity | LOW-MED. LOW for conforming hardware. MED for faulty or hostile GetInfo data. |
| Root cause | SDK and Python share it, for negative values only |
| Fix group | A (no-brainer) |
| Evidence | unit test; code inspection (no hardware run) |
| Since the audit | Unchanged at current yubikit. `AuthenticatorInfo.cs`, `ExcludeListPreflight.cs`, and the registration call site are identical at the branch base and at `df1ec06d`. |

## What the audit says

The audit says that a nonpositive batch size stops the exclude-list probe from making progress. WebAuthn (Web Authentication API) is the W3C standard for registration and sign-in. The batch size is the `maxCredentialCountInList` value that the authenticator reports in GetInfo. CTAP (Client to Authenticator Protocol) is the protocol between the SDK and the authenticator. FIDO is the FIDO Alliance family of standards that includes CTAP. GetInfo is the CTAP call that reports the authenticator's capabilities.

> "Nonpositive WebAuthn batch sizes break exclude-list progress"

## What is right

- GetInfo's `maxCredentialCountInList` is read as a plain integer, with no range check ([AuthenticatorInfo.cs lines 365-367](../../../src/Fido2/src/AuthenticatorInfo.cs#L365-L367)).
- The preflight uses that value as the chunk size without a check. It defaults only a missing value to 1 ([ExcludeListPreflight.cs line 95](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L94-L95)). The chunk size is `Math.Min` of that value and the remaining count ([line 109](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L109)). Zero or a negative value gives an empty chunk.
- When the probe returns `NoCredentials`, the offset moves by the chunk size ([line 142](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L142)). With a size of zero, the offset does not move, so the loop repeats without progress.
- The unit repro fails, as the verification run recorded. An empty probe is sent.

## What is wrong or imprecise

- Severity. The audit rates item #25 as MED as a whole. The verification report rates this sub-item LOW for conforming hardware, because CTAP forbids zero and negative values. It rates it MED for faulty or hostile GetInfo data.
- The audit does not describe the second effect, which is worse than a stall. If the authenticator holds a discoverable credential for the relying party (RP), an empty probe succeeds. The function then returns `null` from an empty chunk ([ExcludeListPreflight.cs line 137](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L137)). The registration goes ahead with an empty exclude list ([WebAuthnClient.Registration.cs lines 308-310](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L308-L310)). A duplicate credential can then be created. This follows from reading the code. It was not run.
- With no discoverable credential, the empty probe returns `NoCredentials`, and the loop repeats until the caller cancels. This also follows from reading the code. It was not run.
- The repro test name carries the prefix `YESDK1634`, but this sub-item is tracked under YESDK-1633. Use the name given in Check it yourself.
- Python is not a complete reference. It defaults a zero value to 1, but a negative value still reaches the loop. See the Python section.

## Why it matters

- Who is affected: a registration with a non-empty exclude list, against an authenticator that reports a zero or negative limit. A conforming authenticator cannot do this. CTAP forbids it.
- Impact: a hang until the caller cancels. With a discoverable credential on the RP, a possible duplicate credential (code reading).
- Precondition: the preflight runs only when there is a token and a non-empty exclude list ([WebAuthnClient.Registration.cs lines 241-244](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L241-L244)).

## Specification

Quote, verbatim, from the CTAP 2.3 Proposed Standard ([spec](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html)), §6.4:

- "Maximum number of credentials supported in credentialID list at a time by the authenticator. MUST be greater than zero if present."

Version table:

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.4: `maxCredentialCountInList` MUST be greater than zero if present | Yes | Yes | Yes | Same wording |

CTAP does not say what a client does with an invalid value. Rejecting it is valid. Clamping it is also valid.

## Canonical Python reference

Python shares the negative-value gap (candidate for the divergence ledger). Python handles zero.

- `max_creds = info.max_creds_in_list or 1` ([fido2/client/__init__.py line 584](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L584)). The probe that follows uses `{"up": False}` ([line 593](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L593)).

```python
max_creds = info.max_creds_in_list or 1
```

- A zero value becomes 1. A negative value is truthy in Python, so `or 1` does not catch it. Reading the loop, a negative slice does not shrink the list as intended. This was not run.
- Deliberate? The `or 1` text was introduced in commit f87a358 (subject: "Filter allow/exclude lists to only matched ids"). The commit has no body. No commit message I checked discusses zero or negative values.

## Sibling SDKs (context only)

yubikit-android uses a null check only. A zero value therefore gives an empty chunk ([Ctap2Client.java lines 880-881](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L880-L881)).

## Reproduction

- Unit: [WebAuthnAuditReproTests.YESDK1634_NonpositiveGetInfoListLimitStillProbesAllCredentials](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L338-L361), with the values 0 and -1. A fake probe throws `NoCredentials`. The test asserts that every probe has at least one credential, and that both credentials are probed. Result in the verification run: fails with an empty probe ("Collection was empty"). The test fails there on purpose, so that it does not hang.
- Hardware: not run. Not applicable.

## Proposed fix

- Recommendation (verification report):
  1. Reject a non-positive `MaxCredentialCountInList` when GetInfo is decoded ([AuthenticatorInfo.cs lines 365-367](../../../src/Fido2/src/AuthenticatorInfo.cs#L365-L367)).
  2. Keep a progress guard in the preflight ([ExcludeListPreflight.cs lines 94-110](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L94-L110)), so that the loop always advances.
- Trade-off of step 1. `FidoSession` reads GetInfo when it initializes ([FidoSession.cs line 156 at the branch base](../../../src/Fido2/src/FidoSession.cs#L156)). A decode error therefore fails every FIDO use of that device, not only WebAuthn. For faulty firmware, that effect is wide.
- Alternative 1: clamp a non-positive value to 1, in the decoder or in the preflight. This avoids the wide failure and still stops the stall.
- Alternative 2: guard in the preflight only, and raise a typed WebAuthn error for a non-positive limit. This keeps the effect inside WebAuthn.
- API impact: none. Malformed device data fails earlier.
- Proving test: the `YESDK1634` theory, with values 0 and -1. After the fix, the test should expect a typed error, or a probe with at least one credential. If the decoder rejects the value, add a decode-level test.
- Depends on / interacts with: the preflight runs only with a token and a non-empty exclude list. See the Why it matters section.
- Open questions for the maintainer: reject at decode (the verification recommendation), or clamp to 1? Rejecting at decode affects every FIDO use of that device.

## Check it yourself

```bash
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1634_NonpositiveGetInfoListLimit"
```

The name carries the prefix `YESDK1634`. The sub-item is tracked under YESDK-1633.

Spec: CTAP 2.3 §6.4. Python: `fido2/client/__init__.py` lines 584 and 593, at the pinned commit.

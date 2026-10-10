# #10 makeCredential sends up false (YESDK-1618)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed |
| Our severity | MED. The request is invalid and fails on the device. It is not a user-presence bypass. |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test; hardware (no touch; result recorded in the verification run) |
| Since the audit | `MakeCredentialOptions.cs` and `FidoSessionRequestEncoding.cs` are unchanged at `df1ec06d`. `FidoSession.cs` changed nearby. The user-presence context gained an `Operation` field. The `up=false` branch and the defect are unchanged. |

## What the audit says

The audit says the low-level makeCredential call encodes `"up": false`. CTAP (Client to Authenticator Protocol) 2.3 forbids that value. The `up` option is the user-presence option. UP means user presence.

> "violates “MUST be true if present”"

## What is right

- The encoder writes `up` whenever `UserPresence` has a value ([FidoSessionRequestEncoding.cs lines 120-124](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L120-L124)). So `UserPresence = false` reaches the device.
- The device rejects that request with `CTAP2_ERR_INVALID_OPTION` (0x2C) before it asks for a touch. The hardware record shows this ([hardware-results.md](../evidence/hardware-results.md), Phase 2).
- The WebAuthn (Web Authentication API) layer never sends `up` on makeCredential. Its backend builds the options from exclude list, `rk` (resident key), and `uv` only ([WebAuthnBackend.cs lines 116-126](../../../src/WebAuthn/src/Client/WebAuthnBackend.cs#L116-L126)).
- The getAssertion `up=false` is correct, and it must stay. It is the exclude-list probe ([ExcludeListPreflight.cs line 120](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L120)). The control test for it passes ([Fido2RequestAuditReproTests.cs lines 62-88](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2RequestAuditReproTests.cs#L62-L88)).

## What is wrong or imprecise

- The XML doc is wrong. It says "Defaults to true. Setting to false is rarely allowed by authenticators" ([MakeCredentialOptions.cs lines 51-57](../../../src/Fido2/src/Credentials/MakeCredentialOptions.cs#L51-L57)). CTAP 2.3 says `false` fails "regardless of authenticator version". For makeCredential, `false` is never allowed.
- The SDK's request is invalid. It is not a user-presence bypass. The device returns 0x2C before any touch, so no presence check is skipped.
- The SDK skips the user-presence prompt when `UserPresence` is `false` ([FidoSession.cs lines 268-270 at the branch base](../../../src/Fido2/src/FidoSession.cs#L268-L270)). The "omit" option in the fix must change that branch too. See [Proposed fix](#proposed-fix).

## Why it matters

- Who is affected: a caller of the low-level `IFidoSession.MakeCredentialAsync` that sets `UserPresence = false`. The call fails with a CTAP error. The WebAuthn layer never triggers it.
- No security impact is shown. The device rejects the request before any touch.
- Hardware: the rejection was observed without a touch.

## Specification

Quotes, verbatim, from the CTAP 2.3 Proposed Standard ([spec](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html)):

- CTAP 2.3 §6.1, option keys: "Platforms MAY send the "up" option key to CTAP2.1 authenticators, and its value MUST be true if present. The value false will cause a CTAP2_ERR_INVALID_OPTION response regardless of authenticator version."
- CTAP 2.3 §6.1.2, authenticator algorithm: "If the "up" option is false, end the operation by returning CTAP2_ERR_INVALID_OPTION."
- CTAP 2.3 §5, Terminology ("pre-flight"): "a platform typically invokes authenticatorGetAssertion with the "up" option key set to false"

Version table:

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.1: makeCredential `up` must be true if present | Yes | Yes | Yes | Same wording |
| §6.1.2: `up` false ends the operation with 0x2C | Yes | Yes | Yes | Authenticator side |
| §5: getAssertion with `up` false for the pre-flight probe | Yes | Yes | Yes | Keep this use |

## Canonical Python reference

Python is correct at the high-level client. It never sends `up` on makeCredential. Its raw CTAP API forwards whatever options the caller passes.

- High-level: the options dictionary holds `rk` (resident key) and `uv` (user verification) only ([fido2/client/__init__.py lines 828-844](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L828-L844)). Abridged:

```python
opts = {}
if rk:
    opts["rk"] = True
if internal_uv:
    opts["uv"] = True
```

- Raw: `Ctap2.make_credential` takes an `options` mapping and passes it on unchanged ([fido2/ctap2/base.py line 384](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/base.py#L384) and [line 420](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/base.py#L420)). So the raw API has the same exposure. This is a candidate for the divergence ledger, for the raw API only.
- Python's probe sends `{"up": False}` on getAssertion only ([fido2/client/__init__.py line 593](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L593)).

## Sibling SDKs (context only)

yubikit-android sends `up: false` only on the getAssertion probe ([Ctap2Client.java line 904](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L904)).

## Reproduction

- Unit: [Fido2RequestAuditReproTests.YESDK1618_MakeCredentialDoesNotEncodeFalseUserPresence](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2RequestAuditReproTests.cs#L24-L60). Encodes makeCredential with `UserPresence = false` and asserts that no `up` key is present. Result in the verification run: fails.
- Unit: [Fido2RequestAuditReproTests.YESDK1618_GetAssertionStillEncodesFalseUserPresence](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2RequestAuditReproTests.cs#L62-L88). The control. getAssertion must keep `up: false`. Result: passes.
- Hardware: [Fido2RequestAuditReproTests.YESDK1618_MakeCredentialFalseUserPresenceReturnsInvalidOption](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.IntegrationTests/AuditV2/Fido2RequestAuditReproTests.cs#L26-L42). Expects a `CtapException` with `InvalidOption`. Result: passed, without a touch. The results file does not name the key used for this run.

## Proposed fix

- Recommendation (reject): refuse `UserPresence = false` on makeCredential at the public boundary, before anything is sent. Put the check at the start of `FidoSession.MakeCredentialAsync`, next to the existing argument checks ([FidoSession.cs lines 229-257 at the branch base](../../../src/Fido2/src/FidoSession.cs#L229-L257)). Throw an `ArgumentException` that names the parameter. Correct the XML doc ([MakeCredentialOptions.cs lines 51-57](../../../src/Fido2/src/Credentials/MakeCredentialOptions.cs#L51-L57)). Keep the getAssertion encoding unchanged.
- Options considered (small decision):
  1. Reject (recommended). The caller learns at once. Cons: callers who pass `false` now fail earlier and with a different exception type.
  2. Omit `up` when it is `false`. Pros: no exception. Cons: the SDK still skips the user-presence prompt for `false` ([FidoSession.cs lines 268-270](../../../src/Fido2/src/FidoSession.cs#L268-L270)). The authenticator would then ask for a touch with no notice. The same branch must change too.
- API impact: behavioural for callers who set `UserPresence = false` on makeCredential. No signature change.
- Proving test: change `YESDK1618_MakeCredentialDoesNotEncodeFalseUserPresence` to expect the rejection before anything is sent. Update the hardware test `YESDK1618_MakeCredentialFalseUserPresenceReturnsInvalidOption` to expect the SDK's rejection, not the device's 0x2C. Keep `YESDK1618_GetAssertionStillEncodesFalseUserPresence` as the control.
- Depends on / interacts with: the user-presence context in `FidoSession.cs`, which changed at `df1ec06d`. The current line numbers are [FidoSession.cs lines 268-277](https://github.com/Yubico/Yubico.NET.SDK/blob/df1ec06d1a03c18c0b697ad424a40e4b0f9b46d1/src/Fido2/src/FidoSession.cs#L268-L277).
- Open questions for the maintainer:
  1. Reject (recommended) or omit?
  2. Should an explicit `UserPresence = true` be omitted for CTAP 2.0 authenticators? CTAP 2.3 says platforms may send `up` to CTAP 2.1 authenticators. The verification report flags this question. It is not decided.

## Check it yourself

```bash
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1618"
dotnet toolchain.cs -- test --integration --project Fido2 --filter "FullyQualifiedName~YESDK1618"
```

The second command needs an allow-listed key. It does not need a touch.

Spec: CTAP 2.3 §6.1, §6.1.2, and §5 (Terminology), quoted above. Python: `fido2/client/__init__.py` lines 828-844 and `fido2/ctap2/base.py` lines 384 and 420, at the pinned commit.

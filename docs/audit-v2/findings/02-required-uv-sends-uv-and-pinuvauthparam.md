# #2 Required UV sends uv and pinUvAuthParam (YESDK-1610)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction |
| Our severity | HIGH. Registration with `UserVerification=Required` and a PIN fails on the 5.7.4 test key. |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test; hardware (SDK test on the 5.7.4 test key; python-fido2 check on both test keys); code inspection |
| Since the audit | Unchanged at current yubikit. `UvDecision.cs`, `WebAuthnClient.Registration.cs`, `WebAuthnClient.Authentication.cs`, `WebAuthnBackend.cs`, and `FidoSessionRequestEncoding.cs` are identical at the branch base and at `df1ec06d`. |

## What the audit says

The audit says that a required-UV request sends two fields that the Client to Authenticator Protocol (CTAP) forbids together. UV means user verification. The SDK (software development kit) sets `UvOption = true` and also obtains a PIN/UV auth token, so both `uv` and `pinUvAuthParam` are serialized. A PIN is a personal identification number. WebAuthn (Web Authentication API) is the W3C standard that the SDK's WebAuthn layer implements.

> "Therefore, both fields are serialized."

## What is right

- The rule is real. CTAP 2.1, 2.2, and 2.3 all forbid a `uv` option key and a `pinUvAuthParam` in the same request. See [Specification](#specification).
- The PIN branch sets both values for `Required`. [UvDecision.cs lines 94-102](../../../src/WebAuthn/src/Client/UserVerification/UvDecision.cs#L94-L102) sets `UseToken = true` and `UvOption = true`.
- The built-in UV branch sets both as well ([UvDecision.cs lines 104-112](../../../src/WebAuthn/src/Client/UserVerification/UvDecision.cs#L104-L112)). The unit test covers this branch. No hardware run covers it.
- The request builders copy both values. Registration: [WebAuthnClient.Registration.cs lines 298-301](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L298-L301) for `uv`, and [lines 319-323](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L319-L323) for `pinUvAuthParam`. Assertion: [WebAuthnClient.Authentication.cs lines 216-219](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs#L216-L219) and [lines 241-245](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs#L241-L245).
- Hardware: the SDK registration with `UserVerification=Required` and the test PIN failed with `CTAP2_ERR_INVALID_OPTION` (0x2C) within a second, before any touch. The 0x2C status is the CTAP error code for an invalid option. A python-fido2 request with `{"uv": true}` and a `pinUvAuthParam` also got 0x2C on both test keys. See [hardware-results.md](../evidence/hardware-results.md), Phase 4.
- The exclude-list probe already sends the correct shape: a token with `up=false` and no `uv` ([ExcludeListPreflight.cs lines 114-123](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L114-L123)). UP means user presence.

## What is wrong or imprecise

- The audit covers the absent-`uv` coercion, but as a separate point. It does not say that every token-based registration is affected. The registration backend turns an absent `uv` into `false` ([WebAuthnBackend.cs lines 124-125](../../../src/WebAuthn/src/Client/WebAuthnBackend.cs#L124-L125)), and the encoder writes `"uv": false` ([FidoSessionRequestEncoding.cs lines 126-130](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L126-L130)). So a `Preferred` registration with a PIN also sends `uv` together with `pinUvAuthParam`.
- That shape breaks a second rule. CTAP 2.3 §6.1 says the `uv` key must not be present at all on an authenticator without built-in UV. See [Specification](#specification).
- The 5.7.4 test key accepted that shape in one run. The pseudo-random function (PRF) hardware run (#1) registered with `UserVerification=Preferred` and the test PIN, and the registration returned a response. That run used a token, so by the code path above the request carried `"uv": false`. The wire bytes were not captured. Treat this as code reading plus an observed success.
- The hardware failure was run through the SDK on the 5.7.4 test key. The 5.8.0 test key was checked with python-fido2 only. The hardware record generalizes this to "every PIN-only YubiKey". That extends the same CTAP rule to other keys. It is not tested.
- The built-in UV branch is wrong by the same rule. No test key exercised it.
- The assertion path is wrong for `Required` with a PIN, by the same rule. It was checked by unit test only.

## Why it matters

- Who is affected: an app that calls WebAuthn registration with `UserVerification=Required` and a PIN, passed in `pinBytes` or through `ICredentialPrompt`. On a key with a PIN, the call fails and the user cannot register. The SDK maps the error to `NotSupported` ([WebAuthnClient.Validation.cs lines 90-91](../../../src/WebAuthn/src/Client/WebAuthnClient.Validation.cs#L90-L91)).
- Preconditions: a PIN is set on the key. A key without a PIN does not reach this branch. Without a PIN and without built-in UV, `Required` throws before the makeCredential request is sent ([UvDecision.cs lines 115-121](../../../src/WebAuthn/src/Client/UserVerification/UvDecision.cs#L115-L121)).
- The default `UserVerification` is `Preferred` ([RegistrationOptions.cs line 59](../../../src/WebAuthn/src/Client/Registration/RegistrationOptions.cs#L59)). The default path is affected only through the absent-`uv` problem above.

## Specification

Quotes, verbatim from the CTAP 2.3 Proposed Standard ([spec](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html)):

- CTAP 2.3 §6.1 (authenticatorMakeCredential): "Platforms MUST NOT include both the "uv" option key and the pinUvAuthParam parameter in the same request."
- CTAP 2.3 §6.2 (authenticatorGetAssertion): "Platforms MUST NOT include both "uv" and pinUvAuthParam parameters in same request."
- CTAP 2.3 §6.1: "Platforms MUST NOT include the "uv" option key if the authenticator does not support built-in user verification."
- CTAP 2.3 §6.1: "Instead, platforms SHOULD create a pinUvAuthParam by obtaining pinUvAuthToken via getPinUvAuthTokenUsingUvWithPermissions or getPinUvAuthTokenUsingPinWithPermissions, as appropriate."

Version table:

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.1: `uv` and pinUvAuthParam in the same request | Yes | Yes | Yes | Same wording in all three |
| §6.2: `uv` and pinUvAuthParam in the same request | Yes | Yes | Yes | Same wording |
| §6.1: no `uv` key without built-in UV | Yes | Yes | Yes | Same wording |
| §6.1: use a token for the PIN/UV auth parameter | Yes | Yes | Yes | |

The test keys advertise FIDO_2_1 (5.7.4) and FIDO_2_2 (5.8.0). See [references.md](../references.md).

## Canonical Python reference

Python is correct here.

- `_get_token` returns a PIN or UV token. It returns `None` when internal UV is used ([fido2/client/__init__.py lines 682-690](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L682-L690)).
- The `uv` option is written only for internal UV. The `pinUvAuthParam` is computed only when there is a token ([lines 842-854](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L842-L854)). Abridged:

```python
if internal_uv:
    opts["uv"] = True
...
if pin_protocol and pin_token:
    pin_auth = (pin_protocol.authenticate(pin_token, client_data_hash), pin_protocol.VERSION)
else:
    pin_auth = (None, None)
```

- The getAssertion path follows the same rule ([lines 1000-1010](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L1000-L1010)).

## Sibling SDKs (context only)

yubikit-android writes `uv` only on the internal-UV branch ([Ctap2Client.java lines 510-511](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L510-L511) and [lines 654-655](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L654-L655)).

## Reproduction

- Unit: [WebAuthnAuditReproTests.YESDK1610_RegistrationTokenDoesNotAlsoRequestUv](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L48-L73). Asserts that the backend request has no `uv` option when a token is attached. Two cases: PIN and built-in UV. Result in the verification run: both fail.
- Unit: [WebAuthnAuditReproTests.YESDK1610_AssertionTokenDoesNotAlsoRequestUv](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L79-L104). The same check for getAssertion. Result: both fail.
- Unit: [WebAuthnAuditReproTests.YESDK1610_BackendPreservesAbsentUvOption](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L110-L147). The registration case fails, because an absent `uv` becomes `false`. The assertion case passes. It is the control.
- Hardware: [WebAuthnAuditHardwareReproTests.YESDK1610_RequiredUvRegistrationWithPinSucceeds](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/AuditV2/WebAuthnAuditHardwareReproTests.cs#L66-L83). Run on the 5.7.4 test key. The test has the RequiresUserPresence category, but the failure happens before any touch. Result: fails with `Invalid option` (0x2C). The python-fido2 check on both test keys also returned 0x2C ([hardware-results.md](../evidence/hardware-results.md), Phase 4).

## Proposed fix

- Recommendation:
  1. In `UvDecisionLogic.Decide` ([UvDecision.cs lines 94-112](../../../src/WebAuthn/src/Client/UserVerification/UvDecision.cs#L94-L112)), leave `UvOption` as `null` in both branches that set `UseToken = true`.
  2. In `WebAuthnBackend.MakeCredentialAsync` ([WebAuthnBackend.cs lines 124-125](../../../src/WebAuthn/src/Client/WebAuthnBackend.cs#L124-L125)), read `uv` and `rk` (the resident key option) with `TryGetValue`, and leave them unset when absent. `GetAssertionAsync` already does this ([lines 181-184](../../../src/WebAuthn/src/Client/WebAuthnBackend.cs#L181-L184)).
  3. Update the tests that assert the defect. `WebAuthnClientMakeCredentialTests.cs` line 299 asserts that `uv` is `true` on the retry request after a PUAT_REQUIRED status. PUAT means PIN/UV auth token. `WebAuthnClientGetAssertionTests.cs` line 264 has the same assertion.
- Options considered: not needed. Group A.
- API impact: none. The change is in the internal transport.
- Proving test: the `YESDK1610` unit repro tests, plus the hardware test `YESDK1610_RequiredUvRegistrationWithPinSucceeds` on an allow-listed key.
- Depends on / interacts with: [#7](07-preferred-resident-key-not-requested.md) uses the same backend coercion for `rk`. Fix both in one change. The PRF assertion work for #1 depends on correct UV handling. The exclude-list probe does not change.
- Open questions for the maintainer: none.

## Check it yourself

```bash
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1610"
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "FullyQualifiedName~YESDK1610"
```

The second command needs an allow-listed key with a known PIN.

Spec: CTAP 2.3 §6.1 and §6.2, quoted above. Python: `fido2/client/__init__.py` lines 682-690 and 842-854 at the pinned commit.

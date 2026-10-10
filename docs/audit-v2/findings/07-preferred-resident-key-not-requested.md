# #7 Preferred resident key is not requested (YESDK-1615)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed, with a correction to the suggested fix. The audit's chain of causes is right. Its suggested logic does not cover `Required` on an authenticator without `rk`. |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | A (no-brainer) |
| Evidence | unit test; hardware (5.7.4 test key, with user verification (UV) set to `Preferred` to keep #2 out of the run); code inspection |
| Since the audit | WebAuthn (Web Authentication API) request code and the encoder for FIDO2 requests (FIDO2 is the FIDO Alliance standard built on CTAP) are unchanged at `df1ec06d`. `CredPropsAdapter.cs` is unchanged too. It derives `rk` from the preference, which the fix must change (see Depends on / interacts with). |

## What the audit says

The audit says that a `residentKey=preferred` request reaches a capable authenticator as `rk=false`. The cause is that the request builder ignores the `rk` value in the authenticator's GetInfo response. GetInfo is the CTAP call (Client to Authenticator Protocol) that reports the authenticator's capabilities. The `rk` option is the resident key option, which makes a credential discoverable.

> "request builder ignores info.Options"

## What is right

- `Preferred` on a capable authenticator should create a discoverable credential. WebAuthn (Web Authentication API) Level 2 (L2) and Level 3 (L3) both define this preference. The SDK creates a non-discoverable one. The hardware run confirms it (see Reproduction).
- The builder asks for `rk` only when the preference is `Required` ([WebAuthnClient.Registration.cs lines 293-296](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L293-L296)). It does not take the capability from GetInfo. `BuildMakeCredentialRequest` has no parameter for it ([lines 282-288](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L282-L288)). GetInfo is read at [lines 84-85](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L84-L85).
- The backend turns an absent `rk` into `false` ([WebAuthnBackend.cs line 124](../../../src/WebAuthn/src/Client/WebAuthnBackend.cs#L124)). The encoder then writes `"rk": false` ([FidoSessionRequestEncoding.cs lines 114-118](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L114-L118)).
- The unit repros fail, as the verification run recorded ([WebAuthnAuditReproTests.cs lines 204-249](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L204-L249)).

## What is wrong or imprecise

- The audit's chain of causes is right: the builder ignores GetInfo, the backend turns the absent entry into `false`, and the encoder writes `"rk": false`. Its line reference (`WebAuthnClient.cs:1012`) is from an older layout. The current places are the builder in `WebAuthnClient.Registration.cs` and the coercion at `WebAuthnBackend.cs` line 124.
- The audit's suggested logic sends `rk=true` for `Required` whatever the authenticator supports. The spec skips an authenticator that cannot store discoverable credentials ([WebAuthn L3 §5.1.3](#specification)). So the fix must fail the ceremony in that case. It must not fall back to `rk=false`.
- The default `ResidentKey` is `Discouraged` ([RegistrationOptions.cs line 54](../../../src/WebAuthn/src/Client/Registration/RegistrationOptions.cs#L54)). The default path is not affected. `Required` already sends `rk=true`, so it is not affected either. Only callers who set `Preferred` are affected.

## Why it matters

- Who is affected: an app that sets `ResidentKey=Preferred`, for example to offer sign-in without a username. On a capable key the credential is stored without `rk`. A later sign-in with no allow list does not find it.
- Hardware: on the 5.7.4 test key, a registration with `Preferred` returned a credential. An assertion with no allow list then returned no match. The credential was not discoverable ([hardware-results.md](../evidence/hardware-results.md), Phase 4).
- Preconditions: a key that advertises `rk`. The 5.7.4 test key does. A `Preferred` request.

## Specification

Quotes, verbatim. Each one is followed by its source.

- WebAuthn L3 §5.1.3 ([spec](https://www.w3.org/TR/webauthn-3/)), the `preferred` branch. Two clauses, quoted verbatim: "If the authenticator is capable of client-side credential storage modality" is followed by "Let requireResidentKey be true." For an authenticator that is not capable, "is not capable of client-side credential storage modality, or if the client cannot determine authenticator capability," is followed by "Let requireResidentKey be false."
- WebAuthn L3 §5.1.3, candidate authenticators: "if pkOptions.authenticatorSelection.requireResidentKey is set to true and the authenticator is not capable of storing a client-side discoverable public key credential source, continue." The word "continue" means the client skips that authenticator. It does not fall back to `rk=false`.
- WebAuthn L3 §10.1.3 (credential properties extension, credProps): "Set rk to the value of the requireResidentKey parameter that was used in the invocation of the authenticatorMakeCredential operation."
- CTAP 2.3 §6.1, option table: "Specifies whether this credential is to be discoverable or not." The default for `rk` is `false`.
- CTAP 2.3 §6.1.2: "Let the "rk" option be treated as being present with the value false. (This is the default.)"
- CTAP 2.3 §12.3 (largeBlobKey): "If the options field of the authenticatorMakeCredential request does not map rk to true, return CTAP2_ERR_INVALID_OPTION."

CTAP version table:

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.1: `rk` option, default `false` | Yes | Yes | Yes | Same wording |
| §6.1.2: absent `rk` treated as `false` | Yes | Yes | Yes | Same wording |
| §12.3: largeBlobKey needs `rk` true | Yes | Yes | Yes | Interacts with #5 (see below) |

WebAuthn version table:

| Clause | WebAuthn L2 | WebAuthn L3 | Notes |
| --- | --- | --- | --- |
| §5.1.3: `preferred` with a capable authenticator gives true | Yes | Yes | Same wording |
| §5.1.3: `required` skips a non-capable authenticator | Yes | Yes | Same wording |
| credProps (the credential properties extension) `rk` reports the effective value (L2 §10.4; L3 §10.1.3) | Yes | Yes | Interacts with the fix (see below) |

## Canonical Python reference

Python is correct here.

- `rk` is requested for `Preferred` only when the authenticator advertises `rk`. Otherwise the option is omitted. `Required` raises an error when the authenticator cannot store a discoverable credential ([fido2/client/__init__.py lines 828-844](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L828-L844)).

```python
can_rk = info.options.get("rk")
rk = selection.resident_key == ResidentKeyRequirement.REQUIRED or (
    selection.resident_key == ResidentKeyRequirement.PREFERRED and can_rk
)
```

- Deliberate: yes. Commit 24e5e77 ("Fix handling of residentKey "preferred"") introduced the `PREFERRED and can_rk` logic.

## Sibling SDKs (context only)

yubikit-android writes `rk` only when it was requested. It throws when the authenticator does not support `rk` ([Ctap2Client.java lines 503-508](https://github.com/Yubico/yubikit-android/blob/f46268563437ac52910001222a77741d229b9b99/fido/src/main/java/com/yubico/yubikit/fido/client/Ctap2Client.java#L503-L508)).

## Reproduction

- Unit: [WebAuthnAuditReproTests.YESDK1615_PreferredResidentKeyOnCapableAuthenticatorRequestsDiscoverable](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L204-L225). A capable authenticator must receive `rk: true` for `Preferred`. Result in the verification run: fails.
- Unit: [WebAuthnAuditReproTests.YESDK1615_BackendPreservesAbsentResidentKeyOption](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L227-L249). `ResidentKey` must stay null when the option is absent. Result: fails, because the value is `false`.
- Hardware: [WebAuthnAuditHardwareReproTests.YESDK1615_PreferredResidentKeyCanBeDiscoveredWithoutAllowList](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.IntegrationTests/AuditV2/WebAuthnAuditHardwareReproTests.cs#L20-L60). Run on the 5.7.4 test key. The test sets UV to `Preferred` so that #2 does not affect the run. It registers with `Preferred`, then runs an assertion with no allow list. Result: fails. The assertion returned an empty list. The test skips itself on keys without `rk`.

## Proposed fix

- Recommendation:
  1. Compute the effective `requireResidentKey` in `MakeCredentialCoreAsync`, after GetInfo is read ([WebAuthnClient.Registration.cs lines 84-85](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L84-L85)). `Required` gives true. `Preferred` gives true only when GetInfo advertises `rk` as true. `Discouraged` gives false.
  2. Pass that value into `BuildMakeCredentialRequest` ([lines 282-296](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L282-L296)). Send `rk` only when it is true.
  3. For `Required` on an authenticator that does not advertise `rk`, fail the ceremony with `NotSupported` before any device call. Do not fall back to `rk=false`. This follows the L3 skip rule.
  4. Keep the backend absence ([WebAuthnBackend.cs line 124](../../../src/WebAuthn/src/Client/WebAuthnBackend.cs#L124)), as in #2.
  5. Make the credProps `rk` value follow the effective value (see below).
- Options considered: not needed. Group A. For an unknown capability, omit `rk`. CTAP's default is `false`, so omitting it has the same meaning and adds no key.
- API impact: none. New discoverable credentials use storage on capable authenticators.
- Proving test: `YESDK1615_PreferredResidentKeyOnCapableAuthenticatorRequestsDiscoverable` and `YESDK1615_BackendPreservesAbsentResidentKeyOption` (unit). `YESDK1615_PreferredResidentKeyCanBeDiscoveredWithoutAllowList` (hardware).
- Depends on / interacts with:
  - [#2](02-required-uv-sends-uv-and-pinuvauthparam.md). Both fixes change the same backend coercion. Make them in one change.
  - [#5](../verification-report.md). The SDK always sends `largeBlobKey: true` when the large-blob storage extension (largeBlob) is requested ([LargeBlobAdapter.cs lines 28-40](../../../src/WebAuthn/src/Extensions/Adapters/LargeBlobAdapter.cs#L28-L40)). It does not check `rk`. CTAP 2.3 §12.3 requires `rk` to be true for that extension. So, by that rule, a `Preferred` or `Discouraged` registration with largeBlob should fail on the device today. Not run on hardware. After this fix, `Preferred` on a capable key works. `Discouraged` still needs the #5 change. The verification report's review correction says to send `largeBlobKey` only when `rk` is true and GetInfo advertises the extension and the option.
  - credProps. [CredPropsAdapter.cs lines 34-44](../../../src/WebAuthn/src/Extensions/Adapters/CredPropsAdapter.cs#L34-L44) derives `rk` from the preference and returns null for `Preferred`. WebAuthn L3 §10.1.3 says the value is the `requireResidentKey` that was used. After the fix, `Preferred` on a capable key should report `true`.
  - Storage: discoverable credentials use authenticator storage.
- Open questions for the maintainer: confirm that unknown capability omits `rk`. This is the recommendation.

## Check it yourself

```bash
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1615"
dotnet toolchain.cs -- test --integration --project WebAuthn --filter "FullyQualifiedName~YESDK1615"
```

The second command needs a key that advertises `rk`, a known test PIN (personal identification number), and a touch.

Spec: WebAuthn L3 §5.1.3 (the `requireResidentKey` steps) and §10.1.3. CTAP 2.3 §6.1, §6.1.2, and §12.3. Python: `fido2/client/__init__.py` lines 828-844 at the pinned commit.

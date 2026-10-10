# 26a Credential-specific PRF inputs are ignored (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (one audit row, #26; sub-item a) |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | C (with [#1](01-prf-not-translated-to-hmac-secret.md)). Choosing among several credentials is a design decision. |
| Evidence | unit test (no hardware) |
| Since the audit | Unchanged at current yubikit (`PrfInput.cs`, `PrfAdapter.cs`, `ExtensionBuilder.cs`, `ExtensionPipeline.cs`, `WebAuthnClient.Authentication.cs`). |

## What the audit says

The audit says per-credential pseudo-random function (PRF) inputs are dropped. The adapter forwards the input object, but the builder serializes only the global values. Its key claim:

> a credential-specific override is ignored when global input exists; supplying only the credential map causes encoding to fail.

PRF is a WebAuthn (Web Authentication) extension that derives secrets from an authenticator. It has two inputs. `eval` applies to every credential. `evalByCredential` maps a base64url-encoded credential ID to its own input. The relying party (RP) chooses which one applies.

## What is right

- The global-plus-override case is dropped silently. `EncodePrfInput` reads only `First` and `Second` ([ExtensionBuilder.cs:385-409](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L385-L409)). Repro [Fido2ExtensionAuditReproTests.cs:45-67](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L45-L67) fails. The request with the override is identical to the request without it.
- The map-only case fails when encoded. `EncodePrfInput` opens a one-entry map (`var evalCount = 1;`, [ExtensionBuilder.cs:390](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L390)) and writes no entry. `WriteEndMap` then throws "Not at end of the definite-length data item" ([ExtensionBuilder.cs:407](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L407)). Two repros fail with that exception: [Fido2ExtensionAuditReproTests.cs:69-86](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L69-L86) and [WebAuthnExtensionAuditReproTests.cs:37-56](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L37-L56).
- The WebAuthn adapter rejects several entries when allowCredentials is not empty ([PrfAdapter.cs:49-54](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L49-L54)). That check is right as far as it goes. It does not cover a one-entry map when several credentials are allowed (see below).
- Selection never happens. The adapter forwards the input object without choosing an entry ([PrfAdapter.cs:44-58](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L44-L58)).

## What is wrong or imprecise

- The audit's examples are right, but the spec has more rules, and the adapter checks none of them:
  - A single entry is forwarded without selection, whatever the allow-list size. Its key is not checked against allowCredentials, and the spec makes a key outside allowCredentials a SyntaxError ([PrfAdapter.cs:44-58](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L44-L58)). With several allowed credentials, a one-entry map does not say which credential will be returned. The comments at [PrfAdapter.cs:48](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L48) and [56](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L56) say that the adapter selects the match, or that the caller already did. Neither holds at this layer: the RP cannot know the returned credential before the request is sent.
  - With allowCredentials empty, evalByCredential is silently dropped. The adapter skips the map. It uses the global input if there is one, and otherwise sends a plain PRF request ([PrfAdapter.cs:59-68](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L59-L68)). The spec makes this a NotSupportedError.
  - At registration, `eval` and `evalByCredential` are both ignored ([PrfAdapter.cs:30-34](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L30-L34)). The spec makes evalByCredential at registration a NotSupportedError.
- The verification report's decision 5 recommends rejecting multi-credential requests now and probing later. We keep that rule for several allowed credentials. For exactly one allowed credential, we also propose selecting its override. Only one credential can be returned in that case, so the choice is deterministic. The report does not separate that case; this is our proposal, based on the spec text.
- The request is built before any probe. `WebAuthnClient.Authentication.cs` builds the request, including the extensions ([line 112](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs#L112) and [lines 234-236](../../../src/WebAuthn/src/Client/WebAuthnClient.Authentication.cs#L234-L236)). The first GetAssertion is then sent with those inputs ([CredentialMatcher.cs:49](../../../src/WebAuthn/src/Client/Authentication/CredentialMatcher.cs#L49)). With several allowed credentials, the salts are fixed before the authenticator picks one.

## Why it matters

- Who is affected: RPs that send per-credential PRF inputs through the WebAuthn client.
- With a global input also present: the RP gets output derived from the wrong input, and the call reports no error.
- With only the map: the request fails when it is built. That is loud, but the feature is still broken.
- Preconditions: evalByCredential with at least one entry. The silent case also needs a global `eval`.

## Specification

WebAuthn Level 3 (L3), §10.1.4 ([prf-extension](https://www.w3.org/TR/webauthn-3/#prf-extension)). The version table covers WebAuthn Level 3 only. WebAuthn Level 2 (L2) has no PRF extension.

> If evalByCredential is present and contains an entry whose key is the base64url encoding of the credential ID that will be returned, let ev be the value of that entry. — WebAuthn L3 §10.1.4 (authentication)

> If ev is null and eval is present, then let ev be the value of eval. — WebAuthn L3 §10.1.4 (authentication)

> If evalByCredential is not empty but allowCredentials is empty, return a DOMException whose name is “NotSupportedError”. — WebAuthn L3 §10.1.4 (authentication)

> If any key in evalByCredential is the empty string, or is not a valid base64url encoding, or does not equal the id of some element of allowCredentials after performing base64url decoding, then return a DOMException whose name is “SyntaxError”. — WebAuthn L3 §10.1.4 (authentication)

> If evalByCredential is present, return a DOMException whose name is “NotSupportedError”. — WebAuthn L3 §10.1.4 (registration)

| Rule | Spec (WebAuthn L3 §10.1.4) | SDK today |
| --- | --- | --- |
| evalByCredential at registration | NotSupportedError | Silently ignored |
| evalByCredential with empty allowCredentials | NotSupportedError | Silently dropped (global input or plain PRF request) |
| Key not in allowCredentials | SyntaxError | Not checked |
| Entry for the returned credential | Used in place of `eval` | Forwarded unselected; fails when map-only |
| No entry for the returned credential | `eval` is used | Global input used, if any |
| One entry, several allowed credentials | The returned credential decides (no rejection stated) | Forwarded unselected ([PrfAdapter.cs:45-57](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L45-L57)) |
| Several entries, allowCredentials not empty | The returned credential decides | Rejected (NotSupported, [PrfAdapter.cs:49-54](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L49-L54)) |

## Canonical Python reference

Python correctly selects authentication inputs for the chosen credential. Its registration validation is incomplete: without hmac-secret-mc, evalByCredential is ignored instead of rejected. This registration gap is a candidate for the divergence ledger.

```python
ids = {websafe_encode(c.id) for c in allow_list}
if not ids.issuperset(by_creds):
    raise ValueError("evalByCredentials contains invalid key")
if selected:
    key = websafe_encode(selected.id)
    if key in by_creds:
        secrets = by_creds[key]
```

([extensions.py:205-211](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L205-L211))

Python selects the credential first. `Fido2Client` probes with getAssertion and `up` set to false ([client/__init__.py:593](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L593)). It then calls `prepare_inputs` with the selected credential ([client/__init__.py:980-994](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L980-L994)). At registration, Python reads PRF inputs only inside its hmac-secret-mc branch ([extensions.py:287-298](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L287-L298)). With hmac-secret-mc, a non-empty evalByCredential raises `ValueError` ([extensions.py:203-204](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L203-L204)).

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [Fido2ExtensionAuditReproTests.YESDK1634_CredentialSpecificPrfOverridesGlobalInput](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L45-L67). Asserts that a credential-specific override changes the request. Result today: fails ("Collections are equal").
- Unit: [Fido2ExtensionAuditReproTests.YESDK1634_OnlyCredentialSpecificPrfEncodesWithoutFailure](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L69-L86). Asserts that a map-only input encodes. Result today: fails ("Not at end of the definite-length data item").
- Unit (WebAuthn): [WebAuthnExtensionAuditReproTests.YESDK1634_PrFOnlyCredentialMapIsSelectedForMatchingAllowCredential](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnExtensionAuditReproTests.cs#L37-L56). The same failure, through the WebAuthn path.
- Hardware: not applicable. No hardware run covers this item.

## Proposed fix

- Recommendation:
  1. Validate in `PrfAdapter`, before any encoding ([PrfAdapter.cs:39-69](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L39-L69)):
     - Registration: reject a present evalByCredential with NotSupported ([PrfAdapter.cs:30-34](../../../src/WebAuthn/src/Extensions/Adapters/PrfAdapter.cs#L30-L34)).
     - Authentication: if evalByCredential is non-empty and allowCredentials is empty, reject with NotSupported.
     - Any key that is not an allowCredentials ID: reject with InvalidRequest, which is the spec's SyntaxError.
  2. Select:
     - Several allowed credentials and a non-empty evalByCredential: reject with NotSupported until probing exists. This includes a one-entry map, which does not say which credential will be returned.
     - Exactly one allowed credential: select its override if the map has one for it. Otherwise use `eval` if present.
  3. Encode the selected values through the corrected hmac-secret path ([#1](01-prf-not-translated-to-hmac-secret.md)).
- Options considered (group C):
  - Option A (recommended now): reject any non-empty evalByCredential when several credentials are allowed. With exactly one allowed credential, select its override, else use `eval`. Pros: small, no extra round trip, and spec-consistent for one credential. Cons: several allowed credentials with evalByCredential stay unsupported.
  - Option B (later): probe first with getAssertion and `up` set to false, as the exclude-list preflight does ([ExcludeListPreflight.cs:98-120](../../../src/WebAuthn/src/Internal/ExcludeListPreflight.cs#L98-L120)) and as python-fido2 does. Then build the salts for the returned credential. Pros: full spec behavior. Cons: an extra round trip. The probe needs a personal identification number (PIN) or user verification (UV) token when UV is required, as the exclude-list probe does. More code.
  - Option C: reject every evalByCredential request. Pros: simplest. Cons: drops a spec feature.
- API impact (application programming interface): none to the WebAuthn call shape. Requests that were accepted and ignored will now be rejected or handled.
- Proving test:
  - The two Fido2 repros test the low-level `WithPrf(PrfInput)` boundary. If [#1](01-prf-not-translated-to-hmac-secret.md) removes that method, move them to the WebAuthn layer.
  - Add WebAuthn tests for: registration with evalByCredential (NotSupported); empty allowCredentials (NotSupported); several allowed credentials with any non-empty evalByCredential, including a one-entry map (NotSupported); a key outside allowCredentials (rejected); one allowed credential with a matching entry (salts come from the entry, not from `eval`); one allowed credential with no matching entry (falls back to `eval`).
  - The WebAuthn repro `YESDK1634_PrFOnlyCredentialMapIsSelectedForMatchingAllowCredential` has one allowed credential, so it stays valid under this rule.
- Depends on / interacts with: [#1](01-prf-not-translated-to-hmac-secret.md), the same encoder. The two Fido2 repros use `WithPrf(PrfInput)`. [26d](26d-previewsign-first-entry.md) has the same selection problem in the low-level previewSign API. Option B depends on the build order in `WebAuthnClient.Authentication.cs`.
- Open questions for the maintainer:
  1. Reject multi-credential evalByCredential now (the report's recommendation), or implement probing now?
  2. Confirm the one-credential rule: with exactly one allowed credential, select its override. The verification report does not state this separately. It follows from the spec's "credential that will be returned" wording.

## Check it yourself

- Unit: `dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1634_PrFOnlyCredentialMap"` and `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1634_CredentialSpecific"`. Both fail at the branch base.
- Spec: WebAuthn L3 §10.1.4 ([prf-extension](https://www.w3.org/TR/webauthn-3/#prf-extension)).
- Python: `fido2/ctap2/extensions.py`, lines 197-231 and 345-349. `fido2/client/__init__.py`, lines 565-593 and 980-994.

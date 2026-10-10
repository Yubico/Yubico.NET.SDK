# 26d FIDO previewSign chooses the first credential's parameters (YESDK-1634)

| | |
| --- | --- |
| Audit severity | MED (one audit row, #26; sub-item d) |
| Our verdict | Confirmed |
| Our severity | MED |
| Root cause | SDK only |
| Fix group | B (small interface decision: reject ambiguous input now, add selection later) |
| Evidence | unit test (no hardware) |
| Since the audit | Unchanged at current yubikit (`PreviewSignAuthenticationInput.cs`, `ExtensionBuilder.cs`, `FidoSessionRequestEncoding.cs`, `PreviewSignAdapter.cs`). |

## What the audit says

The audit says the direct FIDO2 (the FIDO Alliance's standard set for passwordless authentication) previewSign builder takes the first entry of a credential-to-parameters map, whatever the allow list says. Its key claim:

> However, `ExtensionBuilder` selects `First().Value`, discards the credential IDs, and encodes only that entry’s key handle, message and signing arguments.

previewSign is a draft Yubico WebAuthn (Web Authentication) extension. For authentication, the caller maps each credential ID to a key handle, a message to sign, and optional arguments. The WebAuthn client picks one credential and sends that entry. This item concerns the direct FIDO2 application programming interface (API), which has no WebAuthn client in front of it.

## What is right

- The low-level builder ignores the credential IDs. `EncodePreviewSignAuthenticationInput` takes `SignByCredential.First()` ([ExtensionBuilder.cs:420-431](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L420-L431), selection at [423-424](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L423-L424)).
  - Repro [Fido2ExtensionAuditReproTests.cs:88-101](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L88-L101) fails: no exception is thrown for a two-entry map.
- The WebAuthn layer is safe. `PreviewSignAdapter.ApplyToBuilderForAuthentication` requires one entry for each allowed credential ([lines 104-116](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L104-L116)). It rejects any count other than one ([lines 118-127](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L118-L127)). It then checks the entry against `allowCredentials[0]` ([lines 130-138](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L130-L138)) and builds a one-entry map ([lines 148-154](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L148-L154)). So the WebAuthn client is not affected.
- The allow list is encoded separately ([FidoSessionRequestEncoding.cs:75-78](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L75-L78)). Nothing compares the previewSign key with it.

## What is wrong or imprecise

- The audit's example is right: putting B's entry first sends B's parameters. Two more gaps:
  - A single entry whose key is not in the allow list is accepted at the low level. Neither the builder nor `FidoSession.cs` has any previewSign or allow-list check. The builder does not see the allow list.
  - The map's insertion order decides the result. `First()` depends on enumeration order, and the `IReadOnlyDictionary` interface does not guarantee an order.
- The draft says the client chooses one valid credential ("an arbitrary choice of one of those entries") and sends that entry. The example mechanism is a getAssertion with `up` set to false. So the fix is selection by credential, not "first entry". The draft also requires the entry count to match the allow list.
- The legacy .NET software development kit (SDK) v1 does not select among entries. `AddPreviewSignExtension` takes one key handle and one message, and requires a non-empty allow list ([GetAssertionParameters.cs:424-440](https://github.com/Yubico/Yubico.NET.SDK/blob/941874e91a77616f5d7063f2952a0291c7e7c8f8/Yubico.YubiKey/src/Yubico/YubiKey/Fido2/GetAssertionParameters.cs#L424-L440)). It has no credential-to-parameters map, so it does not show how to choose.

## Why it matters

- Who is affected: direct FIDO2 API callers (`Yubico.YubiKit.Fido2`) who pass more than one previewSign entry. The WebAuthn client is not affected.
- Effect: the authenticator signs with the entry that was inserted first. The caller gets a valid signature for a different message or key handle than it intended, and no error. Reordering the map changes the outcome.
- Precondition: more than one entry in `SignByCredential`. The constructor accepts that ([PreviewSignAuthenticationInput.cs:37-50](../../../src/Fido2/src/Extensions/PreviewSign/PreviewSignAuthenticationInput.cs#L37-L50)).

## Specification

Yubico previewSign draft, version 4, §10.2.1 ([draft](https://yubicolabs.github.io/webauthn-sign-extension/4/)). This is a draft, not a ratified standard. The SDK's contract follows it. CTAP (Client to Authenticator Protocol) is the FIDO authenticator protocol named in the note below.

> Let chosenCredentialId be an arbitrary choice of one of those entries. — §10.2.1, authentication processing

> Note: For example, for [FIDO-CTAP] authenticators this might be determined by invoking the CTAP2 authenticatorGetAssertion command with the up option set to false. — §10.2.1

> Let signInputs be extSign.signByCredential[chosenCredentialIdB64]. — §10.2.1

> If the size of extSign.signByCredential does not equal the size of pkOptions.allowCredentials, return a DOMException whose name is “NotSupportedError”. — §10.2.1

> If signInputs is undefined, return a DOMException whose name is “SyntaxError”. — §10.2.1

| Rule | Yubico draft v4 §10.2.1 | SDK low level (`ExtensionBuilder`) | SDK WebAuthn (`PreviewSignAdapter`) |
| --- | --- | --- | --- |
| Entry count equals allowCredentials size | NotSupportedError if different | Not checked | Exactly one entry ([lines 118-127](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L118-L127)) |
| Each allowed credential has an entry | SyntaxError if missing | Not checked | Checked ([lines 104-116](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L104-L116)) |
| Entry used for the signature | The client's chosen credential | `First()`, the first entry | The one entry, matched to `allowCredentials[0]` ([lines 130-138](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L130-L138)) |

## Canonical Python reference

Python correctly uses the selected credential's parameters and checks for missing entries.

```python
cred_inputs = by_creds[websafe_encode(selected.id)]
```

([extensions.py:793](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L793))

- Python checks that every allowed credential has an entry, and raises `ValueError` if not ([extensions.py:790-792](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/ctap2/extensions.py#L790-L792)).
- Python selects the credential before preparing extension inputs ([client/__init__.py:980-994](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L980-L994)).
- It does not enforce equal map and allow-list sizes, so additional map entries are accepted. That validation gap is a candidate for the divergence ledger.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [Fido2ExtensionAuditReproTests.YESDK1634_MultiplePreviewSignCredentialsCannotSilentlySelectFirst](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2ExtensionAuditReproTests.cs#L88-L101). Expects `NotSupportedException` for a two-entry map. Result today: fails ("No exception was thrown").
- Hardware: not applicable. No hardware run covers this item.

## Proposed fix

- Recommendation:
  1. Interim, now: in `ExtensionBuilder.EncodePreviewSignAuthenticationInput` ([ExtensionBuilder.cs:420-431](../../../src/Fido2/src/Extensions/Shared/ExtensionBuilder.cs#L420-L431)), throw `NotSupportedException` when `SignByCredential.Count != 1`, instead of taking `First()`. This matches the existing repro.
  2. Later: check the single entry against the allow list, and implement selection. The builder cannot do the first without the allow list, so this needs one of the options below.
- Options considered:
  - Option A (interim, recommended): reject multi-entry maps at encode time. Pros: one change, no API change, and it matches the WebAuthn layer's rule. Cons: a single entry is still not checked against the allow list at the low level.
  - Option B (group B, for the allow-list check): carry the typed input to request construction, and resolve the entry against `GetAssertionOptions.AllowList` there. Today the caller builds the extensions before the encoder sees the allow list. The change would reach `FidoSessionRequestEncoding.BuildGetAssertionRequest` ([lines 66-98](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L66-L98)). Pros: complete for single entries. Cons: more change; the new field is additive.
  - Option C (later): probe with getAssertion and `up` set to false, as python-fido2 does, and select the entry. Pros: spec behavior. Cons: an extra round trip. Shares the mechanism with [26a](26a-prf-credential-specific-inputs-ignored.md).
- API impact (application programming interface): Option A changes behavior for callers who pass multi-entry maps, which are now rejected. Option B is additive, a new typed field on the options. Option C is internal.
- Proving test:
  - `YESDK1634_MultiplePreviewSignCredentialsCannotSilentlySelectFirst` passes with Option A.
  - With Option B: a single entry whose key is not in the allow list is rejected.
  - With Option C: with credential selection fixed to a particular valid credential, reordering the map must not change the parameters selected for that credential. Verify that the transmitted parameters belong to the selected credential.
- Depends on / interacts with: [26a](26a-prf-credential-specific-inputs-ignored.md) (the same selection problem in the pseudo-random function (PRF) path). The WebAuthn adapter already notes that probe selection is not implemented ([PreviewSignAdapter.cs:118](../../../src/WebAuthn/src/Extensions/Adapters/PreviewSignAdapter.cs#L118)).
- Open questions for the maintainer: ship the interim restriction now (Option A, which the verification report recommends), and select later? Or implement selection now?

## Check it yourself

- Unit: `dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~YESDK1634_MultiplePreviewSign"`. Fails at the branch base.
- Spec: Yubico previewSign draft v4, §10.2.1 ([draft](https://yubicolabs.github.io/webauthn-sign-extension/4/)).
- Python: `fido2/ctap2/extensions.py`, lines 782-810. `fido2/client/__init__.py`, lines 980-994.
- Legacy: the link in "What is wrong or imprecise".

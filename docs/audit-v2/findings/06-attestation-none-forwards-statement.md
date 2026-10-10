# #6 Attestation none forwards the authenticator statement (YESDK-1614)

| | |
| --- | --- |
| Audit severity | MED |
| Our verdict | Confirmed. The correction is to the fix scope, not to the audit's claim. Level 3 (L3) keeps the AAGUID (authenticator attestation GUID, a globally unique identifier) for `none`. Level 2 (L2) zeroes it. |
| Our severity | MED |
| Root cause | SDK and Python share it |
| Fix group | A (no-brainer). One open question on the AAGUID (see Proposed fix). |
| Evidence | unit test; code inspection (no hardware run) |
| Since the audit | Unchanged at current yubikit. `WebAuthnClient.Registration.cs` and `RegistrationOptions.cs` are identical at the branch base and at `df1ec06d`. |

## What the audit says

The audit says that a registration with attestation `none` still returns the authenticator's attestation statement. The SDK (software development kit) never reads the caller's attestation preference when it builds the response. The WebAuthn (Web Authentication API) rule says the client must replace a non-self statement with a `none` statement.

> "it forwards the original statement without evaluating options.Attestation."

## What is right

- The SDK does not read the attestation preference when it builds the response. `options.Attestation` is declared ([RegistrationOptions.cs lines 61-64](../../../src/WebAuthn/src/Client/Registration/RegistrationOptions.cs#L61-L64)). A search of `src/WebAuthn/src` finds no other read of it.
- `BuildRegistrationResponse` takes the authenticator's statement and writes it into the attestation object without change ([WebAuthnClient.Registration.cs lines 339-373](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L339-L373)). The statement is read at line 351 and used at line 357.
- The default is `None` ([RegistrationOptions.cs line 64](../../../src/WebAuthn/src/Client/Registration/RegistrationOptions.cs#L64)). A caller who never sets `Attestation` gets the authenticator's statement. The default path is affected, not only callers who opt in.
- The unit repro fails, as the verification run recorded. The returned format is `packed`, not `none`.

## What is wrong or imprecise

- The audit quotes the L3 rule. The SDK's own documents do not agree on the level. The WebAuthn README lists WebAuthn Level 2 as the specification ([src/WebAuthn/README.md line 179](../../../src/WebAuthn/README.md#L179)). The module CLAUDE.md says "Level 2/3" ([src/WebAuthn/CLAUDE.md line 16](../../../src/WebAuthn/CLAUDE.md#L16)). The two levels differ on the AAGUID for `none`. See [Specification](#specification).
- The audit does not mention the AAGUID, so its claim is not wrong. The verification report's correction is about the fix. L3 no longer zeroes the AAGUID for `none`, according to its change log. L2 replaces the AAGUID with 16 zero bytes. A fix that follows L2 would change the AAGUID too. The verification report follows L3.
- The fix must also handle the self-attestation exception that comes with the rule. A self attestation is not replaced. The audit quotes the rule but not this exception.

## Why it matters

- Who is affected: an app that registers with the default `Attestation` (`None`) and receives a non-self statement from the authenticator. The relying party (RP) then gets an attestation statement that the caller did not want. WebAuthn L3 §5.4.7 gives the purpose of `none`: "in order to potentially avoid having to obtain user consent to relay identifying information to the Relying Party".
- Preconditions: the authenticator returns a non-self statement for the request. The hardware record does not show which attestation format the test keys return. The impact on YubiKeys is unverified.
- The statement and the AAGUID can identify the authenticator model or batch. The privacy effect depends on what the RP does with them.

## Specification

Quotes, verbatim. Each one is followed by its source. `fmt` is the attestation format name. CBOR (Concise Binary Object Representation) is the encoding that CTAP (Client to Authenticator Protocol) and WebAuthn use.

- WebAuthn L3 §5.4.7 ([spec](https://www.w3.org/TR/webauthn-3/)): "If the authenticator generates an attestation statement that is not a self attestation, the client will replace it with a None attestation statement."
- WebAuthn L3 §5.1.3, the `none` branch: "If the aaguid in the attested credential data is 16 zero bytes, credentialCreationData.attestationObjectResult.fmt is "packed", and "x5c" is absent from credentialCreationData.attestationObjectResult, then self attestation is being used and no further action is needed."
- WebAuthn L3 §5.1.3, the `none` branch: "Set the value of credentialCreationData.attestationObjectResult.fmt to "none", and set the value of credentialCreationData.attestationObjectResult.attStmt to be an empty CBOR map."
- WebAuthn L2 §5.1.3, the `none` branch: "Replace the AAGUID in the attested credential data with 16 zero bytes."
- WebAuthn L3 change log: "aaguid in attested credential data is no longer zeroed when attestation preference is none"
- WebAuthn L3 §8.7: "The none attestation statement format is used to replace any authenticator-provided attestation statement when a WebAuthn Relying Party indicates it does not wish to receive attestation information"

Version table:

| Clause | WebAuthn L2 | WebAuthn L3 | Notes |
| --- | --- | --- | --- |
| Replace a non-self statement with `none` (§5.1.3; L3 also §5.4.7) | Yes | Yes | Same rule |
| `fmt` set to `none` with an empty `attStmt` (§5.1.3) | Yes | Yes | Same |
| AAGUID replaced with 16 zero bytes (§5.1.3) | Yes | No | The difference. L3 change log says the AAGUID is no longer zeroed. |
| Self-attestation exception (§5.1.3) | Yes | Yes | Zero AAGUID, `packed`, and no `x5c` |

CTAP is the Client to Authenticator Protocol. None of the three editions has a client rule that replaces a statement with `none`.

## Canonical Python reference

Python has the same issue (candidate for the divergence ledger).

- `fido2/client/__init__.py` builds the attestation object from the authenticator's format and statement. It does not check the preference ([lines 915-917](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L915-L917)). It reads the preference only for enterprise attestation ([lines 761-769](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L761-L769)).

```python
att_obj = AttestationObject.create(
    att_resp.fmt, att_resp.auth_data, att_resp.att_stmt
)
```

- Deliberate? No evidence found. Blame attributes the lines to commit f47f935 (subject: "Use RegistrationResponse and AuthenticationResponse"). That subject does not mention attestation preference.
- python-fido2's Windows client passes the preference to the platform API ([fido2/client/windows.py lines 196-199](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/windows.py#L196-L199)). Windows handling therefore happens outside Python.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [WebAuthnAuditReproTests.YESDK1614_NoneAttestationRemovesAuthenticatorStatement](../../../src/WebAuthn/tests/Yubico.YubiKit.WebAuthn.UnitTests/AuditV2/WebAuthnAuditReproTests.cs#L149-L202). The test returns a packed statement with a non-zero AAGUID, requests `None`, and reads back the attestation object. It asserts that `fmt` is `none` and that `attStmt` is empty. Result in the verification run: fails. The format is `packed`.
- Hardware: not run. The response change is local. The hardware record does not show which attestation format the test keys return ([hardware-results.md](../evidence/hardware-results.md)).

## Proposed fix

- Recommendation: in `BuildRegistrationResponse` ([WebAuthnClient.Registration.cs lines 339-381](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L339-L381)), when `options.Attestation` is `None` and the statement is not a self attestation, build the attestation object with `fmt` set to `none` and an empty `attStmt`. Keep the credential ID and the AAGUID as returned, which is the L3 behaviour. The current code builds the attestation object at line 357 from the authenticator's statement. Build `AttestationObject`, `RawAttestationObject`, and `AttestationStatement` from the same replaced value so that they agree.
- Self-attestation check: an all-zero AAGUID, a `packed` format, and no `x5c` (the attestation certificate chain field) keeps the authenticator's statement.
- Options considered: not a group B decision. The AAGUID choice is in the open questions.
- API impact: none. The response values change to match the requested preference.
- Proving test: `YESDK1614_NoneAttestationRemovesAuthenticatorStatement`. Add a self-attestation control, which should stay `packed`. Add a `Direct` control, which should keep the statement; `Direct` is out of scope for this fix.
- Depends on / interacts with: the attestation object model (`WebAuthnAttestationObject`). The other preferences (`Indirect`, `Direct`, `Enterprise`) need their own policy review, as the verification report says.
- Open questions for the maintainer:
  1. Which WebAuthn level does the SDK follow for `none`? L3 keeps the AAGUID, which is the verification report's recommendation. L2 replaces it with zeros. The README names L2 and the module CLAUDE.md says L2/3. Pick one and document it.
  2. Keep `Indirect`, `Direct`, and `Enterprise` as pass-through for now? Recommended: yes, in a separate change.

## Check it yourself

```bash
dotnet toolchain.cs -- test --project WebAuthn --filter "FullyQualifiedName~YESDK1614"
```

Spec: WebAuthn L3 §5.1.3 (the `constructCredentialAlg` steps for `none`) and §5.4.7. WebAuthn L2 §5.1.3 for the AAGUID step. Python: `fido2/client/__init__.py` lines 915-917.

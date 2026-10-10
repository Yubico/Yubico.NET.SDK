# N1 Duplicate pubKeyCredParams entries accepted (no Jira key)

| | |
| --- | --- |
| Audit severity | Not in the audit. New finding from verification. |
| Our verdict | Confirmed |
| Our severity | LOW |
| Root cause | SDK and Python share it |
| Fix group | A (no-brainer). One open question: reject or deduplicate. |
| Evidence | unit test; code inspection (no hardware run) |
| Since the audit | The encoder is unchanged at `df1ec06d`. `FidoSession.cs` changed elsewhere (the user-presence context). The empty-list check at lines 250-255 is at the same lines at `df1ec06d`. |

## What the audit says

The audit does not cover this item. It was found during verification. WebAuthn (Web Authentication API) is the W3C standard that the SDK's WebAuthn layer implements. CTAP (Client to Authenticator Protocol) is the protocol between the SDK and the authenticator. The CTAP rule that it breaks is quoted under [Specification](#specification).

## What is right

- The encoder writes the caller's list as it is given ([FidoSessionRequestEncoding.cs lines 30-34](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L30-L34), and the array writer at [lines 162-174](../../../src/Fido2/src/Cbor/FidoSessionRequestEncoding.cs#L162-L174)). For a list of distinct entries, that is correct.
- The boundary already checks the list for emptiness ([FidoSession.cs lines 250-255](../../../src/Fido2/src/FidoSession.cs#L250-L255)). That is the natural place for a duplicate check.
- The unit repro fails as expected. Encoding `[ES256, ES256]` produces two entries under key 4 (`pubKeyCredParams`). The result in the verification run is a count of 2.

## What is wrong or imprecise

- The WebAuthn layer passes the caller's list through ([WebAuthnClient.Registration.cs lines 312-314](../../../src/WebAuthn/src/Client/WebAuthnClient.Registration.cs#L312-L314)). It checks only that the list is not empty ([WebAuthnClient.Validation.cs lines 47-52](../../../src/WebAuthn/src/Client/WebAuthnClient.Validation.cs#L47-L52)). So neither layer rejects duplicates.
- The CTAP rule is a platform requirement. It does not say what an authenticator does with duplicates. The effect on hardware is not tested.

## Why it matters

- Who is affected: a caller who passes the same algorithm twice, at either layer.
- Impact: low. The request is invalid for CTAP. A strict authenticator may reject it, and a tolerant one may accept it. No security effect is shown.
- Hardware: not run. CBOR (Concise Binary Object Representation) encoding is enough to show the defect.

## Specification

Quote, verbatim, from the CTAP 2.3 Proposed Standard ([spec](https://fidoalliance.org/specs/fido-v2.3-ps-20260226/fido-client-to-authenticator-protocol-v2.3-ps-20260226.html)), §6.1, parameter `pubKeyCredParams`:

- "The array is ordered from most preferred to least preferred and MUST NOT include duplicate entries."

Version table:

| Clause | CTAP 2.1 | CTAP 2.2 | CTAP 2.3 | Notes |
| --- | --- | --- | --- | --- |
| §6.1: `pubKeyCredParams` MUST NOT include duplicate entries | Yes | Yes | Yes | Same wording |

No WebAuthn clause is involved. The duplicate rule is a CTAP requirement that the platform must meet.

## Canonical Python reference

Python has the same issue (candidate for the divergence ledger). It forwards the caller's list without deduplication.

- `key_params = options.pub_key_cred_params` ([fido2/client/__init__.py line 751](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L751)). The same list is passed to the CTAP call ([line 862](https://github.com/Yubico/python-fido2/blob/5bc9d3a1c8c34a3c4ca408366e630b620db47faa/fido2/client/__init__.py#L862)).
- The line is a plain pass-through. No comment explains it.

## Sibling SDKs (context only)

Not checked for this finding.

## Reproduction

- Unit: [Fido2RequestAuditReproTests.NEW_MakeCredentialDoesNotEncodeDuplicateCredentialParameters](../../../src/Fido2/tests/Yubico.YubiKit.Fido2.UnitTests/AuditV2/Fido2RequestAuditReproTests.cs#L90-L119). Encodes `[ES256, ES256]` and asserts one entry under key 4. Result in the verification run: fails. The actual count is 2.
- Hardware: not applicable. CBOR encoding is enough to show the defect.

## Proposed fix

- Recommendation: reject duplicate `(Type, Algorithm)` pairs in `FidoSession.MakeCredentialAsync` with an `ArgumentException`. Put the check next to the existing empty-list check ([FidoSession.cs lines 250-255](../../../src/Fido2/src/FidoSession.cs#L250-L255)). Keep the order of the valid entries. A WebAuthn-level check in `ValidateRegistrationOptions` is optional, for a typed WebAuthn error.
- Options considered (small decision):
  1. Reject (recommended). Explicit. Cons: callers who pass duplicates now fail.
  2. Stable deduplication, keeping the first occurrence. Tolerant. Cons: the caller's list changes silently.
- API impact: behavioural, for callers who pass duplicate lists only. No signature change.
- Proving test: `NEW_MakeCredentialDoesNotEncodeDuplicateCredentialParameters`. Change it to expect the rejection (option 1), or to expect one entry (option 2).
- Depends on / interacts with: none.
- Open questions for the maintainer: reject (recommended) or deduplicate?

## Check it yourself

```bash
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~NEW_MakeCredentialDoesNotEncodeDuplicateCredentialParameters"
```

Spec: CTAP 2.3 §6.1, quoted above. Python: `fido2/client/__init__.py` lines 751 and 862, at the pinned commit.

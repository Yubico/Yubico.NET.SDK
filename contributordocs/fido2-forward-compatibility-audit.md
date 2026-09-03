<!-- Copyright 2026 Yubico AB

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. -->

# FIDO2 forward-compatibility audit

This audit identifies FIDO2 response-decoding paths where an authenticator can
return valid data that the SDK does not yet model. Its purpose is to distinguish
forward-compatibility limitations from required protocol validation and
malformed-data handling.

## Scope

The audit covers CBOR response decoding under
`Yubico.YubiKey/src/Yubico/YubiKey/Fido2/`. It does not cover caller-supplied
request validation. CTAPHID transport framing under `Pipelines/` has not been
assessed for this class of defect.

A forward-compatibility finding requires all of the following:

- The data was received from an authenticator.
- The data is valid under the applicable CTAP structure.
- The SDK does not recognize an optional value, field, or representation.
- The recognized data would still be useful to the caller.

The following are not forward-compatibility findings:

- malformed CBOR or a violation of CTAP canonical encoding;
- a missing required field;
- a recognized discriminator paired with an invalid payload; or
- a protocol field whose exact algorithm, key type, or curve is required for
  interoperability or security.

Existing tolerant patterns include `AuthenticatorOptions`, which maps unknown
option values to `OptionValue.Unknown`; `CborMap`, which preserves values it
cannot model as `RawCborValue`; and response types that expose raw encoded data
alongside typed properties.

## COSE key decoding

`CoseKey.Create` is the strict public decoder. It returns a modeled key or
throws. `CoseKey.CreateOrUnsupported` is the tolerant decoder for contexts in
which an otherwise valid key representation must be preserved even when the SDK
cannot provide a strongly typed key. It returns `CoseUnsupportedPublicKey` for
an algorithm unsupported by the strongly typed decoder and preserves the
original encoded key, reported key type, and reported algorithm.

The numeric type and algorithm on `CoseUnsupportedPublicKey` may or may not
correspond to named `CoseKeyType` and `CoseAlgorithmIdentifier` members. The
unsupported part is the complete key representation, not necessarily both
metadata values.

The tolerant decoder still rejects malformed encodings, missing type or
algorithm labels, and modeled algorithms paired with the wrong key type. It
also currently rejects a modeled algorithm with an unsupported curve or invalid
coordinate length. This avoids treating corrupted modeled keys as merely
unsupported.

Credential-management and authenticator-data response decoders use
`CreateOrUnsupported` because an unsupported credential public-key
representation must not discard the surrounding credential or response.
Applications that persist such keys can use the same decoder when loading them
and inspect `CoseUnsupportedPublicKey.EncodedKey`.

### Strict PIN/UV key agreement

The `keyAgreement` field returned by `authenticatorClientPIN` is not an open
algorithm-negotiation point. PIN/UV auth protocols one and two define the
returned `COSE_Key` as:

- key type `EC2` (`kty = 2`);
- algorithm `ECDH-ES + HKDF-256` (`alg = -25`); and
- curve NIST P-256 (`crv = 1`).

Consequently, `ClientPinResponse` must continue to decode this field with the
strict `CoseKey.Create` path. Accepting an unsupported key representation here
would only postpone failure until key agreement and could obscure a protocol
violation. Future PIN/UV auth protocols that define another key agreement
scheme require explicit protocol support rather than tolerant decoding of this
field.

## Forward-compatibility findings

Locations identify symbols rather than line numbers so that the audit remains
useful as files evolve.

| ID | Location | Finding | Impact |
|---|---|---|---|
| F1 | `Fido2/RelyingParty.cs`, encoded-value constructor | Any relying-party map key other than `id` or `name` causes a throw instead of being ignored or preserved. | High: credential-management relying-party enumeration can fail on an added field. |
| F2 | `Fido2/Commands/ClientPinResponse.cs`, `GetData` | An unknown top-level response key causes a throw even when all fields needed for the selected subcommand are present. | High: PIN/UV operations can fail on an additive response field. This does not apply to the strictly defined contents of `keyAgreement`. |
| F3 | `Fido2/MakeCredentialData.cs`, constructor | The response requires a `PackedAttestationStatement`; another valid attestation format is rejected after it has been decoded. | Potentially high, but reachability depends on which formats supported authenticators return. |
| F4 | `Fido2/AttestationStatement.cs`, `PackedAttestationStatement.FromCbor` | `ContainsOnlyKeys` rejects a packed attestation statement with an additional field. | High when an authenticator extends a packed statement. |
| F5 | `Fido2/AttestationStatement.cs`, `FidoU2fAttestationStatement.FromCbor`, `AppleAttestationStatement.FromCbor`, and `NoneAttestationStatement.FromCbor` | `ContainsExactKeys` rejects otherwise valid statements with additional fields. | Low to medium: typed attestation data is lost. |
| F6 | `Fido2/Fido2Session.Pin.cs`, `GetPreferredPinProtocol` | Selection inspects only the first advertised protocol, indexing an empty list throws the wrong exception, and an unsupported first protocol prevents construction even when a supported protocol appears later. The configuration escape hatch is an instance method, so constructor failure prevents callers from reaching it. | High: session construction can fail despite a mutually supported protocol. |
| F7 | `Fido2/Cose/CoseEcPublicKey.cs`, encoded-value constructor | Validation of an unsupported curve throws `ArgumentException`, so `CreateOrUnsupported` cannot preserve a key that uses a modeled algorithm with a future EC curve. | Medium: a future key representation is rejected. Any remedy must continue rejecting invalid coordinate lengths for modeled curves. |
| F8 | `Fido2/Cose/CoseEdDsaPublicKey.cs`, `CreateFromEncodedKey` | Public-key length is validated before the curve, making an unsupported OKP curve indistinguishable from a malformed Ed25519 key at that point. | Medium: future OKP representations cannot be preserved without restructuring validation. |
| F9 | `Fido2/AuthenticatorData.cs`, `GetCredProtectExtension` | Values outside the currently modeled range are rejected. | Medium: a future credProtect policy cannot be inspected through this helper. |
| F10 | `Fido2/AuthenticatorInfo.cs`, `Options` decoding | `AsDictionary<bool>()` rejects the entire options map when one value is not Boolean. | Medium: session construction can fail instead of preserving recognized options. |
| F11 | `Fido2/Cbor/CborMap.cs`, `ReadArray<TValue>` | One unconvertible array element rejects the complete array. | Low: callers cannot retain recognized elements where the protocol permits future values. Each call site must be assessed because some arrays are homogeneous by contract. |

`AuthenticatorInfo.Certifications` also uses an all-or-nothing dictionary
conversion. It is excluded from F10 because the current CTAP schema requires
integer certification values; a noninteger value is malformed rather than an
unrecognized extension.

## Related contract and diagnostic findings

These issues were identified in related COSE key handling code but do not meet
the forward-compatibility criteria above.

| ID | Location | Finding | Impact |
|---|---|---|---|
| F12 | `Fido2/Commands/GetKeyAgreementResponse.cs`, `GetData`, and `Fido2/PinProtocols/PinUvAuthProtocolBase.cs`, `Encapsulate` | A present but invalid key-agreement key is reported as a missing field at the response boundary, while the protocol layer reports a different generic key error. | Low: diagnostics do not clearly identify a protocol-invalid key. Requiring EC2, P-256, and algorithm `-25` remains correct. |
| F13 | `Fido2/Cose/CoseEcPublicKey.cs` and `Fido2/Cose/CoseEdDsaPublicKey.cs`, `Curve` properties | The property documentation names `NotSupportedException`, but unsupported curves currently produce `ArgumentException`. Correct the property documentation; do not change `ValidateCurve` while `CreateOrUnsupported` catches `NotSupportedException` (see the constraints below). | Low: the documented exception contract is inaccurate. |
| F14 | `Fido2/Cose/CoseEcPublicKey.cs`, curve-based constructor | Two `ArgumentException` calls pass message and parameter name in the wrong order. | Low: exception diagnostics are misleading. |

## Design constraints for remediation

Do not change `ValidateCurve` to throw `NotSupportedException` while `CreateOrUnsupported` catches
that exception type. Doing so would convert a modeled algorithm with an invalid or unsupported curve
into the forward-compatible sentinel, conflating malformed modeled data with an unmodeled algorithm.

Likewise, do not bypass the typed key constructors' curve and coordinate validation. The tolerant
path is for algorithms the typed decoder does not implement, not for structurally invalid data using
a modeled algorithm. Whether a future sentinel should ever represent a modeled algorithm is a
separate API decision and is not established by this audit.

Any remediation of these findings should retain these boundaries:

- Decode-side tolerance must not fabricate or re-emit authenticator-originated
  unknown fields in request encodings.
- Required fields and malformed CBOR must remain errors.
- Modeled algorithms with structurally invalid keys must remain errors.
- `CoseKey.Create` must remain strict; tolerant behavior belongs in
  `CreateOrUnsupported` and response decoders where surrounding data remains
  useful.
- PIN/UV key agreement must remain strict for protocols one and two: EC2,
  P-256, and algorithm `-25` are required.
- Public nullability and existing public property types should not be changed
  merely to represent an unsupported value when a preserving representation is
  available.

For F7 and F8, broadening a catch from `NotSupportedException` to
`ArgumentException` is unsafe because the same exception also represents
corrupt modeled keys. A solution must distinguish unsupported representation
from invalid data before changing tolerant-decoder behavior.

For F3, confirm that supported firmware can return a non-packed attestation
format before assigning priority. The SDK models multiple attestation statement
formats, but that alone does not establish current device behavior.

## Verification targets

Tests for tolerant COSE decoding should continue to establish that:

- modeled keys produce their modeled key types;
- unsupported representations preserve the complete original encoding and the
  reported metadata;
- persisted unsupported keys can be decoded again through the public tolerant
  API;
- trailing data is excluded from `EncodedKey` and `bytesRead` identifies only
  the key;
- malformed CBOR, missing required labels, wrong key-type pairings, and invalid
  modeled-key lengths still throw; and
- credential enumeration returns all entries when one credential uses an
  unsupported public-key representation.

## References

- CTAP PIN/UV auth protocol definitions, including the required key-agreement
  COSE key parameters.
- RFC 8949, Concise Binary Object Representation.
- RFC 9052, CBOR Object Signing and Encryption structures.
- RFC 9053, CBOR Object Signing and Encryption algorithms.

## Glossary

- **CBOR**: Concise Binary Object Representation.
- **COSE**: CBOR Object Signing and Encryption.
- **CTAP**: Client to Authenticator Protocol.
- **CTAPHID**: CTAP over the USB Human Interface Device transport.
- **EC2**: COSE key type for elliptic-curve keys with x- and y-coordinates.
- **OKP**: Octet Key Pair.
- **PIN/UV**: Personal Identification Number / User Verification.
- **SDK**: Software Development Kit.

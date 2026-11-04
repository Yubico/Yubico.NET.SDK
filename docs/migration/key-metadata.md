# Key identity and metadata migration (v2 development builds)

This coordinated breaking cleanup has no compatibility aliases. Numeric `KeyType` values, OID literals,
COSE identifiers, and ASN.1 encodings are unchanged. Recompile consumers using the new names.

## Names

| Earlier name | Replacement |
| --- | --- |
| `KeyType.ECP256`, `ECP384`, `ECP521` | `P256`, `P384`, `P521` |
| `KeyType.RSA1024/2048/3072/4096` | `Rsa1024/2048/3072/4096` |
| `KeyType.AES128/192/256` | `Aes128/192/256` |
| `KeyType.TripleDES` | `TripleDes` |
| `KeyDefinitions.RSA*`, `AES*` | `Rsa*`, `Aes*` |
| `IsRSA`, `GetByRSALength`, `GetByRSAModulusLength` | `IsRsa`, `GetByRsaLength`, `GetByRsaModulusLength` |
| `Oids.RSA`, `ECP*`, `AES*Cbc`, `TripleDESCbc` | `Rsa`, `P*`, `Aes*Cbc`, `TripleDesCbc` |
| `Oids.ECDSA` | `Oids.EcPublicKey` (id-ecPublicKey, also used with ECDH) |
| `GetOidsByKeyType(...).Curveoid` | `CurveOid` |
| `IsCoseKey()` | `HasCoseMapping()`; not an operation-capability check |
| `KeyDefinition.Name` | `DisplayName` for display, `KeyType` for symbolic identity |
| Core `CoseAlgorithmIdentifier.ES256/384/512`, `EdDSA`, `RS256` | `Es256/384/512`, `EdDsa`, `Rs256` |
| Core `CoseAlgorithmIdentifier.ECDHwHKDF256` | `EcdhEsHkdf256` (-25, ECDH-ES + HKDF-256) |

Existing `ECDsa` algorithm API spelling and independently numbered protocol enums are unchanged. Never
cast Core `KeyType` to a PIV/OpenPGP/COSE wire identifier. Explicit mappings remain at protocol boundaries.

`DisplayName` is explicit (for example `P-256`, `RSA-2048`, `AES-256`, `Triple DES`, `Ed25519`, `X25519`).
For persisted enum-name strings, migrate through an application-owned old-name/new-name table or versioned
serialization contract; do not silently substitute display names. Enum `ToString()` is not stable storage.

## Representation is not an operation

`KeyDefinitions` is the single shared read-only catalog. `KeyDefinition` and the OID extension methods
read that metadata; there is no second OID switch. Generic RSA/id-ecPublicKey OIDs alone cannot identify
a concrete variant. Existing AES/Triple DES algorithm OIDs are CBC-specific, not operation defaults.

`CoseKeyDefinition` moved from `Yubico.YubiKit.Core.Cryptography` to
`Yubico.YubiKit.Core.Cryptography.Cose`. Add that namespace import when constructing metadata. It is now
an immutable sealed record, with required init-only `Type` and nullable init-only `CurveIdentifier`.
Mutation after construction is no longer supported; construct a new value (or use `with`).

`CoseKeyDefinition.AlgorithmIdentifier` was removed. Supply the algorithm through the operation or profile,
or preserve the algorithm received in decoded data. A key can be used by multiple compatible algorithms.
EdDSA is not ECDSA and is not intrinsically limited to Ed25519; profile curve constraints remain separate.
The generic ES identifiers name ECDSA plus a hash, while profiles can constrain a curve. This cleanup does
not adopt new registry algorithms or add curves. [IANA COSE registry](https://www.iana.org/assignments/cose/cose.xhtml)
confirms algorithm -25 is ephemeral-static ECDH with HKDF-256, not a curve identifier.

| Concrete keys | COSE family | Curve |
| --- | --- | --- |
| P-256/P-384/P-521 | `Ec2` (2) | Corresponding `CoseEcCurve` |
| X25519/Ed25519 | `Okp` (1) | Corresponding `CoseEcCurve` |
| RSA-1024/2048/3072/4096 | `Rsa` (3) | null |
| AES-128/192/256 | `Symmetric` (4) | null |
| Triple DES | No mapping | null |

`GetCoseKeyType()` returns the nullable mapped family. Null means a known key without a mapping, whereas
an invalid `KeyType` lookup throws. A mapping does **not** establish codec, algorithm, profile, applet, or
hardware support. In particular AES metadata does not add a symmetric codec to CTAP2.

Active codecs are in `Yubico.YubiKit.Ctap2.Cose`, not the commented-out historical Core classes. CTAP2's
`CoseKey.KeyType` stays an open integer and `CoseAlgorithm` stays an open value carrier; unknown algorithms,
unknown key families, and raw unknown-key data remain preserved. PIN/UV key-agreement profile validation
is unchanged and strict. Core does not depend on CTAP2.

## Adding a future supported key

Add only a real supported concrete variant: assign a new explicit `KeyType` value without renumbering,
add display name/OIDs/length to `KeyDefinitions`, update classifications, and add an optional registered
COSE family/curve mapping. Implement module-specific operation support and provide actual key/encoding
vectors and consistency tests. Merely adding a registry constant is not support: `CoseEcCurve.X448/Ed448`
remain registry identifiers without corresponding supported `KeyType` values.

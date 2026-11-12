# CLAUDE.md - OpenPGP Module

This file provides module-specific guidance for working in **Yubico.YubiKit.OpenPgp**.
For overall repo conventions, see the repository root [CLAUDE.md](../../CLAUDE.md).

## Module Context

OpenPGP implements host access to the OpenPGP card application for YubiKey devices. The referenced
functional specification is Version 3.4.1, March 18, 2020, despite its URL's 3.4 filename. It provides:
- **Key Management**: Generate/import/delete ordinary-slot keys and attest generated Sig/Dec/Aut keys;
  ATT is a vendor extension whose provisioning currently uses malformed CRT encoding
- **Cryptographic Operations**: Sign, raw RSA decipher/ECDH agreement, and authentication using on-device keys;
  host applications handle packet/content processing and trust decisions
- **PIN Management**: Verify, change, and reset User PINs, Admin PINs, and Reset Codes with KDF support
- **Certificate Storage**: Store and retrieve X.509 certificates associated with key slots
- **Device Configuration**: Configure touch policy (UIF), algorithm attributes, and KDF settings

## Architecture

### Transport
SmartCard-only (`ISmartCardConnection`), over USB CCID or NFC. Applet SELECT and APDU send mechanics are routed through the module-local `OpenPgpBackend`; OpenPGP APDU construction stays in `OpenPgpSession`.

### Session Structure
`OpenPgpSession` is a `sealed partial class` split across 7 files:

| File | Responsibility |
|------|----------------|
| `OpenPgpSession.cs` | Core lifecycle, `CreateAsync`, `GetData`/`PutData`, cached state |
| `OpenPgpSession.Pin.cs` | PIN verify/change/reset, KDF integration |
| `OpenPgpSession.Keys.cs` | Key generate/import/delete/attest |
| `OpenPgpSession.Certificates.cs` | Certificate CRUD |
| `OpenPgpSession.Config.cs` | UIF, algorithm attributes, KDF configuration |
| `OpenPgpSession.Crypto.cs` | Sign, decrypt, authenticate |
| `OpenPgpSession.Reset.cs` | Factory reset sequence |

### Shared Session State
- `IOpenPgpBackend? _backend` — selected SmartCard APDU send/select wrapper; protocol disposal remains owned by `ApplicationSession`
- Inherited `ApplicationSession.FirmwareVersion` — effective version for protocol configuration and feature gating;
  initialization chooses the override or detected applet version. The detected version is logged, not retained in a `_version` field.
- `ApplicationRelatedData _appData` — cached card state (refreshable)
- `Kdf? _kdf` — active KDF configuration (lazy-loaded)
- `GetKdfAsync` returns the cached instance and `SetKdfAsync` retains its input instance. Session
  disposal clears the current KDF; replacement/reset currently drops the old reference without disposal.
  Preserve that present ownership caveat in examples; keep KDF objects alive until session teardown.
  Application-data refresh leaves KDF unchanged; raw F9 writes require a fresh session to reload it.
- User-presence policy uses the UIF already parsed into cached application-related data. If that per-key
  entry is absent, the operation falls back to one direct UIF read. `SetUifAsync` updates the session cache

Public key generation uses `GenerateKeyAsync(KeyRef, AlgorithmAttributes, CancellationToken)`. User PIN reset is
split into reset-code and prior-administrator-authentication methods so no input is conditionally ignored.

## Key Patterns

### Initialization Sequence
1. Bind the borrowed connection and register the owned protocol before device I/O.
2. SELECT OpenPGP AID → handle 0x6285/0x6985 with ACTIVATE → re-SELECT
3. GET_VERSION (INS=0xF1) → BCD decode the first three bytes of an OK response with at least three bytes.
   Every non-OK response, short OK response or caught APDU exception falls back to 1.0.0; other exceptions propagate.
   This fallback establishes no qualified minimum device version. Keep applet response, physical-device
   metadata and effective configured version distinct, especially on development devices.
4. Configure the effective override-or-detected version and optional SCP, then rebind the backend if needed.
   Physical-device metadata is not consulted. The override does not skip SELECT/version discovery.
5. Cache `ApplicationRelatedData` for algorithms/UIF; F9/KDF remains lazy. Publish readiness only after initialization
   completes. Failed initialization disposes/detaches the session; the device helper cleans up its hidden connection.

Core guards each logical exchange, not whole certificate, key, credential or reset workflows. Await the complete public
task before another operation or disposal, including parsing and presence callbacks. Keep borrowed inputs/keys and the
caller-owned prompt alive through that task. Synchronous session disposal clears the current KDF before base protocol
drain; asynchronous disposal awaits Core teardown and then invokes derived KDF cleanup in finally, even on failure.
Neither ordering supplies a whole-workflow lease. F9 response/encoding/parser copies retain separate cleanup gaps.

### KDF (Iterated-Salted-S2K)
Iterated-and-Salted S2K hashes a byte count in one hash context:
```
data = salt + pin_bytes
data_count, trailing = divmod(max(iteration_count, len(data)), len(data))
hash data data_count times, then data[:trailing]
```
Supports SHA256 (0x08) and SHA512 (0x0A).

Card §4.3.2 assigns credential/F9 alignment to terminal software. YubiKey's vendor transition uses initial
User/Admin hashes and invalidates the Reset Code. Configure before ordinary keys; a state-dependent F9 refusal
needs inspection rather than an invented universal firmware rule. Change methods derive both old/new inputs
with the current KDF. Current extended User VERIFY selects Reset identity for salt/6982 retry fallback even
though P2=81 and 82 both verify PW1. Distinct salts can reject a correct PIN; preserve this defect qualification.

### Factory Reset Sequence
1. Get PIN status (remaining attempts)
2. Block User PIN: VERIFY with invalid PIN until attempts exhausted
3. Block Admin PIN: same
4. TERMINATE (INS=0xE6)
5. ACTIVATE (INS=0x44)

Current TERMINATE/ACTIVATE statuses are ignored and there is no complete reset postcondition. A later
SELECT/read establishes access, so reset guidance requires independent state inspection. Ordinary material
is erased on successful device reset; YubiKey ATT key/certificate and fixed ATT policy survive. Ordinary
key replacement clears fixed UIF; fixed ATT recovery requires irreversible ATT private-key replacement.

Reset preserves the effective version and inherited readiness/authentication state. An existing SCP session retains
its processor graph and keys: SELECT is plain, subsequent sends are secure, and no new handshake runs. OpenPGP
retains no creation SCP parameters for reset. Reopen after uncertainty and inspect persistent state before retrying;
an authentication flag alone establishes no channel restoration. The integration helper instead resets in a separate
plain session before creating an optional SCP body session, so it supplies no SCP-reset oracle.

## Model Layer

Core owns the shared `CryptographicDefinitions.Keys/Curves/Algorithms` catalog. `EcAttributes.KeyType`
and `EcAttributes.Create(KeyRef, KeyType)` use Core concrete identities directly. OpenPGP owns the optional
immutable `OpenPgpKeyInfo` returned by `KeyInfo.GetOpenPgpInfo()`; Core must not reference OpenPgp. This is
representation metadata, not device capability or a choice of slot/import format. It includes modeled EC
representations and only RSA 2048/3072/4096, not Core's RSA 1024.

Internal `OpenPgpCurveOids` caches DER **value octets only**, deriving them from dotted OIDs with AsnWriter.
Ordinary curve OIDs come from Core; the protocol's legacy X25519 (1.3.6.1.4.1.3029.1.5.1) and Ed25519
(1.3.6.1.4.1.11591.15.1) overrides stay here. They are not RFC 8410 SPKI algorithm OIDs. Failed Try lookup
returns `KeyType.None`. Unknown OID parser rejection is unchanged; do not silently select P-256.

Factories choose ECDH/ECDSA/EdDSA from construction and slot, rejecting non-EC families instead of falling
through to ECDSA. Parsing must preserve the received algorithm byte and import format; never call the factory
while parsing. Existing supported-algorithm firmware corrections remain protocol-owned. Wire IDs stay
ECDH 0x12, ECDSA 0x13 and EdDSA 0x16. `PrivateKeyImport` uses the Core key identity directly, while retaining
the X25519 scalar reversal, private/public consistency checks and owned-buffer clearing.

To admit a new OpenPGP representation, add it to this module's modeled data only after independent OID,
real provider-validated key/import-template and APDU vectors. The four non-NIST ordinary curves are covered
by `NamedCurveCatalogVectorsTests` (Core) and `OpenPgpPutKeyWireTests`; hardware support remains separate.

### Protocol Enums (internal)
- `Ins` — Instruction bytes
- Backend, `BcdHelper`, `DigestInfo`, `PrivateKeyImport` and the private-key-template hierarchy

### Domain Enums (public)
- `KeyRef` — Key slots with computed DO tag properties
- `Crt` (static class), `DataObject` — public CRT/data-object carriers; direct-access rules are object-specific
- `Pw` — logical credentials (User=0x81, Reset Code=0x82, Admin=0x83). VERIFY P2=82 separately means PW1 other-operation mode
- `Uif` — User Interaction Flag with `IsFixed`/`IsCached`
- `PinPolicy`, `KeyStatus`, `RsaSize`, `RsaImportFormat`, `EcImportFormat`
- Core `KeyType` — concrete EC key identity in algorithm attributes
- `ExtendedCapabilityFlags`, `GeneralFeatureManagement` — `[Flags]` enums

### Data Models (public)
- `AlgorithmAttributes` (abstract) → `RsaAttributes`, `EcAttributes`
- `ApplicationRelatedData` — Top-level parsed card state
- `OpenPgpAid` — Parsed AID with BCD-decoded version
- `PwStatus` — PIN attempt counters and length limits
- `Kdf` (abstract) → `KdfNone`, `KdfIterSaltedS2k`
- `SupportedAlgorithm` — ordered per-slot algorithm entry; list slots can repeat
- `KeyInformation`, `KeyFingerprints`, `GenerationTimes` — sealed read-only dictionary domain types with
  private collection storage; parsers transfer exclusively owned dictionaries into them

Public key metadata uses semantic names: `ListKeyInformationAsync`, `GetKeyFingerprintsAsync`, and
`SetKeyFingerprintAsync`, with discretionary properties `KeyInformation`, `KeyFingerprints`, and
`CaKeyFingerprints`. Protocol `DataObject` tag names retain the OpenPGP specification terminology.
`GetSupportedAlgorithmsAsync` returns ordered `SupportedAlgorithm` entries; repeated `KeyRef` values are
valid and preserve the card's per-slot algorithm multiplicity.

## Firmware Compatibility

These are current SDK checks, subject to advertised card/family/slot capabilities and separate historical
qualification. Vendor commercial-version wording and development-source gates have different provenance.

| Feature | SDK threshold | Gating |
|---------|-------------|--------|
| Factory reset | 1.0.6 | `EnsureSupports` |
| UIF (touch policy) | 4.2.0 | `EnsureSupports` |
| EC key support | 5.2.0 | `EnsureSupports` |
| Certificates | 5.2.0 | `EnsureSupports` |
| Key attestation | 5.2.0 | `EnsureSupports` |
| Unverify PIN | 5.6.0 | `EnsureSupports` |

Cached/ATT UIF and retry/challenge capability checks remain incomplete. NEO RSA import advertises CRT 02
while including modulus (vendor uses 03), and vendor NEO deletion differs from attribute switching. Preserve
those compatibility qualifications. RSA generation lacks a shipping ROCA guard: YSA-2017-01 identifies
4.2.6–4.3.4 as affected and 4.3.5 as corrected; imported keys are a mitigation. Existing tests early-return
through 4.3.5 inclusive, including imported-key tests, so runner success there establishes no executed body.

## Security Requirements

- Public secret parameters are named plainly (`pin`, `currentPin`, `newPin`, `resetCode`)
  with no `Utf8` suffix; the UTF-8 encoding contract and the caller's ownership/clearing
  responsibility live in the XML documentation for each parameter, not in its name.
- Zero ALL PIN bytes after use: `CryptographicOperations.ZeroMemory()`
- Zero private key material after import
- Zero KDF-derived bytes after use
- Preserve caller ownership of disposable `IPrivateKey` inputs and certificate objects. Returned signature/
  decipher memory is caller-owned; clear its actual backing storage with `MemoryMarshal.AsMemory`, rather
  than clearing a new copy. Borrowed input/static/model memory retains its own owner's lifetime contract.
- RSA/ECDSA hash input; EdDSA sends raw messages and ignores the hash parameter. RSA signatures are raw,
  ECDSA output is DER and Ed25519 output is 64 bytes. RSA/EdDSA transfer successful response storage;
  ECDSA clears raw storage after conversion. Decipher copies successful output and clears its raw response.
- Current X25519 decipher omits the EC cipher DO wrapper; import/public-key tests establish no agreement result.
- Use `CryptographicOperations.FixedTimeEquals()` for comparisons
- NEVER log PINs, keys, or credentials — only slot names, algorithm types, lengths

## Test Infrastructure

See `tests/CLAUDE.md` for test runner requirements.

**Unit tests** focus on model parsing/encoding:
- AlgorithmAttributes round-trip, ApplicationRelatedData TLV parsing
- KDF process against test vectors, OpenPgpAid BCD decoding
- PrivateKeyTemplate TLV encoding, PwStatus parsing, exact OpenPGP OID value vectors

**Integration tests** use `[Theory] [WithYubiKey]` with firmware version gating.
The helper resets before use when requested and leaves post-test state in place. Comments should name
actual assertions: nonempty/length checks establish response shape, host verification establishes a signature,
and certificate-byte replacement establishes overwrite rather than trust-chain validation. Full reset verification
requires more than the existing three retry-counter assertions.
The Unverify signing-failure test leaves signature policy uncontrolled: `Always` could already consume the earlier
authorization. Its later APDU exception does not isolate Unverify's effect or establish both User modes cleared.

## User-presence notifications

When `SessionCreationOptions.UserPresencePrompt` is supplied, signing, decryption, authentication, and
attestation use the relevant cached UIF before the cryptographic APDU. `On`/`Fixed` map to `PolicyRequires`,
`Cached`/`CachedFixed` map to `PolicyMayRequire`, and `Off` is silent. Attestation uses the attestation-key
UIF (`KeyRef.Att`). A failed UIF read falls back to `PolicyMayRequire` without replacing the cryptographic
operation; cancellation still propagates. Firmware without UIF support stays silent because it cannot configure
that policy. A direct GET DATA is issued only when the cached application-related data omitted that key's UIF.
Device APDU errors resolve as `Failed`; OpenPGP statuses including 6600 currently receive no `TimedOut`
classification. Terminal outcomes are `Completed`, `Cancelled`, and `Failed`. Decryption resolves successfully before materializing the caller's
plaintext copy; the raw response is cleared whether resolution succeeds or throws.

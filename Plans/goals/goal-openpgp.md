# GOAL: Implement OpenPGP Applet for Yubico.NET.SDK

## Context

This is the Yubico.NET.SDK (YubiKit), a .NET 10 / C# 14 SDK for YubiKey devices. You are implementing the **OpenPGP** applet (OpenPGP card v3.4 specification). This is a 2.0 effort on `yubikit-*` branches -- do NOT touch `develop` or `main`.

OpenPGP is the LARGEST applet (1,793 lines canonical). You MUST use partial classes to organize the session.

## MANDATORY: Read These Files First

Before writing ANY code, you MUST read and internalize these files line by line:

1. **`CLAUDE.md`** (repository root) - All coding standards, memory management, security, modern C# patterns, build/test
2. **`Yubico.YubiKit.Management/CLAUDE.md`** - Session patterns, DI, IYubiKey extensions, test infrastructure
3. **`Yubico.YubiKit.SecurityDomain/CLAUDE.md`** - Session initialization, reset patterns, SCP integration
4. **`docs/TESTING.md`** - Test infrastructure, xUnit v2/v3 differences, `[WithYubiKey]` attribute, test categories

## MANDATORY: Study These Reference Implementations

### Canonical Protocol Reference (Python) - READ EVERY LINE
**File:** `/Users/Dennis.Dyall/Code/y/yubikey-manager/yubikit/openpgp.py` (1,793 lines)

### Secondary Reference (Java)
**Directory:** `/Users/Dennis.Dyall/Code/y/yubikit-android/openpgp/src/main/java/com/yubico/yubikit/openpgp/`

### Architecture Reference (Existing C# Applets)
Study these for the EXACT patterns to replicate:
- `Yubico.YubiKit.Management/src/ManagementSession.cs` - Session pattern (private ctor, static CreateAsync, two-phase init)
- `Yubico.YubiKit.Management/src/DependencyInjection.cs` - Factory delegate + C# 14 `extension()` syntax
- `Yubico.YubiKit.Management/src/IYubiKeyExtensions.cs` - Convenience extensions with C# 14 `extension(IYubiKey)` syntax
- `Yubico.YubiKit.Management/src/IManagementSession.cs` - Interface extending IApplicationSession
- `Yubico.YubiKit.SecurityDomain/src/SecurityDomainSession.cs` - SmartCard-only session, direct protocol calls

## Architecture Requirements

### Source Files (in `Yubico.YubiKit.OpenPgp/src/`)

SmartCard-only — follow SecurityDomainSession's direct protocol call pattern (no Backend abstraction).
MUST use partial classes for the session (the session will be 500+ lines).

1. **`IOpenPgpSession.cs`** - Public interface extending `IApplicationSession`
2. **Session partial classes (all `sealed partial class OpenPgpSession`):**
   - `OpenPgpSession.cs` - Core: private ctor, `CreateAsync`, version property, `GetData`/`PutData`, `GetApplicationRelatedData`, `GetChallenge`, `GetSignatureCounter`, `GetPinStatus`
   - `OpenPgpSession.Pin.cs` - PIN operations: `VerifyPin`, `VerifyAdmin`, `UnverifyPin(PW)`, `ChangePin`, `ChangeAdmin`, `SetResetCode`, `ResetPin`, `SetPinAttempts`, `SetSignaturePinPolicy`
   - `OpenPgpSession.Keys.cs` - Key operations: `GenerateRsaKey`, `GenerateEcKey`, `PutKey`, `DeleteKey`, `GetPublicKey`, `AttestKey`, `GetKeyInformation`, `GetGenerationTimes`, `SetGenerationTime`, `GetFingerprints`, `SetFingerprint`
   - `OpenPgpSession.Certificates.cs` - Certificate operations: `GetCertificate`, `PutCertificate`, `DeleteCertificate`
   - `OpenPgpSession.Config.cs` - Configuration: `GetUif`/`SetUif`, `GetAlgorithmAttributes`/`SetAlgorithmAttributes`, `GetAlgorithmInformation`, `GetKdf`/`SetKdf`
   - `OpenPgpSession.Crypto.cs` - Crypto operations: `Sign`, `Decrypt`, `Authenticate`
   - `OpenPgpSession.Reset.cs` - Reset: `Reset()` (block PINs, TERMINATE, ACTIVATE)
3. **`DependencyInjection.cs`** - Factory delegate `OpenPgpSessionFactory` + `AddOpenPgp()` extension using C# 14 `extension(IServiceCollection services)` syntax
4. **`IYubiKeyExtensions.cs`** - Convenience extensions using C# 14 `extension(IYubiKey yubiKey)`:
   - `CreateOpenPgpSessionAsync(...)` for multi-operation scenarios

### Model Files (one per type):
- `Uif.cs` - enum (Off=0x00, On=0x01, Fixed=0x02, Cached=0x03, CachedFixed=0x04) with `IsFixed` and `IsCached` properties, `__bytes__` = struct.pack(">BB", self, BUTTON)
- `PinPolicy.cs` - enum (Always=0x00, Once=0x01)
- `Pw.cs` - enum (User=0x81, Reset=0x82, Admin=0x83)
- `DataObject.cs` - enum with ALL DO tags:
  - PrivateUse1=0x0101 through PrivateUse4=0x0104
  - Aid=0x4F, Name=0x5B, LoginData=0x5E, Language=0xEF2D, Sex=0x5F35
  - Url=0x5F50, HistoricalBytes=0x5F52, ExtendedLengthInfo=0x7F66
  - GeneralFeatureManagement=0x7F74, CardholderRelatedData=0x65
  - ApplicationRelatedData=0x6E
  - AlgorithmAttributesSig=0xC1, AlgorithmAttributesDec=0xC2, AlgorithmAttributesAut=0xC3, AlgorithmAttributesAtt=0xDA
  - PwStatusBytes=0xC4
  - FingerprintSig=0xC7 through FingerprintAtt=0xDB
  - CaFingerprint1=0xCA through CaFingerprint4=0xDC
  - GenerationTimeSig=0xCE through GenerationTimeAtt=0xDD
  - ResettingCode=0xD3
  - UifSig=0xD6, UifDec=0xD7, UifAut=0xD8, UifAtt=0xD9
  - SecuritySupportTemplate=0x7A, CardholderCertificate=0x7F21
  - Kdf=0xF9, AlgorithmInformation=0xFA, AttCertificate=0xFC
- `KeyRef.cs` - enum (Sig=0x01, Dec=0x02, Aut=0x03, Att=0x81) with properties:
  - `AlgorithmAttributesDo` → `DO.AlgorithmAttributes{Name}`
  - `UifDo` → `DO.Uif{Name}`
  - `GenerationTimeDo` → `DO.GenerationTime{Name}`
  - `FingerprintDo` → `DO.Fingerprint{Name}`
  - `Crt` → `CRT.{Name}`
- `KeyStatus.cs` - enum (None=0, Generated=1, Imported=2)
- `Crt.cs` - enum/static class defining Control Reference Templates as byte arrays:
  - Sig = Tlv(0xB6), Dec = Tlv(0xB8), Aut = Tlv(0xA4), Att = Tlv(0xB6, Tlv(0x84, 0x81))
- `Ins.cs` - internal enum of instruction bytes (Verify=0x20, ChangePin=0x24, ResetRetryCounter=0x2C, Pso=0x2A, Activate=0x44, GenerateAsym=0x47, GetChallenge=0x84, InternalAuthenticate=0x88, SelectData=0xA5, GetData=0xCA, PutData=0xDA, PutDataOdd=0xDB, Terminate=0xE6, GetVersion=0xF1, SetPinRetries=0xF2, GetAttestation=0xFB)
- `AlgorithmAttributes.cs` - abstract base class:
  - `int AlgorithmId` property
  - `static AlgorithmAttributes Parse(ReadOnlySpan<byte>)` factory that dispatches to RsaAttributes or EcAttributes
  - Abstract `byte[] ToBytes()` method
- `RsaAttributes.cs` - RSA algorithm attributes: `NLen`, `ELen`, `ImportFormat`
  - `static RsaAttributes Create(RsaSize, RsaImportFormat)` factory
  - Parse: `struct.unpack(">HHB", encoded)` → (nLen, eLen, importFormat)
  - ToBytes: `struct.pack(">BHHB", algorithmId, nLen, eLen, importFormat)`
- `EcAttributes.cs` - EC algorithm attributes: `Oid` (CurveOid), `ImportFormat`
  - `static EcAttributes Create(KeyRef, CurveOid)` factory
  - Algorithm ID selection: Ed25519 → 0x16 (EdDSA), DEC → 0x12 (ECDH), else → 0x13 (ECDSA)
- `RsaSize.cs` - enum (Rsa2048=2048, Rsa3072=3072, Rsa4096=4096)
- `RsaImportFormat.cs` - enum (Standard=0, StandardWMod=1, Crt=2, CrtWMod=3)
- `EcImportFormat.cs` - enum (Standard=0, StandardWPubkey=0xFF)
- `CurveOid.cs` - enum of OIDs: Secp256R1, Secp256K1, Secp384R1, Secp521R1, BrainpoolP256R1, BrainpoolP384R1, BrainpoolP512R1, X25519, Ed25519
- `CardholderRelatedData.cs` - record (Name: byte[], Language: byte[], Sex: int)
- `ExtendedLengthInfo.cs` - record (RequestMaxBytes: int, ResponseMaxBytes: int)
- `ExtendedCapabilities.cs` - record (Flags, SmAlgorithm, ChallengeMaxLength, CertificateMaxLength, SpecialDoMaxLength, PinBlock2Format, MseCommand)
- `ExtendedCapabilityFlags.cs` - `[Flags] enum` (Kdf=1, PsoDecEncAes=2, AlgorithmAttributesChangeable=4, PrivateUse=8, PwStatusChangeable=16, KeyImport=32, GetChallenge=64, SecureMessaging=128)
- `PwStatus.cs` - record (PinPolicyUser, MaxLenUser, MaxLenReset, MaxLenAdmin, AttemptsUser, AttemptsReset, AttemptsAdmin) with `GetMaxLen(PW)` and `GetAttempts(PW)` methods
- `DiscretionaryDataObjects.cs` - composite record containing all algorithm attributes (sig/dec/aut/att), pw_status, fingerprints, ca_fingerprints, generation_times, key_information, uif values
- `ApplicationRelatedData.cs` - top-level record (Aid: OpenPgpAid, Historical: byte[], ExtendedLengthInfo?, GeneralFeatureManagement?, Discretionary: DiscretionaryDataObjects)
- `OpenPgpAid.cs` - class extending `byte[]` with properties: `Version` (tuple, BCD-decoded from bytes 6-7), `Manufacturer` (uint16 from bytes 8-9), `Serial` (BCD from bytes 10-13, negative if invalid BCD)
- `SecuritySupportTemplate.cs` - record (SignatureCounter: int)
- `Kdf.cs` - abstract base with `Process(PW, pin)` method:
  - `KdfNone` - returns pin.encode() directly
  - `KdfIterSaltedS2k` - iterated-salted-S2K: hash_algorithm, iteration_count, salts (user/reset/admin), initial hashes
    - `Process`: concatenate salt + pin bytes, repeat to fill iteration_count bytes, hash once
    - NOT PBKDF2! The iteration_count is bytes-to-hash, not rounds
    - `Create()` factory generates random salts and pre-computes initial hashes for default PINs
- `PrivateKeyTemplate.cs` - abstract base for key import:
  - `RsaKeyTemplate` - e, p, q fields
  - `RsaCrtKeyTemplate` extends RsaKeyTemplate - adds iqmp, dmp1, dmq1, n
  - `EcKeyTemplate` - privateKey, publicKey? fields
  - Format: TLV(0x4D, CRT + TLV(0x7F48, headers) + TLV(0x5F48, concatenated values))
- `GeneralFeatureManagement.cs` - `[Flags] enum` (Touchscreen=1, Microphone=2, Loudspeaker=4, Led=8, Keypad=16, Button=32, Biometric=64, Display=128)

### Wire Protocol Details

**INS Bytes (from Python INS enum):**
- VERIFY=0x20, CHANGE_PIN=0x24, RESET_RETRY_COUNTER=0x2C
- PSO=0x2A (Perform Security Operation: sign=9E/9A, decrypt=80/86)
- ACTIVATE=0x44, GENERATE_ASYM=0x47 (generate=0x80/0x00, read public=0x81/0x00)
- GET_CHALLENGE=0x84, INTERNAL_AUTHENTICATE=0x88
- SELECT_DATA=0xA5, GET_DATA=0xCA, PUT_DATA=0xDA, PUT_DATA_ODD=0xDB
- TERMINATE=0xE6, GET_VERSION=0xF1, SET_PIN_RETRIES=0xF2, GET_ATTESTATION=0xFB

**Key implementation details from Python:**

**SELECT and Initialization:**
- SELECT may fail with NO_INPUT_DATA (0x6285) or CONDITIONS_NOT_SATISFIED (0x6985) → send ACTIVATE (INS=0x44), then re-SELECT
- Version read: GET_VERSION (INS=0xF1), BCD-decoded: `Version(*(_bcd(x) for x in bcd))` where `_bcd(v) = 10 * (v >> 4) + (v & 0xF)`
- Pre-1.0.2 firmware: GET_VERSION throws CONDITIONS_NOT_SATISFIED → default to Version(1, 0, 0)
- After version, cache `_app_data = get_application_related_data()` (WARNING: this cache can become stale)

**Reset Process (complex!):**
1. Get current PIN status (attempts remaining)
2. For both USER and ADMIN PINs: send VERIFY with invalid PIN (`b"\0" * 8`) repeatedly until all attempts exhausted
3. Send TERMINATE (INS=0xE6)
4. Send ACTIVATE (INS=0x44)
5. Requires firmware >= 1.0.6

**PIN Verification:**
- `_process_pin(kdf, pw, pin)` → KDF transforms PIN string to bytes, validates length (6-max for USER, 8-max for ADMIN)
- VERIFY: INS=0x20, P2=PW value (0x81 for USER+sign, 0x82 for USER+extended, 0x83 for ADMIN)
- Extended mode: `verify_pin(pin, extended=True)` uses P2=0x82 — allows decrypt/auth but NOT sign
- Error handling: SW=SECURITY_CONDITION_NOT_SATISFIED or 0x63xx (pre-4.0) → InvalidPinError with remaining attempts
- UnverifyPin (5.6.0+): INS=0x20, P1=0xFF, P2=PW value

**KDF (Iterated-Salted-S2K):**
- NOT PBKDF2! This is OpenPGP-specific:
- `iteration_count` = total bytes to feed into hash function (NOT iteration rounds)
- data = salt + pin_bytes
- Compute: `data_count, trailing = divmod(iteration_count, len(data))`
- Feed `data` to hash `data_count` times, then `data[:trailing]` once
- Call `digest.finalize()` → result
- Supports SHA256 (0x08) and SHA512 (0x0A)
- `Create()`: generates random 8-byte salts, pre-hashes default PINs ("123456" for user, "12345678" for admin)

**Cryptographic Operations:**
- **Sign (PSO):** INS=0x2A, P1=0x9E, P2=0x9A
  - RSA: prepend PKCS#1 v1.5 DigestInfo header (from `_pkcs1v15_headers` lookup by hash type) + hash
  - ECDSA: just the hash bytes
  - EdDSA: raw message (no hashing)
  - EC response: split in half, DER-encode with `encode_dss_signature(r, s)`
- **Decrypt (PSO):** INS=0x2A, P1=0x80, P2=0x86
  - RSA: prepend 0x00 byte
  - EC (ECDH): wrap in TLV(0xA6, TLV(0x7F49, TLV(0x86, public_key_bytes)))
  - X25519: raw bytes
- **Authenticate:** INS=0x88, P1=0x00, P2=0x00 — same padding as Sign but uses AUT key

**PKCS#1 v1.5 DigestInfo Headers:**
- SHA256: `3031300D060960864801650304020105000420`
- SHA384: `3041300D060960864801650304020205000430`
- SHA512: `3051300D060960864801650304020305000440`
- SHA1: `3021300906052B0E03021A05000414`
- (see full list in Python `_pkcs1v15_headers` dict)

**Key Import:**
- PUT_DATA_ODD (INS=0xDB, P1=0x3F, P2=0xFF) with PrivateKeyTemplate bytes
- Set algorithm attributes first if ALGORITHM_ATTRIBUTES_CHANGEABLE flag is set
- NEO (version[0] < 4): use CRT import format with modulus
- Delete key: version < 4 → import random RSA-2048 over it; version >= 4 → change attributes to RSA-4096 then back to RSA-2048

**Certificate Operations:**
- SELECT_DATA (INS=0xA5) to select certificate slot before read/write
- Certificate DO = 0x7F21, ATT certificate DO = 0xFC
- Pre-5.2.0: only AUT certificate slot works (default)
- 5.2.0-5.4.3: non-standard byte 0x06 prepended to SELECT_DATA payload
- Attestation: INS=0xFB with CLA=0x80

**Algorithm Information (5.2.0+):**
- DO 0xFA returns TLV list of supported algorithms per key slot
- Pre-5.6.1 firmware: fix invalid Curve25519 entries (X25519 with EdDSA must be removed, X25519 ECDH added to DEC, EdDSA removed from DEC and ATT)

### Security Requirements

- Zero ALL PIN bytes after verification/change: `CryptographicOperations.ZeroMemory()`
- Zero admin PIN bytes after verification
- Zero private keys after import (RSA numbers, EC private values)
- Zero KDF-derived PIN bytes after use
- Zero reset codes after setting
- Use `CryptographicOperations.FixedTimeEquals()` for any comparisons
- NEVER log PINs, admin PINs, reset codes, or private key material
- Only log: key slot names, algorithm types, version, operation names

### Test Files

**Unit tests** in `Yubico.YubiKit.OpenPgp/tests/Yubico.YubiKit.OpenPgp.UnitTests/`:
- Test AlgorithmAttributes parsing: RSA (2048/3072/4096), EC (various curves), EdDSA (Ed25519)
- Test AlgorithmAttributes round-trip: parse → toBytes → parse should be identical
- Test ApplicationRelatedData parsing from known TLV bytes
- Test KDF encoding/decoding: KdfNone round-trip, KdfIterSaltedS2k with known salt/hash
- Test KdfIterSaltedS2k.Process against known test vector (verify the byte-count iteration logic)
- Test CurveOid mapping: each OID resolves to correct .NET crypto curve
- Test PwStatus parsing from 7-byte encoded status
- Test OpenPgpAid parsing: version (BCD), manufacturer, serial (valid BCD and invalid BCD → negative)
- Test PrivateKeyTemplate encoding: RSA standard, RSA CRT, EC (verify TLV structure)
- Test PKCS#1 v1.5 DigestInfo header lookup for each hash algorithm
- Test BCD decoding helper

**Integration tests** in `Yubico.YubiKit.OpenPgp/tests/Yubico.YubiKit.OpenPgp.IntegrationTests/`:
- Use `[Theory] [WithYubiKey]` attribute pattern
- Create `OpenPgpTestStateExtensions.cs` with `WithOpenPgpSessionAsync` helper
- Test: reset (full PIN-blocking + terminate + activate sequence), verify clean state
- Test: verify default PINs (user: "123456", admin: "12345678")
- Test: change user PIN, verify with new PIN, reset to restore defaults
- Test: get application related data, verify version/serial/capabilities
- Test: get algorithm attributes for all key slots
- Test (5.2.0+): get algorithm information, verify supported algorithms
- Test (5.2.0+): generate EC P256 key in SIG slot, get public key, verify key type
- Test: generate RSA 2048 key in SIG slot (skip 4.2.0-4.3.5), get public key
- Test (5.2.0+): sign message with EC key, verify signature
- Test: sign message with RSA key, verify with PKCS#1 v1.5
- Test (5.2.0+): put/get/delete certificate
- Test (5.2.0+): attest key
- Test (4.2.0+): get/set UIF
- Test: get pin status, verify default attempts (3/0/3)
- Skip touch-requiring tests: `[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]`

**Testing rules:**
- ALWAYS use `dotnet toolchain.cs test` (NEVER `dotnet test`)
- `[WithYubiKey]` + `[InlineData]` is INCOMPATIBLE - use separate test methods
- Skip user-presence tests: `--filter "Category!=RequiresUserPresence"`
- Version-gated tests: `[WithYubiKey(MinFirmware = "5.2.0")]` for EC/attestation/certificates

### CLI Tool (in `Yubico.YubiKit.OpenPgp/examples/OpenPgpTool/`)

```
OpenPgpTool/
├── OpenPgpTool.csproj
├── Program.cs                         # FigletText banner + main menu
├── Cli/
│   ├── Output/OutputHelpers.cs
│   ├── Prompts/DeviceSelector.cs
│   └── Menus/
│       ├── InfoMenu.cs                # Card status, application data, key info
│       ├── PinMenu.cs                 # PIN management (verify, change, reset)
│       ├── KeyMenu.cs                 # Key generation, import, delete, attestation
│       ├── CertificateMenu.cs         # Certificate import/export/delete
│       ├── CryptoMenu.cs             # Sign, decrypt, authenticate
│       ├── ConfigMenu.cs             # UIF, algorithm attributes, KDF
│       └── ResetMenu.cs              # Factory reset
└── OpenPgpExamples/
    ├── GetCardStatus.cs
    ├── GetKeyInfo.cs
    ├── VerifyPin.cs
    ├── ChangePin.cs
    ├── GenerateRsaKey.cs
    ├── GenerateEcKey.cs
    ├── SignMessage.cs
    ├── DecryptData.cs
    ├── ImportCertificate.cs
    ├── ExportCertificate.cs
    ├── GetUif.cs
    ├── SetUif.cs
    └── ResetOpenPgp.cs
```

The CLI tool MUST support **command-line parameters** (not just interactive menus) so automated testing can drive it. Examples:
- `OpenPgpTool info` - show card status and key information
- `OpenPgpTool pin verify --pin "123456"` - verify user PIN
- `OpenPgpTool key generate --slot sig --algorithm ec-p256` - generate key
- `OpenPgpTool sign --message "hello" --hash sha256` - sign message
- `OpenPgpTool reset` - factory reset

### Module CLAUDE.md

Create `Yubico.YubiKit.OpenPgp/CLAUDE.md` following the structure of `Yubico.YubiKit.Management/CLAUDE.md`.

## Coding Standards Checklist

Every file MUST:
- [ ] Use file-scoped namespaces (`namespace Yubico.YubiKit.OpenPgp;`)
- [ ] Use `is null` / `is not null` (NEVER `== null`)
- [ ] Use switch expressions (NEVER old switch statements)
- [ ] Use collection expressions `[..]`
- [ ] Use `Span<byte>` with `stackalloc` for sync <=512 bytes
- [ ] Use `ArrayPool<byte>.Shared.Rent()` for sync >512 bytes with try/finally
- [ ] Zero sensitive data with `CryptographicOperations.ZeroMemory()`
- [ ] Use `CryptographicOperations.FixedTimeEquals()` for crypto comparisons
- [ ] Use `readonly` on fields that don't change
- [ ] Use `{ get; init; }` for immutable properties
- [ ] Handle `CancellationToken` in all async methods
- [ ] Use `.ConfigureAwait(false)` on all awaits
- [ ] NO `#region`, NO `.ToArray()` unless data must escape scope
- [ ] Static logger: `LoggingFactory.CreateLogger<T>()` (NEVER inject ILogger)
- [ ] Use partial classes (session WILL exceed 300 lines)

## Anti-Patterns (FORBIDDEN)

- `== null` (use `is null`)
- `#region` (split large classes instead — use partial classes)
- `.ToArray()` in hot paths
- Injected `ILogger` (use static `LoggingFactory`)
- `dotnet test` (use `dotnet toolchain.cs test`)
- `git add .` or `git add -A`
- Old switch statements
- Exceptions for control flow
- Nullable warnings suppressed with `!` without justification
- Putting the entire session in one file (MUST use partial classes)

## Git

- Branch: `yubikit-openpgp` (already created for you)
- Commit messages: `feat(openpgp): description` / `test(openpgp): description`
- NEVER use `git add .` or `git add -A` - add files explicitly

## Build & Test

```bash
dotnet toolchain.cs build    # Must succeed with zero warnings
dotnet toolchain.cs test     # Must pass all unit tests
dotnet toolchain.cs test --filter "Category!=RequiresUserPresence"  # For integration tests
dotnet format            # Must produce no changes
```

## Definition of Done

1. All source files follow patterns from Management/SecurityDomain exactly
2. Partial classes used for session (7 files minimum)
3. `dotnet toolchain.cs build` succeeds with zero warnings
4. `dotnet toolchain.cs test` passes all unit tests
5. Integration tests pass with physical YubiKey (skip user-presence tests)
6. CLI tool runs and demonstrates all OpenPGP operations with command-line parameters
7. `Yubico.YubiKit.OpenPgp/CLAUDE.md` exists with comprehensive module documentation
8. Code looks like it was written by the same developer who wrote Management/SecurityDomain
9. All sensitive data (PINs, private keys, KDF outputs) properly zeroed
10. KDF implementation matches Python exactly (iterated-salted-S2K, NOT PBKDF2)
11. All BER-TLV parsing correct (verified by unit tests with known byte sequences)
12. No anti-patterns present

# Integration Test Gap Coverage — Execution Plan

## Status: Ready to Write Code (Wave 1 — Critical+High)

Gap analysis complete. 4 reconnaissance agents confirmed API surface and test patterns. Now writing actual test files.

## Context

Comprehensive comparison of integration test coverage across three YubiKey SDKs identified 75 gaps in .NET relative to Java/Android and Python. This plan covers writing the test code to close those gaps.

**SDKs compared:**
- .NET: `/Users/Dennis.Dyall/Code/y/Yubico.NET.SDK/` (~200+ integration test methods)
- Java: `/Users/Dennis.Dyall/Code/y/yubikit-android/` (~115+ instrumented test methods)
- Python: `/Users/Dennis.Dyall/Code/y/yubikey-manager/` (~347 device + 234 CLI test methods)

---

## Executive Summary

The .NET SDK has **strong coverage** in: core PIV crypto (sign, decrypt, ECDH, key gen), FIDO2 fundamentals (GetInfo, MakeCredential, GetAssertion, credential management, core extensions), SecurityDomain SCP03/SCP11b, and YubiHSM credential lifecycle.

**52 distinct gaps** were identified. The most critical are:
- **Cross-applet PIN blocking** (MPE tests) — Java tests this, .NET doesn't
- **OpenPGP decryption** — completely untested in .NET
- **Key import** across PIV and OpenPGP — tested by both Java and Python
- **FIDO2 enterprise attestation and CTAP2 Config** — Java has comprehensive coverage

---

## Module-by-Module Gap Analysis

### 1. PIV — 7 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| PIV-1 | RSA key import (all sizes) | Java, Python | **Critical** | .NET imports ECC P-256 only; RSA import completely untested |
| PIV-2 | Ed25519/X25519 key import | Python | **High** | Modern curve import support |
| PIV-3 | Compressed certificate handling | Java, Python | **High** | Real-world RSA cert chains need compression |
| PIV-4 | PIN complexity validation | Java, Python | **High** | FIPS deployments require this |
| PIV-5 | Biometric PIV positive tests | Java, Python | **Medium** | Only negative test exists in .NET |
| PIV-6 | CSR generation | Python | **Medium** | Common enterprise workflow |
| PIV-7 | PIN policy variation tests (Always, Never) | Python | **Medium** | Only PinOnce tested |

**What .NET already covers well:** Key generation (all types), ECDSA/RSA signing, ECDH, RSA decrypt, PIN/PUK lifecycle, management key 3DES/AES, certificates, metadata, reset, full workflows.

---

### 2. FIDO2 — 14 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| FIDO-1 | Enterprise attestation (platform-managed, vendor-facilitated) | Java | **Critical** | Enterprise deployment requirement |
| FIDO-2 | CTAP2 Config (AlwaysUV toggle, force PIN change, min PIN length) | Java | **Critical** | Security policy enforcement |
| FIDO-3 | Bio enrollment (fingerprint, UV blocked fallback) | Java | **High** | Growing biometric deployment |
| FIDO-4 | credBlob extension | Java | **High** | Credential-attached data for offline scenarios |
| FIDO-5 | largeBlobKey / largeBlob extension | Java | **High** | Certificate storage with credentials |
| FIDO-6 | PRF extension | Java | **High** | Key derivation; password manager use case |
| FIDO-7 | UV discouraged mode | Java | **Medium** | Server-driven UV policy testing |
| FIDO-8 | FIDO over CCID transport switching | Java | **Medium** | Multi-transport resilience |
| FIDO-9 | PIN change (not just set) | Java | **Medium** | PIN lifecycle |
| FIDO-10 | credProps extension | Java | **Medium** | Credential properties feedback |
| FIDO-11 | thirdPartyPayment extension | Java | **Low** | Niche payment use case |
| FIDO-12 | sign extension | Java | **Low** | Extension signing verification |
| FIDO-13 | User info updates in credential management | Java | **Medium** | Update displayName for stored credentials |
| FIDO-14 | Multiple users per RP enumeration | Java | **Medium** | Multi-account scenario |

**What .NET already covers well:** GetInfo (comprehensive), MakeCredential/GetAssertion, credential management CRUD, credProtect, hmac-secret, minPinLength, encrypted metadata, PIN protocols V1/V2, NFC, FIPS, algorithm support.

---

### 3. OATH — 8 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| OATH-1 | SHA-256 hash algorithm credential | Python | **High** | Common modern deployment |
| OATH-2 | SHA-512 hash algorithm credential | Python | **High** | High-security deployments |
| OATH-3 | Non-default TOTP periods (60s) | Python | **Medium** | Some services use non-standard periods |
| OATH-4 | 8-digit TOTP codes | Python | **Medium** | Enterprise systems |
| OATH-5 | Password change (vs set/unset) | Java | **Medium** | Password rotation workflow |
| OATH-6 | PSKC file import | Python | **Low** | Bulk credential provisioning |
| OATH-7 | OTPAuth URI parsing integration | Python | **Low** | Client-side convenience |
| OATH-8 | RFC test vector validation | Python | **Low** | Compliance (could be unit tests) |

**What .NET already covers well:** Credential CRUD, TOTP 6-digit, HOTP counter, CalculateAll, password set/validate/unset, rename, reset, touch-required.

---

### 4. OpenPGP — 12 Gaps (Most Undercovered Module)

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| OPG-1 | RSA key import | Java, Python | **Critical** | Core operation for key migration |
| OPG-2 | EC key import (ECDSA P256/P384) | Java, Python | **Critical** | Core operation |
| OPG-3 | Ed25519 key import | Java, Python | **High** | Modern signing key import |
| OPG-4 | X25519 key import | Java, Python | **High** | Modern encryption key import |
| OPG-5 | X25519 key generation | Java, Python | **High** | Decryption key generation |
| OPG-6 | Decryption operations (RSA, ECDH) | Python | **Critical** | Fundamental crypto op completely untested |
| OPG-7 | KDF setup (iterated-salted-S2K) | Java, Python | **High** | Security hardening feature |
| OPG-8 | PIN reset via reset code | Java | **Medium** | PIN recovery mechanism |
| OPG-9 | PIN unverification | Java | **Medium** | Session cleanup |
| OPG-10 | Signature PIN policy testing | Java | **Medium** | Policy enforcement |
| OPG-11 | Admin requirement validation | Java | **Medium** | Authorization boundary |
| OPG-12 | PIN complexity validation | Java | **Medium** | Enhanced security PIN |

**What .NET already covers well:** Reset, app data retrieval, PIN/Admin verify/change, PIN status, algorithm attributes/info, EC P-256 keygen, RSA 2048 keygen, EC/RSA signing, authentication signature, attestation, certificates, key deletion, UIF, fingerprints, generation times, signature counter, get challenge, KDF read.

---

### 5. YubiOTP — 4 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| OTP-1 | Static password configuration | Python | **Medium** | Common enterprise pattern |
| OTP-2 | Touch-triggered OTP mode | Python | **Medium** | Physical presence verification |
| OTP-3 | YubiOTP slot configuration | Python | **Low** | Classic OTP (declining usage) |
| OTP-4 | HOTP slot configuration | Python | **Low** | Counter-based OTP in slot |

**What .NET already covers well:** Serial number, config state, HMAC-SHA1 programming/challenge-response, slot deletion, slot swap, NDEF.

---

### 6. Management — 3 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| MGMT-1 | NFC restricted mode | Java | **High** | Enterprise NFC policy |
| MGMT-2 | Device reset (5.6.0+) | — | **Medium** | Factory reset capability |
| MGMT-3 | Capability enable/disable verification | — | **Medium** | Capability toggling round-trip |

**What .NET already covers well:** Multi-transport session creation, device info, SCP03 auth, wrong keys, device config, form factor filtering, FIPS checks.

---

### 7. SecurityDomain — 5 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| SD-1 | SCP03 key delete | Java | **High** | Key lifecycle management |
| SD-2 | SCP03 key replace (rotate) | Java | **High** | Critical for long-lived deployments |
| SD-3 | SCP11c authentication | Java, Python | **High** | Complete SCP11 variant coverage |
| SD-4 | SCP11a blocked key (negative) | Java | **Medium** | Security boundary verification |
| SD-5 | SCP11b wrong public key (negative) | Java | **Medium** | Security boundary verification |

**What .NET already covers well:** SCP03 session/key info/reset/import, SCP11b (gen/import/auth/certs), SCP11a (auth/allowlist), card recognition data, CA identifiers, DI integration.

---

### 8. YubiHSM Auth — 3 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| HSM-1 | Put asymmetric credential (EC import) | Python | **High** | Import external EC keys |
| HSM-2 | Calculate session keys (asymmetric) | Python | **High** | EC-based auth flow |
| HSM-3 | Get challenge | Python | **Medium** | Challenge generation |

**What .NET already covers well:** Reset/list, symmetric credentials, derived credentials, management key, retries, symmetric session keys, asymmetric key generation, password change, touch policy.

---

### 9. Cross-Module / Multi-Protocol Environment — 3 Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| XM-1 | PIV PIN blocks FIDO reset | Java | **Critical** | Multi-protocol security boundary |
| XM-2 | FIDO PIN blocks PIV reset | Java | **Critical** | Multi-protocol security boundary |
| XM-3 | APDU size tests with/without SCP | Java | **Medium** | Transport reliability under encryption |

**This entire category is missing from the .NET SDK.**

---

## Priority Summary

| Priority | Count | Key Items |
|----------|-------|-----------|
| **Critical** | 8 | Cross-applet PIN blocking (XM-1/2), OpenPGP decrypt (OPG-6), key import (PIV-1, OPG-1/2), FIDO2 enterprise attestation (FIDO-1), CTAP2 config (FIDO-2) |
| **High** | 19 | Modern curve imports, compressed certs, PIN complexity, bio enrollment, FIDO2 extensions (credBlob/largeBlob/PRF), OATH hash variants, SCP key lifecycle, HSM asymmetric ops |
| **Medium** | 19 | Policy variants, transport switching, PIN management edge cases, slot configs |
| **Low** | 6 | Niche FIDO2 extensions, classic OTP, PSKC import |

---

## Recommended Implementation Order

### Phase 1 — Critical (8 gaps, highest security impact)
1. **XM-1, XM-2**: New `MultiProtocolEnvironmentTests` class — cross-applet PIN blocking
2. **OPG-6**: OpenPGP decryption (RSA PKCS#1, ECDH)
3. **OPG-1, OPG-2**: OpenPGP key import (RSA, EC)
4. **PIV-1**: PIV RSA key import
5. **FIDO-1**: Enterprise attestation
6. **FIDO-2**: CTAP2 Config operations

### Phase 2 — High (19 gaps, core feature completeness)
1. FIDO2 extensions: credBlob, largeBlob, PRF, bio enrollment
2. OpenPGP: Ed25519/X25519 import, X25519 gen, KDF setup
3. PIV: Ed25519/X25519 import, compressed certs, PIN complexity
4. OATH: SHA-256/SHA-512 algorithm variants
5. SecurityDomain: SCP03 key delete/replace, SCP11c
6. YubiHSM: Asymmetric credential import, session keys
7. Management: NFC restricted mode

### Phase 3 — Medium (19 gaps)
Policy variants, negative tests, edge cases across all modules.

### Phase 4 — Low (6 gaps)
OTP slot configurations, OATH URI/PSKC, niche FIDO2 extensions.

---

## Additional Gaps (Deep Dive — Second Pass)

The first pass identified 52 gaps. This second pass found **23 additional gaps** by reading the actual test implementations and helper files in all three SDKs.

### 10. PIV — Additional Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| PIV-8 | Touch policy testing (TouchAlways, TouchCached, TouchNever) | Python | **High** | Entirely untested in .NET; controls physical presence requirement |
| PIV-9 | RSA-PSS padding variants (SHA1/224/256/384/512 with MGF1, salt lengths 0/8) | Java | **High** | Java tests ~10 PSS variants; .NET only tests PKCS#1v1.5 and OAEP |
| PIV-10 | Multiple OAEP padding variants (SHA-1 vs SHA-256 MGF1) | Java | **Medium** | Java tests both; .NET tests OAEP-SHA256 only |
| PIV-11 | RSA key import (all sizes: 1024/2048/3072/4096) | Java, Python | **High** | .NET only imports ECC P-256; Java/Python import all RSA sizes |
| PIV-12 | Signing with all hash algorithms (MD5/SHA1/SHA224/SHA256/SHA384/SHA512) | Java | **Medium** | Java tests full matrix; .NET uses default hash only |
| PIV-13 | Certificate import with key verification (cert-key pairing check) | Python | **Medium** | Python CLI verifies cert matches slot key on import |
| PIV-14 | Cross-key-type slot overwriting (RSA→ECC, ECC→RSA in same slot) | Python | **Medium** | Python tests certificate pairing across algorithm changes |
| PIV-15 | Management key protected by PIN (stored key) | Python | **Medium** | PIN-protected management key workflow |
| PIV-16 | Set PIN retries (custom attempt limits) | Python | **Medium** | .NET has PUK test but not full retry configuration round-trip |

### 11. FIDO2 — Additional Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| FIDO-15 | CBOR command cancellation with delay (500ms) | Java | **Low** | Java tests both immediate and delayed cancellation |
| FIDO-16 | Exclude list stress test (17+ credentials) | Java | **Medium** | Java tests large exclude lists; .NET tests basic exclude |
| FIDO-17 | Read-only credential management (PPUAT with limited permissions) | Java | **Medium** | Tests credential enumeration with read-only token |

### 12. OATH — Additional Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| OATH-9 | Locked state blocks all operations (list/calculate/delete/rename) | Python | **High** | Python tests 5 operations blocked when locked; .NET only tests set/validate/unset |
| OATH-10 | FIPS mode blocks password removal | Java | **Medium** | Java tests CONDITIONS_NOT_SATISFIED on FIPS when removing password |

### 13. Management — Additional Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| MGMT-4 | Per-capability USB/NFC toggling (OTP, U2F, OPENPGP, PIV, OATH, FIDO2) | Python | **High** | Python tests enabling/disabling each capability individually |
| MGMT-5 | Lock code management (set, clear, validate) | Python | **Medium** | Device-level lock code for configuration protection |
| MGMT-6 | Mode switching (CCID, OTP, FIDO modes) | Python | **Medium** | Transport mode configuration |

### 14. OpenPGP — Additional Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| OPG-13 | RSA 3072/4096 key generation | Java, Python | **High** | .NET only generates RSA 2048 |
| OPG-14 | Multiple EC curves (P-384, P-521, SECP256K1, Brainpool) | Java | **Medium** | Java tests 8+ curves; .NET only tests P-256 |
| OPG-15 | PIN attempt limit configuration (SetPinAttempts) | Java, Python | **Medium** | Configure user/reset/admin PIN retry limits |

### 15. Cross-Cutting — Additional Gaps

| ID | Gap | Tested By | Priority | Notes |
|----|-----|-----------|----------|-------|
| XM-4 | SCP wrapping of all applet operations (systematic dual-run) | Java | **High** | Java runs every applet test both plain AND with SCP11b; .NET doesn't |
| XM-5 | Interface switching stress test (FIDO↔OTP↔CCID rapid switching) | Python | **Medium** | Tests transport stability under rapid reconnection |

---

## Revised Priority Summary (All 75 Gaps)

| Priority | Count | Key Items |
|----------|-------|-----------|
| **Critical** | 8 | Cross-applet PIN blocking (XM-1/2), OpenPGP decrypt (OPG-6), key import (PIV-1, OPG-1/2), FIDO2 enterprise attestation (FIDO-1), CTAP2 config (FIDO-2) |
| **High** | 30 | All first-pass High items + touch policy (PIV-8), RSA-PSS variants (PIV-9), RSA import all sizes (PIV-11), locked-state blocking (OATH-9), per-capability toggling (MGMT-4), RSA 3072/4096 keygen (OPG-13), SCP dual-run testing (XM-4) |
| **Medium** | 28 | All first-pass Medium items + OAEP variants (PIV-10), hash algorithm matrix (PIV-12), cert-key verification (PIV-13), cross-type overwriting (PIV-14), protected mgmt key (PIV-15), PIN retries (PIV-16), exclude list stress (FIDO-16), read-only cred mgmt (FIDO-17), FIPS password removal (OATH-10), lock code (MGMT-5), mode switching (MGMT-6), multi-curve OpenPGP (OPG-14), PIN attempts (OPG-15), interface switching (XM-5) |
| **Low** | 9 | Niche FIDO2 extensions, classic OTP, PSKC import, CBOR cancellation delay (FIDO-15) |

---

## Key Observations

1. **OpenPGP is the most undercovered module** — 15 gaps including 3 Critical. Key import, decryption, and larger RSA keygen are fundamental operations completely missing from .NET tests.

2. **Cross-applet and cross-cutting testing doesn't exist in .NET** — Java's MPE tests verify that setting a PIN on one applet correctly blocks reset of another. Java also systematically runs every applet test with and without SCP11b, effectively doubling coverage. This is a structural gap in the .NET test strategy.

3. **PIV has deep algorithm coverage gaps** — While basic operations work, the .NET SDK doesn't test RSA-PSS padding (Java tests ~10 variants), doesn't test touch policies, and only imports ECC P-256 keys (not RSA). Java tests a full matrix of algorithms × padding × hash combinations.

4. **FIDO2 has the most gaps by count (17)** but many are extension-level features. The core FIDO2 flow is well-tested. The critical gaps are enterprise attestation and CTAP2 Config.

5. **Python has the most comprehensive OATH tests** — RFC test vectors, multiple hash algorithms, locked-state enforcement, PSKC import. The .NET OATH tests only cover the basic SHA-1/6-digit/30s path.

6. **Management module is superficially tested in all SDKs** — But Python uniquely tests per-capability toggling and lock codes, which are important for enterprise deployments.

7. **The .NET SDK has unique strengths not found in others**: FIDO2 FIPS compliance tests, comprehensive FIDO2 NFC tests, FIDO2 encrypted metadata (5.7+/5.8+), YubiHSM credential password change (5.8.0+), SecurityDomain DI integration tests, Management multi-transport consistency checks, and PIV full workflow tests (generate→sign→verify→ECDH→move→attest).

---

## Execution Plan — Concrete Files to Create

### Agent Reconnaissance Results

4 agents explored the actual API surfaces and confirmed method signatures. Key findings:
- **FIDO2**: The worktree branch has old SDK layout under `Yubico.YubiKey/`. New SDK has `src/Fido2/` with `ExtensionBuilder`, `AuthenticatorConfig`, `FingerprintBioEnrollment`, `LargeBlobStorage` APIs
- **PIV**: `RSAPrivateKey.CreateFromPkcs8()`, `ECPrivateKey.CreateFromPkcs8()`, `Curve25519PrivateKey.CreateFromPkcs8()` available for imports. `GenerateKeyAsync` accepts `PivPinPolicy` and `PivTouchPolicy` params. `StoreCertificateAsync` has `compress: true`.
- **OpenPGP**: `RsaCrtKeyTemplate` (CRT format), `EcKeyTemplate` for imports. `DecryptAsync` exists. `KdfIterSaltedS2k` with `DoProcess()` for KDF setup. `RsaSize.Rsa3072`/`Rsa4096` available.
- **YubiHSM**: `PutCredentialAsymmetricAsync`, `CalculateSessionKeysAsymmetricAsync`, `GetChallengeAsync` confirmed.
- **SecurityDomain**: `DeleteKeyAsync` exists. `PutKeyAsync` with `replaceKvn` for rotation. `ScpKid.SCP11c` for SCP11c.
- **Management**: Capability toggling via `SetDeviceConfigAsync`. Cross-module tests NOT compilable (missing project references).

### Wave 1 Files (Critical + High — 4 parallel agents)

#### Agent 1: PIV + OATH
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/Piv/tests/.../PivImportTests.cs` | 4 | PIV-1, PIV-2, PIV-11 |
| `src/Piv/tests/.../PivPolicyTests.cs` | 2 | PIV-7, PIV-8 |
| `src/Piv/tests/.../PivCompressedCertTests.cs` | 1 | PIV-3 |
| `src/Oath/tests/.../OathHashAlgorithmTests.cs` | 6 | OATH-1, OATH-2, OATH-3, OATH-4, OATH-9 |

#### Agent 2: FIDO2
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/Fido2/tests/.../FidoCredBlobTests.cs` | 2 | FIDO-4 |
| `src/Fido2/tests/.../FidoLargeBlobTests.cs` | 2 | FIDO-5 |
| `src/Fido2/tests/.../FidoPrfTests.cs` | 2 | FIDO-6 |
| `src/Fido2/tests/.../FidoEnterpriseAttestationTests.cs` | 1 | FIDO-1 |
| `src/Fido2/tests/.../FidoAuthenticatorConfigTests.cs` | 3 | FIDO-2 |
| `src/Fido2/tests/.../FidoBioEnrollmentTests.cs` | 1 | FIDO-3 |
| `src/Fido2/tests/.../FidoCredentialManagementExtendedTests.cs` | 2 | FIDO-13, FIDO-14 |

#### Agent 3: OpenPGP + YubiHSM
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/OpenPgp/tests/.../OpenPgpKeyImportTests.cs` | 6 | OPG-1 through OPG-5 |
| `src/OpenPgp/tests/.../OpenPgpDecryptTests.cs` | 2 | OPG-6 |
| `src/OpenPgp/tests/.../OpenPgpAdvancedTests.cs` | 4 | OPG-5, OPG-7, OPG-13 |
| `src/YubiHsm/tests/.../HsmAuthAsymmetricTests.cs` | 4 | HSM-1, HSM-2, HSM-3 |

#### Agent 4: SecurityDomain + Management
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/SecurityDomain/tests/.../SecurityDomainSession_Scp03KeyLifecycleTests.cs` | 2 | SD-1, SD-2 |
| `src/SecurityDomain/tests/.../SecurityDomainSession_Scp11cTests.cs` | 1 | SD-3 |
| `src/SecurityDomain/tests/.../SecurityDomainSession_NegativeTests.cs` | 2 | SD-4, SD-5 |
| `src/Management/tests/.../ManagementSessionCapabilityTests.cs` | 2 | MGMT-1, MGMT-4 |

**Wave 1 Total: 17 new files, ~42 test methods**

### Wave 2 Files (Medium + Low — 4 parallel agents, after Wave 1)

#### Agent 5: PIV Medium gaps
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/Piv/tests/.../PivSigningAlgorithmTests.cs` | 6 | PIV-9, PIV-12 |
| `src/Piv/tests/.../PivSlotOverwriteTests.cs` | 2 | PIV-14 |
| `src/Piv/tests/.../PivPinRetryTests.cs` | 2 | PIV-5, PIV-6, PIV-15, PIV-16 |

#### Agent 6: FIDO2 Medium gaps
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/Fido2/tests/.../FidoPinManagementTests.cs` | 2 | FIDO-7, FIDO-9 |
| `src/Fido2/tests/.../FidoTransportTests.cs` | 2 | FIDO-8, FIDO-10 |
| `src/Fido2/tests/.../FidoExcludeListStressTests.cs` | 1 | FIDO-16 |

#### Agent 7: OpenPGP Medium gaps
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/OpenPgp/tests/.../OpenPgpPinManagementTests.cs` | 5 | OPG-8 through OPG-12 |
| `src/OpenPgp/tests/.../OpenPgpMultiCurveTests.cs` | 3 | OPG-14 |

#### Agent 8: OTP + Management + OATH Low gaps
| File | Tests | Gaps Covered |
|------|-------|-------------|
| `src/YubiOtp/tests/.../YubiOtpSlotConfigTests.cs` | 4 | OTP-1 through OTP-4 |
| `src/Management/tests/.../ManagementLockCodeTests.cs` | 2 | MGMT-5, MGMT-6 |
| `src/Oath/tests/.../OathPasswordChangeTests.cs` | 2 | OATH-5 |

**Wave 2 Total: ~10 new files, ~31 test methods**

### Post-Write Workflow

1. **Build verification**: `dotnet build.cs build` — all 27 files must compile
2. **Merge**: If worktrees used, merge branches. Otherwise already on `yubikit-applets`
3. **Sequential testing** (one module at a time, one test at a time):
   - PIV → OATH → OpenPGP → YubiHSM → SecurityDomain → Management → FIDO2
   - Command: `dotnet build.cs -- test --integration --project {Module} --filter "FullyQualifiedName~{TestName}"`
   - Fix failures between runs
4. **Code review**: Each module's new tests reviewed for patterns, security, cleanup

### Cross-Module Tests (Deferred)

MPE tests (XM-1, XM-2) require a new cross-module test project with references to PIV, FIDO2, and Management. This is a separate task that needs project file creation and solution modification. Not included in this wave.

### Verification

After all tests pass:
- `dotnet build.cs build` — clean build
- `dotnet build.cs test` — unit tests pass
- Integration tests pass per-module with `--filter` targeting new tests only
- `dotnet format` — code style compliance

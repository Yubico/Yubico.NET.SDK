# PivSession Port Implementation Progress

**Started:** 2026-01-18
**PRD:** `docs/specs/piv-session-port/prd.md`
**Plan Reference:** `docs/plans/ralph-loop/2026-01-18-piv-session-port.md`
**Status:** In Progress

## Workflow Instructions (For Autonomous Agent)

**Role:** You are an autonomous .NET engineer.
**Source of Truth:** THIS FILE. It contains your tasks, priorities, and rules.

### The Loop
1. **Read this file** to understand the current state.
2. **Select Phase:** Find the highest priority incomplete Phase (P0 > P1 > P2).
   - *Rule:* Finish the current phase completely before moving to the next.
   - *Rule:* Within a phase, complete "Core Implementation" before "Error Handling" before "Edge Cases".
3. **Execute Task:** Pick the next unchecked item `[ ]`.
   - **Step 1: Write Failing Test (RED)**
     - Create/Update the test file listed in the Phase.
     - Write a test that asserts the specific criteria of the task.
     - Run: `dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"` -> Expect FAILURE.
   - **Step 2: Implement (GREEN)**
     - Write minimal code in the implementation file.
     - Follow patterns from `dx_audit.md` and `security_audit.md`.
     - Run: `dotnet build.cs test --filter "FullyQualifiedName~{TestClass}"` -> Expect SUCCESS.
   - **Step 3: Refactor & Secure**
     - Check: Is sensitive data zeroed? (See Security Protocol below).
     - Check: Are public APIs documented?
   - **Step 4: Commit**
     - `git add {specific files only}`
     - `git commit -m "feat(piv): {task description}"`
4. **Update Status:** Change `[ ]` to `[x]` in this file and add notes.
5. **Loop:** Repeat.

### Security Protocol (MUST FOLLOW)
- **ZeroMemory:** Always zero sensitive data (PINs, PUKs, Management Keys) using `CryptographicOperations.ZeroMemory`.
- **No Logs:** Never log sensitive values.
- **Validation:** Validate all input lengths and ranges.
- **Temporary PIN:** Document that callers must zero temporary PINs from `VerifyUvAsync`.

### Guidelines & Anti-Patterns
- **❌ Skipping RED verification:** Tests must fail first to prove they test something.
- **✅ Always verify RED:** Run tests before implementation.
- **❌ One giant phase:** Do not tackle all stories in a single phase.
- **✅ Vertical Slices:** Implement one story/feature completely (including errors) before moving on.
- **❌ Missing security:** Forgetting requirements from `security_audit.md`.
- **✅ Explicit Verification:** Build + Test + Security must pass before marking `[x]`.
- **❌ Using `dotnet test`:** This will fail on mixed xUnit v2/v3 projects.
- **✅ Use `dotnet build.cs test`:** Always use the build script.

---

## Phases

### Phase 1: Foundation Types (P0)

**Goal:** Create PIV type definitions for strongly-typed PIV concepts.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSlot.cs`, `PivAlgorithm.cs`, `PivPinPolicy.cs`, `PivTouchPolicy.cs`, `PivManagementKeyType.cs`, `PivDataObject.cs`, `PivMetadata.cs`, `PivFeatures.cs`, `IPivSession.cs`
- Tests: `Yubico.YubiKit.Piv/tests/UnitTests/PivTypesTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 1.1: Create `PivSlot` enum (Auth 0x9A, Sign 0x9C, KeyMgmt 0x9D, CardAuth 0x9E, Retired1-20, Attestation 0xF9)
- [ ] 1.2: Create `PivAlgorithm` enum (RSA 1024/2048/3072/4096, ECC P-256/P-384, Ed25519, X25519)
- [ ] 1.3: Create `PivPinPolicy` enum (Default, Never, Once, Always, MatchOnce, MatchAlways)
- [ ] 1.4: Create `PivTouchPolicy` enum (Default, Never, Always, Cached)
- [ ] 1.5: Create `PivManagementKeyType` enum (TripleDes 0x03, Aes128 0x08, Aes192 0x0A, Aes256 0x0C)
- [ ] 1.6: Create `PivDataObject` static class with all object ID constants
- [ ] 1.7: Create metadata records (PivPinMetadata, PivManagementKeyMetadata, PivSlotMetadata, PivBioMetadata)
- [ ] 1.8: Create `PivFeatures` static class with all Feature instances + ROCA check
- [ ] 1.9: Create `IPivSession` interface per PRD Interface Definition
- [ ] 1.10: Add unit tests for all type values

---

### Phase 2: Session Core (P0)

**Goal:** Create PIV session with YubiKey, supporting CreateAsync factory pattern.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.cs`, `Yubico.YubiKit.Piv/src/IYubiKeyExtensions.cs`
- Tests: `Yubico.YubiKit.Piv/tests/UnitTests/PivSessionTests.cs`, `tests/IntegrationTests/PivSessionIntegrationTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 2.1: Create `PivSession.cs` inheriting `ApplicationSession`, implementing `IAsyncDisposable`
- [ ] 2.2: Implement static `CreateAsync()` factory (SELECT PIV AID, GET VERSION)
- [ ] 2.3: Implement `GetSerialNumberAsync()` (INS 0xF8, requires 5.0+)
- [ ] 2.4: Create `IYubiKeyExtensions.cs` with `CreatePivSessionAsync()` extension

#### Error Handling (PRD §3.2)
- [ ] 2.5: Handle PIV application not available -> throw `NotSupportedException`

---

### Phase 3: Authentication (P0)

**Goal:** Authenticate with management key and verify PIN for privileged operations.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.Authentication.cs`, `InvalidPinException.cs`
- Tests: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivAuthenticationTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 3.1: Implement `AuthenticateAsync()` with 3DES mutual authentication
- [ ] 3.2: Implement `AuthenticateAsync()` with AES-128/192/256 (requires 5.4+)
- [ ] 3.3: Implement `VerifyPinAsync()` (INS 0x20, pad to 8 bytes with 0xFF)
- [ ] 3.4: Implement `GetPinAttemptsAsync()` (metadata on 5.3+ or empty verify fallback)
- [ ] 3.5: Implement `ChangePinAsync()`, `ChangePukAsync()` (INS 0x24)
- [ ] 3.6: Implement `UnblockPinAsync()` (INS 0x2C)
- [ ] 3.7: Implement `SetPinAttemptsAsync()` (INS 0xFA)

#### Error Handling (PRD §3.2)
- [ ] 3.8: Create `InvalidPinException` with `RetriesRemaining` property
- [ ] 3.9: Handle SW 0x63Cx -> throw `InvalidPinException(x)`
- [ ] 3.10: Handle SW 0x6983 -> throw `InvalidPinException(0)` (blocked)
- [ ] 3.11: Handle mgmt key auth failure -> throw `BadResponseException`

#### Security
- [ ] 3.12: Zero PIN buffer in finally block
- [ ] 3.13: Zero management key buffer in finally block

---

### Phase 4: Key Operations (P0)

**Goal:** Generate, import, move, delete, and attest private keys.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.KeyPairs.cs`
- Tests: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivKeyOperationsTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 4.1: Implement `GenerateKeyAsync()` for ECC P-256/P-384 (INS 0x47)
- [ ] 4.2: Implement `GenerateKeyAsync()` for RSA 1024/2048 (with ROCA check)
- [ ] 4.3: Implement `GenerateKeyAsync()` for RSA 3072/4096 (requires 5.7+)
- [ ] 4.4: Implement `GenerateKeyAsync()` for Ed25519/X25519 (requires 5.7+)
- [ ] 4.5: Implement `ImportKeyAsync()` for all key types (INS 0xFE)
- [ ] 4.6: Implement `MoveKeyAsync()` (INS 0xF6, requires 5.7+)
- [ ] 4.7: Implement `DeleteKeyAsync()` (INS 0xF6 with dest=0xFF, requires 5.7+)
- [ ] 4.8: Implement `AttestKeyAsync()` (INS 0xF9, requires 4.3+)

#### Error Handling (PRD §3.2)
- [ ] 4.9: Block RSA gen on 4.2.6-4.3.4 (ROCA) -> throw `NotSupportedException`
- [ ] 4.10: Feature gate check for P384, Cv25519, RSA3072/4096

#### Security
- [ ] 4.11: Zero private key after import

---

### Phase 5: Cryptographic Operations (P0)

**Goal:** Sign/decrypt data and perform ECDH key agreement.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.Crypto.cs`
- Tests: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivCryptoTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 5.1: Implement `SignOrDecryptAsync()` for RSA (INS 0x87, TAG 0x82+0x81)
- [ ] 5.2: Implement `SignOrDecryptAsync()` for ECC (DER-encoded signature)
- [ ] 5.3: Implement `SignOrDecryptAsync()` for Ed25519
- [ ] 5.4: Implement `CalculateSecretAsync()` for ECDH P-256/P-384 (TAG 0x85)
- [ ] 5.5: Implement `CalculateSecretAsync()` for X25519

#### Edge Cases (PRD §3.3)
- [ ] 5.6: Left-pad payload for RSA if shorter than key size
- [ ] 5.7: Truncate payload for ECC if longer than curve order

---

### Phase 6: Certificates & Data Objects (P0)

**Goal:** Store, retrieve, and delete X.509 certificates and PIV data objects.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.Certificates.cs`, `PivSession.DataObjects.cs`
- Tests: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivCertificateTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 6.1: Implement `GetCertificateAsync()` (INS 0xCB, parse TAG 0x70+0x71, decompress if needed)
- [ ] 6.2: Implement `StoreCertificateAsync()` (INS 0xDB, gzip compress if >1856 bytes)
- [ ] 6.3: Implement `DeleteCertificateAsync()` (idempotent, no-op if empty)
- [ ] 6.4: Implement `GetObjectAsync()` (generic GET DATA)
- [ ] 6.5: Implement `PutObjectAsync()` (generic PUT DATA)

#### Edge Cases (PRD §3.3)
- [ ] 6.6: Return null for empty slot (not throw)
- [ ] 6.7: Return `ReadOnlyMemory<byte>.Empty` for missing object

---

### Phase 7: Metadata & Bio (P0)

**Goal:** Retrieve metadata about slots/credentials and support biometric verification.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.Metadata.cs`, `PivSession.Bio.cs`
- Tests: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivMetadataTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 7.1: Implement `GetPinMetadataAsync()` (INS 0xF7, P2=0x80, requires 5.3+)
- [ ] 7.2: Implement `GetPukMetadataAsync()` (INS 0xF7, P2=0x81)
- [ ] 7.3: Implement `GetManagementKeyMetadataAsync()` (INS 0xF7, P2=0x9B)
- [ ] 7.4: Implement `GetSlotMetadataAsync()` (INS 0xF7, P2=slot, return null if empty)
- [ ] 7.5: Implement `SetManagementKeyAsync()` (INS 0xFF)
- [ ] 7.6: Implement `GetBioMetadataAsync()` (INS 0xF7, P2=0x96)
- [ ] 7.7: Implement `VerifyUvAsync()` (INS 0x20, P2=0x96, return temp PIN)
- [ ] 7.8: Implement `VerifyTemporaryPinAsync()` (INS 0x20, P2=0x96, TAG 0x01)

#### Error Handling (PRD §3.2)
- [ ] 7.9: Bio not available -> throw `NotSupportedException`

---

### Phase 8: Reset & Integration (P0)

**Goal:** Reset PIV application and verify all operations work together.
**Files:**
- Src: `Yubico.YubiKit.Piv/src/PivSession.cs` (update)
- Tests: `Yubico.YubiKit.Piv/tests/IntegrationTests/PivResetTests.cs`, `PivFullWorkflowTests.cs`

**Tasks:**
#### Core Implementation
- [ ] 8.1: Implement `ResetAsync()` (block PIN, block PUK, INS 0xFB)
- [ ] 8.2: Create full workflow integration test (generate -> cert -> sign -> verify)

#### Error Handling (PRD §3.2)
- [ ] 8.3: Check bio not configured before reset -> throw if configured

---

### Phase 9: Security Verification (P0)

**Goal:** Verify all security requirements from `security_audit.md`

**Tasks:**
- [ ] S.1: Audit: Verify all PIN/PUK buffers zeroed after use
  ```bash
  grep -rn "ZeroMemory" Yubico.YubiKit.Piv/src/
  ```
- [ ] S.2: Audit: Verify management key buffers zeroed after use
- [ ] S.3: Audit: Verify no secrets in log statements
  ```bash
  grep -rn "Log.*[Pp]in\|Log.*[Kk]ey\|Log.*[Pp]uk" Yubico.YubiKit.Piv/src/
  # Should return nothing suspicious
  ```
- [ ] S.4: Audit: Document temp PIN zeroing requirement in XML docs
- [ ] S.5: Audit: Input validation on all public methods

---

### Phase 10: Documentation (P1)

**Goal:** Update module documentation with usage examples.
**Files:**
- Update: `Yubico.YubiKit.Piv/CLAUDE.md`
- Update: `Yubico.YubiKit.Piv/README.md`

**Tasks:**
- [ ] 10.1: Update CLAUDE.md with migration status
- [ ] 10.2: Create/update README.md with usage examples

---

## Verification Requirements (MUST PASS BEFORE COMPLETION)

1. **Build:** `dotnet build.cs build` (must exit 0)
2. **All Tests:** `dotnet build.cs test` (all tests must pass)
3. **No Regressions:** Existing tests in other modules still pass
4. **Coverage:** New code has test coverage
5. **Security:** All security checks from Phase 9 pass

Only after ALL pass, output `<promise>PIV_SESSION_PORT_COMPLETE</promise>`.
If any fail, fix and re-verify.

---

## Session Notes
* 2026-01-18: Progress file created from plan. Ready to begin Phase 1.

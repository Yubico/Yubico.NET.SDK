# Documentation Audit Plan

**Goal:** Ensure all CLAUDE.md and README.md files accurately describe their modules for developers (README) and AI agents (CLAUDE).

**Scope:** Quick accuracy check, not comprehensive rewrite. Fill gaps where files are missing.

---

## Phase 1: Audit Root Documentation

**Files:**
- `./CLAUDE.md`
- `./README.md`

**Tasks:**
- 1.1: Verify CLAUDE.md reflects current build/test commands and project structure
- 1.2: Verify README.md describes the SDK purpose and getting started

---

## Phase 2: Audit Core Module

**Files:**
- `Yubico.YubiKit.Core/CLAUDE.md` (exists)
- `Yubico.YubiKit.Core/README.md` (missing - create)
- `Yubico.YubiKit.Core/tests/CLAUDE.md` (exists)

**Tasks:**
- 2.1: Verify Core CLAUDE.md describes transport abstractions, IYubiKeyDevice, connection patterns
- 2.2: Create Core README.md with purpose, key classes, usage examples
- 2.3: Verify tests/CLAUDE.md describes test infrastructure

---

## Phase 3: Audit Management Module

**Files:**
- `Yubico.YubiKit.Management/CLAUDE.md` (exists)
- `Yubico.YubiKit.Management/README.md` (exists)
- `Yubico.YubiKit.Management/tests/CLAUDE.md` (exists)

**Tasks:**
- 3.1: Verify all three files are accurate and current

---

## Phase 4: Audit Piv Module

**Files:**
- `Yubico.YubiKit.Piv/CLAUDE.md` (exists)
- `Yubico.YubiKit.Piv/README.md` (exists)
- `Yubico.YubiKit.Piv/tests/CLAUDE.md` (exists)

**Tasks:**
- 4.1: Verify all three files are accurate and current

---

## Phase 5: Audit Fido2 Module

**Files:**
- `Yubico.YubiKit.Fido2/CLAUDE.md` (exists)
- `Yubico.YubiKit.Fido2/README.md` (missing - create)
- `Yubico.YubiKit.Fido2/tests/CLAUDE.md` (exists)

**Tasks:**
- 5.1: Verify Fido2 CLAUDE.md describes FIDO2/WebAuthn patterns
- 5.2: Create Fido2 README.md with purpose, key classes, usage examples
- 5.3: Verify tests/CLAUDE.md describes test infrastructure

---

## Phase 6: Audit SecurityDomain Module

**Files:**
- `Yubico.YubiKit.SecurityDomain/CLAUDE.md` (exists)
- `Yubico.YubiKit.SecurityDomain/README.md` (exists)
- `Yubico.YubiKit.SecurityDomain/tests/CLAUDE.md` (exists)
- `Yubico.YubiKit.SecurityDomain/tests/README.md` (exists)

**Tasks:**
- 6.1: Verify all four files are accurate and current

---

## Phase 7: Audit Oath Module

**Files:**
- `Yubico.YubiKit.Oath/CLAUDE.md` (missing - create)
- `Yubico.YubiKit.Oath/README.md` (missing - create)
- `Yubico.YubiKit.Oath/tests/CLAUDE.md` (exists)

**Tasks:**
- 7.1: Create Oath CLAUDE.md describing TOTP/HOTP patterns
- 7.2: Create Oath README.md with purpose, key classes, usage examples
- 7.3: Verify tests/CLAUDE.md describes test infrastructure

---

## Phase 8: Audit OpenPgp Module

**Files:**
- `Yubico.YubiKit.OpenPgp/CLAUDE.md` (missing - create)
- `Yubico.YubiKit.OpenPgp/README.md` (missing - create)
- `Yubico.YubiKit.OpenPgp/tests/CLAUDE.md` (exists)

**Tasks:**
- 8.1: Create OpenPgp CLAUDE.md describing OpenPGP card patterns
- 8.2: Create OpenPgp README.md with purpose, key classes, usage examples
- 8.3: Verify tests/CLAUDE.md describes test infrastructure

---

## Phase 9: Audit YubiHsm Module

**Files:**
- `Yubico.YubiKit.YubiHsm/CLAUDE.md` (missing - create)
- `Yubico.YubiKit.YubiHsm/README.md` (missing - create)
- `Yubico.YubiKit.YubiHsm/tests/CLAUDE.md` (exists)

**Tasks:**
- 9.1: Create YubiHsm CLAUDE.md describing HSM patterns
- 9.2: Create YubiHsm README.md with purpose, key classes, usage examples
- 9.3: Verify tests/CLAUDE.md describes test infrastructure

---

## Phase 10: Audit YubiOtp Module

**Files:**
- `Yubico.YubiKit.YubiOtp/CLAUDE.md` (missing - create)
- `Yubico.YubiKit.YubiOtp/README.md` (missing - create)
- `Yubico.YubiKit.YubiOtp/tests/CLAUDE.md` (exists)

**Tasks:**
- 10.1: Create YubiOtp CLAUDE.md describing OTP patterns
- 10.2: Create YubiOtp README.md with purpose, key classes, usage examples
- 10.3: Verify tests/CLAUDE.md describes test infrastructure

---

## Phase 11: Audit Tests.Shared Module

**Files:**
- `Yubico.YubiKit.Tests.Shared/CLAUDE.md` (missing - create)
- `Yubico.YubiKit.Tests.Shared/README.md` (exists)

**Tasks:**
- 11.1: Create Tests.Shared CLAUDE.md describing test infrastructure patterns
- 11.2: Verify README.md is current with multi-transport infrastructure

---

## Phase 12: Final Verification

**Tasks:**
- 12.1: Build solution to ensure no broken doc references
- 12.2: Commit all changes with appropriate message

---

## Guidelines for Content

### README.md (for developers)
- Purpose of the module (1-2 sentences)
- Key classes/interfaces
- Basic usage example
- Link to detailed docs if available

### CLAUDE.md (for AI agents)
- Module-specific patterns and conventions
- Common pitfalls to avoid
- Test infrastructure specifics
- Dependencies on other modules

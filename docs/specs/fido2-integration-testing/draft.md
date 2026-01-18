# PRD: FIDO2 FidoSession Integration Testing Enhancement

**Status:** DRAFT
**Author:** spec-writer agent
**Created:** 2026-01-18T01:35:00Z
**Feature Slug:** fido2-integration-testing

---

## 1. Problem Statement

### 1.1 The Problem

The Yubico.NET.SDK's FIDO2 module (`Yubico.YubiKit.Fido2`) has weak integration test coverage (~35%), leaving critical WebAuthn workflows untested against real YubiKey hardware. Developers cannot confidently make changes to FidoSession, credential management, or authentication flows without risking regressions that only manifest on physical devices.

### 1.2 Evidence

| Type | Source | Finding |
|------|--------|---------|
| Quantitative | Code analysis | Only 10 integration tests exist in `FidoSessionSimpleTests.cs`; none cover MakeCredential/GetAssertion |
| Quantitative | Comparison | Java yubikit-android has 50+ FIDO integration tests in `Ctap2ClientTests.java` and `Ctap2SessionTests.java` |
| Qualitative | Test file audit | Existing tests only cover GetInfo operations; no end-to-end credential workflows |
| Qualitative | Code review risk | Changes to CTAP2 encoding/decoding cannot be validated against real device behavior |

### 1.3 Impact of Not Solving

Without comprehensive FIDO2 integration tests:
- Regressions in credential registration (MakeCredential) could ship undetected
- Authentication failures (GetAssertion) may only surface in production
- FIPS-mode specific behaviors remain untested
- New YubiKey firmware features cannot be validated against hardware
- Developers spend excessive time on manual testing with physical keys

---

## 2. User Stories

### Story 1: SDK Developer Validates Credential Registration

**As a** SDK maintainer,
**I want to** run integration tests that exercise MakeCredential workflows on real YubiKeys,
**So that** I can detect regressions in the credential registration pipeline before release.

**Acceptance Criteria:**
- [ ] Test creates non-resident key (non-RK) credential and verifies attestation response
- [ ] Test creates resident key (RK) credential and verifies attestation response contains credentialId
- [ ] Test verifies correct handling of `excludeList` when credential already exists
- [ ] Test runs against YubiKeys with FIDO2 capability (version from ManagementSession, not FIDO AuthenticatorInfo)
- [ ] Test cleans up created credentials after execution

### Story 2: SDK Developer Validates Authentication Flow

**As a** SDK maintainer,
**I want to** run integration tests that exercise GetAssertion workflows on real YubiKeys,
**So that** I can verify the authentication pipeline produces valid signatures.

**Acceptance Criteria:**
- [ ] Test performs GetAssertion on a credential created in test setup
- [ ] Test verifies assertion response contains valid authenticatorData and signature
- [ ] Test handles multiple credentials for same RP (multi-credential scenario)
- [ ] Test verifies userHandle is returned for resident key credentials
- [ ] Test runs only on devices where the test credential was registered

### Story 3: SDK Developer Manages Test Credentials

**As a** SDK maintainer,
**I want to** enumerate, inspect, and delete credentials during integration tests,
**So that** tests can verify credential management APIs and clean up after themselves.

**Acceptance Criteria:**
- [ ] Test enumerates all resident credentials by RP
- [ ] Test updates credential displayName and verifies change
- [ ] Test deletes specific credential and verifies removal
- [ ] Test verifies credential count after operations
- [ ] Test skips credential management on devices that don't support it (pre-2.1)

### Story 4: SDK Developer Tests Algorithm Support

**As a** SDK maintainer,
**I want to** verify credential creation with different cryptographic algorithms,
**So that** I can ensure algorithm negotiation works correctly across YubiKey models.

**Acceptance Criteria:**
- [ ] Test creates credential with ES256 (COSE algorithm -7)
- [ ] Test creates credential with ES384 (COSE algorithm -35) where supported
- [ ] Test creates credential with EdDSA (COSE algorithm -8) on firmware ≥5.2.3
- [ ] Test verifies the attestation response uses the requested algorithm
- [ ] Test gracefully skips unsupported algorithms on older firmware

### Story 5: SDK Developer Tests FIPS Compliance

**As a** SDK maintainer,
**I want to** verify FIDO2 behavior on FIPS-capable YubiKeys,
**So that** I can ensure FIPS-approved operation meets compliance requirements.

**Acceptance Criteria:**
- [ ] Test verifies FIPS devices require PIN/UV AuthProtocol v2
- [ ] Test verifies alwaysUv is enforced on FIPS-approved devices
- [ ] Test runs only on devices with `FipsCapable(DeviceCapabilities.Fido2)`
- [ ] Test reports clear skip message on non-FIPS devices

### Story 6: SDK Developer Tests Extension Support

**As a** SDK maintainer,
**I want to** verify FIDO2 extension handling (hmac-secret, largeBlob),
**So that** I can ensure extensions are properly negotiated and processed.

**Acceptance Criteria:**
- [ ] Test creates credential with hmac-secret extension enabled
- [ ] Test creates credential with largeBlob extension where supported
- [ ] Test verifies extension data is present in registration response
- [ ] Test verifies extension data is accessible during authentication
- [ ] Test gracefully skips unsupported extensions

### Story 7: SDK Developer Tests Enhanced PIN Compliance

**As a** SDK maintainer,
**I want to** verify FIDO2 behavior on YubiKeys with enhanced PIN complexity requirements,
**So that** I can ensure PIN operations work correctly on devices with stricter policies.

**Acceptance Criteria:**
- [ ] Test detects when device has PIN complexity enforcement enabled
- [ ] Test uses a PIN that meets enhanced complexity requirements (e.g., 8+ chars, mixed)
- [ ] Test verifies PIN set/change operations succeed with compliant PIN
- [ ] Test verifies PIN operations fail gracefully with non-compliant PIN
- [ ] Test skips complexity tests on devices without enforcement

---

## 3. Functional Requirements

### 3.1 Test Infrastructure: FidoTestState

A new test state class that integrates with `[WithYubiKey]` attribute infrastructure.

| Step | User Action | System Response |
|------|-------------|-----------------|
| 1 | Apply `[WithYubiKey(Capability = DeviceCapabilities.Fido2)]` to test | Test infrastructure discovers FIDO2-capable devices |
| 2 | Test method receives `YubiKeyTestState` | State includes device info and session factories |
| 3 | Test calls `state.WithFidoSessionAsync(callback)` | Callback receives configured `FidoSession` |
| 4 | Test completes or fails | Session is disposed; connection is released |

**Critical Note on Firmware Version:**
- FIDO2 `AuthenticatorInfo` may report incorrect version (e.g., 0.0.1 on alpha firmware)
- **Always use `ManagementSession` to get accurate firmware version**
- `YubiKeyTestState.FirmwareVersion` comes from ManagementSession, not FIDO

### 3.2 Happy Path: Registration and Authentication

| Step | User Action | System Response |
|------|-------------|-----------------|
| 1 | Test sets up PIN on device (if not set) | PIN configured with compliant test PIN value |
| 2 | Test calls MakeCredential with options | Credential created, attestation returned |
| 3 | Test verifies attestation response | Response contains valid CBOR, AAGUID, credentialId |
| 4 | Test calls GetAssertion with credential | Assertion returned with authenticatorData, signature |
| 5 | Test verifies assertion response | Signature is valid, userHandle matches (for RK) |
| 6 | Test deletes credential | Credential removed from authenticator |

### 3.3 Error States (Unhappy Paths)

| Condition | System Behavior | Error Type |
|-----------|-----------------|------------|
| Device does not support FIDO2 | Test skipped via `Skip.If` | Test Skip (not failure) |
| PIN not set when required | Test sets PIN automatically | N/A (auto-handled) |
| PIN incorrect | Test fails with clear message | `CtapException` with `ERR_PIN_INVALID` |
| PIN does not meet complexity | Test fails with policy violation | `CtapException` with `ERR_PIN_POLICY_VIOLATION` |
| Credential already exists (exclude list) | MakeCredential returns appropriate error | `CtapException` with `ERR_CREDENTIAL_EXCLUDED` |
| No credentials for RP | GetAssertion returns no credentials error | `CtapException` with `ERR_NO_CREDENTIALS` |
| User cancels operation | Operation cancelled cleanly | `CtapException` with `ERR_KEEPALIVE_CANCEL` |
| Device disconnected mid-operation | Connection exception thrown | `IOException` |
| Unsupported algorithm requested | MakeCredential returns unsupported error | `CtapException` with `ERR_UNSUPPORTED_ALGORITHM` |

### 3.4 Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Empty excludeList | MakeCredential proceeds normally |
| Maximum credential count reached | Test documents limit and skips cleanly |
| Zero resident credentials | EnumerateCredentials returns empty list |
| Concurrent test execution | Tests use unique RP IDs to avoid collision |
| USB vs NFC transport | Tests note transport-specific behaviors |
| Bio-enrolled device | Reset tests skip on bioEnroll-capable devices |
| Enhanced PIN device | Tests use complexity-compliant PIN |
| Alpha firmware (v5.8.0-alpha) | Tests rely on ManagementSession version, not FIDO version |

---

## 4. Non-Functional Requirements

### 4.1 Performance

- Individual test timeout: 60 seconds (user presence may be required)
- Total FIDO2 test suite: ≤5 minutes on CI with single device
- No polling loops >100ms without cancellation support

### 4.2 Security

- Test PIN value meets enhanced complexity requirements (8+ chars, mixed case/numbers)
- Tests must clean up all created credentials after completion
- Tests must not leave device in locked/blocked state
- Tests must not expose credential private keys

### 4.3 Compatibility

- Minimum YubiKey firmware: 5.2.0 (FIDO2 baseline) - **version from ManagementSession**
- Feature-specific minimum firmware documented per test
- Tests use `[WithYubiKey(MinFirmware = "X.Y.Z")]` for filtering
- Supported transports: USB HID FIDO, NFC (SmartCard)
- Handles alpha/beta firmware gracefully (e.g., 5.8.0-alpha)

---

## 5. Technical Constraints

### 5.1 Must Use

- Existing `[WithYubiKey]` attribute from `Yubico.YubiKit.Tests.Shared`
- Existing `YubiKeyTestState` and `YubiKeyTestInfrastructure` classes
- xUnit v3 async test patterns (`ValueTask` return types)
- `FidoSession.CreateAsync()` for session creation
- Standard test traits: `[Trait("Category", "Integration")]`
- **ManagementSession for firmware version** (not FIDO AuthenticatorInfo)

### 5.2 Must Not

- Must not use `dotnet test` directly (use `dotnet build.cs test`)
- Must not hard-code serial numbers or device identifiers
- Must not skip credential cleanup (use `try/finally`)
- Must not run destructive tests (Reset) in CI without explicit opt-in
- Must not block on user presence in non-interactive CI mode
- Must not rely on FIDO AuthenticatorInfo for firmware version detection

### 5.3 Dependencies

- `Yubico.YubiKit.Tests.Shared` test infrastructure
- Physical YubiKey with FIDO2 capability (serial in allow list)
- Test runner with USB/NFC access

---

## 6. Out of Scope

- **CTAP1/U2F legacy testing**: This PRD focuses on CTAP2/FIDO2 only
- **WebAuthn high-level client**: Tests target FidoSession directly, not browser-like flows
- **PIN complexity policy testing**: Tests use a compliant PIN, not policy boundary testing
- **Attestation certificate chain validation**: Tests verify structure, not cryptographic validity
- **Biometric enrollment integration**: Covered by separate biometric PRD
- **Large blob read/write tests**: Covered by existing `LargeBlobStorageTests`

---

## 7. Open Questions

- [ ] Should destructive tests (Reset) be in a separate test class with explicit opt-in?
- [x] What is the acceptable test PIN value for shared test utilities? → Use `"Abc12345"` (8+ chars, meets enhanced complexity)
- [ ] Should tests support headless CI mode that auto-approves user presence?
- [ ] How should tests handle multi-device scenarios (multiple keys connected)?

---

## 8. Test Data Specification

### 8.1 Shared Test Constants (FidoTestData class)

| Constant | Value | Purpose |
|----------|-------|---------|
| `RpId` | `"localhost"` | Relying party identifier |
| `RpName` | `"Test RP"` | Display name for RP |
| `UserId` | 16 random bytes | User handle |
| `UserName` | `"testuser@example.com"` | Username |
| `UserDisplayName` | `"Test User"` | Display name |
| `Challenge` | 32 random bytes | WebAuthn challenge |
| `Pin` | `"Abc12345"` | Test PIN (meets enhanced complexity) |
| `SimplePinFallback` | `"123456"` | Fallback for non-complexity devices |

### 8.2 Test Utilities

| Utility | Purpose |
|---------|---------|
| `SetOrVerifyPinAsync(session, pin)` | Ensures PIN is configured (auto-selects compliant PIN) |
| `DeleteAllCredentialsAsync(session)` | Removes all RK credentials for test RP |
| `ParseAuthenticatorData(bytes)` | Extracts rpIdHash, flags, signCount, attestedCredentialData |
| `ParseAttestationObject(bytes)` | Extracts authData, fmt, attStmt |
| `SkipIfNotSupported(info, feature)` | Conditional test skip helper |
| `HasPinComplexity(deviceInfo)` | Checks if enhanced PIN is enforced |
| `GetFirmwareFromManagement(device)` | Gets accurate firmware (not from FIDO) |

---

## 9. Test Matrix

### 9.1 Firmware x Feature Matrix

| Feature | 5.2.0 | 5.4.0 | 5.7.0 | 5.8.0+ |
|---------|-------|-------|-------|--------|
| MakeCredential (ES256) | ✓ | ✓ | ✓ | ✓ |
| GetAssertion | ✓ | ✓ | ✓ | ✓ |
| Credential Management | - | ✓ | ✓ | ✓ |
| hmac-secret extension | ✓ | ✓ | ✓ | ✓ |
| largeBlob extension | - | ✓ | ✓ | ✓ |
| EdDSA algorithm | - | - | ✓ | ✓ |
| alwaysUv config | - | ✓ | ✓ | ✓ |
| Enhanced PIN complexity | - | - | - | ✓ |

### 9.2 Test Class Structure

```
Yubico.YubiKit.Fido2.IntegrationTests/
├── FidoTestData.cs               # Shared constants
├── FidoTestExtensions.cs         # Helper methods
├── Ctap2GetInfoTests.cs          # GetInfo validation
├── MakeCredentialTests.cs        # Registration workflows
├── GetAssertionTests.cs          # Authentication workflows
├── CredentialManagementTests.cs  # Enumerate/delete
├── AlgorithmSupportTests.cs      # ES256, ES384, EdDSA
├── ExtensionTests.cs             # hmac-secret, largeBlob
├── FipsComplianceTests.cs        # FIPS-specific behaviors
├── EnhancedPinTests.cs           # PIN complexity (5.8+)
└── CancelOperationTests.cs       # Cancellation (USB only)
```

---

## 10. Device-Specific Notes

### 10.1 YubiKey 5.8.0+ with Enhanced PIN

Devices with "Enhanced PIN" have stricter PIN requirements:
- Minimum 8 characters
- Must contain mixed character types (letters + numbers minimum)
- Tests must detect this via `DeviceInfo` flags
- Test PIN `"Abc12345"` satisfies these requirements

### 10.2 Firmware Version Detection

**Warning:** FIDO2 `AuthenticatorInfo` firmware version is unreliable on some devices:
- Alpha/beta firmware may report `0.0.1`
- Always query `ManagementSession` for accurate version
- `YubiKeyTestState.FirmwareVersion` already uses ManagementSession

### 10.3 Test Device Configuration

Reference device for development:
- Device type: YubiKey 5 NFC - Enhanced PIN
- Firmware: 5.8.0.alpha.2
- Form factor: Keychain (USB-A)
- Interfaces: OTP, FIDO, CCID (USB); NFC enabled
- PIN complexity: Enforced

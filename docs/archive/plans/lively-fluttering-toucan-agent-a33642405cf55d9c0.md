# FIDO2 Integration Test Plan

## Context

This plan covers writing 7 new integration test files for the FIDO2 module in the **old SDK structure** (not the new modular `src/Fido2/` layout). The existing tests are located at:

```
Yubico.YubiKey/tests/integration/Yubico/YubiKey/Fido2/
```

The project targets `net8.0`, uses xUnit with `Xunit.SkippableFact`, and the existing patterns use:
- `FidoSessionIntegrationTestBase` - base class with `Session`, `Device`, `KeyCollector`, PIN constants, credential cleanup
- `SimpleIntegrationTestConnection` - simpler base with `Device` and `Connection`
- `IntegrationTestDeviceEnumeration.GetTestDevice()` for device acquisition
- `TraitTypes.Category` / `TestCategories.*` for categorization
- `SkippableFact` / `SkippableTheory` for tests that may not have required hardware
- `Fido2Session` as the main session class with `KeyCollector`, `AuthenticatorInfo`, etc.

## Key API Surface Discovered

### Extensions (string constants in `Fido2.Extensions`):
- `CredProtect`, `CredBlob`, `LargeBlobKey`, `MinPinLength`, `HmacSecret`, `HmacSecretMc`, `ThirdPartyPayment`

### MakeCredentialParameters methods:
- `AddExtension(string, byte[])` / `AddExtension(string, bool)` / `AddExtension(string, ICborEncode)`
- `AddCredBlobExtension(byte[], AuthenticatorInfo)` - validates credBlob size
- `AddHmacSecretExtension(AuthenticatorInfo)` - adds hmac-secret
- `AddCredProtectExtension(...)` - adds credProtect
- `AddMinPinLengthExtension(AuthenticatorInfo)` - adds minPinLength
- `AddOption(AuthenticatorOptions.*, bool)`
- `EnterpriseAttestation` property (nullable enum)

### GetAssertionParameters methods:
- `RequestCredBlobExtension()` - requests credBlob return
- `AddExtension("largeBlobKey", new byte[] { 0xF5 })` - requests largeBlobKey
- `AddHmacSecretExtension(...)` - with salt

### AuthenticatorData methods:
- `GetCredBlobExtension()` - returns byte[] (empty if not present)
- `GetCredProtectExtension()` - returns CredProtectPolicy
- `GetMinPinLengthExtension()` - returns int?

### MakeCredentialData:
- `LargeBlobKey` property (ReadOnlyMemory<byte>?)

### GetAssertionData:
- `LargeBlobKey` property (ReadOnlyMemory<byte>?)
- `AuthenticatorData` property

### Fido2Session methods:
- `TryEnableEnterpriseAttestation()` - returns bool
- `TryToggleAlwaysUv()` - returns bool
- `TrySetPinConfig(int?, IReadOnlyList<string>?, bool?)` - returns bool
- `GetBioModality()` - returns BioModality
- `GetFingerprintSensorInfo()` - returns FingerprintSensorInfo
- `EnumerateBioEnrollments()` - returns IReadOnlyList<TemplateInfo>
- `GetSerializedLargeBlobArray()` / `SetSerializedLargeBlobArray(...)` - large blob storage
- `EnumerateRelyingParties()`, `EnumerateCredentialsForRelyingParty(...)`, `DeleteCredential(...)`, `UpdateUserInfoForCredential(...)`
- `MakeCredential(...)`, `GetAssertions(...)`, `GetCredentialMetadata()`

## Files to Create

All files go in: `Yubico.YubiKey/tests/integration/Yubico/YubiKey/Fido2/`

### 1. `FidoCredBlobTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `FidoSessionIntegrationTestBase`
- Traits: `Elevated`, `RequiresTouch`
- Tests:
  1. `MakeCredential_WithCredBlob_StoresBlob` - uses `AddCredBlobExtension`, verifies `AuthenticatorData.GetCredBlobExtension()` returns data
  2. `GetAssertion_WithCredBlobRead_ReturnsBlobData` - makes credential with credBlob, gets assertion with `RequestCredBlobExtension()`, reads blob back

### 2. `FidoLargeBlobTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `FidoSessionIntegrationTestBase`
- Traits: `Elevated`, `RequiresTouch`
- Tests:
  1. `MakeCredential_WithLargeBlobKey_EnablesStorage` - uses `AddExtension(Extensions.LargeBlobKey, new byte[] { 0xF5 })`, verifies `LargeBlobKey` in response
  2. `LargeBlobStorage_WriteAndRead_RoundTrips` - creates credential with largeBlobKey, writes blob data via `SerializedLargeBlobArray`, reads back, verifies match

### 3. `FidoPrfTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `FidoSessionIntegrationTestBase`
- Traits: `Elevated`, `RequiresTouch`
- Uses hmac-secret (the CTAP2 equivalent of WebAuthn PRF)
- Tests:
  1. `MakeCredential_WithHmacSecret_ReturnsEnabled` - uses `AddHmacSecretExtension`, verifies hmac-secret in response extensions
  2. `GetAssertion_WithHmacSecretEval_ReturnsDerivedKey` - creates with hmac-secret, gets assertion with salt, verifies derived key returned

### 4. `FidoEnterpriseAttestationTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `FidoSessionIntegrationTestBase`
- Traits: `Elevated`, `RequiresTouch`
- Tests:
  1. `MakeCredential_WithEnterpriseAttestation_ReturnsAttestationStatement` - sets `EnterpriseAttestation` property, verifies credential created

### 5. `FidoAuthenticatorConfigTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `SimpleIntegrationTestConnection` (like existing ConfigTests)
- Traits: `Elevated`
- Tests:
  1. `ToggleAlwaysUv_SetsAndReadsOption` - toggle, verify via GetOptionValue
  2. `SetMinPinLength_EnforcesMinimum` - TrySetPinConfig with length, verify
  3. `SetForcePinChange_RequiresPinChangeOnNextUse` - TrySetPinConfig with forceChangePin=true, verify ForcePinChange

### 6. `FidoBioEnrollmentTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `SimpleIntegrationTestConnection` with `StandardTestDevice.Fw5Bio`
- Traits: `RequiresBio`, `Elevated`
- Tests:
  1. `GetSensorInfo_ReturnsSensorCapabilities` - calls `GetFingerprintSensorInfo()`, verifies fields

### 7. `FidoCredentialManagementExtendedTests.cs`
- Namespace: `Yubico.YubiKey.Fido2`
- Extends: `FidoSessionIntegrationTestBase`
- Traits: `Elevated`, `RequiresTouch`
- Tests:
  1. `UpdateUserInformation_ChangesDisplayName` - creates credential, updates display name, enumerates, verifies
  2. `EnumerateCredentials_MultipleUsersPerRp_ReturnsAll` - creates 3 resident credentials for same RP, enumerates, verifies all 3

## Implementation Approach

1. Write all 7 files following the exact patterns from `FidoSessionIntegrationTestBase`, `ConfigTests`, `BioEnrollTests`, `LargeBlobTests`, and `MakeCredentialGetAssertionTests`
2. Use `SkippableFact(typeof(DeviceNotFoundException))` for hardware-dependent tests
3. Check feature support via `AuthenticatorInfo.IsExtensionSupported(...)` or `GetOptionValue(...)` before each test
4. Clean up credentials in finally blocks using `DeleteCredential`
5. Build with `dotnet build Yubico.NET.SDK.sln` to verify compilation

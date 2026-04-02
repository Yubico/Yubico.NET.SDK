# CLAUDE.md - FIDO2 Tests

This file provides guidance for the FIDO2 module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

For FIDO2-specific test patterns, CBOR encoding, and backend abstractions, see the **Key Patterns** and **Architecture Overview** sections in [`../CLAUDE.md`](../CLAUDE.md).

## Test Projects

- `Yubico.YubiKit.Fido2.UnitTests` - Unit tests for FIDO2 module (xUnit v3)
- `Yubico.YubiKit.Fido2.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Test Infrastructure

### Transport Requirements

FIDO2 tests must be aware of transport differences:

**USB Tests:**
- Use `IFidoConnection` (HID FIDO interface)
- Primary test transport
- Supports all FIDO2 features

**NFC Tests:**
- Use `ISmartCardConnection` (CCID interface)
- Limited NFC-specific test scenarios
- Some features may not be available

⚠️ **USB CCID is NOT supported for FIDO2** - tests using USB SmartCard connections will fail

### User Interaction

FIDO2 operations require user presence (touch) and user verification (PIN/bio):

```csharp
[Theory]
[WithYubiKey(RequireUsb = true)] // FIDO2 needs HID or NFC
public async Task MakeCredential_TouchRequired_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreateFidoSessionAsync();
    
    // Test will wait for user to touch YubiKey
    // Timeout after 30 seconds if no touch
    var response = await session.MakeCredentialAsync(options);
}
```

### PIN Management in Tests

Tests requiring PIN should use test-specific PINs:

```csharp
// Set test PIN at start of test suite
const string TestPin = "123456";

await session.SetPinAsync(Encoding.UTF8.GetBytes(TestPin));

// Use PIN in credential operations
var options = new MakeCredentialOptions
{
    // ... rpOptions, user, etc.
    Pin = Encoding.UTF8.GetBytes(TestPin)
};
```

### Reset Before Tests

Integration tests should reset FIDO2 application to known state:

```csharp
[Theory]
[WithYubiKey]
public async Task TestWithCleanState(YubiKeyTestState state)
{
    await using var session = await state.Device.CreateFidoSessionAsync();
    
    // Reset FIDO2 app (WARNING: deletes all credentials)
    await session.ResetAsync();
    
    // Now in clean state for testing
}
```

## Running Tests

```bash
# Run all FIDO2 tests
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Fido2"

# Run unit tests only
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Fido2.UnitTests"

# Run integration tests only (requires YubiKey with FIDO2)
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Fido2.IntegrationTests"

# Run specific test class
dotnet build.cs test --filter "FullyQualifiedName~FidoSessionTests"
```

## Common Test Patterns

### Mock CBOR Responses

```csharp
// Unit tests can mock CBOR responses
var mockResponse = new CborMap
{
    { 0x01, credentialIdBytes },
    { 0x02, authDataBytes },
    { 0x03, attestationFormatBytes }
};
```

### Test WebAuthn Extensions

```csharp
var options = new MakeCredentialOptions
{
    // ... standard options
    Extensions = new ExtensionBuilder()
        .AddCredProtect(CredProtectPolicy.UserVerificationRequired)
        .AddHmacSecret(enabled: true)
        .Build()
};

var response = await session.MakeCredentialAsync(options);
Assert.True(response.Extensions.CredProtect.HasValue);
```

## Critical Test Warnings

- **Reset is destructive**: Deletes all FIDO2 credentials permanently
- **PIN retry limits**: Excessive failed PIN attempts block FIDO2
- **User interaction required**: Tests involving touch/PIN need manual intervention or test automation
- **Firmware version gates**: Some tests only work on specific firmware versions (check AuthenticatorInfo)
- **Resident key limits**: YubiKeys have limited storage (~25-32 credentials)


# CLAUDE.md - FIDO2 Tests

This file provides guidance for the FIDO2 module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

For FIDO2-specific test patterns, CBOR encoding, and backend abstractions, see the **Key Patterns** and **Architecture Overview** sections in [`../CLAUDE.md`](../CLAUDE.md).

## Test Projects

- `Yubico.YubiKit.Fido2.UnitTests` - Unit tests for FIDO2 module (xUnit v3)
- `Yubico.YubiKit.Fido2.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Test Infrastructure

### Transport Requirements

FIDO2 tests must be aware of transport differences:

**USB Tests:**
- Use `IFidoHidConnection` (HID FIDO interface)
- Primary test transport
- Supports all FIDO2 features

**NFC Tests:**
- Use `ISmartCardConnection` (CCID interface)
- Limited NFC-specific test scenarios
- Some features may not be available
- NFC-specific coverage requires a current PC/SC connection that reports `Transport.Nfc`; this is transport evidence, not YubiKey NFC capability metadata

**USB SmartCard Tests:**
- Use `ISmartCardConnection` only on firmware 5.8.0+ when the FIDO2 AID is exposed
- Prefer HID FIDO for USB FIDO2 coverage unless the test specifically validates the SmartCard APDU path
- FIDO GetInfo can report a `0.x` sentinel version even when Management reports the real firmware; firmware gates use `Feature.IsSupportedByFirmware(...)` so sentinel versions are treated as modern
- FIDO2 SmartCard session creation selects the FIDO2 AID before the firmware gate because firmware comes from CTAP GetInfo over the selected application

### User Interaction And Coordination Lanes

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

Classify FIDO2 hardware tests before running them:

| Lane | Examples | Agent-runnable? | Rule |
|------|----------|-----------------|------|
| Read-only smoke | `GetInfo`, session creation, transport discovery | Yes | Use `--smoke` for integration |
| User Presence | `MakeCredential`, `GetAssertion`, `previewSign` ceremonies | No by default | Mark with `Category=RequiresUserPresence`; run only with a human present |
| User Verification / PIN | PIN-token, UV-required/preferred, biometric checks | No by default | Requires explicit human approval and known PIN/device state |
| Reset/destructive | `ResetAsync`, broad credential cleanup | No | Human-approved destructive run only |
| Insert/remove/touch timing | reset power-cycle window, manual touch timing | No | Human-coordinated timing only |

Agents must not run User Presence, UV, reset, insert/remove, or destructive FIDO2 tests unless a human explicitly approves the exact command and is physically present for the interaction.

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
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~Yubico.YubiKit.Fido2"

# Run unit tests only
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~Yubico.YubiKit.Fido2.UnitTests"

# Run integration smoke tests only; skips Slow and RequiresUserPresence
dotnet toolchain.cs -- test --integration --project Fido2 --smoke --filter "FullyQualifiedName~Yubico.YubiKit.Fido2.IntegrationTests"

# Human-coordinated only; requires touch/UP approval immediately before running
dotnet toolchain.cs -- test --integration --project Fido2 --filter "Category=RequiresUserPresence"

# Run specific test class
dotnet toolchain.cs -- test --project Fido2 --filter "FullyQualifiedName~FidoSessionTests"
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
        .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
        .WithHmacSecret(hmacSecretInput)
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

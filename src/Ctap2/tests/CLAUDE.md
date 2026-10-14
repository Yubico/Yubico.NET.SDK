# CLAUDE.md - Ctap2 Tests

This file provides guidance for the CTAP2 module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

For CTAP2-specific test patterns, CBOR encoding, and backend abstractions, see the **Key Patterns** and **Architecture Overview** sections in [`../CLAUDE.md`](../CLAUDE.md).

## Test Projects

- `Yubico.YubiKit.Ctap2.UnitTests` - Unit tests for CTAP2 module (xUnit v3)
- `Yubico.YubiKit.Ctap2.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Test Infrastructure

### Transport Requirements

CTAP2 tests must be aware of transport differences:

**USB Tests:**
- Use `IFidoHidConnection` (HID FIDO interface)
- Primary test transport
- Supports all CTAP2 features

**NFC Tests:**
- Use `ISmartCardConnection` (CCID interface)
- Limited NFC-specific test scenarios
- Some features may not be available
- NFC-specific coverage requires a current PC/SC connection that reports `Transport.Nfc`; this is transport evidence, not YubiKey NFC capability metadata

**USB SmartCard Tests:**
- Use `ISmartCardConnection` only on firmware 5.8.0+ when the FIDO AID is exposed
- Prefer HID FIDO for USB CTAP2 coverage unless the test specifically validates the SmartCard APDU path
- FIDO GetInfo can report a `0.x` sentinel version even when Management reports the real firmware; firmware gates use `Feature.IsSupportedByFirmware(...)` so sentinel versions are treated as modern
- CTAP2 SmartCard session creation selects the FIDO AID before the firmware gate because firmware comes from CTAP GetInfo over the selected application

### User Interaction And Coordination Lanes

CTAP2 operations require user presence (touch) and user verification (PIN/bio):

```csharp
[SkippableTheory]
[WithYubiKey(RequireUsb = true)] // CTAP2 needs HID or NFC
public async Task MakeCredential_TouchRequired_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreateCtap2SessionAsync();
    
    // Test will wait for user to touch YubiKey
    // Timeout after 30 seconds if no touch
    var response = await session.MakeCredentialAsync(
        clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options);
}
```

Classify CTAP2 hardware tests before running them:

| Lane | Examples | Agent-runnable? | Rule |
|------|----------|-----------------|------|
| Read-only smoke | `GetInfo`, session creation, transport discovery | Yes | Use `--smoke` for integration |
| User Presence | `MakeCredential`, `GetAssertion`, `previewSign` ceremonies | No by default | Mark with `Category=RequiresUserPresence`; run only with a human present |
| User Verification / PIN | PIN-token, UV-required/preferred, biometric checks | No by default | Requires explicit human approval and known PIN/device state |
| Reset/destructive | `ResetAsync`, broad credential cleanup | No | Human-approved destructive run only |
| Insert/remove/touch timing | reset power-cycle window, manual touch timing | No | Human-coordinated timing only |

Agents must not run User Presence, UV, reset, insert/remove, or destructive CTAP2 tests unless a human explicitly approves the exact command and is physically present for the interaction.

### PIN Management in Tests

Tests requiring PIN should use test-specific PINs:

```csharp
// With an existing ICtap2Session session; only on a human-approved test device.
using var protocol = new PinUvAuthProtocolV2();
using var clientPin = new ClientPin(session, protocol);
byte[] pin = Encoding.UTF8.GetBytes(Ctap2TestStateExtensions.KnownTestPinString);
try
{
    await clientPin.SetPinAsync(pin);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
```

### Reset Before Tests

Do not reset the FIDO application as routine test setup. `Ctap2TestStateExtensions` normalizes
the known test PIN for explicitly approved PIN tests. A CTAP2 `ResetAsync` permanently deletes
credentials and requires a human-approved unplug/replug/touch ceremony; see the FidoTool reset example.

## Running Tests

```bash
# Run all CTAP2 tests
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2"

# Run unit tests only
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2.UnitTests"

# Run integration smoke tests only; skips Slow and RequiresUserPresence
dotnet toolchain.cs -- test --integration --project Ctap2 --smoke --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2.IntegrationTests"

# Human-coordinated only; requires touch/UP approval immediately before running
dotnet toolchain.cs -- test --integration --project Ctap2 --filter "Category=RequiresUserPresence"

# Run specific test class
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Ctap2SessionTests"
```

## Common Test Patterns

### Mock CBOR Responses

```csharp
// GetInfo response payload (the backend strips the CTAP status byte).
var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
writer.WriteStartMap(1);
writer.WriteInt32(1);
writer.WriteStartArray(1);
writer.WriteTextString("FIDO_2_0");
writer.WriteEndArray();
writer.WriteEndMap();
byte[] mockResponse = writer.Encode();
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

// clientDataHash, rp, user, and PIN/UV authorization come from the test setup.
var response = await session.MakeCredentialAsync(
    clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options);
Assert.NotNull(response.Extensions);
```

## Critical Test Warnings

- **Reset is destructive**: Deletes all CTAP2 credentials permanently
- **PIN retry limits**: Excessive failed PIN attempts block CTAP2
- **User interaction required**: Tests involving touch/PIN need manual intervention or test automation
- **Firmware version gates**: Some tests only work on specific firmware versions (check AuthenticatorInfo)
- **Resident key limits**: YubiKeys have limited storage (~25-32 credentials)

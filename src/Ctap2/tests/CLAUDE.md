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
- Exercises the HID path; feature availability still depends on advertised capabilities, model/state and authorization

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

Creation and ordinary assertion tests can require UP and PIN/built-in UV. GetInfo itself needs neither.
The following meaningful creation method belongs in the xUnit v2 IntegrationTests project, with
Tests.Shared/Infrastructure, TestExtensions, Ctap2 Credentials/Pin, Core Devices, and System.Security.Cryptography
imports. It uses the existing known-PIN helper, whose default setup may set/change/verify a PIN before
the callback. Run it only under the existing approval policy below; it creates a persistent demo credential,
does not verify RP registration, and does not restore prior PIN state or delete the credential.

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.HidFido)]
[Trait(TestCategories.Category, TestCategories.RequiresUserPresence)]
public async Task MakeCredential_TouchRequired_Succeeds(YubiKeyTestState state)
{
    await state.WithCtap2SessionAsync(async session =>
    {
        using var protocol = new PinUvAuthProtocolV2(); // Test device must support V2.
        using var clientPin = new ClientPin(session, protocol);
        byte[]? token = null;
        byte[]? authParam = null;
        try
        {
            byte[] clientDataHash = RandomNumberGenerator.GetBytes(32); // Protocol demo, not client data.
            var rp = new PublicKeyCredentialRpEntity("example.com", "Example");
            var user = new PublicKeyCredentialUserEntity(
                RandomNumberGenerator.GetBytes(32), "test@example.com", "Test");
            token = await clientPin.GetPinUvAuthTokenUsingPinAsync(
                Ctap2TestStateExtensions.KnownTestPin,
                PinUvAuthTokenPermissions.MakeCredential, rp.Id);
            authParam = protocol.Authenticate(token, clientDataHash);
            var options = new MakeCredentialOptions { ResidentKey = true };
            options.WithPinUvAuth(authParam, protocol.Version); // uv absent.
            var response = await session.MakeCredentialAsync(
                clientDataHash, rp, user, [PublicKeyCredentialParameters.CreateES256()], options);
            var attested = Assert.IsType<AttestedCredentialData>(response.AuthenticatorData.AttestedCredentialData);
            Assert.False(attested.CredentialId.IsEmpty);
        }
        finally
        {
            if (token is not null) CryptographicOperations.ZeroMemory(token);
            if (authParam is not null) CryptographicOperations.ZeroMemory(authParam);
        }
    }, preferredConnection: ConnectionType.HidFido);
}
```

Classify CTAP2 hardware tests before running them:

| Lane | Examples | Agent-runnable? | Rule |
|------|----------|-----------------|------|
| Read-only smoke | Direct `GetInfo`, session creation, transport discovery without PIN setup | Yes | Inspect setup; `--smoke` excludes traits, not implicit PIN normalization |
| User Presence | `MakeCredential`, `GetAssertion`, `previewSign` ceremonies | No by default | Mark with `Category=RequiresUserPresence`; run only with a human present |
| User Verification / PIN | PIN-token, UV-required/preferred, biometric checks | No by default | Requires explicit human approval and known PIN/device state |
| Reset/destructive | `ResetAsync`, broad credential cleanup | No | Human-approved destructive run only |
| Insert/remove/touch timing | reset power-cycle window, manual touch timing | No | Human-coordinated timing only |

Agents must not run User Presence, UV, reset, insert/remove, or destructive CTAP2 tests unless a human explicitly approves the exact command and is physically present for the interaction.

### PIN Management in Tests

Tests requiring PIN use known test bytes only on the explicitly approved device. This fragment requires
an existing `ICtap2Session session` on a device with PIN support and **no PIN already set**; it sets the
PIN, not verifies/normalizes an existing one:

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

Do not reset the FIDO application as routine test setup. `WithCtap2SessionAsync` defaults to
`normalizePin: true`: after initialization it may set the known test PIN, request a token to verify it,
or change it through a temporary value for forcePinChange recovery. Even a matching PIN is not a no-op;
a mismatch can consume a retry before a skip. Failed recovery can leave the temporary PIN, and current
helpers have cleanup gaps. Use explicit `normalizePin: false` when the test is intended to make only
read-only calls; do not treat existing default-helper smoke paths as read-only. A CTAP2 `ResetAsync` permanently deletes
credentials and requires a human-approved unplug/replug/touch ceremony; see the FidoTool reset example.

## Running Tests

```bash
# Default CTAP2 selection: unit projects only
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2"

# Explicit unit-test FQN filter (still unit projects only)
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2.UnitTests"

# Includes both project kinds, then filters to integration; excludes Slow and RequiresUserPresence.
# Inspect PIN-normalizing setup and apply the existing approval policy before any hardware run.
dotnet toolchain.cs -- test --integration --project Ctap2 --smoke --filter "FullyQualifiedName~Yubico.YubiKit.Ctap2.IntegrationTests"

# Human-coordinated only; requires touch/UP approval immediately before running
dotnet toolchain.cs -- test --integration --project Ctap2 --filter "Category=RequiresUserPresence"

# Focused unit-test class
dotnet toolchain.cs -- test --project Ctap2 --filter "FullyQualifiedName~Ctap2SessionTests"
```

## Common Test Patterns

### Mock CBOR Responses

```csharp
// Partial parser fixture (backend status already stripped), not a conforming GetInfo response.
// Required AAGUID is absent; current decoder accepts this incomplete envelope.
var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
writer.WriteStartMap(1);
writer.WriteInt32(1);
writer.WriteStartArray(1);
writer.WriteTextString("FIDO_2_0");
writer.WriteEndArray();
writer.WriteEndMap();
byte[] mockResponse = writer.Encode();
```

### Test CTAP Extension Encoding

This complete method belongs in the UnitTests project with xUnit v3, System.Formats.Cbor and Ctap2
Extensions imports. It checks the encoded input fields only, not device enforcement or WebAuthn PRF:

```csharp
[Fact]
public void RegistrationExtensions_EncodePolicyAndHmacEnablement()
{
    var encoded = new ExtensionBuilder()
        .WithCredProtect(CredProtectPolicy.UserVerificationRequired)
        .WithHmacSecret()
        .Build() ?? throw new InvalidOperationException("Expected extension bytes.");
    var reader = new CborReader(encoded, CborConformanceMode.Ctap2Canonical);
    Assert.Equal(2, reader.ReadStartMap());
    Assert.Equal("credProtect", reader.ReadTextString());
    Assert.Equal((int)CredProtectPolicy.UserVerificationRequired, reader.ReadInt32());
    Assert.Equal("hmac-secret", reader.ReadTextString());
    Assert.True(reader.ReadBoolean());
    reader.ReadEndMap();
}
```

## Critical Test Warnings

- **Reset is destructive**: Deletes all CTAP2 credentials permanently
- **PIN retry limits**: Excessive failed PIN attempts block CTAP2
- **User interaction required**: Tests involving touch/PIN need manual intervention or test automation
- **Firmware version gates**: Some tests only work on specific firmware versions (check AuthenticatorInfo)
- **Resident key limits**: Read credential metadata; remaining discoverable capacity is an estimate, not a fixed slot count
- **Evidence scope**: Parsed responses, nonempty signatures and fake-wire assertions are not RP/attestation,
  firmware, biometric or FIPS qualification. Bio APIs have known standard wire/schema/completion limitations.
- **Secret lifetime**: Existing integration fixtures have cleanup gaps; the optional AuditV2 blob interop
  fixture can export a large-blob key as immutable hex text to disk. It is a human-controlled secret-export
  path, not a no-persistence or unattended safety example.

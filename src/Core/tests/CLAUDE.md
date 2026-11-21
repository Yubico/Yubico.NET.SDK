# CLAUDE.md - Core Tests

This file provides guidance for the Core module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

For Core-specific patterns and test utilities, see the **Test Infrastructure** section in [`../CLAUDE.md`](../CLAUDE.md#test-infrastructure).

## Test Projects

- `Yubico.YubiKit.Core.UnitTests` - Unit tests for Core module (xUnit v3)
- `Yubico.YubiKit.Core.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Test Structure

```
tests/
├── Yubico.YubiKit.Core.UnitTests/
│   ├── Devices/              # YubiKey model, discovery, metadata tests
│   ├── Protocols/
│   │   ├── SmartCard/Apdu/   # APDU protocol tests and fakes
│   │   ├── SmartCard/Scp/    # SCP protocol tests
│   │   └── Otp/Hid/          # OTP HID protocol tests
│   ├── Transports/           # HID and SmartCard transport tests
│   ├── Cryptography/
│   ├── Credentials/
│   └── Utilities/            # TLV, utility tests
└── Yubico.YubiKit.Core.IntegrationTests/
    ├── Devices/              # YubiKeyManager, device tests
    └── Transports/           # HID and SmartCard integration tests
```

## Key Test Utilities

### FakeSmartCardConnection

Use `FakeSmartCardConnection` to test protocol logic without hardware:

```csharp
using var fakeConnection = new FakeSmartCardConnection();

// Queue expected responses
fakeConnection.EnqueueResponse(new byte[] { 0x90, 0x00 }); // Success

// Create protocol with fake
using var protocol = new PcscProtocol(fakeConnection);

// Test
var result = await protocol.SelectAsync(ApplicationIds.Piv, CancellationToken.None);

// Verify commands sent
Assert.Single(fakeConnection.TransmittedCommands);
```

This is a Core unit-test fragment using the internal protocol and fake from
`Protocols/SmartCard/Apdu/Fakes`. It checks one transmitted SELECT, not device interoperability or
all response bytes. The synchronous fake disposal updates its counter; its current async disposal
does not provide that same observation. Keep those test-oracle meanings separate.

### Integration Test Base

Integration tests inherit from `IntegrationTestBase` and use `[WithYubiKey]` attribute:

```csharp
public class MyIntegrationTests : IntegrationTestBase
{
    [SkippableTheory]
    [WithYubiKey]
    public async Task MyTest_DoesX_Succeeds(YubiKeyTestState state)
    {
        // The placeholder row binds lazily to its first matching device.
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
        // Test logic
    }
}
```

## Running Tests

```bash
# Run all Core tests
dotnet toolchain.cs -- test --filter "FullyQualifiedName~Yubico.YubiKit.Core"

# Run unit tests only
dotnet toolchain.cs -- test --filter "FullyQualifiedName~Yubico.YubiKit.Core.UnitTests"

# Run integration tests only (requires YubiKey)
dotnet toolchain.cs -- test --integration --project Core.IntegrationTests --smoke

# Run specific test class
dotnet toolchain.cs -- test --filter "FullyQualifiedName~PcscProtocolTests"
```

Review selected setup and trait requirements before hardware execution. The allow-list is applied
after live identification; discovery can already have opened a connection to read a serial. Hardware
fixtures, human-coordinated touch/hotplug/PIN work, and actual native/platform qualification remain
separate from managed fake assertions and source compilation.

# CLAUDE.md - Core Tests

This file provides guidance for the Core module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

For Core-specific patterns and test utilities, see the **Test Infrastructure** section in [`../CLAUDE.md`](../CLAUDE.md#test-infrastructure).

## Test Projects

- `Yubico.YubiKit.Core.UnitTests` - Unit tests for Core module (xUnit v3)
- `Yubico.YubiKit.Core.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Test Structure

```
tests/
├── Yubico.YubiKit.Core.UnitTests/
│   ├── SmartCard/
│   │   ├── Scp/              # SCP protocol tests
│   │   ├── Fakes/            # FakeSmartCardConnection, FakeApduProcessor
│   │   └── PcscProtocolTests.cs
│   ├── Utils/                # TLV, utility tests
│   └── Hid/                  # HID protocol tests
└── Yubico.YubiKit.Core.IntegrationTests/
    ├── Core/                 # YubiKeyManager, device tests
    └── Hid/                  # HID enumeration tests
```

## Key Test Utilities

### FakeSmartCardConnection

Use `FakeSmartCardConnection` to test protocol logic without hardware:

```csharp
var fakeConnection = new FakeSmartCardConnection();

// Queue expected responses
fakeConnection.QueueResponse([0x90, 0x00]); // Success
fakeConnection.QueueResponse([0x69, 0x82]); // Security status not satisfied

// Create protocol with fake
var protocol = new PcscProtocol(fakeConnection);

// Test
var result = await protocol.SelectAsync(ApplicationIds.Piv, CancellationToken.None);

// Verify commands sent
Assert.Single(fakeConnection.SentCommands);
```

### Integration Test Base

Integration tests inherit from `IntegrationTestBase` and use `[WithYubiKey]` attribute:

```csharp
public class MyIntegrationTests : IntegrationTestBase
{
    [Theory]
    [WithYubiKey]
    public async Task MyTest_DoesX_Succeeds(YubiKeyTestState state)
    {
        // state.YubiKey is available
        using var connection = await state.YubiKey.OpenConnectionAsync<ISmartCardConnection>();
        // Test logic
    }
}
```

## Running Tests

```bash
# Run all Core tests
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Core"

# Run unit tests only
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Core.UnitTests"

# Run integration tests only (requires YubiKey)
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Core.IntegrationTests"

# Run specific test class
dotnet build.cs test --filter "FullyQualifiedName~PcscProtocolTests"
```


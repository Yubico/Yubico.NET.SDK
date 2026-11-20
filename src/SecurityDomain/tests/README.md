# Security Domain Tests

This directory contains unit and integration tests for the Security Domain module.

## Test Projects

| Project | Purpose | Hardware Required |
|---------|---------|-------------------|
| `UnitTests` | Session behavior, parsing, internal logic | No |
| `IntegrationTests` | Session creation, SCP protocols, key operations | Yes |

## Running Tests

```bash
# Unit tests only (no hardware)
dotnet toolchain.cs -- test --project SecurityDomain --filter "FullyQualifiedName~Yubico.YubiKit.SecurityDomain.UnitTests"

# Integration tests (requires YubiKey)
dotnet toolchain.cs -- test --integration --project SecurityDomain --smoke --filter "FullyQualifiedName~Yubico.YubiKit.SecurityDomain.IntegrationTests"

# All Security Domain unit tests (integration is selected separately above)
dotnet toolchain.cs -- test --project SecurityDomain
```

## Integration Test Setup

### Allow List Configuration

Use only authorized dedicated test devices: the suite imports/deletes keys and requests destructive
Security Domain resets. Add your test device serial numbers to `appsettings.json`:

```json
{
  "YubiKeyTests": {
    "AllowedSerialNumbers": [12345678, 87654321]
  }
}
```

### Firmware Requirements

| Test Category | Minimum Firmware |
|---------------|------------------|
| SCP03 | 5.4.3 |
| SCP11 | 5.7.2 |

## Writing Tests

### Basic Pattern

This class-member fragment belongs in the existing xUnit-v2 integration project. It needs `Xunit`,
`Yubico.YubiKit.Tests.Shared`, `Yubico.YubiKit.Tests.Shared.Infrastructure`,
`Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions`,
`Yubico.YubiKit.Core.Protocols.SmartCard.Scp`, `Yubico.YubiKit.Core.Devices`, and standard threading/task namespaces.

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
public async Task MyTest(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.ListKeyInformationAsync(ct);
            Assert.NotEmpty(keyInfo);
        },
        scpKeyParams: parameters,
        cancellationToken: ct);
}
```

## Test Extensions

Implementation lives in `Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`.

### WithSecurityDomainSessionAsync

Borrows one SmartCard connection and creates each session with `SecurityDomainSession.CreateAsync()`.

The required arguments are `resetBeforeUse` followed by the session action. Optional arguments are
`configuration`, `scpKeyParams`, and `cancellationToken`. Omit SCP parameters for read-only discovery;
keep supplied disposable parameters in a scope enclosing the awaited helper call. For successive sessions,
use `resetBeforeUse: false` to preserve the previous session's changes. See the
[testing guidelines](../../../docs/TESTING.md) for test-runner and hardware requirements.

## Automatic SD Reset

The helper requests a reset when `resetBeforeUse: true`. Use `false` between related sessions to preserve
the state created by the previous session.

**How it works:**
1. Opens unauthenticated session
2. Calls `ResetAsync()` with at most 65 raw attempts per processed reference, then disposes that session
3. Opens a fresh test session with the supplied configuration, firmware and optional SCP parameters
4. Runs test action

Yubico documents factory SCP03 restoration after last-set deletion, but the SDK does not verify factory
contents after reset. Exhaustion alone does not throw, an empty key listing skips reset, and errors or
cancellation can leave partial changes. Tests that require defaults should assert the relevant state.

## Test Categories

### Unit Tests

- Session factory behavior (no hardware)
- Protocol response parsing
- Internal wire and validation logic

### Integration Tests

- Session creation with real hardware
- SCP03 authentication
- SCP11 protocols (firmware 5.7.2+)
- Key import, generation, replacement and deletion (not private-key export)
- Direct session factory end-to-end validation

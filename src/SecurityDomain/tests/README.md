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

# All Security Domain tests
dotnet toolchain.cs -- test --project SecurityDomain
```

## Integration Test Setup

### Allow List Configuration

Add your test device serial numbers to `appsettings.json`:

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

```csharp
[SkippableTheory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task MyTest(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keyInfo = await session.GetKeyInfoAsync(ct);
            Assert.NotEmpty(keyInfo);
        },
        scpKeyParams: Scp03KeyParameters.Default,
        cancellationToken: ct);
```

## Test Extensions

Implementation lives in `Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`.

### WithSecurityDomainSessionAsync

Borrows one SmartCard connection and creates each session with `SecurityDomainSession.CreateAsync()`.

```csharp
await state.WithSecurityDomainSessionAsync(
    resetBeforeUse: true,           // Reset SD to default keys
    async session => { },           // Your test action
    configuration: null,            // Optional protocol config
    scpKeyParams: Scp03KeyParameters.Default,  // SCP authentication
    cancellationToken: ct);
```

## Automatic SD Reset

Integration tests use an automatic reset mechanism that factory-resets the Security Domain before each test (when `resetBeforeUse: true`).

**How it works:**
1. Opens unauthenticated session
2. Calls `ResetAsync()` which blocks all keys (65 failed auth attempts per key)
3. Opens test session with SCP authentication
4. Runs test action

**After reset:** Default SCP03 keys are restored (KVN=0xFF).

## Test Categories

### Unit Tests

- Session factory behavior (no hardware)
- Protocol response parsing
- Internal wire and validation logic

### Integration Tests

- Session creation with real hardware
- SCP03 authentication
- SCP11 protocols (firmware 5.7.2+)
- Key import/export operations
- Direct session factory end-to-end validation

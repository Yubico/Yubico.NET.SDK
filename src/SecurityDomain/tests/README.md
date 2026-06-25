# Security Domain Tests

This directory contains unit and integration tests for the Security Domain module.

## Test Projects

| Project | Purpose | Hardware Required |
|---------|---------|-------------------|
| `UnitTests` | DI registration, parsing, internal logic | No |
| `IntegrationTests` | Session creation, SCP protocols, key operations | Yes |

## Running Tests

```bash
# Unit tests only (no hardware)
dotnet test Yubico.YubiKit.SecurityDomain.UnitTests

# Integration tests (requires YubiKey)
dotnet test Yubico.YubiKit.SecurityDomain.IntegrationTests

# All Security Domain tests
dotnet test Yubico.YubiKit.SecurityDomain/tests
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
[Theory]
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

### Testing with DI Factory

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task DIFactory_Test(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionFromDIAsync(
        resetBeforeUse: true,
        async session =>
        {
            Assert.True(session.IsAuthenticated);
        },
        scpKeyParams: Scp03KeyParameters.Default,
        cancellationToken: ct);
```

## Test Extensions

Implementation lives in `Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`.

### WithSecurityDomainSessionAsync

Creates a session directly via `Device.CreateSecurityDomainSessionAsync()`.

```csharp
await state.WithSecurityDomainSessionAsync(
    resetBeforeUse: true,           // Reset SD to default keys
    async session => { },           // Your test action
    configuration: null,            // Optional protocol config
    scpKeyParams: Scp03KeyParameters.Default,  // SCP authentication
    cancellationToken: ct);
```

### WithSecurityDomainSessionFromDIAsync

Creates a session via the DI-registered `SecurityDomainSessionFactory`.

```csharp
// Simple form - builds ServiceProvider internally with:
//   services.AddYubiKeyManagerCore();
//   services.AddYubiKeySecurityDomain();
await state.WithSecurityDomainSessionFromDIAsync(
    resetBeforeUse: true,
    async session => { },
    configuration: null,
    scpKeyParams: Scp03KeyParameters.Default,
    cancellationToken: ct);

// Custom ServiceProvider (for additional service registrations)
var services = new ServiceCollection();
services.AddYubiKeyManagerCore();
services.AddYubiKeySecurityDomain();
services.AddSingleton<IMyService, MyService>(); // your services
await using var provider = services.BuildServiceProvider();

await state.WithSecurityDomainSessionFromDIAsync(
    resetBeforeUse: true,
    async session => { },
    serviceProvider: provider,
    configuration: null,
    scpKeyParams: Scp03KeyParameters.Default,
    cancellationToken: ct);
```

**Note:** `AddYubiKeySecurityDomain()` requires `AddYubiKeyManagerCore()` to be called first. The simple form handles this automatically.

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

- DI registration mechanics (no hardware)
- Factory delegate signature validation
- Service collection integration

### Integration Tests

- Session creation with real hardware
- SCP03 authentication
- SCP11 protocols (firmware 5.7.2+)
- Key import/export operations
- DI factory end-to-end validation

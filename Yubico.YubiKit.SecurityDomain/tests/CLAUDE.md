# CLAUDE.md - Security Domain Tests

This file provides Claude-specific guidance for the Security Domain test infrastructure.

## Test Extension Methods

### Location

`IntegrationTests/TestExtensions/TestStateExtensions.cs`

### Two Session Creation Patterns

#### 1. Direct Session Creation

```csharp
extension(YubiKeyTestState state)
{
    public Task WithSecurityDomainSessionAsync(
        bool resetBeforeUse,
        Func<SecurityDomainSession, Task> action,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
}
```

**Use when:** Testing `SecurityDomainSession` behavior directly.

**Implementation:**
- Calls `state.Device.CreateSecurityDomainSessionAsync()` directly
- Handles SD reset via separate unauthenticated session
- Uses `SharedSmartCardConnection` to share connection between reset and test sessions

#### 2. DI Factory Session Creation

```csharp
extension(YubiKeyTestState state)
{
    // Simple form - builds ServiceProvider internally
    public Task WithSecurityDomainSessionFromDIAsync(
        bool resetBeforeUse,
        Func<SecurityDomainSession, Task> action,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)

    // Custom ServiceProvider overload
    public Task WithSecurityDomainSessionFromDIAsync(
        bool resetBeforeUse,
        Func<SecurityDomainSession, Task> action,
        IServiceProvider serviceProvider,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
}
```

**Use when:** Testing that the DI-registered factory creates working sessions.

**Implementation:**
- Simple form builds `ServiceCollection` + `ServiceProvider` internally
- Registers both `AddYubiKeyManagerCore()` and `AddYubiKeySecurityDomain()` (no chaining)
- Resolves `SecurityDomainSessionFactory` and invokes with connection
- Same reset pattern as direct creation
- Provider is disposed after test completes

**DI Pattern (no chaining):**
```csharp
// Each module registers only its own services
// Caller must register dependencies explicitly
services.AddYubiKeyManagerCore();      // Core services (idempotent via TryAdd*)
services.AddYubiKeySecurityDomain();   // SecurityDomain factory (idempotent)
```

### SharedSmartCardConnection

Both extensions use `SharedSmartCardConnection` to share a single physical connection between the reset session and test session. This wrapper prevents the reset session from disposing the connection when it completes.

## Unit vs Integration Test Separation

### Unit Tests (DependencyInjectionTests.cs)

**What they test:**
- Factory registration (`AddYubiKeySecurityDomain()`)
- Singleton lifetime
- Fluent API chaining
- Delegate signature via reflection

**What they DON'T test:**
- Actual session creation (no connection)
- SCP authentication (no hardware)
- Protocol communication

### Integration Tests (SecurityDomainSession_DependencyInjectionTests.cs)

**What they test:**
- End-to-end: DI container → Factory resolution → Session creation → Query
- Configuration flows through factory correctly
- SCP parameters work via factory
- Unauthenticated session creation

## Reset Mechanism Details

The `ResetAsync()` method (in `SecurityDomainSession.cs:685`):

1. **Enumerates keys** via `GetKeyInfoAsync()`
2. **For each key type**, sends bogus authentication attempts:
   - SCP03 (KID=0x01): `INITIALIZE UPDATE` with bad payload
   - SCP11a/c (KID=0x10/0x15): `EXTERNAL AUTHENTICATE`
   - SCP11b (KID=0x13): `INTERNAL AUTHENTICATE`
3. **Up to 65 attempts** per key until blocked (`0x6983` or `0x6988`)
4. **Reinitializes session** after all keys blocked

**Post-reset state:** Default SCP03 key with KVN=0xFF.

## Test Patterns

### Standard Integration Test

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task TestName_Condition_ExpectedResult(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            // Arrange/Act/Assert
        },
        scpKeyParams: Scp03KeyParameters.Default,
        cancellationToken: CancellationTokenSource.Token);
```

### DI Integration Test

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task Factory_Condition_ExpectedResult(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionFromDIAsync(
        resetBeforeUse: true,
        async session =>
        {
            // Arrange/Act/Assert
        },
        scpKeyParams: Scp03KeyParameters.Default,
        cancellationToken: CancellationTokenSource.Token);
```

### Multi-Session Test (Key Import)

```csharp
[Theory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task KeyImport_MultiSession_Test(YubiKeyTestState state)
{
    // Session 1: Import key (reset first)
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            await session.PutKeyAsync(keyRef, keys, 0, ct);
        },
        scpKeyParams: Scp03KeyParameters.Default,
        cancellationToken: ct);

    // Session 2: Verify with new key (DON'T reset!)
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: false,  // Preserve imported key
        async session =>
        {
            Assert.True(session.IsAuthenticated);
        },
        scpKeyParams: newKeyParams,
        cancellationToken: ct);
}
```

## Firmware Version Handling

```csharp
// Key count varies by firmware
var expectedKeyCount = state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3;
Assert.Equal(expectedKeyCount, keyInfo.Count);
```

## Common Gotchas

1. **Reset destroys all keys** - Once `resetBeforeUse: true`, any custom keys are gone
2. **Don't reset between related sessions** - Use `resetBeforeUse: false` for second session in multi-session tests
3. **CancellationToken in GetDataAsync** - Use named parameter: `GetDataAsync(0x66, cancellationToken: ct)`
4. **Firmware checks** - SCP11 tests require `MinFirmware = "5.7.2"`
5. **DI prerequisite** - `AddYubiKeySecurityDomain()` requires `AddYubiKeyManagerCore()` first (no chaining)

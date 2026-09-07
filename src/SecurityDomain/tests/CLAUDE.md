# CLAUDE.md - Security Domain Tests

This file provides Claude-specific guidance for the Security Domain test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

## Test Extension Methods

### Location

`IntegrationTests/TestExtensions/TestStateExtensions.cs`

### Session creation pattern

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
- Borrows one SmartCard connection and calls `SecurityDomainSession.CreateAsync()` for each session
- Handles SD reset via a separate unauthenticated session
- Runs the reset session and the test session in sequence over that connection

### One connection, successive sessions

The helper runs the reset session and the test session over a single physical connection. No wrapper is needed: a session does not dispose a connection it did not create, so the reset session leaves the connection open for the test session, and `WithConnectionAsync` disposes it at the end.

The reset session is scoped to its own disposal declaration deliberately. A connection hosts one live session at a time, so the reset session must be disposed before the test session is constructed.

## Unit vs Integration Test Separation

### Session factory tests

**What they test:**
- Static factory and device-extension behavior
- Connection ownership and initialization behavior
- Protocol and response handling

**What they DON'T test:**
- Actual session creation (no connection)
- SCP authentication (no hardware)
- Protocol communication

### Integration tests

**What they test:**
- End-to-end: Connection → Session creation → Query
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
[SkippableTheory]
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

### Multi-Session Test (Key Import)

```csharp
[SkippableTheory]
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
5. **Creation scope** - applet factories and Core discovery are static APIs; no applet dependency-injection helper exists

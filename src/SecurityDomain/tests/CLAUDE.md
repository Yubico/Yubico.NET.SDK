# CLAUDE.md - Security Domain Tests

This file provides Claude-specific guidance for the Security Domain test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet toolchain.cs test` - NEVER use `dotnet test` directly.**

## Test Extension Methods

### Location

[`Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs`](Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/TestStateExtensions.cs)

### Session creation pattern

The helper returns `Task` and accepts `bool resetBeforeUse`, `Func<SecurityDomainSession, Task> action`,
`ProtocolConfiguration? configuration = null`, `ScpKeyParameters? scpKeyParams = null`, then
`CancellationToken cancellationToken = default`. Place the required reset flag before the action.

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

Factories are exercised using mocks or recording connections, including actual selection and wire
encoding. The specialized SCP03 fixture derives keys for one deterministic exchange sequence; its CMAC
primitive has RFC vectors, but it does not verify the host cryptogram/MAC or establish SCP conformance.
Unit tests do not establish device interoperability. Certificate bundle tests observe native handles,
order, identity and caller ownership; the convenience listing test does not directly observe disposal.

### Integration tests

**What they test:**
- End-to-end: Connection → Session creation → Query
- Configuration flows through factory correctly
- SCP parameters work via factory
- Unauthenticated session creation

## Reset Mechanism Details

The `ResetAsync()` method (in `SecurityDomainSession.cs`):

1. **Enumerates keys** via `ListKeyInformationAsync()`
2. **For each key type**, sends bogus authentication attempts:
    - SCP03 (KID=0x01): `INITIALIZE UPDATE` with reference 00/00; component KIDs 02/03 are skipped
    - SCP11a/c (KID=0x11/0x15): `EXTERNAL AUTHENTICATE`
    - SCP11b (KID=0x13): `INTERNAL AUTHENTICATE`
    - Other references: `PERFORM SECURITY OPERATION`
3. **At most 65 raw attempts** per processed reference, stopping on `AuthenticationMethodBlocked` or
   `SecurityStatusNotSatisfied`; ordinary transmission errors are retried and exhaustion itself does not throw
4. **Reinitializes session**, without verifying factory contents; an empty key listing skips reset

Custom state may be lost even on failure/cancellation. The vendor's factory SCP03 restoration after
last-set deletion does not verify every mixed-key/firmware state. The helper uses an unauthenticated reset
session, disposes it, then creates the test session with the supplied configuration and test firmware.
That fresh creation avoids relying on reset reinitialization, which currently omits those options.

## Test Patterns

### Standard Integration Test

The following are class-member fragments for the existing **xUnit-v2 integration project**, not the v3
unit project. Import `Xunit`, `Yubico.YubiKit.Tests.Shared`, `Yubico.YubiKit.Tests.Shared.Infrastructure`,
`Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions`,
`Yubico.YubiKit.Core.Protocols.SmartCard.Scp`, and `Yubico.YubiKit.Core.Devices`, plus standard threading/task
namespaces. Use only a dedicated authorized SmartCard device; these examples request destructive reset.

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
public async Task ListAfterReset_ContainsFactoryVersion(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    await state.WithSecurityDomainSessionAsync(
        resetBeforeUse: true,
        async session =>
        {
            var keys = await session.ListKeyInformationAsync(ct);
            Assert.Contains(keys, entry => entry.KeyReference.Kvn == 0xFF);
        },
        scpKeyParams: parameters,
        cancellationToken: ct);
}
```

### Multi-Session Test (Key Generation)

This additional class-member fragment uses firmware 5.7.2+. The first session generates a key through
SCP03; the second lists its reference without SCP. It checks persistence, not SCP11 authentication.
No preliminary reset is used for the second session. Cleanup has its own token so test cancellation
does not automatically cancel the cleanup request; cleanup still cannot guarantee factory restoration.

```csharp
[SkippableTheory]
[WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.2")]
public async Task GeneratedKey_IsListedInNextSession(YubiKeyTestState state)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(100));
    CancellationToken ct = timeout.Token;
    using var parameters = Scp03KeyParameters.Default;
    var keyRef = new KeyReference(ScpKid.SCP11b, 0x03);
    try
    {
        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: true,
            async session => { await session.GenerateKeyAsync(keyRef, cancellationToken: ct); },
            scpKeyParams: parameters,
            cancellationToken: ct);

        await state.WithSecurityDomainSessionAsync(
            resetBeforeUse: false,
            async session =>
            {
                var keys = await session.ListKeyInformationAsync(ct);
                Assert.Contains(keys, entry => entry.KeyReference.Kid == keyRef.Kid &&
                    entry.KeyReference.Kvn == keyRef.Kvn);
            },
            cancellationToken: ct);
    }
    finally
    {
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await state.ResetSecurityDomainAsync(cleanup.Token);
    }
}
```

## Firmware Version Handling

Existing factory-state tests expect three entries before 5.7.2 and four from 5.7.2. That expectation is not
a universal key count after provisioning. The helper supplies `state.FirmwareVersion` to each new session.

## Common Gotchas

1. **Reset is destructive** - It can remove custom SD keys/data; neither return nor cancellation proves a complete factory state
2. **Don't reset between related sessions** - Use `resetBeforeUse: false` for second session in multi-session tests
3. **CancellationToken in GetDataAsync** - Use named parameter: `GetDataAsync(0x66, cancellationToken: ct)`
4. **Firmware checks** - SCP11 tests require `MinFirmware = "5.7.2"`
5. **Creation scope** - applet factories and Core discovery are static APIs; no applet dependency-injection helper exists

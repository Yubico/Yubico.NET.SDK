# CLAUDE.md - Security Domain Module

This file provides Claude-specific guidance for working with the Security Domain module. **Read [README.md](README.md) first** for general module documentation.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new SCP protocols, key operations) should include usage examples in README.md and implementation guidance in CLAUDE.md
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** (especially automatic reset mechanism) should be reflected in the test pattern sections below

## Module Context

The Security Domain module manages YubiKey's root security application, which controls secure channel establishment using SCP (Secure Channel Protocol). This is a **low-level security module** that requires careful handling of cryptographic keys and secure sessions.

**Key Files:**
- [`SecurityDomainSession.cs`](src/SecurityDomainSession.cs) - Main session class (~984 lines, single file implementation)
- Test infrastructure in `tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/`

## Critical Security Requirements

### Memory and Crypto Safety

This module handles **highly sensitive cryptographic material**. Apply these rules strictly:

```csharp
// ✅ ALWAYS zero sensitive data after use
CryptographicOperations.ZeroMemory(keySpan);

// ✅ ALWAYS use ArrayPool for temporary key buffers
byte[]? keyBuffer = ArrayPool<byte>.Shared.Rent(16);
try
{
    // Use keyBuffer
}
finally
{
    if (keyBuffer is not null)
    {
        CryptographicOperations.ZeroMemory(keyBuffer.AsSpan(0, 16));
        ArrayPool<byte>.Shared.Return(keyBuffer, clearArray: true);
    }
}

// ✅ ALWAYS use fixed-time comparisons for crypto verification
if (!CryptographicOperations.FixedTimeEquals(expected, actual))
    throw new InvalidOperationException("Verification failed");

// ❌ NEVER log sensitive data (keys, PINs, challenge/response data)
_logger.LogDebug("Key imported"); // ✅ OK
_logger.LogDebug($"Key value: {keyHex}"); // ❌ NEVER
```

### Key Component Handling

The `EncodeKeyComponent()` method pattern (lines 869-905) demonstrates correct key handling:
1. Rent buffer from `ArrayPool`
2. Copy sensitive data to rented buffer
3. Perform encryption
4. Zero the buffer in `finally` block
5. Return buffer to pool with `clearArray: true`

## Test Infrastructure

### Automatic SD Reset System

**CRITICAL:** All Security Domain integration tests use an **automatic reset system** that factory-resets the SD before each test. Understanding this is essential when writing or debugging tests.

#### How It Works

Tests use the `WithSecurityDomainSessionAsync` extension method in [`SecurityDomainTestStateExtensions.cs`](tests/Yubico.YubiKit.SecurityDomain.IntegrationTests/TestExtensions/SecurityDomainTestStateExtensions.cs):

```csharp
extension(YubiKeyTestState state)
{
    public Task WithSecurityDomainSessionAsync(
        Func<SecurityDomainSession, Task> action,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        bool resetBeforeUse = true,  // ← Defaults to TRUE
        CancellationToken cancellationToken = default)
}
```

**Two-Session Reset Pattern:**
1. Creates `resetSession` without SCP authentication
2. Calls `ResetAsync()` to block all keys (65 failed auth attempts per key)
3. Creates the actual test `session` with SCP parameters
4. Runs the test action

#### The ResetAsync Method

Located at [line 685](src/SecurityDomainSession.cs#L685), the reset process:

1. **Enumerates keys** via `GetKeyInformationAsync()`
2. **For each key type**, determines the appropriate blocking instruction:
   - SCP03 (KID=0x01): `INITIALIZE UPDATE`
   - SCP11a/c (KID=0x10/0x15): `EXTERNAL AUTHENTICATE`
   - SCP11b (KID=0x13): `INTERNAL AUTHENTICATE`
   - Others: `PERFORM SECURITY OPERATION`
3. **Sends up to 65 failed attempts** with bogus payload (`ResetAttemptPayload`)
4. **Waits for block status**: `0x6983` (blocked) or `0x6988` (security not satisfied)
5. **Reinitializes session** after all keys are blocked

#### Writing Tests

```csharp
// Standard pattern: reset enabled (clean slate for each test)
[Theory]
[WithYubiKey(MinFirmware = "5.7.2")]
public async Task MyTest_DoesX_Succeeds(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionAsync(
        async session =>
        {
            // Test runs with default keys (0xFF) after reset
            var keyInfo = await session.GetKeyInformationAsync(cancellationToken);
            Assert.Contains(keyInfo.Keys, k => k.Kvn == 0xFF);
        },
        resetBeforeUse: true,  // Explicitly documented
        cancellationToken: CancellationTokenSource.Token);

// When testing reset itself
[Theory]
[WithYubiKey(MinFirmware = "5.4.3")]
public async Task ResetAsync_ReinitializesSession(YubiKeyTestState state) =>
    await state.WithSecurityDomainSessionAsync(
        async session =>
        {
            await session.ResetAsync(cancellationToken);
            // Verify reset worked
        },
        resetBeforeUse: false,  // Don't reset before testing reset!
        cancellationToken: cancellationToken);

// Multi-session tests
await state.WithSecurityDomainSessionAsync(
    async session =>
    {
        // Generate key
        var publicKey = await session.GenerateKeyAsync(keyRef, 0, cancellationToken);
    },
    scpKeyParams: Scp03KeyParameters.Default,
    resetBeforeUse: true,  // Clean start
    cancellationToken: cancellationToken);

// Second session verifies first session's changes
await state.WithSecurityDomainSessionAsync(
    async session =>
    {
        var keyInfo = await session.GetKeyInformationAsync(cancellationToken);
        Assert.Contains(keyInfo.Keys, k => k.Kid == keyRef.Kid);
    },
    resetBeforeUse: false,  // Don't destroy what we just created!
    cancellationToken: cancellationToken);
```

## Common Patterns

### SCP Parameter Types

```csharp
// SCP03: Symmetric keys
using var scp03Keys = new StaticKeys(encKey, macKey, dekKey);
var scp03Params = new Scp03KeyParameters(keyRef, scp03Keys);

// SCP11b: Public key only
var scp11bParams = new Scp11KeyParameters(keyRef, publicKey);

// SCP11a/c: Full certificate chain + private key
var scp11aParams = new Scp11KeyParameters(
    keyRef,
    publicKey,
    privateKey,
    oceKeyRef,
    certificateChain);
```

### Session Initialization Pattern

The module uses a two-phase initialization:

```csharp
// Phase 1: Constructor (private)
private SecurityDomainSession(
    ISmartCardConnection connection,
    ScpKeyParameters? scpKeyParams = null)

// Phase 2: Async initialization
private async Task InitializeAsync(
    FirmwareVersion? firmwareVersion = null,
    ProtocolConfiguration? configuration = null,
    CancellationToken cancellationToken = default)
{
    // 1. Create protocol (uses global YubiKit logging)
    Protocol = PcscProtocolFactory<ISmartCardConnection>
        .Create()
        .Create(connection);

    // 2. Select SD application + configure protocol
    firmwareVersion ??= FirmwareVersion.V5_3_0;
    await Protocol.SelectAsync(ApplicationIds.SecurityDomain, cancellationToken);
    Protocol.Configure(firmwareVersion, configuration);

    // 3. Establish SCP if keys provided
    if (scpKeyParams is not null && Protocol is ISmartCardProtocol sc)
    {
        Protocol = await sc.WithScpAsync(scpKeyParams, cancellationToken);
        IsAuthenticated = true;
    }

    IsInitialized = true;
}
```

**Pattern:** Factory method `CreateAsync()` combines both phases.

### Error Handling

```csharp
try
{
    return await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    throw;  // Always rethrow cancellation
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Operation failed");
    throw;
}
```

## GlobalPlatform Command Structure

Commands follow GlobalPlatform specifications:

```csharp
const byte ClaGlobalPlatform = 0x80;

// Common instructions
const byte InsGetData = 0xCA;
const byte InsPutKey = 0xD8;
const byte InsInitializeUpdate = 0x50;
const byte InsExternalAuthenticate = 0x82;
const byte InsInternalAuthenticate = 0x88;
const byte InsDelete = 0xE4;
const byte InsGenerateKey = 0xF1;
const byte InsPerformSecurityOperation = 0x2A;

// TLV tags for data encoding
const int TagKeyInformationTemplate = 0xE0;
const byte TagControlReference = 0xA6;
const byte TagKidKvn = 0x83;
```

## Debugging Tips

### Enable Verbose Logging

```csharp
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole();
});

using var session = await SecurityDomainSession.CreateAsync(
    connection,
    loggerFactory: loggerFactory,
    cancellationToken: cancellationToken);
```

### Key Information Inspection

```csharp
var keyInfo = await session.GetKeyInformationAsync(cancellationToken);
foreach (var key in keyInfo.Keys)
{
    _logger.LogDebug("Key: KID={Kid:X2}, KVN={Kvn:X2}", key.Kid, key.Kvn);
}
```

## Firmware Version Considerations

- **5.3.0**: Initial Security Domain support, SCP03 only
- **5.4.3**: Enhanced SCP03 features
- **5.7.2**: SCP11 protocols added (4 keys reported instead of 3)

```csharp
// Version-aware assertions
Assert.Equal(
    state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3,
    keyInformation.Count);
```

## Known Gotchas

1. **Reset is irreversible during test**: Once `ResetAsync()` is called, all custom keys are blocked. No undo.
2. **Default keys are always KVN=0xFF**: After reset, the default SCP03 key has KVN=0xFF.
3. **SCP11 requires 5.7.2+**: Don't test SCP11 features on older firmware.
4. **Certificate chain order matters**: For SCP11a/c, leaf cert goes last in the chain.
5. **ArrayPool buffer sizes**: Always rent exact or larger size, never assume exact size returned.

## Related Modules

- **Core.SmartCard.Scp**: Base SCP protocol implementations
- **Core.Cryptography**: ECPrivateKey, ECPublicKey, StaticKeys
- **Tests.Shared**: YubiKeyTestState, test infrastructure

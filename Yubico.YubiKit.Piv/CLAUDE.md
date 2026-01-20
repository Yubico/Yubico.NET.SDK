# CLAUDE.md - PIV Module

This file provides Claude-specific guidance for working with the PIV module. **Read [README.md](README.md) first** for general module documentation.

## Documentation Maintenance

> **Important:** This documentation is subject to change. When working on this module:
> - **Notable changes** to APIs, patterns, or behavior should be documented in both CLAUDE.md and README.md
> - **New features** (e.g., new slot operations, key algorithms) should include usage examples in README.md and implementation guidance in CLAUDE.md
> - **Breaking changes** require updates to both files with migration guidance
> - **Test infrastructure changes** should be reflected in the test pattern sections below

## Module Context

The PIV module provides access to the PIV (Personal Identity Verification) application on YubiKeys, implementing FIPS 201 smart card standard for identity verification and cryptographic operations.

**Key Components:**
- `PivSession.cs` - Main session class and initialization
- `PivSession.Authentication.cs` - PIN, PUK, and management key operations
- `PivSession.KeyPairs.cs` - Key generation, import, and public key retrieval
- `PivSession.Crypto.cs` - Sign, decrypt, and key agreement operations
- `PivSession.Certificates.cs` - Certificate import, retrieval, and management
- `PivSession.Metadata.cs` - Key and certificate metadata operations
- `PivSession.DataObjects.cs` - PIV data object read/write operations
- `PivSession.Bio.cs` - YubiKey Bio series biometric operations

**PIV Capabilities:**
- **24 key slots**: 4 standard PIV slots + 20 retired key management slots + attestation slot
- **Multiple algorithms**: RSA (1024, 2048, 3072, 4096), EC (P-256, P-384, P-521)
- **Flexible authentication**: PIN, PUK, Management Key (3DES, AES-128, AES-192, AES-256)
- **Policy enforcement**: PIN policy (Default, Never, Once, Always) and Touch policy (Default, Never, Always, Cached)
- **Attestation**: Generate attestation statements for on-device key generation

## Critical Security Requirements

### Sensitive Data Handling

PIV manages **highly sensitive material** including PINs, PUKs, and private keys. Apply strict security hygiene:

```csharp
// ✅ ALWAYS zero PIN/PUK after use
Span<byte> pin = stackalloc byte[8];
try
{
    GetPinFromUser(pin);
    session.VerifyPin(pin);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}

// ✅ ALWAYS zero management keys
byte[]? managementKey = ArrayPool<byte>.Shared.Rent(24);
try
{
    GetManagementKeyFromStorage(managementKey.AsSpan(0, 24));
    session.AuthenticateManagementKey(managementKey.AsMemory(0, 24));
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey.AsSpan(0, 24));
    ArrayPool<byte>.Shared.Return(managementKey, clearArray: true);
}

// ❌ NEVER log sensitive data
_logger.LogDebug("PIN verified"); // ✅ OK
_logger.LogDebug($"PIN value: {pin}"); // ❌ NEVER
_logger.LogDebug($"Management key: {Convert.ToHexString(key)}"); // ❌ NEVER
```

### KeyCollector Pattern

The KeyCollector delegate is the **primary mechanism** for obtaining secrets from users:

```csharp
private bool KeyCollector(KeyEntryData keyEntryData)
{
    // ALWAYS handle Release
    if (keyEntryData.Request == KeyEntryRequest.Release)
    {
        // Zero any cached secrets, clean up UI
        return true;
    }

    // Check for retry and inform user
    if (keyEntryData.IsRetry)
    {
        ShowError($"Incorrect. {keyEntryData.RetriesRemaining} attempts remaining.");
        
        // Handle blocked state
        if (keyEntryData.RetriesRemaining == 0)
        {
            ShowError("PIN/PUK blocked!");
            return false; // Cancel
        }
    }

    // Dispatch based on request type
    return keyEntryData.Request switch
    {
        KeyEntryRequest.VerifyPivPin => HandlePinRequest(keyEntryData),
        KeyEntryRequest.ChangePivPin => HandleChangePinRequest(keyEntryData),
        KeyEntryRequest.ResetPivPinWithPuk => HandlePukRequest(keyEntryData),
        KeyEntryRequest.AuthenticatePivManagementKey => HandleManagementKeyRequest(keyEntryData),
        _ => false // Unknown request, cancel
    };
}

private bool HandlePinRequest(KeyEntryData keyEntryData)
{
    byte[]? pin = GetPinFromUser(); // Your UI logic
    if (pin is null)
        return false; // User cancelled
    
    try
    {
        keyEntryData.SubmitValue(pin);
        return true;
    }
    finally
    {
        CryptographicOperations.ZeroMemory(pin);
    }
}
```

## Test Infrastructure

### PIV Reset Pattern

Unlike Security Domain (which has automatic reset), PIV tests must **manually reset** the application to factory defaults:

```csharp
[Theory]
[WithYubiKey]
public void MyPivTest(YubiKeyTestState state)
{
    using var session = new PivSession(state.YubiKey);
    
    // ALWAYS reset to factory defaults at start of test
    session.ResetApplication();
    
    // Now you have clean state:
    // PIN = "123456"
    // PUK = "12345678"
    // Management Key = default 3DES key
    
    // Your test logic
    // ...
}
```

### Test Helper Extension (To Be Created)

When implementing test infrastructure, follow Security Domain's pattern:

```csharp
extension(YubiKeyTestState state)
{
    public void WithPivSession(
        Action<PivSession> action,
        bool resetBeforeUse = true,
        Func<KeyEntryData, bool>? keyCollector = null)
    {
        using var session = new PivSession(state.YubiKey);
        
        if (resetBeforeUse)
        {
            session.ResetApplication();
        }
        
        if (keyCollector is not null)
        {
            session.KeyCollector = keyCollector;
        }
        else
        {
            session.KeyCollector = DefaultTestKeyCollector;
        }
        
        action(session);
    }
    
    private bool DefaultTestKeyCollector(KeyEntryData keyEntryData)
    {
        if (keyEntryData.Request == KeyEntryRequest.Release)
            return true;
            
        // Provide default test credentials
        return keyEntryData.Request switch
        {
            KeyEntryRequest.VerifyPivPin => 
                SubmitDefaultPin(keyEntryData),
            KeyEntryRequest.AuthenticatePivManagementKey => 
                SubmitDefaultManagementKey(keyEntryData),
            _ => false
        };
    }
}
```

### Multi-Step Test Pattern

PIV tests often require multiple authentication steps:

```csharp
[Theory]
[WithYubiKey]
public void GenerateKeyAndSign_WithPinPolicy_RequiresPinEachTime(YubiKeyTestState state)
{
    state.WithPivSession(session =>
    {
        // Step 1: Authenticate management key (for key generation)
        var mgmtKey = GetDefaultManagementKey();
        Assert.True(session.TryAuthenticateManagementKey(mgmtKey));
        
        // Step 2: Generate key with PIN policy Always
        var publicKey = session.GenerateKeyPair(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            PivPinPolicy.Always);
        
        // Step 3: Sign (KeyCollector will be called for PIN)
        byte[] dataToSign = new byte[32];
        RandomNumberGenerator.Fill(dataToSign);
        
        var signature = session.Sign(PivSlot.Authentication, dataToSign);
        Assert.NotNull(signature);
        
        // Step 4: Sign again (PIN required again due to Always policy)
        // KeyCollector should be called again
        var signature2 = session.Sign(PivSlot.Authentication, dataToSign);
        Assert.NotNull(signature2);
    }, resetBeforeUse: true);
}
```

## Common Patterns

### Partial Classes

PIV session uses **partial classes** to organize functionality:

```csharp
// PivSession.cs - Main class, session management
public sealed partial class PivSession : IDisposable
{
    // Connection, disposal, etc.
}

// PivSession.KeyPairs.cs - Key and certificate operations
public sealed partial class PivSession : IDisposable
{
    public PivPublicKey GenerateKeyPair(...) { }
    public void ImportPrivateKey(...) { }
    public void ImportCertificate(...) { }
    public X509Certificate2 GetCertificate(...) { }
}

// PivSession.Crypto.cs - Cryptographic operations
public sealed partial class PivSession : IDisposable
{
    public byte[] Sign(...) { }
    public byte[] Decrypt(...) { }
    public byte[] KeyAgree(...) { }
}

// PivSession.Pin.cs - PIN/PUK/management key
public sealed partial class PivSession : IDisposable
{
    public bool TryVerifyPin(...) { }
    public void ChangePin(...) { }
    public void ResetPin(...) { }
    public bool TryAuthenticateManagementKey(...) { }
}
```

**Pattern:** Group related operations in separate partial class files for maintainability.

### Authentication State Tracking

PIV session tracks authentication state across operations:

```csharp
private bool _managementKeyAuthenticated;
private bool _pinVerified;

private void RefreshManagementKeyAuthentication()
{
    if (_managementKeyAuthenticated)
        return;
        
    AuthenticateManagementKey(); // Calls KeyCollector if needed
}

public PivPublicKey GenerateKeyPair(byte slotNumber, PivAlgorithm algorithm, ...)
{
    RefreshManagementKeyAuthentication(); // Ensure authenticated
    
    var command = new GenerateKeyPairCommand(slotNumber, algorithm, ...);
    var response = Connection.SendCommand(command);
    
    return response.GetData();
}
```

**Pattern:** Operations that require authentication automatically call `RefreshAuthentication()` to ensure the session is properly authenticated before proceeding.

### Response Status Handling

```csharp
var response = Connection.SendCommand(command);

if (response.Status != ResponseStatus.Success)
{
    // Specific error handling based on status
    if (response.Status == ResponseStatus.AuthenticationRequired)
    {
        // Re-authenticate and retry
    }
    else if (response.Status == ResponseStatus.Failed)
    {
        throw new InvalidOperationException(response.StatusMessage);
    }
}

return response.GetData();
```

## PIV-Specific Patterns

### Slot Number Validation

```csharp
private static void ValidateSlotNumber(byte slotNumber)
{
    bool isValid = slotNumber switch
    {
        0x9A => true, // Authentication
        0x9C => true, // Signing
        0x9D => true, // Key Management
        0x9E => true, // Card Authentication
        >= 0x82 and <= 0x95 => true, // Retired slots
        _ => false
    };
    
    if (!isValid)
        throw new ArgumentException($"Invalid PIV slot: 0x{slotNumber:X2}");
}
```

### Certificate Compression

YubiKey supports gzip compression for certificates > 1856 bytes:

```csharp
public void ImportCertificate(byte slotNumber, X509Certificate2 certificate, bool compress = false)
{
    byte[] certData = certificate.RawData;
    
    if (compress || certData.Length > 1856)
    {
        certData = CompressCertificate(certData);
    }
    
    // Store using PUT DATA command
    // ...
}

private static byte[] CompressCertificate(byte[] data)
{
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionMode.Compress))
    {
        gzip.Write(data, 0, data.Length);
    }
    return output.ToArray();
}
```

### PIN/PUK Blocking for Reset Tests

```csharp
private void BlockPinOrPuk(byte slotNumber)
{
    // Intentionally fail authentication until blocked
    byte[] wrongValue = new byte[8];
    RandomNumberGenerator.Fill(wrongValue);
    
    while (GetRetriesRemaining(slotNumber) > 0)
    {
        var command = new VerifyPinCommand(wrongValue);
        Connection.SendCommand(command);
    }
}

[Theory]
[WithYubiKey]
public void ResetPin_WithBlockedPin_Succeeds(YubiKeyTestState state)
{
    state.WithPivSession(session =>
    {
        // Block PIN
        BlockPinOrPuk(PivSlot.Pin);
        
        // Verify it's blocked
        Assert.Equal(0, session.GetPinRetriesRemaining());
        
        // Reset with PUK
        session.ResetPin(defaultPuk, newPin);
        
        // Verify PIN works now
        Assert.True(session.TryVerifyPin(newPin));
    }, resetBeforeUse: true);
}
```

## Firmware Version Considerations

Different YubiKey firmware versions support different features:

```csharp
// YubiKey 4 and later
if (yubiKey.FirmwareVersion >= FirmwareVersion.V4_0_0)
{
    // Can use RSA2048, RSA1024, EccP256, EccP384
}

// YubiKey 5.3 and later
if (yubiKey.FirmwareVersion >= FirmwareVersion.V5_3_0)
{
    // Can retrieve public key from slot anytime
    var publicKey = session.GetPublicKey(slotNumber);
}

// YubiKey 5.7 and later
if (yubiKey.FirmwareVersion >= FirmwareVersion.V5_7_0)
{
    // AES management key support
    session.SetManagementKey(aes128Key, PivAlgorithm.Aes128);
}
```

## Known Gotchas

1. **Management Key Required First**: Key generation/import requires management key authentication before any other operations
2. **PIN Verification Persists**: Once verified, PIN stays verified for the session unless an operation fails or session is disposed
3. **Touch Policy Timing**: `Cached` touch policy caches for 15 seconds after first touch
4. **Certificate Size Limits**: Standard PIV limit is 1856 bytes; YubiKey extends to 3052 bytes
5. **Default Credentials**: Many YubiKeys ship with default PIN/PUK/management key - **always change these**
6. **Retry Counter**: After PIN/PUK blocked, only factory reset can recover (unless using PUK to unblock PIN)
7. **Signing Slot**: Slot 0x9C (signing) typically requires PIN for each operation per PIV standard
8. **Private Key Never Leaves Device**: Cannot export private keys - they're generated/imported and remain on device

## Implementation Notes

When implementing new PIV features:

1. **Preserve Partial Class Organization**: Keep the logical separation (KeyPairs, Crypto, Authentication, Certificates, etc.)
2. **Modern Patterns**: Use `async`/`await`, `Memory<T>`, `Span<T>` for all new code
3. **Maintain KeyCollector Pattern**: This is fundamental to PIV security and should remain unchanged
4. **Test Coverage**: Add both unit tests and integration tests for new operations
5. **Command/Response Pattern**: Follow the established APDU command architecture

## Related Modules

- **Core.SmartCard**: Base smart card protocol implementations
- **Core.Cryptography**: ECPrivateKey, ECPublicKey, RSAPrivateKey, RSAPublicKey
- **Tests.Shared**: YubiKeyTestState, test infrastructure
- **Management**: YubiKey device management, firmware version detection

# CLAUDE.md - PIV Tests

This file provides guidance for the PIV module test infrastructure.

## Required Reading

**CRITICAL:** Read [`docs/TESTING.md`](../../../docs/TESTING.md) for test runner requirements. Key rule: **ALWAYS use `dotnet build.cs test` - NEVER use `dotnet test` directly.**

For PIV-specific test patterns, KeyCollector handling, and security considerations, see the **Test Infrastructure** and **Critical Security Requirements** sections in [`../CLAUDE.md`](../CLAUDE.md).

## Test Projects

- `Yubico.YubiKit.Piv.UnitTests` - Unit tests for PIV module (xUnit v3)
- `Yubico.YubiKit.Piv.IntegrationTests` - Integration tests requiring YubiKey hardware (xUnit v2)

## Security Requirements in Tests

PIV tests handle sensitive data (PINs, PUKs, management keys). Follow strict security hygiene:

```csharp
// ✅ ALWAYS zero PIN/PUK after use
Span<byte> pin = stackalloc byte[8];
try
{
    GetTestPin(pin);
    await session.VerifyPinAsync(pin);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}

// ❌ NEVER log sensitive data
_logger.LogDebug("PIN verified"); // ✅ OK
_logger.LogDebug($"PIN: {pin}"); // ❌ NEVER
```

## KeyCollector Pattern in Tests

Tests must implement KeyCollector delegates for PIN/PUK prompts:

```csharp
private bool TestKeyCollector(KeyEntryData keyEntryData)
{
    if (keyEntryData.Request == KeyEntryRequest.Release)
    {
        // Clean up cached secrets
        return true;
    }

    return keyEntryData.Request switch
    {
        KeyEntryRequest.VerifyPivPin => HandleTestPin(keyEntryData),
        KeyEntryRequest.AuthenticatePivManagementKey => HandleTestMgmtKey(keyEntryData),
        _ => false
    };
}
```

## Test Device Requirements

PIV integration tests require:
- YubiKey with PIV application enabled
- Known PIN/PUK/management key (or using defaults)
- Test device should be reset to factory defaults before test suite
- Tests should clean up generated keys/certificates after execution

## Running Tests

```bash
# Run all PIV tests
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Piv"

# Run unit tests only
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Piv.UnitTests"

# Run integration tests only (requires YubiKey)
dotnet build.cs test --filter "FullyQualifiedName~Yubico.YubiKit.Piv.IntegrationTests"

# Run specific test class
dotnet build.cs test --filter "FullyQualifiedName~PivSessionTests"
```

## Critical Test Warnings

- **Never commit real PINs/keys**: Use test-specific credentials
- **Reset after destructive tests**: Tests that modify slots should reset PIV app
- **Verify retry counters**: Tests should not permanently block PINs/PUKs
- **Clean up certificates**: Remove test certificates from slots after tests


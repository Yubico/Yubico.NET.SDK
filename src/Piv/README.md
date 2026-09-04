# Yubico.YubiKit.Piv

The PIV module provides async access to the YubiKey PIV application for certificates, keys, PIN/PUK management, attestation, signing, decryption, and key agreement.

## Session Creation

Use the `IYubiKey` extension when starting from a discovered device:

```csharp
await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
```

Use direct session creation when you already own a SmartCard connection:

```csharp
await using var connection = await device.ConnectAsync<ISmartCardConnection>(cancellationToken);
await using var session = await PivSession.CreateAsync(connection, cancellationToken: cancellationToken);
```

## Common Operations

### Verify PIN

```csharp
byte[] pin = Encoding.UTF8.GetBytes("123456");
try
{
    await session.VerifyPinAsync(pin, cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(pin);
}
```

### Authenticate Management Key

```csharp
byte[] managementKey = Convert.FromHexString("010203040506070801020304050607080102030405060708");
try
{
    await session.AuthenticateAsync(managementKey, cancellationToken);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}
```

`PivSession.DefaultManagementKey` exposes the well-known 24-byte default value used by both
Triple-DES and AES-192 defaults. Use `session.ManagementKeyType` to select the algorithm.
`session.IsManagementKeyAuthenticated` reports PIV management-key authentication; it is distinct
from the inherited `IsAuthenticated`, which reports application-protocol authentication such as SCP.

### Generate a Key

```csharp
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Authentication,
    PivAlgorithm.EccP256,
    new PivKeyCreationOptions
    {
        PinPolicy = PivPinPolicy.Once,
        TouchPolicy = PivTouchPolicy.Never
    },
    cancellationToken);
```

### Store and Read a Certificate

```csharp
await session.StoreCertificateAsync(
    PivSlot.Authentication,
    certificate,
    PivCertificateCompression.Automatic,
    cancellationToken);
var stored = await session.GetCertificateAsync(PivSlot.Authentication, cancellationToken);
```

### Sign or Decrypt

```csharp
byte[] digest = SHA256.HashData(data);
var signature = await session.SignOrDecryptAsync(
    PivSlot.Authentication,
    digest,
    cancellationToken);
```

### Key Agreement

```csharp
var sharedSecret = await session.CalculateSecretAsync(
    PivSlot.KeyManagement,
    peerPublicKey,
    cancellationToken);
```

### Retry Attempts

```csharp
var pinMetadata = await session.GetPinMetadataAsync(cancellationToken);
var pukMetadata = await session.GetPukMetadataAsync(cancellationToken);

await session.SetPinAttemptsAsync(
    pinAttempts: 5,
    pukAttempts: 5,
    cancellationToken);
```

### Biometric user verification

```csharp
var temporaryPin = await session.VerifyUvAsync(
    PivUserVerification.VerifyAndRequestTemporaryPin,
    cancellationToken);
```

Use `PivUserVerification.Verify` when no temporary PIN is needed and `CheckOnly` for the explicit check-only
mode. A returned temporary PIN is caller-owned secret material and must be zeroed after use.

## Security Notes

- PINs, PUKs, management keys, and private-key material are sensitive; zero caller-owned buffers after use.
- Enabling PIN-protected management-key mode re-authenticates the supplied key before PIN verification or persistent mutation.
- Disabling PIN-only mode restores the type-appropriate default key before deleting PRINTED, then ADMIN DATA, so failures leave a recoverable boundary.
- Mixed PIN-only recovery restores a successful PIN-protected authentication after a stale derived candidate fails, or returns no success mode if restoration fails.
- Successful management-key changes update `ManagementKeyType` and preserve card-session authentication without retaining key bytes. A `SecurityStatusNotSatisfied` response clears recorded authentication; unrelated SET failures preserve the prior authentication state and key type.
- Any failed management-key authentication attempt clears a previously recorded authenticated state.
- Initialization and reset refresh the default key type; unavailable metadata uses AES-192 only for reliable firmware 5.7+ and Triple-DES for major-zero/older versions, while unexpected reset refresh errors still propagate.
- Prefer `Span<byte>`, `Memory<byte>`, and `ReadOnlyMemory<byte>` over strings for secrets.
- Do not log PINs, PUKs, keys, plaintexts, or sensitive APDU payloads.
- Reset, PIN/PUK changes, management-key changes, key generation/import/delete, and certificate writes mutate persistent applet state.

## Testing Guidance

Unit tests should prefer fake SmartCard protocol or connection seams that assert APDU/TLV bytes and parser behavior.

Integration tests use `Tests.Shared` with standard xUnit `[Theory]` and `[WithYubiKey]`:

```csharp
[SkippableTheory]
[WithYubiKey(Capability = DeviceCapabilities.Piv)]
public async Task GetPinMetadata_ReadOnly_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    var metadata = await session.GetPinMetadataAsync();
    Assert.NotNull(metadata);
}
```

PIV reset, PIN/PUK mutation, management-key mutation, and key/certificate writes are expected against an allow-listed test device — that is what the harness is for. What needs a human is presence and timing, not destruction: touch-policy ceremonies and physical insert/remove. See [docs/TESTING.md](../../docs/TESTING.md#hardware-authorization).

## Related Example

The interactive PIV sample lives at `src/Piv/examples/PivTool/`.

From the repository root:

```bash
dotnet run --project src/Piv/examples/PivTool/PivTool.csproj
```

## Related Modules

- `Yubico.YubiKit.Core` - SmartCard protocol, APDU, TLV, and cryptography primitives.
- `Yubico.YubiKit.Management` - device information and firmware source of truth.
- `Yubico.YubiKit.Tests.Shared` - hardware allow-list and integration-test helpers.

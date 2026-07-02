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

### Generate a Key

```csharp
var publicKey = await session.GenerateKeyAsync(
    PivSlot.Authentication,
    PivAlgorithm.EccP256,
    PivPinPolicy.Once,
    PivTouchPolicy.Never,
    cancellationToken);
```

### Store and Read a Certificate

```csharp
await session.StoreCertificateAsync(PivSlot.Authentication, certificate, cancellationToken);
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

## Security Notes

- PINs, PUKs, management keys, and private-key material are sensitive; zero caller-owned buffers after use.
- Prefer `Span<byte>`, `Memory<byte>`, and `ReadOnlyMemory<byte>` over strings for secrets.
- Do not log PINs, PUKs, keys, plaintexts, or sensitive APDU payloads.
- Reset, PIN/PUK changes, management-key changes, key generation/import/delete, and certificate writes mutate persistent applet state.

## Testing Guidance

Unit tests should prefer fake SmartCard protocol or connection seams that assert APDU/TLV bytes and parser behavior.

Integration tests use `Tests.Shared` with standard xUnit `[Theory]` and `[WithYubiKey]`:

```csharp
[Theory]
[WithYubiKey(Capability = DeviceCapabilities.Piv)]
public async Task GetPinMetadata_ReadOnly_Succeeds(YubiKeyTestState state)
{
    await using var session = await state.Device.CreatePivSessionAsync();
    var metadata = await session.GetPinMetadataAsync();
    Assert.NotNull(metadata);
}
```

Do not run PIV reset, PIN/PUK mutation, management-key mutation, or key/certificate write integration tests without human-coordinated hardware approval.

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

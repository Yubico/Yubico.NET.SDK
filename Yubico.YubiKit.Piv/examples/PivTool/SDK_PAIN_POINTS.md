# SDK Pain Points Discovered During PivTool Development

This document captures usability issues and improvement opportunities discovered while implementing the PivTool example application.

## Pain Points

### 1. SignOrDecryptAsync API Ergonomics ✅ FIXED
**Issue**: The `SignOrDecryptAsync` method requires passing the algorithm explicitly, even though the slot metadata already contains this information.

**Resolution**: Added overload `SignOrDecryptAsync(PivSlot slot, ReadOnlyMemory<byte> data, CancellationToken)` that auto-detects the algorithm from slot metadata. Requires firmware 5.3+.

```csharp
// New simplified API (firmware 5.3+)
var result = await session.SignOrDecryptAsync(slot, data, cancellationToken);
```

---

### 2. Nullable PivSlotMetadata Return Type (Deferred)
**Issue**: `GetSlotMetadataAsync` returns `PivSlotMetadata?` (nullable struct), requiring `.Value` access after null check.

**Status**: Deferred to future release. The nullable pattern is intentional to distinguish between "slot is empty" (null) and "slot has a key" (non-null). Changing this would be a breaking API change.

**Workaround**: Use pattern matching:
```csharp
if (await session.GetSlotMetadataAsync(slot) is { } metadata)
{
    // metadata is non-null here
}
```

---

### 3. Management Key Default Value Not Exposed ✅ FIXED
**Issue**: The default management key value is not exposed as a constant, requiring developers to hardcode it.

**Resolution**: Added `PivSession.DefaultManagementKey` property (ReadOnlySpan<byte>).

```csharp
// Authenticate with default key
await session.AuthenticateAsync(PivSession.DefaultManagementKey);
```

---

### 4. Certificate Creation Requires External Public Key ✅ FIXED
**Issue**: Generating a self-signed certificate for a slot requires the public key, but there's no direct way to get the public key bytes in a usable format for `CertificateRequest`.

**Resolution**: Added extension methods in `PivSlotMetadataExtensions`:
- `GetRsaPublicKey()` - Returns `RSA` instance for RSA keys
- `GetECDsaPublicKey()` - Returns `ECDsa` instance for P-256/P-384 keys
- `IsRsa()` / `IsEcc()` - Algorithm type helpers

```csharp
var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
using var rsa = metadata.Value.GetRsaPublicKey();
var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
```

**Note**: Ed25519/X25519 curves are not supported by .NET's ECDsa class.

---

### 5. No Progress/Touch Callback for Long Operations ✅ FIXED
**Issue**: Operations requiring touch provide no callback mechanism to prompt users.

**Resolution**: Added `OnTouchRequired` callback property to `PivSession`:

```csharp
session.OnTouchRequired = () => Console.WriteLine("Touch your YubiKey now...");
await session.SignOrDecryptAsync(slot, data, cancellationToken);
```

The callback fires before operations with `TouchPolicy.Always` or `TouchPolicy.Cached`. On firmware < 5.3, it fires conservatively for all crypto operations.

**Security Note**: The callback receives NO operation context (no slot, algorithm, or data) to prevent information leakage.

---

### 6. Inconsistent Async Disposal ✅ FIXED
**Issue**: `ManagementSession` uses `IDisposable` while `PivSession` uses `IAsyncDisposable`, leading to mixed `using` and `await using` patterns.

**Resolution**: `ManagementSession` now implements `IAsyncDisposable`:

```csharp
await using var management = ManagementSession.Create(connection);
await using var piv = await PivSession.CreateAsync(connection);
```

---

## Positive SDK Aspects

- Device discovery API (`YubiKey.FindAllAsync()`) is clean and intuitive
- PIN/PUK/Management key metadata APIs provide useful information
- Key generation with policy configuration is well-designed
- Attestation API is straightforward

## Summary

| Pain Point | Status |
|------------|--------|
| SignOrDecryptAsync ergonomics | ✅ Fixed |
| Nullable PivSlotMetadata | ⏳ Deferred (intentional design) |
| Default management key | ✅ Fixed |
| Public key extraction | ✅ Fixed |
| Touch callback | ✅ Fixed |
| Async disposal consistency | ✅ Fixed |

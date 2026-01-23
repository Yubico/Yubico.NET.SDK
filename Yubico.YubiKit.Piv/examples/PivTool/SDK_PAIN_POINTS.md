# SDK Pain Points Discovered During PivTool Development

This document captures usability issues and improvement opportunities discovered while implementing the PivTool example application.

## Pain Points

### 1. SignOrDecryptAsync API Ergonomics
**Issue**: The `SignOrDecryptAsync` method requires passing the algorithm explicitly, even though the slot metadata already contains this information.

**Current API**:
```csharp
var result = await session.SignOrDecryptAsync(slot, slotMetadata.Algorithm, data, cancellationToken);
```

**Suggested Improvement**: Consider an overload that automatically retrieves the algorithm from slot metadata:
```csharp
var result = await session.SignOrDecryptAsync(slot, data, cancellationToken);
```

**Impact**: Reduces boilerplate and chance of algorithm mismatch errors.

---

### 2. Nullable PivSlotMetadata Return Type
**Issue**: `GetSlotMetadataAsync` returns `PivSlotMetadata?` (nullable struct), requiring `.Value` access after null check.

**Example**:
```csharp
var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
if (metadata is null) { return; }
var slotMetadata = metadata.Value; // Extra step
```

**Suggested Improvement**: Return a non-nullable struct with an `IsEmpty` property, or use a record class.

---

### 3. Management Key Default Value Not Exposed
**Issue**: The default management key value is not exposed as a constant, requiring developers to hardcode it.

**Current Workaround**:
```csharp
byte[] defaultKey = [0x01, 0x02, 0x03, ...]; // Hardcoded
```

**Suggested Improvement**: Expose `PivSession.DefaultManagementKey` constant.

---

### 4. Certificate Creation Requires External Public Key
**Issue**: Generating a self-signed certificate for a slot requires the public key, but there's no direct way to get the public key bytes in a usable format for `CertificateRequest`.

**Current Workaround**: Must have an existing certificate in the slot to extract the public key.

**Suggested Improvement**: Add `GetPublicKeyAsync(slot)` that returns an `AsymmetricAlgorithm` compatible with `CertificateRequest`.

---

### 5. No Progress/Touch Callback for Long Operations
**Issue**: Operations requiring touch provide no callback mechanism to prompt users.

**Impact**: Developers must implement timing-based prompts or hope users remember to touch.

**Suggested Improvement**: Add optional `IProgress<PivOperationProgress>` parameter with touch-required notifications.

---

### 6. Inconsistent Async Disposal
**Issue**: `ManagementSession` uses `IDisposable` while `PivSession` uses `IAsyncDisposable`, leading to mixed `using` and `await using` patterns.

**Example**:
```csharp
using var management = ManagementSession.Create(connection);  // sync
await using var piv = await PivSession.CreateAsync(connection);  // async
```

**Suggested Improvement**: Align all session types to use `IAsyncDisposable`.

---

## Positive SDK Aspects

- Device discovery API (`YubiKey.FindAllAsync()`) is clean and intuitive
- PIN/PUK/Management key metadata APIs provide useful information
- Key generation with policy configuration is well-designed
- Attestation API is straightforward

## Recommendations

1. Consider adding convenience overloads for common patterns
2. Expose more constants for default values
3. Add progress/notification callbacks for touch-required operations
4. Consider adding a high-level certificate builder API
5. Unify disposal patterns across session types

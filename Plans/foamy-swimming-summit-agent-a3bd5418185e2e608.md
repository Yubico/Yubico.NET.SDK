# Deferred Code Audit Fix Analysis

## 1. Oath: CredentialData IDisposable

### What changes
- `CredentialData` gains `IDisposable`, zeroing `Secret` in `Dispose()`.

### Problem with IDisposable + `init`-only property
The `Secret` property is `required byte[] { get; init; }`. Since `init` allows setting only during object initialization, `IDisposable` works fine mechanically -- the class owns the array reference after init. The real question is ownership: who allocated the `byte[]` that `Secret` points to?

- **`ParseUri()`** -- allocates `Secret` internally via `ParseBase32Key()`. `CredentialData` clearly owns it.
- **Manual construction** -- caller passes `Secret = someArray`. Caller may or may not retain a reference. If caller retains a reference and `Dispose()` zeros it, that's a surprise.

### Callers (exhaustive search)
| File | Usage Pattern | Needs `using`? |
|------|---------------|----------------|
| `src/Oath/src/OathSession.cs:196-283` (`PutCredentialAsync`) | Receives `CredentialData`, calls `GetProcessedSecret()` and `GetId()`. Does NOT own it. | No -- session doesn't own it |
| `src/Cli.Commands/src/Oath/OathCommands.cs` | Creates `CredentialData` (likely via `ParseUri`), passes to `PutCredentialAsync` | Yes -- creator should dispose |
| `src/Oath/examples/OathTool/Commands/AccountsCommand.cs` | Creates `CredentialData`, passes to session | Yes |
| `src/Oath/tests/.../CredentialDataTests.cs` | Unit tests creating instances | Yes, or use `try/finally` |
| `src/Oath/tests/.../OathHashAlgorithmTests.cs` | Integration tests | Yes |
| `src/Oath/tests/.../OathSessionTests.cs` | Integration tests | Yes |

### GetProcessedSecret / HmacShortenKey intermediates
- `GetProcessedSecret()` calls `HmacShortenKey(Secret, HashAlgorithm)` which returns a new `byte[]` (either the original or a hashed copy), then `PadSecret()` which returns either the input or a new padded array.
- `PutCredentialAsync` already zeros the returned `secret` in its `finally` block (line 282: `CryptographicOperations.ZeroMemory(secret)`).
- `HmacShortenKey` returns either the original `key` reference (if short enough) or a new `SHA*.HashData(key)` array. When it returns the original, `PutCredentialAsync`'s zero hits the `Secret` property's backing array -- which is fine since the session is done with it.
- No intermediate leak: `shortened` is either `Secret` itself or a fresh array; `PadSecret` either returns `shortened` or a new padded array. The final `secret` returned from `GetProcessedSecret` gets zeroed. The only remaining sensitive data is `Secret` itself.

### Recommendation: DO IT, but document ownership
```csharp
public sealed class CredentialData : IDisposable
{
    // ... existing members ...
    
    public void Dispose()
    {
        if (Secret is not null)
        {
            CryptographicOperations.ZeroMemory(Secret);
        }
    }
}
```

**Risk: LOW.** The pattern is simple and callers are few. The `init`-only property is fine -- `IDisposable` governs cleanup timing, not mutability. Document in XML docs that `Dispose()` zeros the `Secret` array and callers should not retain separate references.

---

## 2. Oath: DeriveKey / ValidateAsync / SetKeyAsync API

### Current signatures
```csharp
byte[] DeriveKey(ReadOnlyMemory<byte> passwordUtf8);    // returns derived key
Task ValidateAsync(byte[] key, CancellationToken ct);    // consumes key
Task SetKeyAsync(byte[] key, CancellationToken ct);      // consumes key
```

### What should change?
The `byte[]` parameters on `ValidateAsync`/`SetKeyAsync` should become `ReadOnlyMemory<byte>` for consistency. The return of `DeriveKey` is trickier.

**`DeriveKey` return type options:**
1. **`byte[]`** (current) -- caller zeroes when done. Simple, but easy to forget.
2. **`IMemoryOwner<byte>`** -- forces `using`, zeroes on dispose. But `MemoryPool.Shared` doesn't guarantee zeroing. Would need a custom `IMemoryOwner` that zeros.
3. **Keep `byte[]`**, document zeroing responsibility -- pragmatic, consistent with rest of codebase.

**Recommendation:** Change `ValidateAsync`/`SetKeyAsync` to `ReadOnlyMemory<byte>`, keep `DeriveKey` returning `byte[]` with clear documentation. The entire codebase uses `byte[]` returns for derived keys (e.g., `PBKDF2`, `HMAC`). A custom `IMemoryOwner` is over-engineering for this one callsite.

### Callers
| File | Current Call | Migration |
|------|-------------|-----------|
| `src/Oath/src/OathSession.cs:498` | `ValidateAsync(byte[] key, ...)` | Already zeros in finally |
| `src/Oath/src/OathSession.cs:564` | `SetKeyAsync(byte[] key, ...)` | Already zeros in finally |
| `src/Cli.Commands/src/Oath/OathCommands.cs` | Calls `DeriveKey`, `ValidateAsync`, `SetKeyAsync` | Needs `ReadOnlyMemory` adapter |
| `src/Cli.Commands/src/Oath/OathHelpers.cs` | Helper for key derivation | Minor signature update |
| `src/Oath/examples/OathTool/Commands/AccessCommand.cs` | `DeriveKey` + `ValidateAsync`/`SetKeyAsync` | Update to pass `ReadOnlyMemory` |
| `src/Oath/examples/OathTool/Cli/SessionHelper.cs` | Session helper | Update |
| `src/Oath/tests/.../OathSessionTests.cs` (unit + integration) | Various calls | Update test signatures |
| `src/Oath/tests/.../OathPasswordChangeTests.cs` | Password change tests | Update |

### Interface change
```csharp
// IOathSession.cs
byte[] DeriveKey(ReadOnlyMemory<byte> passwordUtf8);  // unchanged
Task ValidateAsync(ReadOnlyMemory<byte> key, CancellationToken ct = default);  // byte[] -> ROM<byte>
Task SetKeyAsync(ReadOnlyMemory<byte> key, CancellationToken ct = default);    // byte[] -> ROM<byte>
```

**Risk: LOW-MEDIUM.** The signature change is source-breaking but mechanically simple. `byte[]` implicitly converts to `ReadOnlyMemory<byte>`, so most callers compile without changes. The OathSession implementations already handle zeroing internally.

---

## 3. Fido2: Encapsulate DRY V1/V2

### Identical code between V1 and V2 `Encapsulate`
Comparing `PinUvAuthProtocolV1.Encapsulate` (lines 68-137) and `PinUvAuthProtocolV2.Encapsulate` (lines 75-144):

**Identical sections (lines 74-136 in V1, 79-143 in V2):**
1. Disposed check + null check
2. Extract and validate peerX/peerY from COSE key (30 lines)
3. Generate ephemeral ECDH key pair
4. Import peer public key
5. DeriveRawSecretAgreement
6. Call `Kdf(z)` (polymorphic -- V1 does SHA256, V2 does HKDF)
7. ZeroMemory(z)
8. Build COSE key agreement dictionary

**Only difference:** `Kdf(z)` call dispatches to the respective version's implementation. Everything else is character-for-character identical.

### Recommended extraction
A `static` helper in a new file `PinUvAuthHelpers.cs` (or internal static class):

```csharp
namespace Yubico.YubiKit.Fido2.Pin;

internal static class PinUvAuthHelpers
{
    // Constants (shared)
    internal const int CoseKeyType = 1;
    internal const int CoseAlgorithm = 3;
    internal const int CoseEC2Curve = -1;
    internal const int CoseEC2X = -2;
    internal const int CoseEC2Y = -3;
    internal const int CoseKeyTypeEC2 = 2;
    internal const int CoseAlgEcdhEsHkdf256 = -25;
    internal const int CoseEC2CurveP256 = 1;

    internal static (Dictionary<int, object?> KeyAgreement, byte[] RawZ) 
        PerformEcdhKeyAgreement(IReadOnlyDictionary<int, object?> peerCoseKey)
    {
        // Validate peer key, generate ephemeral, derive Z, build COSE key
        // Returns raw Z (caller applies their own KDF and zeroes Z)
    }
}
```

Then each protocol's `Encapsulate` becomes:
```csharp
public (Dictionary<int, object?>, byte[]) Encapsulate(IReadOnlyDictionary<int, object?> peerCoseKey)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    var (keyAgreement, z) = PinUvAuthHelpers.PerformEcdhKeyAgreement(peerCoseKey);
    var sharedSecret = Kdf(z);
    CryptographicOperations.ZeroMemory(z);
    return (keyAgreement, sharedSecret);
}
```

### Other duplicated methods?
- `Encrypt`/`Decrypt` -- **NOT duplicated**. V1 uses zero IV, V2 uses random IV and prepends it. Different logic.
- `Authenticate` -- Different truncation (V1: 16 bytes, V2: 32 bytes) and key slicing. Not worth extracting.
- `Verify` -- Identical pattern but trivial (5 lines). Not worth extracting.
- **COSE constants** -- Duplicated in both files. Extract to shared helper.

### Risk: LOW. 
Pure refactor, no behavioral change. Static helper, no inheritance complications. The ECDH ceremony is deterministic and well-tested.

---

## 4. Fido2: CBOR Construction Pattern

### Analysis of payload construction across 4 classes

**AuthenticatorConfig** (`BuildCommandPayload`, `BuildSetMinPinLengthPayload`):
- Auth message: `32*0xFF || commandByte || subCommand [|| subCommandParams]`
- CBOR keys: 1=subCommand, 2=subCommandParams, 3=pinUvAuthProtocol, 4=pinUvAuthParam

**CredentialManagement** (`BuildCommandPayload`, `BuildEnumerateCredentialsPayload`, etc.):
- Auth message: `subCommand [|| subCommandParams]` (NO 0xFF prefix, NO command byte)
- CBOR keys: 1=subCommand, 2=subCommandParams, 3=pinUvAuthProtocol, 4=pinUvAuthParam

**FingerprintBioEnrollment** (`BuildEnrollBeginPayload`, `BuildEnumerateEnrollmentsPayload`, etc.):
- Auth message: `subCommand [|| subCommandParams]` (NO 0xFF prefix, NO command byte)
- CBOR keys: 1=modality, 2=subCommand, 3=subCommandParams, 4=pinUvAuthProtocol, 5=pinUvAuthParam
- Extra: modality field (always fingerprint=1), timeout field

**LargeBlobStorage** (`WriteFragmentAsync`):
- Auth message: `32*0xFF || 0x0C || 0x00 || uint32LE(offset) || SHA256(data)` (completely unique)
- CBOR keys: Different structure entirely (set, offset, length, pinUvAuthParam, pinUvAuthProtocol)

### Assessment
The four classes have **three different auth message formats** and **three different CBOR structures**. A shared helper would need parameters for:
- Whether to include 0xFF prefix
- Whether to include command byte in auth message
- Whether to include modality
- Variable CBOR key assignments
- Optional extra fields (timeout, offset, length)

**Recommendation: SKIP -- the abstraction cost exceeds the benefit.**

The "common" part is essentially:
1. Compute `pinUvAuthParam = protocol.Authenticate(token, message)`
2. Write a CBOR map with subCommand, protocol version, and authParam

That's 6-8 lines of CBOR writing per call site. A shared helper would need ~15 lines of parameter setup to save ~6 lines of CBOR writing. The auth message construction differs significantly between AuthenticatorConfig (0xFF prefix), CredentialManagement (bare subCommand), BioEnrollment (bare subCommand), and LargeBlobs (completely custom). Extracting this would make each call site harder to audit against the CTAP spec.

**Risk of extraction: MEDIUM** (spec compliance harder to verify, parameter explosion).
**Risk of leaving: LOW** (repetition is mechanical, each class follows its spec section clearly).

---

## 5. PIV: Retry Count Extraction

### Current state
`SWConstants.ExtractRetryCount(short sw)` already exists in Core (line 184):
```csharp
public static int? ExtractRetryCount(short sw) =>
    IsVerifyFailWithRetries(sw) ? sw & 0x0F : null;
```

### Inline extraction sites in PIV

**Site 1: `PivSession.Authentication.cs:460-463` (VerifyPinAsync)**
```csharp
if ((response.SW & 0xFFF0) == SWConstants.VerifyFail)
{
    var retriesRemaining = (int)(response.SW & 0x0F);
    throw new InvalidPinException(retriesRemaining);
}
if (response.SW == SWConstants.AuthenticationMethodBlocked)
{
    throw new InvalidPinException(0, "PIN is blocked. Use PUK to unblock.");
}
```
**Becomes:**
```csharp
if (SWConstants.ExtractRetryCount(response.SW) is { } retries)
    throw new InvalidPinException(retries);
if (response.SW == SWConstants.AuthenticationMethodBlocked)
    throw new InvalidPinException(0, "PIN is blocked. Use PUK to unblock.");
```

**Site 2: `PivSession.Authentication.cs:513-515` (GetPinAttemptsAsync)**
```csharp
if ((response.SW & 0xFFF0) == SWConstants.VerifyFail)
    return (int)(response.SW & 0x0F);
if (response.SW == SWConstants.AuthenticationMethodBlocked)
    return 0;
```
**Becomes:**
```csharp
if (SWConstants.ExtractRetryCount(response.SW) is { } retries)
    return retries;
if (response.SW == SWConstants.AuthenticationMethodBlocked)
    return 0;
```

**Site 3: `PivSession.Authentication.cs:574-576` (ChangePinAsync)**
Same pattern as Site 1. Same transformation.

**Site 4: `PivSession.Bio.cs:108-110` (VerifyUvAsync)**
Uses raw `0x63C0` instead of `SWConstants.VerifyFail`:
```csharp
if ((response.SW & 0xFFF0) == 0x63C0)
{
    var retriesRemaining = response.SW & 0x0F;
```
**Becomes:** Same pattern using `SWConstants.ExtractRetryCount`.

**Site 5: `PivSession.Bio.cs:170-172` (VerifyTemporaryPinAsync)**
Same as Site 4.

**Sites 6-9: Via `PivPinUtilities.GetRetriesFromStatusWord()`**
Called at:
- `PivSession.Metadata.cs:260` (ChangePukAsync)
- `PivSession.Metadata.cs:298` (UnblockPinAsync)
- `PivSession.cs:324` (BlockPinAsync or similar)
- `PivSession.cs:359` (similar)

### Should `GetRetriesFromStatusWord` delegate or be removed?
`PivPinUtilities.GetRetriesFromStatusWord(int statusWord)` handles both `0x6983` (returns 0) and `0x63Cx` (returns retries). It also returns `-1` for unrecognized SWs.

`SWConstants.ExtractRetryCount(short sw)` only handles `0x63Cx` (returns `int?`, null for non-match). It does NOT handle `0x6983`.

**Recommendation:**
- Make `GetRetriesFromStatusWord` delegate to `SWConstants.ExtractRetryCount` for the `0x63Cx` case
- Keep the `0x6983` case as a separate check (it's semantically different -- "blocked" vs "retries remaining")
- OR add `SWConstants.IsBlocked(short sw)` to Core for completeness
- The inline sites in Authentication.cs and Bio.cs should use `SWConstants.ExtractRetryCount` + explicit `AuthenticationMethodBlocked` check

```csharp
// Updated PivPinUtilities.GetRetriesFromStatusWord
public static int GetRetriesFromStatusWord(int statusWord)
{
    if (statusWord == SWConstants.AuthenticationMethodBlocked)
        return 0;
    return SWConstants.ExtractRetryCount((short)statusWord) ?? -1;
}
```

**Risk: LOW.** Pure mechanical refactor. No behavioral change. All 9 sites become more readable and reference the canonical Core implementation.

---

## 6. PIV: List<byte> to Span/ArrayPool in APDU construction

### Sites

**Site 1: `PivSession.Certificates.cs:118-138` (StoreCertificateAsync)**
Uses `List<byte>` with `.Add()`, `.AddRange()`, then `.ToArray()` for building TLV data (cert + info + LRC tags).

**Site 2: `PivSession.DataObjects.cs:99-119` (PutObjectAsync)**
Uses `List<byte>` with `.AddRange()` and `.Add()` for building TAG 0x5C + TAG 0x53 wrapper.

### Better pattern: `ArrayBufferWriter<byte>`
`ArrayBufferWriter<byte>` is the modern replacement for `List<byte>` as a byte accumulator:
- Implements `IBufferWriter<byte>` -- can be used with `Span`-based writers
- `.WrittenSpan` / `.WrittenMemory` avoids the `.ToArray()` allocation
- Already used elsewhere in this codebase (`OathSession.CollectResponseData`)

```csharp
// Before (StoreCertificateAsync)
var dataList = new List<byte>();
dataList.Add(0x70);
dataList.AddRange(certLenBuf);
dataList.AddRange(certBytes);
// ...
await PutObjectAsync(objectId, dataList.ToArray(), ct);

// After
var buffer = new ArrayBufferWriter<byte>(certBytes.Length + 16);
buffer.Write([0x70]);
buffer.Write(certLenBuf);
buffer.Write(certBytes);
buffer.Write([0x71, 0x01, (byte)(shouldCompress ? 0x01 : 0x00)]);
buffer.Write([0xFE, 0x00]);
await PutObjectAsync(objectId, buffer.WrittenMemory, ct);
```

### PutObjectAsync signature needs updating
Currently `PutObjectAsync` takes `ReadOnlyMemory<byte>?`. The `List<byte>.ToArray()` already produces a `byte[]` which implicitly converts. With `ArrayBufferWriter`, we'd pass `.WrittenMemory` directly -- no change needed to the signature.

### Assessment
Only 2 sites. The conversion is clean but small. `List<byte>` works fine here -- these are not hot paths (certificate storage is rare).

**Recommendation: DO IT, but low priority.** The `ArrayBufferWriter` version is cleaner and avoids the `.ToArray()` copy. Each site is ~15 lines to convert. No caller impact since it's internal to the methods.

**Risk: LOW.** No signature changes, no caller impact. Pure internal refactor.

---

## 7. YubiOtp: UpdateConfiguration duplication

### Analysis
`UpdateConfiguration` and `KeyboardSlotConfiguration` have nearly identical methods:
- `AppendCr`, `TabFirst`, `AppendTab1`, `AppendTab2`, `AppendDelay1`, `AppendDelay2`
- `FastTrigger`, `PacingChar10`, `PacingChar20`, `UseNumericKeypad`

Each method is 4-5 lines: call `SetTktFlag`/`SetExtFlag`/`SetCfgFlag` and return `this` (typed as the concrete class).

### Why they can't share
The fluent return type differs:
- `KeyboardSlotConfiguration.AppendCr()` returns `KeyboardSlotConfiguration`
- `UpdateConfiguration.AppendCr()` returns `UpdateConfiguration`

`UpdateConfiguration` does NOT extend `KeyboardSlotConfiguration` -- it extends `SlotConfiguration` directly. This is by design: `UpdateConfiguration` only allows updating flags (not setting key material), while `KeyboardSlotConfiguration` is for full slot programming.

### Generic self-referencing pattern (CRTP)
```csharp
public abstract class FluentSlotConfiguration<TSelf> : SlotConfiguration
    where TSelf : FluentSlotConfiguration<TSelf>
{
    public TSelf AppendCr(bool enable = true) { SetTktFlag(TicketFlag.AppendCr, enable); return (TSelf)this; }
    // etc.
}
```

This would require `KeyboardSlotConfiguration` and `UpdateConfiguration` to both inherit from `FluentSlotConfiguration<T>`. But `KeyboardSlotConfiguration` is already `abstract` and has subclasses (`YubiOtpSlotConfiguration`, `HotpSlotConfiguration`, etc.) that need their own fluent return types. The CRTP doesn't compose well across three levels of inheritance.

### Recommendation: SKIP -- acceptable duplication
The methods are trivial (1 line of logic + return). The duplication is 11 methods x 4 lines = ~44 lines. Any DRY solution (CRTP, interface default methods, extension methods) would:
1. Add complexity to the type hierarchy
2. Break the fluent chaining for subclasses of `KeyboardSlotConfiguration`
3. Be harder to understand than the current flat duplication

**Risk of leaving: NEGLIGIBLE.** The flag-setting logic is in the base `SlotConfiguration` class. The duplicated methods are pure delegation + fluent return. They won't diverge because they call the same base methods.

---

## 8. SecurityDomain: DI delegate missing firmwareVersion

### Current delegate
```csharp
// src/SecurityDomain/src/DependencyInjection.cs
public delegate Task<SecurityDomainSession> SecurityDomainSessionFactory(
    ISmartCardConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParams,
    CancellationToken cancellationToken);
```

### Current factory implementation
```csharp
services.TryAddSingleton<SecurityDomainSessionFactory>(
    (conn, cfg, scp, ct) => SecurityDomainSession.CreateAsync(conn, cfg, scp, cancellationToken: ct));
```

### SecurityDomainSession.CreateAsync signature
Let me check what parameters `CreateAsync` actually accepts:

The `CreateAsync` method (from `SecurityDomainSession`) accepts:
- `ISmartCardConnection connection`
- `ProtocolConfiguration? configuration`
- `ScpKeyParameters? scpKeyParams`
- `FirmwareVersion? firmwareVersion = null` (defaults to `V5_3_0`)
- `CancellationToken cancellationToken`

The delegate is **missing the `FirmwareVersion?` parameter**. This means DI consumers cannot pass a firmware version, forcing the session to always use the default `V5_3_0`.

### Who calls this factory?
The `SecurityDomainSessionFactory` delegate is registered in DI but I need to check if any code resolves it. In practice, most code uses `SecurityDomainSession.CreateAsync()` directly. The DI factory is for integration with ASP.NET/hosted service patterns.

### Recommended fix
```csharp
public delegate Task<SecurityDomainSession> SecurityDomainSessionFactory(
    ISmartCardConnection connection,
    ProtocolConfiguration? configuration,
    ScpKeyParameters? scpKeyParams,
    FirmwareVersion? firmwareVersion,    // NEW
    CancellationToken cancellationToken);
```

And the registration:
```csharp
services.TryAddSingleton<SecurityDomainSessionFactory>(
    (conn, cfg, scp, fw, ct) => SecurityDomainSession.CreateAsync(conn, cfg, scp, fw, ct));
```

### Impact
Adding a parameter to the delegate is a **source-breaking change** for any code that:
1. Implements the delegate (assigns a lambda matching the old signature)
2. Calls the delegate (passes 4 args instead of 5)

Since this is a new SDK (not yet released publicly), breaking changes are acceptable now.

### Other session factories for comparison
Check if other modules (Oath, Fido2, YubiOtp, Management) have similar DI delegates. If they do and include firmware version, this is clearly a consistency fix.

**Risk: LOW.** New SDK, source-breaking is acceptable. The fix aligns the DI delegate with the actual `CreateAsync` capabilities. Without this, firmware-version-gated behavior (APDU sizes, SCP11 support) cannot be configured via DI.

---

## Summary: Priority and Effort

| # | Item | Recommendation | Risk | Effort | Priority |
|---|------|---------------|------|--------|----------|
| 1 | Oath CredentialData IDisposable | DO IT | Low | Small | Medium |
| 2 | Oath DeriveKey/Validate/SetKey API | DO IT (ROM<byte> params) | Low-Med | Medium | Medium |
| 3 | Fido2 Encapsulate DRY | DO IT (static helper) | Low | Small | Low |
| 4 | Fido2 CBOR Construction | SKIP | Med | Large | N/A |
| 5 | PIV Retry Count Extraction | DO IT | Low | Small | High |
| 6 | PIV List<byte> to ArrayBufferWriter | DO IT, low priority | Low | Small | Low |
| 7 | YubiOtp UpdateConfiguration DRY | SKIP | Negligible | Medium | N/A |
| 8 | SecurityDomain DI firmwareVersion | DO IT | Low | Small | Medium |

### Recommended execution order
1. **#5 (PIV retry count)** -- highest value, lowest risk, purely mechanical
2. **#8 (SD DI delegate)** -- quick fix, consistency
3. **#1 (Oath CredentialData)** -- security improvement
4. **#2 (Oath API signatures)** -- breaking change, do with #1
5. **#3 (Fido2 Encapsulate)** -- nice cleanup
6. **#6 (PIV ArrayBufferWriter)** -- polish
7. **#4 and #7** -- skip, cost exceeds benefit

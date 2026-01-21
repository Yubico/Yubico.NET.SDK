# SmartCard Disposal & Robustness Improvements - Implementation Plan

> **Goal**: Fix disposal-related issues that cause SmartCard unavailability after test failures. Improve robustness without breaking the SDK for future releases.

---

## Problem Summary

When tests fail (or any exception path), the `UsbSmartCardConnection` disposal chain can leave the SmartCard in an unavailable state. Root causes:

1. **Default `RESET_CARD` disposition**: `SCardCardHandle.ReleaseDisposition` defaults to `RESET_CARD`, which resets the card on every disconnect. This is aggressive and can leave the card in a transitional state.

2. **Resource leak in `GetConnection`**: If `SCardConnect` fails after `SCardEstablishContext` succeeds, the context handle leaks.

3. **Missing transaction cleanup**: Per `SCARD-Improvements.md`, active transactions should be ended before disconnecting the card handle.

4. **No async disposal support**: The connection uses native handles but only implements `IDisposable`, not `IAsyncDisposable`.

---

## Implementation Steps

### Phase 1: Fix Critical Disposal Issues

#### Step 1.1: Change Default Disposition to `LEAVE_CARD`

**File**: `Yubico.YubiKit.Core/src/PlatformInterop/Desktop/SCard/SCardCardHandle.cs`

**Change**:
```diff
- public SCARD_DISPOSITION ReleaseDisposition { get; set; } = SCARD_DISPOSITION.RESET_CARD;
+ public SCARD_DISPOSITION ReleaseDisposition { get; set; } = SCARD_DISPOSITION.LEAVE_CARD;
```

**Rationale**: `LEAVE_CARD` is the safest default—it leaves the card in its current state. This prevents unexpected resets that can cause availability issues. Code that explicitly needs to reset (e.g., after authentication failure) can still set `ReleaseDisposition = RESET_CARD`.

---

#### Step 1.2: Fix Resource Leak in `GetConnection`

**File**: `Yubico.YubiKit.Core/src/SmartCard/UsbSmartCardConnection.cs`

**Current code** (lines 109–141) leaks the context if `SCardConnect` fails:
```csharp
private static (SCardContext, SCardCardHandle, SCARD_PROTOCOL) GetConnection(string readerName)
{
    var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
    if (result != ErrorCode.SCARD_S_SUCCESS)
        throw new SCardException("...", result);

    // ... SCardConnect fails here → context leaks!
    result = NativeMethods.SCardConnect(...);
    if (result != ErrorCode.SCARD_S_SUCCESS)
        throw new SCardException(...);  // 👈 No cleanup of context

    return (context, cardHandle, activeProtocol);
}
```

**Fix**: Wrap the connection attempt in try-catch to dispose the context on failure:
```csharp
private static (SCardContext, SCardCardHandle, SCARD_PROTOCOL) GetConnection(string readerName)
{
    var result = NativeMethods.SCardEstablishContext(SCARD_SCOPE.USER, out var context);
    if (result != ErrorCode.SCARD_S_SUCCESS)
        throw new SCardException("ExceptionMessages.SCardCantEstablish", result);

    try
    {
        var shareMode = SCARD_SHARE.SHARED;
        if (AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out var isEnabled) &&
            isEnabled)
            shareMode = SCARD_SHARE.EXCLUSIVE;

        result = NativeMethods.SCardConnect(
            context,
            readerName,
            shareMode,
            SCARD_PROTOCOL.Tx,
            out var cardHandle,
            out var activeProtocol);

        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException(
                string.Format(CultureInfo.CurrentCulture,
                    "ExceptionMessages.SCardCardCantConnect {0}", readerName),
                result);

        // Explicitly set LEAVE_CARD for safety
        cardHandle.ReleaseDisposition = SCARD_DISPOSITION.LEAVE_CARD;
        
        return (context, cardHandle, activeProtocol);
    }
    catch
    {
        context.Dispose();
        throw;
    }
}
```

---

#### Step 1.3: Improve Dispose Method Robustness

**File**: `Yubico.YubiKit.Core/src/SmartCard/UsbSmartCardConnection.cs`

**Current code**:
```csharp
public void Dispose()
{
    if (_disposed) return;

    _cardHandle?.Dispose();
    _context?.Dispose();
    _cardHandle = null!;
    _context = null!;
    _disposed = true;
}
```

**Issues**:
- No exception handling if disposal of handles fails
- No logging for debugging purposes
- Fields nulled with `null!` which defeats null safety

**Fix**:
```csharp
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;

    try
    {
        _cardHandle?.Dispose();
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to dispose card handle for reader {ReaderName}", smartCardDevice.ReaderName);
    }

    try
    {
        _context?.Dispose();
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to dispose SCard context for reader {ReaderName}", smartCardDevice.ReaderName);
    }

    _cardHandle = null;
    _context = null;
}
```

**Additional considerations**:
- Set `_disposed = true` at the start to prevent re-entrant disposal attempts
- Catch exceptions to ensure both handles get a disposal attempt
- Remove `null!` and make fields nullable (`SCardCardHandle?`, `SCardContext?`)

---

### Phase 2: Prepare for Transaction Support

> Note: Full transaction support is documented in `SCARD-Improvements.md`. This phase sets up the groundwork.

#### Step 2.1: Add Transaction Tracking Field

**File**: `Yubico.YubiKit.Core/src/SmartCard/UsbSmartCardConnection.cs`

Add a field to track active transactions (for future implementation):
```csharp
private bool _transactionActive;
```

Update `Dispose` to end any active transaction before releasing handles:
```csharp
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;

    // End any active transaction first
    if (_transactionActive && _cardHandle is not null && !_cardHandle.IsInvalid)
    {
        try
        {
            var result = NativeMethods.SCardEndTransaction(_cardHandle, SCARD_DISPOSITION.LEAVE_CARD);
            if (result != ErrorCode.SCARD_S_SUCCESS)
                _logger.LogDebug("SCardEndTransaction returned {ErrorCode} during dispose", result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to end transaction during dispose");
        }
        _transactionActive = false;
    }

    // ... rest of disposal
}
```

---

### Phase 3: Handle Initialization Failures

#### Step 3.1: Fix Partial Initialization Cleanup

**File**: `Yubico.YubiKit.Core/src/SmartCard/UsbSmartCardConnection.cs`

The `InitializeAsync` method currently stores the result of `GetConnection` but doesn't clean up if an exception occurs afterward.

**Current code**:
```csharp
public ValueTask InitializeAsync(CancellationToken cancellationToken)
{
    _logger.LogDebug("Initializing smart card connection to reader {ReaderName}", smartCardDevice.ReaderName);
    var task = Task.Run(() =>
    {
        (_context, _cardHandle, _protocol) = GetConnection(smartCardDevice.ReaderName);
    }, cancellationToken);

    _logger.LogDebug("Smart card connection initialized to reader {ReaderName}", smartCardDevice.ReaderName);
    return new ValueTask(task);
}
```

**Issues**:
- The second `LogDebug` runs immediately (before the Task completes)—misleading log
- If cancellation occurs after `GetConnection` succeeds but before returning, resources leak
- No way to know if initialization completed successfully

**Fix**:
```csharp
public async ValueTask InitializeAsync(CancellationToken cancellationToken)
{
    _logger.LogDebug("Initializing smart card connection to reader {ReaderName}", smartCardDevice.ReaderName);
    
    try
    {
        await Task.Run(() =>
        {
            (_context, _cardHandle, _protocol) = GetConnection(smartCardDevice.ReaderName);
        }, cancellationToken).ConfigureAwait(false);
        
        _logger.LogDebug("Smart card connection initialized to reader {ReaderName}", smartCardDevice.ReaderName);
    }
    catch
    {
        // Clean up any partially initialized resources
        _cardHandle?.Dispose();
        _context?.Dispose();
        _cardHandle = null;
        _context = null;
        throw;
    }
}
```

---

#### Step 3.2: Update Factory to Handle Initialization Failures

**File**: `Yubico.YubiKit.Core/src/SmartCard/SmartCardConnectionFactory.cs`

The factory should ensure the connection is disposed if initialization fails after construction:

**Current code**:
```csharp
public async Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice,
    CancellationToken cancellationToken = default)
{
    var connection = new UsbSmartCardConnection(
        smartCardDevice,
        _loggerFactory.CreateLogger<UsbSmartCardConnection>());

    await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
    return connection;
}
```

**Fix** (optional—only if `InitializeAsync` doesn't clean up itself):
```csharp
public async Task<ISmartCardConnection> CreateAsync(IPcscDevice smartCardDevice,
    CancellationToken cancellationToken = default)
{
    var connection = new UsbSmartCardConnection(
        smartCardDevice,
        _loggerFactory.CreateLogger<UsbSmartCardConnection>());

    try
    {
        await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
    catch
    {
        connection.Dispose();
        throw;
    }
}
```

---

### Phase 4: Remove Dead Code and Add Documentation

#### Step 4.1: Remove Unused `CreateAsync` Method

**File**: `Yubico.YubiKit.Core/src/SmartCard/UsbSmartCardConnection.cs`

Lines 143–149 define a `CreateAsync` method that returns `ValueTask` but never returns the connection. This appears to be dead/incorrect code:

```csharp
public static ValueTask CreateAsync(
    IPcscDevice smartCardDevice,
    CancellationToken cancellationToken = default, ILogger<UsbSmartCardConnection>? logger = null)
{
    var connection = new UsbSmartCardConnection(smartCardDevice, logger);
    return connection.InitializeAsync(cancellationToken);  // ❌ Returns ValueTask, not connection!
}
```

**Action**: Delete this method. The factory should be the only way to create connections.

---

#### Step 4.2: Add XML Documentation

Add documentation to `ISmartCardConnection` and `UsbSmartCardConnection` explaining:
- Disposal behavior and expectations
- Thread safety (or lack thereof)
- Transaction requirements for multi-APDU sequences

---

## Testing Verification

After implementing these changes, verify:

1. **Happy path**: Connect, send APDUs, dispose—card should be available for next test
2. **Exception path**: Connect, throw exception, dispose—card should still be available
3. **Cancellation path**: Start connect, cancel mid-operation—no resource leaks
4. **Rapid reconnect**: Connect/dispose in quick succession—should not cause sharing violations

---

## Implementation Order

| Step | Priority | Risk | Notes |
|------|----------|------|-------|
| 1.1 | **High** | Low | Change default to `LEAVE_CARD` |
| 1.2 | **High** | Low | Fix context leak in `GetConnection` |
| 1.3 | Medium | Low | Improve `Dispose` robustness |
| 3.1 | Medium | Low | Fix `InitializeAsync` cleanup |
| 4.1 | Low | None | Remove dead code |
| 2.1 | Low | Medium | Transaction tracking (prep for future) |
| 3.2 | Low | Low | Factory cleanup (optional) |
| 4.2 | Low | None | Documentation |

---

## Files to Modify

| File | Changes |
|------|---------|
| `SCardCardHandle.cs` | Change default `ReleaseDisposition` to `LEAVE_CARD` |
| `UsbSmartCardConnection.cs` | Fix `GetConnection`, improve `Dispose`, fix `InitializeAsync`, remove dead code |
| `SmartCardConnectionFactory.cs` | Optional: add try-catch around `InitializeAsync` |

---

## Transaction API Usage Guidance

This section provides guidance on when and how to use the Transaction API across different YubiKey applets.

### Overview: When to Use Transactions

PC/SC transactions (`BeginTransaction`) provide **atomicity** against other processes accessing the same card. Use transactions when:

1. **PIN/password verification followed by a protected operation** — CRITICAL
2. **Multi-APDU sequences that must not be interleaved** — e.g., command chaining
3. **Read-modify-write operations** — To prevent TOCTOU (time-of-check to time-of-use) issues
4. **Application selection followed by sensitive operations** — Prevents applet switching mid-operation

### Decision Matrix by Operation Type

| Operation Pattern | Transaction Required? | Rationale |
|-------------------|----------------------|-----------|
| PIN verify → crypto operation | ✅ **Always** | Another process could interleave and use authenticated state |
| Multi-APDU command chain | ✅ **Always** | Card expects continuation; interleaving corrupts state |
| Read-modify-write config | ✅ **Recommended** | Prevents race conditions |
| SELECT applet → operation | ⚠️ **Recommended** | Prevents applet switching between SELECT and use |
| Single atomic APDU | ❌ Optional | Already atomic at card level |
| Read-only query (serial, version) | ❌ Optional | No state change, but may improve consistency |

---

### PIV (Personal Identity Verification)

**High transaction requirement** — PIV operations are inherently stateful.

#### ✅ Always use transactions for:

```csharp
// PIN verification + cryptographic operation
await using var connection = await factory.CreateAsync(device, ct);
using (connection.BeginTransaction(ct))
{
    // 1. SELECT PIV applet
    await connection.TransmitAndReceiveAsync(SelectPivApdu, ct);
    
    // 2. VERIFY PIN
    await connection.TransmitAndReceiveAsync(VerifyPinApdu, ct);
    
    // 3. GENERAL AUTHENTICATE (sign/decrypt)
    await connection.TransmitAndReceiveAsync(SignApdu, ct);
}
```

**Operations requiring transactions:**
- `VERIFY` → `GENERAL AUTHENTICATE` (sign, decrypt, key agreement)
- `VERIFY` → `GENERATE ASYMMETRIC KEY PAIR`
- `VERIFY` → `PUT DATA` (certificate storage)
- Any chained command (data > 255 bytes)

#### ❌ Single APDUs (optional):
- `GET DATA` (reading certificates, discovery object)
- `SELECT` alone

---

### OpenPGP

**High transaction requirement** — Similar to PIV, all crypto operations require prior PIN verification.

#### ✅ Always use transactions for:

```csharp
using (connection.BeginTransaction(ct))
{
    await connection.TransmitAndReceiveAsync(SelectOpenPgpApdu, ct);
    await connection.TransmitAndReceiveAsync(VerifyPw1Apdu, ct);  // User PIN
    await connection.TransmitAndReceiveAsync(DecipherApdu, ct);
}
```

**Operations requiring transactions:**
- `VERIFY PW1` → `COMPUTE DIGITAL SIGNATURE`
- `VERIFY PW1` → `DECIPHER`
- `VERIFY PW3` (Admin PIN) → `GENERATE KEY` / `PUT DATA`
- `INTERNAL AUTHENTICATE`

#### ❌ Single APDUs (optional):
- `GET DATA` (public keys, cardholder info)
- `GET CHALLENGE`

---

### OATH (TOTP/HOTP)

**Medium transaction requirement** — Depends on whether the OATH applet is password-protected.

#### ✅ Use transactions when password-protected:

```csharp
using (connection.BeginTransaction(ct))
{
    await connection.TransmitAndReceiveAsync(SelectOathApdu, ct);
    await connection.TransmitAndReceiveAsync(ValidateApdu, ct);  // Password auth
    await connection.TransmitAndReceiveAsync(CalculateApdu, ct); // Get OTP
}
```

#### ⚠️ Recommended for multi-credential operations:

```csharp
using (connection.BeginTransaction(ct))
{
    await connection.TransmitAndReceiveAsync(SelectOathApdu, ct);
    await connection.TransmitAndReceiveAsync(CalculateAllApdu, ct); // All codes at once
}
```

#### ❌ Single APDUs (optional):
- `LIST` credentials (if not password-protected)
- `SELECT` when no password set

---

### FIDO2 / U2F

**Low transaction requirement over SmartCard** — FIDO protocol is designed to be stateless.

> **Note:** FIDO typically runs over **HID** (CTAPHID), not SmartCard. When accessed via NFC (SmartCard interface), operations are usually single-APDU.

#### ⚠️ Recommended for NFC/SmartCard FIDO:

```csharp
// NFC FIDO operations - single transaction for full ceremony
using (connection.BeginTransaction(ct))
{
    await connection.TransmitAndReceiveAsync(SelectFido2Apdu, ct);
    await connection.TransmitAndReceiveAsync(GetInfoApdu, ct);
    // ... authenticator operations
}
```

#### ❌ Usually not needed:
- HID-based FIDO operations (different transport, no SmartCard)

---

### YubiOTP

**Low transaction requirement** — Primarily HID-based (keyboard interface).

> **Note:** YubiOTP configuration is typically done via the **Management** applet or HID. When accessed over SmartCard, operations are simple.

#### ⚠️ Use transactions for configuration:

```csharp
using (connection.BeginTransaction(ct))
{
    await connection.TransmitAndReceiveAsync(SelectYubiOtpApdu, ct);
    await connection.TransmitAndReceiveAsync(ConfigureSlotApdu, ct);
}
```

---

### Management

**Medium transaction requirement** — Configuration operations benefit from atomicity.

#### ✅ Use transactions for configuration changes:

```csharp
using (connection.BeginTransaction(ct))
{
    await connection.TransmitAndReceiveAsync(SelectManagementApdu, ct);
    
    // Read current config
    var config = await connection.TransmitAndReceiveAsync(ReadConfigApdu, ct);
    
    // Modify and write back
    await connection.TransmitAndReceiveAsync(WriteConfigApdu, ct);
}
```

#### ❌ Single APDUs (optional):
- `GET DEVICE INFO` (serial number, firmware version)

---

### Session-Level Transaction Patterns

For higher-level session classes (e.g., `PivSession`, `OathSession`), consider these patterns:

#### Pattern A: Transaction per operation (fine-grained)

```csharp
public async Task<byte[]> SignAsync(byte[] data, CancellationToken ct)
{
    using (_connection.BeginTransaction(ct))
    {
        await VerifyPinIfNeeded(ct);
        return await PerformSign(data, ct);
    }
}
```

#### Pattern B: Transaction for entire session (coarse-grained)

```csharp
public static async Task<PivSession> CreateAsync(ISmartCardConnection connection, CancellationToken ct)
{
    var transaction = connection.BeginTransaction(ct);
    try
    {
        await SelectPivApplet(connection, ct);
        return new PivSession(connection, transaction);
    }
    catch
    {
        transaction.Dispose();
        throw;
    }
}

// Session holds transaction until disposed
public void Dispose() => _transaction.Dispose();
```

#### Pattern C: Hybrid (recommended)

- Hold transaction during session for simple use cases
- Allow explicit transaction control for advanced scenarios
- Document behavior clearly

---

### Common Pitfalls

1. **Holding transactions too long** — Blocks other processes from accessing the card. Keep transactions as short as possible.

2. **Forgetting to use transactions for PIN operations** — The #1 source of "works on my machine" bugs. Other software (GPG, browser extensions) may interleave.

3. **Nested transactions** — Not supported. Will throw `InvalidOperationException`. Design flows to avoid nesting.

4. **Transaction across async boundaries** — The `IDisposable` returned by `BeginTransaction` can be held across `await`, but ensure proper disposal with `using`.

5. **Reset behavior** — If you need to reset card state after a failed operation (e.g., wrong PIN), use the overload: `BeginTransaction(SCARD_DISPOSITION.RESET_CARD, ct)`.

---

## References

- [SCARD-Improvements.md](./SCARD-Improvements.md) — Full transaction support design
- [Microsoft PC/SC Documentation](https://learn.microsoft.com/en-us/windows/win32/api/winscard/)
- [ISO 7816-4](https://www.iso.org/standard/77180.html) — Smart card command structure
- [PIV Specification (NIST SP 800-73)](https://csrc.nist.gov/publications/detail/sp/800-73/4/final)
- [OpenPGP Card Specification](https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf)


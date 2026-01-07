# PC/SC Transaction Support & SmartCard Connection Robustness

**Type:** Feature / Technical Debt  
**Component:** Yubico.YubiKit.Core / SmartCard  
**Priority:** High  
**Complexity:** Medium  
**Status:** Ready for Development  

---

## Summary

Implement PC/SC transaction support in `UsbSmartCardConnection` and improve connection robustness to eliminate `SCARD_E_SHARING_VIOLATION` errors and card unavailability after test failures. This work has been **partially completed** - disposal robustness and basic transaction API are implemented. Remaining work focuses on **reconnect/retry logic** and **session-level integration**.

---

## Problem Statement

### Current Issues

1. **Card unavailability after test failures** ✅ **FIXED**
   - Default `RESET_CARD` disposition caused aggressive resets
   - Resource leaks when connection initialization failed
   - Missing transaction cleanup in disposal path

2. **APDU interleaving with other processes** ✅ **PARTIALLY FIXED**
   - No transaction support for multi-APDU sequences
   - PIN verification followed by crypto operations can be interrupted by GPG, browser extensions, etc.
   - **Transaction API now available** but not yet integrated into session classes

3. **No resilience to card resets** ⚠️ **TODO**
   - `SCARD_W_RESET_CARD` errors are not handled
   - No automatic reconnect/retry logic

---

## Completed Work

### Phase 1: Disposal Robustness ✅

- [x] Changed default `ReleaseDisposition` to `LEAVE_CARD` in `SCardCardHandle`
- [x] Fixed resource leak in `GetConnection` (context disposal on failure)
- [x] Improved `Dispose()` with exception handling and logging
- [x] Fixed `InitializeAsync` to clean up on cancellation/failure
- [x] Added defensive disposal in `SmartCardConnectionFactory`
- [x] Removed `#region` usage per CLAUDE.md
- [x] Switched to `ArrayPool<byte>` for APDU buffers

### Phase 2: Transaction API ✅

- [x] Added `IConnection : IDisposable, IAsyncDisposable`
- [x] Added `BeginTransaction(CancellationToken)` to `ISmartCardConnection`
- [x] Implemented `TransactionScope` nested class
- [x] Implemented `BeginTransactionInternal` with cancellation support
- [x] Added `DisposeAsync()` for modern async disposal patterns
- [x] Added overload `BeginTransaction(SCARD_DISPOSITION, CancellationToken)` on concrete class

**Files Modified:**
- `Yubico.YubiKit.Core/src/PlatformInterop/Desktop/SCard/SCardCardHandle.cs`
- `Yubico.YubiKit.Core/src/SmartCard/UsbSmartCardConnection.cs`
- `Yubico.YubiKit.Core/src/SmartCard/SmartCardConnectionFactory.cs`

---

## Remaining Work

### 1. Reconnect & Retry Logic (High Priority)

**Goal:** Handle `SCARD_W_RESET_CARD` gracefully by reconnecting and retrying the operation.

#### Implementation

Add to `UsbSmartCardConnection`:

```csharp
/// <summary>
/// Transmits an APDU with automatic reconnect on card reset.
/// </summary>
public async Task<ReadOnlyMemory<byte>> TransmitWithReconnectAsync(
    ReadOnlyMemory<byte> command,
    CancellationToken cancellationToken = default)
{
    const int maxRetries = 1;
    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await TransmitAndReceiveAsync(command, cancellationToken);
        }
        catch (SCardException ex) when (ex.ErrorCode == ErrorCode.SCARD_W_RESET_CARD && attempt < maxRetries)
        {
            _logger.LogWarning("Card reset detected, attempting reconnect...");
            await ReconnectAsync(SCARD_DISPOSITION.LEAVE_CARD, cancellationToken);
        }
    }
    
    throw new InvalidOperationException("Unreachable");
}

private async Task ReconnectAsync(SCARD_DISPOSITION init, CancellationToken ct)
{
    var shareMode = AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out var ex) && ex
        ? SCARD_SHARE.EXCLUSIVE : SCARD_SHARE.SHARED;
    
    var result = await Task.Run(() => NativeMethods.SCardReconnect(
        _cardHandle!,
        shareMode,
        SCARD_PROTOCOL.Tx,
        init,
        out var newProtocol), ct).ConfigureAwait(false);
    
    if (result != ErrorCode.SCARD_S_SUCCESS)
        throw new SCardException("Reconnect failed", result);
    
    _protocol = newProtocol;
    _logger.LogInformation("Card reconnected successfully");
}
```

**Decision:** Should this be:
- **Option A:** Automatic in `TransmitAndReceiveAsync` (gated by AppContext switch)
- **Option B:** Separate method `TransmitWithReconnectAsync` (recommended - explicit opt-in)

**Recommendation:** Option B. Automatic retry can mask real issues. Let session classes decide when to retry.

---

### 2. Session-Level Transaction Integration (Medium Priority)

**Goal:** Integrate transaction API into higher-level session classes (`PivSession`, `OathSession`, etc.)

#### Recommended Pattern: Hybrid Approach

```csharp
public class PivSession : IDisposable
{
    private readonly ISmartCardConnection _connection;
    private IDisposable? _sessionTransaction;
    
    // Option 1: Hold transaction for entire session (simple use case)
    public static async Task<PivSession> CreateAsync(
        ISmartCardConnection connection,
        bool holdTransaction = false,
        CancellationToken ct = default)
    {
        IDisposable? transaction = null;
        try
        {
            if (holdTransaction)
                transaction = connection.BeginTransaction(ct);
            
            await SelectPivApplet(connection, ct);
            return new PivSession(connection, transaction);
        }
        catch
        {
            transaction?.Dispose();
            throw;
        }
    }
    
    // Option 2: Transaction per operation (advanced use case)
    public async Task<byte[]> SignAsync(byte[] data, CancellationToken ct)
    {
        // If session doesn't hold transaction, create one per operation
        if (_sessionTransaction is null)
        {
            using (_connection.BeginTransaction(ct))
            {
                await VerifyPinIfNeeded(ct);
                return await PerformSignInternal(data, ct);
            }
        }
        else
        {
            // Session already holds transaction
            await VerifyPinIfNeeded(ct);
            return await PerformSignInternal(data, ct);
        }
    }
    
    public void Dispose()
    {
        _sessionTransaction?.Dispose();
    }
}
```

#### Sessions to Update

| Session Class | Transaction Strategy | Priority |
|---------------|---------------------|----------|
| `PivSession` | Per-operation (PIN verify → crypto) | High |
| `OathSession` | Per-operation (if password-protected) | Medium |
| `OpenPgpSession` | Per-operation (PW1/PW3 → crypto) | High |
| `ManagementSession` | Optional (config read-modify-write) | Low |

---

### 3. Documentation & Examples (Medium Priority)

#### Add to XML Docs

Update `ISmartCardConnection.BeginTransaction`:

```csharp
/// <summary>
/// Starts a PC/SC transaction. The transaction is ended when the returned scope is disposed.
/// </summary>
/// <remarks>
/// <para>
/// Transactions provide atomicity against other processes accessing the same card.
/// Use transactions for:
/// <list type="bullet">
/// <item>PIN verification followed by cryptographic operations</item>
/// <item>Multi-APDU command chains</item>
/// <item>Read-modify-write configuration operations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong> Keep transactions as short as possible. Holding a transaction
/// blocks other processes from accessing the card.
/// </para>
/// <example>
/// <code>
/// await using var connection = await factory.CreateAsync(device, ct);
/// using (connection.BeginTransaction(ct))
/// {
///     await connection.TransmitAndReceiveAsync(verifyPinApdu, ct);
///     await connection.TransmitAndReceiveAsync(signApdu, ct);
/// }
/// </code>
/// </example>
/// </remarks>
/// <param name="cancellationToken">Token to cancel transaction start.</param>
/// <returns>A disposable scope that ends the transaction when disposed.</returns>
/// <exception cref="InvalidOperationException">A transaction is already active.</exception>
/// <exception cref="SCardException">Failed to begin transaction.</exception>
```

#### Add Usage Guide

Create `docs/SmartCard-Transactions.md` with:
- When to use transactions (decision matrix)
- Per-applet guidance (PIV, OATH, OpenPGP, etc.)
- Common pitfalls
- Session-level patterns

**Reference:** See `SCARD-Improvements-plan.md` lines 374-621 for complete content.

---

## Acceptance Criteria

### Functional

- [ ] `TransmitWithReconnectAsync` (or equivalent) handles `SCARD_W_RESET_CARD` with one retry
- [ ] `PivSession` uses transactions for PIN verify → crypto operations
- [ ] `OathSession` uses transactions when password-protected
- [ ] Session classes document transaction behavior in XML comments
- [ ] No nested transaction attempts (throws `InvalidOperationException`)

### Non-Functional

- [ ] All changes follow CLAUDE.md guidelines (no `#region`, use `ArrayPool`, etc.)
- [ ] Logging at appropriate levels (Debug for transaction lifecycle, Warning for failures)
- [ ] No breaking changes to public API surface
- [ ] Performance: Transaction overhead < 5ms on typical hardware

### Testing

- [ ] Unit tests for `TransactionScope` lifecycle (begin, end, dispose, cancellation)
- [ ] Unit tests for nested transaction prevention
- [ ] Unit tests for reconnect logic (mock `SCARD_W_RESET_CARD`)
- [ ] Integration tests with real YubiKey:
  - [ ] PIN verify → sign in transaction (no interleaving)
  - [ ] Rapid connect/disconnect (no sharing violations)
  - [ ] Card reset during operation (reconnect succeeds)

---

## Technical Notes

### Transaction Lifecycle

```
BeginTransaction(ct)
  ↓
Task.Run(() => SCardBeginTransaction(_cardHandle), ct)  // Worker thread for cancellation
  ↓
TransactionScope created, _transactionActive = true
  ↓
[User code executes APDUs]
  ↓
TransactionScope.Dispose()
  ↓
SCardEndTransaction(_cardHandle, LEAVE_CARD)
  ↓
_transactionActive = false
```

### Cancellation Behavior

- `BeginTransaction` runs on worker thread to enable cancellation
- PC/SC has no native timeout for `SCardBeginTransaction`
- `SCardCancel` primarily affects `SCardGetStatusChange`, not `BeginTransaction`
- Best-effort cancellation only

### Platform Differences

- Windows (WinSCard), macOS, and Linux (pcsc-lite) all support transactions
- Blocking behavior may differ slightly across platforms
- Do not rely on `SCardCancel` to abort `BeginTransaction` on all OSes

---

## References

- **Implementation Plan:** `SCARD-Improvements-plan.md`
- **Original Design:** `SCARD-Improvments.md`
- **Microsoft PC/SC Docs:** https://learn.microsoft.com/en-us/windows/win32/api/winscard/
- **ISO 7816-4:** Smart card command structure
- **PIV Spec (NIST SP 800-73):** https://csrc.nist.gov/publications/detail/sp/800-73/4/final
- **OpenPGP Card Spec:** https://gnupg.org/ftp/specs/OpenPGP-smart-card-application-3.4.pdf

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Deadlocks from long-held transactions | High | Document best practices, add logging for long waits |
| Platform differences in cancellation | Medium | Document as best-effort, test on all platforms |
| Breaking changes to session APIs | Low | Use optional parameters, maintain backward compatibility |
| Performance regression | Low | Profile transaction overhead, keep < 5ms |

---

## Estimated Effort

- **Reconnect logic:** 2-4 hours
- **Session integration (PIV, OATH):** 4-6 hours
- **Documentation & examples:** 2-3 hours
- **Testing:** 4-6 hours

**Total:** 12-19 hours (1.5-2.5 days)

---

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Code reviewed and approved
- [ ] Unit tests passing (>90% coverage for new code)
- [ ] Integration tests passing on Windows, macOS, Linux
- [ ] Documentation updated (XML comments + usage guide)
- [ ] No CLAUDE.md violations
- [ ] Performance benchmarks within acceptable range

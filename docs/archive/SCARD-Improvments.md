### Work Item: PC/SC Transaction & Robustness Improvements for SmartCardConnection

#### Summary
Implement PC/SC transaction support and connection robustness in `SmartCardConnection` to reduce `SCARD_E_SHARING_VIOLATION` issues and improve resilience to card resets (`SCARD_W_RESET_CARD`). Provide a simple, safe API for callers to execute multi‑APDU sequences atomically (w.r.t. other processes) and to recover from resets.

---

### Background & Current Behavior
- File: `/Yubico.YubiKit.Core/src/SmartCard/SmartCardConnection.cs`
- Today, the class:
  - Establishes context and connects (line 124 uses `SCardConnect`).
  - Sends APDUs via `TransmitAndReceiveAsync` without a transaction.
  - Defaults to `SCARD_SHARE.SHARED` (unless a feature switch requests EXCLUSIVE) but has no cross‑process serialization around multi‑APDU sequences.
- Result: When other apps (e.g., GPG via `scdaemon`) use the card, our `SCardConnect` can fail with `SCARD_E_SHARING_VIOLATION`. Even if we connect, APDUs can be interleaved with other processes unless we hold a PC/SC transaction.

---

### Goals
1) Add explicit transaction scoping (`SCardBeginTransaction`/`SCardEndTransaction`) to `SmartCardConnection`.
2) Provide an ergonomic way for callers to run multi‑APDU sequences atomically.
3) Improve robustness against `SCARD_W_RESET_CARD` by documenting and optionally implementing a reconnect‑and‑retry helper.
4) Keep default behavior safe and non‑disruptive (prefer `SCARD_SHARE.SHARED`, avoid implicit resets/EXCLUSIVE unless requested).

---

### Non‑Goals / Out of Scope
- Do not attempt to kill other processes or restart smart‑card services (e.g., `gpgconf --kill scdaemon`, `pcscd`, Windows Smart Card service).
- Do not change APDU buffer sizing/fragmentation logic (separate task; current `512`‑byte buffer remains).
- Do not silently change public behavior of `TransmitAndReceiveAsync` (no hidden auto‑transactions by default).

---

### API Changes (Proposed)
Two options are provided; choose A (preferred, clearer API) or B (non‑breaking for interface).

- Option A — Add to the interface (breaking change within the solution):
  - File: `SmartCardConnection.cs` (interface region around lines 27–38)
  - Change `ISmartCardConnection` to include a transaction method:
    ```csharp
    public interface ISmartCardConnection : IConnection
    {
        Transport Transport { get; }

        // Start a PC/SC transaction. Ended automatically when the returned scope is disposed.
        // Uses SCARD_LEAVE_CARD on end by default.
        IDisposable BeginTransaction(CancellationToken cancellationToken = default);

        Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default);

        bool SupportsExtendedApdu();
    }
    ```
  - Note: This is a minor breaking change; update all internal consumers accordingly.

- Option B — Keep interface as‑is, add method(s) only to concrete class:
  - Add public overloads on `SmartCardConnection`:
    ```csharp
    public IDisposable BeginTransaction(CancellationToken cancellationToken = default);
    public IDisposable BeginTransaction(SCARD_DISPOSITION endDisposition, CancellationToken cancellationToken = default);
    ```
  - No interface change; callers who need transactions can downcast or use an extension method provided in the same assembly.

Decision record: Prefer A if you control consumers across this solution; otherwise use B.

---

### Implementation Details (This File)
File: `/Yubico.YubiKit.Core/src/SmartCard/SmartCardConnection.cs`

1) Add transaction tracking field
```csharp
private TransactionScope? _activeTransaction;
```

2) Add private nested scope type
```csharp
private sealed class TransactionScope : IDisposable
{
    private readonly SmartCardConnection _owner;
    private readonly SCARD_DISPOSITION _endDisposition;
    private bool _ended;

    public TransactionScope(SmartCardConnection owner, SCARD_DISPOSITION endDisposition)
    {
        _owner = owner;
        _endDisposition = endDisposition;
    }

    public void MarkBeganOrThrow(uint ec)
    {
        if (ec != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardBeginTransactionFailure", ec);
    }

    public void Dispose()
    {
        if (_ended) return;
        _ended = true;

        if (ReferenceEquals(_owner._activeTransaction, this))
            _owner._activeTransaction = null;

        var result = NativeMethods.SCardEndTransaction(_owner._cardHandle!, SCARD_DISPOSITION.LEAVE_CARD);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            _owner._logger.LogDebug("SCardEndTransaction returned {Error}", result);
    }
}
```

3) Add public BeginTransaction API(s)
```csharp
public IDisposable BeginTransaction(CancellationToken cancellationToken = default)
    => BeginTransactionInternal(SCARD_DISPOSITION.LEAVE_CARD, cancellationToken);

// Optional concrete overload for callers that require a specific end disposition.
public IDisposable BeginTransaction(SCARD_DISPOSITION endDisposition, CancellationToken cancellationToken = default)
    => BeginTransactionInternal(endDisposition, cancellationToken);

private IDisposable BeginTransactionInternal(SCARD_DISPOSITION endDisposition, CancellationToken ct)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    ArgumentNullException.ThrowIfNull(_cardHandle);

    if (_activeTransaction is not null)
        throw new InvalidOperationException("A card transaction is already active on this connection.");

    var scope = new TransactionScope(this, endDisposition);

    // BeginTransaction can block behind another process's transaction. Run it on a worker
    // to allow best-effort cancellation. (PC/SC has no timeout parameter.)
    var beginTask = Task.Run(() => NativeMethods.SCardBeginTransaction(_cardHandle!), ct);

    try
    {
        var ec = beginTask.GetAwaiter().GetResult();
        scope.MarkBeganOrThrow(ec);
    }
    catch (OperationCanceledException)
    {
        // Not holding a transaction; ensure scope doesn't try to end it.
        scope.Dispose();
        throw;
    }

    _activeTransaction = scope;
    return scope;
}
```

4) Dispose path: end any active transaction before releasing handles
```csharp
public void Dispose()
{
    if (_disposed) return;

    _activeTransaction?.Dispose();

    _cardHandle?.Dispose();
    _context?.Dispose();
    _cardHandle = null!;
    _context = null!;
    _disposed = true;
}
```

5) Optional helper: scoped execution
Add a convenience method to reduce boilerplate for callers who want an async transaction scope.
```csharp
public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default)
{
    using (BeginTransaction(ct))
    {
        return await work(ct).ConfigureAwait(false);
    }
}
```

6) Optional resilience helper: reconnect on `SCARD_W_RESET_CARD`
Provide an internal helper that catches a reset, calls `SCardReconnect`, and retries once. Either:
- Add a new method `TransmitAndReceiveWithReconnectAsync` (non‑breaking), or
- Gate automatic retry in `TransmitAndReceiveAsync` behind an `AppContext` switch (default off to preserve behavior).

Skeleton:
```csharp
private ReadOnlyMemory<byte> TransmitCore(ReadOnlySpan<byte> command, out uint ec)
{
    var output = new byte[512];
    var io = new SCARD_IO_REQUEST(_protocol!.Value);
    ec = NativeMethods.SCardTransmit(_cardHandle!, io, command, IntPtr.Zero, output, out var got);
    if (ec == ErrorCode.SCARD_S_SUCCESS) Array.Resize(ref output, got);
    return output;
}

private void Reconnect(SCARD_DISPOSITION init)
{
    var rc = NativeMethods.SCardReconnect(_cardHandle!,
        AppContext.TryGetSwitch(CoreCompatSwitches.OpenSmartCardHandlesExclusively, out var ex) && ex
            ? SCARD_SHARE.EXCLUSIVE : SCARD_SHARE.SHARED,
        SCARD_PROTOCOL.Tx,
        init,
        out var newProtocol);
    if (rc != ErrorCode.SCARD_S_SUCCESS) throw new SCardException("ExceptionMessages.SCardReconnectFailure", rc);
    _protocol = newProtocol;
}
```

---

### Considerations & Caveats
- Transactions:
  - Only one transaction per connection; nested transactions should throw `InvalidOperationException`.
  - Keep transactions as short as possible; they serialize other apps’ access while held.
  - Use `SCARD_DISPOSITION.LEAVE_CARD` on `EndTransaction` unless you explicitly need to reset.
- Share Modes:
  - Prefer `SCARD_SHARE.SHARED` + transaction for atomicity.
  - Avoid `SCARD_SHARE.EXCLUSIVE` unless required; it increases the likelihood of `SCARD_E_SHARING_VIOLATION`.
- Cancellation/Timeouts:
  - PC/SC provides no timeout for `BeginTransaction`. Running it on a worker enables app‑level cancellation, but it does not forcibly abort the OS call across all platforms.
  - `SCardCancel(context)` primarily cancels `SCardGetStatusChange` waits; do not rely on it to cancel `BeginTransaction`/`Transmit` on all OSes.
- Reset handling:
  - `SCARD_W_RESET_CARD` indicates the card reset since your last op (possibly by another app). After this, reselect your application and re‑authenticate.
- Logging:
  - Log at `Debug` when a transaction begins, ends, and when `BeginTransaction` waits longer than a threshold (e.g., >2s). Include reader name and share mode.

---

### Cross‑Platform Notes
- Windows (WinSCard), macOS, and Linux (pcsc‑lite) all implement PC/SC semantics, but blocking/cancellation behavior can differ slightly.
- Do not attempt to manage system services (`SCardSvr`, `pcscd`) from the SDK.

---

### Testing Strategy
Because `NativeMethods` is static, introduce a small seam for testing, or create a minimal abstraction:
- Add internal interface `ISCard` mirroring the used subset (`SCardConnect`, `SCardBeginTransaction`, `SCardEndTransaction`, `SCardTransmit`, `SCardReconnect`, `SCardReleaseContext`, `SCardDisconnect`).
- Provide a default implementation that wraps `NativeMethods`.
- Allow `SmartCardConnection` to accept an `ISCard` (internal ctor overload) for tests.

Unit tests (Yubico.YubiKit.Core.UnitTests):
- Transaction scope:
  - Begin returns success → End called on dispose.
  - Nested Begin throws `InvalidOperationException`.
  - Dispose of `SmartCardConnection` while transaction is active calls `EndTransaction` before releasing handles.
- Cancellation:
  - Simulate delayed `BeginTransaction` and ensure cancellation throws and no transaction remains active.
- Reset handling helper (if implemented):
  - First `SCardTransmit` returns `SCARD_W_RESET_CARD`, then `SCardReconnect` returns success, then second transmit succeeds → method returns success.
- Connect retry/backoff (if added in this ticket):
  - Simulate N failures with `SCARD_E_SHARING_VIOLATION` followed by success; ensure elapsed wait is within expected bounds and that success path returns a handle.

Optional integration tests (manual/hardware‑backed):
- With two processes, verify that when one holds a transaction, the other blocks until `EndTransaction`.
- Induce a reset via another process; verify `SCARD_W_RESET_CARD` handling.

---

### Acceptance Criteria
- A developer can call:
  ```csharp
  using (connection.BeginTransaction(ct))
  {
      var r1 = await connection.TransmitAndReceiveAsync(cmd1, ct);
      var r2 = await connection.TransmitAndReceiveAsync(cmd2, ct);
  }
  ```
  and the transaction begins and ends correctly.
- Nested `BeginTransaction` on the same `SmartCardConnection` throws a clear error.
- Disposing `SmartCardConnection` while a transaction is active ends the transaction first.
- Documentation in XML comments explains when to use transactions and how to handle `SCARD_W_RESET_CARD`.
- If the reconnect helper is included, a single reset during transmit is recovered via `SCardReconnect` and one retry (feature‑flagged or separate method).

---

### Risk & Mitigation
- Deadlocks/lengthy blocking on `BeginTransaction`: Mitigate with worker thread, logging when wait exceeds thresholds, and guidance to keep scope short.
- API surface change (Option A): Minor breaking change; coordinate updates across solution.
- Platform differences in cancellation behavior: Document as best‑effort; tests should not assume `SCardCancel` aborts `BeginTransaction`.

---

### References
- `SCard.Interop.cs` (existing P/Invoke): `/Yubico.YubiKit.Core/src/PlatformInterop/Desktop/SCard/SCard.Interop.cs` — contains `SCardBeginTransaction`, `SCardEndTransaction`, `SCardReconnect`, `SCardTransmit`.
- Microsoft WinSCard docs: Begin/End/Connect/Transmit/Reconnect/Disconnect/Cancel.
- pcsc‑lite API documentation (behavior mirrors WinSCard for these calls).

---

### Follow‑Ups (separate tickets)
- Large APDU support in `TransmitAndReceiveAsync` (buffer sizing/extended APDU handling).
- Connect retry/backoff policy for `SCARD_E_SHARING_VIOLATION` at `SCardConnect` (if not included here).
- Public, platform‑neutral `CardDisposition` enum if exposing disposition beyond the concrete class.

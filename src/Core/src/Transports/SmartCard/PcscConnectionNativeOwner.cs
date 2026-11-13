// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;

namespace Yubico.YubiKit.Core.Transports.SmartCard;

/// <summary>Owns one PC/SC connection's native worker, operation admission and checked shutdown.</summary>
/// <remarks>
///     The owner roots itself before native acquisition and lazily starts one dedicated worker. Ordinary operations
///     have one admission slot and no backlog; concurrent work is rejected rather than serialized for later execution.
///     Native calls, transaction end and resource release run on that worker. Cancellation is checked before dispatch
///     only; dispatched calls retain admission and borrowed storage until they return. An active transaction allows
///     sequential ordinary work and does not enforce logical-caller or session exclusion.
///     Shutdown drains admitted work, ends the transaction and checks native release. Unproven release or worker
///     failure after acquisition retains the root and uncertain resources, notifies the quarantine observer and
///     faults the shared shutdown outcome. Neither a timeout nor a replacement worker is used to claim recovery.
/// </remarks>
internal sealed class PcscConnectionNativeOwner
{
    private readonly string _readerName;
    private readonly PcscNativeResources _nativeResources;
    private readonly Action? _released;
    private readonly Action<Exception>? _unrecovered;
    private readonly Action<Thread> _startWorker;
    private readonly Action? _workerLoopStarting;
    private readonly Lock _sync = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly TaskCompletionSource _shutdownCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GCHandle _root;
    private NativeWorkItem? _ordinaryWork;
    private NativeWorkItem? _executingWork;
    private Thread? _worker;
    private TransactionState? _transaction;
    private bool _ordinaryActive;
    private bool _closed;
    private bool _shutdownRequested;
    private bool _workerTerminated;
    private int _workerThreadId;
    private int _nativeReleaseProven = 1;

    /// <summary>Gets whether the native handle was successfully opened with SCARD_SHARE_EXCLUSIVE.</summary>
    /// <remarks>False before successful open; this records the opened mode rather than current release state.</remarks>
    public bool IsExclusive => _nativeResources.IsExclusive;

    /// <summary>Creates and roots an unopened native owner without starting its worker or acquiring PC/SC resources.</summary>
    /// <param name="readerName">The reader to open and identify in worker diagnostics.</param>
    /// <param name="connectionApi">The synchronous native seam used by the worker-owned resources.</param>
    /// <param name="sharing">The native opening policy, defaulting to strict exclusive access.</param>
    /// <param name="released">The ownership-release observer invoked after proven cleanup or failure before acquisition.</param>
    /// <param name="unrecovered">The quarantine observer invoked when native release cannot be proven.</param>
    /// <param name="startWorker">An optional test seam that must start this exact worker or throw before starting it.</param>
    /// <param name="workerLoopStarting">An optional hook invoked on the worker before processing queued work.</param>
    public PcscConnectionNativeOwner(
        string readerName,
        ISCardConnectionApi connectionApi,
        SmartCardSharing sharing = SmartCardSharing.Exclusive,
        Action? released = null,
        Action<Exception>? unrecovered = null,
        Action<Thread>? startWorker = null,
        Action? workerLoopStarting = null)
    {
        _readerName = readerName;
        _nativeResources = new PcscNativeResources(readerName, connectionApi, sharing);
        _released = released;
        _unrecovered = unrecovered;
        _startWorker = startWorker ?? (static worker => worker.Start());
        _workerLoopStarting = workerLoopStarting;
        _root = GCHandle.Alloc(this);
    }

    /// <summary>Gets whether the current thread is this owner's running native worker.</summary>
    public bool IsWorkerThread => Environment.CurrentManagedThreadId == Volatile.Read(ref _workerThreadId);

    /// <summary>Gets whether native acquisition has not begun or checked native release has succeeded.</summary>
    /// <remarks>
    ///     Becomes false before open touches native resources and remains false on uncertain release. A faulted
    ///     shutdown can still have release proof when only a transaction-end or ownership-observer error remains.
    /// </remarks>
    public bool IsNativeReleaseProven => Volatile.Read(ref _nativeReleaseProven) != 0;

    /// <summary>Admits native context establishment and card opening as one worker operation.</summary>
    /// <remarks>
    ///     Cancellation before dispatch skips native acquisition; after dispatch it cannot interrupt open, which
    ///     may succeed. Failed partial acquisition is retained for <see cref="RequestShutdown" /> to release.
    ///     This method does not itself perform failed-open cleanup or make repeated opening safe.
    /// </remarks>
    /// <param name="cancellationToken">The token checked before admission and immediately before native dispatch.</param>
    /// <returns>Completion of native opening, or its acquisition/admission failure.</returns>
    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        _ = await RunAsync(
            () =>
            {
                Volatile.Write(ref _nativeReleaseProven, 0);
                _nativeResources.Open();
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Synchronously forwards an exchange to this owner's native resources.</summary>
    /// <remarks>
    ///     This method supplies no admission or thread switch. Call it only within work admitted through
    ///     <see cref="RunAsync{T}" /> on this owner's worker, after open succeeds. Both spans are borrowed until return.
    /// </remarks>
    /// <param name="command">The caller-owned command bytes.</param>
    /// <param name="response">The caller-owned native response destination.</param>
    /// <returns>The native response byte count on success.</returns>
    /// <exception cref="SCardException">Native transmission reports a failure.</exception>
    public int Transmit(ReadOnlySpan<byte> command, Span<byte> response) =>
        _nativeResources.Transmit(command, response);

    /// <summary>Admits one synchronous native delegate for execution on the dedicated worker.</summary>
    /// <remarks>
    ///     There is no ordinary-work backlog. The worker checks cancellation before invoking the delegate, but does
    ///     not interrupt it or cancel its successful result after dispatch. Captured storage must remain valid until
    ///     the returned task completes. Completion is published only after invocation and admission retirement.
    ///     Delegates must not synchronously wait for work or disposal on this same owner.
    /// </remarks>
    /// <typeparam name="T">The unmanaged result type carried from the worker to the caller.</typeparam>
    /// <param name="nativeCall">The synchronous operation; native failure translation belongs to this delegate.</param>
    /// <param name="cancellationToken">The token checked before admission and again before delegate invocation.</param>
    /// <returns>The admitted operation's result after the delegate returns, or its cancellation/failure.</returns>
    /// <exception cref="ObjectDisposedException">Admission is closed.</exception>
    /// <exception cref="InvalidOperationException">Another ordinary operation is active, or a transaction is ending or failed to end.</exception>
    /// <exception cref="OperationCanceledException">Cancellation has already been requested before admission.</exception>
    public Task<T> RunAsync<T>(Func<T> nativeCall, CancellationToken cancellationToken)
        where T : unmanaged
    {
        cancellationToken.ThrowIfCancellationRequested();
        var work = new NativeWorkItem<T>(nativeCall, cancellationToken);

        lock (_sync)
        {
            ThrowIfOrdinaryWorkCannotStart();
            QueueOrdinaryWork(work);
        }

        SignalWorker();
        return work.Task;
    }

    /// <summary>Synchronously waits for worker-directed native transaction acquisition.</summary>
    /// <remarks>
    ///     This blocks without a timeout and uses the same admission and pre-dispatch cancellation rules as
    ///     <see cref="BeginTransactionAsync" />. The returned scope also implements <see cref="IAsyncDisposable" />.
    /// </remarks>
    /// <param name="endDisposition">The disposition requested when the returned scope is disposed.</param>
    /// <param name="cancellationToken">The token checked before admission and dispatch, not during native acquisition.</param>
    /// <param name="reportEndFailure">The scope-disposal observer for a non-success native end status.</param>
    /// <returns>The acquired transaction's disposal scope.</returns>
    /// <exception cref="InvalidOperationException">Called on the native worker or when operation/transaction admission is unavailable.</exception>
    public IDisposable BeginTransaction(
        SCARD_DISPOSITION endDisposition,
        CancellationToken cancellationToken,
        Action<uint> reportEndFailure)
    {
        if (IsWorkerThread)
            throw new InvalidOperationException("A synchronous transaction cannot begin from the native worker.");

        return BeginTransactionAsync(endDisposition, cancellationToken, reportEndFailure).GetAwaiter().GetResult();
    }

    /// <summary>Admits native transaction acquisition and asynchronously returns its disposal scope.</summary>
    /// <remarks>
    ///     A second transaction begin and overlapping ordinary work are rejected. Cancellation before dispatch skips
    ///     acquisition; afterward native success still returns a scope requiring disposal. An active transaction does
    ///     not hold the ordinary admission slot between calls. Scope disposal requests the supplied disposition;
    ///     shutdown requests LEAVE_CARD for an active transaction and waits for its end before native release.
    ///     End-status failures notify the scope observer and close admission, and are also reported by shutdown.
    /// </remarks>
    /// <param name="endDisposition">The native card disposition requested by scope disposal.</param>
    /// <param name="cancellationToken">The token checked before admission and immediately before dispatch.</param>
    /// <param name="reportEndFailure">The observer invoked by scope disposal for a non-success native end status.</param>
    /// <returns>A scope implementing both <see cref="IDisposable" /> and <see cref="IAsyncDisposable" /> after successful acquisition.</returns>
    /// <exception cref="SCardException">Native transaction acquisition reports a failure.</exception>
    public async Task<IDisposable> BeginTransactionAsync(
        SCARD_DISPOSITION endDisposition,
        CancellationToken cancellationToken,
        Action<uint> reportEndFailure)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var transaction = new TransactionState(endDisposition);
        var work = new NativeWorkItem<uint>(
            _nativeResources.BeginTransaction,
            cancellationToken,
            completed => CompleteTransactionBegin(transaction, completed));

        lock (_sync)
        {
            ThrowIfOrdinaryWorkCannotStart();
            if (_transaction is not null)
                throw new InvalidOperationException("A card transaction is already active on this connection.");

            _transaction = transaction;
            QueueOrdinaryWork(work);
        }

        SignalWorker();
        var result = await work.Task.ConfigureAwait(false);
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardBeginTransactionFailure", result);

        return new TransactionScope(this, transaction, reportEndFailure);
    }

    /// <summary>Closes admission, requests worker-directed cleanup and returns the shared shutdown task.</summary>
    /// <remarks>
    ///     Repeated requests observe the same outcome. Admitted work is retired before transaction end and checked
    ///     disconnection with UNPOWER_CARD followed by context release. This request neither cancels native calls nor
    ///     supplies a drain timeout. Unproven release retains the root/resources and invokes the quarantine observer;
    ///     proven release invokes the ownership-release observer and frees the root, even if a separate diagnostic
    ///     faults shutdown. Inspect <see cref="IsNativeReleaseProven" /> rather than treating a fault as release proof.
    /// </remarks>
    /// <returns>The shared cleanup outcome, faulted on release, transaction-end, worker or observer failure.</returns>
    public Task RequestShutdown()
    {
        lock (_sync)
        {
            if (_workerTerminated || _shutdownRequested)
                return _shutdownCompletion.Task;

            _closed = true;
            _shutdownRequested = true;
            if (_transaction is { Phase: TransactionPhase.Active } transaction)
                transaction.RequestEnd(SCARD_DISPOSITION.LEAVE_CARD);

            EnsureWorker();
        }

        SignalWorker();
        return _shutdownCompletion.Task;
    }

    private void ThrowIfOrdinaryWorkCannotStart()
    {
        if (_closed)
            throw new ObjectDisposedException("UsbSmartCardConnection");
        if (_ordinaryActive)
            throw new InvalidOperationException(
                "Another raw smart-card operation is already active on this connection. " +
                "Await it before starting another operation.");
        if (_transaction is { Phase: TransactionPhase.EndRequested or TransactionPhase.EndFailed })
            throw new InvalidOperationException("The smart-card transaction is ending or failed to end.");
    }

    private void QueueOrdinaryWork(NativeWorkItem work)
    {
        _ordinaryActive = true;
        _ordinaryWork = work;
        EnsureWorker();
    }

    private void CompleteTransactionBegin(TransactionState transaction, NativeWorkItem<uint> completed)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_transaction, transaction))
                return;

            if (completed.WasCanceled || completed.Exception is not null ||
                completed.Result != ErrorCode.SCARD_S_SUCCESS)
            {
                transaction.Phase = TransactionPhase.Ended;
                _transaction = null;
                transaction.EndCompletion.TrySetResult(ErrorCode.SCARD_S_SUCCESS);
                return;
            }

            transaction.Phase = TransactionPhase.Active;
            if (_shutdownRequested)
                transaction.RequestEnd(SCARD_DISPOSITION.LEAVE_CARD);
        }
    }

    private Task<uint> RequestTransactionEnd(TransactionState transaction)
    {
        var signal = false;
        lock (_sync)
        {
            if (transaction.Phase == TransactionPhase.Active)
            {
                transaction.RequestEnd(transaction.ScopeDisposition);
                signal = true;
            }

            if (signal && !_workerTerminated)
                EnsureWorker();
        }

        if (signal)
            SignalWorker();
        return transaction.EndCompletion.Task;
    }

    private void EnsureWorker()
    {
        if (_worker is not null)
            return;
        if (_workerTerminated)
            throw new InvalidOperationException("The native worker has already terminated.");

        var worker = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = $"YubiKit PCSC {_readerName}"
        };
        _worker = worker;
        try
        {
            // The injectable start action is a narrow test seam. It must either start this exact worker or
            // throw before starting it; native work remains queued until SignalWorker runs after this returns.
            _startWorker(worker);
        }
        catch (Exception ex)
        {
            HandleWorkerStartFailure(ex);
        }
    }

    private void HandleWorkerStartFailure(Exception failure)
    {
        _worker = null;
        _workerTerminated = true;
        _closed = true;
        _ordinaryActive = false;

        var ordinaryWork = _ordinaryWork;
        _ordinaryWork = null;
        var transaction = _transaction;
        _transaction = null;

        try
        {
            _released?.Invoke();
        }
        catch (Exception observerError)
        {
            failure = new AggregateException(failure, observerError);
        }

        ReleaseRoot();
        _signal.Dispose();
        ordinaryWork?.Fail(failure);
        transaction?.EndCompletion.TrySetException(failure);
        _shutdownCompletion.TrySetException(failure);
    }

    private void SignalWorker()
    {
        lock (_sync)
        {
            if (_workerTerminated)
                return;
            _signal.Set();
        }
    }

    private void WorkerLoop()
    {
        Volatile.Write(ref _workerThreadId, Environment.CurrentManagedThreadId);
        try
        {
            _workerLoopStarting?.Invoke();
            RunWorkerLoop();
        }
        catch (Exception ex)
        {
            HandleUnexpectedWorkerFailure(ex);
        }
        finally
        {
            lock (_sync)
            {
                _workerTerminated = true;
                _worker = null;
                Volatile.Write(ref _workerThreadId, 0);
            }

            _signal.Dispose();
        }
    }

    private void RunWorkerLoop()
    {
        while (true)
        {
            _signal.WaitOne();
            if (DrainWorkerSteps())
                return;
        }
    }

    /// <summary>Runs queued steps until the worker is idle (returns false) or shutdown completes (returns true).</summary>
    private bool DrainWorkerSteps()
    {
        while (true)
        {
            var step = TakeWorkerStep();
            if (step.Work is not null)
                ExecuteOrdinaryWork(step.Work);
            else if (step.TransactionToEnd is not null)
                EndTransaction(step.TransactionToEnd);
            else if (step.Shutdown)
            {
                CompleteShutdown();
                return true;
            }
            else
                return false;
        }
    }

    private WorkerStep TakeWorkerStep()
    {
        lock (_sync)
        {
            var work = _ordinaryWork;
            _ordinaryWork = null;
            if (work is not null || _ordinaryActive)
                return new WorkerStep(work, null, false);

            return TakeIdleWorkerStepLocked();
        }
    }

    private WorkerStep TakeIdleWorkerStepLocked()
    {
        if (_transaction is { Phase: TransactionPhase.Active } active && _shutdownRequested)
            active.RequestEnd(SCARD_DISPOSITION.LEAVE_CARD);

        if (_transaction is { Phase: TransactionPhase.EndRequested, EndAttempted: false } ending)
        {
            ending.EndAttempted = true;
            return new WorkerStep(null, ending, false);
        }

        var shutdown = _shutdownRequested &&
            _transaction is not { Phase: TransactionPhase.Beginning or TransactionPhase.EndRequested };
        return new WorkerStep(null, null, shutdown);
    }

    private void ExecuteOrdinaryWork(NativeWorkItem work)
    {
        _executingWork = work;
        work.Invoke();
        work.AfterInvoke();
        lock (_sync)
            _ordinaryActive = false;
        work.PublishCompletion();
        _executingWork = null;
    }

    private void EndTransaction(TransactionState transaction)
    {
        uint result;
        Exception? failure = null;
        try
        {
            result = _nativeResources.EndTransaction(transaction.EndDisposition);
        }
        catch (Exception ex)
        {
            result = uint.MaxValue;
            failure = ex;
        }

        TaskCompletionSource<uint> completion;
        lock (_sync)
        {
            completion = transaction.EndCompletion;
            transaction.EndFailure = failure ?? (result == ErrorCode.SCARD_S_SUCCESS
                ? null
                : new SCardException("ExceptionMessages.SCardEndTransactionFailure", result));
            if (result == ErrorCode.SCARD_S_SUCCESS)
            {
                transaction.Phase = TransactionPhase.Ended;
                if (ReferenceEquals(_transaction, transaction))
                    _transaction = null;
            }
            else
            {
                transaction.Phase = TransactionPhase.EndFailed;
                _closed = true;
            }
        }

        completion.TrySetResult(result);
    }

    private void CompleteShutdown()
    {
        var releasedNativeResources = _nativeResources.TryRelease(out var cleanupError);
        var transactionError = _transaction?.EndFailure;
        if (!releasedNativeResources)
        {
            var failure = CombineErrors(transactionError, cleanupError) ?? new InvalidOperationException(
                $"The PC/SC resources for reader '{_readerName}' could not be proven released.");
            try
            {
                _unrecovered?.Invoke(failure);
            }
            catch (Exception observerError)
            {
                failure = new AggregateException(failure, observerError);
            }

            _shutdownCompletion.TrySetException(failure);
            return;
        }

        Volatile.Write(ref _nativeReleaseProven, 1);
        Exception? releaseObserverError = null;
        try
        {
            _released?.Invoke();
        }
        catch (Exception ex)
        {
            releaseObserverError = ex;
        }

        ReleaseRoot();
        var reportedError = CombineErrors(transactionError, cleanupError, releaseObserverError);
        if (reportedError is null)
            _shutdownCompletion.TrySetResult();
        else
            _shutdownCompletion.TrySetException(reportedError);
    }

    private void HandleUnexpectedWorkerFailure(Exception failure)
    {
        lock (_sync)
            _closed = true;

        if (IsNativeReleaseProven)
        {
            try
            {
                _released?.Invoke();
            }
            catch (Exception observerError)
            {
                failure = new AggregateException(failure, observerError);
            }

            ReleaseRoot();
        }
        else
        {
            try
            {
                _unrecovered?.Invoke(failure);
            }
            catch (Exception observerError)
            {
                failure = new AggregateException(failure, observerError);
            }
        }

        _executingWork?.Fail(failure);
        _ordinaryWork?.Fail(failure);
        _transaction?.EndCompletion.TrySetException(failure);
        _shutdownCompletion.TrySetException(failure);
    }

    private void ReleaseRoot()
    {
        if (_root.IsAllocated)
            _root.Free();
    }

    private static Exception? CombineErrors(params Exception?[] errors)
    {
        var present = errors.Where(static error => error is not null).Cast<Exception>().ToArray();
        return present.Length switch
        {
            0 => null,
            1 => present[0],
            _ => new AggregateException(present)
        };
    }

    private enum TransactionPhase
    {
        Beginning,
        Active,
        EndRequested,
        Ended,
        EndFailed
    }

    /// <summary>Carries the next ordinary-work, transaction-end or shutdown step selected under the owner lock.</summary>
    /// <param name="Work">The admitted ordinary work to run, or <see langword="null" /> when there is none.</param>
    /// <param name="TransactionToEnd">The transaction whose end attempt was reserved, or <see langword="null" />.</param>
    /// <param name="Shutdown">Whether idle transaction state permits final resource cleanup.</param>
    private readonly record struct WorkerStep(
        NativeWorkItem? Work,
        TransactionState? TransactionToEnd,
        bool Shutdown);

    /// <summary>Tracks one transaction's acquisition, requested end and shared end outcome under the owner lock.</summary>
    /// <param name="scopeDisposition">The card disposition requested by ordinary scope disposal.</param>
    private sealed class TransactionState(SCARD_DISPOSITION scopeDisposition)
    {
        /// <summary>Gets the original disposition requested by the transaction scope.</summary>
        public SCARD_DISPOSITION ScopeDisposition { get; } = scopeDisposition;
        /// <summary>Gets the disposition selected for the pending native end attempt.</summary>
        public SCARD_DISPOSITION EndDisposition { get; private set; } = scopeDisposition;
        /// <summary>Gets or sets the acquisition/end phase used for owner admission and shutdown ordering.</summary>
        public TransactionPhase Phase { get; set; } = TransactionPhase.Beginning;
        /// <summary>Gets or sets whether the worker has reserved the single native end attempt.</summary>
        public bool EndAttempted { get; set; }
        /// <summary>Gets or sets the end-call exception or translated end-status failure reported during shutdown.</summary>
        public Exception? EndFailure { get; set; }
        /// <summary>Gets the shared native end-status completion, with asynchronously scheduled continuations.</summary>
        /// <remarks>Worker failure may fault it; native end failure is represented by a non-success result.</remarks>
        public TaskCompletionSource<uint> EndCompletion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Records the requested end disposition and advances to the end-requested phase.</summary>
        /// <remarks>The owner calls this under its lock; it does not dispatch native work or signal the worker itself.</remarks>
        public void RequestEnd(SCARD_DISPOSITION disposition)
        {
            EndDisposition = disposition;
            Phase = TransactionPhase.EndRequested;
        }
    }

    /// <summary>Separates worker invocation and owner bookkeeping from public completion publication.</summary>
    private abstract class NativeWorkItem
    {
        /// <summary>Gets the failure captured during delegate invocation, if any.</summary>
        public Exception? Exception { get; protected set; }
        /// <summary>Gets whether the worker skipped delegate invocation because cancellation preceded dispatch.</summary>
        public bool WasCanceled { get; protected set; }

        /// <summary>Invokes or skips the synchronous delegate on the owner worker without publishing completion.</summary>
        public abstract void Invoke();
        /// <summary>Performs worker-side owner bookkeeping after invocation and before admission retirement.</summary>
        public abstract void AfterInvoke();
        /// <summary>Publishes the captured result, cancellation or failure after the owner retires admission.</summary>
        public abstract void PublishCompletion();
        /// <summary>Faults the public operation when worker startup or execution prevents normal completion.</summary>
        /// <param name="failure">The terminal worker failure.</param>
        public abstract void Fail(Exception failure);
    }

    /// <summary>Carries one synchronous delegate, its pre-dispatch token and asynchronously published result.</summary>
    /// <typeparam name="T">The unmanaged result type.</typeparam>
    /// <param name="nativeCall">The synchronous delegate to invoke on the owner worker.</param>
    /// <param name="cancellationToken">The token checked before invocation, not used to interrupt a dispatched call.</param>
    /// <param name="afterInvoke">Optional owner bookkeeping run before admission retirement and completion publication.</param>
    private sealed class NativeWorkItem<T>(
        Func<T> nativeCall,
        CancellationToken cancellationToken,
        Action<NativeWorkItem<T>>? afterInvoke = null) : NativeWorkItem
        where T : unmanaged
    {
        private readonly TaskCompletionSource<T> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private T _result;

        /// <summary>Gets the operation's public completion with asynchronously scheduled continuations.</summary>
        public Task<T> Task => _completion.Task;
        /// <summary>Gets the captured delegate result, meaningful only after successful invocation.</summary>
        public T Result => _result;

        /// <inheritdoc />
        /// <remarks>Cancellation is checked once before dispatch; native success is not replaced by later cancellation.</remarks>
        public override void Invoke()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                WasCanceled = true;
                return;
            }

            try
            {
                _result = nativeCall();
            }
            catch (Exception ex)
            {
                Exception = ex;
            }
        }

        /// <inheritdoc />
        /// <remarks>A bookkeeping exception propagates to the owner's unexpected-worker-failure handling.</remarks>
        public override void AfterInvoke() => afterInvoke?.Invoke(this);

        /// <inheritdoc />
        public override void PublishCompletion()
        {
            if (WasCanceled)
                _completion.TrySetCanceled(cancellationToken);
            else if (Exception is not null)
                _completion.TrySetException(Exception);
            else
                _completion.TrySetResult(_result);
        }

        /// <inheritdoc />
        public override void Fail(Exception failure) => _completion.TrySetException(failure);
    }

    /// <summary>Requests and observes the shared end outcome for one acquired native transaction.</summary>
    /// <param name="owner">The worker and transaction-lifetime authority.</param>
    /// <param name="transaction">The acquired transaction state.</param>
    /// <param name="reportEndFailure">The observer notified of a non-success native end status.</param>
    private sealed class TransactionScope(
        PcscConnectionNativeOwner owner,
        TransactionState transaction,
        Action<uint> reportEndFailure) : IDisposable, IAsyncDisposable
    {
        /// <summary>Requests transaction end and synchronously waits for its shared native status.</summary>
        /// <remarks>
        ///     The wait has no timeout. Repeated disposal observes the same end task; a non-success status notifies
        ///     the observer rather than being thrown here, while task and observer exceptions propagate.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Called on the owner worker, where waiting would deadlock.</exception>
        public void Dispose()
        {
            if (owner.IsWorkerThread)
                throw new InvalidOperationException("Synchronous transaction disposal cannot wait on the native worker.");

            var endTask = owner.RequestTransactionEnd(transaction);
            var result = endTask.GetAwaiter().GetResult();
            if (result != ErrorCode.SCARD_S_SUCCESS)
                reportEndFailure(result);
        }

        /// <summary>Requests transaction end and asynchronously awaits its shared native status.</summary>
        /// <remarks>
        ///     There is no cancellation token or timeout. A non-success status notifies the observer rather than
        ///     being thrown here; task and observer exceptions propagate. This ends the transaction, not the connection.
        /// </remarks>
        /// <returns>Completion after the shared end outcome has been observed and any status failure reported.</returns>
        public async ValueTask DisposeAsync()
        {
            var result = await owner.RequestTransactionEnd(transaction).ConfigureAwait(false);
            if (result != ErrorCode.SCARD_S_SUCCESS)
                reportEndFailure(result);
        }
    }
}
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

internal sealed class PcscConnectionNativeState
{
    private readonly string _readerName;
    private readonly PcscNativeResources _resources;
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

    public PcscConnectionNativeState(
        string readerName,
        ISCardConnectionApi api,
        Action? released = null,
        Action<Exception>? unrecovered = null,
        Action<Thread>? startWorker = null,
        Action? workerLoopStarting = null)
    {
        _readerName = readerName;
        _resources = new PcscNativeResources(readerName, api);
        _released = released;
        _unrecovered = unrecovered;
        _startWorker = startWorker ?? (static worker => worker.Start());
        _workerLoopStarting = workerLoopStarting;
        _root = GCHandle.Alloc(this);
    }

    public bool IsWorkerThread => Environment.CurrentManagedThreadId == Volatile.Read(ref _workerThreadId);

    public bool IsNativeReleaseProven => Volatile.Read(ref _nativeReleaseProven) != 0;

    public async Task OpenAsync(CancellationToken cancellationToken)
    {
        _ = await RunAsync(
            () =>
            {
                Volatile.Write(ref _nativeReleaseProven, 0);
                _resources.Open();
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    public int Transmit(ReadOnlySpan<byte> command, Span<byte> response) =>
        _resources.Transmit(command, response);

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

    public IDisposable BeginTransaction(
        SCARD_DISPOSITION endDisposition,
        CancellationToken cancellationToken,
        Action<uint> reportEndFailure)
    {
        if (IsWorkerThread)
            throw new InvalidOperationException("A synchronous transaction cannot begin from the native worker.");

        cancellationToken.ThrowIfCancellationRequested();
        var transaction = new TransactionState(endDisposition);
        var work = new NativeWorkItem<uint>(
            _resources.BeginTransaction,
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
        var result = work.Task.GetAwaiter().GetResult();
        if (result != ErrorCode.SCARD_S_SUCCESS)
            throw new SCardException("ExceptionMessages.SCardBeginTransactionFailure", result);

        return new TransactionScope(this, transaction, reportEndFailure);
    }

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
            throw new ObjectDisposedException(nameof(UsbSmartCardConnection));
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

            while (true)
            {
                NativeWorkItem? work;
                TransactionState? transactionToEnd = null;
                var shutdown = false;
                lock (_sync)
                {
                    work = _ordinaryWork;
                    _ordinaryWork = null;
                    if (work is null && !_ordinaryActive)
                    {
                        if (_transaction is { Phase: TransactionPhase.Active } active && _shutdownRequested)
                            active.RequestEnd(SCARD_DISPOSITION.LEAVE_CARD);

                        if (_transaction is { Phase: TransactionPhase.EndRequested, EndAttempted: false } ending)
                        {
                            ending.EndAttempted = true;
                            transactionToEnd = ending;
                        }
                        else if (_shutdownRequested &&
                                 _transaction is not { Phase: TransactionPhase.Beginning or TransactionPhase.EndRequested })
                        {
                            shutdown = true;
                        }
                    }
                }

                if (work is not null)
                {
                    _executingWork = work;
                    work.Invoke();
                    work.AfterInvoke();
                    lock (_sync)
                        _ordinaryActive = false;
                    work.PublishCompletion();
                    _executingWork = null;
                    continue;
                }

                if (transactionToEnd is not null)
                {
                    EndTransaction(transactionToEnd);
                    continue;
                }

                if (!shutdown)
                    break;

                CompleteShutdown();
                return;
            }
        }
    }

    private void EndTransaction(TransactionState transaction)
    {
        uint result;
        Exception? failure = null;
        try
        {
            result = _resources.EndTransaction(transaction.EndDisposition);
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
        var releasedNativeResources = _resources.TryRelease(out var cleanupError);
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

    private sealed class TransactionState(SCARD_DISPOSITION scopeDisposition)
    {
        public SCARD_DISPOSITION ScopeDisposition { get; } = scopeDisposition;
        public SCARD_DISPOSITION EndDisposition { get; private set; } = scopeDisposition;
        public TransactionPhase Phase { get; set; } = TransactionPhase.Beginning;
        public bool EndAttempted { get; set; }
        public Exception? EndFailure { get; set; }
        public TaskCompletionSource<uint> EndCompletion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RequestEnd(SCARD_DISPOSITION disposition)
        {
            EndDisposition = disposition;
            Phase = TransactionPhase.EndRequested;
        }
    }

    private abstract class NativeWorkItem
    {
        public Exception? Exception { get; protected set; }
        public bool WasCanceled { get; protected set; }

        public abstract void Invoke();
        public abstract void AfterInvoke();
        public abstract void PublishCompletion();
        public abstract void Fail(Exception failure);
    }

    private sealed class NativeWorkItem<T>(
        Func<T> nativeCall,
        CancellationToken cancellationToken,
        Action<NativeWorkItem<T>>? afterInvoke = null) : NativeWorkItem
        where T : unmanaged
    {
        private readonly TaskCompletionSource<T> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private T _result;

        public Task<T> Task => _completion.Task;
        public T Result => _result;

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

        public override void AfterInvoke() => afterInvoke?.Invoke(this);

        public override void PublishCompletion()
        {
            if (WasCanceled)
                _completion.TrySetCanceled(cancellationToken);
            else if (Exception is not null)
                _completion.TrySetException(Exception);
            else
                _completion.TrySetResult(_result);
        }

        public override void Fail(Exception failure) => _completion.TrySetException(failure);
    }

    private sealed class TransactionScope(
        PcscConnectionNativeState owner,
        TransactionState transaction,
        Action<uint> reportEndFailure) : IDisposable
    {
        public void Dispose()
        {
            if (owner.IsWorkerThread)
                throw new InvalidOperationException("Synchronous transaction disposal cannot wait on the native worker.");

            var endTask = owner.RequestTransactionEnd(transaction);
            var result = endTask.GetAwaiter().GetResult();
            if (result != ErrorCode.SCARD_S_SUCCESS)
                reportEndFailure(result);
        }
    }
}
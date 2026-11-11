using System.Diagnostics;

namespace Yubico.YubiKit.Core.UnitTests.BoundaryInventory;

// One invocation and one held native-seam call. Five-second waits are hang watchdogs,
// not responsiveness budgets. Assertions must run before Release is signaled.
internal sealed class WithheldNativeProbe : IDisposable
{
    private readonly ManualResetEventSlim _entered = new();
    private Thread? _caller;
    private Task? _operation;
    private int _nativeThread;
    private int _callerThread;
    private long _invokedAt;
    private long _returnedAt;
    private long _nativeEnteredAt;
    private long _nativeExitedAt;

    internal ManualResetEventSlim Release { get; } = new();
    internal ManualResetEventSlim Returned { get; } = new();
    // Outer completion is invocation return; its result is the still-pending operation.
    internal TaskCompletionSource<Task> Invocation { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int CallerThread => Volatile.Read(ref _callerThread);

    internal void Start(Func<Task> invoke)
    {
        _caller = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            Volatile.Write(ref _callerThread, Environment.CurrentManagedThreadId);
            _invokedAt = Stopwatch.GetTimestamp();
            try
            {
                _operation = invoke();
                _returnedAt = Stopwatch.GetTimestamp();
                Invocation.TrySetResult(_operation);
            }
            catch (Exception ex) { Invocation.TrySetException(ex); }
            finally { Returned.Set(); }
        })
        { IsBackground = true };
        _caller.Start();
    }

    internal void EnterNative()
    {
        Volatile.Write(ref _nativeThread, Environment.CurrentManagedThreadId);
        _nativeEnteredAt = Stopwatch.GetTimestamp();
        _entered.Set();
        try { Release.Wait(TestContext.Current.CancellationToken); }
        finally { _nativeExitedAt = Stopwatch.GetTimestamp(); }
    }

    internal void AssertResponsive()
    {
        Assert.True(_entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "native operation was never entered");
        Assert.NotEqual(CallerThread, Volatile.Read(ref _nativeThread));
        Assert.True(Returned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken),
            "invocation did not return before native release");
        Assert.False(Release.IsSet);
        Assert.NotNull(_operation);
        Assert.False(_operation.IsCompleted, "operation completed while native work was still held");
    }

    internal async Task ReleaseAndDrainAsync()
    {
        Release.Set();
        // Lifetime cleanup must drain even if runner cancellation has already fired.
        var operation = await Invocation.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        await operation.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
    }

    internal string DescribeDurations() => FormattableString.Invariant(
        $"callerReturnMs={Stopwatch.GetElapsedTime(_invokedAt, _returnedAt).TotalMilliseconds:F3} fakeHeldCallMs={Stopwatch.GetElapsedTime(_nativeEnteredAt, _nativeExitedAt).TotalMilliseconds:F3} (managed barrier only; not native duration)");

    public void Dispose()
    {
        Release.Set();
        if (_caller?.Join(TimeSpan.FromSeconds(5)) != false)
        {
            _entered.Dispose();
            Release.Dispose();
            Returned.Dispose();
        }
    }
}
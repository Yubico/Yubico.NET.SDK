using Yubico.YubiKit.Core.Transports.Hid.Linux;
using FidoApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.ReportApi;
using OtpApi = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxOtpHidConnectionTests.ReportApi;
using Reactor = Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux.LinuxFidoHidConnectionTests.Reactor;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid.Linux;

public class LinuxDirectFacadeAcceptanceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(5);

    [Fact]
    public void InputOpen_MetadataFailureClosesPartialOwner()
    {
        var failure = new IOException("descriptor failure");
        var api = new FidoApi { MetadataError = failure };
        Assert.Same(failure, Assert.Throws<IOException>(() => new LinuxHidIOReportConnection("fake", api, new Reactor())));
        Assert.Equal(1, api.Opens);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public void FeatureOpen_FailurePreservesOriginalWithoutClosingUnacquiredDescriptor()
    {
        var failure = new IOException("open failure");
        var api = new OtpApi { OpenError = failure };
        Assert.Same(failure, Assert.Throws<IOException>(() => new LinuxHidFeatureReportConnection("fake", api)));
        Assert.Equal(1, api.Opens);
        Assert.Equal(0, api.Closes);
    }

    [Fact]
    public async Task InputSet_HeldWriteBlocksSyncCallerAndDrainsBeforeClose()
    {
        var api = new FidoApi();
        await using var connection = new LinuxHidIOReportConnection("fake", api, new Reactor());
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        api.Before = stage => { if (stage == "write") { entered.TrySetResult(); release.Wait(Ct); } };
        var set = Task.Factory.StartNew(() => connection.SetReport([0xA7, 2, 3]), Ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task? dispose = null;
        try
        {
            await entered.Task.WaitAsync(Bound, Ct);
            Assert.False(set.IsCompleted);
            dispose = connection.DisposeAsync().AsTask();
            Assert.False(dispose.IsCompleted);
            Assert.Equal(0, api.Closes);
        }
        finally { release.Set(); await set.WaitAsync(Bound, Ct); if (dispose is not null) await dispose.WaitAsync(Bound, Ct); }
        Assert.Equal(3, api.WrittenLength);
        Assert.Equal(0xA7, api.WrittenFirst);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public async Task FeatureSet_HeldIoctlBlocksSyncCallerAndPreservesExpertLength()
    {
        var api = new OtpApi();
        await using var connection = new LinuxHidFeatureReportConnection("fake", api);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        api.Before = stage => { if (stage == "set") { entered.TrySetResult(); release.Wait(Ct); } };
        var set = Task.Factory.StartNew(() => connection.SetReport([1, 2, 3]), Ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task? dispose = null;
        try
        {
            await entered.Task.WaitAsync(Bound, Ct);
            Assert.False(set.IsCompleted);
            dispose = connection.DisposeAsync().AsTask();
            Assert.False(dispose.IsCompleted);
            Assert.Equal(0, api.Closes);
        }
        finally { release.Set(); await set.WaitAsync(Bound, Ct); if (dispose is not null) await dispose.WaitAsync(Bound, Ct); }
        Assert.Equal(new byte[] { 1, 2, 3 }, api.Sent);
        Assert.Equal(1, api.Closes);
    }

    [Fact]
    public Task InputDispose_HeldQuiescenceBlocksSyncDisposeAndSharesCheckedClose() =>
        InputDisposeWhileHeldAsync(immediateReturnControl: false);

    private static async Task InputDisposeWhileHeldAsync(bool immediateReturnControl)
    {
        var api = new FidoApi();
        var reactor = new Reactor { HoldDrain = true };
        var connection = new LinuxHidIOReportConnection("fake", api, reactor);
        var first = connection.DisposeAsync().AsTask();
        HeldSynchronousDisposeProbe? second = null;
        try
        {
            await reactor.Removal.Task.WaitAsync(Bound, Ct);
            Assert.False(first.IsCompleted);
            second = new HeldSynchronousDisposeProbe(immediateReturnControl ? ImmediateReturnDispose : connection.Dispose);
            second.AssertBlockedWhileHeld(() => reactor.Drain.Task.IsCompleted);
            Assert.Equal(0, api.Closes);
        }
        finally
        {
            reactor.Drain.TrySetResult();
            // Cleanup must drain both callers even if the assertion or runner cancellation failed.
            var synchronousDrain = second?.DrainAsync() ?? Task.CompletedTask;
            await Task.WhenAll(first, synchronousDrain).WaitAsync(Bound, CancellationToken.None);
        }
        connection.Dispose();
        Assert.Equal(1, api.Closes);
        Assert.Equal(1, reactor.ReferenceReleases);
    }

    [Fact]
    public Task FeatureDispose_HeldCloseBlocksSyncDisposeAndSharesOutcome() =>
        FeatureDisposeWhileHeldAsync(immediateReturnControl: false);

    private static async Task FeatureDisposeWhileHeldAsync(bool immediateReturnControl)
    {
        var api = new OtpApi();
        var connection = new LinuxHidFeatureReportConnection("fake", api);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        api.Before = stage => { if (stage == "close") { entered.TrySetResult(); release.Wait(Ct); } };
        var first = connection.DisposeAsync().AsTask();
        HeldSynchronousDisposeProbe? second = null;
        try
        {
            await entered.Task.WaitAsync(Bound, Ct);
            Assert.False(first.IsCompleted);
            second = new HeldSynchronousDisposeProbe(immediateReturnControl ? ImmediateReturnDispose : connection.Dispose);
            second.AssertBlockedWhileHeld(() => release.IsSet);
        }
        finally
        {
            release.Set();
            // The held native close must return before either caller/fake can be abandoned.
            var synchronousDrain = second?.DrainAsync() ?? Task.CompletedTask;
            await Task.WhenAll(first, synchronousDrain).WaitAsync(Bound, CancellationToken.None);
        }
        connection.Dispose();
        Assert.Equal(1, api.Closes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronousDisposeWitness_ImmediateReturnControlIsRejected(bool feature)
    {
        var failure = await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(() => feature
            ? FeatureDisposeWhileHeldAsync(immediateReturnControl: true)
            : InputDisposeWhileHeldAsync(immediateReturnControl: true));
        Assert.Contains("Synchronous Dispose returned before a blocking wait was witnessed", failure.Message, StringComparison.Ordinal);
    }

    // Deliberately broken synchronous Dispose surrogate: the real async caller still holds native cleanup.
    private static void ImmediateReturnDispose() { }

    private sealed class HeldSynchronousDisposeProbe
    {
        private readonly Thread _caller;
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _invoking, _returned;

        internal HeldSynchronousDisposeProbe(Action dispose)
        {
            _caller = new Thread(() =>
            {
                Exception? failure = null;
                Volatile.Write(ref _invoking, 1);
                // No probe wait, signal, context dispatch or task scheduling occurs in this interval.
                // Any WaitSleepJoin observed before _returned is published is inside Dispose itself.
                try { dispose(); }
                catch (Exception ex) { failure = ex; }
                finally { Volatile.Write(ref _returned, 1); }
                if (failure is null) _completion.TrySetResult(); else _completion.TrySetException(failure);
            })
            { IsBackground = true };
            _caller.Start();
        }

        internal void AssertBlockedWhileHeld(Func<bool> released)
        {
            Assert.False(released());
            var blocked = false;
            _ = SpinWait.SpinUntil(() =>
            {
                Ct.ThrowIfCancellationRequested();
                blocked = Volatile.Read(ref _invoking) != 0 && Volatile.Read(ref _returned) == 0 &&
                    (_caller.ThreadState & ThreadState.WaitSleepJoin) != 0 && Volatile.Read(ref _returned) == 0;
                return blocked || Volatile.Read(ref _returned) != 0;
            }, Bound);
            Assert.True(blocked, Volatile.Read(ref _returned) != 0
                ? "Synchronous Dispose returned before a blocking wait was witnessed while native cleanup was held."
                : "Synchronous Dispose did not enter a blocking wait before the watchdog expired.");
            // After witnessing entry into the actual wait, observe nonreturn with the gate still held.
            // This finite observation is not a disposal-latency or performance guarantee.
            Assert.False(SpinWait.SpinUntil(() =>
            {
                Ct.ThrowIfCancellationRequested();
                return Volatile.Read(ref _returned) != 0;
            }, TimeSpan.FromMilliseconds(200)), "Synchronous Dispose returned while native cleanup remained held.");
            Assert.Equal(0, Volatile.Read(ref _returned));
            Assert.False(_completion.Task.IsCompleted);
            Assert.False(released());
        }

        internal async Task DrainAsync()
        {
            // Gate release belongs to the enclosing finally; cancellation must not abandon this caller.
            try { await _completion.Task.WaitAsync(Bound, CancellationToken.None); }
            finally { Assert.True(_caller.Join(Bound), "Synchronous disposal caller did not exit after native release."); }
        }
    }
}
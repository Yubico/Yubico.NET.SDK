using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidFidoReadCancellationTests
{
    [Fact]
    public async Task CancelPendingReadAllowsNextReadAndKeepsInputRunning()
    {
        var bridge = new ControlledBridge();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var first = connection.ReceiveAsync(cancellation.Token);

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Equal(0, bridge.CancelCount);

        var next = connection.ReceiveAsync(TestContext.Current.CancellationToken);
        bridge.Emit(7);
        Assert.Equal((byte)7, (await next.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Span[0]);
    }

    [Fact]
    public async Task ReportBeforeCancellationDeliversOnceAndQueuesNextReport()
    {
        var bridge = new ControlledBridge();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var oldCancellation = new CancellationTokenSource();
        var first = connection.ReceiveAsync(oldCancellation.Token);
        bridge.Emit(1);

        oldCancellation.Cancel();
        bridge.Emit(2);
        Assert.Equal((byte)1, (await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Span[0]);
        Assert.Equal((byte)2, (await connection.ReceiveAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Span[0]);
    }

    [Fact]
    public async Task CancellationBeforeReportQueuesReportForNextReader()
    {
        var bridge = new ControlledBridge();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var first = connection.ReceiveAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        bridge.Emit(3);
        Assert.Equal((byte)3, (await connection.ReceiveAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Span[0]);
    }

    [Fact]
    public async Task CancellingOldReadCannotDetachNewPendingRead()
    {
        var bridge = new ControlledBridge();
        await using var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var oldCancellation = new CancellationTokenSource();
        var first = connection.ReceiveAsync(oldCancellation.Token);
        bridge.Emit(4);
        var next = connection.ReceiveAsync(TestContext.Current.CancellationToken);

        oldCancellation.Cancel();
        Assert.Equal((byte)4, (await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Span[0]);
        Assert.False(next.IsCompleted);
        bridge.Emit(5);
        Assert.Equal((byte)5, (await next.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Span[0]);
    }

    [Fact]
    public async Task DisposalAfterCancelledReadStillWaitsForNativeAcknowledgment()
    {
        var bridge = new ControlledBridge { HoldAck = true };
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var read = connection.ReceiveAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        var closing = connection.DisposeAsync().AsTask();
        await bridge.AckEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(closing.IsCompleted);
        Assert.Equal(0, bridge.ReleaseCount);
        bridge.ReleaseAck.SetResult();
        await closing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, bridge.ReleaseCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposalCompletesAdmittedPublicReadBeforeReturning(bool cancellable)
    {
        var bridge = new ControlledBridge();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var read = connection.ReceiveAsync(cancellable ? cancellation.Token : CancellationToken.None);

        await connection.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(read.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => read);
        Assert.Equal(1, bridge.ReleaseCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynchronousDisposalCompletesAdmittedPublicReadBeforeReturning(bool cancellable)
    {
        var bridge = new ControlledBridge();
        var connection = await MacOSFidoHidConnection.OpenAsync(1, bridge, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var read = connection.ReceiveAsync(cancellable ? cancellation.Token : CancellationToken.None);

        await Task.Run(connection.Dispose, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(read.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => read);
        Assert.Equal(1, bridge.ReleaseCount);
    }

    private sealed class ControlledBridge : IHidInputBridge
    {
        private Action<ReadOnlyMemory<byte>>? _report;
        public readonly TaskCompletionSource AckEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource ReleaseAck = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldAck;
        public int CancelCount;
        public int ReleaseCount;

        public void Emit(byte value)
        {
            var packet = new byte[64];
            packet[0] = value;
            (_report ?? throw new InvalidOperationException("Input not started"))(packet);
        }

        public nint CreateDevice(long entryId) => 1;
        public void OpenDevice(nint device) { }
        public int InputSize(nint device) => 64;
        public bool CloseUnstarted(nint device) => true;
        public void ReleaseDevice(nint device) => Interlocked.Increment(ref ReleaseCount);
        public nint CreateInput(nint device, int size, Action<ReadOnlyMemory<byte>> report, Action<int> terminal)
        {
            _report = report;
            return 2;
        }
        public int Start(nint owner) => 0;
        public void Cancel(nint owner) => Interlocked.Increment(ref CancelCount);
        public int WaitShutdown(nint owner)
        {
            AckEntered.SetResult();
            if (HoldAck) ReleaseAck.Task.GetAwaiter().GetResult();
            return 0;
        }
        public int Destroy(nint owner) => 0;
        public void SetReport(nint device, byte[] report) { }
    }
}

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native;
using Yubico.YubiKit.Core.Transports.Hid.MacOS;

namespace Yubico.YubiKit.Core.UnitTests.Transports.Hid;

public class MacOSHidFeatureFacadeTests
{
    [Fact]
    public async Task DescriptorAndExpertLength_ArePreservedWhileTypedOtpRemainsEightBytes()
    {
        var native = new Lifetime();
        using (var expert = new MacOSHidFeatureReportConnection(42, native))
        {
            Assert.Equal(ConnectionType.Hid, expert.Type);
            Assert.Equal(65, expert.InputReportSize);
            Assert.Equal(72, expert.OutputReportSize);
            expert.SetReport(new byte[72]);
            Assert.Equal(72, native.SentLength);
            Assert.Equal(8, expert.GetReport().Length);
        }

        var typed = await MacOSOtpHidConnection.OpenAsync(42, native, TestContext.Current.CancellationToken);
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => typed.SendAsync(new byte[72], TestContext.Current.CancellationToken));
            Assert.Equal(72, native.SentLength);
        }
        finally { await typed.DisposeAsync(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldReport_DrainsBeforeAsyncDisposeAndReleasesOnce(bool get)
    {
        var native = new Lifetime { HoldReport = true };
        var connection = new MacOSHidFeatureReportConnection(42, native);
        var operation = Task.Run(() => { if (get) _ = connection.GetReport(); else connection.SetReport(new byte[13]); }, TestContext.Current.CancellationToken);
        try
        {
            Assert.True(native.InReport.Wait(TimeSpan.FromSeconds(5)));
            var disposal = connection.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
            Assert.Equal(0, native.Releases);
            native.AllowReport.Set();
            await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            connection.Dispose();
            await connection.DisposeAsync();
            Assert.Equal(1, native.Closes);
            Assert.Equal(1, native.Releases);
            Assert.Single(native.Threads.Distinct());
        }
        finally { native.AllowReport.Set(); await connection.DisposeAsync(); }
    }

    [Fact]
    public void NativeGetError_IsPreservedAndPartialBufferCleared()
    {
        var native = new Lifetime { GetStatus = 123 };
        using var connection = new MacOSHidFeatureReportConnection(42, native);
        var failure = Assert.Throws<PlatformApiException>(connection.GetReport);
        Assert.Contains("0000007B", failure.Message, StringComparison.Ordinal);
        Assert.All(native.GetBuffer ?? [], value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    public void ExpertGet_TransfersOriginalEightByteBufferAndPadsShortReports(int length)
    {
        var native = new Lifetime { GetLength = length };
        using var connection = new MacOSHidFeatureReportConnection(42, native);

        byte[] result = connection.GetReport();

        Assert.Same(native.GetBuffer, result);
        Assert.Equal(8, result.Length);
        Assert.Equal(length == 0 ? (byte)0 : (byte)42, result[0]);
        Assert.All(result.Skip(1), value => Assert.Equal(0, value));
    }

    [Fact]
    public void OversizedExpertGet_FailsAndClearsOriginalBuffer()
    {
        var native = new Lifetime { GetLength = 9 };
        using var connection = new MacOSHidFeatureReportConnection(42, native);

        Assert.Throws<InvalidOperationException>(connection.GetReport);
        Assert.All(native.GetBuffer ?? [], value => Assert.Equal(0, value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstructorFailure_UsesCheckedPartialOpenRelease(bool exclusive)
    {
        var native = new Lifetime { OpenStatus = exclusive ? unchecked((int)0xE00002C5) : -1 };
        Assert.Throws<PlatformApiException>(() => new MacOSHidFeatureReportConnection(42, native));
        Assert.Equal(exclusive ? 1 : 0, native.Closes);
        Assert.Equal(1, native.Releases);
    }

    [Fact]
    public void UnprovenPartialOpen_IsNotReportedAsOrdinaryFailure()
    {
        var native = new Lifetime { OpenStatus = unchecked((int)0xE00002C5), CloseSucceeds = false };
        Assert.Throws<UnrecoveredConnectionException>(() => new MacOSHidFeatureReportConnection(42, native));
        Assert.Equal(0, native.Releases);
    }

    private sealed class Lifetime : IIOKitDeviceLifetime
    {
        public bool HoldReport { get; init; }
        public int GetStatus { get; init; }
        public int GetLength { get; init; } = 8;
        public int OpenStatus { get; init; }
        public bool CloseSucceeds { get; init; } = true;
        public int Closes { get; private set; }
        public int Releases { get; private set; }
        public int SentLength { get; private set; }
        public byte[]? GetBuffer { get; private set; }
        public List<int> Threads { get; } = [];
        public ManualResetEventSlim InReport { get; } = new(false);
        public ManualResetEventSlim AllowReport { get; } = new(false);
        private void Record() => Threads.Add(Environment.CurrentManagedThreadId);
        public nint CreateDevice(long id) { Record(); return 42; }
        public void OpenDevice(nint device) => throw new NotSupportedException();
        public int OpenDeviceResult(nint device) { Record(); return OpenStatus; }
        public void CloseDevice(nint device) => throw new NotSupportedException();
        public bool CloseDeviceChecked(nint device) { Record(); Closes++; return CloseSucceeds; }
        public void ReleaseCFObject(nint device) { Record(); Releases++; }
        public int GetFeatureReport(nint device, byte[] buffer, ref long length)
        {
            Record(); GetBuffer = buffer; InReport.Set(); if (HoldReport) AllowReport.Wait();
            if (GetLength > 0) buffer[0] = 42;
            length = GetLength; return GetStatus;
        }
        public int SetFeatureReport(nint device, byte[] buffer)
        {
            Record(); SentLength = buffer.Length; InReport.Set(); if (HoldReport) AllowReport.Wait(); return 0;
        }
        public int GetIntProperty(nint device, string name)
        {
            Record(); return name.Contains("Input", StringComparison.Ordinal) ? 65 : 72;
        }
        public nint CreateRunLoopMode(string name) => throw new NotSupportedException();
        public void RegisterInputReportCallback(nint device, byte[] buffer, int length, nint callback, nint context) { }
        public void RegisterRemovalCallback(nint device, nint callback, nint context) { }
    }
}

PROPOSED SAMPLE — no verdict: core-001:unit:67e1a8bec12453ab / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysScopeTests.cs:396–397
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal ConcurrentBag<IdentityConnection> Connections { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:ad828279bcc1a175 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysScopeTests.cs:486–487
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

            public HidInterfaceType InterfaceType => type;


PROPOSED SAMPLE — no verdict: core-001:unit:0b530ce89cb7095a / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysScopeTests.cs:488–489
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

            public IHidConnection ConnectToIOReports() => throw new InvalidOperationException("No native access in unit tests");


PROPOSED SAMPLE — no verdict: core-001:unit:83af4510d6870b80 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysScopeTests.cs:523–524
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => throw new NotSupportedException();


PROPOSED SAMPLE — no verdict: core-001:unit:4417d95fc05659c6 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysTests.cs:23–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class FindYubiKeysTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:0815dd8ff2a94421 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysTests.cs:62–79
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task FindAllAsync_UnsupportedHid_DoesNotReachFactoryOrAbortScan()
    {
        var findHid = new FakeFindHidInterfaces([
            new FakeHidInterface("unsupported-path", HidInterfaceType.Unknown),
            new FakeHidInterface("fido", HidInterfaceType.Fido)
        ]);
        var factory = new RejectUnsupportedHidFactory();
        var findYubiKeys = new FindYubiKeys(new FakeFindPcscDevices([]), findHid, factory.Create);

        var result = await findYubiKeys.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        var device = Assert.Single(result);
        Assert.Equal(ConnectionType.HidFido, device.AvailableConnections);
        Assert.Equal(1, factory.CreateCalls);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1c7b9d760a3c992a / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysTests.cs:127–128
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class RejectUnsupportedHidFactory
    {


PROPOSED SAMPLE — no verdict: core-001:unit:11f57474a407f72d / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FirmwareVersionSelectResponseTests.cs:20–21
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class FirmwareVersionSelectResponseTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:01a4b75c868acca2 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HotplugSignalsTests.cs:119–120
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public DeviceListenerStatus StartStatus { get; init; } = DeviceListenerStatus.Started;


PROPOSED SAMPLE — no verdict: core-001:unit:42db4fde4cc406d0 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HotplugSignalsTests.cs:120–121
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public bool ThrowOnStart { get; init; }


PROPOSED SAMPLE — no verdict: core-001:unit:3fa9b3935e42253a / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HotplugSignalsTests.cs:142–143
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public void Dispose() => Cleanup("dispose");


PROPOSED SAMPLE — no verdict: core-001:unit:86dd7e0403e14eec / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/HotplugSignalsTests.cs:143–149
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private void Cleanup(string operation)
        {
            calls.Add($"{name}.{operation}");
            if (ThrowOnCleanup)
                throw new InvalidOperationException("cleanup failed");
        }


PROPOSED SAMPLE — no verdict: core-001:unit:450d843b33a6a063 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PhysicalYubiKeyTests.cs:10–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    [Theory]
    [InlineData(ConnectionType.SmartCard, ConnectionType.SmartCard, true)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.HidFido, false)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard, ConnectionType.HidFido, true)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard, ConnectionType.HidOtp, false)]
    [InlineData(ConnectionType.HidFido, ConnectionType.Hid, true)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.Hid, false)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.Unknown, false)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.All, false)]
    [InlineData(ConnectionType.HidFido | ConnectionType.SmartCard, ConnectionType.HidFido | ConnectionType.SmartCard, false)]
    public void SupportsConnection_DefinedSemantics(ConnectionType available, ConnectionType requested, bool expected)
    {
        IYubiKey device = new FakePhysicalYubiKey(available);

        Assert.Equal(expected, device.SupportsConnection(requested));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:f125825211ee249e / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/PhysicalYubiKeyTests.cs:122–123
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ConnectionType Type => type;


PROPOSED SAMPLE — no verdict: core-001:unit:c5f1b3b40ff4c027 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/SnapshotDiffTests.cs:248–249
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public ConnectionType ConnectionType => type;


PROPOSED SAMPLE — no verdict: core-001:unit:4703c2c99161ed3a / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/WindowsDeviceTopologyResolverTests.cs:241–242
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public List<string> ReaderLookups { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:2a405e0a6f71ced1 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/WindowsDeviceTopologyResolverTests.cs:273–274
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public string ReaderName { get; } = readerName;


PROPOSED SAMPLE — no verdict: core-001:unit:5579c68c6c6763a3 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:25–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void FlatSlots_ExposeCombinedConnectionsAndSortedInterfaceIds()
    {
        var device = FullKey(out var smartCard, out var fido, out var otp);

        Assert.Equal(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            device.AvailableConnections);
        Assert.Equal(
            new[] { smartCard.InterfaceId, fido.InterfaceId, otp.InterfaceId }.Order(StringComparer.Ordinal),
            device.InterfaceIds);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:2708dc6180c9ad82 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:119–127
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void HidGroupRouting_RegistrySelectsFidoBeforeOtp()
    {
        var device = FullKey(out _, out var fido, out _);

        Assert.Equal(fido.InterfaceId, DeviceConnectionRegistry.ResolveInterfaceId(device, ConnectionType.Hid));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:b870979a9f4e3530 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:300–312
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    private static YubiKeyDevice FullKey(
        out RecordingSlot smartCard,
        out RecordingSlot fido,
        out RecordingSlot otp,
        DeviceInfo? deviceInfo = null)
    {
        smartCard = new RecordingSlot(ConnectionType.SmartCard);
        fido = new RecordingSlot(ConnectionType.HidFido);
        otp = new RecordingSlot(ConnectionType.HidOtp);
        return new YubiKeyDevice("ykphysical:103", smartCard, fido, otp, deviceInfo);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:9d433e7baf55acb8 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:320–322
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public ConnectionType ConnectionType => connection;


PROPOSED SAMPLE — no verdict: core-001:unit:e99851af4e5d53d8 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:396–398
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Transport Transport => Transport.Usb;


PROPOSED SAMPLE — no verdict: core-001:unit:36bcfc8b36e95ae8 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:398–402
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) => Task.FromResult(ReadOnlyMemory<byte>.Empty);


PROPOSED SAMPLE — no verdict: core-001:unit:ee2c12cc9ea7f04e / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:402–405
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();


PROPOSED SAMPLE — no verdict: core-001:unit:5fd5a4456bc2f98a / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:405–407
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public bool SupportsExtendedApdu() => true;


PROPOSED SAMPLE — no verdict: core-001:unit:61fde3f07bcf9769 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyDeviceTests.cs:416–417
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FakeFidoConnection : IFidoHidConnection
    {


PROPOSED SAMPLE — no verdict: core-001:unit:4b3bc2890cc78b9d / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorAcceptanceTests.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class YubiKeyMonitorAcceptanceTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:48b6250a4ff72a5f / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorAcceptanceTests.cs:98–99
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class DevicePicker(IAsyncEnumerable<DeviceChange> changes, CancellationToken cancellationToken) : IAsyncDisposable
    {


PROPOSED SAMPLE — no verdict: core-001:unit:89f76aaff9cdd2e9 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorAcceptanceTests.cs:118–120
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public ValueTask DisposeAsync() => _changes.DisposeAsync();


PROPOSED SAMPLE — no verdict: core-001:unit:75bc794c75bd22a2 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorTests.cs:23–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);


PROPOSED SAMPLE — no verdict: core-001:unit:c7318fc32f6e0ab7 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/YubiKeyMonitorTests.cs:653–692
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task CreateAsync_CancellationRetainsAdmittedReadLeaseUntilNativeTeardown()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestToken);
        var rig = new FindYubiKeysScopeTests.ScopeRig { PendingOpen = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var creation = YubiKeyMonitor.CreateAsync(rig.CreateFinder(ConnectionType.SmartCard),
            [new(ConnectionType.Hid, () => new OwnedMonitorFixture.FakeDeviceChangeListener())],
            TimeSpan.FromHours(1), new ObservedTimeProvider(), cancellation.Token);
        try
        {
            await rig.OpenEntered.Task.WaitAsync(Bound, TestToken);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => creation.WaitAsync(Bound, TestToken));
            var id = rig.InterfaceId(FindYubiKeysScopeTests.ScopeRig.Reader);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id));
            var recovery = DeviceConnectionRegistry.WaitForRecoveryForTest(id);
            Assert.False(recovery.IsCompleted);
            rig.PendingOpen.SetResult(rig.CreateIdentityConnection(111));
            await recovery.WaitAsync(Bound, TestToken);
            using var next = DeviceConnectionRegistry.TryAcquireDiscovery(id);
            Assert.NotNull(next);
            Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
        }
        finally
        {
            await cancellation.CancelAsync();
            rig.PendingOpen.TrySetException(new InvalidOperationException("Test cleanup"));
            try
            {
                var monitor = await creation.WaitAsync(Bound, CancellationToken.None);
                await monitor.DisposeAsync();
            }
            catch (OperationCanceledException) when (creation.IsCanceled) { }
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:4aa7e90a418de9a1 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/AsyncWait.cs:35–40
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    /// <summary>
    /// Default bound for "this should already have happened". Generous on purpose: these waits gate
    /// on real scheduler progress, and CI machines are slower than laptops.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);


PROPOSED SAMPLE — no verdict: core-001:unit:51c280523f6234e6 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/DeviceChangeWatcher.cs:112–135
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    /// Starts a watcher and waits for SnapshotComplete, proving subscription registration completed.
    /// </summary>
    public static async Task<DeviceChangeWatcher> StartAsync(
        Func<CancellationToken, IAsyncEnumerable<DeviceChange>> watch,
        CancellationToken cancellationToken,
        bool includeReplay = true)
    {
        var watcher = new DeviceChangeWatcher(watch, cancellationToken, includeReplay);

        try
        {
            await watcher._ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        catch
        {
            await watcher.DisposeAsync();
            throw;
        }

        return watcher;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:713cc9ce2a5207bd / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/FakeYubiKey.cs:1–27
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Infrastructure;

/// <summary>
/// Inert <see cref="IYubiKey"/> for tests that only need identity and advertised connections.
/// </summary>
/// <remarks>
/// Connecting throws — this is for tests about discovery, caching, merging, and event plumbing, not
/// about transports. A test that needs <see cref="ConnectAsync{TConnection}"/> to actually hand back a
/// connection is testing something else and should declare its own double rather than extend this one.
/// </remarks>



PROPOSED SAMPLE — no verdict: core-001:unit:163cb964de48b8aa / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/FakeYubiKey.cs:32–34
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public ConnectionType AvailableConnections { get; } = availableConnections;


PROPOSED SAMPLE — no verdict: core-001:unit:9cab1848ff8ab7d2 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/FakeYubiKey.cs:34–38
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection =>
        throw new NotSupportedException("FakeYubiKey does not support connections.");


PROPOSED SAMPLE — no verdict: core-001:unit:8bad4a42b615d77e / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/ObservedTimeProvider.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
internal sealed class ObservedTimeProvider : FakeTimeProvider
{


PROPOSED SAMPLE — no verdict: core-001:unit:70a48e4813608786 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/ObservedTimeProvider.cs:23–30
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _timers.Writer.TryWrite(dueTime);
        return timer;
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7a65a03366eb682e / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/OwnedMonitorFixture.cs:27–28
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public FakeDeviceChangeListener SmartCard { get; } = new();


PROPOSED SAMPLE — no verdict: core-001:unit:680d48b9974bd459 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/OwnedMonitorFixture.cs:75–82
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public async Task AdvanceHotplugAsync()
    {
        await Clock.WaitForTimerAsync(Interval);
        await Clock.WaitForTimerAsync(RescanScheduler.QuietPeriod);
        Clock.Advance(RescanScheduler.QuietPeriod);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:bcdef9ae7ec43a7f / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/OwnedMonitorFixture.cs:103–109
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>> Scan { get; set; } =
            ct =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<IYubiKey>>([]);
            };


PROPOSED SAMPLE — no verdict: core-001:unit:d703dbd287854fe6 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/OwnedMonitorFixture.cs:110–111
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Action? OnRetire { get; set; }


PROPOSED SAMPLE — no verdict: core-001:unit:b64161990dfd9695 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/OwnedMonitorFixture.cs:130–131
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public bool Started => Status == DeviceListenerStatus.Started;


PROPOSED SAMPLE — no verdict: core-001:unit:3a2582bf8fc7edd1 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Infrastructure/TransientScanRetryTests.cs:21–23
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private static InvalidOperationException Saturation() =>
        new(FindPcscDevices.WorkerSaturationMessage);


PROPOSED SAMPLE — no verdict: core-001:unit:58782f112f94c8a1 / A / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:scripts/toolchain/Discovery.cs:6–6
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
sealed record TestProject(string Path, bool UsesTestingPlatformRunner);


PROPOSED SAMPLE — no verdict: core-001:unit:a60076fefa595227 / A / type-declarations
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Tests.Shared/IYubiKeyFilter.cs:51–52
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public interface IYubiKeyFilter
{

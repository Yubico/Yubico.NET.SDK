PROPOSED SAMPLE — no verdict: core-001:unit:2a9641be276feaca / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/HotPlugIdentityContractTests.cs:39–40
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    public Task InitializeAsync() => Task.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:84f185f37bd981c0 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/HotPlugIdentityContractTests.cs:40–42
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    public Task DisposeAsync() => Task.CompletedTask;


PROPOSED SAMPLE — no verdict: core-001:unit:ffe76af0995b6cc7 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.IntegrationTests/Devices/YubiKeyTests.cs:23–24
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class YubiKeyTests : IAsyncLifetime
{


PROPOSED SAMPLE — no verdict: core-001:unit:241694d8cc9f58ee / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CapabilityMapperTests.cs:6–7
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class CapabilityMapperTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:ada359febf4d2cad / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerTests.cs:21–22
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

    private const ushort FullKeyPid = 0x0407;


PROPOSED SAMPLE — no verdict: core-001:unit:ee0a17e4473a845f / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/CompositeDeviceMergerTests.cs:204–205
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FakeSlot(string deviceId, ConnectionType connectionType) : IYubiKeyConnectionSlot
    {


PROPOSED SAMPLE — no verdict: core-001:unit:6f4d2bce814292ac / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/ConnectionTypeTests.cs:19–20
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class ConnectionTypeTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:d371f276b0b17d55 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:25–26
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
public class DeviceIdentityContractTests
{


PROPOSED SAMPLE — no verdict: core-001:unit:b8b678a2e86cffac / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:110–144
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // The replacement has only the metadata established for that scan.
    [Fact]
    public async Task UpdateCache_ConnectionSetChangeRepublication_NewObjectDoesNotInheritSerial()
    {
        var repository = new DeviceChangeFeed();
        using var cts = new CancellationTokenSource(Bound);
        var withSerial = new YubiKeyDevice(
            "ykphysical:103",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            new FakeSlot("hid-fido:a", ConnectionType.HidFido),
            hidOtp: null,
            WithSerial(103));
        repository.Apply([withSerial]);

        await using var watcher = await DeviceChangeWatcher.StartAsync(repository, cts.Token);

        // Same interface set, different connection set, no metadata on the fresh scan object.
        var republished = new YubiKeyDevice(
            "ykphysical:pid:0405",
            new FakeSlot("pcsc:a", ConnectionType.SmartCard),
            hidFido: null,
            new FakeSlot("hid-fido:a", ConnectionType.HidOtp),
            deviceInfo: null);
        repository.Apply([republished]);

        var cached = await DeviceChangeWatcher.ReadSnapshotAsync(repository.Subscribe, cts.Token);
        var events = await watcher.DrainAsync(repository, cts.Token);

        Assert.Equal(2, events.Count);
        var added = Assert.Single(cached);
        Assert.Same(republished, added);
        Assert.Null(added.SerialNumber);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:6cae9db755d5bd3e / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:144–164
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task UpdateCache_RemovalEvent_ObjectRetainsLastKnownSerial()
    {
        var repository = new DeviceChangeFeed();
        using var cts = new CancellationTokenSource(Bound);
        var firstScan = Published(deviceInfo: null);
        repository.Apply([firstScan]);
        repository.Apply([Published(deviceInfo: WithSerial(103))]); // late serial arrival

        await using var watcher = await DeviceChangeWatcher.StartAsync(repository, cts.Token);

        repository.Apply([]);

        var events = await watcher.DrainAsync(repository, cts.Token);

        var removal = Assert.IsType<DeviceRemoved>(Assert.Single(events));
        Assert.Same(firstScan, removal.Device);
        Assert.Equal(103, removal.Device.SerialNumber);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:129c402fc85f1cea / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:164–176
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // ---------------------------------------------------------------------------------------------
    // SameDeviceAs tri-state truth table
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void SameDeviceAs_SameReferenceWithUnknownSerial_Same()
    {
        IYubiKey device = Published(deviceInfo: null);

        Assert.Equal(DeviceCorrelation.Same, device.SameDeviceAs(device));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:88137649bb50251c / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:239–250
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static DeviceInfo WithSerial(int serial) =>
        default(DeviceInfo) with
        {
            FirmwareVersion = new FirmwareVersion(5, 7, 2),
            SerialNumber = serial
        };


PROPOSED SAMPLE — no verdict: core-001:unit:7e041972161b3438 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:264–268
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     An external implementation declaring only the abstract interface members.
    /// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:c84fdf084fc2b106 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:269–270
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class BareThirdPartyYubiKey(string deviceId) : IYubiKey
    {


PROPOSED SAMPLE — no verdict: core-001:unit:1d6c3d9681522a9b / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:277–282
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    /// <summary>
    ///     A deliberately minimal external implementation: only the abstract members, plus an optional
    ///     serial override to exercise the default <see cref="IYubiKey.SameDeviceAs" /> semantics.
    /// </summary>



PROPOSED SAMPLE — no verdict: core-001:unit:c6bd7a57ecbcdb99 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceIdentityContractTests.cs:284–285
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public string DeviceId { get; } = deviceId;


PROPOSED SAMPLE — no verdict: core-001:unit:df7b185fc050c276 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:123–136
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ReadAsync_SmartCardInvalidPageLength_ThrowsPageAwareBadResponse()
    {
        var protocol = new FakeSmartCardProtocol([0x02, 0x01]);

        var ex = await Assert.ThrowsAsync<BadResponseException>(
            () => DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken));

        Assert.Contains("page 0", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("declared 2", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("actual 1", ex.Message, StringComparison.OrdinalIgnoreCase);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5819f85c176432a0 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:163–175
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ReadAsync_FidoSinglePage_ParsesDeviceInfo()
    {
        var protocol = new FakeFidoHidProtocol(BuildPage(CreateRequiredDeviceInfoTlvs()));

        var info = await DeviceInfoReader.ReadAsync(protocol, null, TestContext.Current.CancellationToken);

        Assert.Equal(0x01020304, info.SerialNumber);
        Assert.Equal(new FirmwareVersion(5, 7, 2), info.FirmwareVersion);
        Assert.Equal([0], protocol.RequestedPages);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:0985fedf9fa9be5c / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:300–301
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly Queue<byte[]> _pages = new(pages);


PROPOSED SAMPLE — no verdict: core-001:unit:3f42a5d73901f082 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:327–329
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public List<byte> RequestedPages { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:a44c5039d794914f / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:336–346
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<ReadOnlyMemory<byte>> SendCommandAsync(
            byte command,
            ReadOnlyMemory<byte> data,
            global::Yubico.YubiKit.Core.Credentials.UserPresenceNotification userPresenceNotification,
            CancellationToken cancellationToken = default)
        {
            RequestedPages.Add(data.Span[0]);
            return Task.FromResult<ReadOnlyMemory<byte>>(_pages.Dequeue());
        }


PROPOSED SAMPLE — no verdict: core-001:unit:f765dbcbbf891d58 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:346–348
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }


PROPOSED SAMPLE — no verdict: core-001:unit:178443944681bf16 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoReaderTests.cs:355–357
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public List<byte> RequestedPages { get; } = [];


PROPOSED SAMPLE — no verdict: core-001:unit:5c75f53b740f7a56 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoTests.cs:53–65
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void CreateFromTlvs_InvalidVersionQualifierLength_ThrowsBadResponseException()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs([new Tlv(0x19, [0x01, 0x03, 0x05])]);

        var ex = Assert.Throws<BadResponseException>(() =>
            DeviceInfo.CreateFromTlvs(tlvs, new FirmwareVersion(5, 7, 2)));

        Assert.Contains("tag 0x19", ex.Message);
        Assert.Contains("length 3", ex.Message);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5248e323347ed4ed / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DeviceInfoTests.cs:65–83
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void CreateFromTlvs_VersionQualifierMissingType_ThrowsBadResponseException()
    {
        using var tlvs = CreateRequiredDeviceInfoTlvs([
            new Tlv(0x19,
            [
                0x01, 0x03, 0x05, 0x07, 0x02,
                0x03, 0x04, 0x00, 0x00, 0x00, 0x01,
                0x04, 0x01, 0x00
            ])
        ]);

        var ex = Assert.Throws<BadResponseException>(() =>
            DeviceInfo.CreateFromTlvs(tlvs, new FirmwareVersion(5, 7, 2)));

        Assert.Contains("tag 0x02", ex.Message);
    }


PROPOSED SAMPLE — no verdict: core-001:unit:5b10960d28bc25d8 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoveryIdentityReaderTests.cs:75–78
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public TaskCompletionSource ConnectFinished { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);


PROPOSED SAMPLE — no verdict: core-001:unit:264b9a61da0148c8 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoveryReadSchedulingTests.cs:67–103
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindAllAsync_PotentialSiblingReads_WaitForNativeDisposal(bool unparsedCcid)
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var rig = new ScopeRig
        {
            IncludeSecondKey = true,
            PrimaryReader = unparsedCcid ? "Yubico YubiKey future USB reader" : ScopeRig.Reader
        };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var (held, siblingOpened) = HoldFirstDisposal(rig, release.Task);
        var scan = rig.CreateFinder(ConnectionType.All).FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
        try
        {
            await held.DisposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            // Metadata has already returned, but native disposal is still active. A sibling must not open.
            await Assert.ThrowsAsync<TimeoutException>(() =>
                siblingOpened.WaitAsync(TimeSpan.FromMilliseconds(150), TestContext.Current.CancellationToken));
            Assert.Equal(0, held.DisposeCount);

            release.SetResult();
            AssertComplete(await scan, ConnectionTypeExtensions.ConcreteConnections);
            Assert.Equal(6, rig.OpenCalls.Values.Sum());
            Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
        }
        finally
        {
            release.TrySetResult();
            // Native-owner fakes must drain even if the runner has canceled this test.
            await scan.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:06d91e3f9f9dbfde / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:257–292
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public async Task ReadBoundedAsync_OneWaiterCancels_DoesNotCancelSharedUnderlyingRead()
    {
        var device = new ControllableConnectYubiKey(cancelConnectWithCaller: true);
        using var firstWaiter = new CancellationTokenSource();

        try
        {
            var firstRead = DiscoveryDeviceInfoReader.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromSeconds(5),
                NullLogger.Instance,
                firstWaiter.Token);
            await device.ConnectStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            firstWaiter.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstRead);

            await Assert.ThrowsAsync<TimeoutException>(() => DiscoveryDeviceInfoReader.ReadBoundedAsync(
                device,
                ConnectionType.SmartCard,
                TimeSpan.FromMilliseconds(50),
                NullLogger.Instance,
                TestContext.Current.CancellationToken));

            Assert.Equal(1, device.ConnectCalls);
        }
        finally
        {
            device.FailConnect();
            await device.ConnectFinished.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:7272d5c9e7cc2028 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:537–539
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public int ConnectCalls => Volatile.Read(ref _connectCalls);


PROPOSED SAMPLE — no verdict: core-001:unit:ccd03d010c5e42ee / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:613–615
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void ReleaseSynchronousBlock() => _releaseSynchronousBlock.Set();


PROPOSED SAMPLE — no verdict: core-001:unit:30569540689bd08c / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:648–650
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public int ConnectCalls => Volatile.Read(ref _connectCalls);


PROPOSED SAMPLE — no verdict: core-001:unit:e59a7fc4150cd7c1 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:660–665
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) =>
            ConnectAsync<IConnection>(cancellationToken);


PROPOSED SAMPLE — no verdict: core-001:unit:fd5536c255cb40d9 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:674–676
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public int ConnectCalls => Volatile.Read(ref _connectCalls);


PROPOSED SAMPLE — no verdict: core-001:unit:21e721dfeef0acc0 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/DiscoverySingleFlightTests.cs:710–712
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public int CreateCalls => Volatile.Read(ref _createCalls);


PROPOSED SAMPLE — no verdict: core-001:unit:e1750f48a7e48d56 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FeatureTests.cs:27–36
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Fact]
    public void IsSupportedByFirmware_WhenFirmwareMeetsFeatureVersion_ReturnsTrue()
    {
        var feature = new Feature("Test feature", 5, 8, 0);

        Assert.True(feature.IsSupportedByFirmware(new FirmwareVersion(5, 8, 0)));
        Assert.True(feature.IsSupportedByFirmware(new FirmwareVersion(5, 8, 1)));
    }


PROPOSED SAMPLE — no verdict: core-001:unit:ee61d4edd0255704 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysEnumerationBudgetTests.cs:79–120
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MonitoredFindAllAsync_HungTransport_ThrowsInsteadOfReturningPartialScan(bool hangPcsc)
    {
        var pcsc = new PcscEnumerator();
        var hid = new HidEnumerator();
        var pendingPcsc = new TaskCompletionSource<IReadOnlyList<IPcscDevice>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingHid = new TaskCompletionSource<IReadOnlyList<IHidInterface>>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (hangPcsc)
            pcsc.Pending = pendingPcsc.Task;
        else
            hid.Pending = pendingHid.Task;

        var finder = new FindYubiKeys(pcsc, hid, CreateSlot, enumerationBudget: TimeSpan.Zero);
        IMonitoredDiscovery monitored = finder;
        try
        {
            // A monitor diffs against its previous snapshot, so a partial scan would read as removals.
            // The watchdog sits outside the assertion so only discovery's own budget can satisfy it.
            var scan = monitored.FindAllAsync(TestContext.Current.CancellationToken);
            var completed = await Task.WhenAny(scan, Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            Assert.Same(scan, completed);
            var timeout = await Assert.ThrowsAsync<TimeoutException>(() => scan);
            Assert.Contains("not published", timeout.Message, StringComparison.Ordinal);

            pendingPcsc.SetResult([new PcscDevice()]);
            pendingHid.SetResult([new HidInterface()]);
            pcsc.Pending = null;
            hid.Pending = null;
            var recovered = await monitored.FindAllAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, recovered.Count);
        }
        finally
        {
            pendingPcsc.TrySetResult([]);
            pendingHid.TrySetResult([]);
            finder.RetireReadScope();
        }
    }


PROPOSED SAMPLE — no verdict: core-001:unit:1d6488ddfeddc05b / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysEnumerationBudgetTests.cs:208–209
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal Task<IReadOnlyList<IHidInterface>>? Pending { get; set; }


PROPOSED SAMPLE — no verdict: core-001:unit:6f9b6d5f4aa9c49c / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysEnumerationBudgetTests.cs:228–229
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public HidInterfaceType InterfaceType => HidInterfaceType.Otp;


PROPOSED SAMPLE — no verdict: core-001:unit:8ad47adcb57fb1ec / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:739–740
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class MutableFindPcscDevices : IFindPcscDevices
    {


PROPOSED SAMPLE — no verdict: core-001:unit:fc02fc15609161b7 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:741–744
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Devices);


PROPOSED SAMPLE — no verdict: core-001:unit:a04e1e78211fc8ee / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:758–759
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public string ReaderName { get; } = readerName;


PROPOSED SAMPLE — no verdict: core-001:unit:e8433d7f15d34b70 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:764–765
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FakeHidInterface(short productId, HidInterfaceType interfaceType, string name) : IHidInterface
    {


PROPOSED SAMPLE — no verdict: core-001:unit:457eb72bbf026185 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:787–788
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class ScriptedIdentityFactory
    {


PROPOSED SAMPLE — no verdict: core-001:unit:c0b686c061e5e6d6 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:788–789
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        private readonly string _prefix = $"test-fi:{Guid.NewGuid():N}";


PROPOSED SAMPLE — no verdict: core-001:unit:42d5bf06a4834d36 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:801–803
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        public void FailReads(string name) => _scripts[name] = (ReadOutcome.Fail, 0);


PROPOSED SAMPLE — no verdict: core-001:unit:1b00e9525e807878 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysFaultInjectionTests.cs:815–823
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:


        /// <summary>Fails all pending blocked connects; later blocked connects fail immediately (sticky).</summary>
        public void FailAllBlockedReads()
        {
            _failAllBlocked = true;
            foreach (var blocked in _blocked)
                blocked.TrySetException(new InvalidOperationException("Expected blocked-read cleanup failure."));
        }


PROPOSED SAMPLE — no verdict: core-001:unit:6d9764a032730fca / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysPidMergeTests.cs:99–101
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(devices);


PROPOSED SAMPLE — no verdict: core-001:unit:9146747552870875 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysPidMergeTests.cs:104–105
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:
    private sealed class FakeFindHidInterfaces(IReadOnlyList<IHidInterface> devices) : IFindHidInterfaces
    {


PROPOSED SAMPLE — no verdict: core-001:unit:0f08c4ac8fa60582 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysPidMergeTests.cs:121–122
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();


PROPOSED SAMPLE — no verdict: core-001:unit:4c687969a62af799 / A / tests-and-helpers
Original fe342fb0b6fdc4087ed4cbe4ee292e5039f5a7ef:src/Core/tests/Yubico.YubiKit.Core.UnitTests/Devices/FindYubiKeysScopeTests.cs:393–394
Full exact original unit; enclosing owner/helper/assertion/cleanup context remains required:

        internal List<string> CreatedSlots { get; } = [];

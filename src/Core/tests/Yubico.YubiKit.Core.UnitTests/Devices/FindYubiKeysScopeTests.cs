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

using System.Buffers.Binary;
using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Protocols.SmartCard.Apdu.Fakes;
using Yubico.YubiKit.Core.Utilities;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class FindYubiKeysScopeTests
{
    [Theory]
    [InlineData(ConnectionType.Unknown, ConnectionType.Unknown, 0, 0)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.SmartCard, 1, 0)]
    [InlineData(ConnectionType.HidFido, ConnectionType.HidFido, 0, 1)]
    [InlineData(ConnectionType.HidOtp, ConnectionType.HidOtp, 0, 1)]
    [InlineData(ConnectionType.Hid, ConnectionType.HidFido | ConnectionType.HidOtp, 0, 1)]
    [InlineData(ConnectionType.SmartCard | ConnectionType.HidOtp, ConnectionType.SmartCard | ConnectionType.HidOtp, 1, 1)]
    [InlineData(ConnectionType.All, ConnectionType.HidFido | ConnectionType.HidOtp | ConnectionType.SmartCard, 1, 1)]
    [InlineData((ConnectionType)0x80, ConnectionType.Unknown, 0, 0)]
    [InlineData(ConnectionType.HidOtp | (ConnectionType)0x80, ConnectionType.HidOtp, 0, 1)]
    public async Task ScopedFinder_OnlyEnumeratesRequestedFamiliesAndBuildsRequestedSlots(
        ConnectionType scope, ConnectionType expected, int pcscCalls, int hidCalls)
    {
        var rig = new ScopeRig();
        var finder = rig.CreateFinder(scope);

        var devices = await finder.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(pcscCalls, rig.Pcsc.Calls);
        Assert.Equal(hidCalls, rig.Hid.Calls);
        Assert.Equal(expected, devices.Aggregate(ConnectionType.Unknown, (all, device) => all | device.AvailableConnections));
        var expectedNames = rig.NamesFor(expected).Order().ToList();
        Assert.Equal(expectedNames, rig.CreatedSlots.Order());
        Assert.Equal(expectedNames, rig.TopologyCalls.Order());
        Assert.All(rig.OpenCalls.Keys, name => Assert.Contains(name, expectedNames));
        Assert.Equal(expectedNames.Count, devices.Sum(device => Assert.IsType<LocalYubiKey>(device).InterfaceIds.Count));
    }

    [Fact]
    public async Task FindAllAsync_ResultFilterDoesNotNarrowEnumerationOrCompositeSlots()
    {
        var rig = new ScopeRig();
        var finder = rig.CreateFinder(ConnectionType.All);

        var device = Assert.Single(await finder.FindAllAsync(ConnectionType.HidFido, TestContext.Current.CancellationToken));

        Assert.Equal(ConnectionTypeExtensions.ConcreteConnections, device.AvailableConnections);
        Assert.Equal(1, rig.Pcsc.Calls);
        Assert.Equal(1, rig.Hid.Calls);
        Assert.Equal(3, Assert.IsType<LocalYubiKey>(device).InterfaceIds.Count);

        var again = Assert.Single(await finder.FindAllAsync(ConnectionType.SmartCard, TestContext.Current.CancellationToken));
        Assert.Equal(ConnectionTypeExtensions.ConcreteConnections, again.AvailableConnections);
        Assert.Equal(2, rig.Pcsc.Calls);
        Assert.Equal(2, rig.Hid.Calls);
        Assert.Equal(1, rig.OpenCalls.Values.Sum()); // The existing finder's successful metadata cache survives result-filter changes.
    }

    [Fact]
    public async Task ScopedFinder_PartialTriplePidWithoutIdentity_DoesNotTreatScopeAsPidCompleteness()
    {
        var rig = new ScopeRig { FailReads = true };
        var finder = rig.CreateFinder(ConnectionType.Hid);

        var devices = await finder.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, devices.Count);
        Assert.All(devices, device => Assert.Single(Assert.IsType<LocalYubiKey>(device).InterfaceIds));
        Assert.DoesNotContain(devices, device => device.DeviceId.StartsWith("ykphysical:pid:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScopedFinder_TwoSamePidKeysWithPartialObservations_UsesOnlyInScopeSerialEvidence()
    {
        var rig = new ScopeRig { IncludeSecondKey = true };
        var finder = rig.CreateFinder(ConnectionType.Hid);

        var devices = await finder.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, devices.Count);
        Assert.Equal(new int?[] { 111, 222 }, devices.Select(device => device.SerialNumber).Order());
        Assert.All(devices, device => Assert.Equal(ConnectionType.HidFido | ConnectionType.HidOtp, device.AvailableConnections));
        Assert.Equal(0, rig.Pcsc.Calls);
        Assert.Equal(4, rig.OpenCalls.Values.Sum());
        Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
    }

    [Fact]
    public async Task ScopedFinder_TwoSamePidKeysWithoutIdentity_DoesNotCollapsePartialViews()
    {
        var rig = new ScopeRig { IncludeSecondKey = true, FailReads = true };
        var devices = await rig.CreateFinder(ConnectionType.Hid)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(4, devices.Count);
        Assert.All(devices, device => Assert.Single(Assert.IsType<LocalYubiKey>(device).InterfaceIds));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScanAsync_RepeatScansRereadMetadataAndIdentityRatherThanSharingEvidence(bool multipleKeys)
    {
        var rig = new ScopeRig { IncludeSecondKey = multipleKeys };
        var firstScan = await YubiKeyManager.ScanAsync(
            ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder);
        var first = Assert.Single(firstScan, key => key.SerialNumber == 111);
        rig.Serial = 333;
        var secondScan = await YubiKeyManager.ScanAsync(
            ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder);
        var second = Assert.Single(secondScan, key => key.SerialNumber == 333);

        Assert.NotSame(first, second);
        Assert.Equal(111, first.SerialNumber);
        Assert.Equal(333, second.SerialNumber);
        Assert.Equal(2, rig.Pcsc.Calls);
        Assert.Equal(multipleKeys ? 4 : 2, rig.OpenCalls.Values.Sum());
        Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
    }

    [Fact]
    public async Task ScanAsync_ChangingScopeNeverImportsSiblingSlotsFromPriorScan()
    {
        var rig = new ScopeRig();
        var all = await YubiKeyManager.ScanAsync(ConnectionType.All, TestContext.Current.CancellationToken, rig.CreateFinder);
        var hid = await YubiKeyManager.ScanAsync(ConnectionType.Hid, TestContext.Current.CancellationToken, rig.CreateFinder);
        var ccid = await YubiKeyManager.ScanAsync(ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder);

        Assert.Equal(ConnectionTypeExtensions.ConcreteConnections, Assert.Single(all).AvailableConnections);
        Assert.All(hid, key => Assert.Equal(ConnectionType.Unknown, key.AvailableConnections & ConnectionType.SmartCard));
        Assert.Equal(2, hid.Sum(key => Assert.IsType<LocalYubiKey>(key).InterfaceIds.Count));
        Assert.Equal(ConnectionType.SmartCard, Assert.Single(ccid).AvailableConnections);
    }

    [Fact]
    public async Task ScanAsync_ConcurrentCallsHaveIndependentReadScopesButShareInterfaceOwnership()
    {
        var rig = new ScopeRig { PendingOpen = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var first = YubiKeyManager.ScanAsync(ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder);
        try
        {
            await rig.OpenEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            // A second fresh finder must skip the leased interface, not join the first finder's blocked read.
            var second = Assert.Single(await YubiKeyManager.ScanAsync(
                ConnectionType.SmartCard, TestContext.Current.CancellationToken, rig.CreateFinder)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            Assert.Null(second.CachedDeviceInfo);
            Assert.Equal(1, rig.OpenCalls.Values.Sum());

            rig.PendingOpen.SetResult(rig.CreateIdentityConnection(111));
            Assert.Equal(111, Assert.Single(await first).SerialNumber);
            Assert.Null(second.CachedDeviceInfo);
        }
        finally
        {
            rig.PendingOpen.TrySetException(new InvalidOperationException("Test cleanup"));
            // The read owns these fakes; release/drain it before the test can leave the collection.
            await first.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    [Fact]
    public async Task ScopedViews_OverlappingAllAndScopedConnectionsExcludeEachOtherInBothDirections()
    {
        var rig = new ScopeRig();
        var all = Assert.Single(await rig.CreateFinder(ConnectionType.All)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken));
        var scoped = Assert.Single(await rig.CreateFinder(ConnectionType.SmartCard)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken));
        var repeated = Assert.Single(await rig.CreateFinder(ConnectionType.SmartCard)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken));

        await using (var held = await all.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken))
        {
            await Assert.ThrowsAsync<ConnectionInUseException>(() => scoped.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken));
        }

        await using (var held = await scoped.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken))
        {
            await Assert.ThrowsAsync<ConnectionInUseException>(() => all.ConnectAsync<IFidoHidConnection>(TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<ConnectionInUseException>(() => repeated.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken));
            var before = rig.OpenCalls.Values.Sum();
            var during = Assert.Single(await rig.CreateFinder(ConnectionType.SmartCard)
                .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken));
            Assert.Null(during.CachedDeviceInfo);
            Assert.Equal(before, rig.OpenCalls.Values.Sum());
        }

        await using var reopened = await repeated.ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ScopedViews_DisjointMembersEvenWithSameSerial_HaveOnlyPerInterfaceExclusion()
    {
        var rig = new ScopeRig();
        // Merely observing a complete group does not register persistent physical-identity aliases.
        Assert.Single(await rig.CreateFinder(ConnectionType.All)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken));
        var ccid = Assert.IsType<LocalYubiKey>(Assert.Single(await rig.CreateFinder(ConnectionType.SmartCard)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)));
        var fido = Assert.IsType<LocalYubiKey>(Assert.Single(await rig.CreateFinder(ConnectionType.HidFido)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)));
        Assert.Equal(ccid.SerialNumber, fido.SerialNumber);
        Assert.Empty(ccid.InterfaceIds.Intersect(fido.InterfaceIds));

        // Pin the actual coordination bound, not a supported parallel-use recommendation: matching serials
        // do not add a registry alias. No global correlation or native open is involved in these claims.
        using var ccidClaim = await DeviceConnectionRegistry.AcquireConnectionAsync(ccid.InterfaceIds, TestContext.Current.CancellationToken);
        var beforeProbe = rig.OpenCalls.Values.Sum();
        var disjointProbe = Assert.Single(await rig.CreateFinder(ConnectionType.HidFido)
            .FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken));
        Assert.Equal(111, disjointProbe.SerialNumber);
        Assert.Equal(beforeProbe + 1, rig.OpenCalls.Values.Sum());
        using var fidoClaim = await DeviceConnectionRegistry.AcquireConnectionAsync(fido.InterfaceIds, TestContext.Current.CancellationToken);
        Assert.All(ccid.InterfaceIds.Concat(fido.InterfaceIds), id => Assert.True(DeviceConnectionRegistry.IsInUse(id)));
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ScanAsync_CancellationRetainsOpenedReadOwnershipUntilBackgroundTeardown()
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var rig = new ScopeRig { PendingOpen = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var scan = YubiKeyManager.ScanAsync(ConnectionType.SmartCard, cancellation.Token, rig.CreateFinder);
        try
        {
            await rig.OpenEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scan.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

            var id = rig.InterfaceId(ScopeRig.Reader);
            Assert.Throws<UnrecoveredConnectionException>(() => DeviceConnectionRegistry.TryAcquireDiscovery(id));
            var recovery = DeviceConnectionRegistry.WaitForRecoveryForTest(id);
            Assert.False(recovery.IsCompleted);
            rig.PendingOpen.SetResult(rig.CreateIdentityConnection(111));
            await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            using var next = DeviceConnectionRegistry.TryAcquireDiscovery(id);
            Assert.NotNull(next);
            Assert.All(rig.Connections, connection => Assert.Equal(1, connection.DisposeCount));
        }
        finally
        {
            rig.PendingOpen.TrySetException(new InvalidOperationException("Test cleanup"));
            // The abandoned read owns these fakes; drain even when the runner has canceled the test.
            await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "RuntimeResilience")]
    public async Task ScanAsync_ExitRetiresQueuedIdentityReadsBeforeLaterWorkerAdmission(bool cancel)
    {
        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        var admissions = new List<IDisposable>();
        var rig = new ScopeRig { IncludeSecondKey = true };
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        try
        {
            for (var i = 0; i < DiscoveryWorkerAdmission.MaximumConcurrentWorkers; i++)
            {
                Assert.True(DiscoveryWorkerAdmission.TryAcquire(out var admission));
                admissions.Add(admission);
            }

            FindYubiKeys? oneShot = null;
            var scan = YubiKeyManager.ScanAsync(ConnectionType.SmartCard, cancellation.Token, scope => oneShot = rig.CreateFinder(scope));
            Assert.Equal(2, rig.CreatedSlots.Count);
            if (cancel)
            {
                await cancellation.CancelAsync();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scan);
            }
            else
            {
                Assert.Equal(2, (await scan.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken)).Count);
            }

            // Prove retirement before releasing worker capacity, rather than relying on a delay to detect
            // late opens. A live scope would join its queued reads and wait the full 2s identity budget.
            Assert.NotNull(oneShot);
            await oneShot.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            Assert.Empty(rig.OpenCalls);
        }
        finally
        {
            foreach (var admission in admissions)
                admission.Dispose();
        }

        await DiscoveryWorkerAdmissionCollection.WaitUntilIdleAsync(TestContext.Current.CancellationToken);
        Assert.Empty(rig.OpenCalls);
    }

    internal sealed class ScopeRig : IDeviceTopologyResolver
    {
        internal const string Reader = "Yubico YubiKey OTP+FIDO+CCID";
        private readonly string _prefix = $"scope:{Guid.NewGuid():N}:";
        internal int Serial { get; set; } = 111;
        internal bool IncludeSecondKey { get; init; }
        internal bool FailReads { get; init; }
        internal TaskCompletionSource<IConnection>? PendingOpen { get; init; }
        internal TaskCompletionSource OpenEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal PcscFinder Pcsc { get; } = new();
        internal HidFinder Hid { get; } = new();
        internal List<string> CreatedSlots { get; } = [];
        internal List<string> TopologyCalls { get; } = [];
        internal ConcurrentDictionary<string, int> OpenCalls { get; } = new();
        internal ConcurrentBag<IdentityConnection> Connections { get; } = [];

        internal FindYubiKeys CreateFinder(ConnectionType scope)
        {
            Pcsc.Devices = IncludeSecondKey ? [new PcscInterface(Reader), new PcscInterface(Reader + " 01")] : [new PcscInterface(Reader)];
            Hid.Devices = IncludeSecondKey
                ? [new HidInterface("fido", HidInterfaceType.Fido), new HidInterface("otp", HidInterfaceType.Otp),
                    new HidInterface("fido2", HidInterfaceType.Fido), new HidInterface("otp2", HidInterfaceType.Otp)]
                : [new HidInterface("fido", HidInterfaceType.Fido), new HidInterface("otp", HidInterfaceType.Otp), new HidInterface("unsupported", HidInterfaceType.Unknown)];
            return new FindYubiKeys(Pcsc, Hid, CreateSlot, scope, this);
        }

        internal IEnumerable<string> NamesFor(ConnectionType connections)
        {
            if (connections.SupportsConnection(ConnectionType.SmartCard)) yield return Reader;
            if (connections.SupportsConnection(ConnectionType.HidFido)) yield return "fido";
            if (connections.SupportsConnection(ConnectionType.HidOtp)) yield return "otp";
        }

        internal string InterfaceId(string name) => _prefix + name;

        private IYubiKeyConnectionSlot CreateSlot(IDevice device)
        {
            CreatedSlots.Add(device.ReaderName);
            var connection = device is IHidInterface hid ? ConnectionTypeMapper.ToConnectionType(hid.InterfaceType) : ConnectionType.SmartCard;
            return new Slot(this, device.ReaderName, connection);
        }

        public bool TryGetTopologyKey(IDevice device, ConnectionType connection, out string? topologyKey)
        {
            TopologyCalls.Add(device.ReaderName);
            topologyKey = null;
            return false;
        }

        private Task<IConnection> OpenAsync(string name)
        {
            OpenCalls.AddOrUpdate(name, 1, (_, calls) => calls + 1);
            OpenEntered.TrySetResult();
            if (PendingOpen is { } pending) return pending.Task;
            if (FailReads) return Task.FromException<IConnection>(new InvalidOperationException("Scripted unreadable identity"));
            return Task.FromResult<IConnection>(CreateIdentityConnection(name.EndsWith('2') || name.EndsWith(" 01", StringComparison.Ordinal) ? 222 : Serial));
        }

        internal IdentityConnection CreateIdentityConnection(int serial)
        {
            // ProtocolDeviceInfo dispatches on the returned connection type, so the same scripted
            // SELECT + GetDeviceInfo responses exercise identity/metadata reads for HID slots too.
            var inner = new FakeSmartCardConnection();
            inner.EnqueueResponse(new byte[] { 0x90, 0x00 });
            var serialBytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(serialBytes, serial);
            var encoded = TlvHelper.EncodeAndDisposeList(
            [
                new Tlv(0x0A, [0x00]), new Tlv(0x04, [(byte)FormFactor.UsbAKeychain]),
                new Tlv(0x18, [0x00]), new Tlv(0x03, [0x00, 0x01]), new Tlv(0x01, [0x00, 0x01]),
                new Tlv(0x0E, [0x00]), new Tlv(0x0D, [0x00]), new Tlv(0x14, [0x00]), new Tlv(0x15, [0x00]),
                new Tlv(0x06, [0x00, 0x00]), new Tlv(0x07, [0x2A]), new Tlv(0x08, [0x00]),
                new Tlv(0x05, [0x05, 0x07, 0x02]), new Tlv(0x02, serialBytes)
            ]);
            var response = new byte[encoded.Length + 3];
            response[0] = (byte)encoded.Length;
            encoded.Span.CopyTo(response.AsSpan(1));
            response[^2] = 0x90;
            inner.EnqueueResponse(response);
            var connection = new IdentityConnection(inner);
            Connections.Add(connection);
            return connection;
        }

        private sealed class Slot(ScopeRig rig, string name, ConnectionType connection) : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
        {
            public string InterfaceId => rig.InterfaceId(name);
            public ConnectionType ConnectionType => connection;
            public Task<IConnection> OpenRawConnectionAsync(ConnectionType connectionType, CancellationToken cancellationToken) => rig.OpenAsync(name);
            public Task<IConnection> ConnectForDiscoveryAsync(ConnectionType connectionType, CancellationToken cancellationToken) => rig.OpenAsync(name);
        }

        private sealed class PcscInterface(string name) : IPcscDevice
        {
            public string ReaderName => name;
            public AnswerToReset? Atr => null;
            public PscsConnectionKind Kind => PscsConnectionKind.Usb;
        }

        private sealed class HidInterface(string name, HidInterfaceType type) : IHidInterface
        {
            public string ReaderName => name;
            public HidDescriptorInfo DescriptorInfo { get; } = new() { VendorId = 0x1050, ProductId = 0x0407 };
            public HidInterfaceType InterfaceType => type;
            public IHidConnection ConnectToFeatureReports() => throw new InvalidOperationException("No native access in unit tests");
            public IHidConnection ConnectToIOReports() => throw new InvalidOperationException("No native access in unit tests");
        }
    }

    internal sealed class PcscFinder : IFindPcscDevices
    {
        internal int Calls { get; private set; }
        internal IReadOnlyList<IPcscDevice> Devices { get; set; } = [];
        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Devices);
        }
    }

    internal sealed class HidFinder : IFindHidInterfaces
    {
        internal int Calls { get; private set; }
        internal IReadOnlyList<IHidInterface> Devices { get; set; } = [];
        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Devices);
        }
    }

    internal sealed class IdentityConnection(FakeSmartCardConnection inner) : ISmartCardConnection
    {
        internal int DisposeCount { get; private set; }
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public bool SupportsExtendedApdu() => true;
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default) => inner.TransmitAndReceiveAsync(command, cancellationToken);
        public void Dispose() { DisposeCount++; inner.Dispose(); }
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
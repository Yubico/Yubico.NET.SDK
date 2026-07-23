// Copyright 2026 Yubico AB
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

using System.Diagnostics;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class FindYubiKeysPidMergeTests
{
    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task FindAllAsync_DuplicatePidBlockedIdentityReads_AreBoundedAsOneGroup()
    {
        var factory = new BlockingIdentityFactory();
        var find = new FindYubiKeys(
            new FakeFindPcscDevices([
                new FakePcscDevice("Yubico YubiKey OTP+FIDO+CCID", PscsConnectionKind.Usb),
                new FakePcscDevice("Yubico YubiKey OTP+FIDO+CCID 01", PscsConnectionKind.Usb)
            ]),
            new FakeFindHidDevices([]),
            factory);

        IReadOnlyList<IYubiKey> result;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);
            stopwatch.Stop();
        }
        finally
        {
            factory.FailAllReads();
        }

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Independent 2s identity budgets accumulated sequentially for {stopwatch.Elapsed}.");
        Assert.Equal(2, result.Count);
        Assert.Equal(2, factory.TotalConnectCalls);
        Assert.All(result, device => Assert.Equal(ConnectionType.SmartCard, device.AvailableConnections));
    }

    [Fact]
    public async Task FindAllAsync_FullKey_MergesByPid_EvenWhenEveryConnectFails()
    {
        // One physical key (CCID + FIDO + OTP, PID 0x0407). Every ConnectAsync throws (simulating an
        // exclusive CCID holder / unavailable interfaces). Grouping must still merge by PID alone.
        var find = new FindYubiKeys(
            new FakeFindPcscDevices([new FakePcscDevice("Yubico YubiKey OTP+FIDO+CCID 00 00", PscsConnectionKind.Usb)]),
            new FakeFindHidDevices([new FakeHidDevice(0x0407, HidInterfaceType.Fido), new FakeHidDevice(0x0407, HidInterfaceType.Otp)]),
            new ThrowingFactory());

        var result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        var device = Assert.Single(result);
        Assert.Equal("ykphysical:pid:0407", device.DeviceId);
        Assert.Equal(ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            device.AvailableConnections);
    }

    [Fact]
    public async Task FindAllAsync_HidProductIdZero_NotMergedOnSharedZeroPid()
    {
        // Defaulted/unknown ProductId (0) must not become a merge key — the interfaces stay separate.
        var find = new FindYubiKeys(
            new FakeFindPcscDevices([]),
            new FakeFindHidDevices([new FakeHidDevice(0, HidInterfaceType.Fido), new FakeHidDevice(0, HidInterfaceType.Otp)]),
            new ThrowingFactory());

        var result = await find.FindAllAsync(ConnectionType.All, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, d => d is CompositeYubiKey);
    }

    private sealed class FakeFindPcscDevices(IReadOnlyList<IPcscDevice> devices) : IFindPcscDevices
    {
        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(devices);
    }

    private sealed class FakeFindHidDevices(IReadOnlyList<IHidDevice> devices) : IFindHidDevices
    {
        public Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(devices);
    }

    private sealed class FakePcscDevice(string readerName, PscsConnectionKind kind) : IPcscDevice
    {
        public string ReaderName { get; } = readerName;
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind { get; } = kind;
    }

    private sealed class FakeHidDevice(short productId, HidInterfaceType interfaceType) : IHidDevice
    {
        public string ReaderName { get; } = $"hid-{productId:X4}-{interfaceType}";
        public HidDescriptorInfo DescriptorInfo { get; } = new() { VendorId = 0x1050, ProductId = productId };
        public HidInterfaceType InterfaceType { get; } = interfaceType;
        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();
        public IHidConnection ConnectToIOReports() => throw new NotSupportedException();
    }

    private sealed class ThrowingFactory : IYubiKeyFactory
    {
        public IYubiKey Create(IDevice device) => device switch
        {
            IPcscDevice p => new ThrowingYubiKey($"pcsc:{p.ReaderName}", ConnectionType.SmartCard),
            IHidDevice h => new ThrowingYubiKey(
                $"hid:{h.ReaderName}", ConnectionTypeMapper.ToConnectionType(h.InterfaceType)),
            _ => throw new NotSupportedException()
        };
    }

    private sealed class BlockingIdentityFactory : IYubiKeyFactory
    {
        private readonly List<BlockingIdentityYubiKey> _devices = [];

        public int TotalConnectCalls => _devices.Sum(device => device.ConnectCalls);

        public IYubiKey Create(IDevice device)
        {
            var pcscDevice = Assert.IsAssignableFrom<IPcscDevice>(device);
            var yubiKey = new BlockingIdentityYubiKey($"pcsc:{pcscDevice.ReaderName}");
            _devices.Add(yubiKey);
            return yubiKey;
        }

        public void FailAllReads()
        {
            foreach (var device in _devices)
                device.FailRead();
        }
    }

    private sealed class BlockingIdentityYubiKey(string deviceId) : IYubiKey, IDiscoveryConnectionProvider
    {
        private readonly TaskCompletionSource<IConnection> _connection =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _connectCalls;

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public string DeviceId { get; } = deviceId;

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromException<TConnection>(new InvalidOperationException("Public connect must not be used by discovery."));

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken)
        {
            _ = Interlocked.Increment(ref _connectCalls);
            return _connection.Task;
        }

        public void FailRead() =>
            _connection.TrySetException(new InvalidOperationException("Expected identity-read cleanup failure."));
    }

    private sealed class ThrowingYubiKey(string deviceId, ConnectionType connectionType) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType AvailableConnections { get; } = connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new InvalidOperationException("connection unavailable (simulated exclusive holder).");
    }
}
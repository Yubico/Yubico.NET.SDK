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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.UnitTests.Devices;
using Yubico.YubiKit.Core.UnitTests.Infrastructure;

namespace Yubico.YubiKit.Core.UnitTests.Transports.SmartCard;

[Collection(DiscoveryWorkerAdmissionCollection.Name)]
public class FindPcscDevicesTests
{
    // Reference the product constant rather than copying the string: a copy stops matching silently if the
    // wording changes, which would leave this test asserting nothing useful.
    private const string SaturatedMessage = FindPcscDevices.WorkerSaturationMessage;

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task FindAllAsync_SaturatedWorkerAdmission_ThrowsTransientFailureWithoutStartingNativeScan()
    {
        var api = new StatusBlockingSCardApi("Yubico YubiKey OTP+FIDO+CCID");
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api);
        using var saturation = await DiscoveryWorkerAdmissionCollection.SaturateAsync(
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finder.FindAllAsync(TestContext.Current.CancellationToken));

        Assert.Equal(SaturatedMessage, exception.Message);
        Assert.Equal(0, api.EstablishContextCalls);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task ListenerScan_SaturatedPcscAdmission_PreservesSnapshotWithoutRemovalEvent()
    {
        var repository = new YubiKeyDeviceRepository();
        var rig = new FindYubiKeysScopeTests.ScopeRig();
        var api = new StatusBlockingSCardApi("Yubico YubiKey OTP+FIDO+CCID");
        var finder = new FindYubiKeys(
            new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api),
            new EmptyHidFinder(),
            device => new MetadataSlot(rig, device.ReaderName), topologyResolver: rig);
        var hid = new OwnedMonitorFixture.FakeHidListener();
        var smartCard = new OwnedMonitorFixture.FakeSmartCardListener();
        var worker = new YubiKeyMonitorWorker(repository, finder, () => hid, () => smartCard);
        var completedScans = 0;
        worker.MonitoringWaitReachedForTest = () => Interlocked.Increment(ref completedScans);
        await using var monitor = await YubiKeyMonitor.CreateAsync(repository, worker,
            TestContext.Current.CancellationToken, TimeSpan.FromHours(1));
        var existingDevice = Assert.Single(monitor.CurrentDevices);
        Assert.Equal(111, existingDevice.SerialNumber);
        await using var watcher = await DeviceEventWatcher.StartAsync(repository, TestContext.Current.CancellationToken);
        var nativeCalls = api.EstablishContextCalls;
        Assert.True(nativeCalls > 0);

        using (await DiscoveryWorkerAdmissionCollection.SaturateAsync(TestContext.Current.CancellationToken))
        {
            smartCard.Raise();
            await AsyncWait.WaitUntilAsync(() => Volatile.Read(ref completedScans) >= 2,
                "The actual loop did not finish its saturated PC/SC scan", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Same(existingDevice, Assert.Single(monitor.CurrentDevices));
            Assert.Equal(nativeCalls, api.EstablishContextCalls);
        }

        // The next real hint retries successfully once capacity returns, retaining the published object.
        smartCard.Raise();
        await AsyncWait.WaitUntilAsync(() => Volatile.Read(ref completedScans) >= 3,
            "PC/SC monitoring did not recover after saturation", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(api.EstablishContextCalls > nativeCalls);
        Assert.Same(existingDevice, Assert.Single(monitor.CurrentDevices));
        await monitor.DisposeAsync();
        await watcher.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(watcher.EndedNormally);
        Assert.Empty(watcher.Events);
    }

    [Fact]
    [Trait("Category", "RuntimeResilience")]
    public async Task FindAllAsync_RecognizedUsbYubiKeyReaders_DoNotQueryCardStatus()
    {
        var api = new StatusBlockingSCardApi(
            "Yubico YubiKey OTP+FIDO+CCID",
            "Yubico YubiKey OTP+FIDO+CCID 01");
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api);

        var devices = await finder.FindAllAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, devices.Count);
        Assert.Equal(0, api.StatusChangeCalls);
        Assert.All(devices, device =>
        {
            Assert.Null(device.Atr);
            Assert.Equal(PscsConnectionKind.Usb, device.Kind);
        });
    }

    [Fact]
    public async Task FindAllAsync_UnrecognizedReader_StillQueriesCardStatus()
    {
        var api = new StatusBlockingSCardApi("Generic NFC Reader");
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finder.FindAllAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, api.StatusChangeCalls);
    }

    [Fact]
    public async Task FindAllAsync_OtpOnlyYubiKeyReader_StillQueriesCardStatus()
    {
        var api = new StatusBlockingSCardApi("Yubico YubiKey OTP");
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finder.FindAllAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, api.StatusChangeCalls);
        Assert.Equal(["Yubico YubiKey OTP"], api.StatusReaderNames);
    }

    [Fact]
    public async Task FindAllAsync_MixedReaders_BypassesOnlySmartCardCapableIntegratedUsbReader()
    {
        var api = new StatusBlockingSCardApi(
            "Yubico YubiKey OTP+FIDO+CCID",
            "Yubico YubiKey OTP",
            "Generic NFC Reader");
        var finder = new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            finder.FindAllAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, api.StatusChangeCalls);
        Assert.Equal(["Yubico YubiKey OTP", "Generic NFC Reader"], api.StatusReaderNames);
    }

    private sealed class StatusBlockingSCardApi(params string[] readerNames) : ISCardApi
    {
        public int EstablishContextCalls { get; private set; }

        public int StatusChangeCalls { get; private set; }

        public IReadOnlyList<string> StatusReaderNames { get; private set; } = [];

        public uint SCardEstablishContext(SCARD_SCOPE scope, out SCardContext context)
        {
            EstablishContextCalls++;
            context = new SCardContext();
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public uint SCardListReaders(SCardContext context, string[]? groups, out string[] names)
        {
            names = readerNames;
            return ErrorCode.SCARD_S_SUCCESS;
        }

        public uint SCardGetStatusChange(
            SCardContext context,
            int timeout,
            SCARD_READER_STATE[] readerStates,
            int readerStatesCount)
        {
            StatusChangeCalls++;
            StatusReaderNames = readerStates.Select(state => state.GetReaderName()).ToList();
            throw new InvalidOperationException("A recognized USB YubiKey reader must not query card status.");
        }

        public uint SCardCancel(SCardContext context) => ErrorCode.SCARD_S_SUCCESS;
    }

    private sealed class EmptyHidFinder : IFindHidInterfaces
    {
        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IHidInterface>>([]);
    }

    private sealed class MetadataSlot(FindYubiKeysScopeTests.ScopeRig rig, string name)
        : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
    {
        public string InterfaceId => rig.InterfaceId(name);
        public ConnectionType ConnectionType => ConnectionType.SmartCard;
        public Task<IConnection> OpenRawConnectionAsync(ConnectionType connectionType, CancellationToken cancellationToken) =>
            Task.FromResult<IConnection>(rig.CreateIdentityConnection(111));
        public Task<IConnection> ConnectForDiscoveryAsync(ConnectionType connectionType, CancellationToken cancellationToken) =>
            OpenRawConnectionAsync(connectionType, cancellationToken);
    }
}
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
    public async Task RescanAsync_SaturatedPcscAdmission_PreservesSnapshotWithoutRemovalEvent()
    {
        using var repository = new YubiKeyDeviceRepository();
        var existingDevice = new FakeYubiKey("pcsc:existing", ConnectionType.SmartCard);
        repository.UpdateCache([existingDevice]);

        var events = new RecordingObserver<DeviceEvent>();
        using var subscription = repository.DeviceChanges.Subscribe(events);
        var api = new StatusBlockingSCardApi("Yubico YubiKey OTP+FIDO+CCID");
        var finder = new FindYubiKeys(
            new FindPcscDevices(NullLogger<FindPcscDevices>.Instance, api),
            new EmptyHidFinder(),
            new UnexpectedYubiKeyFactory());
        await using var monitor = new YubiKeyDeviceMonitorService(repository, finder);
        using var saturation = await DiscoveryWorkerAdmissionCollection.SaturateAsync(
            TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            monitor.RescanAsync(TestContext.Current.CancellationToken));

        Assert.Equal(SaturatedMessage, exception.Message);
        Assert.Same(existingDevice, Assert.Single(repository.GetAll()));
        Assert.Empty(events);
        Assert.Equal(0, api.EstablishContextCalls);
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

    private sealed class EmptyHidFinder : IFindHidDevices
    {
        public Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IHidDevice>>([]);
    }

    private sealed class UnexpectedYubiKeyFactory : IYubiKeyFactory
    {
        public IYubiKey Create(IDevice device) =>
            throw new InvalidOperationException("No device should be created when PC/SC admission is saturated.");
    }
}
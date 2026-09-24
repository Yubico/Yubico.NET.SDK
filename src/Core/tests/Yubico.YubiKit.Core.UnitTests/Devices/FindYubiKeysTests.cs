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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class FindYubiKeysTests
{
    [Fact]
    public async Task FindAllAsync_WithHidFido_ReturnsOnlyFidoHidDevices()
    {
        // Arrange
        var findHid = new FakeFindHidInterfaces([
            new FakeHidInterface("fido", HidInterfaceType.Fido),
            new FakeHidInterface("generic-hid", HidInterfaceType.Unknown),
            new FakeHidInterface("otp", HidInterfaceType.Otp)
        ]);
        var findYubiKeys = new FindYubiKeys(new FakeFindPcscDevices([]), findHid, CreateFakeSlot);

        // Act
        var result = await findYubiKeys.FindAllAsync(ConnectionType.HidFido, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(ConnectionType.HidFido, result[0].AvailableConnections);
        Assert.Equal(1, findHid.ScanCount);
    }

    [Fact]
    public async Task FindAllAsync_WithHid_ReturnsFidoAndOtpHidDevices()
    {
        // Arrange
        var findHid = new FakeFindHidInterfaces([
            new FakeHidInterface("fido", HidInterfaceType.Fido),
            new FakeHidInterface("otp", HidInterfaceType.Otp)
        ]);
        var findYubiKeys = new FindYubiKeys(new FakeFindPcscDevices([]), findHid, CreateFakeSlot);

        // Act
        var result = await findYubiKeys.FindAllAsync(ConnectionType.Hid, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, yubiKey => yubiKey.AvailableConnections == ConnectionType.HidFido);
        Assert.Contains(result, yubiKey => yubiKey.AvailableConnections == ConnectionType.HidOtp);
    }

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

    [Fact]
    public async Task FindAllAsync_WithUnknown_DoesNotScanEitherTransport()
    {
        // Arrange
        var findPcsc = new FakeFindPcscDevices([new FakePcscDevice("smartcard")]);
        var findHid = new FakeFindHidInterfaces([new FakeHidInterface("fido", HidInterfaceType.Fido)]);
        var findYubiKeys = new FindYubiKeys(findPcsc, findHid, CreateFakeSlot);

        // Act
        var result = await findYubiKeys.FindAllAsync(ConnectionType.Unknown, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
        Assert.Equal(0, findPcsc.ScanCount);
        Assert.Equal(0, findHid.ScanCount);
    }

    private sealed class FakeFindPcscDevices(IReadOnlyList<IPcscDevice> devices) : IFindPcscDevices
    {
        public int ScanCount { get; private set; }

        public Task<IReadOnlyList<IPcscDevice>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            ScanCount++;
            return Task.FromResult(devices);
        }
    }

    private sealed class FakeFindHidInterfaces(IReadOnlyList<IHidInterface> devices) : IFindHidInterfaces
    {
        public int ScanCount { get; private set; }

        public Task<IReadOnlyList<IHidInterface>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            ScanCount++;
            return Task.FromResult(devices);
        }
    }

    private static IYubiKeyConnectionSlot CreateFakeSlot(IDevice device) => device switch
    {
        IPcscDevice pcscDevice => new FakeSlot(pcscDevice.ReaderName, ConnectionType.SmartCard),
        IHidInterface hidInterface => new FakeSlot(hidInterface.ReaderName, ConnectionTypeMapper.ToConnectionType(hidInterface.InterfaceType)),
        _ => throw new NotSupportedException()
    };

    private sealed class RejectUnsupportedHidFactory
    {
        public int CreateCalls { get; private set; }

        public IYubiKeyConnectionSlot Create(IDevice device)
        {
            var hid = Assert.IsAssignableFrom<IHidInterface>(device);
            Assert.NotEqual(HidInterfaceType.Unknown, hid.InterfaceType);
            CreateCalls++;
            return new FakeSlot(hid.ReaderName, ConnectionTypeMapper.ToConnectionType(hid.InterfaceType));
        }
    }

    private sealed class FakePcscDevice(string readerName) : IPcscDevice
    {
        public string ReaderName { get; } = readerName;
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind => PscsConnectionKind.Usb;
    }

    private sealed class FakeHidInterface(string readerName, HidInterfaceType interfaceType) : IHidInterface
    {
        public string ReaderName { get; } = readerName;
        public HidDescriptorInfo DescriptorInfo { get; } = new() { VendorId = 0x1050 };
        public HidInterfaceType InterfaceType { get; } = interfaceType;

        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();

        public IHidConnection ConnectToIOReports() => throw new NotSupportedException();
    }
    private sealed class FakeSlot(string deviceId, ConnectionType connectionType) : IYubiKeyConnectionSlot
    {
        public string InterfaceId { get; } = deviceId;
        public ConnectionType ConnectionType { get; } = connectionType;
    }
}

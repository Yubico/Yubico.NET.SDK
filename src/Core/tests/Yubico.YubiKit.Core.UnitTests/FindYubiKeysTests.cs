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

using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests;

public class FindYubiKeysTests
{
    [Fact]
    public async Task FindAllAsync_WithHidFido_ReturnsOnlyFidoHidDevices()
    {
        // Arrange
        var findHid = new FakeFindHidDevices([
            new FakeHidDevice("fido", HidInterfaceType.Fido),
            new FakeHidDevice("generic-hid", HidInterfaceType.Unknown),
            new FakeHidDevice("otp", HidInterfaceType.Otp)
        ]);
        var findYubiKeys = new FindYubiKeys(new FakeFindPcscDevices([]), findHid, new FakeYubiKeyFactory());

        // Act
        var result = await findYubiKeys.FindAllAsync(ConnectionType.HidFido, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(ConnectionType.HidFido, result[0].ConnectionType);
        Assert.Equal(1, findHid.ScanCount);
    }

    [Fact]
    public async Task FindAllAsync_WithHid_ReturnsFidoAndOtpHidDevices()
    {
        // Arrange
        var findHid = new FakeFindHidDevices([
            new FakeHidDevice("fido", HidInterfaceType.Fido),
            new FakeHidDevice("otp", HidInterfaceType.Otp)
        ]);
        var findYubiKeys = new FindYubiKeys(new FakeFindPcscDevices([]), findHid, new FakeYubiKeyFactory());

        // Act
        var result = await findYubiKeys.FindAllAsync(ConnectionType.Hid, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, yubiKey => yubiKey.ConnectionType == ConnectionType.HidFido);
        Assert.Contains(result, yubiKey => yubiKey.ConnectionType == ConnectionType.HidOtp);
    }

    [Fact]
    public async Task FindAllAsync_WithUnknown_DoesNotScanEitherTransport()
    {
        // Arrange
        var findPcsc = new FakeFindPcscDevices([new FakePcscDevice("smartcard")]);
        var findHid = new FakeFindHidDevices([new FakeHidDevice("fido", HidInterfaceType.Fido)]);
        var findYubiKeys = new FindYubiKeys(findPcsc, findHid, new FakeYubiKeyFactory());

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

    private sealed class FakeFindHidDevices(IReadOnlyList<IHidDevice> devices) : IFindHidDevices
    {
        public int ScanCount { get; private set; }

        public Task<IReadOnlyList<IHidDevice>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            ScanCount++;
            return Task.FromResult(devices);
        }
    }

    private sealed class FakeYubiKeyFactory : IYubiKeyFactory
    {
        public IYubiKey Create(IDevice device) => device switch
        {
            IPcscDevice pcscDevice => new FakeYubiKey(pcscDevice.ReaderName, ConnectionType.SmartCard),
            IHidDevice hidDevice => new FakeYubiKey(hidDevice.ReaderName, ConnectionTypeMapper.ToConnectionType(hidDevice.InterfaceType)),
            _ => throw new NotSupportedException()
        };
    }

    private sealed class FakePcscDevice(string readerName) : IPcscDevice
    {
        public string ReaderName { get; } = readerName;
        public AnswerToReset? Atr => null;
        public PscsConnectionKind Kind => PscsConnectionKind.Usb;
    }

    private sealed class FakeHidDevice(string readerName, HidInterfaceType interfaceType) : IHidDevice
    {
        public string ReaderName { get; } = readerName;
        public HidDescriptorInfo DescriptorInfo { get; } = new() { VendorId = 0x1050 };
        public HidInterfaceType InterfaceType { get; } = interfaceType;

        public IHidConnection ConnectToFeatureReports() => throw new NotSupportedException();

        public IHidConnection ConnectToIOReports() => throw new NotSupportedException();
    }

    private sealed class FakeYubiKey(string deviceId, ConnectionType connectionType) : IYubiKey
    {
        public string DeviceId { get; } = deviceId;
        public ConnectionType ConnectionType { get; } = connectionType;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException("FakeYubiKey does not support connections.");
    }
}
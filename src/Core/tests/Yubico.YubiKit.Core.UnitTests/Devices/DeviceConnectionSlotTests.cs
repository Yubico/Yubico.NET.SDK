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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class DeviceConnectionSlotTests
{
    [Fact]
    public async Task PcscSlot_UsesExactInterfaceId_AndOpensSmartCardConnection()
    {
        var device = new PcscDevice { ReaderName = "Yubico YubiKey OTP+FIDO+CCID 01 00", Atr = null };
        var expected = new FakeSmartCardConnection();
        var factory = new RecordingSmartCardConnectionFactory(expected);
        var slot = new DeviceConnectionSlot(device, factory);

        var connection = await slot.OpenRawConnectionAsync(
            ConnectionType.SmartCard,
            TestContext.Current.CancellationToken);

        Assert.Equal("pcsc:Yubico YubiKey OTP+FIDO+CCID 01 00", slot.InterfaceId);
        Assert.Equal(ConnectionType.SmartCard, slot.ConnectionType);
        Assert.Same(expected, connection);
        Assert.Equal(device, factory.LastDevice);
    }

    [Theory]
    [InlineData(HidInterfaceType.Fido, 0x0001, "hid:test-hid:0001", typeof(IFidoHidConnection))]
    [InlineData(HidInterfaceType.Otp, 0x00AF, "hid:test-hid:00AF", typeof(IOtpHidConnection))]
    public async Task HidSlot_UsesExactUppercasePaddedInterfaceId_AndOpensTypedConnection(
        HidInterfaceType interfaceType,
        ushort usage,
        string expectedInterfaceId,
        Type expectedConnectionType)
    {
        var device = new FakeHidDevice("test-hid", interfaceType, usage);
        var slot = new DeviceConnectionSlot(device);
        var connectionType = ConnectionTypeMapper.ToConnectionType(interfaceType);

        await using var connection = await slot.OpenRawConnectionAsync(
            connectionType,
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedInterfaceId, slot.InterfaceId);
        Assert.Equal(connectionType, slot.ConnectionType);
        Assert.IsAssignableFrom(expectedConnectionType, connection);
    }

    private sealed class RecordingSmartCardConnectionFactory(ISmartCardConnection connection)
        : ISmartCardConnectionFactory
    {
        public IPcscDevice? LastDevice { get; private set; }

        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice,
            CancellationToken cancellationToken = default)
        {
            LastDevice = smartCardDevice;
            return Task.FromResult(connection);
        }
    }

    private sealed class FakeHidDevice(
        string readerName,
        HidInterfaceType interfaceType,
        ushort usage) : IHidDevice
    {
        public string ReaderName { get; } = readerName;

        public HidDescriptorInfo DescriptorInfo { get; } = new()
        {
            VendorId = 0x1050,
            ProductId = 0x0407,
            UsagePage = interfaceType == HidInterfaceType.Fido ? (ushort)0xF1D0 : (ushort)0x0001,
            Usage = usage
        };

        public HidInterfaceType InterfaceType { get; } = interfaceType;

        public IHidConnection ConnectToFeatureReports() => new FakeHidConnection(ConnectionType.HidOtp);

        public IHidConnection ConnectToIOReports() => new FakeHidConnection(ConnectionType.HidFido);
    }

    private sealed class FakeHidConnection(ConnectionType type) : IHidConnection
    {
        public ConnectionType Type { get; } = type;
        public int InputReportSize => 64;
        public int OutputReportSize => 64;
        public void SetReport(byte[] report) { }
        public byte[] GetReport() => new byte[64];
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeSmartCardConnection : ISmartCardConnection
    {
        public ConnectionType Type => ConnectionType.SmartCard;
        public Transport Transport => Transport.Usb;
        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ReadOnlyMemory<byte>.Empty);
        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public bool SupportsExtendedApdu() => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
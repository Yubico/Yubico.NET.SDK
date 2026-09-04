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
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class PcscConnectionSlotTests
{
    [Fact]
    public async Task PcscSlot_UsesExactInterfaceId_AndOpensSmartCardConnection()
    {
        var device = new PcscDevice { ReaderName = "Yubico YubiKey OTP+FIDO+CCID 01 00", Atr = null };
        var expected = new FakeSmartCardConnection();
        var factory = new RecordingSmartCardConnectionFactory(expected);
        var slot = new PcscConnectionSlot(device, factory);

        var connection = await slot.OpenRawConnectionAsync(
            ConnectionType.SmartCard,
            TestContext.Current.CancellationToken);

        Assert.Equal("pcsc:Yubico YubiKey OTP+FIDO+CCID 01 00", slot.InterfaceId);
        Assert.Equal(ConnectionType.SmartCard, slot.ConnectionType);
        Assert.Same(expected, connection);
        Assert.Equal(device, factory.LastDevice);
    }

    [Fact]
    public async Task PcscSlot_WrongConnectionType_ThrowsWithoutTouchingFactory()
    {
        var factory = new RecordingSmartCardConnectionFactory(new FakeSmartCardConnection());
        var slot = new PcscConnectionSlot(new PcscDevice { ReaderName = "reader", Atr = null }, factory);

        await Assert.ThrowsAsync<NotSupportedException>(() => slot.OpenRawConnectionAsync(
            ConnectionType.HidFido,
            TestContext.Current.CancellationToken));
        Assert.Null(factory.LastDevice);
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
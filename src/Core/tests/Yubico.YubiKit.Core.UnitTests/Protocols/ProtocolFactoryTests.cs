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
using Yubico.YubiKit.Core.Protocols;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.Otp.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Protocols;

public class ProtocolFactoryTests
{
    [Fact]
    public void Create_MapsSupportedConnectionTypes()
    {
        IConnection smartCardConnection = new FakeSmartCardConnection();
        IConnection fidoHidConnection = new FakeFidoHidConnection();
        IConnection otpHidConnection = new FakeOtpHidConnection();

        using var smartCard = ProtocolFactory.Create(smartCardConnection);
        using var fidoHid = ProtocolFactory.Create(fidoHidConnection);
        using var otpHid = ProtocolFactory.Create(otpHidConnection);

        Assert.IsAssignableFrom<ISmartCardProtocol>(smartCard);
        Assert.IsAssignableFrom<IFidoHidProtocol>(fidoHid);
        Assert.IsAssignableFrom<IOtpHidProtocol>(otpHid);
    }

    [Fact]
    public void Create_RejectsUnsupportedConnectionType()
    {
        var exception = Assert.Throws<NotSupportedException>(
            () => ProtocolFactory.Create(new FakeConnection()));

        Assert.Contains(nameof(FakeConnection), exception.Message, StringComparison.Ordinal);
    }

    private class FakeConnection : IConnection
    {
        public ConnectionType Type => ConnectionType.Unknown;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeSmartCardConnection : FakeConnection, ISmartCardConnection
    {
        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => false;
    }

    private sealed class FakeFidoHidConnection : FakeConnection, IFidoHidConnection
    {
        public int PacketSize => 64;

        public Task SendAsync(ReadOnlyMemory<byte> packet, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOtpHidConnection : FakeConnection, IOtpHidConnection
    {
        public int FeatureReportSize => 8;

        public Task SendAsync(ReadOnlyMemory<byte> report, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
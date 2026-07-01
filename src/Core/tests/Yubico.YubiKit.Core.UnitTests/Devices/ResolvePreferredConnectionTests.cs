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

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class ResolvePreferredConnectionTests
{
    private const ConnectionType Full =
        ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp;

    // Management preference order: SmartCard -> HidFido -> HidOtp (ISC-18/ISC-22).
    private static readonly ConnectionType[] ManagementOrder =
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

    // YubiOTP preference order: SmartCard -> HidOtp (ISC-18/ISC-22).
    private static readonly ConnectionType[] YubiOtpOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    [Theory]
    [InlineData(Full, ConnectionType.SmartCard)]
    [InlineData(ConnectionType.HidFido | ConnectionType.HidOtp, ConnectionType.HidFido)]
    [InlineData(ConnectionType.HidOtp, ConnectionType.HidOtp)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.SmartCard)]
    [InlineData(ConnectionType.Unknown, ConnectionType.Unknown)]
    public void ManagementOrder_ResolvesExpected(ConnectionType available, ConnectionType expected)
    {
        var device = new FakeYubiKey(available);
        Assert.Equal(expected, device.ResolvePreferredConnection(ManagementOrder));
    }

    [Theory]
    [InlineData(Full, ConnectionType.SmartCard)]
    [InlineData(ConnectionType.HidFido | ConnectionType.HidOtp, ConnectionType.HidOtp)]
    [InlineData(ConnectionType.HidOtp, ConnectionType.HidOtp)]
    [InlineData(ConnectionType.SmartCard, ConnectionType.SmartCard)]
    [InlineData(ConnectionType.HidFido, ConnectionType.Unknown)]
    public void YubiOtpOrder_ResolvesExpected(ConnectionType available, ConnectionType expected)
    {
        var device = new FakeYubiKey(available);
        Assert.Equal(expected, device.ResolvePreferredConnection(YubiOtpOrder));
    }

    [Fact]
    public void SingleInterfaceDevice_ResolvesToThatInterface()
    {
        Assert.Equal(ConnectionType.HidOtp,
            new FakeYubiKey(ConnectionType.HidOtp).ResolvePreferredConnection(ManagementOrder));
    }

    private sealed class FakeYubiKey(ConnectionType available) : IYubiKey
    {
        public string DeviceId => "fake";
        public ConnectionType AvailableConnections { get; } = available;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new NotSupportedException();
    }
}
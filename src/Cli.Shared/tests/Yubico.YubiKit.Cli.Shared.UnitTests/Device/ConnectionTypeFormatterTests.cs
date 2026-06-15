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

using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Cli.Shared.UnitTests.Device;

public class ConnectionTypeFormatterTests
{
    [Fact]
    public void Format_CombinedConnections_ReturnsJoinedTransportNames()
    {
        var value = ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp;

        Assert.Equal("SmartCard, FIDO HID, OTP HID", ConnectionTypeFormatter.Format(value));
    }

    [Fact]
    public void DeviceSelection_DisplayName_CombinedConnections_DoesNotSayUnknown()
    {
        var selection = new DeviceSelection(
            new FakeYubiKey(),
            123456,
            FormFactor.UsbAKeychain,
            "5.7.2",
            ConnectionType.SmartCard | ConnectionType.HidFido);

        Assert.Contains("SmartCard", selection.DisplayName);
        Assert.Contains("FIDO HID", selection.DisplayName);
        Assert.DoesNotContain("Unknown", selection.DisplayName);
    }

    private sealed class FakeYubiKey : IYubiKey
    {
        public string DeviceId => "fake";
        public ConnectionType AvailableConnections => ConnectionType.SmartCard | ConnectionType.HidFido;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            throw new NotSupportedException();
    }
}

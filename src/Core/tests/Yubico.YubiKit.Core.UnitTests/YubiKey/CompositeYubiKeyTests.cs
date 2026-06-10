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

using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Core.UnitTests.CoreYubiKey;

public class CompositeYubiKeyTests
{
    private static CompositeYubiKey FullKey() => new(
        "ykphysical:103",
        [
            new FakeMember(ConnectionType.SmartCard),
            new FakeMember(ConnectionType.HidFido),
            new FakeMember(ConnectionType.HidOtp)
        ],
        deviceInfo: null);

    [Fact]
    public void AvailableConnections_IsUnionOfMembers()
    {
        Assert.Equal(
            ConnectionType.SmartCard | ConnectionType.HidFido | ConnectionType.HidOtp,
            FullKey().AvailableConnections);
    }

    [Fact]
    public async Task ConnectAsync_SmartCard_RoutesToSmartCardMember()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => FullKey().ConnectAsync<ISmartCardConnection>(TestContext.Current.CancellationToken));
        Assert.Contains("routed-to:SmartCard", ex.Message);
    }

    [Fact]
    public async Task ConnectAsync_FidoHid_RoutesToFidoMember()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => FullKey().ConnectAsync<IFidoHidConnection>(TestContext.Current.CancellationToken));
        Assert.Contains("routed-to:HidFido", ex.Message);
    }

    [Fact]
    public async Task ConnectAsync_OtpHid_RoutesToOtpMember()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => FullKey().ConnectAsync<IOtpHidConnection>(TestContext.Current.CancellationToken));
        Assert.Contains("routed-to:HidOtp", ex.Message);
    }

    [Fact]
    public async Task ConnectAsync_UnsupportedConnectionInterface_Throws()
    {
        var composite = FullKey();
        await Assert.ThrowsAsync<NotSupportedException>(() => composite.ConnectAsync<IConnection>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConnectAsync_RequestedTypeNotAvailableOnDevice_ThrowsNotSupported()
    {
        var composite = new CompositeYubiKey(
            "ykphysical:1",
            [new FakeMember(ConnectionType.SmartCard), new FakeMember(ConnectionType.HidFido)],
            deviceInfo: null);

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => composite.ConnectAsync<IOtpHidConnection>(TestContext.Current.CancellationToken));
        Assert.Contains("HidOtp", ex.Message);
    }

    [Fact]
    public async Task DefaultConnectAsync_OnMultiConnectionDevice_ThrowsAmbiguous()
    {
        IYubiKey composite = FullKey();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => composite.ConnectAsync(TestContext.Current.CancellationToken));
        Assert.Contains("ambiguous", ex.Message);
    }

    [Fact]
    public void Composite_OwnsNoDisposableState()
    {
        // ISC-6: IYubiKey is not IDisposable; the composite holds only references to member interfaces.
        Assert.False(typeof(CompositeYubiKey).IsAssignableTo(typeof(IDisposable)));
        Assert.False(typeof(CompositeYubiKey).IsAssignableTo(typeof(IAsyncDisposable)));
    }

    [Fact]
    public void Constructor_RejectsFewerThanTwoMembers()
    {
        Assert.Throws<ArgumentException>(() =>
            new CompositeYubiKey("ykphysical:1", [new FakeMember(ConnectionType.SmartCard)], null));
    }

    // FakeMember.ConnectAsync always throws a distinctive InvalidOperationException naming the connection it
    // backs, so a routing test can assert which member the composite dispatched to without full connection fakes.
    private sealed class FakeMember : IYubiKey
    {
        private readonly ConnectionType _connection;

        public FakeMember(ConnectionType connection) => _connection = connection;

        public string DeviceId => $"member:{_connection}";
        public ConnectionType AvailableConnections => _connection;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
            => throw new InvalidOperationException($"routed-to:{_connection}:{typeof(TConnection).Name}");
    }
}
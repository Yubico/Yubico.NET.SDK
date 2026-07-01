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

using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     Phase 38.5 ISC-9: a held <see cref="SCardException" /> from a SmartCard connect must reach the applet
///     connect site as a top-level, unwrapped <see cref="SCardException" /> with its held PC/SC HResult
///     preserved, so <c>IsHeldTransportError</c> can detect it. These pin the current connect chain
///     (<see cref="CompositeYubiKey" /> and <see cref="PcscYubiKey" />) against a future wrapping regression.
/// </summary>
public class HeldExceptionPropagationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CompositeYubiKey_MemberThrowsHeldScard_PropagatesUnwrapped()
    {
        var held = new SCardException("held", (long)ErrorCode.SCARD_E_SHARING_VIOLATION);
        var smartCardMember = new ThrowingMember(ConnectionType.SmartCard, held);
        var hidMember = new ThrowingMember(ConnectionType.HidFido, new InvalidOperationException("unused"));
        var composite = new CompositeYubiKey("composite:test", [smartCardMember, hidMember], deviceInfo: null);

        var ex = await Assert.ThrowsAsync<SCardException>(
            () => composite.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_SHARING_VIOLATION), ex.HResult);
    }

    [Fact]
    public async Task PcscYubiKey_FactoryThrowsHeldScard_PropagatesUnwrapped()
    {
        var held = new SCardException("held", (long)ErrorCode.SCARD_E_SERVER_TOO_BUSY);
        var device = new PcscDevice { ReaderName = "fake-reader", Atr = null };
        var yubiKey = new PcscYubiKey(device, new ThrowingFactory(held), NullLogger<PcscYubiKey>.Instance);

        var ex = await Assert.ThrowsAsync<SCardException>(
            () => yubiKey.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_SERVER_TOO_BUSY), ex.HResult);
    }

    private sealed class ThrowingMember(ConnectionType available, Exception exception) : IYubiKey
    {
        public string DeviceId => $"member:{available}";
        public ConnectionType AvailableConnections => available;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection =>
            Task.FromException<TConnection>(exception);
    }

    private sealed class ThrowingFactory(Exception exception) : ISmartCardConnectionFactory
    {
        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice, CancellationToken cancellationToken = default) =>
            Task.FromException<ISmartCardConnection>(exception);
    }
}
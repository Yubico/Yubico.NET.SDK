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
using Yubico.YubiKit.Core.Native.Desktop.SCard;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

/// <summary>
///     A held <see cref="SCardException" /> from a SmartCard connect must propagate unchanged, without
///     cross-transport fallback. These tests pin the published-device and concrete PC/SC slot connect chains
///     against a future wrapping regression.
/// </summary>
public class HeldExceptionPropagationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task YubiKeyDevice_SmartCardSlotThrowsHeldScard_PropagatesUnwrapped()
    {
        var held = new SCardException("held", (long)ErrorCode.SCARD_E_SHARING_VIOLATION);
        var smartCardMember = new ThrowingMember(ConnectionType.SmartCard, held);
        var hidMember = new ThrowingMember(ConnectionType.HidFido, new InvalidOperationException("unused"));
        var device = new YubiKeyDevice(
            "composite:test",
            smartCardMember,
            hidMember,
            hidOtp: null,
            deviceInfo: null);

        var ex = await Assert.ThrowsAsync<SCardException>(
            () => device.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_SHARING_VIOLATION), ex.HResult);
    }
    [Fact]
    public async Task YubiKeyDevice_ConcretePcscSlotThrowsHeldScard_PropagatesUnwrapped()
    {
        var held = new SCardException("held", (long)ErrorCode.SCARD_E_SERVER_TOO_BUSY);
        var device = new PcscDevice { ReaderName = "fake-reader", Atr = null };
        var slot = new DeviceConnectionSlot(
            device,
            new ThrowingFactory(held));
        var yubiKey = new YubiKeyDevice(
            slot.InterfaceId,
            slot,
            hidFido: null,
            hidOtp: null,
            deviceInfo: null);

        var ex = await Assert.ThrowsAsync<SCardException>(
            () => yubiKey.ConnectAsync<ISmartCardConnection>(Ct));

        Assert.Equal(unchecked((int)ErrorCode.SCARD_E_SERVER_TOO_BUSY), ex.HResult);
    }
    private sealed class ThrowingMember(ConnectionType available, Exception exception)
        : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
    {
        private readonly string _deviceId = $"member:{available}:{Guid.NewGuid():N}";

        public string InterfaceId => _deviceId;
        public ConnectionType ConnectionType => available;

        public Task<IConnection> OpenRawConnectionAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) => Task.FromException<IConnection>(exception);

        Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
            ConnectionType connection,
            CancellationToken cancellationToken) =>
            OpenRawConnectionAsync(connection, cancellationToken);
    }

    private sealed class ThrowingFactory(Exception exception) : ISmartCardConnectionFactory
    {
        public Task<ISmartCardConnection> CreateAsync(
            IPcscDevice smartCardDevice, CancellationToken cancellationToken = default) =>
            Task.FromException<ISmartCardConnection>(exception);
    }
}
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

using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Management.Backend;

namespace Yubico.YubiKit.Management.UnitTests;

/// <summary>
///     Pins the ownership contract: Management backends borrow the protocol handed to them and own no
///     disposable state, so neither the abstraction nor its implementations advertise <see cref="IDisposable" />.
///     Disposal belongs to the session (protocol) and to whoever created the connection.
/// </summary>
public class ManagementBackendLifecycleTests
{
    [Fact]
    public async Task FidoHidBackend_WriteConfigAsync_UsesPreparedChannelWithoutInitializingIt()
    {
        using var protocol = new PreparedFidoProtocol();
        var backend = new FidoHidBackend(protocol);

        await backend.WriteConfigAsync(ReadOnlyMemory<byte>.Empty, TestContext.Current.CancellationToken);

        Assert.Equal((byte)0xC3, protocol.LastCommand);
    }

    [Fact]
    public void IManagementBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(IManagementBackend)));
    }

    [Fact]
    public void SmartCardBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(SmartCardBackend)));
    }

    [Fact]
    public void FidoHidBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(FidoHidBackend)));
    }

    [Fact]
    public void OtpBackend_DoesNotAdvertiseResourceOwnership()
    {
        Assert.False(typeof(IDisposable).IsAssignableFrom(typeof(OtpBackend)));
    }

    private sealed class PreparedFidoProtocol : IFidoHidProtocol
    {
        public byte? LastCommand { get; private set; }
        public bool IsChannelInitialized => true;
        public FirmwareVersion? FirmwareVersion => new FirmwareVersion(5, 7, 2);

        public Task InitializeAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The session already initialized the channel.");

        public Task<ReadOnlyMemory<byte>> SendCommandAsync(
            byte command,
            ReadOnlyMemory<byte> data,
            UserPresenceNotification userPresenceNotification,
            CancellationToken cancellationToken = default)
        {
            LastCommand = command;
            return Task.FromResult(ReadOnlyMemory<byte>.Empty);
        }

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null) { }
        public void Dispose() { }
    }
}
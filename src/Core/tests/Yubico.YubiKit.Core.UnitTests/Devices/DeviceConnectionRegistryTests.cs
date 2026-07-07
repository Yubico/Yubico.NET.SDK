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
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class DeviceConnectionRegistryTests
{
    private static string NewId() => $"test:{Guid.NewGuid():N}";

    [Fact]
    public void Register_RefCountsPerDeviceId_AndDisposeIsIdempotent()
    {
        var id = NewId();
        Assert.False(DeviceConnectionRegistry.IsInUse(id));

        var first = DeviceConnectionRegistry.Register(id);
        var second = DeviceConnectionRegistry.Register(id);
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        first.Dispose();
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        first.Dispose(); // double-dispose must not steal the remaining count
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        second.Dispose();
        Assert.False(DeviceConnectionRegistry.IsInUse(id));
    }

    /// <summary>
    ///     An identity read against an interface this process holds a live connection to must be skipped
    ///     entirely (no connection opened — a discovery SELECT would clobber the session's applet state).
    /// </summary>
    [Fact]
    public async Task IdentityRead_DeviceInUse_SkipsWithoutConnecting()
    {
        var device = new RecordingYubiKey(NewId(), ConnectionType.SmartCard);
        using var registration = DeviceConnectionRegistry.Register(device.DeviceId);

        var info = await DiscoveryIdentityReader.TryReadAsync(
            device, ConnectionType.SmartCard, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Null(info);
        Assert.Equal(0, device.ConnectCalls);
    }

    /// <summary>
    ///     The metadata read must skip only the in-use member interface of a composite and still attempt
    ///     the remaining free transports (independent USB interfaces are safe to read).
    /// </summary>
    [Fact]
    public async Task MetadataRead_CompositeWithInUseSmartCardMember_SkipsItButTriesOtpTransport()
    {
        var smartCardMember = new RecordingYubiKey(NewId(), ConnectionType.SmartCard);
        var otpMember = new RecordingYubiKey(NewId(), ConnectionType.HidOtp);
        var composite = new CompositeYubiKey(NewId(), [smartCardMember, otpMember], deviceInfo: null);
        using var registration = DeviceConnectionRegistry.Register(smartCardMember.DeviceId);

        var info = await CompositeMetadataReader.TryReadAsync(
            composite, TimeSpan.FromSeconds(5), NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Null(info); // OTP member's connect fails by design; result degrades to null
        Assert.Equal(0, smartCardMember.ConnectCalls);
        Assert.Equal(1, otpMember.ConnectCalls);
    }

    /// <summary>
    ///     TOCTOU guard: if a session opens the interface between the pre-connect skip check and the first
    ///     APDU, the read must abort after connect without transmitting anything (its SELECT would deselect
    ///     the session's applet), release its own registration, and degrade to unknown identity without
    ///     retrying (an owned interface is not a transient failure).
    /// </summary>
    [Fact]
    public async Task IdentityRead_SessionOpensInterfaceDuringConnect_AbortsBeforeTransmitting()
    {
        var device = new SessionStealsInterfaceYubiKey(NewId());

        var info = await DiscoveryIdentityReader.TryReadAsync(
            device, ConnectionType.SmartCard, NullLogger.Instance, TestContext.Current.CancellationToken);

        Assert.Null(info);
        Assert.Equal(1, device.ConnectCalls);
        Assert.Equal(0, device.LastConnection!.TransmitCalls);
        Assert.True(device.LastConnection.Disposed);

        // Discovery's own registration was released by the abort; only the session's remains.
        Assert.True(DeviceConnectionRegistry.IsInUse(device.DeviceId));
        device.ConcurrentSessionRegistration!.Dispose();
        Assert.False(DeviceConnectionRegistry.IsInUse(device.DeviceId));
    }

    [Fact]
    public async Task RegisteredSmartCardConnection_Dispose_ReleasesRegistration_EvenWhenInnerThrows()
    {
        var id = NewId();
        var throwingInner = new FakeSmartCardConnection { ThrowOnDispose = true };
        var wrapped = new RegisteredSmartCardConnection(throwingInner, DeviceConnectionRegistry.Register(id));
        Assert.True(DeviceConnectionRegistry.IsInUse(id));

        Assert.Throws<InvalidOperationException>(wrapped.Dispose);
        Assert.False(DeviceConnectionRegistry.IsInUse(id));

        var asyncId = NewId();
        var inner = new FakeSmartCardConnection();
        var asyncWrapped = new RegisteredSmartCardConnection(inner, DeviceConnectionRegistry.Register(asyncId));
        Assert.True(DeviceConnectionRegistry.IsInUse(asyncId));

        await asyncWrapped.DisposeAsync();
        Assert.False(DeviceConnectionRegistry.IsInUse(asyncId));
        Assert.True(inner.Disposed);
    }

    private sealed class RecordingYubiKey(string deviceId, ConnectionType available) : IYubiKey
    {
        public int ConnectCalls { get; private set; }

        public string DeviceId => deviceId;

        public ConnectionType AvailableConnections => available;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCalls++;
            throw new InvalidOperationException("Test connect refused by design.");
        }
    }

    /// <summary>
    ///     Mirrors <c>PcscYubiKey.ConnectAsync</c> (registers its own connection, returns it wrapped) and
    ///     simulates a session registering the same interface concurrently — the TOCTOU window between
    ///     discovery's pre-connect check and its first APDU.
    /// </summary>
    private sealed class SessionStealsInterfaceYubiKey(string deviceId) : IYubiKey
    {
        public int ConnectCalls { get; private set; }

        public FakeSmartCardConnection? LastConnection { get; private set; }

        public IDisposable? ConcurrentSessionRegistration { get; private set; }

        public string DeviceId => deviceId;

        public ConnectionType AvailableConnections => ConnectionType.SmartCard;

        public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
            where TConnection : class, IConnection
        {
            ConnectCalls++;
            LastConnection = new FakeSmartCardConnection();
            var wrapped = new RegisteredSmartCardConnection(
                LastConnection, DeviceConnectionRegistry.Register(deviceId));
            ConcurrentSessionRegistration = DeviceConnectionRegistry.Register(deviceId);
            return Task.FromResult((TConnection)(object)wrapped);
        }
    }

    private sealed class FakeSmartCardConnection : ISmartCardConnection
    {
        public bool Disposed { get; private set; }

        public bool ThrowOnDispose { get; init; }

        public int TransmitCalls { get; private set; }

        public ConnectionType Type => ConnectionType.SmartCard;

        public Transport Transport => Transport.Usb;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command, CancellationToken cancellationToken = default)
        {
            TransmitCalls++;
            return Task.FromResult(ReadOnlyMemory<byte>.Empty);
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool SupportsExtendedApdu() => true;

        public void Dispose()
        {
            Disposed = true;
            if (ThrowOnDispose)
                throw new InvalidOperationException("Inner dispose failure.");
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
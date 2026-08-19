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

namespace Yubico.YubiKit.Management.UnitTests;

using System.Reflection;
using System.Runtime.CompilerServices;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management.Backend;

public class ManagementSessionTests
{
    [Fact]
    public async Task CreateAsync_UnsupportedConnection_DoesNotLeaveSessionAttached()
    {
        var connection = new UnsupportedConnection();

        _ = await Assert.ThrowsAsync<NotSupportedException>(
            () => ManagementSession.CreateAsync(
                connection,
                cancellationToken: TestContext.Current.CancellationToken));

        await using var probe = new ProbeSession(connection);
    }

    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new FailingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        // Borrowed: a failed CreateAsync must not dispose a connection it did not create.
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_CancellationDuringInitialization_DoesNotDisposeTheBorrowedConnection()
    {
        var connection = new FailingSmartCardConnection();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: cancellationSource.Token));

        // Borrowed: a failed CreateAsync must not dispose a connection it did not create.
        Assert.Equal(0, connection.DisposeCount);
    }

    [Fact]
    public void IManagementSession_InheritsIAsyncDisposable()
    {
        // Verify that IManagementSession inherits from IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(IManagementSession)));
    }

    [Fact]
    public void ManagementSession_ImplementsIAsyncDisposable()
    {
        // Verify that ManagementSession implements IAsyncDisposable
        Assert.True(typeof(IAsyncDisposable).IsAssignableFrom(typeof(ManagementSession)));
    }

    [Fact]
    public async Task SetDeviceConfigAsync_ZeroesEncodedConfigAfterBackendWrite()
    {
        var backend = new CapturingBackend();
        var session = CreateSessionForBackend(backend);
        var lockCode = Enumerable.Repeat((byte)0xA5, 16).ToArray();
        var config = new DeviceConfig
        {
            EnabledCapabilities = new Dictionary<Transport, int>
            {
                [Transport.Usb] = 1
            }
        };

        await session.SetDeviceConfigAsync(
            config,
            reboot: false,
            currentLockCode: lockCode,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(backend.SawNonZeroConfig);
        Assert.True(backend.CapturedConfig.Span.ToArray().All(static b => b == 0));
    }

    [Fact]
    public async Task SmartCardBackend_DeviceResetAsync_SendsDeviceResetApdu()
    {
        var protocol = new RecordingSmartCardProtocol();
        var backend = new SmartCardBackend(protocol);

        await backend.DeviceResetAsync(TestContext.Current.CancellationToken);

        var command = Assert.Single(protocol.Commands);
        Assert.Equal(0x1F, command.Ins);
    }

    /// <summary>
    ///     Disposal must be observable. Of the eight application sessions, Management was the only one that
    ///     guarded its public surface with neither <c>ThrowIfDisposed</c> nor <c>EnsureInitialized</c>, so a
    ///     call after disposal ran on into a torn-down protocol and surfaced whatever incidental failure that
    ///     produced — or, worse, none at all.
    /// </summary>
    /// <remarks>
    ///     Firmware is pinned at 5.6.0 so the feature gates in <c>SetDeviceConfigAsync</c> and
    ///     <c>ResetDeviceAsync</c> are satisfied; the only thing left that can reject the call is the disposal
    ///     guard, which is what these pin.
    /// </remarks>
    [Fact]
    public async Task GetDeviceInfoAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateDisposableSession();
        await session.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.GetDeviceInfoAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetDeviceConfigAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateDisposableSession();
        await session.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.SetDeviceConfigAsync(
                new DeviceConfig
                {
                    EnabledCapabilities = new Dictionary<Transport, int> { [Transport.Usb] = 1 }
                },
                reboot: false,
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetDeviceAsync_AfterDispose_ThrowsObjectDisposed()
    {
        var session = CreateDisposableSession();
        await session.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(
            () => session.ResetDeviceAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    ///     A real, fully constructed session over an inert connection: the disposal gate, connection, and
    ///     logger are genuine, so <c>DisposeAsync</c> runs the production teardown rather than a simulation of
    ///     it. Only initialization is skipped.
    /// </summary>
    private static ManagementSession CreateDisposableSession()
    {
        var session = (ManagementSession)Activator.CreateInstance(
            typeof(ManagementSession),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [new InertSmartCardConnection(), null],
            culture: null)!;

        typeof(ApplicationSession)
            .GetProperty(nameof(ApplicationSession.FirmwareVersion))!
            .SetValue(session, new FirmwareVersion(5, 6, 0));

        return session;
    }

    private static ManagementSession CreateSessionForBackend(IManagementBackend backend)
    {
        var session = (ManagementSession)RuntimeHelpers.GetUninitializedObject(typeof(ManagementSession));

        typeof(ManagementSession)
            .GetField("_backend", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, backend);

        typeof(ApplicationSession)
            .GetProperty(nameof(ApplicationSession.FirmwareVersion))!
            .SetValue(session, new FirmwareVersion(5, 0, 0));

        return session;
    }

    private sealed class CapturingBackend : IManagementBackend
    {
        public ReadOnlyMemory<byte> CapturedConfig { get; private set; }
        public bool SawNonZeroConfig { get; private set; }

        public ValueTask<FirmwareVersion?> InitializeAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<FirmwareVersion?>(new FirmwareVersion(5, 0, 0));

        public ValueTask WriteConfigAsync(ReadOnlyMemory<byte> config, CancellationToken cancellationToken = default)
        {
            CapturedConfig = config;
            SawNonZeroConfig = config.Span.ContainsAnyExcept((byte)0);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetModeAsync(byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask DeviceResetAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnsupportedConnection : IConnection
    {
        public ConnectionType Type => ConnectionType.Unknown;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ProbeSession(IConnection connection) : ApplicationSession(connection);

    private sealed class RecordingSmartCardProtocol : ISmartCardProtocol
    {
        public List<ApduCommand> Commands { get; } = [];

        public Task<ApduResponse> TransmitAndReceiveAsync(
            ApduCommand command,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return Task.FromResult(new ApduResponse([], unchecked((short)0x9000)));
        }

        public Task<ReadOnlyMemory<byte>> SelectAsync(
            ReadOnlyMemory<byte> applicationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Configure(FirmwareVersion version, ProtocolConfiguration? configuration = null)
        {
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    ///     A structurally valid SmartCard connection that never answers. Enough for a session to be
    ///     constructed and disposed for real; any actual exchange is a test bug.
    /// </summary>
    private sealed class InertSmartCardConnection : ISmartCardConnection
    {
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("the disposal guard should have rejected this call");

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullConnectionDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => default;

        private sealed class NullConnectionDisposable : IDisposable
        {
            public static NullConnectionDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class FailingSmartCardConnection : ISmartCardConnection
    {
        public int DisposeCount { get; private set; }
        public Transport Transport => Transport.Usb;
        public ConnectionType Type => ConnectionType.SmartCard;

        public Task<ReadOnlyMemory<byte>> TransmitAndReceiveAsync(
            ReadOnlyMemory<byte> command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("session-init probe failure");
        }

        public IDisposable BeginTransaction(CancellationToken cancellationToken = default) =>
            NullDisposable.Instance;

        public bool SupportsExtendedApdu() => false;

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return default;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
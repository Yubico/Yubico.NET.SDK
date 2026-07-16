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
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Management.Backend;

public class ManagementSessionTests
{
    [Fact]
    public async Task CreateAsync_AppletProbeFailure_DisposesProtocolExactlyOnce()
    {
        var connection = new FailingSmartCardConnection();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, connection.DisposeCount);
    }

    [Fact]
    public async Task CreateAsync_CancellationDuringInitialization_DisposesProtocolExactlyOnce()
    {
        var connection = new FailingSmartCardConnection();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ManagementSession.CreateAsync(connection, cancellationToken: cancellationSource.Token));

        Assert.Equal(1, connection.DisposeCount);
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

        public void Dispose()
        {
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
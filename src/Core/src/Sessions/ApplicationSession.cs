// Copyright 2025 Yubico AB
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

using Microsoft.Extensions.Logging;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Sessions;

public abstract class ApplicationSession : IApplicationSession, IAsyncDisposable
{
    private bool _disposed;
    private int _released;
    private bool _ownsConnection;

    protected ILogger Logger { get; }
    protected IProtocol? Protocol { get; set; }

    /// <summary>
    ///     The connection this session runs over. The session is a USER of it, not its owner: disposing the
    ///     session leaves a caller-created connection open and reusable by the next session.
    /// </summary>
    protected IConnection Connection { get; }

    public FirmwareVersion FirmwareVersion { get; protected set; } = new();
    public bool IsInitialized { get; protected set; }
    public bool IsAuthenticated { get; protected set; }

    /// <summary>
    ///     Binds the session to its connection. Throws if that connection already has a live session — one
    ///     connection hosts one session at a time, checked here because this runs before any wire operation.
    /// </summary>
    /// <param name="connection">The connection the session will run over.</param>
    /// <exception cref="ConnectionInUseException">The connection already has a live session.</exception>
    protected ApplicationSession(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Logger = YubiKitLogging.CreateLogger(GetType().FullName ?? GetType().Name);
        Connection = connection;
        ConnectionSessionGuard.Attach(connection, this);
    }

    /// <summary>
    ///     Transfers ownership of <see cref="Connection" /> to this session, so disposing the session also
    ///     disposes the connection.
    /// </summary>
    /// <remarks>
    ///     Only the code that CREATED the connection may call this, and only the convenience
    ///     <c>IYubiKey.Create&lt;App&gt;SessionAsync</c> entry points do: they open a connection the caller
    ///     never sees, so the session they return is the only thing that can close it. A caller who opened the
    ///     connection keeps ownership and this is never called.
    /// </remarks>
    internal void OwnConnection() => _ownsConnection = true;

    protected async Task InitializeCoreAsync(
        IProtocol protocol,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
            return;

        ArgumentNullException.ThrowIfNull(protocol);

        protocol.Configure(firmwareVersion, configuration);

        IProtocol effectiveProtocol = protocol;
        var isAuthenticated = false;

        if (scpKeyParams is not null)
        {
            if (effectiveProtocol is not ISmartCardProtocol smartCardProtocol)
                throw new NotSupportedException("SCP is only supported on SmartCard protocols.");

            effectiveProtocol = await smartCardProtocol
                .WithScpAsync(scpKeyParams, cancellationToken)
                .ConfigureAwait(false);

            isAuthenticated = true;
        }

        // Only mutate session state on successful completion.
        Protocol = effectiveProtocol;
        FirmwareVersion = firmwareVersion;
        IsAuthenticated = isAuthenticated;
        IsInitialized = true;
    }

    public bool IsSupported(Feature feature) =>
        feature.IsSupportedByFirmware(FirmwareVersion);

    public void EnsureSupports(Feature feature)
    {
        if (!IsSupported(feature))
            throw new NotSupportedException($"{feature.Name} requires firmware {feature.Version}+");
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            Protocol?.Dispose();
            Protocol = null;
        }

        // Unconditional: DisposeAsync's Dispose(disposing: false) leg must release too, and releasing twice
        // is a no-op.
        ReleaseConnection();
        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        Protocol?.Dispose();
        Protocol = null;
        await ReleaseConnectionAsync().ConfigureAwait(false);
    }

    /// <summary>Detaches from the connection, and disposes it only if this session was given ownership.</summary>
    private void ReleaseConnection()
    {
        if (!TryClaimRelease())
            return;

        if (_ownsConnection)
            Connection.Dispose();
    }

    /// <inheritdoc cref="ReleaseConnection" />
    private async ValueTask ReleaseConnectionAsync()
    {
        if (!TryClaimRelease())
            return;

        if (_ownsConnection)
            await Connection.DisposeAsync().ConfigureAwait(false);
    }

    private bool TryClaimRelease()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return false;

        ConnectionSessionGuard.Detach(Connection, this);
        return true;
    }
}
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

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>
///     Base class for applet and raw sessions that attach to one opened YubiKey connection.
/// </summary>
/// <remarks>
///     <para>
///         One connection admits one live session. A direct session factory borrows its caller's connection;
///         only an <c>IYubiKey.Create*SessionAsync</c> convenience entry point can transfer ownership of a
///         hidden connection to the returned session.
///     </para>
///     <para>
///         Protocol construction and initialization are internal SDK seams. External derived classes cannot
///         inject or replace Core protocol implementations; compose over a public raw session for low-level work.
///     </para>
/// </remarks>
public abstract class ApplicationSession : IApplicationSession, IAsyncDisposable
{
    private readonly DisposalGate _disposalGate = new();
    // Guards reject calls that begin after disposal starts. Operations already in flight have no lifetime
    // admission lease and may complete, fail at a later guard, or race teardown.
    private int _disposalStarted;
    private bool _isInitialized;
    private bool _isAuthenticated;
    private bool _disposed;
    private int _released;
    private bool _ownsConnection;

    protected ILogger Logger { get; }
    internal IProtocol? Protocol { get; set; }
    protected bool IsDisposalStarted => Volatile.Read(ref _disposalStarted) != 0;

    /// <summary>
    ///     The connection this session runs over. The session is a USER of it, not its owner: disposing the
    ///     session leaves a caller-created connection open and reusable by the next session.
    /// </summary>
    protected IConnection Connection { get; }

    public FirmwareVersion FirmwareVersion { get; protected set; } = new();
    public bool IsInitialized
    {
        get => Volatile.Read(ref _disposalStarted) == 0 && _isInitialized;
        protected set => _isInitialized = value;
    }

    public bool IsAuthenticated
    {
        get => Volatile.Read(ref _disposalStarted) == 0 && _isAuthenticated;
        protected set => _isAuthenticated = value;
    }

    /// <summary>
    ///     Records the connection this session will run over. Does NOT bind the session to it — see
    ///     <see cref="Construct{TSession}" />, which binds once construction has actually succeeded.
    /// </summary>
    /// <param name="connection">The connection the session will run over.</param>
    protected ApplicationSession(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Logger = YubiKitLogging.CreateLogger(GetType().FullName ?? GetType().Name);
        Connection = connection;
    }

    /// <summary>
    ///     Constructs a session and binds it to its connection, enforcing one live session per connection.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Binding deliberately happens here rather than in the constructor. A constructor that binds and
    ///         then throws leaves nothing to unbind it: the object never finishes construction, so no
    ///         <c>using</c>, <c>finally</c> or factory <c>catch</c> can reach it, and the connection is
    ///         refused forever by a session that does not exist. Binding after <paramref name="create" />
    ///         returns makes that state unrepresentable.
    ///     </para>
    ///     <para>
    ///         It must NOT move later still — into initialization — because derived
    ///         <c>InitializeAsync</c> implementations issue their applet SELECT before calling
    ///         <see cref="InitializeProtocolAsync" />. Binding there would refuse the second session only after
    ///         the first session's state had already been destroyed, which is the damage this guard exists to
    ///         prevent. Construction performs no wire I/O, so here is the last safe point.
    ///     </para>
    ///     <para>
    ///         On refusal the constructed session is disposed and the exception propagates. Disposal cannot
    ///         disturb the incumbent: <see cref="ConnectionSessionGuard.Detach" /> only clears the slot when
    ///         the disposing session is the recorded holder, and this one never became it.
    ///     </para>
    /// </remarks>
    /// <exception cref="ConnectionInUseException">The connection already has a live session.</exception>
    protected static TSession Construct<TSession>(IConnection connection, Func<TSession> create)
        where TSession : ApplicationSession
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(create);

        var session = create();
        try
        {
            ConnectionSessionGuard.Attach(connection, session);
        }
        catch
        {
            session.Dispose();
            throw;
        }

        return session;
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

    internal async Task<IProtocol> InitializeProtocolAsync(
        IProtocol protocol,
        FirmwareVersion firmwareVersion,
        ProtocolConfiguration? configuration = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        // Checked before the IsInitialized early-return: a derived session that sets IsInitialized
        // itself would otherwise skip the bind check entirely and run unguarded.
        // Binding is not automatic, so a session reaching initialization without having gone through
        // Construct could run concurrently with another on the same connection and deselect its applet.
        if (!ConnectionSessionGuard.IsHolder(Connection, this))
            throw new InvalidOperationException(
                $"{GetType().Name} was not bound to its connection. Sessions must be created through their " +
                "CreateAsync factory, which routes construction through ApplicationSession.Construct so the " +
                "one-live-session-per-connection rule is enforced before any wire operation.");

        ArgumentNullException.ThrowIfNull(protocol);

        if (IsInitialized)
            return Protocol ?? protocol;

        protocol.Configure(firmwareVersion, configuration);

        IProtocol effectiveProtocol = protocol;
        var isAuthenticated = false;

        if (scpKeyParams is not null)
        {
            if (effectiveProtocol is not PcscProtocol pcscProtocol)
            {
                throw new NotSupportedException(
                    "SCP is only supported on PC/SC SmartCard protocols created by Core.");
            }

            effectiveProtocol = await pcscProtocol
                .InitializeScpAsync(scpKeyParams, cancellationToken)
                .ConfigureAwait(false);

            isAuthenticated = true;
        }

        // Only mutate session state on successful completion.
        Protocol = effectiveProtocol;
        FirmwareVersion = firmwareVersion;
        IsAuthenticated = isAuthenticated;
        IsInitialized = true;

        return effectiveProtocol;
    }

    /// <summary>
    ///     Releases resources owned by a session whose asynchronous factory failed. Disposal failures
    ///     are logged and suppressed so the initialization exception remains the primary failure.
    /// </summary>
    protected void DisposeAfterInitializationFailure()
    {
        try
        {
            Dispose();
        }
        catch (Exception ex)
        {
            try
            {
                Logger.LogWarning(ex, "Failed to dispose session resources after initialization failed");
            }
            catch
            {
                // Initialization is already failing. Logging must not replace that original exception.
            }
        }
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
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposalStarted) != 0, this);
    }

    /// <summary>Closes operation admission, drains an admitted exchange, and releases session resources.</summary>
    /// <remarks>
    ///     This synchronous path blocks while an admitted exchange finishes. Prefer <see cref="DisposeAsync" />
    ///     for asynchronous callers, and never invoke synchronous disposal from inside the operation being drained.
    /// </remarks>
    public void Dispose()
    {
        BeginDisposal();
        _disposalGate.Dispose(() => Dispose(disposing: true));
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Closes operation admission, asynchronously drains an admitted exchange, and releases session resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        BeginDisposal();
        await _disposalGate.DisposeAsync(DisposeSessionAsync).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private void BeginDisposal()
    {
        Interlocked.Exchange(ref _disposalStarted, 1);
        // Clear the backing state as a teardown backstop. The public getters also observe
        // _disposalStarted, so their result becomes false at the admission boundary.
        IsAuthenticated = false;
        IsInitialized = false;
    }

    private async ValueTask DisposeSessionAsync()
    {
        try
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }
        finally
        {
            // Managed state and sensitive buffers must be cleared even if asynchronous connection teardown fails.
            Dispose(disposing: true);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        // A disposed session has no usable protocol or authenticated channel, even when teardown fails.
        IsAuthenticated = false;
        IsInitialized = false;

        try
        {
            try
            {
                if (disposing)
                    DisposeProtocol();
            }
            finally
            {
                // Protocol failure must not retain the session guard or an owned connection.
                ReleaseConnection();
            }
        }
        finally
        {
            // A failed teardown is still terminal; DisposalGate shares that failed completion with every caller.
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        try
        {
            await DisposeProtocolAsync().ConfigureAwait(false);
        }
        finally
        {
            await ReleaseConnectionAsync().ConfigureAwait(false);
        }
    }

    private void DisposeProtocol()
    {
        IProtocol? protocol = Protocol;
        Protocol = null;
        protocol?.Dispose();
    }

    private async ValueTask DisposeProtocolAsync()
    {
        IProtocol? protocol = Protocol;
        Protocol = null;
        if (protocol is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            protocol?.Dispose();
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
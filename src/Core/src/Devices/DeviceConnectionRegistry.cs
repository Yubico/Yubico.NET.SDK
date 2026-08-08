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

using System.Collections.Concurrent;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Process-wide ownership coordinator per interface device, keyed by <see cref="IYubiKey.DeviceId" />.
///     Connections hold the lease; discovery takes a nonblocking exclusive lease. This makes the
///     connection/discovery exclusion atomic instead of relying on count/check/recheck timing.
/// </summary>
/// <remarks>
///     <para>
///         The lease belongs to the CONNECTION, not to a session: it is acquired before physical connection
///         creation and released if creation fails or when that connection is disposed. Sessions come and go
///         over a connection without touching it.
///     </para>
///     <para>
///         CCID (SmartCard) and OTP HID interfaces admit exactly ONE live connection. CCID holds one selected
///         applet on the basic channel, so a second connection's SELECT would deselect the first holder's applet
///         — measured, SW=0x6D00, see docs/architecture/connection-ownership-and-contention.md. An OTP HID logical
///         exchange spans multiple feature reports, which separate protocol instances must not interleave.
///         A second acquisition is refused immediately with <see cref="ConnectionInUseException" />; it never
///         waits, because waiting for an unbounded session to end is worse than a clear error. FIDO HID remains
///         shared and is the route Management takes while CCID is held.
///     </para>
///     <para>
///         Discovery holds its exclusive lease across physical connect, device-info exchange, and connection
///         disposal. Waiting connections have priority over a later discovery attempt so repeated scans cannot
///         starve a session.
///     </para>
///     <para>
///         LIMITATION: in-process only. A different process holding the card can still interfere; that is
///         outside what this registry can see. Keyed by the PER-INTERFACE DeviceId (reader name / HID path
///         based), which is stable across scans while the device stays plugged, so registrations made through
///         devices from one scan are visible to readers created in later scans. A composite's own DeviceId is
///         never used as a key here — <see cref="ResolveInterfaceId" /> always resolves to a member — because
///         it names the evidence tier that resolved the merge and is not stable across scans. Idle coordinator entries are retained
///         for the process lifetime: this is bounded by unique interface IDs observed and avoids unsafe
///         remove/recreate races between lease acquisition and dictionary eviction.
///     </para>
/// </remarks>
internal static class DeviceConnectionRegistry
{
    private static readonly ConcurrentDictionary<string, InterfaceOwnership> Interfaces = new();

    /// <summary>Whether this process currently holds at least one live connection to the interface.</summary>
    public static bool IsInUse(string deviceId) =>
        Interfaces.TryGetValue(deviceId, out var ownership) && ownership.HasConnections;

    /// <summary>
    ///     Whether the interface of <paramref name="device" /> that would serve <paramref name="connection" />
    ///     is in use. Composite members are resolved through
    ///     <see cref="CompositeYubiKey.TryResolveMember" />, the same routing a connect uses, so the check
    ///     matches the interface a read would actually open.
    /// </summary>
    public static bool IsInterfaceInUse(IYubiKey device, ConnectionType connection) =>
        IsInUse(ResolveInterfaceId(device, connection));

    /// <summary>
    ///     The per-interface DeviceId that a connect for <paramref name="connection" /> would register: the
    ///     composite member selected by <see cref="CompositeYubiKey.TryResolveMember" />, or the device's
    ///     own id when it is not a composite or exposes no member for the connection. Sharing that resolver
    ///     with the connect path is what makes this agreement compiler-enforced rather than a convention.
    /// </summary>
    public static string ResolveInterfaceId(IYubiKey device, ConnectionType connection)
    {
        if (device is not CompositeYubiKey composite)
            return device.DeviceId;

        return composite.TryResolveMember(connection, out var member)
            ? member.DeviceId
            : device.DeviceId;
    }

    /// <summary>
    ///     Acquires the interface lease for a connection, before physical connection creation. Waits while
    ///     discovery owns the interface; cancellation applies only while waiting.
    /// </summary>
    /// <param name="deviceId">The per-interface device id.</param>
    /// <param name="exclusive">
    ///     <see langword="true" /> for an interface that admits one live connection and refuses a second
    ///     (CCID or OTP HID); <see langword="false" /> for a shared interface (FIDO HID).
    /// </param>
    /// <param name="cancellationToken">Cancels the wait for an active discovery read only.</param>
    /// <exception cref="ConnectionInUseException">
    ///     <paramref name="exclusive" /> and the interface already has a live connection.
    /// </exception>
    public static ValueTask<IDisposable> AcquireConnectionAsync(
        string deviceId,
        bool exclusive,
        CancellationToken cancellationToken = default) =>
        GetOwnership(deviceId).AcquireConnectionAsync(deviceId, exclusive, cancellationToken);

    /// <summary>
    ///     Attempts to acquire exclusive discovery ownership without waiting. Returns <c>null</c> while any
    ///     connection owns or is already waiting for the interface.
    /// </summary>
    public static IDisposable? TryAcquireDiscovery(string deviceId) =>
        GetOwnership(deviceId).TryAcquireDiscovery();

    private static InterfaceOwnership GetOwnership(string deviceId) =>
        Interfaces.GetOrAdd(deviceId, static _ => new InterfaceOwnership());

    private enum LeaseKind
    {
        Connection,
        Discovery
    }

    private sealed class InterfaceOwnership
    {
        private readonly Lock _sync = new();
        private int _connectionCount;
        private int _waitingConnections;
        private bool _discoveryActive;
        private TaskCompletionSource? _discoveryReleased;

        public bool HasConnections
        {
            get
            {
                lock (_sync)
                    return _connectionCount > 0;
            }
        }

        public async ValueTask<IDisposable> AcquireConnectionAsync(
            string deviceId,
            bool exclusive,
            CancellationToken cancellationToken)
        {
            Task discoveryReleased;
            lock (_sync)
            {
                if (!_discoveryActive)
                    return Claim(deviceId, exclusive);

                _waitingConnections++;
                discoveryReleased = _discoveryReleased?.Task
                    ?? throw new InvalidOperationException("Discovery ownership has no release signal.");
            }

            try
            {
                await discoveryReleased.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                lock (_sync)
                    _waitingConnections--;
                throw;
            }

            lock (_sync)
            {
                _waitingConnections--;
                if (_discoveryActive)
                    throw new InvalidOperationException("Discovery ownership was reacquired ahead of a waiting connection.");

                return Claim(deviceId, exclusive);
            }
        }

        /// <summary>Takes the lease, or refuses when the interface is exclusive and already held.</summary>
        private IDisposable Claim(string deviceId, bool exclusive)
        {
            if (exclusive && _connectionCount > 0)
                throw new ConnectionInUseException(
                    $"The exclusive interface '{deviceId}' already has a live connection in this process. " +
                    "Concurrent connections could change shared application state or interleave a multi-report " +
                    "exchange. Dispose the existing connection first, then open the next connection.");

            _connectionCount++;
            return new Registration(this, LeaseKind.Connection);
        }

        public IDisposable? TryAcquireDiscovery()
        {
            lock (_sync)
            {
                if (_discoveryActive || _connectionCount > 0 || _waitingConnections > 0)
                    return null;

                _discoveryActive = true;
                _discoveryReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return new Registration(this, LeaseKind.Discovery);
            }
        }

        public void Release(LeaseKind kind)
        {
            TaskCompletionSource? releaseSignal = null;
            lock (_sync)
            {
                if (kind == LeaseKind.Connection)
                {
                    if (_connectionCount <= 0)
                        throw new InvalidOperationException("Connection ownership was released without a matching acquisition.");

                    _connectionCount--;
                    return;
                }

                if (!_discoveryActive)
                    throw new InvalidOperationException("Discovery ownership was released without a matching acquisition.");

                _discoveryActive = false;
                releaseSignal = _discoveryReleased;
                _discoveryReleased = null;
            }

            releaseSignal?.TrySetResult();
        }
    }

    private sealed class Registration(InterfaceOwnership ownership, LeaseKind kind) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            ownership.Release(kind);
        }
    }
}
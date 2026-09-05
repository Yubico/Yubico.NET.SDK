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
///     Process-wide ownership coordinator keyed by stable member interface IDs. A physical-device connection
///     claims all known member IDs; discovery takes one nonblocking per-interface lease.
/// </summary>
/// <remarks>
///     <para>
///         The lease belongs to the CONNECTION, not to a session: it is acquired before physical connection
///         creation and released if creation fails or when that connection is disposed. Sessions come and go
///         over a connection without touching it.
///     </para>
///     <para>
///         A grouped physical YubiKey admits one live connection across CCID, FIDO HID, and OTP HID. A second
///         acquisition through any known member is refused immediately with
///         <see cref="ConnectionInUseException" />; it never waits for an unbounded holder to end.
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
///         devices from one scan are visible to readers created in later scans. For a composite,
///         <see cref="ResolveInterfaceId" /> resolves the requested connection to a member instead of using
///         the composite DeviceId, which names the evidence tier and is not stable across scans. Standalone
///         devices use their own DeviceId. Idle coordinator entries are retained
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
    ///     is in use. Published-device slots are resolved through
    ///     <see cref="YubiKeyDevice.TryResolveSlot" />, the same routing a connect uses, so the check
    ///     matches the interface a read would actually open.
    /// </summary>
    public static bool IsInterfaceInUse(IYubiKey device, ConnectionType connection) =>
        IsInUse(ResolveInterfaceId(device, connection));

    /// <summary>
    ///     Resolves the member interface ID serving <paramref name="connection" />. Discovery uses this to
    ///     coordinate one interface; grouped public connections acquire the complete member lease scope.
    /// </summary>
    public static string ResolveInterfaceId(IYubiKey device, ConnectionType connection)
    {
        if (device is not YubiKeyDevice published)
            return device.DeviceId;

        return published.TryResolveSlot(connection, out var slot)
            ? slot.InterfaceId
            : device.DeviceId;
    }
    /// <summary>
    ///     Acquires every known interface lease for one physical YubiKey as a single logical registration,
    ///     before physical connection creation. Interface ids are de-duplicated and acquired in ordinal
    ///     order; partial acquisition rolls back in reverse order. Waits while discovery owns an interface;
    ///     cancellation applies only while waiting. Standalone devices pass a one-element scope.
    /// </summary>
    /// <exception cref="ConnectionInUseException">Any member ID already has a live connection.</exception>
    public static async ValueTask<IDisposable> AcquireConnectionAsync(
        IReadOnlyCollection<string> interfaceIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interfaceIds);
        if (interfaceIds.Count == 0)
            throw new ArgumentException("At least one interface id is required.", nameof(interfaceIds));

        var uniqueIds = new HashSet<string>(interfaceIds, StringComparer.Ordinal);
        var orderedIds = new string[uniqueIds.Count];
        uniqueIds.CopyTo(orderedIds);
        Array.Sort(orderedIds, StringComparer.Ordinal);

        var acquired = new List<IDisposable>(orderedIds.Length);
        try
        {
            foreach (var id in orderedIds)
            {
                acquired.Add(await GetOwnership(id)
                    .AcquireConnectionAsync(id, cancellationToken)
                    .ConfigureAwait(false));
            }

            return new CompositeRegistration(acquired);
        }
        catch
        {
            for (var i = acquired.Count - 1; i >= 0; i--)
                acquired[i].Dispose();
            throw;
        }
    }

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
            CancellationToken cancellationToken)
        {
            Task discoveryReleased;
            lock (_sync)
            {
                if (!_discoveryActive)
                    return Claim(deviceId);

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

                return Claim(deviceId);
            }
        }

        /// <summary>Takes the lease, or refuses when the interface is already held.</summary>
        private IDisposable Claim(string deviceId)
        {
            if (_connectionCount > 0)
                throw new ConnectionInUseException(
                    $"This YubiKey already has a live connection in this process (held interface: '{deviceId}'). " +
                    "A physical YubiKey supports one live connection at a time across all interfaces. " +
                    "Dispose the existing connection first; connections are reused sequentially, not in parallel.");

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

    private sealed class CompositeRegistration(IReadOnlyList<IDisposable> registrations) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            for (var i = registrations.Count - 1; i >= 0; i--)
                registrations[i].Dispose();
        }
    }
}
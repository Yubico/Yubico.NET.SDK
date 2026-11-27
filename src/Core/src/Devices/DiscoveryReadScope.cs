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

namespace Yubico.YubiKit.Core.Devices;

// A finder owns one read scope, so its single-flight reads cannot be joined by an independent finder.
// Within that scope, generation and supersede token live in one immutable transport epoch captured under
// a short lock. Transport activity either advances the default direct-call scope or terminally retires a
// finder scope; a read admitted after retirement fails before worker admission or hardware open.
/// <summary>Owns finder-scoped single-flight reads and transport epochs.</summary>
internal sealed class DiscoveryReadScope
{
    private readonly Lock _gate = new();
    private readonly ConcurrentDictionary<DiscoveryReadKey, SharedDeviceInfoRead> _inFlightReads = new();
    private readonly ConcurrentDictionary<string, IReadOnlyList<SemaphoreSlim>> _usbReadGates = new();
    private TransportEpoch _currentEpoch = new(generation: 0);
    private bool _retired;

    internal bool TryCaptureEpoch(out TransportEpoch epoch)
    {
        lock (_gate)
        {
            epoch = _currentEpoch;
            return !_retired;
        }
    }

    internal SharedDeviceInfoRead GetOrAdd(DiscoveryReadKey key, Func<DiscoveryReadKey, SharedDeviceInfoRead> create) =>
        _inFlightReads.GetOrAdd(key, create);

    internal void CoordinateUsbRead(string interfaceId, ushort? pid) =>
        _usbReadGates[interfaceId] = DiscoveryReadAdmission.GatesFor(pid);

    internal IReadOnlyList<SemaphoreSlim>? ReadGatesFor(string interfaceId) =>
        _usbReadGates.GetValueOrDefault(interfaceId);

    internal void Remove(DiscoveryReadKey key, SharedDeviceInfoRead read) =>
        _ = _inFlightReads.TryRemove(KeyValuePair.Create(key, read));

    internal bool IsCurrent(TransportEpoch epoch)
    {
        lock (_gate)
            return !_retired && ReferenceEquals(_currentEpoch, epoch);
    }

    /// <summary>
    ///     Signals that hotplug activity was observed, detaching future device-info reads from any
    ///     in-flight read started against the previous physical topology, and failing superseded reads
    ///     that have not yet opened their interface (including reads still queued for worker admission,
    ///     whose wait is cancelled so they cannot accumulate behind hung workers). Called from the same
    ///     place <see cref="FindYubiKeys" /> invalidates its identity/metadata caches.
    /// </summary>
    internal void Advance()
    {
        TransportEpoch retired;
        lock (_gate)
        {
            if (_retired)
                return;

            retired = _currentEpoch;
            _currentEpoch = new TransportEpoch(retired.Generation + 1);
        }

        retired.MarkSuperseded();
    }

    internal void Retire()
    {
        TransportEpoch retired;
        lock (_gate)
        {
            if (_retired)
                return;

            _retired = true;
            retired = _currentEpoch;
        }

        retired.MarkSuperseded();
    }
}

/// <summary>
///     One immutable hotplug epoch: a generation and the cancellation source that fires when this
///     epoch is superseded. The retired CTS is only cancelled, never disposed, because detached
///     waiters may still be observing it (a cancelled timer-less CTS is plain collectible garbage).
/// </summary>
internal sealed class TransportEpoch(long generation)
{
    private readonly CancellationTokenSource _superseded = new();

    public long Generation { get; } = generation;

    /// <summary>Fires when this epoch has been replaced by newer transport activity.</summary>
    public CancellationToken Superseded => _superseded.Token;

    public void MarkSuperseded() => _superseded.Cancel();
}

/// <summary>Identifies one interface/connection read within a transport generation.</summary>
internal readonly record struct DiscoveryReadKey(string InterfaceId, ConnectionType Connection, long Generation)
{
    internal DiscoveryReadSkippedException Superseded() =>
        new(InterfaceId, DiscoveryReadSkipCause.SupersededByTransportActivity);
}
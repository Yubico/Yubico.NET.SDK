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
///     Normal connections share session ownership; discovery takes a nonblocking exclusive lease. This makes
///     the session/discovery exclusion atomic instead of relying on count/check/recheck timing.
/// </summary>
/// <remarks>
///     <para>
///         Session ownership is acquired before physical connection creation and is released if creation
///         fails or when the wrapping connection is disposed. Discovery holds its exclusive lease across
///         physical connect, device-info exchange, and connection disposal. Waiting sessions have priority
///         over a later discovery attempt so repeated scans cannot starve a session.
///     </para>
///     <para>
///         LIMITATION: in-process only. A different process holding the card can still interfere; that is
///         outside what this registry can see. Keyed by DeviceId string, which is stable across scans while
///         the device stays plugged (reader name / HID path based), so registrations made through devices
///         from one scan are visible to readers created in later scans. Idle coordinator entries are retained
///         for the process lifetime: this is bounded by unique interface IDs observed and avoids unsafe
///         remove/recreate races between lease acquisition and dictionary eviction.
///     </para>
/// </remarks>
internal static class DeviceConnectionRegistry
{
    private static readonly ConcurrentDictionary<string, InterfaceOwnership> Interfaces = new();

    /// <summary>Whether this process currently holds at least one live connection to the interface.</summary>
    public static bool IsInUse(string deviceId) =>
        Interfaces.TryGetValue(deviceId, out var ownership) && ownership.HasSessions;

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
    ///     Acquires shared session ownership before physical connection creation. Waits while discovery owns
    ///     the interface; cancellation applies only while waiting.
    /// </summary>
    public static ValueTask<IDisposable> AcquireSessionAsync(
        string deviceId,
        CancellationToken cancellationToken = default) =>
        GetOwnership(deviceId).AcquireSessionAsync(cancellationToken);

    /// <summary>
    ///     Attempts to acquire exclusive discovery ownership without waiting. Returns <c>null</c> while any
    ///     session owns or is already waiting for the interface.
    /// </summary>
    public static IDisposable? TryAcquireDiscovery(string deviceId) =>
        GetOwnership(deviceId).TryAcquireDiscovery();

    private static InterfaceOwnership GetOwnership(string deviceId) =>
        Interfaces.GetOrAdd(deviceId, static _ => new InterfaceOwnership());

    private enum LeaseKind
    {
        Session,
        Discovery
    }

    private sealed class InterfaceOwnership
    {
        private readonly Lock _sync = new();
        private int _sessionCount;
        private int _waitingSessions;
        private bool _discoveryActive;
        private TaskCompletionSource? _discoveryReleased;

        public bool HasSessions
        {
            get
            {
                lock (_sync)
                    return _sessionCount > 0;
            }
        }

        public async ValueTask<IDisposable> AcquireSessionAsync(CancellationToken cancellationToken)
        {
            Task? discoveryReleased = null;
            lock (_sync)
            {
                if (!_discoveryActive)
                {
                    _sessionCount++;
                    return new Registration(this, LeaseKind.Session);
                }

                _waitingSessions++;
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
                    _waitingSessions--;
                throw;
            }

            lock (_sync)
            {
                _waitingSessions--;
                if (_discoveryActive)
                    throw new InvalidOperationException("Discovery ownership was reacquired ahead of a waiting session.");

                _sessionCount++;
                return new Registration(this, LeaseKind.Session);
            }
        }

        public IDisposable? TryAcquireDiscovery()
        {
            lock (_sync)
            {
                if (_discoveryActive || _sessionCount > 0 || _waitingSessions > 0)
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
                if (kind == LeaseKind.Session)
                {
                    if (_sessionCount <= 0)
                        throw new InvalidOperationException("Session ownership was released without a matching acquisition.");

                    _sessionCount--;
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
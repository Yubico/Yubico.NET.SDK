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
///     Process-wide registry of live connections per per-interface device, keyed by
///     <see cref="IYubiKey.DeviceId" />. Discovery's best-effort device-info reads consult it to skip
///     interfaces this process is actively using, so passive enumeration never clobbers the applet
///     selection or authentication state of an open session (a second shared-mode CCID handle shares the
///     card's basic logical channel — a discovery <c>SELECT</c> would deselect the session's applet).
/// </summary>
/// <remarks>
///     <para>
///         Registration is wired into the per-interface device implementations (<see cref="PcscYubiKey" />,
///         <see cref="HidYubiKey" />): every opened connection is counted, and the wrapping registration is
///         released exactly once when the connection is disposed. Skipped reads degrade the same way as any
///         failed best-effort read: identity unknown ⇒ conservative no-merge; metadata read falls through to
///         a transport that is not in use.
///     </para>
///     <para>
///         LIMITATION: in-process only. A different process holding the card can still interfere; that is
///         outside what this registry can see. Keyed by DeviceId string, which is stable across scans while
///         the device stays plugged (reader name / HID path based), so registrations made through devices
///         from one scan are visible to readers created in later scans.
///     </para>
/// </remarks>
internal static class DeviceConnectionRegistry
{
    private static readonly ConcurrentDictionary<string, int> LiveCounts = new();

    /// <summary>Whether this process currently holds at least one live connection to the interface.</summary>
    public static bool IsInUse(string deviceId) =>
        LiveCounts.TryGetValue(deviceId, out var count) && count > 0;

    /// <summary>
    ///     Whether another holder shares the interface beyond the caller's own single registration.
    ///     PRECONDITION: the caller holds exactly one live registration for <paramref name="deviceId" />
    ///     (discovery reads register through the device's <c>ConnectAsync</c> before calling this).
    /// </summary>
    public static bool IsInUseByOther(string deviceId) =>
        LiveCounts.TryGetValue(deviceId, out var count) && count > 1;

    /// <summary>
    ///     Whether the interface of <paramref name="device" /> that would serve <paramref name="connection" />
    ///     is in use. Resolves composite members the same way <see cref="CompositeYubiKey.ConnectAsync{T}" />
    ///     routes connects, so the check matches the interface a read would actually open.
    /// </summary>
    public static bool IsInterfaceInUse(IYubiKey device, ConnectionType connection) =>
        IsInUse(ResolveInterfaceId(device, connection));

    /// <summary>
    ///     The per-interface DeviceId that a connect for <paramref name="connection" /> would register:
    ///     the FIRST composite member supporting the connection (mirroring
    ///     <see cref="CompositeYubiKey.ConnectAsync{T}" /> routing) or the device's own id.
    /// </summary>
    public static string ResolveInterfaceId(IYubiKey device, ConnectionType connection)
    {
        if (device is not CompositeYubiKey composite)
            return device.DeviceId;

        foreach (var member in composite.Members)
        {
            if (member.AvailableConnections.SupportsConnection(connection))
                return member.DeviceId;
        }

        return device.DeviceId;
    }

    /// <summary>Counts a live connection. Dispose the returned registration exactly when the connection closes.</summary>
    public static IDisposable Register(string deviceId)
    {
        LiveCounts.AddOrUpdate(deviceId, 1, static (_, count) => count + 1);
        return new Registration(deviceId);
    }

    private sealed class Registration(string deviceId) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            // Decrement; remove the entry at zero so the map does not grow with unplugged devices.
            while (true)
            {
                if (!LiveCounts.TryGetValue(deviceId, out var count))
                    return;

                if (count <= 1)
                {
                    if (LiveCounts.TryRemove(KeyValuePair.Create(deviceId, count)))
                        return;
                }
                else if (LiveCounts.TryUpdate(deviceId, count - 1, count))
                {
                    return;
                }
            }
        }
    }
}
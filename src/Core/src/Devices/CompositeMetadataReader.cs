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

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Best-effort read of read-only <see cref="DeviceInfo"/> from a merged physical device for metadata only.
/// </summary>
/// <remarks>
///     Runs after grouping, independently of <see cref="DiscoveryIdentityReader"/>. It makes one pass over
///     available transports in preference order (CCID → OTP HID → FIDO HID), without retrying a failed transport.
///     Each read receives the remaining caller-wait budget from a shared stopwatch. A read exceeding that
///     remainder abandons the wait, while dispatched work retains its ownership through cleanup (see
///     <see cref="DiscoveryDeviceInfoReader.ReadBoundedAsync"/>). After a failure the next transport is tried
///     if budget remains. Total failure returns null; the already-computed grouping stays unchanged.
///     Caller cancellation propagates rather than falling through to another transport.
/// </remarks>
internal static class CompositeMetadataReader
{
    private static readonly ConnectionType[] PreferredOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp, ConnectionType.HidFido];

    public static async Task<DeviceInfo?> TryReadAsync(
        IYubiKey device,
        TimeSpan totalBudget,
        ILogger logger,
        CancellationToken cancellationToken,
        DiscoveryReadScope? scope = null)
    {
        var clock = Stopwatch.StartNew();

        foreach (var connection in PreferredOrder)
        {
            if (!device.SupportsConnection(connection))
                continue;

            if (DeviceConnectionRegistry.IsInterfaceInUse(device, connection))
            {
                // Never disturb an interface this process is using (a discovery SELECT on a second shared
                // CCID handle can change the session's selected applet). The next available interface can
                // still be attempted under its own discovery admission and remaining wait budget.
                logger.LogDebug(
                    "Metadata read for {DeviceId} over {Connection} skipped: interface has a live connection in this process; trying next transport.",
                    device.DeviceId,
                    connection);
                continue;
            }

            var remaining = totalBudget - clock.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                logger.LogDebug(
                    "Metadata read budget exhausted for {DeviceId}; skipping remaining transports.",
                    device.DeviceId);
                return null;
            }

            try
            {
                return await DiscoveryDeviceInfoReader
                    .ReadBoundedAsync(device, connection, remaining, logger, cancellationToken, scope: scope)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogDebug(
                    e,
                    "Metadata read for {DeviceId} over {Connection} failed; trying next transport.",
                    device.DeviceId,
                    connection);
            }
        }

        return null;
    }

}
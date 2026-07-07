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
///     Distinct from <see cref="DiscoveryIdentityReader"/>: this is decoupled from merge correctness. It makes
///     a single pass over the device's available transports in preference order (CCID → OTP HID → FIDO HID)
///     with NO retries and a hard total wall-clock budget shared across all transports, so a busy or slow
///     interface cannot stall discovery — a read that exceeds the remaining budget is abandoned, not aborted
///     (see <see cref="ProtocolDeviceInfo.ReadBoundedAsync" />), and the reader falls through to the next
///     transport if budget remains. Total failure returns <c>null</c>; the merge never depends on the result.
///     Transport preference is CCID → OTP HID → FIDO HID, matching the Rust reference.
/// </remarks>
internal static class CompositeMetadataReader
{
    private static readonly ConnectionType[] PreferredOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp, ConnectionType.HidFido];

    public static async Task<DeviceInfo?> TryReadAsync(
        IYubiKey device,
        TimeSpan totalBudget,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();

        foreach (var connection in PreferredOrder)
        {
            if (!device.SupportsConnection(connection))
                continue;

            if (DeviceConnectionRegistry.IsInterfaceInUse(device, connection))
            {
                // Never disturb an interface this process is using (a discovery SELECT on a second shared
                // CCID handle would deselect the session's applet). Other interfaces of the same physical
                // key are independent USB interfaces and remain safe to read.
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
                return await ProtocolDeviceInfo
                    .ReadBoundedAsync(device, connection, remaining, logger, cancellationToken)
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
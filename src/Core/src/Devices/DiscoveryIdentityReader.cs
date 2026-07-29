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
using Yubico.YubiKit.Core.Abstractions;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads read-only <see cref="DeviceInfo" /> (including the serial number) from a single per-interface
///     device, used to disambiguate multiple same-PID physical keys during composite discovery.
/// </summary>
/// <remarks>
///     This opens a short-lived connection over the interface and reads device info via the Core-owned
///     <see cref="ProtocolDeviceInfo" />, with a hard per-attempt wall-clock budget so a card that is busy
///     with a long applet operation (e.g. RSA key generation) cannot stall the scan — the attempt is
///     abandoned, not aborted (see <see cref="ProtocolDeviceInfo.ReadBoundedAsync" />). It retries transient
///     PC/SC failures (e.g. sharing violations) because, unlike the metadata read, a correct serial here is
///     required to tell two same-model keys apart; a budget timeout is NOT retried because it indicates a
///     busy card, and retrying would only extend the stall. Any failure is swallowed and reported as
///     <c>null</c> so discovery degrades to conservative no-merge rather than aborting. It uses only Core
///     primitives and introduces no dependency on Management.
/// </remarks>
internal static class DiscoveryIdentityReader
{
    private const int MaxAttempts = 3;

    // Hard wall-clock budget for one connect+read attempt. Generous: an idle key answers a device-info
    // read in well under a second; only a card blocked by a long applet operation exceeds this.
    private static readonly TimeSpan PerAttemptBudget = TimeSpan.FromSeconds(2);

    public static async Task<DeviceInfo?> TryReadAsync(
        IYubiKey device,
        ConnectionType connection,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (DeviceConnectionRegistry.IsInterfaceInUse(device, connection))
        {
            logger.LogDebug(
                "Discovery identity read for {DeviceId} over {Connection} skipped: interface has a live connection in this process; treating serial as unknown.",
                device.DeviceId,
                connection);
            return null;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ProtocolDeviceInfo
                    .ReadBoundedAsync(device, connection, PerAttemptBudget, logger, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DiscoveryReadSkippedException)
            {
                // A session opened the interface while the read was starting (TOCTOU window behind the
                // pre-connect check above). Same degradation as the pre-connect skip; retrying is pointless.
                logger.LogDebug(
                    "Discovery identity read for {DeviceId} over {Connection} aborted: interface gained a live connection; treating serial as unknown.",
                    device.DeviceId,
                    connection);
                return null;
            }
            catch (TimeoutException)
            {
                logger.LogDebug(
                    "Discovery identity read for {DeviceId} over {Connection} exceeded its {Budget} budget (device busy?); treating serial as unknown.",
                    device.DeviceId,
                    connection,
                    PerAttemptBudget);
                return null;
            }
            catch (Exception e)
            {
                if (attempt >= MaxAttempts)
                {
                    logger.LogDebug(
                        e,
                        "Discovery identity read failed for {DeviceId} over {Connection} after {Attempts} attempts; treating serial as unknown.",
                        device.DeviceId,
                        connection,
                        attempt);
                    return null;
                }

                logger.LogDebug(
                    e,
                    "Discovery identity read attempt {Attempt} for {DeviceId} over {Connection} failed; retrying.",
                    attempt,
                    device.DeviceId,
                    connection);
                await Task.Delay(150 * attempt, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
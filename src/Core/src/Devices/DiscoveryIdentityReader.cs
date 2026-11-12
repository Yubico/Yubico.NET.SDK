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

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads read-only <see cref="DeviceInfo" /> (including the serial number) from a single per-interface
///     device, used to disambiguate multiple same-PID physical keys during composite discovery.
/// </summary>
/// <remarks>
///     Opens a discovery-owned connection through <see cref="DiscoveryDeviceInfoReader"/>. Each attempt has
///     a bounded caller wait, including admission and teardown; already-dispatched native work can continue
///     after that wait ends. Ordinary exceptions are retried up to three attempts, with 150 ms then 300 ms
///     backoff. A skipped read or timeout returns null immediately; a timeout alone does not identify a busy
///     card. Other failures return null after the final attempt, leaving grouping to the remaining evidence.
///     Cancellation propagates. The shared read retains native ownership through actual cleanup.
/// </remarks>
internal static class DiscoveryIdentityReader
{
    private const int MaxAttempts = 3;

    public static async Task<DeviceInfo?> TryReadAsync(
        IYubiKeyConnectionSlot device,
        ConnectionType connection,
        ILogger logger,
        CancellationToken cancellationToken,
        DiscoveryReadScope? scope = null)
    {
        if (DeviceConnectionRegistry.IsInUse(device.InterfaceId))
        {
            logger.LogDebug(
                "Discovery identity read for {DeviceId} over {Connection} skipped: interface has a live connection in this process; treating serial as unknown.",
                device.InterfaceId,
                connection);
            return null;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await DiscoveryDeviceInfoReader
                    .ReadSlotBoundedAsync(
                        device,
                        connection,
                        DiscoveryBudgets.IdentityRead,
                        logger,
                        cancellationToken,
                        waitForWorkerSlot: true,
                        scope)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DiscoveryReadSkippedException e)
            {
                // With identity reads waiting for worker slots, the remaining skip causes are a session
                // owning the interface (TOCTOU window behind the pre-connect check above — same degradation
                // as the pre-connect skip; retrying is pointless) or a device without the discovery-only
                // connection path. The cause is logged verbatim so diagnostics can tell them apart.
                logger.LogDebug(
                    "Discovery identity read for {DeviceId} over {Connection} skipped ({Cause}); treating serial as unknown.",
                    device.InterfaceId,
                    connection,
                    e.Cause);
                return null;
            }
            catch (TimeoutException)
            {
                logger.LogDebug(
                    "Discovery identity read for {DeviceId} over {Connection} exceeded its {Budget} budget (device busy?); treating serial as unknown.",
                    device.InterfaceId,
                    connection,
                    DiscoveryBudgets.IdentityRead);
                return null;
            }
            catch (Exception e)
            {
                if (attempt >= MaxAttempts)
                {
                    logger.LogDebug(
                        e,
                        "Discovery identity read failed for {DeviceId} over {Connection} after {Attempts} attempts; treating serial as unknown.",
                        device.InterfaceId,
                        connection,
                        attempt);
                    return null;
                }

                logger.LogDebug(
                    e,
                    "Discovery identity read attempt {Attempt} for {DeviceId} over {Connection} failed; retrying.",
                    attempt,
                    device.InterfaceId,
                    connection);
                await Task.Delay(150 * attempt, cancellationToken).ConfigureAwait(false);
            }
        }
    }

}
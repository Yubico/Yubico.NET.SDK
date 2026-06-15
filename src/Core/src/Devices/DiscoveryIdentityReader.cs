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
using Yubico.YubiKit.Core.Protocols.Fido.Hid;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Transports.Hid;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

/// <summary>
///     Reads read-only <see cref="DeviceInfo" /> (including the serial number) from a single per-interface
///     device, used to disambiguate multiple same-PID physical keys during composite discovery.
/// </summary>
/// <remarks>
///     This opens a short-lived connection over the interface and reads device info via the Core-owned
///     <see cref="ProtocolDeviceInfo" />. It retries transient PC/SC sharing violations because, unlike the
///     metadata read, a correct serial here is required to tell two same-model keys apart. Any failure is
///     swallowed and reported as <c>null</c> so discovery degrades to conservative no-merge rather than
///     aborting. It uses only Core primitives and introduces no dependency on Management.
/// </remarks>
internal static class DiscoveryIdentityReader
{
    private const int MaxAttempts = 3;

    public static async Task<DeviceInfo?> TryReadAsync(
        IYubiKey device,
        ConnectionType connection,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var conn = await ConnectAsync(device, connection, cancellationToken).ConfigureAwait(false);
                return await ProtocolDeviceInfo.ReadAsync(conn, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
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

    internal static Task<IConnection> ConnectAsync(
        IYubiKey device,
        ConnectionType connection,
        CancellationToken cancellationToken) => connection switch
        {
            ConnectionType.SmartCard => Upcast(device.ConnectAsync<ISmartCardConnection>(cancellationToken)),
            ConnectionType.HidFido => Upcast(device.ConnectAsync<IFidoHidConnection>(cancellationToken)),
            ConnectionType.HidOtp => Upcast(device.ConnectAsync<IOtpHidConnection>(cancellationToken)),
            _ => throw new NotSupportedException($"Cannot open connection {connection} for identity read.")
        };

    private static async Task<IConnection> Upcast<TConnection>(Task<TConnection> task)
        where TConnection : class, IConnection => await task.ConfigureAwait(false);
}
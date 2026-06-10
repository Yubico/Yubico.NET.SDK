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
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.Hid.Interfaces;
using Yubico.YubiKit.Core.Hid.Otp;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Reads read-only <see cref="DeviceInfo" /> (including the serial number) from a single per-interface
///     device during composite-device discovery, so interfaces can be grouped into physical YubiKeys by serial.
/// </summary>
/// <remarks>
///     This opens a short-lived connection over the interface, reads device info via the Core-owned
///     <see cref="DeviceInfoReader" />, and disposes the connection (via the protocol it owns) before returning.
///     Any failure is swallowed and reported as <c>null</c> so discovery degrades to conservative no-merge
///     rather than aborting. It uses only Core primitives and introduces no dependency on Management.
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
        // A short retry handles transient PC/SC sharing violations (SCARD_E_SHARING_VIOLATION) and
        // not-ready cards that occur when another connection to the same key is briefly open during
        // discovery. A persistent failure resolves to null (conservative no-merge), never an error.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return connection switch
                {
                    ConnectionType.SmartCard => await ReadSmartCardAsync(device, cancellationToken).ConfigureAwait(false),
                    ConnectionType.HidFido => await ReadFidoAsync(device, cancellationToken).ConfigureAwait(false),
                    ConnectionType.HidOtp => await ReadOtpAsync(device, cancellationToken).ConfigureAwait(false),
                    _ => null
                };
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

    private static async Task<DeviceInfo?> ReadSmartCardAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var conn = await device.ConnectAsync<ISmartCardConnection>(cancellationToken).ConfigureAwait(false);
        var protocol = PcscProtocolFactory<ISmartCardConnection>.Create().Create(conn);
        try
        {
            await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
            return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            protocol.Dispose();
        }
    }

    private static async Task<DeviceInfo?> ReadFidoAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var conn = await device.ConnectAsync<IFidoHidConnection>(cancellationToken).ConfigureAwait(false);
        var protocol = FidoProtocolFactory.Create().Create(conn);
        try
        {
            // Initializes the HID channel; the application id is unused for HID.
            await protocol.SelectAsync(ApplicationIds.Management, cancellationToken).ConfigureAwait(false);
            return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            protocol.Dispose();
        }
    }

    private static async Task<DeviceInfo?> ReadOtpAsync(IYubiKey device, CancellationToken cancellationToken)
    {
        var conn = await device.ConnectAsync<IOtpHidConnection>(cancellationToken).ConfigureAwait(false);
        var protocol = OtpProtocolFactory.Create().Create(conn);
        try
        {
            return await DeviceInfoReader.ReadAsync(protocol, null, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            protocol.Dispose();
        }
    }
}
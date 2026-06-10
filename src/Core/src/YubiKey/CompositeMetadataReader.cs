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
using Yubico.YubiKit.Core.Interfaces;

namespace Yubico.YubiKit.Core.YubiKey;

/// <summary>
///     Best-effort read of read-only <see cref="DeviceInfo"/> from a merged physical device for metadata only.
/// </summary>
/// <remarks>
///     Distinct from <see cref="DiscoveryIdentityReader"/>: this is decoupled from merge correctness. It makes
///     a single bounded pass over the device's available transports in preference order (CCID → OTP HID →
///     FIDO HID) with a hard per-read timeout and NO retries, so a locked or slow CCID cannot stall discovery —
///     it simply falls through to a HID transport, and total failure returns <c>null</c>. The merge never
///     depends on the result. Transport preference is CCID → OTP HID → FIDO HID, matching the Rust reference.
/// </remarks>
internal static class CompositeMetadataReader
{
    private static readonly ConnectionType[] PreferredOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp, ConnectionType.HidFido];

    public static async Task<DeviceInfo?> TryReadAsync(
        IYubiKey device,
        TimeSpan perReadTimeout,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        foreach (var connection in PreferredOrder)
        {
            if (!device.SupportsConnection(connection))
                continue;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(perReadTimeout);

            try
            {
                var conn = await DiscoveryIdentityReader
                    .ConnectAsync(device, connection, timeoutCts.Token)
                    .ConfigureAwait(false);
                return await ProtocolDeviceInfo.ReadAsync(conn, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
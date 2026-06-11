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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Convenience extension methods for YubiOTP operations on <see cref="IYubiKey"/>.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        /// Gets the OTP slot configuration state from the YubiKey.
        /// Creates a session, queries state, and disposes automatically.
        /// </summary>
        public async Task<ConfigState> GetConfigStateAsync(CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return session.GetConfigState();
        }

        /// <summary>
        /// Writes a slot configuration to the YubiKey.
        /// Creates a session, writes the configuration, and disposes automatically.
        /// </summary>
        public async ValueTask PutConfigurationAsync(
            Slot slot,
            SlotConfiguration config,
            ReadOnlyMemory<byte> accessCode = default,
            ReadOnlyMemory<byte> currentAccessCode = default,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await session.PutConfigurationAsync(slot, config, accessCode, currentAccessCode, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Performs an HMAC-SHA1 challenge-response operation.
        /// Creates a session, computes the response, and disposes automatically.
        /// </summary>
        public async Task<ReadOnlyMemory<byte>> CalculateHmacSha1Async(
            Slot slot,
            ReadOnlyMemory<byte> challenge,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return await session.CalculateHmacSha1Async(slot, challenge, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a YubiOTP session for the device. The caller owns the session lifetime.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters for a SmartCard session.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="preferredConnection">
        /// Optional explicit transport override. When <see langword="null"/> (the default), YubiOTP selects a
        /// transport in its documented default order: <see cref="ConnectionType.SmartCard"/>, then
        /// <see cref="ConnectionType.HidOtp"/>. When set, it must be one of those two transports and supported
        /// by the device; otherwise an <see cref="ArgumentException"/> (invalid transport) or
        /// <see cref="NotSupportedException"/> (transport not available on this device) is thrown.
        /// </param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        public async Task<YubiOtpSession> CreateYubiOtpSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            ConnectionType? preferredConnection = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await ConnectForYubiOtpAsync(yubiKey, preferredConnection, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                return await YubiOtpSession.CreateAsync(
                        connection,
                        configuration,
                        scpKeyParams,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Source-compatibility overload preserving the pre-Phase-38 positional shape
        /// (<c>scpKeyParams, configuration, cancellationToken</c>); forwards using the default transport order.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        public Task<YubiOtpSession> CreateYubiOtpSessionAsync(
            ScpKeyParameters? scpKeyParams,
            ProtocolConfiguration? configuration,
            CancellationToken cancellationToken) =>
            yubiKey.CreateYubiOtpSessionAsync(scpKeyParams, configuration, null, cancellationToken);
    }

    // YubiOTP is dual-transport (SmartCard or OTP HID). On a physical (possibly multi-connection) device
    // the parameterless ConnectAsync() is ambiguous, so a transport is chosen by an app-specific smart
    // default (SmartCard first, matching the shipped OtpTool example's "prefers SmartCard for richer
    // protocol support", then OTP HID) or an explicit caller override. The ordered default candidate list
    // resolved here drives ConnectSessionTransportAsync, which opens the most-preferred candidate and falls
    // back to OTP HID when the SmartCard transport is held by another process (Phase 38.5).
    private static readonly ConnectionType[] YubiOtpTransportOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

    private static async Task<IConnection> ConnectForYubiOtpAsync(
        IYubiKey yubiKey,
        ConnectionType? preferredConnection,
        CancellationToken cancellationToken)
    {
        var candidates = yubiKey.ResolveSessionTransports(
            preferredConnection,
            "YubiOTP",
            YubiOtpTransportOrder);

        return await yubiKey.ConnectSessionTransportAsync(candidates, "YubiOTP", cancellationToken)
            .ConfigureAwait(false);
    }
}
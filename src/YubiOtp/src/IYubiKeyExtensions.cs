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

using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;

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
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        public async Task<ConfigState> GetConfigStateAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            return session.GetConfigState();
        }

        /// <summary>
        /// Writes a slot configuration to the YubiKey.
        /// Creates a session, writes the configuration, and disposes automatically.
        /// </summary>
        /// <param name="slot">The slot to configure.</param>
        /// <param name="config">The configuration to write.</param>
        /// <param name="accessCode">The new access code.</param>
        /// <param name="currentAccessCode">The current access code.</param>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        public async Task PutConfigurationAsync(
            Slot slot,
            SlotConfiguration config,
            ReadOnlyMemory<byte> accessCode = default,
            ReadOnlyMemory<byte> currentAccessCode = default,
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            await session.PutConfigurationAsync(slot, config, accessCode, currentAccessCode, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Performs an HMAC-SHA1 challenge-response operation.
        /// Creates a session, computes the response, and disposes automatically.
        /// </summary>
        /// <param name="slot">The challenge-response slot.</param>
        /// <param name="challenge">The challenge data.</param>
        /// <param name="options">Optional cross-cutting session creation settings.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        public async Task<ReadOnlyMemory<byte>> CalculateHmacSha1Async(
            Slot slot,
            ReadOnlyMemory<byte> challenge,
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            return await session.CalculateHmacSha1Async(slot, challenge, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a YubiOTP session for the device. The caller owns the session lifetime.
        /// </summary>
        /// <param name="options">
        /// Optional creation settings. YubiOTP selects SmartCard, then HID OTP unless
        /// <see cref="SessionCreationOptions.PreferredConnectionType" /> specifies a supported transport.
        /// Secure-channel parameters force SmartCard when no preference is specified.
        /// </param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        public async Task<YubiOtpSession> CreateYubiOtpSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var scpKeyParams = options?.ScpKeyParameters;
            var preferredConnectionType = options?.PreferredConnectionType;
            var transport = yubiKey.ResolveSessionTransport(
                scpKeyParams is not null && preferredConnectionType is null
                    ? ConnectionType.SmartCard
                    : preferredConnectionType,
                "YubiOTP",
                YubiOtpTransportOrder);
            var sessionOptions = (options ?? new SessionCreationOptions())
                .WithPreferredConnectionType(transport);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await YubiOtpSession
                            .CreateAsync(
                                connection,
                                sessionOptions,
                                ct)
                            .ConfigureAwait(false);

                        // This entry point opened the connection, so the session it returns is the only thing
                        // that can close it. A caller-created connection is never owned this way.
                        session.OwnConnection();
                        return session;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

    }

    // YubiOTP is dual-transport (SmartCard or OTP HID). On a physical (possibly multi-connection) device
    // the parameterless ConnectAsync() is ambiguous, so a transport is chosen by an app-specific smart
    // default (SmartCard first, matching the shipped OtpTool example's "prefers SmartCard for richer
    // protocol support", then OTP HID) or an explicit caller override. The default order selects exactly one
    // transport; connection failures propagate without fallback.
    private static readonly ConnectionType[] YubiOtpTransportOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

}

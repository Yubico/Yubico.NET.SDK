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
        /// Opens a session to obtain its initial OTP-status snapshot, then disposes it automatically.
        /// Unlike an already-open session's cached GetConfigState, this opens a new session for the read.
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
        /// <param name="config">The caller-owned configuration to write; not disposed by this method.</param>
        /// <param name="accessCode">The new six-byte code; omission sends zeros, not preservation of the old code.</param>
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
        /// <remarks>
        /// See IYubiOtpSession.CalculateHmacSha1Async for input/payload/slot-mode distinctions. Use one
        /// challenge-length convention per slot. Explicit OTP HID is the USB recipe; default SmartCard
        /// selection does not establish challenge-command parity. Output ownership transfers to the caller.
        /// Transfer occurs only when this outer task returns successfully, after hidden-session teardown.
        /// If teardown throws after calculation succeeds, the task faults without exposing the result to
        /// clear; the current helper does not clear that stranded result. Keep caller input cleanup in
        /// finally even when creation or teardown fails. Use an explicit session when result ownership
        /// must be visible before teardown; lower-layer copy cleanup remains a separate limitation.
        /// </remarks>
        /// <returns>A caller-owned 20-byte response; clear its backing memory after use.</returns>
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
        /// Creates a YubiOTP session that owns its opened connection. The caller disposes the session.
        /// </summary>
        /// <param name="options">
        /// Optional creation settings. YubiOTP selects SmartCard, then HID OTP unless
        /// <see cref="SessionCreationOptions.PreferredConnectionType" /> specifies a supported transport.
        /// Secure-channel parameters force SmartCard when no preference is specified.
        /// A connection failure propagates without fallback. Session transport support does not guarantee
        /// support for every command on every device/interface.
        /// </param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        /// <remarks>
        /// Grouped connection exclusion is process-local and limited to overlapping known member IDs.
        /// Await complete public operations before disposal; the exchange guard does not own a whole
        /// configuration/status/notification workflow or the caller's configuration and prompt resources.
        /// </remarks>
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
    // default (SmartCard first, then OTP HID) or an explicit caller override. The default order selects exactly one
    // transport; connection failures propagate without fallback.
    private static readonly ConnectionType[] YubiOtpTransportOrder =
        [ConnectionType.SmartCard, ConnectionType.HidOtp];

}
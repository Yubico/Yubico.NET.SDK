// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKit.Management;

/// <summary>
///     Opens Management sessions and provides one-shot device-information and configuration operations.
/// </summary>
/// <remarks>
///     One-shot operations dispose their temporary session and connection before returning.
///     For several sequential operations, create a session once and dispose it with <c>await using</c>.
/// </remarks>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        ///     Queries current device information using a temporary Management session and connection.
        /// </summary>
        /// <remarks>
        ///     Creates and disposes an owned session/connection even if the read fails. Initialization can
        ///     itself query metadata before this read. See <see cref="CreateManagementSessionAsync" /> for
        ///     transport selection and initialization, and <see cref="IManagementSession.GetDeviceInfoAsync" />
        ///     for supported firmware, optional fields, and multi-exchange/cancellation boundaries.
        /// </remarks>
        /// <param name="options">Optional transport, protocol, firmware-override, and SCP creation settings.</param>
        /// <param name="cancellationToken">
        ///     A token requesting cancellation of creation and the read at transport-supported boundaries.
        /// </param>
        /// <returns>
        ///     A <see cref="DeviceInfo" /> structure containing detailed information about the YubiKey device.
        /// </returns>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        /// <exception cref="ArgumentException">The transport preference is invalid.</exception>
        /// <exception cref="NotSupportedException">No requested/supported transport is exposed, or SCP is requested over HID.</exception>
        public async Task<DeviceInfo> GetDeviceInfoAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(options, cancellationToken)
                .ConfigureAwait(false);
            return await mgmtSession.GetDeviceInfoAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Writes device configuration using a temporary Management session and connection.
        /// </summary>
        /// <remarks>
        ///     The temporary session and connection are disposed on success or failure. Null configuration
        ///     properties preserve settings; application masks and flags replace their sets. Each protected
        ///     write needs the current lock code. Keep borrowed lock-code buffers valid and unchanged through
        ///     the entire call, including session creation, then clear them when no longer needed in a
        ///     <c>finally</c> block. A reboot request is not a factory reset; cancellation does not roll back
        ///     a transmitted configuration. See <see cref="IManagementSession.SetDeviceConfigAsync" /> for
        ///     firmware, validation, ownership, and transport-specific cancellation details, and
        ///     <see cref="CreateManagementSessionAsync" /> for creation failures.
        /// </remarks>
        /// <param name="config">
        ///     The desired device configuration to be applied to the YubiKey.
        /// </param>
        /// <param name="options">Optional reboot request and borrowed current/replacement lock codes.</param>
        /// <param name="sessionOptions">
        ///     Optional cross-cutting settings for creating the temporary management session. These are distinct
        ///     from <paramref name="options" />, which controls the device-configuration operation.
        /// </param>
        /// <param name="cancellationToken">
        ///     A token requesting cancellation of creation and the write at transport-supported boundaries.
        /// </param>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="config" /> is null.</exception>
        /// <exception cref="ArgumentException">A lock code is not 16 bytes, or the transport preference is invalid.</exception>
        /// <exception cref="InvalidOperationException">The USB mask is explicitly None, or initialization cannot determine firmware.</exception>
        /// <exception cref="NotSupportedException">Firmware, transport, or SCP settings are unsupported.</exception>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        /// <exception cref="ApduException">The device rejects a configuration command over SmartCard.</exception>
        public async Task SetDeviceConfigAsync(
            DeviceConfig config,
            SetDeviceConfigOptions? options = null,
            SessionCreationOptions? sessionOptions = null,
            CancellationToken cancellationToken = default)
        {
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(sessionOptions, cancellationToken)
                .ConfigureAwait(false);
            await mgmtSession.SetDeviceConfigAsync(config, options, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Opens one connection and returns an initialized Management session that owns it.
        /// </summary>
        /// <remarks>
        ///     The caller must dispose the returned session, preferably with <c>await using</c>; this also
        ///     closes its connection. Creation failure disposes the connection opened here. In contrast,
        ///     <see cref="ManagementSession.CreateAsync" /> borrows a caller-opened connection.
        ///     Exactly one exposed transport is selected. Connection and initialization failures propagate
        ///     without trying another interface. SCP requires SmartCard; an explicit HID preference with
        ///     SCP is rejected before initialization I/O.
        ///     Firmware probing and a best-effort device-information read precede optional SCP establishment.
        ///     A firmware override affects protocol configuration and feature checks, not discovery or
        ///     reported hardware metadata. See <see cref="ManagementSession.CreateAsync" /> for initialization
        ///     details and <see cref="IManagementSession" /> for operation sequencing and cancellation.
        /// </remarks>
        /// <param name="options">
        ///     Optional creation settings. Management selects SmartCard, HID FIDO, then HID OTP unless
        ///     <see cref="SessionCreationOptions.PreferredConnectionType" /> specifies a supported transport.
        ///     Supplying caller-owned <see cref="ScpKeyParameters" /> forces SmartCard when no preference is specified.
        /// </param>
        /// <param name="cancellationToken">
        ///     A token requesting cancellation of connection opening and initialization at transport-supported boundaries.
        /// </param>
        /// <returns>
        ///     A <see cref="ManagementSession" /> instance configured for the YubiKey device.
        ///     The session must be disposed by the caller when no longer needed.
        /// </returns>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        /// <exception cref="ArgumentException">The preferred connection type is not one supported concrete transport.</exception>
        /// <exception cref="NotSupportedException">The selected transport is not exposed, no supported transport is exposed, or SCP is requested over HID.</exception>
        /// <exception cref="InvalidOperationException">Initialization cannot determine a firmware version.</exception>
        public async Task<ManagementSession> CreateManagementSessionAsync(
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var scpKeyParams = options?.ScpKeyParameters;
            var preferredConnectionType = options?.PreferredConnectionType;
            var transport = yubiKey.ResolveSessionTransport(
                scpKeyParams is not null && preferredConnectionType is null
                    ? ConnectionType.SmartCard
                    : preferredConnectionType,
                "Management",
                ManagementTransportOrder);
            var sessionOptions = (options ?? new SessionCreationOptions())
                .WithPreferredConnectionType(transport);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await ManagementSession
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

    // Management can run over SmartCard or HID. On a physical (possibly multi-connection) device the
    // parameterless ConnectAsync() is ambiguous, so a transport is chosen by an app-specific smart default
    // (SmartCard first/richest, then FIDO HID, then OTP HID) or an explicit caller override. The default
    // order selects exactly one transport. Connection and session-creation failures propagate without
    // trying another interface.
    private static readonly ConnectionType[] ManagementTransportOrder =
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

}
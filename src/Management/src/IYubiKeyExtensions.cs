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

namespace Yubico.YubiKit.Management;

/// <summary>
///     Provides a set of static extension methods for interacting with and managing
///     YubiKeys in the Yubico SDK.
/// </summary>
/// <remarks>
///     This class is intended to extend functionality related to YubiKey management
///     within the Yubico SDK. These methods can simplify operations, enhance
///     interoperability, and provide additional utilities when working with YubiKey devices.
/// </remarks>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        ///     Retrieves device information from a YubiKey asynchronously.
        /// </summary>
        /// <param name="cancellationToken">
        ///     An optional token to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="DeviceInfo" /> structure containing detailed information about the YubiKey device.
        /// </returns>
        public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
        {
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return await mgmtSession.GetDeviceInfoAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Sets the device configuration on a YubiKey asynchronously.
        /// </summary>
        /// <param name="config">
        ///     The desired device configuration to be applied to the YubiKey.
        /// </param>
        /// <param name="reboot">
        ///     A value indicating whether the YubiKey should reboot after applying the configuration.
        /// </param>
        /// <param name="currentLockCode">
        ///     The current lock code for the device, if required.
        /// </param>
        /// <param name="newLockCode">
        ///     An optional new lock code to set for the device.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional token to cancel the operation.
        /// </param>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        public async ValueTask SetDeviceConfigAsync(
            DeviceConfig config,
            bool reboot,
            byte[]? currentLockCode = null,
            byte[]? newLockCode = null,
            CancellationToken cancellationToken = default)
        {
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await mgmtSession.SetDeviceConfigAsync(config, reboot, currentLockCode, newLockCode, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates a management session for interacting with a YubiKey asynchronously.
        ///     The session provides capabilities to perform management operations on the device.
        /// </summary>
        /// <param name="scpKeyParams">
        ///     Optional SCP (Secure Channel Protocol) key parameters necessary to establish
        ///     a secure session with the YubiKey device. SCP requires <see cref="ConnectionType.SmartCard" />.
        ///     Supplying SCP with an explicit <see cref="ConnectionType.HidFido" /> or
        ///     <see cref="ConnectionType.HidOtp" /> override throws <see cref="NotSupportedException" />.
        ///     When SCP is supplied without an override, SmartCard is forced; if CCID is held, creation fails
        ///     rather than falling back to a plaintext HID session.
        /// </param>
        /// <param name="configuration"></param>
        /// <param name="preferredConnection">
        ///     Optional explicit transport override. When <see langword="null" /> (the default), Management
        ///     selects a transport in its documented default order:
        ///     <see cref="ConnectionType.SmartCard" />, then <see cref="ConnectionType.HidFido" />, then
        ///     <see cref="ConnectionType.HidOtp" />. When set, it must be exactly one of those three transports
        ///     and supported by the device; otherwise an <see cref="ArgumentException" /> (invalid transport) or
        ///     <see cref="NotSupportedException" /> (transport not available on this device) is thrown.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional token to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="ManagementSession" /> instance configured for the YubiKey device.
        ///     The session must be disposed by the caller when no longer needed.
        /// </returns>
        /// <exception cref="ConnectionInUseException">The physical YubiKey already has a live connection.</exception>
        public async Task<ManagementSession> CreateManagementSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            ConnectionType? preferredConnection = null,
            CancellationToken cancellationToken = default)
        {
            var transport = yubiKey.ResolveSessionTransport(
                scpKeyParams is not null && preferredConnection is null ? ConnectionType.SmartCard : preferredConnection,
                "Management",
                ManagementTransportOrder);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await ManagementSession
                            .CreateAsync(connection, configuration, scpKeyParams, cancellationToken: ct)
                            .ConfigureAwait(false);

                        // This entry point opened the connection, so the session it returns is the only thing
                        // that can close it. A caller-created connection is never owned this way.
                        session.OwnConnection();
                        return session;
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Source-compatibility overload preserving the pre-Phase-38 positional shape
        ///     (<c>scpKeyParams, configuration, cancellationToken</c>); forwards using the default transport order.
        /// </summary>
        /// <param name="scpKeyParams">Optional SCP key parameters.</param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="cancellationToken">An optional token to cancel the operation.</param>
        /// <returns>A <see cref="ManagementSession" /> the caller must dispose.</returns>
        public Task<ManagementSession> CreateManagementSessionAsync(
            ScpKeyParameters? scpKeyParams,
            ProtocolConfiguration? configuration,
            CancellationToken cancellationToken) =>
            yubiKey.CreateManagementSessionAsync(scpKeyParams, configuration, null, cancellationToken);
    }

    // Management can run over SmartCard or HID. On a physical (possibly multi-connection) device the
    // parameterless ConnectAsync() is ambiguous, so a transport is chosen by an app-specific smart default
    // (SmartCard first/richest, then FIDO HID, then OTP HID) or an explicit caller override. The default
    // order selects exactly one transport. Connection and session-creation failures propagate without
    // trying another interface.
    private static readonly ConnectionType[] ManagementTransportOrder =
        [ConnectionType.SmartCard, ConnectionType.HidFido, ConnectionType.HidOtp];

}
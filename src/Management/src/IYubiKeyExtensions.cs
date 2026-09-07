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
        /// <param name="options">Optional device-configuration policy and borrowed lock-code memory.</param>
        /// <param name="cancellationToken">
        ///     An optional token to cancel the operation.
        /// </param>
        /// <returns>
        ///     A task representing the asynchronous operation.
        /// </returns>
        public async Task SetDeviceConfigAsync(
            DeviceConfig config,
            SetDeviceConfigOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await mgmtSession.SetDeviceConfigAsync(config, options, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates a management session for interacting with a YubiKey asynchronously.
        ///     The session provides capabilities to perform management operations on the device.
        /// </summary>
        /// <param name="options">
        ///     Optional creation settings. Management selects SmartCard, HID FIDO, then HID OTP unless
        ///     <see cref="SessionCreationOptions.PreferredConnectionType" /> specifies a supported transport.
        ///     Secure-channel parameters force SmartCard when no preference is specified.
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
            SessionCreationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var configuration = options?.ProtocolConfiguration;
            var scpKeyParams = options?.ScpKeyParameters;
            var preferredConnectionType = options?.PreferredConnectionType;
            var firmwareVersionOverride = options?.FirmwareVersionOverride;
            var transport = yubiKey.ResolveSessionTransport(
                scpKeyParams is not null && preferredConnectionType is null
                    ? ConnectionType.SmartCard
                    : preferredConnectionType,
                "Management",
                ManagementTransportOrder);

            return await yubiKey.CreateSessionOverTransportAsync(
                    transport,
                    async (connection, ct) =>
                    {
                        var session = await ManagementSession
                            .CreateAsync(
                                connection,
                                new SessionCreationOptions
                                {
                                    ProtocolConfiguration = configuration,
                                    ScpKeyParameters = scpKeyParams,
                                    PreferredConnectionType = transport,
                                    FirmwareVersionOverride = firmwareVersionOverride
                                },
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

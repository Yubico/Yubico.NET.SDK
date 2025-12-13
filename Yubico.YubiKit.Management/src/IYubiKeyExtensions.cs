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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

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
    #region Nested type: <extension>

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
            using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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
            using var mgmtSession = await yubiKey.CreateManagementSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await mgmtSession.SetDeviceConfigAsync(config, reboot, currentLockCode, newLockCode, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Creates a management session for interacting with a YubiKey asynchronously.
        ///     The session provides capabilities to perform management operations on the device.
        /// </summary>
        /// <param name="scpKeyParams">
        ///     Optional SCP (Secure Channel Protocol) key parameters necessary to establish
        ///     a secure session with the YubiKey device.
        /// </param>
        /// <param name="cancellationToken">
        ///     An optional token to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="ManagementSession{TConnection}" /> instance configured for the YubiKey device.
        ///     The session must be disposed by the caller when no longer needed.
        /// </returns>
        public async Task<ManagementSession<ISmartCardConnection>> CreateManagementSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            // Connection is disposed inside session. User must dispose session.
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken).ConfigureAwait(false);
            return await ManagementSession<ISmartCardConnection>.CreateAsync(connection, scpKeyParams: scpKeyParams,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion
}
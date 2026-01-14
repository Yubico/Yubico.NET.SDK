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
using Yubico.YubiKit.Management;

namespace Yubico.YubiKit.Tests.Shared;

/// <summary>
///     Extension methods for <see cref="YubiKeyTestState" /> providing convenient helpers
///     for common test scenarios.
/// </summary>
/// <remarks>
///     These extension methods provide a clean API for working with YubiKey test devices,
///     handling connection management and session lifecycle automatically.
/// </remarks>
public static class YubiKeyTestStateExtensions
{
    extension(YubiKeyTestState state)
    {
        /// <summary>
        ///     Executes an action with a Management session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="state">The test device.</param>
        /// <param name="action">
        ///     Action to execute with the management session and device info.
        /// </param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        ///     <code>
        /// [YubiKeyTheory]
        /// public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestDevice device)
        /// {
        ///     await device.WithManagementAsync(async (mgmt, info) =>
        ///     {
        ///         var deviceInfo = await mgmt.GetDeviceInfoAsync();
        ///         Assert.Equal(info.SerialNumber, deviceInfo.SerialNumber);
        ///     });
        /// }
        ///     </code>
        /// </example>
        public async Task WithManagementAsync(
            Func<ManagementSession, DeviceInfo, Task> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(action);

            using var session = await state.Device
                .CreateManagementSessionAsync(scpKeyParams, configuration, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await action(session, state.DeviceInfo).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes a synchronous action with a Management session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">
        ///     Synchronous action to execute with the management session and device info.
        /// </param>
        /// <param name="configuration"></param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        ///     <code>
        /// [YubiKeyTheory]
        /// public async Task CheckDeviceProperties(YubiKeyTestDevice device)
        /// {
        ///     await device.WithManagementAsync((mgmt, info) =>
        ///     {
        ///         Assert.True(info.SerialNumber > 0);
        ///         Assert.NotEqual(FormFactor.Unknown, info.FormFactor);
        ///     });
        /// }
        ///     </code>
        /// </example>
        public async Task WithManagementAsync(
            Action<ManagementSession, DeviceInfo> action,
            ProtocolConfiguration? configuration = null,
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(action);

            await state.WithManagementAsync(
                (mgmt, info) =>
                {
                    action(mgmt, info);
                    return Task.CompletedTask;
                },
                configuration,
                scpKeyParams,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes an action with a SmartCard connection.
        ///     Automatically handles connection lifecycle.
        /// </summary>
        /// <param name="action">Action to execute with the connection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        ///     <code>
        /// [YubiKeyTheory]
        /// public async Task SendApdu_Success(YubiKeyTestDevice device)
        /// {
        ///     await device.WithConnectionAsync(async connection =>
        ///     {
        ///         var apdu = new CommandApdu(0x00, 0xA4, 0x04, 0x00);
        ///         var response = await connection.TransmitAsync(apdu);
        ///         Assert.True(response.IsSuccess);
        ///     });
        /// }
        ///     </code>
        /// </example>
        public async Task WithConnectionAsync(
            Func<ISmartCardConnection, Task> action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(action);

            await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>(cancellationToken)
                .ConfigureAwait(false);
            await action(connection).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes a synchronous action with a SmartCard connection.
        ///     Automatically handles connection lifecycle.
        /// </summary>
        /// <param name="state">The test device.</param>
        /// <param name="action">Synchronous action to execute with the connection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task WithConnectionAsync(
            Action<ISmartCardConnection> action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(action);

            await state.WithConnectionAsync(connection =>
            {
                action(connection);
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
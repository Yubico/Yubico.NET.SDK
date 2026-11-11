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

using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
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
        /// <param name="action">
        ///     Action to execute with the initialized session and cached <c>state.DeviceInfo</c>.
        ///     The helper does not refresh this metadata; query the session for current persisted settings.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="preferredConnection">
        ///     Pins the session to a specific transport. <c>null</c> uses Management's default order
        ///     (SmartCard, then HID FIDO, then HID OTP).
        ///     <para>
        ///         Pass this whenever a test's meaning depends on WHICH transport it runs over.
        ///         <c>[WithYubiKey(ConnectionType = X)]</c> does NOT pin the transport — it is a device
        ///         filter. A composite key can expose both SmartCard and the requested HID interface.
        ///         Without a pin, Management can select SmartCard; that does not exercise HID.
        ///         Assert <c>mgmt.ConnectionType</c> to verify the interface actually opened.
        ///     </para>
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        ///     <code>
        /// [SkippableTheory]
        /// [WithYubiKey]
        /// public async Task GetDeviceInfo_ReturnsValidData(YubiKeyTestState state)
        /// {
        ///     await state.WithManagementAsync(async (mgmt, info) =>
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
            ConnectionType? preferredConnection = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(state);
            ArgumentNullException.ThrowIfNull(action);

            await using var session = await state.Device
                .CreateManagementSessionAsync(
                    new SessionCreationOptions
                    {
                        ProtocolConfiguration = configuration,
                        ScpKeyParameters = scpKeyParams,
                        PreferredConnectionType = preferredConnection
                    },
                    cancellationToken)
                .ConfigureAwait(false);
            await action(session, state.DeviceInfo).ConfigureAwait(false);
        }

        /// <summary>
        ///     Executes a synchronous action with a Management session.
        ///     Automatically handles connection and session lifecycle.
        /// </summary>
        /// <param name="action">
        ///     Synchronous action to execute with the initialized session and cached <c>state.DeviceInfo</c>.
        /// </param>
        /// <param name="configuration">Optional protocol configuration.</param>
        /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
        /// <param name="preferredConnection">
        ///     Optional concrete transport pin. A device filter alone does not pin the session.
        ///     Assert the session's <c>ConnectionType</c> when transport matters.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        ///     <code>
        /// [SkippableTheory]
        /// [WithYubiKey]
        /// public async Task CheckDeviceProperties(YubiKeyTestState state)
        /// {
        ///     await state.WithManagementAsync((mgmt, info) =>
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
            ConnectionType? preferredConnection = null,
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
                preferredConnection,
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
        /// [SkippableTheory]
        /// [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
        /// public async Task ReadMetadata_WithBorrowedConnection(YubiKeyTestState state)
        /// {
        ///     await state.WithConnectionAsync(async connection =>
        ///     {
        ///         await using var session = await ManagementSession.CreateAsync(connection);
        ///         var info = await session.GetDeviceInfoAsync();
        ///         Assert.Equal(state.SerialNumber, info.SerialNumber);
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
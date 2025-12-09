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
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Tests.Shared;

/// <summary>
///     Test state for Management application integration tests.
///     Provides read-only access to device information and management operations.
/// </summary>
/// <remarks>
///     <para>
///     ManagementTestState is non-destructive - it does not reset the device or modify
///     any configuration. It simply provides a convenient way to execute management
///     operations with proper connection and session management.
///     </para>
///     <para>
///     Since the Management application is read-only, this state doesn't need to
///     set up default credentials or perform initialization beyond caching device info.
///     </para>
/// </remarks>
public class ManagementTestState : TestState
{
    /// <summary>
    ///     Initializes a new instance of <see cref="ManagementTestState"/>.
    /// </summary>
    /// <param name="device">The YubiKey device under test.</param>
    /// <param name="deviceInfo">Device information (firmware, capabilities, etc.).</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    /// <param name="reconnectCallback">Optional callback for device reconnection.</param>
    private ManagementTestState(
        IYubiKey device,
        DeviceInfo deviceInfo,
        ScpKeyParams? scpKeyParams = null,
        Func<Task<IYubiKey>>? reconnectCallback = null)
        : base(device, deviceInfo)
    {
        ScpKeyParams = scpKeyParams;
        ReconnectCallback = reconnectCallback;
    }

    /// <summary>
    ///     Creates a new ManagementTestState asynchronously.
    /// </summary>
    /// <param name="device">The YubiKey device to create state for.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters.</param>
    /// <param name="reconnectCallback">Optional reconnection callback.</param>
    /// <returns>A configured ManagementTestState instance.</returns>
    /// <remarks>
    ///     This factory method retrieves device information and creates the state.
    ///     No destructive operations are performed.
    /// </remarks>
    public static async Task<ManagementTestState> CreateAsync(
        IYubiKey device,
        ScpKeyParams? scpKeyParams = null,
        Func<Task<IYubiKey>>? reconnectCallback = null)
    {
        // Get device information via Management session
        var deviceInfo = await GetDeviceInfoAsync(device, scpKeyParams).ConfigureAwait(false);

        return new ManagementTestState(device, deviceInfo, scpKeyParams, reconnectCallback);
    }

    /// <summary>
    ///     Executes an action with a Management session.
    /// </summary>
    /// <param name="action">The action to execute with the session and state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    ///     <code>
    ///     await state.WithManagementAsync(async (mgmt, state) =>
    ///     {
    ///         var deviceInfo = await mgmt.GetDeviceInfoAsync();
    ///         Assert.NotNull(deviceInfo.SerialNumber);
    ///     });
    ///     </code>
    /// </example>
    public async Task WithManagementAsync(
        Func<ManagementSession<ISmartCardConnection>, ManagementTestState, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var connection = await OpenConnectionAsync<ISmartCardConnection>().ConfigureAwait(false);
        using var session = await ManagementSession<ISmartCardConnection>
            .CreateAsync(connection, scpKeyParams: ScpKeyParams)
            .ConfigureAwait(false);

        await action(session, this).ConfigureAwait(false);
    }

    /// <summary>
    ///     Executes an action with a Management session (synchronous variant).
    /// </summary>
    /// <param name="action">The synchronous action to execute with the session and state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    ///     <code>
    ///     await state.WithManagementAsync((mgmt, state) =>
    ///     {
    ///         // Synchronous test code
    ///         Assert.Equal(FormFactor.UsbAKeychain, state.FormFactor);
    ///     });
    ///     </code>
    /// </example>
    public async Task WithManagementAsync(
        Action<ManagementSession<ISmartCardConnection>, ManagementTestState> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        await WithManagementAsync((session, state) =>
        {
            action(session, state);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    #region Helper Methods

    /// <summary>
    ///     Gets device information from a YubiKey device.
    /// </summary>
    private static async Task<DeviceInfo> GetDeviceInfoAsync(
        IYubiKey device,
        ScpKeyParams? scpKeyParams = null)
    {
        using var connection = await device.ConnectAsync<ISmartCardConnection>().ConfigureAwait(false);
        using var mgmt = await ManagementSession<ISmartCardConnection>
            .CreateAsync(connection, scpKeyParams: scpKeyParams)
            .ConfigureAwait(false);

        return await mgmt.GetDeviceInfoAsync().ConfigureAwait(false);
    }

    #endregion
}
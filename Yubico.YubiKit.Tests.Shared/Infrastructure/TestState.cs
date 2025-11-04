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

namespace Yubico.YubiKit.Tests.Shared.Infrastructure;

/// <summary>
///     Base class for application-specific test state management.
///     Provides common functionality for managing YubiKey device state during integration tests.
/// </summary>
/// <remarks>
///     <para>
///     TestState classes are responsible for:
///     - Ensuring device is in a known, consistent state before tests run
///     - Providing access to application-specific credentials and configuration
///     - Managing device reconnection after destructive operations
///     - Checking device capabilities and FIPS status
///     </para>
///     <para>
///     Derived classes (PivTestState, OathTestState, etc.) implement application-specific
///     setup logic, such as resetting the application and configuring default credentials.
///     </para>
/// </remarks>
public abstract class TestState
{
    /// <summary>
    ///     Gets or sets the YubiKey device under test.
    /// </summary>
    protected IYubiKey CurrentDevice { get; set; }

    /// <summary>
    ///     Gets the device information (firmware version, capabilities, form factor, etc.).
    /// </summary>
    public DeviceInfo DeviceInfo { get; protected init; }

    /// <summary>
    ///     Gets the SCP key parameters if SCP is being used, otherwise null.
    /// </summary>
    public ScpKeyParams? ScpKeyParams { get; protected init; }

    /// <summary>
    ///     Gets the device reconnection callback, used after operations that invalidate connections.
    /// </summary>
    protected Func<Task<IYubiKey>>? ReconnectCallback { get; init; }

    /// <summary>
    ///     Initializes a new instance of <see cref="TestState"/> with the specified device.
    /// </summary>
    /// <param name="device">The YubiKey device to manage.</param>
    /// <param name="deviceInfo">Device information.</param>
    protected TestState(IYubiKey device, DeviceInfo deviceInfo)
    {
        CurrentDevice = device;
        DeviceInfo = deviceInfo;
    }

    #region Helper Methods

    /// <summary>
    ///     Opens a connection of the specified type to the current device.
    /// </summary>
    /// <typeparam name="TConnection">The connection type to open.</typeparam>
    /// <returns>A connection to the device.</returns>
    protected async Task<TConnection> OpenConnectionAsync<TConnection>()
        where TConnection : class, IConnection
    {
        return await CurrentDevice.ConnectAsync<TConnection>().ConfigureAwait(false);
    }

    /// <summary>
    ///     Reconnects to the device after a destructive operation.
    /// </summary>
    /// <remarks>
    ///     Some operations (like application reset) invalidate existing connections.
    ///     This method uses the reconnect callback to re-acquire the device.
    /// </remarks>
    protected async Task ReconnectAsync()
    {
        if (ReconnectCallback is not null)
        {
            CurrentDevice = await ReconnectCallback().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Checks if a specific capability is FIPS capable.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if the capability is FIPS capable; otherwise, false.</returns>
    public bool IsFipsCapable(DeviceCapabilities capability)
    {
        return (DeviceInfo.FipsCapabilities & capability) != 0;
    }

    /// <summary>
    ///     Checks if a specific capability is in FIPS approved mode.
    /// </summary>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if the capability is in FIPS approved mode; otherwise, false.</returns>
    public bool IsFipsApproved(DeviceCapabilities capability)
    {
        return (DeviceInfo.FipsApproved & capability) != 0;
    }

    /// <summary>
    ///     Checks if the device is connected via USB transport.
    /// </summary>
    public bool IsUsbTransport => DeviceInfo.UsbSupported != DeviceCapabilities.None;

    /// <summary>
    ///     Checks if the device is connected via NFC transport.
    /// </summary>
    public bool IsNfcTransport => DeviceInfo.NfcSupported != DeviceCapabilities.None;

    /// <summary>
    ///     Gets the firmware version of the device.
    /// </summary>
    public FirmwareVersion FirmwareVersion => DeviceInfo.FirmwareVersion;

    /// <summary>
    ///     Gets the form factor of the device.
    /// </summary>
    public FormFactor FormFactor => DeviceInfo.FormFactor;

    #endregion
}

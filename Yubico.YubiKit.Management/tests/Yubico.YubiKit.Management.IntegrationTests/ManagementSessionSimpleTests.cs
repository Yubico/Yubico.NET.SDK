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

using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid.Fido;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Management.IntegrationTests;

/// <summary>
///     Integration tests for ManagementSession creation via different connection types and factories.
///     All tests use the YubiKeyTestInfrastructure which enforces AllowList authorization.
/// </summary>
public class ManagementSessionSimpleTests
{
    #region SmartCard Connection Tests

    /// <summary>
    ///     Verify ManagementSession can be created via SmartCard connection using CreateAsync factory.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateManagementSession_WithSmartCardConnection_ReturnsValidSession(YubiKeyTestState state)
    {
        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();
        await using var mgmtSession = await ManagementSession.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    /// <summary>
    ///     Verify ManagementSession can be created via the device extension method.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateManagementSession_WithExtensionMethod_ReturnsValidSession(YubiKeyTestState state)
    {
        await using var mgmtSession = await state.Device.CreateManagementSessionAsync();

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    /// <summary>
    ///     Verify GetDeviceInfoAsync can be called via YubiKey extension method.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetDeviceInfoAsync_WithYubiKeyExtensionMethod_ReturnsValidInfo(YubiKeyTestState state)
    {
        var deviceInfo = await state.Device.GetDeviceInfoAsync();

        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    #endregion

    #region HID Connection Tests

    /// <summary>
    ///     Verify ManagementSession can be created via HID FIDO connection.
    ///     Management over HID requires the FIDO interface (UsagePage 0xF1D0).
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task CreateManagementSession_WithHidFidoConnection_ReturnsValidSession(YubiKeyTestState state)
    {
        await using var connection = await state.Device.ConnectAsync<IFidoHidConnection>();
        await using var mgmtSession = await ManagementSession.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    /// <summary>
    ///     Verify device info can be retrieved directly from HID FIDO device.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidFido)]
    public async Task GetDeviceInfo_WithHidFidoDevice_ReturnsValidInfo(YubiKeyTestState state)
    {
        Assert.Equal(ConnectionType.HidFido, state.ConnectionType);

        var deviceInfo = await state.Device.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    /// <summary>
    ///     Verify device info can be retrieved from HID OTP device.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.HidOtp)]
    public async Task GetDeviceInfo_WithHidOtpDevice_ReturnsValidInfo(YubiKeyTestState state)
    {
        Assert.Equal(ConnectionType.HidOtp, state.ConnectionType);

        var deviceInfo = await state.Device.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    #endregion

    #region Device Configuration Tests

    /// <summary>
    ///     Verify SetDeviceConfigAsync works via ManagementSession.
    ///     Tests changing and restoring AutoEjectTimeout to verify config changes are applied.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task SetDeviceConfigAsync_WithManagementSession_AppliesAndRestoresConfig(YubiKeyTestState state)
    {
        await state.WithManagementAsync(async (mgmt, deviceInfo) =>
        {
            var originalAutoEject = deviceInfo.AutoEjectTimeout;
            var newAutoEject = originalAutoEject == 0 ? (ushort)10 : (ushort)0;

            var newConfig = DeviceConfig.CreateBuilder()
                .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                .WithAutoEjectTimeout(newAutoEject)
                .Build();

            await mgmt.SetDeviceConfigAsync(newConfig, false);

            var updatedInfo = await mgmt.GetDeviceInfoAsync();
            Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);

            // Restore original setting
            var restoreConfig = DeviceConfig.CreateBuilder()
                .WithCapabilities(Transport.Usb, (int)deviceInfo.UsbEnabled)
                .WithAutoEjectTimeout(originalAutoEject)
                .Build();

            await mgmt.SetDeviceConfigAsync(restoreConfig, false);
        });
    }

    /// <summary>
    ///     Verify SetDeviceConfigAsync works via YubiKey extension method.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.0.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task SetDeviceConfigAsync_WithExtensionMethod_AppliesAndRestoresConfig(YubiKeyTestState state)
    {
        var device = state.Device;
        var originalInfo = await device.GetDeviceInfoAsync();
        var originalAutoEject = originalInfo.AutoEjectTimeout;
        var newAutoEject = originalAutoEject == 0 ? (ushort)10 : (ushort)0;

        var newConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)originalInfo.UsbEnabled)
            .WithAutoEjectTimeout(newAutoEject)
            .Build();

        await device.SetDeviceConfigAsync(newConfig, false);

        var updatedInfo = await device.GetDeviceInfoAsync();
        Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);

        // Restore original setting
        var restoreConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)originalInfo.UsbEnabled)
            .WithAutoEjectTimeout(originalAutoEject)
            .Build();

        await device.SetDeviceConfigAsync(restoreConfig, false);
    }

    #endregion

    #region SCP03 Tests

    /// <summary>
    ///     Verify ManagementSession can be created with SCP03 using default keys.
    ///     Requires a YubiKey with default SCP03 keys configured (KVN 0xFF).
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateManagementSession_WithScp03DefaultKeys_CommunicatesSecurely(YubiKeyTestState state)
    {
        // Default SCP03 keys: 0x404142434445464748494A4B4C4D4E4F
        using var scpKeyParams = Scp03KeyParameters.Default;

        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();

        // Create ManagementSession with SCP03 enabled
        await using var mgmtSession = await ManagementSession.CreateAsync(
            connection,
            scpKeyParams: scpKeyParams);

        // Verify we can communicate over SCP by getting device info
        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.Equal(state.SerialNumber, deviceInfo.SerialNumber);
    }

    /// <summary>
    ///     Verify SCP03 authentication fails with incorrect keys.
    /// </summary>
    [SkippableTheory]
    [WithYubiKey(MinFirmware = "5.3.0", ConnectionType = ConnectionType.SmartCard)]
    public async Task CreateManagementSession_WithScp03WrongKeys_ThrowsException(YubiKeyTestState state)
    {
        // Create SCP03 key parameters with intentionally wrong keys
        var wrongKeyBytes = new byte[16];
        for (var i = 0; i < 16; i++)
            wrongKeyBytes[i] = (byte)(0xFF - i); // Different from default

        using var staticKeys = new StaticKeys(wrongKeyBytes, wrongKeyBytes, wrongKeyBytes);
        var keyRef = new KeyReference(0x01, 0xFF);
        var scpKeyParams = new Scp03KeyParameters(keyRef, staticKeys);

        await using var connection = await state.Device.ConnectAsync<ISmartCardConnection>();

        // Attempt to create ManagementSession with wrong SCP keys should throw
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await using var mgmtSession = await ManagementSession.CreateAsync(
                connection,
                scpKeyParams: scpKeyParams);
        });
    }

    #endregion

    #region Tests Without Attributes (using AuthorizedDevices directly)

    /// <summary>
    ///     Demonstrates using AuthorizedDevices directly without [WithYubiKey] attribute.
    ///     This pattern is useful for tests that need custom device selection logic.
    /// </summary>
    [SkippableFact]
    public async Task CreateManagementSession_UsingAuthorizedDevicesDirectly_WorksWithoutAttributes()
    {
        // Get the first authorized SmartCard device directly (no attribute needed)
        var device = AuthorizedDevices
            .GetByConnectionType(ConnectionType.SmartCard)
            .FirstOrDefault();

        Skip.If(device is null, "No SmartCard device available");

        await using var mgmtSession = await device.Device.CreateManagementSessionAsync();
        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();

        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.True(AuthorizedDevices.IsAllowed(deviceInfo.SerialNumber));
    }

    /// <summary>
    ///     Demonstrates filtering with FilterCriteria directly.
    /// </summary>
    [SkippableFact]
    public async Task GetDeviceInfo_UsingFilterCriteria_AppliesFiltersCorrectly()
    {
        // Build custom filter criteria
        var criteria = new FilterCriteria
        {
            MinFirmware = "5.0.0",
            ConnectionType = ConnectionType.SmartCard
        };

        // Get first device matching criteria or skip
        var device = AuthorizedDevices.GetFirstOrSkip(criteria);

        var deviceInfo = await device.Device.GetDeviceInfoAsync();

        Assert.NotEqual(0, deviceInfo.SerialNumber);
        Assert.True(deviceInfo.FirmwareVersion.IsAtLeast(5, 0, 0));
    }

    /// <summary>
    ///     Demonstrates iterating over all authorized devices with specific capabilities.
    /// </summary>
    [SkippableFact]
    public async Task GetDeviceInfo_ForAllAuthorizedDevices_ValidatesAllowList()
    {
        var devices = AuthorizedDevices.GetAll();
        Skip.If(devices.Count == 0, "No authorized devices available");

        foreach (var device in devices)
        {
            // Every device from AuthorizedDevices is guaranteed to be in AllowList
            Assert.True(AuthorizedDevices.IsAllowed(device.SerialNumber));

            var deviceInfo = await device.Device.GetDeviceInfoAsync();
            Assert.Equal(device.SerialNumber, deviceInfo.SerialNumber);
        }
    }

    #endregion
}






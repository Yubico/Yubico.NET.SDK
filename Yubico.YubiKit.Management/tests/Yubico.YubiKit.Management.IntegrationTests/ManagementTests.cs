using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management.IntegrationTests;

public class ManagementTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateManagementSession_with_Constructor()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);

        using var connection = await device.ConnectAsync<ISmartCardConnection>();
        using var mgmtSession = await ManagementSession<ISmartCardConnection>.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_FactoryInstance()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.First();
        var sessionFactory = ServiceProvider.GetRequiredService<IManagementSessionFactory<ISmartCardConnection>>();

        using var connection = await device.ConnectAsync<ISmartCardConnection>();
        using var mgmtSession = await sessionFactory.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_FactoryMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.First();

        using var connection = await device.ConnectAsync<ISmartCardConnection>();
        using var mgmtSession = await ManagementSession<ISmartCardConnection>.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_ExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.First();

        using var mgmtSession = await device.CreateManagementSessionAsync();

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_with_YubiKeyExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);

        var deviceInfo = await device.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    private async Task SetDeviceConfigAsync_with_ManagementSession()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.First();

        using var mgmtSession = await device.CreateManagementSessionAsync();

        var originalInfo = await mgmtSession.GetDeviceInfoAsync();
        var originalAutoEject = originalInfo.AutoEjectTimeout;
        var newAutoEject = originalAutoEject == 0 ? (ushort)10 : (ushort)0;

        var newConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(newAutoEject)
            .Build();

        await mgmtSession.SetDeviceConfigAsync(newConfig, false);

        var updatedInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);

        // Restore original setting
        var restoreConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(originalAutoEject)
            .Build();

        await mgmtSession.SetDeviceConfigAsync(restoreConfig, false);
    }

    [Fact]
    public async Task SetDeviceConfigAsync_with_YubiKeyExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.First();

        var originalInfo = await device.GetDeviceInfoAsync();
        var originalAutoEject = originalInfo.AutoEjectTimeout;
        var newAutoEject = originalAutoEject == 0 ? (ushort)10 : (ushort)0;

        var newConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(newAutoEject)
            .Build();

        await device.SetDeviceConfigAsync(newConfig, false);

        var updatedInfo = await device.GetDeviceInfoAsync();
        Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);

        // Restore original setting
        var restoreConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(originalAutoEject)
            .Build();

        await device.SetDeviceConfigAsync(restoreConfig, false);
    }

    [Fact]
    public async Task CreateManagementSession_with_SCP03_DefaultKeys()
    {
        // This test requires a YubiKey with default SCP03 keys configured (KVN 0xFF)
        // Skip this test if no suitable YubiKey is available
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.FirstOrDefault();

        if (device == null)
            // Skip test if no device is found
            return;

        // Create SCP03 key parameters using default keys
        // Default SCP03 keys: 0x404142434445464748494A4B4C4D4E4F
        using var staticKeys = StaticKeys.GetDefaultKeys();
        var keyRef = KeyRef.Default;
        var scpKeyParams = new Scp03KeyParams(keyRef, staticKeys);

        using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Create ManagementSession with SCP03 enabled
        using var mgmtSession = await ManagementSession<ISmartCardConnection>.CreateAsync(
            connection,
            scpKeyParams: scpKeyParams);

        // Verify we can communicate over SCP by getting device info
        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_SCP03_WrongKeys_ShouldFail()
    {
        // This test verifies that SCP authentication fails with wrong keys
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.FirstOrDefault();

        if (device == null)
            // Skip test if no device is found
            return;

        // Create SCP03 key parameters with intentionally wrong keys
        var wrongKeyBytes = new byte[16];
        for (var i = 0; i < 16; i++) wrongKeyBytes[i] = (byte)(0xFF - i); // Different from default

        using var staticKeys = new StaticKeys(wrongKeyBytes, wrongKeyBytes, wrongKeyBytes);
        var keyRef = new KeyRef(0x01, 0xFF);
        var scpKeyParams = new Scp03KeyParams(keyRef, staticKeys);

        using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Attempt to create ManagementSession with wrong SCP keys should throw
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var mgmtSession = await ManagementSession<ISmartCardConnection>.CreateAsync(
                connection,
                scpKeyParams: scpKeyParams);
        });
    }
}
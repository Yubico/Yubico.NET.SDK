using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.SmartCard;

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
            .WithCapabilities(Core.YubiKey.Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(newAutoEject)
            .Build();

        await mgmtSession.SetDeviceConfigAsync(newConfig, false);

        var updatedInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);

        // Restore original setting
        var restoreConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Core.YubiKey.Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
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
            .WithCapabilities(Core.YubiKey.Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(newAutoEject)
            .Build();

        await device.SetDeviceConfigAsync(newConfig, false);

        var updatedInfo = await device.GetDeviceInfoAsync();
        Assert.Equal(newAutoEject, updatedInfo.AutoEjectTimeout);

        // Restore original setting
        var restoreConfig = DeviceConfig.CreateBuilder()
            .WithCapabilities(Core.YubiKey.Transport.Usb, (int)DeviceCapabilities.All) // TODO Whats a good default value here?
            .WithAutoEjectTimeout(originalAutoEject)
            .Build();

        await device.SetDeviceConfigAsync(restoreConfig, false);
    }
}
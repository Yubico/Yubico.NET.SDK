using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.Core.Connections;

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
    public async Task GetDeviceInfoAsync_with_YubiKeyExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync();
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);

        var deviceInfo = await device.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }
}
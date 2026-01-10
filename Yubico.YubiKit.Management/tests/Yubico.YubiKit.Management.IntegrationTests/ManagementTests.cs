using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Hid;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Management.IntegrationTests;

public class
    ManagementTests : IntegrationTestBase // TODO This test class is dangerous,  it can do stuff on your private YubiKey, no test filter
{
    [Fact]
    public async Task CreateManagementSession_with_CreateAsync()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices.FirstOrDefault();
        Assert.NotNull(device);

        using var connection = await device.ConnectAsync<ISmartCardConnection>();
        using var mgmtSession = await ManagementSession.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_Hid_CreateAsync()
    {
        // Management over HID requires the FIDO interface (UsagePage 0xF1D0)
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);

        // Filter for FIDO devices - DeviceId contains FIDO usage
        var fidoDevice =
            devices.FirstOrDefault(d =>
                d.DeviceId.Contains(":0001") || d.DeviceId.Contains(":F1D0")); // TODO refactor, bad pattern

        if (fidoDevice is null)
            // Skip test if no FIDO HID interface found
            return;

        await using var connection = await fidoDevice.ConnectAsync<IFidoConnection>();
        using var mgmtSession = await ManagementSession.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_Hid_with_CreateAsync()
    {
        // Management over HID requires the FIDO interface (UsagePage 0xF1D0)
        // OTP/Keyboard interface does not support Management application
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Hid);

        // Filter for FIDO devices only - DeviceId format is "hid:VID:PID:USAGE"
        // FIDO usage page 0xF1D0, typical usage is 0x0001 -> "hid:1050:XXXX:0001"
        var fidoDevice = devices.FirstOrDefault(d => d.DeviceId.Contains(":0001") || d.DeviceId.Contains(":F1D0"));

        if (fidoDevice is null)
            // Skip test if no FIDO HID interface found
            return;

        await using var connection = await fidoDevice.ConnectAsync<IFidoConnection>();
        using var mgmtSession = await ManagementSession.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_FactoryInstance()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices.First();
        var sessionFactory = ServiceProvider.GetRequiredService<ManagementSessionFactoryDelegate>();

        using var connection = await device.ConnectAsync<ISmartCardConnection>();
        using var mgmtSession = await sessionFactory(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_FactoryMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices.First();

        using var connection = await device.ConnectAsync<ISmartCardConnection>();
        using var mgmtSession = await ManagementSession.CreateAsync(connection);

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task CreateManagementSession_with_ExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices[0];

        using var mgmtSession = await device.CreateManagementSessionAsync();

        var deviceInfo = await mgmtSession.GetDeviceInfoAsync();
        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    public async Task GetDeviceInfoAsync_with_YubiKeyExtensionMethod()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var deviceInfo = await devices[0]!.GetDeviceInfoAsync();

        Assert.NotEqual(0, deviceInfo.SerialNumber);
    }

    [Fact]
    private async Task SetDeviceConfigAsync_with_ManagementSession()
    {
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices[0];

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
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices[0];

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
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices[0];

        // Create SCP03 key parameters using default keys
        // Default SCP03 keys: 0x404142434445464748494A4B4C4D4E4F
        using var scpKeyParams = Scp03KeyParameters.Default;

        await using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Create ManagementSession with SCP03 enabled
        using var mgmtSession = await ManagementSession.CreateAsync(
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
        var devices = await YubiKeyManager.FindAllAsync(ConnectionType.Ccid);
        var device = devices.FirstOrDefault();

        if (device == null)
            // Skip test if no device is found
            return;

        // Create SCP03 key parameters with intentionally wrong keys
        var wrongKeyBytes = new byte[16];
        for (var i = 0; i < 16; i++) wrongKeyBytes[i] = (byte)(0xFF - i); // Different from default

        using var staticKeys = new StaticKeys(wrongKeyBytes, wrongKeyBytes, wrongKeyBytes);
        var keyRef = new KeyReference(0x01, 0xFF);
        var scpKeyParams = new Scp03KeyParameters(keyRef, staticKeys);

        using var connection = await device.ConnectAsync<ISmartCardConnection>();

        // Attempt to create ManagementSession with wrong SCP keys should throw
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var mgmtSession = await ManagementSession.CreateAsync(
                connection,
                scpKeyParams: scpKeyParams);
        });
    }
}
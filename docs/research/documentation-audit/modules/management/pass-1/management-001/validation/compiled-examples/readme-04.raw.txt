var info = await device.GetDeviceInfoAsync();
var requested = DeviceCapabilities.Piv | DeviceCapabilities.Oath;
if ((info.UsbSupported & requested) != requested)
{
    throw new NotSupportedException("This key does not support both requested USB applications.");
}
var config = new DeviceConfig { UsbEnabled = requested };

await using (var session = await device.CreateManagementSessionAsync())
{
    await session.SetDeviceConfigAsync(config, new SetDeviceConfigOptions { Reboot = true });
}

// The session above is disposed. This pause is illustrative, not an enumeration guarantee.
await Task.Delay(TimeSpan.FromSeconds(3));
var rebooted = await YubiKeyManager.FindAllAsync(forceRescan: true);

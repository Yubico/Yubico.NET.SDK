try
{
    await device.SetDeviceConfigAsync(
        new DeviceConfig(),
        new SetDeviceConfigOptions
        {
            CurrentLockCode = currentLockCode,
            NewLockCode = new byte[16]
        });
}
finally
{
    CryptographicOperations.ZeroMemory(currentLockCode);
}

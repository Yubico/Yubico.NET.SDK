await using var session = await device.CreateManagementSessionAsync();
byte[] lockCode = RandomNumberGenerator.GetBytes(16);
try
{
    // Persist lockCode in your secret store first. It cannot be recovered.
    await session.SetDeviceConfigAsync(
        new DeviceConfig(),
        new SetDeviceConfigOptions { NewLockCode = lockCode });
}
finally
{
    CryptographicOperations.ZeroMemory(lockCode);
}

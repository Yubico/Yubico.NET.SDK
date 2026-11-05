await using var session = await device.CreatePivSessionAsync();

// Evaluation only: this public factory value must match the key currently installed.
byte[] managementKey = PivSession.DefaultManagementKey.ToArray();
try
{
    await session.AuthenticateAsync(managementKey);
    var keyOptions = new KeyCreationOptions { PinPolicy = PinPolicy.Once, TouchPolicy = TouchPolicy.Never };
    IPublicKey publicKey = await session.GenerateKeyAsync(Slot.Authentication, KeyType.P256, keyOptions);
}
finally
{
    CryptographicOperations.ZeroMemory(managementKey);
}

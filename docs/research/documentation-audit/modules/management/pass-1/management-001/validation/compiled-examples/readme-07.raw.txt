await using var session = await device.CreateManagementSessionAsync(
    new SessionCreationOptions { PreferredConnectionType = ConnectionType.SmartCard });

// Destructive: the caller has confirmed Bio MPE applicability and application-data erasure.
await session.ResetDeviceAsync();

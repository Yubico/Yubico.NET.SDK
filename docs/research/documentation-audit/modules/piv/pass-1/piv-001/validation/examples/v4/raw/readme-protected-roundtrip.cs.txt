static async Task ProtectedAdministrationRoundTripAsync(
    IYubiKey device,
    ReadOnlyMemory<byte> pin,
    ReadOnlyMemory<byte> activeManagementKey,
    CancellationToken cancellationToken)
{
    await using (var setup = await device.CreatePivSessionAsync(cancellationToken: cancellationToken))
    {
        await setup.AuthenticateAsync(activeManagementKey, cancellationToken);
        await setup.SetPinOnlyModeAsync(PinOnlyMode.PinProtected, pin, activeManagementKey, cancellationToken);
    }

    await using (var reopened = await device.CreatePivSessionAsync(cancellationToken: cancellationToken))
    {
        PinOnlyMode detected = await reopened.GetPinOnlyModeAsync(cancellationToken);
        if (!detected.HasFlag(PinOnlyMode.PinProtected))
            throw new InvalidOperationException("Inspect ADMIN DATA before continuing.");
        PinOnlyMode recovered = await reopened.RecoverPinOnlyModeAsync(pin, cancellationToken);
        if (!recovered.HasFlag(PinOnlyMode.PinProtected) || !reopened.IsManagementKeyAuthenticated)
            throw new InvalidOperationException("Use the backed-up active management key to recover access.");

        // Perform authorized administration here, then restore the type-appropriate public default.
        await reopened.SetPinOnlyModeAsync(PinOnlyMode.None, pin, cancellationToken: cancellationToken);
    }
}

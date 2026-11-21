// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class ChangeManagementKey
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        // Arrange protected, recoverable storage of the replacement before this diagnostic operation.
        // Store the replacement recoverably before installing it.
        using var currentKey = OutputHelpers.ReadKey("Current management key (16 bytes, hex)", 16, cancellationToken);
        if (currentKey is null)
            return;

        using var newKey = OutputHelpers.ReadKey("New management key (16 bytes, hex)", 16, cancellationToken);
        if (newKey is null)
            return;

        await session.PutManagementKeyAsync(currentKey.Memory, newKey.Memory, cancellationToken);
        OutputHelpers.WriteSuccess("Management key changed successfully.");
        OutputHelpers.WriteWarning("Store the new key securely. Losing it will require a factory reset.");
    }
}
// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class GenerateAsymmetricCredential
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var label = AnsiConsole.Ask<string>("Credential [green]label[/]:");
        using var credentialPassword = OutputHelpers.ReadPassword("Credential password", cancellationToken);
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return;
        }

        using var password = CredentialPassword.FromUtf8(credentialPassword.Memory.Span);

        using var managementKey = OutputHelpers.ReadKey("Management key (16 bytes, hex)", 16, cancellationToken);
        if (managementKey is null)
            return;

        var touchRequired = AnsiConsole.Confirm("Require touch?", defaultValue: false);

        await session.GenerateCredentialAsymmetricAsync(
            managementKey.Memory,
            label,
            password,
            touchRequired,
            cancellationToken);

        OutputHelpers.WriteSuccess($"Asymmetric credential '{label}' generated on device.");
        OutputHelpers.WriteInfo("Private key was generated on-device and never leaves the YubiKey.");

        // Export the credential's PK-OCE for enrollment in the HSM authentication object.
        // Obtain the HSM's trusted static public key separately for asymmetric calculation.
        var publicKey = await session.GetPublicKeyAsync(label, cancellationToken);
        OutputHelpers.WriteHex("Public key", publicKey.Span);
    }
}
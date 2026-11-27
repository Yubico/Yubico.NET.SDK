// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

/// <summary>
///     Stores keys on the YubiKey for diagnostic evaluation. Enroll the identical K-ENC/K-MAC pair
///     in the intended HSM authentication object through the separate provisioning workflow.
/// </summary>
public static class AddSymmetricCredential
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

        var useRandomKeys = AnsiConsole.Confirm("Generate random keys?", defaultValue: true);

        using var keyEncInput = useRandomKeys ? null : OutputHelpers.ReadKey("Encryption key (16 bytes, hex)", 16, cancellationToken);
        using var keyMacInput = useRandomKeys ? null : OutputHelpers.ReadKey("MAC key (16 bytes, hex)", 16, cancellationToken);
        if (!useRandomKeys && (keyEncInput is null || keyMacInput is null))
            return;

        byte[]? keyEnc = null;
        byte[]? keyMac = null;

        if (useRandomKeys)
        {
            keyEnc = RandomNumberGenerator.GetBytes(16);
            keyMac = RandomNumberGenerator.GetBytes(16);

            // Diagnostic output exposes long-lived authentication keys. Use disposable test material;
            // a deployed application's provisioning layer should protect any retained recovery copy.
            OutputHelpers.WriteHex("K-ENC (generated, demo only)", keyEnc);
            OutputHelpers.WriteHex("K-MAC (generated, demo only)", keyMac);
        }
        var touchRequired = AnsiConsole.Confirm("Require touch?", defaultValue: false);


        try
        {
            await session.PutCredentialSymmetricAsync(
                managementKey.Memory,
                label,
                keyEncInput is null ? keyEnc : keyEncInput.Memory,
                keyMacInput is null ? keyMac : keyMacInput.Memory,
                password,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess($"Symmetric credential '{label}' stored successfully.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyEnc);
            CryptographicOperations.ZeroMemory(keyMac);
        }
    }
}
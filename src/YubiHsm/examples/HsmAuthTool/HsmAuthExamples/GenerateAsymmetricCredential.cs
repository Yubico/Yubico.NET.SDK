// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using SecureCredential = Yubico.YubiKit.Cli.Shared.Output.SecureCredential;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class GenerateAsymmetricCredential
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var label = AnsiConsole.Ask<string>("Credential [green]label[/]:");
        using var credentialPassword = SecureCredential.Prompt("Credential password");
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return;
        }

        var mgmtKeyHex = AnsiConsole.Prompt(
            new TextPrompt<string>("Management key ([grey]hex, 16 bytes[/]):")
                .DefaultValue("00000000000000000000000000000000"));

        var touchRequired = AnsiConsole.Confirm("Require touch?", defaultValue: false);

        var managementKey = Convert.FromHexString(mgmtKeyHex);
        try
        {
            await session.GenerateCredentialAsymmetricAsync(
                managementKey,
                label,
                credentialPassword.Memory,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess($"Asymmetric credential '{label}' generated on device.");
            OutputHelpers.WriteInfo("Private key was generated on-device and never leaves the YubiKey.");

            var publicKey = await session.GetPublicKeyAsync(label, cancellationToken);
            OutputHelpers.WriteHex("Public key", publicKey.Span);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(managementKey);
        }
    }
}
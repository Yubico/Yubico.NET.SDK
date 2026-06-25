// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
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
        var credentialPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Credential [green]password[/]:")
                .Secret());

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
                credentialPassword,
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

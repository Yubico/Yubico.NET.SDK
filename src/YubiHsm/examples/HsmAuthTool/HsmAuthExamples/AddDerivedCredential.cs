// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class AddDerivedCredential
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var label = AnsiConsole.Ask<string>("Credential [green]label[/]:");
        var derivationPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Derivation [green]password[/] (used to derive K-ENC/K-MAC via PBKDF2):")
                .Secret());
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
            await session.PutCredentialDerivedAsync(
                managementKey,
                label,
                derivationPassword,
                credentialPassword,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess($"Derived credential '{label}' stored successfully.");
            OutputHelpers.WriteInfo("Keys were derived using PBKDF2-HMAC-SHA256 (10,000 iterations, salt='Yubico').");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(managementKey);
        }
    }
}

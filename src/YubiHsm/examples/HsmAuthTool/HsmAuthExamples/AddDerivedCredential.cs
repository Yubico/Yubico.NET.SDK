// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using SecureCredential = Yubico.YubiKit.Cli.Shared.Output.SecureCredential;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class AddDerivedCredential
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var label = AnsiConsole.Ask<string>("Credential [green]label[/]:");
        using var derivationPassword = SecureCredential.Prompt(
            "Derivation password (used to derive K-ENC/K-MAC via PBKDF2)");
        if (derivationPassword is null)
        {
            OutputHelpers.WriteError("Derivation password is required.");
            return;
        }
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
            await session.PutCredentialDerivedAsync(
                managementKey,
                label,
                derivationPassword.Memory,
                credentialPassword.Memory,
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
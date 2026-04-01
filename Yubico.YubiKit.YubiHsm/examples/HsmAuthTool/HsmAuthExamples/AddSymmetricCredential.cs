// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class AddSymmetricCredential
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

        var useRandomKeys = AnsiConsole.Confirm("Generate random keys?", defaultValue: true);

        byte[] keyEnc;
        byte[] keyMac;

        if (useRandomKeys)
        {
            keyEnc = RandomNumberGenerator.GetBytes(16);
            keyMac = RandomNumberGenerator.GetBytes(16);
            OutputHelpers.WriteHex("K-ENC (generated)", keyEnc);
            OutputHelpers.WriteHex("K-MAC (generated)", keyMac);
        }
        else
        {
            var keyEncHex = AnsiConsole.Ask<string>("Encryption key ([grey]hex, 16 bytes[/]):");
            var keyMacHex = AnsiConsole.Ask<string>("MAC key ([grey]hex, 16 bytes[/]):");
            keyEnc = Convert.FromHexString(keyEncHex);
            keyMac = Convert.FromHexString(keyMacHex);
        }

        var touchRequired = AnsiConsole.Confirm("Require touch?", defaultValue: false);

        var managementKey = Convert.FromHexString(mgmtKeyHex);

        try
        {
            await session.PutCredentialSymmetricAsync(
                managementKey,
                label,
                keyEnc,
                keyMac,
                credentialPassword,
                touchRequired,
                cancellationToken);

            OutputHelpers.WriteSuccess($"Symmetric credential '{label}' stored successfully.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(managementKey);
            CryptographicOperations.ZeroMemory(keyEnc);
            CryptographicOperations.ZeroMemory(keyMac);
        }
    }
}

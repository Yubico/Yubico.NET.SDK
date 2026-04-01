// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class ChangeManagementKey
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var currentKeyHex = AnsiConsole.Prompt(
            new TextPrompt<string>("Current management key ([grey]hex, 16 bytes[/]):")
                .DefaultValue("00000000000000000000000000000000"));

        var newKeyHex = AnsiConsole.Ask<string>("New management key ([grey]hex, 16 bytes[/]):");

        var currentKey = Convert.FromHexString(currentKeyHex);
        var newKey = Convert.FromHexString(newKeyHex);

        try
        {
            await session.PutManagementKeyAsync(currentKey, newKey, cancellationToken);
            OutputHelpers.WriteSuccess("Management key changed successfully.");
            OutputHelpers.WriteWarning("Store the new key securely. Losing it will require a factory reset.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentKey);
            CryptographicOperations.ZeroMemory(newKey);
        }
    }
}

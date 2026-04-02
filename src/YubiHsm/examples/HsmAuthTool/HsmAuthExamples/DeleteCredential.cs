// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class DeleteCredential
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        // Show existing credentials first
        var credentials = await session.ListCredentialsAsync(cancellationToken);
        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials to delete.");
            return;
        }

        var labels = credentials
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Label)
            .ToList();

        var label = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select credential to [red]delete[/]:")
                .AddChoices(labels));

        var mgmtKeyHex = AnsiConsole.Prompt(
            new TextPrompt<string>("Management key ([grey]hex, 16 bytes[/]):")
                .DefaultValue("00000000000000000000000000000000"));

        if (!AnsiConsole.Confirm($"Delete credential '{label}'?", defaultValue: false))
        {
            OutputHelpers.WriteInfo("Cancelled.");
            return;
        }

        var managementKey = Convert.FromHexString(mgmtKeyHex);
        try
        {
            await session.DeleteCredentialAsync(managementKey, label, cancellationToken);
            OutputHelpers.WriteSuccess($"Credential '{label}' deleted.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(managementKey);
        }
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class CalculateSessionKeys
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        // Show existing credentials first
        var credentials = await session.ListCredentialsAsync(cancellationToken);
        if (credentials.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials available for session key calculation.");
            return;
        }

        var labels = credentials
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Label)
            .ToList();

        var label = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select credential for session key calculation:")
                .AddChoices(labels));

        var credentialPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Credential [green]password[/]:")
                .Secret());

        // Generate random context (host challenge + HSM challenge)
        var context = RandomNumberGenerator.GetBytes(32);
        OutputHelpers.WriteHex("Context (host[16] + HSM[16])", context);

        using var keys = await session.CalculateSessionKeysSymmetricAsync(
            label,
            context,
            credentialPassword,
            cancellationToken: cancellationToken);

        AnsiConsole.WriteLine();
        OutputHelpers.WriteSuccess("Session keys calculated successfully.");

        // SECURITY NOTE: Session keys displayed for developer diagnostics only.
        // Never display session key material in production applications.
        OutputHelpers.WriteHex("S-ENC", keys.SEnc);
        OutputHelpers.WriteHex("S-MAC", keys.SMac);
        OutputHelpers.WriteHex("S-RMAC", keys.SRmac);

        CryptographicOperations.ZeroMemory(context);
    }
}

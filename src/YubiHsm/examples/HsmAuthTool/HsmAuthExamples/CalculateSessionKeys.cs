// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class CalculateSessionKeys
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        var credentials = await session.ListCredentialsAsync(cancellationToken);
        var labels = credentials
            .Where(c => c.Algorithm == HsmAuthAlgorithm.Aes128YubicoAuthentication)
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Label)
            .ToList();
        if (labels.Count == 0)
        {
            OutputHelpers.WriteInfo("No symmetric credentials available for session key calculation.");
            return;
        }

        var label = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a symmetric credential for session key calculation:")
                .AddChoices(labels));

        using var credentialPassword = OutputHelpers.ReadPassword("Credential password", cancellationToken);
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return;
        }

        using var password = CredentialPassword.FromUtf8(credentialPassword.Memory.Span);

        var hostChallengeHex = AnsiConsole.Ask<string>(
            "Host challenge from the YubiKey or caller ([grey]hex, 8 bytes[/]):");
        var hsmChallengeHex = AnsiConsole.Ask<string>(
            "HSM challenge from the connector ([grey]hex, 8 bytes[/]):");
        var cardCryptogramHex = AnsiConsole.Prompt(
            new TextPrompt<string>("Card cryptogram from the connector ([grey]hex, optional[/]):")
                .AllowEmpty());

        byte[]? hostChallenge = null;
        byte[]? hsmChallenge = null;
        byte[]? cardCryptogram = null;
        var context = new byte[16];
        try
        {
            hostChallenge = Convert.FromHexString(hostChallengeHex);
            hsmChallenge = Convert.FromHexString(hsmChallengeHex);
            cardCryptogram = string.IsNullOrWhiteSpace(cardCryptogramHex)
                ? null
                : Convert.FromHexString(cardCryptogramHex);

            if (hostChallenge.Length != 8 || hsmChallenge.Length != 8)
            {
                OutputHelpers.WriteError("Host and HSM challenges must each be 8 bytes.");
                return;
            }

            // Use the host value sent to the HSM followed by its actual returned challenge.
            // Omitting the optional cryptogram skips applet verification; the external HSM library
            // remains responsible for peer verification, host authentication and protected commands.
            hostChallenge.CopyTo(context, 0);
            hsmChallenge.CopyTo(context, 8);
            ReadOnlyMemory<byte>? cardCryptogramMemory = cardCryptogram is null
                ? null
                : cardCryptogram.AsMemory();

            using var keys = await session.CalculateSessionKeysSymmetricAsync(
                label,
                context,
                password,
                cardCryptogramMemory,
                cancellationToken: cancellationToken);

            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("Session keys calculated successfully.");

            // Diagnostic output exposes session secrets. Use disposable test material and keep key
            // output out of an application's terminal/logging path.
            OutputHelpers.WriteHex("S-ENC (demo only)", keys.SEnc);
            OutputHelpers.WriteHex("S-MAC (demo only)", keys.SMac);
            OutputHelpers.WriteHex("S-RMAC (demo only)", keys.SRmac);
        }
        finally
        {
            if (hostChallenge is not null)
                CryptographicOperations.ZeroMemory(hostChallenge);
            if (hsmChallenge is not null)
                CryptographicOperations.ZeroMemory(hsmChallenge);
            if (cardCryptogram is not null)
                CryptographicOperations.ZeroMemory(cardCryptogram);
            CryptographicOperations.ZeroMemory(context);
        }
    }
}
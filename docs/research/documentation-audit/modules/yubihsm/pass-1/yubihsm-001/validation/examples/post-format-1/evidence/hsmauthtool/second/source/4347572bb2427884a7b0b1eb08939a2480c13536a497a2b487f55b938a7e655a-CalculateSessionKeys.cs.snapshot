// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Security.Cryptography;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using SecureCredential = Yubico.YubiKit.Cli.Shared.Output.SecureCredential;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

public static class CalculateSessionKeys
{
    public static async Task RunAsync(
        HsmAuthSession session,
        CancellationToken cancellationToken = default)
    {
        // The chooser lists every algorithm, but this operation always calls symmetric CALCULATE.
        // Select a symmetric credential; asymmetric handshake dispatch requires separate application code.
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

        using var credentialPassword = SecureCredential.Prompt("Credential password");
        if (credentialPassword is null)
        {
            OutputHelpers.WriteError("Credential password is required.");
            return;
        }

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
            // Supplied symmetric cryptograms can yield 56 response bytes; the SDK currently requires
            // 48 and rejects that shape. Verify the complete target-app handshake before relying on it.
            hostChallenge.CopyTo(context, 0);
            hsmChallenge.CopyTo(context, 8);
            ReadOnlyMemory<byte>? cardCryptogramMemory = cardCryptogram is null
                ? null
                : cardCryptogram.AsMemory();

            using var keys = await session.CalculateSessionKeysSymmetricAsync(
                label,
                context,
                credentialPassword.Memory,
                cardCryptogramMemory,
                cancellationToken: cancellationToken);

            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("Session keys calculated successfully.");

            // Diagnostic output exposes session secrets. Use disposable test material and keep key
            // output out of an application's terminal/logging path.
            OutputHelpers.WriteHex("S-ENC", keys.SEnc);
            OutputHelpers.WriteHex("S-MAC", keys.SMac);
            OutputHelpers.WriteHex("S-RMAC", keys.SRmac);
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
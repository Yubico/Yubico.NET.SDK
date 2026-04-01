// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using System.Text;
using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates signing a message using the Signature key.
/// </summary>
public static class SignMessage
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var inputChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Input source:")
                .AddChoices(["Type message", "Read from file"]));

        byte[] message;
        if (inputChoice == "Type message")
        {
            var text = AnsiConsole.Ask<string>("Message to sign:");
            message = Encoding.UTF8.GetBytes(text);
        }
        else
        {
            var path = AnsiConsole.Ask<string>("File path:");
            if (!File.Exists(path))
            {
                OutputHelpers.WriteError($"File not found: {path}");
                return;
            }

            message = await File.ReadAllBytesAsync(path, cancellationToken);
        }

        var hashAlgorithm = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Hash algorithm:")
                .AddChoices(["SHA-256", "SHA-384", "SHA-512"]));

        var hashName = hashAlgorithm switch
        {
            "SHA-384" => HashAlgorithmName.SHA384,
            "SHA-512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        // Verify User PIN for signing
        var pin = OutputHelpers.PromptPin("User PIN (required for signing)");

        try
        {
            await session.VerifyPinAsync(pin, extended: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"PIN verification failed: {ex.Message}");
            return;
        }

        try
        {
            var signature = await session.SignAsync(message, hashName, cancellationToken);

            AnsiConsole.MarkupLine("[bold]Signature[/]");
            OutputHelpers.WriteKeyValue("Algorithm", hashAlgorithm);
            OutputHelpers.WriteKeyValue("Message Length", $"{message.Length} bytes");
            OutputHelpers.WriteKeyValue("Signature Length", $"{signature.Length} bytes");
            AnsiConsole.WriteLine();

            // Display signature as hex
            AnsiConsole.MarkupLine("[grey]Signature (hex):[/]");
            var hex = Convert.ToHexString(signature.Span);
            // Wrap long hex strings
            for (int i = 0; i < hex.Length; i += 64)
            {
                var chunk = hex.Substring(i, Math.Min(64, hex.Length - i));
                AnsiConsole.MarkupLine($"  [grey]{chunk}[/]");
            }

            // Offer to save
            if (AnsiConsole.Confirm("Save signature to file?", defaultValue: false))
            {
                var outPath = AnsiConsole.Ask<string>("Output file path:");
                await File.WriteAllBytesAsync(outPath, signature.ToArray(), cancellationToken);
                OutputHelpers.WriteSuccess($"Signature saved to {outPath}");
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Signing failed: {ex.Message}");
            OutputHelpers.WriteInfo("Make sure a signing key has been generated");
        }
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates decrypting data using the Decryption key.
/// </summary>
public static class DecryptData
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var path = AnsiConsole.Ask<string>("Path to encrypted file:");
        if (!File.Exists(path))
        {
            OutputHelpers.WriteError($"File not found: {path}");
            return;
        }

        var ciphertext = await File.ReadAllBytesAsync(path, cancellationToken);
        OutputHelpers.WriteKeyValue("Ciphertext Length", $"{ciphertext.Length} bytes");

        // Verify User PIN for decryption
        var pin = OutputHelpers.PromptPin("User PIN (required for decryption)");

        try
        {
            await session.VerifyPinAsync(pin, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"PIN verification failed: {ex.Message}");
            return;
        }

        try
        {
            var plaintext = await session.DecryptAsync(ciphertext, cancellationToken);

            OutputHelpers.WriteKeyValue("Plaintext Length", $"{plaintext.Length} bytes");

            var outputChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Output:")
                    .AddChoices(["Save to file", "Display as text", "Display as hex"]));

            switch (outputChoice)
            {
                case "Save to file":
                    var outPath = AnsiConsole.Ask<string>("Output file path:");
                    await File.WriteAllBytesAsync(outPath, plaintext.ToArray(), cancellationToken);
                    OutputHelpers.WriteSuccess($"Decrypted data saved to {outPath}");
                    break;

                case "Display as text":
                    var text = System.Text.Encoding.UTF8.GetString(plaintext.Span);
                    AnsiConsole.MarkupLine("[grey]Plaintext:[/]");
                    AnsiConsole.WriteLine(text);
                    break;

                case "Display as hex":
                    OutputHelpers.WriteHex("Plaintext", plaintext.Span);
                    break;
            }
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Decryption failed: {ex.Message}");
            OutputHelpers.WriteInfo("Make sure a decryption key has been generated and the ciphertext is valid");
        }
    }
}

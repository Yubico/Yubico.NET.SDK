// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for cryptographic operations (signing, decryption, verification).
/// </summary>
public static class CryptoMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Cryptographic Operations");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "Sign Data",
                    "Decrypt Data (RSA only)",
                    "Verify Signature",
                    "Back"
                ]));

        if (choice == "Back")
        {
            return;
        }

        await using var session = await selection.Device.CreatePivSessionAsync(cancellationToken: cancellationToken);
        OutputHelpers.SetupTouchNotification(session);

        switch (choice)
        {
            case "Sign Data":
                await SignDataAsync(session, cancellationToken);
                break;
            case "Decrypt Data (RSA only)":
                await DecryptDataAsync(session, cancellationToken);
                break;
            case "Verify Signature":
                await VerifySignatureAsync(session, cancellationToken);
                break;
        }
    }

    private static async Task SignDataAsync(IPivSession session, CancellationToken ct)
    {
        var slot = SlotSelector.SelectSlot("Select slot with signing key:");

        // Verify PIN if needed
        using var pin = PinPrompt.GetPinWithDefault("PIN");
        if (pin is null)
        {
            return;
        }

        var pinResult = await PinManagement.VerifyPinAsync(session, pin.Memory.Span.ToArray(), ct);
        if (!pinResult.Success)
        {
            OutputHelpers.WriteError(pinResult.ErrorMessage ?? "PIN verification failed");
            return;
        }

        // Get hash algorithm
        var hashAlg = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select hash algorithm:")
                .AddChoices(["SHA-256", "SHA-384", "SHA-512"])) switch
        {
            "SHA-384" => HashAlgorithmName.SHA384,
            "SHA-512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        // Get data to sign
        var inputSource = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Data source:")
                .AddChoices(["Enter text", "Read from file"]));

        byte[] dataToSign;
        if (inputSource == "Enter text")
        {
            var text = AnsiConsole.Ask<string>("Enter text to sign:");
            dataToSign = System.Text.Encoding.UTF8.GetBytes(text);
        }
        else
        {
            var filename = AnsiConsole.Ask<string>("Enter file path:");
            if (!File.Exists(filename))
            {
                OutputHelpers.WriteError("File not found");
                return;
            }
            dataToSign = await File.ReadAllBytesAsync(filename, ct);
        }

        OutputHelpers.WriteInfo("Touch YubiKey if prompted...");

        var result = await Signing.SignDataAsync(session, slot, dataToSign, hashAlg, ct);

        if (result.Success)
        {
            OutputHelpers.WriteSuccess($"Signing completed in {result.ElapsedMilliseconds}ms");
            OutputHelpers.WriteKeyValue("Signature size", $"{result.Signature.Length} bytes");
            
            var sigHex = Convert.ToHexString(result.Signature.Span);
            AnsiConsole.WriteLine($"Signature (hex): {sigHex[..Math.Min(64, sigHex.Length)]}...");

            if (AnsiConsole.Confirm("Save signature to file?", defaultValue: false))
            {
                var outFile = AnsiConsole.Ask<string>("Enter filename:");
                await File.WriteAllBytesAsync(outFile, result.Signature.ToArray(), ct);
                OutputHelpers.WriteSuccess($"Signature saved to {outFile}");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Signing failed");
        }
    }

    private static async Task DecryptDataAsync(IPivSession session, CancellationToken ct)
    {
        var slot = SlotSelector.SelectSlot("Select slot with RSA decryption key:");

        // Verify PIN
        using var pin = PinPrompt.GetPinWithDefault("PIN");
        if (pin is null)
        {
            return;
        }

        var pinResult = await PinManagement.VerifyPinAsync(session, pin.Memory.Span.ToArray(), ct);
        if (!pinResult.Success)
        {
            OutputHelpers.WriteError(pinResult.ErrorMessage ?? "PIN verification failed");
            return;
        }

        // Get encrypted data
        var filename = AnsiConsole.Ask<string>("Enter encrypted file path:");
        if (!File.Exists(filename))
        {
            OutputHelpers.WriteError("File not found");
            return;
        }

        var encryptedData = await File.ReadAllBytesAsync(filename, ct);

        OutputHelpers.WriteInfo("Touch YubiKey if prompted...");

        var result = await Decryption.DecryptDataAsync(session, slot, encryptedData, ct);

        if (result.Success)
        {
            OutputHelpers.WriteSuccess($"Decryption completed in {result.ElapsedMilliseconds}ms");
            OutputHelpers.WriteKeyValue("Decrypted size", $"{result.DecryptedData.Length} bytes");

            if (AnsiConsole.Confirm("Save decrypted data to file?", defaultValue: true))
            {
                var outFile = AnsiConsole.Ask<string>("Enter filename:");
                await File.WriteAllBytesAsync(outFile, result.DecryptedData.ToArray(), ct);
                OutputHelpers.WriteSuccess($"Decrypted data saved to {outFile}");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Decryption failed");
        }
    }

    private static async Task VerifySignatureAsync(IPivSession session, CancellationToken ct)
    {
        var slot = SlotSelector.SelectSlot("Select slot with certificate:");

        // Get hash algorithm
        var hashAlg = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select hash algorithm used during signing:")
                .AddChoices(["SHA-256", "SHA-384", "SHA-512"])) switch
        {
            "SHA-384" => HashAlgorithmName.SHA384,
            "SHA-512" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        // Get original data
        var dataSource = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Original data source:")
                .AddChoices(["Enter text", "Read from file"]));

        byte[] originalData;
        if (dataSource == "Enter text")
        {
            var text = AnsiConsole.Ask<string>("Enter original text:");
            originalData = System.Text.Encoding.UTF8.GetBytes(text);
        }
        else
        {
            var filename = AnsiConsole.Ask<string>("Enter file path:");
            if (!File.Exists(filename))
            {
                OutputHelpers.WriteError("File not found");
                return;
            }
            originalData = await File.ReadAllBytesAsync(filename, ct);
        }

        // Get signature
        var sigFilename = AnsiConsole.Ask<string>("Enter signature file path:");
        if (!File.Exists(sigFilename))
        {
            OutputHelpers.WriteError("File not found");
            return;
        }

        var signature = await File.ReadAllBytesAsync(sigFilename, ct);

        var result = await Verification.VerifySignatureAsync(
            session, slot, originalData, signature, hashAlg, ct);

        if (result.Success)
        {
            if (result.IsValid)
            {
                OutputHelpers.WriteSuccess($"Signature is VALID (verified in {result.ElapsedMilliseconds}ms)");
            }
            else
            {
                OutputHelpers.WriteError($"Signature is INVALID (verified in {result.ElapsedMilliseconds}ms)");
            }
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Verification failed");
        }
    }
}

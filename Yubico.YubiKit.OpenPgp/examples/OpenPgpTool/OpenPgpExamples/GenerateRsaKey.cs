// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates generating an RSA key in a specified slot.
/// </summary>
public static class GenerateRsaKey
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = PromptForKeySlot();
        var size = PromptForKeySize();

        // Admin PIN required for key generation
        var adminPin = OutputHelpers.PromptPin("Admin PIN (required for key generation)");

        try
        {
            await session.VerifyAdminAsync(adminPin, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Admin PIN verification failed: {ex.Message}");
            return;
        }

        if (!OutputHelpers.ConfirmDangerous($"generate RSA {(int)size} key in {FormatSlot(slot)} slot (overwrites existing key)"))
        {
            OutputHelpers.WriteInfo("Key generation cancelled");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync($"Generating RSA {(int)size} key...", async _ =>
            {
                await session.GenerateRsaKeyAsync(slot, size, cancellationToken);
            });

        OutputHelpers.WriteSuccess($"RSA {(int)size} key generated in {FormatSlot(slot)} slot");
    }

    private static KeyRef PromptForKeySlot()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot:")
                .AddChoices(["Signature", "Decryption", "Authentication"]));

        return choice switch
        {
            "Signature" => KeyRef.Sig,
            "Decryption" => KeyRef.Dec,
            _ => KeyRef.Aut
        };
    }

    private static RsaSize PromptForKeySize()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select RSA key size:")
                .AddChoices(["2048", "3072", "4096"]));

        return choice switch
        {
            "3072" => RsaSize.Rsa3072,
            "4096" => RsaSize.Rsa4096,
            _ => RsaSize.Rsa2048
        };
    }

    private static string FormatSlot(KeyRef slot) =>
        slot switch
        {
            KeyRef.Sig => "Signature",
            KeyRef.Dec => "Decryption",
            KeyRef.Aut => "Authentication",
            _ => slot.ToString()
        };
}

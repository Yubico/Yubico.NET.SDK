// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates deleting a key from a specified slot.
/// </summary>
public static class DeleteKey
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot to delete:")
                .AddChoices(["Signature", "Decryption", "Authentication"]));

        var keyRef = slot switch
        {
            "Signature" => KeyRef.Sig,
            "Decryption" => KeyRef.Dec,
            _ => KeyRef.Aut
        };

        // Admin PIN required
        var adminPin = OutputHelpers.PromptPin("Admin PIN");

        try
        {
            await session.VerifyAdminAsync(adminPin, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Admin PIN verification failed: {ex.Message}");
            return;
        }

        if (!OutputHelpers.ConfirmDangerous($"permanently delete the {slot} key"))
        {
            OutputHelpers.WriteInfo("Key deletion cancelled");
            return;
        }

        try
        {
            await session.DeleteKeyAsync(keyRef, cancellationToken);
            OutputHelpers.WriteSuccess($"{slot} key deleted");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to delete key: {ex.Message}");
        }
    }
}

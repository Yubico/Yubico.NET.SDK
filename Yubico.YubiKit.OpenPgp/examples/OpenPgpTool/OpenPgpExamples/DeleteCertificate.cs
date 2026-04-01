// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates deleting a certificate from a key slot.
/// </summary>
public static class DeleteCertificate
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot:")
                .AddChoices(["Signature", "Decryption", "Authentication", "Attestation"]));

        var keyRef = slot switch
        {
            "Signature" => KeyRef.Sig,
            "Decryption" => KeyRef.Dec,
            "Authentication" => KeyRef.Aut,
            _ => KeyRef.Att
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

        if (!OutputHelpers.ConfirmDangerous($"delete the certificate from the {slot} slot"))
        {
            OutputHelpers.WriteInfo("Certificate deletion cancelled");
            return;
        }

        try
        {
            await session.DeleteCertificateAsync(keyRef, cancellationToken);
            OutputHelpers.WriteSuccess($"Certificate deleted from {slot} slot");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to delete certificate: {ex.Message}");
        }
    }
}

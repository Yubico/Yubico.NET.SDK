// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates generating an EC key in a specified slot.
/// </summary>
public static class GenerateEcKey
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = PromptForKeySlot();
        var curve = PromptForCurve(slot);

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

        if (!OutputHelpers.ConfirmDangerous($"generate EC {curve} key in {FormatSlot(slot)} slot (overwrites existing key)"))
        {
            OutputHelpers.WriteInfo("Key generation cancelled");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync($"Generating EC {curve} key...", async _ =>
            {
                await session.GenerateEcKeyAsync(slot, curve, cancellationToken);
            });

        OutputHelpers.WriteSuccess($"EC {curve} key generated in {FormatSlot(slot)} slot");
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

    private static CurveOid PromptForCurve(KeyRef slot)
    {
        // Ed25519 is for signing/auth only, X25519 is for decryption only
        List<string> curves = ["NIST P-256", "NIST P-384", "NIST P-521"];

        if (slot == KeyRef.Dec)
        {
            curves.Add("X25519");
        }
        else
        {
            curves.Add("Ed25519");
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select curve:")
                .AddChoices(curves));

        return choice switch
        {
            "NIST P-256" => CurveOid.Secp256R1,
            "NIST P-384" => CurveOid.Secp384R1,
            "NIST P-521" => CurveOid.Secp521R1,
            "X25519" => CurveOid.X25519,
            "Ed25519" => CurveOid.Ed25519,
            _ => CurveOid.Secp256R1
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

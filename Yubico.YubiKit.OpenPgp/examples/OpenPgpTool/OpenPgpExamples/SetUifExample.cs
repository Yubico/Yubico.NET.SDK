// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates setting the User Interaction Flag (touch policy) for a key slot.
/// </summary>
public static class SetUifExample
{
    public static async Task RunAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var slot = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key slot:")
                .AddChoices(["Signature", "Decryption", "Authentication"]));

        var keyRef = slot switch
        {
            "Signature" => KeyRef.Sig,
            "Decryption" => KeyRef.Dec,
            _ => KeyRef.Aut
        };

        // Show current value
        try
        {
            var current = await session.GetUifAsync(keyRef, cancellationToken);
            OutputHelpers.WriteKeyValue("Current Touch Policy", current.ToString());

            if (current.IsFixed())
            {
                OutputHelpers.WriteError("Touch policy is fixed and cannot be changed without factory reset");
                return;
            }
        }
        catch (NotSupportedException)
        {
            OutputHelpers.WriteError("Touch policy requires firmware 4.2.0 or later");
            return;
        }

        var uifChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select touch policy:")
                .AddChoices(["Off", "On", "Fixed (permanent)", "Cached", "Cached Fixed (permanent)"]));

        var uif = uifChoice switch
        {
            "Off" => Uif.Off,
            "On" => Uif.On,
            "Fixed (permanent)" => Uif.Fixed,
            "Cached" => Uif.Cached,
            "Cached Fixed (permanent)" => Uif.CachedFixed,
            _ => Uif.Off
        };

        if (uif.IsFixed() &&
            !OutputHelpers.ConfirmDangerous("set a PERMANENT touch policy that cannot be changed without factory reset"))
        {
            OutputHelpers.WriteInfo("Touch policy change cancelled");
            return;
        }

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

        try
        {
            await session.SetUifAsync(keyRef, uif, cancellationToken);
            OutputHelpers.WriteSuccess($"Touch policy for {slot} set to {uif}");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to set touch policy: {ex.Message}");
        }
    }
}

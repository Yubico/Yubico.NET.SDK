// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

/// <summary>
/// Demonstrates changing User PIN, Admin PIN, and resetting the User PIN.
/// </summary>
public static class ChangePin
{
    public static async Task RunUserAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var currentPin = OutputHelpers.PromptPin("Current User PIN");
        var newPin = OutputHelpers.PromptPin("New User PIN");
        var confirmPin = OutputHelpers.PromptPin("Confirm new User PIN");

        if (!string.Equals(newPin, confirmPin, StringComparison.Ordinal))
        {
            OutputHelpers.WriteError("New PINs do not match");
            return;
        }

        try
        {
            await session.ChangePinAsync(currentPin, newPin, cancellationToken);
            OutputHelpers.WriteSuccess("User PIN changed successfully");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to change User PIN: {ex.Message}");
        }
    }

    public static async Task RunAdminAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var currentPin = OutputHelpers.PromptPin("Current Admin PIN");
        var newPin = OutputHelpers.PromptPin("New Admin PIN");
        var confirmPin = OutputHelpers.PromptPin("Confirm new Admin PIN");

        if (!string.Equals(newPin, confirmPin, StringComparison.Ordinal))
        {
            OutputHelpers.WriteError("New PINs do not match");
            return;
        }

        try
        {
            await session.ChangeAdminAsync(currentPin, newPin, cancellationToken);
            OutputHelpers.WriteSuccess("Admin PIN changed successfully");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to change Admin PIN: {ex.Message}");
        }
    }

    public static async Task RunResetAsync(IOpenPgpSession session, CancellationToken cancellationToken)
    {
        var useAdmin = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Reset PIN using:")
                .AddChoices(["Admin PIN", "Reset Code"])) == "Admin PIN";

        var resetCode = OutputHelpers.PromptPin(useAdmin ? "Admin PIN" : "Reset Code");
        var newPin = OutputHelpers.PromptPin("New User PIN");
        var confirmPin = OutputHelpers.PromptPin("Confirm new User PIN");

        if (!string.Equals(newPin, confirmPin, StringComparison.Ordinal))
        {
            OutputHelpers.WriteError("New PINs do not match");
            return;
        }

        try
        {
            await session.ResetPinAsync(resetCode, newPin, useAdmin, cancellationToken);
            OutputHelpers.WriteSuccess("User PIN reset successfully");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to reset User PIN: {ex.Message}");
        }
    }
}

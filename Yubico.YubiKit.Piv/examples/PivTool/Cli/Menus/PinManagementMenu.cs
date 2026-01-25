// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Menus;

/// <summary>
/// CLI menu for PIN/PUK management operations.
/// </summary>
public static class PinManagementMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("PIN Management");

        var device = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (device is null)
        {
            return;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "Verify PIN",
                    "Change PIN",
                    "Change PUK",
                    "Unblock PIN using PUK",
                    "Authenticate Management Key",
                    "Change Management Key",
                    "Back"
                ]));

        if (choice == "Back")
        {
            return;
        }

        await using var session = await device.CreatePivSessionAsync(cancellationToken: cancellationToken);
        OutputHelpers.SetupTouchNotification(session);

        switch (choice)
        {
            case "Verify PIN":
                await VerifyPinAsync(session, cancellationToken);
                break;
            case "Change PIN":
                await ChangePinAsync(session, cancellationToken);
                break;
            case "Change PUK":
                await ChangePukAsync(session, cancellationToken);
                break;
            case "Unblock PIN using PUK":
                await UnblockPinAsync(session, cancellationToken);
                break;
            case "Authenticate Management Key":
                await AuthenticateAsync(session, cancellationToken);
                break;
            case "Change Management Key":
                await ChangeManagementKeyAsync(session, cancellationToken);
                break;
        }
    }

    private static async Task VerifyPinAsync(IPivSession session, CancellationToken ct)
    {
        using var pin = PinPrompt.GetPinWithDefault("PIN to verify");
        if (pin is null)
        {
            return;
        }

        var result = await PinManagement.VerifyPinAsync(session, pin.Memory.Span.ToArray(), ct);
        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PIN verified successfully");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "PIN verification failed");
            if (result.RetriesRemaining.HasValue)
            {
                OutputHelpers.WriteInfo($"Retries remaining: {result.RetriesRemaining}");
            }
        }
    }

    private static async Task ChangePinAsync(IPivSession session, CancellationToken ct)
    {
        using var currentPin = PinPrompt.GetPinWithDefault("Current PIN");
        if (currentPin is null)
        {
            return;
        }

        using var newPin = PinPrompt.GetNewPin();
        if (newPin is null)
        {
            return;
        }

        var result = await PinManagement.ChangePinAsync(session, currentPin.Memory.Span.ToArray(), newPin.Memory.Span.ToArray(), ct);
        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PIN changed successfully");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to change PIN");
            if (result.RetriesRemaining.HasValue)
            {
                OutputHelpers.WriteInfo($"Retries remaining: {result.RetriesRemaining}");
            }
        }
    }

    private static async Task ChangePukAsync(IPivSession session, CancellationToken ct)
    {
        using var currentPuk = PinPrompt.GetPukWithDefault("Current PUK");
        if (currentPuk is null)
        {
            return;
        }

        using var newPuk = PinPrompt.GetNewPuk();
        if (newPuk is null)
        {
            return;
        }

        var result = await PinManagement.ChangePukAsync(session, currentPuk.Memory.Span.ToArray(), newPuk.Memory.Span.ToArray(), ct);
        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PUK changed successfully");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to change PUK");
            if (result.RetriesRemaining.HasValue)
            {
                OutputHelpers.WriteInfo($"Retries remaining: {result.RetriesRemaining}");
            }
        }
    }

    private static async Task UnblockPinAsync(IPivSession session, CancellationToken ct)
    {
        using var puk = PinPrompt.GetPukWithDefault("PUK");
        if (puk is null)
        {
            return;
        }

        using var newPin = PinPrompt.GetNewPin();
        if (newPin is null)
        {
            return;
        }

        var result = await PinManagement.UnblockPinAsync(session, puk.Memory.Span.ToArray(), newPin.Memory.Span.ToArray(), ct);
        if (result.Success)
        {
            OutputHelpers.WriteSuccess("PIN unblocked successfully");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to unblock PIN");
            if (result.RetriesRemaining.HasValue)
            {
                OutputHelpers.WriteInfo($"PUK retries remaining: {result.RetriesRemaining}");
            }
        }
    }

    private static async Task AuthenticateAsync(IPivSession session, CancellationToken ct)
    {
        using var mgmtKey = PinPrompt.GetManagementKeyWithDefault("Management key");
        if (mgmtKey is null)
        {
            return;
        }

        var result = await PinManagement.AuthenticateAsync(session, mgmtKey.Memory.Span.ToArray(), ct);
        if (result.Success)
        {
            OutputHelpers.WriteSuccess("Management key authenticated successfully");
        }
        else
        {
            OutputHelpers.WriteError(result.ErrorMessage ?? "Authentication failed");
        }
    }

    private static async Task ChangeManagementKeyAsync(IPivSession session, CancellationToken ct)
    {
        // First authenticate with current key
        OutputHelpers.WriteInfo("First, authenticate with current management key:");
        using var currentKey = PinPrompt.GetManagementKeyWithDefault("Current management key");
        if (currentKey is null)
        {
            return;
        }

        var authResult = await PinManagement.AuthenticateAsync(session, currentKey.Memory.Span.ToArray(), ct);
        if (!authResult.Success)
        {
            OutputHelpers.WriteError(authResult.ErrorMessage ?? "Authentication failed");
            return;
        }

        OutputHelpers.WriteSuccess("Authenticated with current key");

        // Now get new key
        using var newKey = PinPrompt.GetNewManagementKey();
        if (newKey is null)
        {
            return;
        }

        var changeResult = await PinManagement.ChangeManagementKeyAsync(session, newKey.Memory.Span.ToArray(), cancellationToken: ct);
        if (changeResult.Success)
        {
            OutputHelpers.WriteSuccess("Management key changed successfully");
        }
        else
        {
            OutputHelpers.WriteError(changeResult.ErrorMessage ?? "Failed to change management key");
        }
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

/// <summary>
/// CLI menu for PIN management operations.
/// </summary>
public static class PinMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("PIN Management");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select PIN operation:")
                .AddChoices(
                [
                    "Verify User PIN",
                    "Verify Admin PIN",
                    "Change User PIN",
                    "Change Admin PIN",
                    "Reset User PIN",
                    "Back"
                ]));

        if (choice == "Back")
        {
            return;
        }

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        switch (choice)
        {
            case "Verify User PIN":
                await VerifyPin.RunUserAsync(session, cancellationToken);
                break;
            case "Verify Admin PIN":
                await VerifyPin.RunAdminAsync(session, cancellationToken);
                break;
            case "Change User PIN":
                await ChangePin.RunUserAsync(session, cancellationToken);
                break;
            case "Change Admin PIN":
                await ChangePin.RunAdminAsync(session, cancellationToken);
                break;
            case "Reset User PIN":
                await ChangePin.RunResetAsync(session, cancellationToken);
                break;
        }
    }

    public static async Task RunVerifyAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Verify PIN");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await VerifyPin.RunUserAsync(session, cancellationToken);
    }

    public static async Task RunChangeAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Change PIN");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await ChangePin.RunUserAsync(session, cancellationToken);
    }

    public static async Task RunResetAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Reset PIN");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await ChangePin.RunResetAsync(session, cancellationToken);
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

/// <summary>
/// CLI menu for cryptographic operations.
/// </summary>
public static class CryptoMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Cryptographic Operations");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select operation:")
                .AddChoices(
                [
                    "Sign Message",
                    "Decrypt Data",
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
            case "Sign Message":
                await SignMessage.RunAsync(session, cancellationToken);
                break;
            case "Decrypt Data":
                await DecryptData.RunAsync(session, cancellationToken);
                break;
        }
    }

    public static async Task RunSignAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Sign Message");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await SignMessage.RunAsync(session, cancellationToken);
    }

    public static async Task RunDecryptAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Decrypt Data");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await DecryptData.RunAsync(session, cancellationToken);
    }
}

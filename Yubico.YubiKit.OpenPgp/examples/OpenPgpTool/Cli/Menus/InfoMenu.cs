// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

/// <summary>
/// CLI menu for displaying OpenPGP card information.
/// </summary>
public static class InfoMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("OpenPGP Card Information");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What information would you like to view?")
                .AddChoices(
                [
                    "Card Status",
                    "Key Information",
                    "Back"
                ]));

        if (choice == "Back")
        {
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Reading card data...", async _ =>
            {
                await using var session = await selection.Device.CreateOpenPgpSessionAsync(
                    cancellationToken: cancellationToken);

                switch (choice)
                {
                    case "Card Status":
                        await GetCardStatus.RunAsync(session, cancellationToken);
                        break;
                    case "Key Information":
                        await GetKeyInfo.RunAsync(session, cancellationToken);
                        break;
                }
            });
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

/// <summary>
/// CLI menu for device configuration operations.
/// </summary>
public static class ConfigMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Configuration");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select configuration option:")
                .AddChoices(
                [
                    "View Touch Policy (UIF)",
                    "Set Touch Policy (UIF)",
                    "View Algorithm Attributes",
                    "View KDF Configuration",
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
            case "View Touch Policy (UIF)":
                await GetUifExample.RunAsync(session, cancellationToken);
                break;
            case "Set Touch Policy (UIF)":
                await SetUifExample.RunAsync(session, cancellationToken);
                break;
            case "View Algorithm Attributes":
                await ViewAlgorithmAttributes.RunAsync(session, cancellationToken);
                break;
            case "View KDF Configuration":
                await ViewKdfConfig.RunAsync(session, cancellationToken);
                break;
        }
    }

    public static async Task RunUifAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Touch Policy (UIF)");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await GetUifExample.RunAsync(session, cancellationToken);
    }

    public static async Task RunAlgorithmAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Algorithm Attributes");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await ViewAlgorithmAttributes.RunAsync(session, cancellationToken);
    }

    public static async Task RunKdfAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("KDF Configuration");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await ViewKdfConfig.RunAsync(session, cancellationToken);
    }
}

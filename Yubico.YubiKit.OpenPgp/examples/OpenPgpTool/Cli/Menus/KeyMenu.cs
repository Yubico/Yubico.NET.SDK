// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

/// <summary>
/// CLI menu for key management operations.
/// </summary>
public static class KeyMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Key Management");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select key operation:")
                .AddChoices(
                [
                    "Generate RSA Key",
                    "Generate EC Key",
                    "Delete Key",
                    "Attest Key",
                    "View Key Info",
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
            case "Generate RSA Key":
                await GenerateRsaKey.RunAsync(session, cancellationToken);
                break;
            case "Generate EC Key":
                await GenerateEcKey.RunAsync(session, cancellationToken);
                break;
            case "Delete Key":
                await DeleteKey.RunAsync(session, cancellationToken);
                break;
            case "Attest Key":
                await AttestKey.RunAsync(session, cancellationToken);
                break;
            case "View Key Info":
                await GetKeyInfo.RunAsync(session, cancellationToken);
                break;
        }
    }

    public static async Task RunGenerateAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Generate Key");

        var keyType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Key type:")
                .AddChoices(["RSA", "EC"]));

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        if (keyType == "RSA")
        {
            await GenerateRsaKey.RunAsync(session, cancellationToken);
        }
        else
        {
            await GenerateEcKey.RunAsync(session, cancellationToken);
        }
    }

    public static async Task RunDeleteAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Delete Key");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await DeleteKey.RunAsync(session, cancellationToken);
    }

    public static async Task RunAttestAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Attest Key");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await AttestKey.RunAsync(session, cancellationToken);
    }
}

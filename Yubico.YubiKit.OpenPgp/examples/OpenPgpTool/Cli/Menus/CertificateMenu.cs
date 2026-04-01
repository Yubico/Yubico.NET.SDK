// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.OpenPgpExamples;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Menus;

/// <summary>
/// CLI menu for certificate management operations.
/// </summary>
public static class CertificateMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Certificate Management");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select certificate operation:")
                .AddChoices(
                [
                    "Import Certificate",
                    "Export Certificate",
                    "Delete Certificate",
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
            case "Import Certificate":
                await ImportCertificate.RunAsync(session, cancellationToken);
                break;
            case "Export Certificate":
                await ExportCertificate.RunAsync(session, cancellationToken);
                break;
            case "Delete Certificate":
                await DeleteCertificate.RunAsync(session, cancellationToken);
                break;
        }
    }

    public static async Task RunImportAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Import Certificate");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await ImportCertificate.RunAsync(session, cancellationToken);
    }

    public static async Task RunExportAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Export Certificate");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await ExportCertificate.RunAsync(session, cancellationToken);
    }

    public static async Task RunDeleteAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Delete Certificate");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOpenPgpSessionAsync(
            cancellationToken: cancellationToken);

        await DeleteCertificate.RunAsync(session, cancellationToken);
    }
}

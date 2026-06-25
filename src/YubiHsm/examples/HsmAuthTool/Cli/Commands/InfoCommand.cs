// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;

/// <summary>
/// Implements: hsmauth info
/// Displays general status of the YubiHSM Auth applet.
/// </summary>
internal static class InfoCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);

        var retries = await session.GetManagementKeyRetriesAsync(cancellationToken);
        var credentials = await session.ListCredentialsAsync(cancellationToken);

        AnsiConsole.MarkupLine($"YubiHSM Auth version: [green]{session.FirmwareVersion}[/]");
        AnsiConsole.MarkupLine($"Management key retries remaining: [green]{retries}[/]");
        AnsiConsole.MarkupLine($"Stored credentials: [green]{credentials.Count}[/]");

        if (retries <= 3)
        {
            OutputHelpers.WriteWarning("Low retry count! Incorrect attempts will reduce this further.");
        }

        return 0;
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;

/// <summary>
/// Implements: hsmauth reset [-f]
/// Resets the YubiHSM Auth applet to factory defaults.
/// </summary>
internal static class ResetCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var force = CommandArgs.HasFlag(args, "-f", "--force");

        if (!force)
        {
            AnsiConsole.MarkupLine("[red bold]WARNING:[/] This will reset the YubiHSM Auth applet.");
            AnsiConsole.MarkupLine("[red]ALL stored credentials will be permanently deleted.[/]");
            AnsiConsole.MarkupLine("[red]The management key will be reset to the default (all zeros).[/]");

            if (!AnsiConsole.Confirm("Are you sure?", defaultValue: false))
            {
                AnsiConsole.MarkupLine("[grey]Aborted.[/]");
                return 1;
            }
        }

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);

        await session.ResetAsync(cancellationToken);

        OutputHelpers.WriteSuccess("YubiHSM Auth applet has been reset.");

        return 0;
    }
}

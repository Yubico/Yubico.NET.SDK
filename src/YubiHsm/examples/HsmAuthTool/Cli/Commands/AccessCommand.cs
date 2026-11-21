// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Commands;

/// <summary>
/// Implements: hsmauth access change-management-key
/// Changes the management key on the YubiHSM Auth applet.
/// </summary>
internal static class AccessCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 2 || !string.Equals(args[1], "change-management-key", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 1;
        }

        return await ChangeManagementKeyAsync(cancellationToken);
    }

    private static async Task<int> ChangeManagementKeyAsync(CancellationToken cancellationToken)
    {
        // Store the replacement recoverably before installing it.
        using var currentKey = OutputHelpers.ReadKey("Current management key (16 bytes, hex)", 16, cancellationToken);
        if (currentKey is null)
            return 1;

        using var newKey = OutputHelpers.ReadKey("New management key (16 bytes, hex)", 16, cancellationToken);
        if (newKey is null)
            return 1;

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
            return 1;

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(cancellationToken: cancellationToken);
        await session.PutManagementKeyAsync(currentKey.Memory, newKey.Memory, cancellationToken);
        OutputHelpers.WriteSuccess("Management key changed successfully.");
        OutputHelpers.WriteWarning("Store the new key securely. Losing it will require a factory reset.");
        return 0;
    }

    private static void PrintUsage()
    {
        AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool access change-management-key");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Management keys are prompted for, not accepted as arguments.");
    }
}
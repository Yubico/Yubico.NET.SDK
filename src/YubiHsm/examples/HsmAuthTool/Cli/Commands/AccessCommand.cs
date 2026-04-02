// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
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

        return await ChangeManagementKeyAsync(args, cancellationToken);
    }

    private static async Task<int> ChangeManagementKeyAsync(string[] args, CancellationToken cancellationToken)
    {
        var currentKeyHex = CommandArgs.GetOption(args, "--management-key");
        var newKeyHex = CommandArgs.GetOption(args, "--new-management-key");

        // Prompt for missing values
        currentKeyHex ??= AnsiConsole.Prompt(
            new TextPrompt<string>("Current management key (hex, 16 bytes):")
                .DefaultValue("00000000000000000000000000000000"));

        newKeyHex ??= AnsiConsole.Ask<string>("New management key (hex, 16 bytes):");

        var currentKey = CommandArgs.ParseHex(currentKeyHex);
        var newKey = CommandArgs.ParseHex(newKeyHex);

        if (currentKey is null || currentKey.Length != 16)
        {
            OutputHelpers.WriteError("Invalid current management key. Must be 16 bytes (32 hex characters).");
            return 1;
        }

        if (newKey is null || newKey.Length != 16)
        {
            OutputHelpers.WriteError("Invalid new management key. Must be 16 bytes (32 hex characters).");
            return 1;
        }

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        try
        {
            await using var session = await selection.Device.CreateHsmAuthSessionAsync(
                cancellationToken: cancellationToken);

            await session.PutManagementKeyAsync(currentKey, newKey, cancellationToken);
            OutputHelpers.WriteSuccess("Management key changed successfully.");
            OutputHelpers.WriteWarning("Store the new key securely. Losing it will require a factory reset.");
            return 0;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentKey);
            CryptographicOperations.ZeroMemory(newKey);
        }
    }

    private static void PrintUsage()
    {
        AnsiConsole.MarkupLine("[bold]Usage:[/] HsmAuthTool access change-management-key [options]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.MarkupLine("  --management-key HEX       Current management key (hex, 16 bytes)");
        AnsiConsole.MarkupLine("  --new-management-key HEX   New management key (hex, 16 bytes)");
    }
}

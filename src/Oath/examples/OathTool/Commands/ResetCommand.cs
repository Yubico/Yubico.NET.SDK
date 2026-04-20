// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Commands;

/// <summary>
/// Implements 'oath reset' - factory resets the OATH application.
/// </summary>
public static class ResetCommand
{
    public static async Task<int> ExecuteAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        if (!force)
        {
            Console.Error.WriteLine(
                "WARNING: This will destroy all OATH credentials and the access password on the YubiKey.");
            Console.Error.Write("Proceed? [y/N] ");
            var response = Console.ReadLine();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                OutputHelpers.WriteInfo("Reset cancelled.");
                return 1;
            }
        }

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        try
        {
            await session.ResetAsync(cancellationToken);
            OutputHelpers.WriteSuccess("OATH application reset.");
            return 0;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Reset failed: {ex.Message}");
            return 1;
        }
    }
}
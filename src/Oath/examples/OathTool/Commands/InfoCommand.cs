// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Oath.Examples.OathTool.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Commands;

/// <summary>
/// Implements 'oath info' - displays OATH application status.
/// </summary>
public static class InfoCommand
{
    public static async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        OutputHelpers.WriteKeyValue("OATH version", session.FirmwareVersion.ToString());
        OutputHelpers.WriteKeyValue("Password protected", session.IsLocked ? "Yes" : "No");
        OutputHelpers.WriteKeyValue("Device ID", session.DeviceId);

        return 0;
    }
}
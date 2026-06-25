// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Prompts;
using Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.HsmAuthExamples;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Menus;

public static class ManagementKeyMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Change Management Key");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await ChangeManagementKey.RunAsync(session, cancellationToken);
    }

    public static async Task GetRetriesAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Management Key Retries");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null) return;

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateHsmAuthSessionAsync(
            cancellationToken: cancellationToken);
        await GetManagementKeyRetries.RunAsync(session, cancellationToken);
    }
}

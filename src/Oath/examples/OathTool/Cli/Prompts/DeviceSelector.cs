// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Management;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

/// <summary>SmartCard-only selection for OATH operations.</summary>
public static class DeviceSelector
{
    /// <summary>Finds an eligible YubiKey, choosing the first when multiple devices cannot be prompted for.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(
        CancellationToken cancellationToken = default,
        IAnsiConsole? console = null,
        Func<CancellationToken, Task<IReadOnlyList<IYubiKey>>>? discover = null)
    {
        // Keep injectable application I/O on the shared production selection path.
        var selector = discover is null
            ? new SharedSelector([ConnectionType.SmartCard], NonInteractiveSelection.FirstEligible, console: console)
            : new SharedSelector([ConnectionType.SmartCard], NonInteractiveSelection.FirstEligible,
                console ?? AnsiConsole.Console, discover,
                async (device, token) => await device.GetDeviceInfoAsync(cancellationToken: token));
        return selector.SelectDeviceAsync(cancellationToken);
    }
}
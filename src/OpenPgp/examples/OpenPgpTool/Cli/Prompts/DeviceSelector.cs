// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Devices;
using SharedSelector = Yubico.YubiKit.Cli.Shared.Device.DeviceSelector;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;

/// <summary>SmartCard-only selection for OpenPGP operations.</summary>
public static class DeviceSelector
{
    /// <summary>Finds an eligible YubiKey; noninteractive diagnostics stay on stderr.</summary>
    public static Task<DeviceSelection?> SelectDeviceAsync(CancellationToken cancellationToken = default) =>
        new SharedSelector([ConnectionType.SmartCard], NonInteractiveSelection.FirstEligible,
            console: AnsiConsole.Profile.Capabilities.Interactive ? AnsiConsole.Console :
                AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) }))
            .SelectDeviceAsync(cancellationToken);
}
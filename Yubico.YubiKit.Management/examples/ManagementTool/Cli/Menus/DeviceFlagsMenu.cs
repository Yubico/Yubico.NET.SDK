// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

/// <summary>
/// CLI menu for configuring device flags.
/// </summary>
public static class DeviceFlagsMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Device Flags Configuration");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateManagementSessionAsync(cancellationToken: cancellationToken);

        var infoResult = await DeviceInfoQuery.GetDeviceInfoAsync(session, cancellationToken);
        if (!infoResult.Success || !infoResult.DeviceInfo.HasValue)
        {
            OutputHelpers.WriteError(infoResult.ErrorMessage ?? "Failed to get device info");
            return;
        }

        var info = infoResult.DeviceInfo.Value;

        // Display current flags
        AnsiConsole.MarkupLine("[bold]Current Device Flags[/]");
        OutputHelpers.WriteDeviceFlags("Flags", info.DeviceFlags);
        AnsiConsole.WriteLine();

        // Flag descriptions
        AnsiConsole.MarkupLine("[grey]Flag descriptions:[/]");
        AnsiConsole.MarkupLine("[grey]  • Remote Wakeup: USB remote wakeup is enabled[/]");
        AnsiConsole.MarkupLine("[grey]  • Touch Eject: CCID touch-eject feature (smart card ejected by default, touch to insert)[/]");
        AnsiConsole.WriteLine();

        // Build available flags
        var availableFlags = new Dictionary<string, DeviceFlags>
        {
            ["Remote Wakeup"] = DeviceFlags.RemoteWakeup,
            ["Touch Eject"] = DeviceFlags.TouchEject
        };

        // Get currently enabled flags
        var currentlyEnabled = new List<string>();
        if (info.DeviceFlags.HasFlag(DeviceFlags.RemoteWakeup)) currentlyEnabled.Add("Remote Wakeup");
        if (info.DeviceFlags.HasFlag(DeviceFlags.TouchEject)) currentlyEnabled.Add("Touch Eject");

        // Multi-select prompt
        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select device flags to enable:")
            .PageSize(10)
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
            .AddChoices(availableFlags.Keys);

        // Pre-select currently enabled flags
        foreach (var item in currentlyEnabled)
        {
            prompt.Select(item);
        }

        var selectedFlags = AnsiConsole.Prompt(prompt);

        // Convert to flags enum
        DeviceFlags newFlags = DeviceFlags.None;
        foreach (var name in selectedFlags)
        {
            if (availableFlags.TryGetValue(name, out var flag))
            {
                newFlags |= flag;
            }
        }

        // Prompt for lock code if device is locked
        byte[]? lockCode = null;
        if (info.IsLocked)
        {
            AnsiConsole.WriteLine();
            lockCode = LockCodePrompt.PromptForLockCode("Device is locked. Enter lock code to proceed:");
            if (lockCode is null)
            {
                return;
            }
        }

        try
        {
            // Confirm
            AnsiConsole.WriteLine();
            if (!OutputHelpers.ConfirmDangerous("change device flags (device will reboot)"))
            {
                OutputHelpers.WriteInfo("Operation cancelled");
                return;
            }

            // Apply configuration
            var result = await DeviceConfiguration.SetDeviceFlagsAsync(
                session,
                newFlags,
                lockCode,
                reboot: true,
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Device flags updated successfully");
                if (result.RebootRequired)
                {
                    OutputHelpers.WriteWarning("Device is rebooting. Please wait and reconnect.");
                }
            }
            else
            {
                OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to update device flags");
            }
        }
        finally
        {
            if (lockCode is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(lockCode);
            }
        }
    }
}

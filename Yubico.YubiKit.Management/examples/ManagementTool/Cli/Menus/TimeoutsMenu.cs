// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

/// <summary>
/// CLI menu for configuring device timeouts.
/// </summary>
public static class TimeoutsMenu
{
    public static async Task RunAsync(IYubiKeyManager manager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manager);
        
        OutputHelpers.WriteHeader("Timeout Configuration");

        var selection = await DeviceSelector.SelectDeviceAsync(manager, cancellationToken);
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

        // Display current timeouts
        AnsiConsole.MarkupLine("[bold]Current Timeouts[/]");
        OutputHelpers.WriteKeyValue("Auto-Eject Timeout", info.AutoEjectTimeout > 0 ? $"{info.AutoEjectTimeout} seconds" : "Disabled (0)");
        OutputHelpers.WriteKeyValue("Challenge-Response Timeout", info.ChallengeResponseTimeout.Length > 0 ? $"{info.ChallengeResponseTimeout.Span[0]} seconds" : "Unknown");
        AnsiConsole.WriteLine();

        // Prompt for new auto-eject timeout
        var autoEject = AnsiConsole.Prompt(
            new TextPrompt<ushort>("[blue]Auto-eject timeout (0-3600 seconds, 0 to disable):[/]")
                .DefaultValue(info.AutoEjectTimeout)
                .Validate(value => value <= 3600 
                    ? ValidationResult.Success() 
                    : ValidationResult.Error("[red]Value must be 0-3600 seconds[/]")));

        // Prompt for challenge-response timeout
        byte currentCrTimeout = info.ChallengeResponseTimeout.Length > 0 ? info.ChallengeResponseTimeout.Span[0] : (byte)15;
        var crTimeout = AnsiConsole.Prompt(
            new TextPrompt<byte>("[blue]Challenge-response timeout (0-60 seconds):[/]")
                .DefaultValue(currentCrTimeout)
                .Validate(value => value <= 60 
                    ? ValidationResult.Success() 
                    : ValidationResult.Error("[red]Value must be 0-60 seconds[/]")));

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
            if (!OutputHelpers.ConfirmDangerous("change device timeouts (device will reboot)"))
            {
                OutputHelpers.WriteInfo("Operation cancelled");
                return;
            }

            // Apply configuration
            var result = await DeviceConfiguration.SetTimeoutsAsync(
                session,
                autoEject,
                crTimeout,
                lockCode,
                reboot: true,
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Timeouts updated successfully");
                if (result.RebootRequired)
                {
                    OutputHelpers.WriteWarning("Device is rebooting. Please wait and reconnect.");
                }
            }
            else
            {
                OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to update timeouts");
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

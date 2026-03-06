// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Output;
using Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;
using Yubico.YubiKit.Management.Examples.ManagementTool.Features;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Menus;

/// <summary>
/// CLI menu for lock code management.
/// </summary>
public static class LockCodeMenu
{
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Lock Code Management");

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

        // Display current lock status
        AnsiConsole.MarkupLine("[bold]Current Lock Status[/]");
        if (info.IsLocked)
        {
            OutputHelpers.WriteKeyValueMarkup("Status", "[yellow]LOCKED[/] - Configuration changes require lock code");
        }
        else
        {
            OutputHelpers.WriteKeyValueMarkup("Status", "[green]UNLOCKED[/] - Configuration changes do not require lock code");
        }
        AnsiConsole.WriteLine();

        // Warning about lock codes
        AnsiConsole.MarkupLine("[red bold]⚠️  IMPORTANT WARNING ⚠️[/]");
        AnsiConsole.MarkupLine("[red]If you set a lock code and forget it, you will NOT be able to change[/]");
        AnsiConsole.MarkupLine("[red]the device configuration. The only recovery is a factory reset (5.6.0+),[/]");
        AnsiConsole.MarkupLine("[red]which erases ALL data on the device.[/]");
        AnsiConsole.WriteLine();

        // Show menu options based on lock status
        string[] choices = info.IsLocked
            ? ["Change Lock Code", "Remove Lock Code", "Cancel"]
            : ["Set Lock Code", "Cancel"];

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .AddChoices(choices));

        switch (choice)
        {
            case "Set Lock Code":
                await SetLockCodeAsync(session, cancellationToken);
                break;

            case "Change Lock Code":
                await ChangeLockCodeAsync(session, cancellationToken);
                break;

            case "Remove Lock Code":
                await RemoveLockCodeAsync(session, cancellationToken);
                break;

            default:
                OutputHelpers.WriteInfo("Operation cancelled");
                break;
        }
    }

    private static async Task SetLockCodeAsync(
        IManagementSession session,
        CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Set Lock Code");

        AnsiConsole.MarkupLine("[grey]Lock code must be exactly 16 bytes (32 hex characters).[/]");
        AnsiConsole.MarkupLine("[grey]Example: 00112233445566778899AABBCCDDEEFF[/]");
        AnsiConsole.WriteLine();

        byte[]? newLockCode = null;
        try
        {
            newLockCode = LockCodePrompt.PromptForNewLockCode("Enter new lock code:");
            if (newLockCode is null)
            {
                return;
            }

            AnsiConsole.WriteLine();
            if (!OutputHelpers.ConfirmDangerous("set a configuration lock code"))
            {
                OutputHelpers.WriteInfo("Operation cancelled");
                return;
            }

            var result = await DeviceConfiguration.SetLockCodeAsync(
                session,
                currentLockCode: null,
                newLockCode,
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Lock code set successfully");
                OutputHelpers.WriteWarning("Remember this lock code! You will need it to change configuration.");
            }
            else
            {
                OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to set lock code");
            }
        }
        finally
        {
            if (newLockCode is not null)
            {
                CryptographicOperations.ZeroMemory(newLockCode);
            }
        }
    }

    private static async Task ChangeLockCodeAsync(
        IManagementSession session,
        CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Change Lock Code");

        byte[]? currentLockCode = null;
        byte[]? newLockCode = null;
        try
        {
            currentLockCode = LockCodePrompt.PromptForLockCode("Enter current lock code:");
            if (currentLockCode is null)
            {
                return;
            }

            AnsiConsole.WriteLine();
            newLockCode = LockCodePrompt.PromptForNewLockCode("Enter new lock code:");
            if (newLockCode is null)
            {
                return;
            }

            AnsiConsole.WriteLine();
            if (!OutputHelpers.ConfirmDangerous("change the configuration lock code"))
            {
                OutputHelpers.WriteInfo("Operation cancelled");
                return;
            }

            var result = await DeviceConfiguration.SetLockCodeAsync(
                session,
                currentLockCode,
                newLockCode,
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Lock code changed successfully");
            }
            else
            {
                OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to change lock code");
            }
        }
        finally
        {
            if (currentLockCode is not null)
            {
                CryptographicOperations.ZeroMemory(currentLockCode);
            }

            if (newLockCode is not null)
            {
                CryptographicOperations.ZeroMemory(newLockCode);
            }
        }
    }

    private static async Task RemoveLockCodeAsync(
        IManagementSession session,
        CancellationToken cancellationToken)
    {
        OutputHelpers.WriteHeader("Remove Lock Code");

        byte[]? currentLockCode = null;
        try
        {
            currentLockCode = LockCodePrompt.PromptForLockCode("Enter current lock code:");
            if (currentLockCode is null)
            {
                return;
            }

            AnsiConsole.WriteLine();
            if (!OutputHelpers.ConfirmDangerous("remove the configuration lock code"))
            {
                OutputHelpers.WriteInfo("Operation cancelled");
                return;
            }

            var result = await DeviceConfiguration.RemoveLockCodeAsync(
                session,
                currentLockCode,
                cancellationToken);

            if (result.Success)
            {
                OutputHelpers.WriteSuccess("Lock code removed successfully");
                OutputHelpers.WriteInfo("Device configuration is now unlocked.");
            }
            else
            {
                OutputHelpers.WriteError(result.ErrorMessage ?? "Failed to remove lock code");
            }
        }
        finally
        {
            if (currentLockCode is not null)
            {
                CryptographicOperations.ZeroMemory(currentLockCode);
            }
        }
    }
}

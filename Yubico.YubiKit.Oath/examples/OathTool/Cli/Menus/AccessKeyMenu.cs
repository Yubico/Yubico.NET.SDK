// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Spectre.Console;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli.Menus;

/// <summary>
/// CLI menu for access key management: set password, unset password, reset.
/// </summary>
public static class AccessKeyMenu
{
    /// <summary>
    /// Sets or changes the OATH access key (password protection).
    /// </summary>
    public static async Task SetKeyAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Set OATH Password");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        // If currently locked, need to unlock first
        if (session.IsLocked)
        {
            if (!await UnlockSessionAsync(session))
            {
                return;
            }
        }

        AnsiConsole.MarkupLine("[grey]The password will be used to protect access to OATH credentials.[/]");
        AnsiConsole.MarkupLine("[grey]You will need this password each time you access the OATH applet.[/]");
        AnsiConsole.WriteLine();

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter new password:")
                .Secret());

        var confirmPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Confirm new password:")
                .Secret());

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            OutputHelpers.WriteError("Passwords do not match.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            OutputHelpers.WriteError("Password cannot be empty.");
            return;
        }

        AnsiConsole.WriteLine();

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(password);
            await session.SetKeyAsync(key, cancellationToken);
            OutputHelpers.WriteSuccess("OATH password set successfully.");
            OutputHelpers.WriteWarning("Remember this password! You will need it to access OATH credentials.");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to set password: {ex.Message}");
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    /// <summary>
    /// Removes the OATH access key (password protection).
    /// </summary>
    public static async Task UnsetKeyAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Remove OATH Password");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        if (!session.IsLocked)
        {
            OutputHelpers.WriteInfo("This device does not have a password set.");
            return;
        }

        if (!await UnlockSessionAsync(session))
        {
            return;
        }

        AnsiConsole.WriteLine();
        if (!OutputHelpers.ConfirmDangerous("remove the OATH password protection"))
        {
            OutputHelpers.WriteInfo("Operation cancelled.");
            return;
        }

        try
        {
            await session.UnsetKeyAsync(cancellationToken);
            OutputHelpers.WriteSuccess("OATH password removed. Credentials are now accessible without authentication.");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to remove password: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets the OATH applet, erasing all credentials and the access key.
    /// </summary>
    public static async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        OutputHelpers.WriteHeader("Reset OATH Application");

        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return;
        }

        OutputHelpers.WriteActiveDevice(selection.DisplayName);

        // Severe warning
        AnsiConsole.MarkupLine("[red bold]╔══════════════════════════════════════════════════════╗[/]");
        AnsiConsole.MarkupLine("[red bold]║               ⚠️  EXTREME WARNING ⚠️                  ║[/]");
        AnsiConsole.MarkupLine("[red bold]╠══════════════════════════════════════════════════════╣[/]");
        AnsiConsole.MarkupLine("[red bold]║  This will PERMANENTLY DESTROY:                      ║[/]");
        AnsiConsole.MarkupLine("[red bold]║                                                      ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • All TOTP/HOTP credentials on this device           ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • The OATH access key (password)                     ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  • The device identity (salt regenerated)              ║[/]");
        AnsiConsole.MarkupLine("[red bold]║                                                      ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  You will lose access to all accounts using these     ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  credentials unless you have backup codes.            ║[/]");
        AnsiConsole.MarkupLine("[red bold]║                                                      ║[/]");
        AnsiConsole.MarkupLine("[red bold]║  THIS CANNOT BE UNDONE.                               ║[/]");
        AnsiConsole.MarkupLine("[red bold]╚══════════════════════════════════════════════════════╝[/]");
        AnsiConsole.WriteLine();

        if (!OutputHelpers.ConfirmDestructive("erase all OATH credentials and reset the OATH application"))
        {
            OutputHelpers.WriteInfo("Reset cancelled.");
            return;
        }

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        try
        {
            await session.ResetAsync(cancellationToken);

            AnsiConsole.WriteLine();
            OutputHelpers.WriteSuccess("OATH application reset successfully.");
            OutputHelpers.WriteInfo("All credentials have been erased.");
            OutputHelpers.WriteInfo("The access key has been cleared.");
            OutputHelpers.WriteInfo($"New device ID: {session.DeviceId}");
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Reset failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Prompts for password and validates against the locked session.
    /// </summary>
    private static async Task<bool> UnlockSessionAsync(IOathSession session)
    {
        AnsiConsole.MarkupLine("[yellow]This device is password-protected.[/]");

        var password = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter current OATH password:")
                .Secret());

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(password);
            await session.ValidateAsync(key);
            OutputHelpers.WriteSuccess("Device unlocked.");
            AnsiConsole.WriteLine();
            return true;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Authentication failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }
}

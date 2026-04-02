// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Commands;

/// <summary>
/// Implements 'oath access' subcommands: change, remember, forget.
/// </summary>
public static class AccessCommand
{
    /// <summary>
    /// Changes the OATH access password. If --clear is set, removes the password.
    /// </summary>
    public static async Task<int> ChangeAsync(
        string? password = null,
        string? newPassword = null,
        bool clear = false,
        CancellationToken cancellationToken = default)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return 1;
        }

        await using var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        // Unlock if currently locked
        if (session.IsLocked)
        {
            if (!await OathSessionHelper.UnlockSessionAsync(session, password))
            {
                return 1;
            }
        }

        if (clear)
        {
            try
            {
                await session.UnsetKeyAsync(cancellationToken);
                OutputHelpers.WriteSuccess("OATH access password cleared.");
                return 0;
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError($"Failed to clear password: {ex.Message}");
                return 1;
            }
        }

        // Set or change password
        if (newPassword is null)
        {
            if (!Console.IsInputRedirected)
            {
                newPassword = SessionHelper.ReadPasswordMasked("Enter new OATH password");

                var confirm = SessionHelper.ReadPasswordMasked("Confirm new OATH password");

                if (!string.Equals(newPassword, confirm, StringComparison.Ordinal))
                {
                    OutputHelpers.WriteError("Passwords do not match.");
                    return 1;
                }
            }
            else
            {
                newPassword = Console.ReadLine();
            }
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            OutputHelpers.WriteError("New password cannot be empty. Use --clear to remove password protection.");
            return 1;
        }

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(newPassword);
            await session.SetKeyAsync(key, cancellationToken);
            OutputHelpers.WriteSuccess("OATH access password set.");
            return 0;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to set password: {ex.Message}");
            return 1;
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
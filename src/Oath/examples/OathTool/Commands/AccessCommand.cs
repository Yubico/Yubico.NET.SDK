// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Oath.Credentials;
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
    /// <param name="passwordBytes">
    /// Current password bytes for unlocking (caller retains ownership). Null to prompt interactively.
    /// </param>
    /// <param name="newPasswordBytes">
    /// New password bytes to set (caller retains ownership). Null to prompt interactively.
    /// </param>
    /// <param name="clear">If true, removes the password instead of setting a new one.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<int> ChangeAsync(
        IMemoryOwner<byte>? passwordBytes = null,
        IMemoryOwner<byte>? newPasswordBytes = null,
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
            if (!await OathSessionHelper.UnlockSessionAsync(session, passwordBytes))
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
        IMemoryOwner<byte>? prompted = null;
        byte[]? key = null;

        try
        {
            if (newPasswordBytes is null)
            {
                var reader = new ConsoleCredentialReader();
                var options = OathCredentialOptions.ForOathPassword() with
                {
                    Prompt = "Enter new OATH password: ",
                    ConfirmPrompt = "Confirm new OATH password: "
                };

                prompted = reader.ReadCredentialWithConfirmation(options);
                if (prompted is null)
                {
                    OutputHelpers.WriteError(
                        "New password cannot be empty. Use --clear to remove password protection.");
                    return 1;
                }

                newPasswordBytes = prompted;
            }

            key = session.DeriveKey(newPasswordBytes.Memory);
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

            prompted?.Dispose();
        }
    }
}
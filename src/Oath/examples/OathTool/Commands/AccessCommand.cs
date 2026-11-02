// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Commands.Infrastructure;
using Yubico.YubiKit.Oath.Credentials;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Commands;

/// <summary>
/// Implements 'oath access change' for setting, changing, or clearing the OATH access password.
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
    /// <param name="services">Application I/O dependencies; null uses the console and device selector.</param>
    public static async Task<int> ChangeAsync(
        IMemoryOwner<byte>? passwordBytes = null,
        IMemoryOwner<byte>? newPasswordBytes = null,
        bool clear = false,
        CancellationToken cancellationToken = default,
        OathToolServices? services = null)
    {
        services ??= OathToolServices.Default;
        cancellationToken.ThrowIfCancellationRequested();
        await using var session = await services.OpenSession(cancellationToken);
        if (session is null)
        {
            return 1;
        }

        // Unlock if currently locked
        if (session.IsLocked)
        {
            if (!await OathSessionHelper.UnlockSessionAsync(session, passwordBytes, cancellationToken, services.CredentialReader))
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
            catch (Exception ex) when (ex is not OperationCanceledException)
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
            cancellationToken.ThrowIfCancellationRequested();
            if (newPasswordBytes is null)
            {
                var reader = services.CredentialReader;
                var options = OathCredentialOptions.ForOathPassword() with
                {
                    Prompt = "Enter new OATH password: ",
                    ConfirmPrompt = "Confirm new OATH password: "
                };

                prompted = reader.ReadCredentialWithConfirmation(options, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (prompted is null)
                {
                    OutputHelpers.WriteInfo(
                        "Password entry cancelled or not confirmed. Use --clear to remove password protection.");
                    return ExitCode.UserCancelled;
                }

                newPasswordBytes = prompted;
            }

            key = session.DeriveKey(newPasswordBytes.Memory);
            cancellationToken.ThrowIfCancellationRequested();
            await session.SetKeyAsync(key, cancellationToken);
            OutputHelpers.WriteSuccess("OATH access password set.");
            return 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
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
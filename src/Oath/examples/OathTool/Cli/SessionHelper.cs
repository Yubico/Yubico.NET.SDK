// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Oath.Credentials;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli;

/// <summary>
/// Shared helpers for device selection, session creation, and password unlock.
/// </summary>
public static class OathSessionHelper
{
    /// <summary>
    /// Selects a device and creates an OATH session. If the session is locked,
    /// prompts for (or uses the provided) password bytes to unlock it.
    /// Returns null if device selection fails or unlock fails.
    /// </summary>
    /// <param name="passwordBytes">
    /// Pre-encoded password bytes (e.g., from CLI flag). Caller retains ownership; this method does not dispose.
    /// If null and the session is locked, the user will be prompted interactively.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task<(DeviceSelection Selection, OathSession Session)?> CreateUnlockedSessionAsync(
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        var selection = await DeviceSelector.SelectDeviceAsync(cancellationToken);
        if (selection is null)
        {
            return null;
        }

        var session = await selection.Device.CreateOathSessionAsync(
            cancellationToken: cancellationToken);

        if (session.IsLocked)
        {
            if (!await UnlockSessionAsync(session, passwordBytes))
            {
                await session.DisposeAsync();
                return null;
            }
        }

        return (selection, session);
    }

    /// <summary>
    /// Unlocks a password-protected OATH session.
    /// Uses the provided password bytes, or prompts interactively via <see cref="ConsoleCredentialReader"/> if null.
    /// </summary>
    /// <param name="session">The locked OATH session.</param>
    /// <param name="passwordBytes">
    /// Pre-encoded password bytes. Caller retains ownership; this method does not dispose.
    /// If null, the user is prompted interactively.
    /// </param>
    public static async Task<bool> UnlockSessionAsync(
        IOathSession session,
        IMemoryOwner<byte>? passwordBytes = null)
    {
        IMemoryOwner<byte>? prompted = null;
        byte[]? key = null;

        try
        {
            if (passwordBytes is null)
            {
                var reader = new ConsoleCredentialReader();
                prompted = reader.ReadCredential(OathCredentialOptions.ForOathPassword());
                if (prompted is null)
                {
                    OutputHelpers.WriteError("Password required but not provided.");
                    return false;
                }

                passwordBytes = prompted;
            }

            key = session.DeriveKey(passwordBytes.Memory);
            await session.ValidateAsync(key);
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

            prompted?.Dispose();
        }
    }
}
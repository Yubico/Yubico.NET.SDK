// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Cli;
using Yubico.YubiKit.Cli.Shared.Device;
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
    /// prompts for (or uses the provided) password to unlock it.
    /// Returns null if device selection fails or unlock fails.
    /// </summary>
    public static async Task<(DeviceSelection Selection, OathSession Session)?> CreateUnlockedSessionAsync(
        string? password = null,
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
            if (!await UnlockSessionAsync(session, password))
            {
                await session.DisposeAsync();
                return null;
            }
        }

        return (selection, session);
    }

    /// <summary>
    /// Unlocks a password-protected OATH session.
    /// Uses the provided password string, or prompts on stdin if null.
    /// </summary>
    public static async Task<bool> UnlockSessionAsync(
        IOathSession session,
        string? password = null)
    {
        password ??= SessionHelper.ReadPasswordMasked("Enter OATH password");

        if (string.IsNullOrEmpty(password))
        {
            OutputHelpers.WriteError("Password required but not provided.");
            return false;
        }

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(password);
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
        }
    }
}
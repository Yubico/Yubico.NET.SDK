// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Device;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Prompts;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Cli;

/// <summary>
/// Shared helpers for device selection, session creation, and password unlock.
/// </summary>
public static class SessionHelper
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
        if (password is null)
        {
            if (!Console.IsInputRedirected)
            {
                Console.Error.Write("Enter OATH password: ");
                password = ReadPasswordMasked();
                Console.Error.WriteLine();
            }
            else
            {
                password = Console.ReadLine();
            }
        }

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

    /// <summary>
    /// Reads a password from the console with masking (no echo).
    /// </summary>
    private static string ReadPasswordMasked()
    {
        var password = new List<char>();
        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            if (keyInfo.Key is ConsoleKey.Enter)
            {
                break;
            }

            if (keyInfo.Key is ConsoleKey.Backspace && password.Count > 0)
            {
                password.RemoveAt(password.Count - 1);
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                password.Add(keyInfo.KeyChar);
            }
        }

        return new string(password.ToArray());
    }
}
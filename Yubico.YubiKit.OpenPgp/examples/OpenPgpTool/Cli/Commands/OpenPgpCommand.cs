// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using Spectre.Console.Cli;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Base class for all OpenPGP CLI commands. Handles device selection
///     and session lifecycle.
/// </summary>
public abstract class OpenPgpCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        YubiKeyManager.StartMonitoring();

        try
        {
            var selection = await DeviceSelector.SelectDeviceAsync();
            if (selection is null)
            {
                OutputHelpers.WriteError("No YubiKey detected.");
                return 1;
            }

            OutputHelpers.WriteActiveDevice(selection.DisplayName);

            await using var session = await selection.Device.CreateOpenPgpSessionAsync();
            return await ExecuteCommandAsync(context, settings, session);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError(ex.Message);
            return 1;
        }
        finally
        {
            await YubiKeyManager.ShutdownAsync();
        }
    }

    /// <summary>
    ///     Executes the command logic with an already-opened OpenPGP session.
    /// </summary>
    protected abstract Task<int> ExecuteCommandAsync(
        CommandContext context,
        TSettings settings,
        IOpenPgpSession session);

    /// <summary>
    ///     Parses a key argument string (sig/dec/aut/att) into a <see cref="KeyRef" />.
    /// </summary>
    protected static KeyRef ParseKeyRef(string key) =>
        key.ToLowerInvariant() switch
        {
            "sig" => KeyRef.Sig,
            "dec" => KeyRef.Dec,
            "aut" => KeyRef.Aut,
            "att" => KeyRef.Att,
            _ => throw new ArgumentException($"Invalid key slot: {key}. Must be sig, dec, aut, or att.")
        };

    /// <summary>
    ///     Gets the display name for a key slot.
    /// </summary>
    protected static string FormatKeyRef(KeyRef keyRef) =>
        keyRef switch
        {
            KeyRef.Sig => "Signature (SIG)",
            KeyRef.Dec => "Decryption (DEC)",
            KeyRef.Aut => "Authentication (AUT)",
            KeyRef.Att => "Attestation (ATT)",
            _ => keyRef.ToString()
        };

    /// <summary>
    ///     Gets a PIN value, using the provided option or prompting interactively.
    /// </summary>
    protected static string GetPin(string? provided, string promptLabel)
    {
        if (!string.IsNullOrEmpty(provided))
        {
            return provided;
        }

        return OutputHelpers.PromptPin(promptLabel);
    }

    /// <summary>
    ///     Confirms a destructive action. If <paramref name="force" /> is true, skips the prompt.
    /// </summary>
    protected static bool ConfirmAction(string action, bool force)
    {
        if (force)
        {
            return true;
        }

        return OutputHelpers.ConfirmDangerous(action);
    }
}
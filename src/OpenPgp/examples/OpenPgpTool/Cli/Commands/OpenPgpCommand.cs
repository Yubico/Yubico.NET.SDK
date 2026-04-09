// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.OpenPgp.Credentials;
using Yubico.YubiKit.Core.Utils;
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

    private static readonly ConsoleCredentialReader CredentialReader = new();

    /// <summary>
    ///     Gets a credential value as secure bytes, using the provided option or prompting interactively.
    /// </summary>
    protected static IMemoryOwner<byte>? GetCredential(string? provided, CredentialReaderOptions options)
    {
        if (!string.IsNullOrEmpty(provided))
        {
            return DisposableArrayPoolBuffer.CreateFromSpan(Encoding.UTF8.GetBytes(provided));
        }

        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    ///     Gets the OpenPGP User PIN (6-127 characters).
    /// </summary>
    protected static IMemoryOwner<byte>? GetPin(string? provided) =>
        GetCredential(provided, OpenPgpCredentialOptions.ForOpenPgpPin());

    /// <summary>
    ///     Gets the OpenPGP Admin PIN (8-127 characters).
    /// </summary>
    protected static IMemoryOwner<byte>? GetAdminPin(string? provided) =>
        GetCredential(provided, OpenPgpCredentialOptions.ForOpenPgpAdminPin());

    /// <summary>
    ///     Gets the OpenPGP Reset Code (8-127 characters).
    /// </summary>
    protected static IMemoryOwner<byte>? GetResetCode(string? provided) =>
        GetCredential(provided, OpenPgpCredentialOptions.ForOpenPgpResetCode());

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
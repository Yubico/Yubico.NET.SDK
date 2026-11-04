// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console.Cli;
using System.Buffers;
using System.Text;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.OpenPgp.Credentials;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Output;
using Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Prompts;

namespace Yubico.YubiKit.OpenPgp.Examples.OpenPgpTool.Cli.Commands;

/// <summary>
///     Base class for all OpenPGP CLI commands. Handles device selection
///     and session lifecycle.
/// </summary>
/// <remarks>
///     Uses static DeviceSelector, whose noninteractive path selects the first SmartCard device.
///     Confirm the intended key before mutation. The session has no user-presence prompt installed;
///     applications needing touch UI should create a session with their own prompt.
/// </remarks>
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
    ///     Gets disposable credential bytes from an option or interactive reader.
    /// </summary>
    /// <remarks>
    ///     Disposing the result clears its byte storage. A supplied command-line option remains in an immutable
    ///     string and can be exposed through process arguments; prefer application-owned input buffers for secrets.
    /// </remarks>
    protected static IMemoryOwner<byte>? GetCredential(string? provided, CredentialReaderOptions options)
    {
        if (!string.IsNullOrEmpty(provided))
        {
            // Encode directly to avoid extra heap copy of credential bytes
            var byteCount = Encoding.UTF8.GetByteCount(provided);
            var buffer = new DisposableArrayPoolBuffer(byteCount, clearOnCreate: false);
            Encoding.UTF8.GetBytes(provided, buffer.Span);
            return buffer;
        }

        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    ///     Gets User PIN input with reader limits 6–127; the application also applies card byte/profile limits.
    /// </summary>
    protected static IMemoryOwner<byte>? GetPin(string? provided) =>
        GetCredential(provided, OpenPgpCredentialOptions.ForOpenPgpPin());

    /// <summary>
    ///     Gets Admin PIN input with reader limits 8–127; the application also applies card byte/profile limits.
    /// </summary>
    protected static IMemoryOwner<byte>? GetAdminPin(string? provided) =>
        GetCredential(provided, OpenPgpCredentialOptions.ForOpenPgpAdminPin());

    /// <summary>
    ///     Gets Reset Code input with reader limits 8–127; the application also applies card byte/profile limits.
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
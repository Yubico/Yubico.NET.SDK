// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Spectre.Console;
using System.Buffers;
using Yubico.YubiKit.Core.Credentials;
using SharedConfirm = Yubico.YubiKit.Cli.Shared.Output.ConfirmationPrompts;
using SharedOutput = Yubico.YubiKit.Cli.Shared.Output.OutputHelpers;

namespace Yubico.YubiKit.YubiHsm.Examples.HsmAuthTool.Cli.Output;

/// <summary>
/// Provides Spectre.Console formatting utilities for consistent output.
/// Delegates common methods to the shared CLI library.
/// </summary>
public static class OutputHelpers
{
    private static readonly ConsoleCredentialReader CredentialReader = new();

    internal static IMemoryOwner<byte>? ReadPassword(string prompt, CancellationToken token) =>
        ReadMasked(CredentialReaderOptions.ForPassphrase() with { Prompt = prompt + ": " }, token);

    internal static IMemoryOwner<byte>? ReadDerivationPassword(CancellationToken token,
        Func<CredentialReaderOptions, CancellationToken, IMemoryOwner<byte>?>? read = null)
    {
        const int sampleMaxLength = 128;
        var options = CredentialReaderOptions.ForPassphrase() with
        {
            Prompt = "Derivation password (PBKDF2): ",
            // The console reader ignores further keystrokes at capacity. Reserve a slot so an
            // overlong input cannot be accepted as an exact-limit password after truncation.
            MaxLength = sampleMaxLength + 1
        };
        var password = (read ?? ReadMasked)(options, token);
        if (password is null || options.Encoding.GetCharCount(password.Memory.Span) <= sampleMaxLength)
            return password;

        password.Dispose();
        throw new ArgumentException($"The demo accepts derivation passwords of at most {sampleMaxLength} UTF-16 code units.");
    }

    internal static IMemoryOwner<byte>? ReadKey(string prompt, int length, CancellationToken token,
        Func<CredentialReaderOptions, CancellationToken, IMemoryOwner<byte>?>? read = null) =>
        (read ?? ReadMasked)(CredentialReaderOptions.ForHexKey(length) with
        {
            Prompt = prompt + ": ",
            // Ignore separators before they can consume capacity. Keep one extra digit so the
            // existing hex parser rejects overflow as odd-length rather than accepting a prefix.
            CharacterFilter = char.IsAsciiHexDigit,
            MaxLength = length * 2 + 1
        }, token);

    private static IMemoryOwner<byte>? ReadMasked(CredentialReaderOptions options, CancellationToken token)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
            throw new InvalidOperationException("Secret input requires an interactive console for masking.");

        return CredentialReader.ReadCredential(options, token);
    }

    // -- Delegated to shared library --

    /// <inheritdoc cref="SharedOutput.WriteHeader"/>
    public static void WriteHeader(string title) => SharedOutput.WriteHeader(title);

    /// <inheritdoc cref="SharedOutput.WriteSuccess"/>
    public static void WriteSuccess(string message) => SharedOutput.WriteSuccess(message);

    /// <inheritdoc cref="SharedOutput.WriteError"/>
    public static void WriteError(string message) => SharedOutput.WriteError(message);

    /// <inheritdoc cref="SharedOutput.WriteWarning"/>
    public static void WriteWarning(string message) => SharedOutput.WriteWarning(message);

    /// <inheritdoc cref="SharedOutput.WriteInfo"/>
    public static void WriteInfo(string message) => SharedOutput.WriteInfo(message);

    /// <inheritdoc cref="SharedOutput.WriteKeyValue"/>
    public static void WriteKeyValue(string key, string? value) => SharedOutput.WriteKeyValue(key, value);

    /// <inheritdoc cref="SharedOutput.WriteHex(string, ReadOnlySpan{byte})"/>
    public static void WriteHex(string label, ReadOnlySpan<byte> data) => SharedOutput.WriteHex(label, data);

    /// <inheritdoc cref="SharedOutput.CreateTable"/>
    public static Table CreateTable(params string[] columns) => SharedOutput.CreateTable(columns);

    /// <inheritdoc cref="SharedConfirm.ConfirmDestructive"/>
    public static bool ConfirmDestructive(string action, string confirmationWord = "RESET") =>
        SharedConfirm.ConfirmDestructive(action, confirmationWord);

    /// <inheritdoc cref="SharedOutput.WriteActiveDevice"/>
    public static void WriteActiveDevice(string deviceDisplayName) =>
        SharedOutput.WriteActiveDevice(deviceDisplayName);
}
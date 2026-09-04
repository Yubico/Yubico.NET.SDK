// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.OpenPgp;

namespace Yubico.YubiKit.Cli.Commands.OpenPgp;

/// <summary>
///     Shared helpers for OpenPGP commands: key slot parsing, PIN prompts,
///     and confirmation flows.
/// </summary>
public static class OpenPgpHelpers
{
    public static KeyRef ParseKeyRef(string key) =>
        key.ToLowerInvariant() switch
        {
            "sig" => KeyRef.Sig,
            "dec" => KeyRef.Dec,
            "aut" => KeyRef.Aut,
            "att" => KeyRef.Att,
            _ => throw new ArgumentException($"Invalid key slot: {key}. Must be sig, dec, aut, or att.")
        };

    public static string FormatKeyRef(KeyRef keyRef) =>
        keyRef switch
        {
            KeyRef.Sig => "Signature (SIG)",
            KeyRef.Dec => "Decryption (DEC)",
            KeyRef.Aut => "Authentication (AUT)",
            KeyRef.Att => "Attestation (ATT)",
            _ => keyRef.ToString()
        };

    /// <summary>
    /// Resolves a PIN from the command line when supplied, otherwise prompts for it.
    /// </summary>
    /// <returns>
    /// Owned UTF-8 bytes that the caller must dispose to zero, or <see langword="null"/> if the
    /// user declined to supply a PIN.
    /// </returns>
    public static IMemoryOwner<byte>? GetPin(string? provided, string promptLabel) =>
        PinPrompt.Resolve(provided, promptLabel);

    public static bool ConfirmAction(string action, bool force)
    {
        if (force)
        {
            return true;
        }

        return ConfirmationPrompts.ConfirmDangerous(action);
    }
}
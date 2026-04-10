// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using System.Text;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Oath;

namespace Yubico.YubiKit.Cli.YkTool.Commands.Oath;

internal static class OathHelpers
{
    public static string FormatCredentialName(string? issuer, string name) =>
        issuer is not null ? $"{issuer}:{name}" : name;

    public static async Task<bool> UnlockIfNeededAsync(IOathSession session, string? password)
    {
        if (!session.IsLocked)
        {
            return true;
        }

        if (string.IsNullOrEmpty(password))
        {
            password = PinPrompt.PromptForPin("OATH password");
        }

        if (string.IsNullOrEmpty(password))
        {
            OutputHelpers.WriteError("OATH application is password-protected. Provide --password.");
            return false;
        }

        byte[]? key = null;
        try
        {
            key = session.DeriveKey(Encoding.UTF8.GetBytes(password));
            await session.ValidateAsync(key);
            return true;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to unlock OATH session: {ex.Message}");
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

    public static async Task<Credential?> FindCredentialAsync(
        IOathSession session, string query, CancellationToken ct = default)
    {
        var credentials = await session.ListCredentialsAsync(ct);

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteError("No credentials stored on this device.");
            return null;
        }

        var matches = credentials
            .Where(c => FormatCredentialName(c.Issuer, c.Name)
                .Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            OutputHelpers.WriteError($"No credential matching '{query}'.");
            return null;
        }

        if (matches.Count > 1)
        {
            OutputHelpers.WriteError($"Multiple credentials match '{query}'. Be more specific:");
            foreach (var match in matches)
            {
                OutputHelpers.WriteKeyValue("  -", FormatCredentialName(match.Issuer, match.Name));
            }

            return null;
        }

        return matches[0];
    }

    public static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        input = input.ToUpperInvariant().Replace(" ", "").TrimEnd('=');

        if (input.Length == 0)
        {
            return [];
        }

        int byteCount = input.Length * 5 / 8;
        byte[] result = new byte[byteCount];

        int buffer = 0;
        int bitsInBuffer = 0;
        int index = 0;

        foreach (char c in input)
        {
            int val = alphabet.IndexOf(c);
            if (val < 0)
            {
                throw new ArgumentException($"Invalid Base32 character: '{c}'.");
            }

            buffer = (buffer << 5) | val;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                result[index++] = (byte)(buffer >> bitsInBuffer);
                buffer &= (1 << bitsInBuffer) - 1;
            }
        }

        return result;
    }
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Security.Cryptography;
using Yubico.YubiKit.Cli.Shared.Output;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Oath;

namespace Yubico.YubiKit.Cli.Commands.Oath;

public static class OathHelpers
{
    internal const string ArgvPasswordWarning =
        "Warning: Passing passwords on the command line is inherently insecure and intended only for testing or demos. Prefer the interactive prompt, for example: yk oath accounts list";

    public static string FormatCredentialName(string? issuer, string name) =>
        issuer is not null ? $"{issuer}:{name}" : name;

    public static Task<bool> UnlockIfNeededAsync(IOathSession session, string? password) =>
        UnlockIfNeededAsync(session, password, () => PinPrompt.PromptForCredential("OATH password"));

    internal static async Task<bool> UnlockIfNeededAsync(
        IOathSession session,
        string? password,
        Func<IMemoryOwner<byte>?> promptCredentialFactory)
    {
        if (!session.IsLocked)
        {
            return true;
        }

        var hasArgvPassword = !string.IsNullOrEmpty(password);
        IMemoryOwner<byte>? passwordBytes = null;
        byte[]? key = null;
        try
        {
            if (hasArgvPassword)
            {
                Console.Error.WriteLine(ArgvPasswordWarning);
                passwordBytes = SecureCredential.FromUtf8String(password!);
            }
            else
            {
                passwordBytes = promptCredentialFactory();
            }

            if (passwordBytes is null)
            {
                OutputHelpers.WriteError("No OATH password was supplied.");
                return false;
            }

            key = session.DeriveKey(passwordBytes.Memory);
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

            passwordBytes?.Dispose();
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

    /// <summary>Decodes nonempty OATH secret material. The caller owns and must clear returned bytes.</summary>
    public static byte[] Base32Decode(string input)
    {
        byte[] decoded = Base32.Decode(input.AsSpan());
        if (decoded.Length == 0)
        {
            throw new ArgumentException("OATH secret material must not be empty.", nameof(input));
        }

        return decoded;
    }
}
// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli;
using Yubico.YubiKit.Oath.Examples.OathTool.Cli.Output;

namespace Yubico.YubiKit.Oath.Examples.OathTool.Commands;

/// <summary>
/// Implements 'oath accounts' subcommands: list, add, code, delete, rename, uri.
/// </summary>
public static class AccountsCommand
{
    /// <summary>
    /// Lists all OATH credentials on the device.
    /// </summary>
    public static async Task<int> ListAsync(
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken);
        if (result is null)
        {
            return 1;
        }

        var (_, session) = result.Value;
        await using (session)
        {
            var credentials = await session.ListCredentialsAsync(cancellationToken);

            if (credentials.Count == 0)
            {
                OutputHelpers.WriteInfo("No credentials stored on this device.");
                return 0;
            }

            foreach (var cred in credentials.OrderBy(c =>
                         OutputHelpers.FormatCredentialName(c.Issuer, c.Name)))
            {
                Console.WriteLine(OutputHelpers.FormatCredentialName(cred.Issuer, cred.Name));
            }

            return 0;
        }
    }

    /// <summary>
    /// Adds a credential from explicit parameters.
    /// </summary>
    public static async Task<int> AddAsync(
        string name,
        string secret,
        string? issuer = null,
        OathType oathType = OathType.Totp,
        int digits = 6,
        OathHashAlgorithm algorithm = OathHashAlgorithm.Sha1,
        int period = 30,
        bool touch = false,
        bool force = false,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        byte[] secretBytes;
        try
        {
            secretBytes = Base32Decode(secret);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Invalid Base32 secret: {ex.Message}");
            return 1;
        }

        var credentialData = new CredentialData
        {
            Name = name,
            OathType = oathType,
            HashAlgorithm = algorithm,
            Secret = secretBytes,
            Digits = digits,
            Period = period,
            Issuer = issuer
        };

        var displayName = OutputHelpers.FormatCredentialName(credentialData.Issuer, credentialData.Name);

        if (!force)
        {
            Console.Error.Write($"Add credential {displayName}? [y/N] ");
            var response = Console.ReadLine();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                OutputHelpers.WriteInfo("Add cancelled.");
                return 1;
            }
        }

        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken);
        if (result is null)
        {
            return 1;
        }

        var (_, session) = result.Value;
        await using (session)
        {
            try
            {
                await session.PutCredentialAsync(credentialData, touch, cancellationToken);
                OutputHelpers.WriteSuccess($"Credential added: {displayName}");
                return 0;
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError($"Failed to add credential: {ex.Message}");
                return 1;
            }
        }
    }

    /// <summary>
    /// Generates codes. If a query is given, filters by name.
    /// With no query, calculates all codes (like ykman oath accounts code).
    /// </summary>
    public static async Task<int> CodeAsync(
        string? query = null,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken);
        if (result is null)
        {
            return 1;
        }

        var (_, session) = result.Value;
        await using (session)
        {
            if (query is not null)
            {
                return await CalculateSingleCodeAsync(session, query, cancellationToken);
            }

            return await CalculateAllCodesAsync(session, cancellationToken);
        }
    }

    /// <summary>
    /// Deletes a credential by name.
    /// </summary>
    public static async Task<int> DeleteAsync(
        string name,
        bool force = false,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken);
        if (result is null)
        {
            return 1;
        }

        var (_, session) = result.Value;
        await using (session)
        {
            var credential = await FindCredentialAsync(session, name, cancellationToken);
            if (credential is null)
            {
                return 1;
            }

            var displayName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);

            if (!force)
            {
                Console.Error.Write($"Delete credential {displayName}? [y/N] ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    OutputHelpers.WriteInfo("Delete cancelled.");
                    return 1;
                }
            }

            try
            {
                await session.DeleteCredentialAsync(credential, cancellationToken);
                OutputHelpers.WriteSuccess($"Credential deleted: {displayName}");
                return 0;
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError($"Failed to delete credential: {ex.Message}");
                return 1;
            }
        }
    }

    /// <summary>
    /// Renames a credential.
    /// </summary>
    public static async Task<int> RenameAsync(
        string name,
        string newName,
        bool force = false,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken);
        if (result is null)
        {
            return 1;
        }

        var (_, session) = result.Value;
        await using (session)
        {
            var credential = await FindCredentialAsync(session, name, cancellationToken);
            if (credential is null)
            {
                return 1;
            }

            var displayName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);

            if (!force)
            {
                Console.Error.Write($"Rename {displayName} to {newName}? [y/N] ");
                var response = Console.ReadLine();
                if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                {
                    OutputHelpers.WriteInfo("Rename cancelled.");
                    return 1;
                }
            }

            // Parse new name for issuer:name format
            string? newIssuer = null;
            string newAccountName = newName;
            if (newName.Contains(':'))
            {
                int colonIndex = newName.IndexOf(':');
                newIssuer = newName[..colonIndex];
                newAccountName = newName[(colonIndex + 1)..];
            }

            try
            {
                var renamed = await session.RenameCredentialAsync(
                    credential, newIssuer, newAccountName, cancellationToken);
                var renamedDisplay = OutputHelpers.FormatCredentialName(renamed.Issuer, renamed.Name);
                OutputHelpers.WriteSuccess($"Renamed: {displayName} -> {renamedDisplay}");
                return 0;
            }
            catch (NotSupportedException)
            {
                OutputHelpers.WriteError("Rename requires firmware 5.3.1 or later.");
                return 1;
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError($"Failed to rename credential: {ex.Message}");
                return 1;
            }
        }
    }

    /// <summary>
    /// Adds a credential from an otpauth:// URI.
    /// </summary>
    public static async Task<int> UriAsync(
        string uri,
        bool touch = false,
        bool force = false,
        IMemoryOwner<byte>? passwordBytes = null,
        CancellationToken cancellationToken = default)
    {
        CredentialData credentialData;
        try
        {
            credentialData = CredentialData.ParseUri(uri);
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Invalid otpauth URI: {ex.Message}");
            return 1;
        }

        var displayName = OutputHelpers.FormatCredentialName(credentialData.Issuer, credentialData.Name);

        if (!force)
        {
            Console.Error.Write($"Add credential {displayName} ({credentialData.OathType})? [y/N] ");
            var response = Console.ReadLine();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                OutputHelpers.WriteInfo("Add cancelled.");
                return 1;
            }
        }

        var result = await OathSessionHelper.CreateUnlockedSessionAsync(passwordBytes, cancellationToken);
        if (result is null)
        {
            return 1;
        }

        var (_, session) = result.Value;
        await using (session)
        {
            try
            {
                await session.PutCredentialAsync(credentialData, touch, cancellationToken);
                OutputHelpers.WriteSuccess($"Credential added: {displayName}");
                return 0;
            }
            catch (Exception ex)
            {
                OutputHelpers.WriteError($"Failed to add credential: {ex.Message}");
                return 1;
            }
        }
    }

    /// <summary>
    /// Calculates a single code matching the query.
    /// </summary>
    private static async Task<int> CalculateSingleCodeAsync(
        IOathSession session,
        string query,
        CancellationToken cancellationToken)
    {
        var credential = await FindCredentialAsync(session, query, cancellationToken);
        if (credential is null)
        {
            return 1;
        }

        try
        {
            var code = await session.CalculateCodeAsync(credential, cancellationToken: cancellationToken);
            var displayName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);
            Console.WriteLine($"{displayName}  {code.Value}");
            return 0;
        }
        catch (Exception ex)
        {
            OutputHelpers.WriteError($"Failed to calculate code: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Calculates codes for all credentials.
    /// </summary>
    private static async Task<int> CalculateAllCodesAsync(
        IOathSession session,
        CancellationToken cancellationToken)
    {
        var results = await session.CalculateAllAsync(cancellationToken: cancellationToken);

        if (results.Count == 0)
        {
            OutputHelpers.WriteInfo("No credentials stored on this device.");
            return 0;
        }

        foreach (var (credential, code) in results.OrderBy(r =>
                     OutputHelpers.FormatCredentialName(r.Key.Issuer, r.Key.Name)))
        {
            var displayName = OutputHelpers.FormatCredentialName(credential.Issuer, credential.Name);
            string codeValue = code switch
            {
                not null => code.Value,
                _ when credential.TouchRequired == true => "[Touch credential]",
                _ => "[HOTP credential]"
            };
            Console.WriteLine($"{displayName}  {codeValue}");
        }

        return 0;
    }

    /// <summary>
    /// Finds a credential by partial name match.
    /// </summary>
    private static async Task<Credential?> FindCredentialAsync(
        IOathSession session,
        string query,
        CancellationToken cancellationToken)
    {
        var credentials = await session.ListCredentialsAsync(cancellationToken);

        if (credentials.Count == 0)
        {
            OutputHelpers.WriteError("No credentials stored on this device.");
            return null;
        }

        var matches = credentials
            .Where(c => OutputHelpers.FormatCredentialName(c.Issuer, c.Name)
                .Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            OutputHelpers.WriteError($"No credential matching '{query}'.");
            return null;
        }

        if (matches.Count > 1)
        {
            OutputHelpers.WriteError(
                $"Multiple credentials match '{query}'. Be more specific:");
            foreach (var match in matches)
            {
                Console.Error.WriteLine($"  {OutputHelpers.FormatCredentialName(match.Issuer, match.Name)}");
            }

            return null;
        }

        return matches[0];
    }

    /// <summary>
    /// Decodes a Base32-encoded string (RFC 4648), tolerating missing padding and whitespace.
    /// </summary>
    private static byte[] Base32Decode(string input)
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
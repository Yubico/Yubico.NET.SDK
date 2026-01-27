// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Spectre.Console;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Piv.Examples.PivTool.Cli.Output;

namespace Yubico.YubiKit.Piv.Examples.PivTool.Cli.Prompts;

/// <summary>
/// Provides secure credential prompting with proper memory zeroing.
/// </summary>
public static class PinPrompt
{
    private const int MaxPinLength = 8;
    private const int MaxPukLength = 8;
    private const int ManagementKeyLength3Des = 24;
    private const int ManagementKeyLengthAes128 = 16;
    private const int ManagementKeyLengthAes192 = 24;
    private const int ManagementKeyLengthAes256 = 32;
    private const int PbkdfIterations = 600_000; // OWASP recommended

    private const string DefaultPinValue = "123456";
    private const string DefaultPukValue = "12345678";

    private static readonly ConsoleCredentialReader CredentialReader = new();

    /// <summary>
    /// Prompts for a PIN and returns the bytes. Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PIN as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetPin(string prompt = "Enter PIN: ")
    {
        var options = new CredentialReaderOptions
        {
            Prompt = prompt,
            MinLength = 6,
            MaxLength = 8,
            CharacterFilter = char.IsAsciiDigit
        };
        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    /// Prompts for a PIN with option to use the default. Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PIN as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetPinWithDefault(string prompt = "PIN")
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter custom PIN"]));

        if (choice == "Use default")
        {
            return CreateFromDefault(DefaultPinValue);
        }

        var options = CredentialReaderOptions.ForPin();
        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    /// Prompts for a PUK and returns the bytes. Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PUK as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetPuk(string prompt = "Enter PUK: ")
    {
        var options = new CredentialReaderOptions
        {
            Prompt = prompt,
            MinLength = 6,
            MaxLength = 8,
            CharacterFilter = char.IsAsciiDigit
        };
        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    /// Prompts for a PUK with option to use the default. Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PUK as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetPukWithDefault(string prompt = "PUK")
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter custom PUK"]));

        if (choice == "Use default")
        {
            return CreateFromDefault(DefaultPukValue);
        }

        var options = CredentialReaderOptions.ForPuk();
        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    /// Prompts for a new PIN with confirmation. Caller must dispose the returned memory owner.
    /// </summary>
    /// <returns>The new PIN as UTF-8 bytes in a memory owner, or null if cancelled or mismatch.</returns>
    public static IMemoryOwner<byte>? GetNewPin()
    {
        var options = new CredentialReaderOptions
        {
            Prompt = "Enter new PIN: ",
            ConfirmPrompt = "Confirm new PIN: ",
            MinLength = 6,
            MaxLength = 8,
            CharacterFilter = char.IsAsciiDigit
        };
        return CredentialReader.ReadCredentialWithConfirmation(options);
    }

    /// <summary>
    /// Prompts for a new PUK with confirmation. Caller must dispose the returned memory owner.
    /// </summary>
    /// <returns>The new PUK as UTF-8 bytes in a memory owner, or null if cancelled or mismatch.</returns>
    public static IMemoryOwner<byte>? GetNewPuk()
    {
        var options = new CredentialReaderOptions
        {
            Prompt = "Enter new PUK: ",
            ConfirmPrompt = "Confirm new PUK: ",
            MinLength = 6,
            MaxLength = 8,
            CharacterFilter = char.IsAsciiDigit
        };
        return CredentialReader.ReadCredentialWithConfirmation(options);
    }

    /// <summary>
    /// Prompts for a management key in hex format. Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <param name="expectedLength">Expected key length in bytes (16, 24, or 32).</param>
    /// <returns>The management key bytes in a memory owner, or null if cancelled or invalid.</returns>
    public static IMemoryOwner<byte>? GetManagementKey(string prompt = "Enter management key (hex): ", int expectedLength = ManagementKeyLength3Des)
    {
        var options = new CredentialReaderOptions
        {
            Prompt = prompt,
            MinLength = expectedLength * 2,
            MaxLength = expectedLength * 3, // Allow for separators
            IsHexMode = true,
            ExpectedByteLength = expectedLength,
            CharacterFilter = static c => char.IsAsciiHexDigit(c) || c is ' ' or ':' or '-'
        };
        return CredentialReader.ReadCredential(options);
    }

    /// <summary>
    /// Prompts for a management key with options for default, passphrase, or hex.
    /// Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <param name="keyLength">Expected key length in bytes (16, 24, or 32). Default is 24.</param>
    /// <returns>The management key bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetManagementKeyWithDefault(string prompt = "Management key", int keyLength = ManagementKeyLength3Des)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter passphrase (derives key)", "Enter hex string (advanced)"]));

        return choice switch
        {
            "Use default" => CreateFromSpan(PivSession.DefaultManagementKey),
            "Enter passphrase (derives key)" => GetManagementKeyFromPassphrase(keyLength),
            "Enter hex string (advanced)" => GetManagementKey("Enter management key (hex): ", keyLength),
            _ => null
        };
    }

    /// <summary>
    /// Prompts for a NEW management key with options for passphrase, random, or hex.
    /// Caller must dispose the returned memory owner.
    /// </summary>
    /// <param name="keyLength">Key length in bytes (16, 24, or 32). Default is 24.</param>
    /// <returns>The management key bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetNewManagementKey(int keyLength = ManagementKeyLength3Des)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[blue]New management key:[/]")
                .AddChoices(["Generate from passphrase", "Generate random key", "Enter hex string (advanced)"]));

        return choice switch
        {
            "Generate from passphrase" => GetManagementKeyFromPassphraseWithConfirm(keyLength),
            "Generate random key" => GenerateRandomManagementKey(keyLength),
            "Enter hex string (advanced)" => GetManagementKey("Enter new management key (hex): ", keyLength),
            _ => null
        };
    }

    /// <summary>
    /// Derives a management key from a passphrase with confirmation.
    /// </summary>
    private static IMemoryOwner<byte>? GetManagementKeyFromPassphraseWithConfirm(int keyLength)
    {
        var options = new CredentialReaderOptions
        {
            Prompt = "Enter passphrase: ",
            ConfirmPrompt = "Confirm passphrase: ",
            MinLength = 1,
            MaxLength = 128
        };

        using var passphrase = CredentialReader.ReadCredentialWithConfirmation(options);
        if (passphrase is null)
        {
            OutputHelpers.WriteError("Passphrases do not match or were cancelled.");
            return null;
        }

        var result = DeriveKeyFromPassphraseBytes(passphrase.Memory.Span, keyLength);
        OutputHelpers.WriteInfo("Key derived from passphrase. Remember this passphrase for future authentication.");
        return result;
    }

    /// <summary>
    /// Generates a random management key.
    /// </summary>
    private static IMemoryOwner<byte> GenerateRandomManagementKey(int keyLength)
    {
        var key = new byte[keyLength];
        RandomNumberGenerator.Fill(key);

        // Show the hex so user can save it
        OutputHelpers.WriteInfo($"Generated key: {Convert.ToHexString(key)}");
        OutputHelpers.WriteWarning("Save this key securely! You will need it for future management operations.");

        return CreateFromSpan(key);
    }

    /// <summary>
    /// Derives a management key from a passphrase using PBKDF2-SHA256.
    /// </summary>
    private static IMemoryOwner<byte>? GetManagementKeyFromPassphrase(int keyLength)
    {
        var options = new CredentialReaderOptions
        {
            Prompt = "Enter passphrase: ",
            MinLength = 1,
            MaxLength = 128
        };

        using var passphrase = CredentialReader.ReadCredential(options);
        if (passphrase is null)
        {
            return null;
        }

        return DeriveKeyFromPassphraseBytes(passphrase.Memory.Span, keyLength);
    }

    /// <summary>
    /// Derives a key from passphrase bytes using PBKDF2-SHA256.
    /// </summary>
    /// <param name="passphraseBytes">The passphrase as UTF-8 bytes.</param>
    /// <param name="keyLength">The desired key length in bytes.</param>
    /// <returns>The derived key bytes in a memory owner.</returns>
    private static IMemoryOwner<byte> DeriveKeyFromPassphraseBytes(ReadOnlySpan<byte> passphraseBytes, int keyLength)
    {
        // Fixed salt for this example app - in production, use unique salt per key
        ReadOnlySpan<byte> salt = "YubiKit.PIV.Example.2026"u8;

        var key = new byte[keyLength];
        Rfc2898DeriveBytes.Pbkdf2(
            passphraseBytes,
            salt,
            key,
            PbkdfIterations,
            HashAlgorithmName.SHA256);

        return CreateFromSpan(key);
    }

    /// <summary>
    /// Creates a secure memory owner from a default string value.
    /// </summary>
    private static IMemoryOwner<byte> CreateFromDefault(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return CreateFromSpan(bytes);
    }

    /// <summary>
    /// Creates a secure memory owner from a span, zeroing the source after copy.
    /// </summary>
    private static IMemoryOwner<byte> CreateFromSpan(ReadOnlySpan<byte> source) =>
        DisposableArrayPoolBuffer.CreateFromSpan(source);
}

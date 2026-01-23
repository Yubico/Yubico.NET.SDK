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

    /// <summary>
    /// Prompts for a PIN and returns the bytes. Caller must zero the returned memory.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PIN as UTF-8 bytes, or null if cancelled.</returns>
    public static byte[]? GetPin(string prompt = "Enter PIN")
    {
        return GetCredential(prompt, MaxPinLength, minLength: 6);
    }

    /// <summary>
    /// Prompts for a PIN with option to use the default. Caller must zero the returned memory.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PIN as UTF-8 bytes, or null if cancelled.</returns>
    public static byte[]? GetPinWithDefault(string prompt = "PIN")
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter custom PIN"]));

        return choice == "Use default"
            ? Encoding.UTF8.GetBytes(DefaultPinValue)
            : GetCredential("Enter PIN", MaxPinLength, minLength: 6);
    }

    /// <summary>
    /// Prompts for a PUK and returns the bytes. Caller must zero the returned memory.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PUK as UTF-8 bytes, or null if cancelled.</returns>
    public static byte[]? GetPuk(string prompt = "Enter PUK")
    {
        return GetCredential(prompt, MaxPukLength, minLength: 6);
    }

    /// <summary>
    /// Prompts for a PUK with option to use the default. Caller must zero the returned memory.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PUK as UTF-8 bytes, or null if cancelled.</returns>
    public static byte[]? GetPukWithDefault(string prompt = "PUK")
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter custom PUK"]));

        return choice == "Use default"
            ? Encoding.UTF8.GetBytes(DefaultPukValue)
            : GetCredential("Enter PUK", MaxPukLength, minLength: 6);
    }

    /// <summary>
    /// Prompts for a new PIN with confirmation. Caller must zero the returned memory.
    /// </summary>
    /// <returns>The new PIN as UTF-8 bytes, or null if cancelled or mismatch.</returns>
    public static byte[]? GetNewPin()
    {
        var pin = GetCredential("Enter new PIN", MaxPinLength, minLength: 6);
        if (pin is null)
        {
            return null;
        }

        var confirm = GetCredential("Confirm new PIN", MaxPinLength, minLength: 6);
        if (confirm is null)
        {
            CryptographicOperations.ZeroMemory(pin);
            return null;
        }

        if (!pin.AsSpan().SequenceEqual(confirm))
        {
            CryptographicOperations.ZeroMemory(pin);
            CryptographicOperations.ZeroMemory(confirm);
            OutputHelpers.WriteError("PINs do not match.");
            return null;
        }

        CryptographicOperations.ZeroMemory(confirm);
        return pin;
    }

    /// <summary>
    /// Prompts for a new PUK with confirmation. Caller must zero the returned memory.
    /// </summary>
    /// <returns>The new PUK as UTF-8 bytes, or null if cancelled or mismatch.</returns>
    public static byte[]? GetNewPuk()
    {
        var puk = GetCredential("Enter new PUK", MaxPukLength, minLength: 6);
        if (puk is null)
        {
            return null;
        }

        var confirm = GetCredential("Confirm new PUK", MaxPukLength, minLength: 6);
        if (confirm is null)
        {
            CryptographicOperations.ZeroMemory(puk);
            return null;
        }

        if (!puk.AsSpan().SequenceEqual(confirm))
        {
            CryptographicOperations.ZeroMemory(puk);
            CryptographicOperations.ZeroMemory(confirm);
            OutputHelpers.WriteError("PUKs do not match.");
            return null;
        }

        CryptographicOperations.ZeroMemory(confirm);
        return puk;
    }

    /// <summary>
    /// Prompts for a management key in hex format. Caller must zero the returned memory.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <param name="expectedLength">Expected key length in bytes (16, 24, or 32).</param>
    /// <returns>The management key bytes, or null if cancelled or invalid.</returns>
    public static byte[]? GetManagementKey(string prompt = "Enter management key (hex)", int expectedLength = ManagementKeyLength3Des)
    {
        var hexInput = AnsiConsole.Prompt(
            new TextPrompt<string>($"{prompt}:")
                .Secret()
                .Validate(hex =>
                {
                    if (string.IsNullOrWhiteSpace(hex))
                    {
                        return ValidationResult.Error("Management key cannot be empty.");
                    }

                    // Remove any spaces or dashes
                    var cleanHex = hex.Replace(" ", "").Replace("-", "");

                    if (cleanHex.Length != expectedLength * 2)
                    {
                        return ValidationResult.Error($"Management key must be {expectedLength} bytes ({expectedLength * 2} hex characters).");
                    }

                    try
                    {
                        _ = Convert.FromHexString(cleanHex);
                        return ValidationResult.Success();
                    }
                    catch
                    {
                        return ValidationResult.Error("Invalid hex format.");
                    }
                }));

        try
        {
            var cleanHex = hexInput.Replace(" ", "").Replace("-", "");
            return Convert.FromHexString(cleanHex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Prompts for a management key with options for default, passphrase, or hex.
    /// Caller must zero the returned memory.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <param name="keyLength">Expected key length in bytes (16, 24, or 32). Default is 24.</param>
    /// <returns>The management key bytes, or null if cancelled.</returns>
    public static byte[]? GetManagementKeyWithDefault(string prompt = "Management key", int keyLength = ManagementKeyLength3Des)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[blue]{prompt}:[/]")
                .AddChoices(["Use default", "Enter passphrase (derives key)", "Enter hex string (advanced)"]));

        return choice switch
        {
            "Use default" => PivSession.DefaultManagementKey.ToArray(),
            "Enter passphrase (derives key)" => GetManagementKeyFromPassphrase(keyLength),
            "Enter hex string (advanced)" => GetManagementKey("Enter management key (hex)", keyLength),
            _ => null
        };
    }

    /// <summary>
    /// Derives a management key from a passphrase using PBKDF2-SHA256.
    /// </summary>
    private static byte[]? GetManagementKeyFromPassphrase(int keyLength)
    {
        var passphrase = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter passphrase:")
                .Secret()
                .Validate(s => string.IsNullOrEmpty(s)
                    ? ValidationResult.Error("Passphrase cannot be empty.")
                    : ValidationResult.Success()));

        if (string.IsNullOrEmpty(passphrase))
        {
            return null;
        }

        return DeriveKeyFromPassphrase(passphrase, keyLength);
    }

    /// <summary>
    /// Derives a key from a passphrase using PBKDF2-SHA256.
    /// </summary>
    /// <param name="passphrase">The passphrase to derive from.</param>
    /// <param name="keyLength">The desired key length in bytes.</param>
    /// <returns>The derived key bytes.</returns>
    private static byte[] DeriveKeyFromPassphrase(string passphrase, int keyLength)
    {
        // Fixed salt for this example app - in production, use unique salt per key
        ReadOnlySpan<byte> salt = "YubiKit.PIV.Example.2026"u8;

        var key = new byte[keyLength];
        Rfc2898DeriveBytes.Pbkdf2(
            passphrase,
            salt,
            key,
            PbkdfIterations,
            HashAlgorithmName.SHA256);

        return key;
    }

    /// <summary>
    /// Prompts for a credential and returns the UTF-8 bytes. Caller must zero the returned memory.
    /// </summary>
    private static byte[]? GetCredential(string prompt, int maxLength, int minLength = 1)
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>($"{prompt}:")
                .Secret()
                .Validate(s =>
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        return ValidationResult.Error("Cannot be empty.");
                    }

                    if (s.Length < minLength)
                    {
                        return ValidationResult.Error($"Must be at least {minLength} characters.");
                    }

                    if (s.Length > maxLength)
                    {
                        return ValidationResult.Error($"Cannot exceed {maxLength} characters.");
                    }

                    return ValidationResult.Success();
                }));

        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        // Convert to UTF-8 bytes using pooled buffer
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(input.Length);
        var buffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

        try
        {
            var byteCount = Encoding.UTF8.GetBytes(input, buffer);
            var result = new byte[byteCount];
            buffer.AsSpan(0, byteCount).CopyTo(result);
            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer.AsSpan(0, maxByteCount));
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Executes an action with PIN bytes and ensures they are zeroed afterward.
    /// </summary>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <param name="pin">The PIN bytes to use.</param>
    /// <param name="action">The action to execute with the PIN.</param>
    /// <returns>The result of the action.</returns>
    public static async Task<T> UseAndZeroAsync<T>(byte[] pin, Func<byte[], Task<T>> action)
    {
        try
        {
            return await action(pin);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }

    /// <summary>
    /// Executes an action with PIN bytes and ensures they are zeroed afterward.
    /// </summary>
    /// <param name="pin">The PIN bytes to use.</param>
    /// <param name="action">The action to execute with the PIN.</param>
    public static async Task UseAndZeroAsync(byte[] pin, Func<byte[], Task> action)
    {
        try
        {
            await action(pin);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pin);
        }
    }
}

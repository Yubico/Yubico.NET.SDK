// Copyright 2025 Yubico AB
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

using System.Text;

namespace Yubico.YubiKit.Core.Credentials;

/// <summary>
/// Configuration options for credential input.
/// </summary>
/// <remarks>
/// Use the factory methods <see cref="ForPin"/>, <see cref="ForPuk"/>, <see cref="ForPassphrase"/>,
/// or <see cref="ForHexKey"/> to create pre-configured options for common credential types.
/// </remarks>
public sealed record CredentialReaderOptions
{
    /// <summary>
    /// Gets or sets the prompt text displayed to the user.
    /// </summary>
    public string Prompt { get; init; } = "Enter credential: ";

    /// <summary>
    /// Gets or sets the confirmation prompt text for credential confirmation.
    /// </summary>
    public string ConfirmPrompt { get; init; } = "Confirm credential: ";

    /// <summary>
    /// Gets or sets the minimum allowed credential length.
    /// </summary>
    public int MinLength { get; init; } = 1;

    /// <summary>
    /// Gets or sets the maximum allowed credential length.
    /// </summary>
    public int MaxLength { get; init; } = 128;

    /// <summary>
    /// Gets or sets the character used to mask input (default: '*').
    /// </summary>
    public char MaskCharacter { get; init; } = '*';

    /// <summary>
    /// Gets or sets a filter function that determines which characters are allowed.
    /// Returns <c>true</c> to accept the character, <c>false</c> to reject it.
    /// </summary>
    public Func<char, bool>? CharacterFilter { get; init; }

    /// <summary>
    /// Gets or sets the encoding used to convert characters to bytes.
    /// Defaults to UTF-8.
    /// </summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;

    /// <summary>
    /// Gets or sets whether the input is expected in hexadecimal format.
    /// When <c>true</c>, input is parsed as hex and <see cref="ExpectedByteLength"/>
    /// should be set to validate the result length.
    /// </summary>
    public bool IsHexMode { get; init; }

    /// <summary>
    /// Gets or sets the expected byte length when <see cref="IsHexMode"/> is <c>true</c>.
    /// A value of <c>null</c> means any length is accepted.
    /// </summary>
    public int? ExpectedByteLength { get; init; }

    /// <summary>
    /// Creates options configured for PIN input (6-8 numeric digits).
    /// </summary>
    public static CredentialReaderOptions ForPin() => new()
    {
        Prompt = "Enter PIN: ",
        ConfirmPrompt = "Confirm PIN: ",
        MinLength = 6,
        MaxLength = 8,
        CharacterFilter = char.IsAsciiDigit
    };

    /// <summary>
    /// Creates options configured for PUK input (6-8 numeric digits).
    /// </summary>
    public static CredentialReaderOptions ForPuk() => new()
    {
        Prompt = "Enter PUK: ",
        ConfirmPrompt = "Confirm PUK: ",
        MinLength = 6,
        MaxLength = 8,
        CharacterFilter = char.IsAsciiDigit
    };

    /// <summary>
    /// Creates options configured for passphrase input.
    /// </summary>
    public static CredentialReaderOptions ForPassphrase() => new()
    {
        Prompt = "Enter passphrase: ",
        ConfirmPrompt = "Confirm passphrase: ",
        MinLength = 1,
        MaxLength = 128
    };

    /// <summary>
    /// Creates options configured for hexadecimal key input.
    /// </summary>
    /// <param name="byteLength">Expected key length in bytes (e.g., 24 for 3DES key).</param>
    public static CredentialReaderOptions ForHexKey(int byteLength) => new()
    {
        Prompt = $"Enter {byteLength}-byte key (hex): ",
        ConfirmPrompt = $"Confirm {byteLength}-byte key (hex): ",
        MinLength = byteLength * 2,
        MaxLength = byteLength * 3, // Allow for separators
        IsHexMode = true,
        ExpectedByteLength = byteLength,
        CharacterFilter = static c => char.IsAsciiHexDigit(c) || c is ' ' or ':' or '-'
    };

    /// <summary>
    /// Creates options configured for FIDO2 PIN input (4-63 bytes UTF-8).
    /// </summary>
    public static CredentialReaderOptions ForFido2Pin() => new()
    {
        Prompt = "Enter FIDO2 PIN: ",
        ConfirmPrompt = "Confirm FIDO2 PIN: ",
        MinLength = 4,
        MaxLength = 63
    };

    /// <summary>
    /// Creates options configured for OpenPGP user PIN input (6-127 characters).
    /// </summary>
    public static CredentialReaderOptions ForOpenPgpPin() => new()
    {
        Prompt = "Enter OpenPGP PIN: ",
        ConfirmPrompt = "Confirm OpenPGP PIN: ",
        MinLength = 6,
        MaxLength = 127
    };

    /// <summary>
    /// Creates options configured for OpenPGP admin PIN input (8-127 characters).
    /// </summary>
    public static CredentialReaderOptions ForOpenPgpAdminPin() => new()
    {
        Prompt = "Enter Admin PIN: ",
        ConfirmPrompt = "Confirm Admin PIN: ",
        MinLength = 8,
        MaxLength = 127
    };

    /// <summary>
    /// Creates options configured for OpenPGP reset code input.
    /// </summary>
    public static CredentialReaderOptions ForOpenPgpResetCode() => new()
    {
        Prompt = "Enter Reset Code: ",
        ConfirmPrompt = "Confirm Reset Code: ",
        MinLength = 8,
        MaxLength = 127
    };

    /// <summary>
    /// Creates options configured for OATH password input.
    /// </summary>
    public static CredentialReaderOptions ForOathPassword() => new()
    {
        Prompt = "Enter OATH password: ",
        ConfirmPrompt = "Confirm OATH password: ",
        MinLength = 1,
        MaxLength = 128
    };
}

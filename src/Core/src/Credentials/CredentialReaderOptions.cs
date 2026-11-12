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
/// <para>
/// Use the factory methods <see cref="ForPin"/>, <see cref="ForPuk"/>, <see cref="ForPassphrase"/>,
/// or <see cref="ForHexKey"/> to create pre-configured options for common credential types.
/// </para>
/// <para>
/// These are input presets, rather than applet validation rules. Select options appropriate to
/// the operation's character, encoded-byte, and credential requirements.
/// In the console reader, interactive limits include hex separators and count UTF-16 code units.
/// Redirected hex input bypasses MinLength, MaxLength, and CharacterFilter, then checks hex syntax
/// and ExpectedByteLength. ExpectedByteLength counts decoded bytes, excluding separators.
/// </para>
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
    /// Gets or sets the minimum input length in UTF-16 code units before text encoding.
    /// </summary>
    public int MinLength { get; init; } = 1;

    /// <summary>
    /// Gets or sets the maximum input length in UTF-16 code units before text encoding.
    /// </summary>
    public int MaxLength { get; init; } = 128;

    /// <summary>
    /// Gets or sets the character used to mask input (default: '*').
    /// </summary>
    public char MaskCharacter { get; init; } = '*';

    /// <summary>
    /// Gets or sets a filter function that determines which characters are allowed.
    /// Returns <c>true</c> to accept a UTF-16 code unit. The console reader ignores disallowed
    /// interactive characters but rejects a redirected text line containing one.
    /// </summary>
    public Func<char, bool>? CharacterFilter { get; init; }

    /// <summary>
    /// Gets or sets the encoding used to convert characters to bytes.
    /// Defaults to UTF-8 with replacement fallback for malformed surrogate input.
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
    /// A value of <c>null</c> skips the decoded-length check. Empty decoded input currently fails
    /// when the console reader allocates its positive-sized result owner.
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

}
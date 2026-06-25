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

using System.Security.Cryptography;
using Spectre.Console;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

/// <summary>
/// Handles secure lock code input with validation.
/// Lock codes are 16 bytes (32 hex characters).
/// 
/// <para><b>Security Pattern:</b> Always use try/finally to ensure lock codes are zeroed:</para>
/// <code>
/// byte[]? lockCode = null;
/// try
/// {
///     lockCode = LockCodePrompt.PromptForLockCode("Enter lock code");
///     if (lockCode is null) return; // User cancelled
///     
///     // Use lock code...
/// }
/// finally
/// {
///     if (lockCode is not null)
///     {
///         CryptographicOperations.ZeroMemory(lockCode);
///     }
/// }
/// </code>
/// </summary>
public static class LockCodePrompt
{
    /// <summary>
    /// Lock code size in bytes.
    /// </summary>
    public const int LockCodeSize = 16;

    /// <summary>
    /// Lock code size as hex string (32 characters).
    /// </summary>
    public const int LockCodeHexLength = LockCodeSize * 2;

    /// <summary>
    /// Prompts for a lock code (16 bytes / 32 hex characters).
    /// </summary>
    /// <param name="prompt">The prompt message to display.</param>
    /// <returns>The lock code as a byte array, or null if user cancelled or input was invalid.</returns>
    /// <remarks>
    /// <b>CRITICAL:</b> Caller MUST zero the returned byte array when done using
    /// <see cref="CryptographicOperations.ZeroMemory(Span{byte})"/> in a finally block.
    /// </remarks>
    public static byte[]? PromptForLockCode(string prompt = "Enter lock code (32 hex characters)")
    {
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(prompt)}[/]");
        AnsiConsole.MarkupLine("[grey]Format: 32 hexadecimal characters (16 bytes), e.g., 00112233445566778899AABBCCDDEEFF[/]");

        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Lock code:[/]")
                .Secret()
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(input))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return null;
        }

        if (!TryParseHex(input, out var lockCode))
        {
            AnsiConsole.MarkupLine($"[red]Invalid lock code. Must be exactly {LockCodeHexLength} hexadecimal characters.[/]");
            return null;
        }

        return lockCode;
    }

    /// <summary>
    /// Prompts for a new lock code with confirmation.
    /// </summary>
    /// <param name="prompt">The prompt message for the first entry.</param>
    /// <returns>The confirmed lock code, or null if user cancelled or entries didn't match.</returns>
    /// <remarks>
    /// <b>CRITICAL:</b> Caller MUST zero the returned byte array when done using
    /// <see cref="CryptographicOperations.ZeroMemory(Span{byte})"/> in a finally block.
    /// </remarks>
    public static byte[]? PromptForNewLockCode(string prompt = "Enter new lock code (32 hex characters)")
    {
        var lockCode = PromptForLockCode(prompt);
        if (lockCode is null)
        {
            return null;
        }

        AnsiConsole.WriteLine();
        var confirmInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[blue]Confirm lock code:[/]")
                .Secret()
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(confirmInput))
        {
            CryptographicOperations.ZeroMemory(lockCode);
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return null;
        }

        if (!TryParseHex(confirmInput, out var confirmCode))
        {
            CryptographicOperations.ZeroMemory(lockCode);
            AnsiConsole.MarkupLine($"[red]Invalid confirmation. Must be exactly {LockCodeHexLength} hexadecimal characters.[/]");
            return null;
        }

        try
        {
            if (!CryptographicOperations.FixedTimeEquals(lockCode, confirmCode))
            {
                CryptographicOperations.ZeroMemory(lockCode);
                AnsiConsole.MarkupLine("[red]Lock codes do not match.[/]");
                return null;
            }

            return lockCode;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(confirmCode);
        }
    }

    /// <summary>
    /// Tries to parse a hex string into a lock code byte array.
    /// </summary>
    /// <param name="hexString">The hex string to parse.</param>
    /// <param name="lockCode">The parsed lock code, or null if parsing failed.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private static bool TryParseHex(string hexString, out byte[] lockCode)
    {
        lockCode = [];

        // Remove any whitespace or dashes
        var cleaned = hexString.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        if (cleaned.Length != LockCodeHexLength)
        {
            return false;
        }

        try
        {
            lockCode = Convert.FromHexString(cleaned);
            return lockCode.Length == LockCodeSize;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a zeroed lock code (all zeros, 16 bytes).
    /// Used to remove/clear a lock code.
    /// </summary>
    /// <returns>A 16-byte array filled with zeros.</returns>
    /// <remarks>
    /// <b>CRITICAL:</b> Caller MUST zero the returned byte array when done using
    /// <see cref="CryptographicOperations.ZeroMemory(Span{byte})"/> in a finally block.
    /// </remarks>
    public static byte[] CreateZeroLockCode()
    {
        return new byte[LockCodeSize];
    }
}

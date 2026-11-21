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
using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.Management.Examples.ManagementTool.Cli.Prompts;

/// <summary>
/// Masks lock-code input and validates the parsed length.
/// Lock codes are 16 bytes (32 hex characters).
/// Interactive input uses the SDK's masked hex credential reader.
/// The confirmation prompt can throw before the first parsed array enters cleanup. This sample is not
/// a production secret-input primitive.
/// 
/// <para><b>Security Pattern:</b> Always use try/finally to ensure lock codes are zeroed:</para>
/// <code>
/// byte[]? lockCode = null;
/// try
/// {
///     lockCode = LockCodePrompt.PromptForLockCode("Enter lock code");
///     if (lockCode is null) return; // User cancelled
///     
///     // session is an initialized IManagementSession; ct is the caller's CancellationToken.
///     await session.SetDeviceConfigAsync(
///         new DeviceConfig { ChallengeResponseTimeout = 15 },
///         new SetDeviceConfigOptions { CurrentLockCode = lockCode }, ct);
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
    /// Prompts for a lock code (16 bytes / 32 hex characters).
    /// </summary>
    /// <param name="prompt">The prompt message to display.</param>
    /// <param name="cancellationToken">Cancellation token for console input.</param>
    /// <returns>The lock code as a byte array, or null if user cancelled or input was invalid.</returns>
    /// <remarks>
    /// <b>CRITICAL:</b> Caller MUST zero the returned byte array when done using
    /// <see cref="CryptographicOperations.ZeroMemory(Span{byte})"/> in a finally block.
    /// </remarks>
    public static byte[]? PromptForLockCode(string prompt = "Enter lock code", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var entry = new ConsoleCredentialReader().ReadCredential(Options(prompt), cancellationToken);
        return entry?.Memory.ToArray(); // Transfer a copy to the existing caller-owned array scope.
    }

    /// <summary>
    /// Prompts for a new lock code with confirmation.
    /// </summary>
    /// <param name="prompt">The prompt message for the first entry.</param>
    /// <param name="cancellationToken">Cancellation token for console input.</param>
    /// <returns>The confirmed lock code, or null if user cancelled or entries didn't match.</returns>
    /// <remarks>
    /// <b>CRITICAL:</b> Caller MUST zero the returned byte array when done using
    /// <see cref="CryptographicOperations.ZeroMemory(Span{byte})"/> in a finally block.
    /// </remarks>
    public static byte[]? PromptForNewLockCode(string prompt = "Enter new lock code", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var entry = new ConsoleCredentialReader().ReadCredentialWithConfirmation(Options(prompt), cancellationToken);
        return entry?.Memory.ToArray();
    }

    private static CredentialReaderOptions Options(string prompt) => CredentialReaderOptions.ForHexKey(LockCodeSize) with
    {
        Prompt = $"{prompt.TrimEnd(':')} (32 hex digits): ",
        ConfirmPrompt = "Confirm lock code (32 hex digits): ",
    };

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
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

namespace Yubico.YubiKit.Cli.Shared.Output;

/// <summary>
/// Provides PIN/password prompts with masked input for CLI tools.
/// </summary>
/// <remarks>
/// Credential-producing members return an <see cref="IMemoryOwner{T}"/> whose
/// <see cref="IMemoryOwner{T}.Memory"/> is sized exactly to the secret and zeroed on disposal, so
/// results must always be consumed with <c>using</c>. A <see langword="null"/> result means the
/// user declined to supply a credential and never signals an input error.
/// </remarks>
public static class PinPrompt
{
    /// <summary>
    /// Resolves a credential supplied on the command line, prompting for it when absent.
    /// </summary>
    /// <param name="provided">The value passed on the command line, if any.</param>
    /// <param name="label">Label shown when prompting.</param>
    /// <returns>
    /// Owned bytes, or <see langword="null"/> if no value was supplied on the command line and the
    /// user declined the prompt.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// No command-line value was supplied and the prompted credential contains invalid Unicode
    /// text, or its UTF-8 encoding exceeds the prompt limit.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// No command-line value was supplied and <paramref name="label"/> is <see langword="null"/>,
    /// empty, or consists only of white-space characters.
    /// </exception>
    /// <remarks>
    /// Command-line and interactive values are encoded as UTF-8 text. When standard input is
    /// redirected, bytes are read as supplied after removing line endings. Resolve is the command
    /// seam between already-supplied values and prompting. Command-line strings can be visible to
    /// other processes and cannot be cleared from managed memory; prefer prompting for production
    /// credential entry.
    /// </remarks>
    public static IMemoryOwner<byte>? Resolve(string? provided, string label) =>
        string.IsNullOrEmpty(provided)
            ? PromptForCredential(label)
            : SecureCredential.FromUtf8String(provided);

    /// <summary>
    /// Prompts the user for a credential, returning owned bytes that are zeroed on disposal.
    /// </summary>
    /// <returns>
    /// The credential, or <see langword="null"/> if the user pressed Escape, pressed Enter with an
    /// empty entry, or supplied an empty redirected line or end-of-input.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The entered credential contains invalid Unicode text, or its UTF-8 encoding exceeds the
    /// prompt limit.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="label"/> is <see langword="null"/>, empty, or consists only of white-space characters.
    /// </exception>
    /// <remarks>
    /// Interactive text is encoded as UTF-8. Redirected standard input is treated as raw bytes
    /// after line endings are removed.
    /// </remarks>
    public static IMemoryOwner<byte>? PromptForCredential(string label = "PIN") =>
        SecureCredential.Prompt(label);

    /// <summary>
    /// Prompts a second time and reports whether the re-entered value matches
    /// <paramref name="credential"/>, the conventional check when establishing a new secret.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> only if the user supplied a second value and it matches.
    /// A declined confirmation counts as a mismatch.
    /// </returns>
    /// <remarks>
    /// The confirmation entry is resolved through the prompt seam, and
    /// <see cref="CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/>
    /// avoids content-dependent secret comparisons. Callers should use this method rather than
    /// comparing credential bytes with <c>SequenceEqual</c>.
    /// </remarks>
    public static bool ConfirmMatches(IMemoryOwner<byte> credential, string confirmLabel) =>
        ConfirmMatches(credential, () => PromptForCredential(confirmLabel));

    /// <inheritdoc cref="ConfirmMatches(IMemoryOwner{byte}, string)"/>
    /// <param name="credential">The credential to confirm.</param>
    /// <param name="promptForConfirmation">Supplies the second entry.</param>
    internal static bool ConfirmMatches(
        IMemoryOwner<byte> credential,
        Func<IMemoryOwner<byte>?> promptForConfirmation)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(promptForConfirmation);

        using var confirmation = promptForConfirmation();
        return confirmation is not null
               && CryptographicOperations.FixedTimeEquals(
                   credential.Memory.Span, confirmation.Memory.Span);
    }
}
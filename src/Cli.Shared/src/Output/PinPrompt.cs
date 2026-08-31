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
/// Every member returns <see cref="IMemoryOwner{T}"/> whose <see cref="IMemoryOwner{T}.Memory"/>
/// is sized exactly to the secret and is zeroed on disposal, so results must always be consumed
/// with <c>using</c>. A <see langword="null"/> result means the user <b>declined</b> to supply a
/// credential and never signals an error, matching the SDK's <c>ICredentialPrompt</c> contract.
/// </remarks>
public static class PinPrompt
{
    /// <summary>
    /// Resolves a credential supplied on the command line, prompting for it when absent.
    /// </summary>
    /// <param name="provided">The value passed on the command line, if any.</param>
    /// <param name="label">Label shown when prompting.</param>
    /// <returns>
    /// Owned UTF-8 bytes, or <see langword="null"/> if no value was supplied on the command line
    /// and the user declined the prompt.
    /// </returns>
    /// <remarks>
    /// This is the single seam CLI commands should use. Passing a secret on the command line is
    /// inherently insecure — it is visible to other processes and cannot be zeroed — so the
    /// prompt is the intended path and the command-line value is a testing/demo convenience.
    /// </remarks>
    public static IMemoryOwner<byte>? Resolve(string? provided, string label) =>
        string.IsNullOrEmpty(provided)
            ? PromptForCredential(label)
            : SecureCredential.FromUtf8String(provided);

    /// <summary>
    /// Prompts the user for a credential interactively, returning owned UTF-8 bytes that are
    /// zeroed on disposal.
    /// </summary>
    /// <returns>
    /// The credential, or <see langword="null"/> if the user declined to supply one.
    /// </returns>
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
    /// Comparison is constant-time. Callers must not hand-roll this check: the natural
    /// spellings (<c>string.Equals</c>, <c>SequenceEqual</c>) are content-dependent in timing
    /// and are forbidden on secrets by the repository's security rules.
    /// </remarks>
    public static bool ConfirmMatches(IMemoryOwner<byte> credential, string confirmLabel) =>
        ConfirmMatches(credential, () => PromptForCredential(confirmLabel));

    /// <inheritdoc cref="ConfirmMatches(IMemoryOwner{byte}, string)"/>
    /// <param name="credential">The credential to confirm.</param>
    /// <param name="promptForConfirmation">
    /// Supplies the second entry. Exists so the constant-time comparison can be tested without a
    /// console.
    /// </param>
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
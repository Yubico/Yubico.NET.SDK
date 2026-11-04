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
using System.Text;
using Yubico.YubiKit.Core.Credentials;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.Ctap2.Credentials;

namespace Yubico.YubiKit.Ctap2.Examples.FidoTool.Cli.Prompts;

/// <summary>
/// Provides FIDO2 PIN entry as UTF-8 bytes in an <see cref="IMemoryOwner{T}"/> that clears on disposal.
/// </summary>
/// <remarks>
/// Dispose the owner after the awaited operation. CLI arguments remain immutable strings in process
/// memory and may be exposed by shell history/process listings; clearing encoded bytes cannot erase
/// them. Interactive prompts are synchronous and do not honor the operation cancellation token while
/// blocked. The console preset counts UTF-16 characters before encoding, not CTAP code points/bytes;
/// it is not complete UTF-8/NFC/device-PIN-policy validation.
/// </remarks>
internal static class FidoPinHelper
{
    private static readonly ConsoleCredentialReader Reader = new();

    /// <summary>
    /// Prompts interactively for a FIDO2 PIN.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The PIN as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? PromptForPin(string prompt = "Enter FIDO2 PIN: ")
    {
        var options = Ctap2CredentialOptions.ForCtap2Pin() with { Prompt = prompt };
        return Reader.ReadCredential(options);
    }

    /// <summary>
    /// Prompts interactively for a new FIDO2 PIN with confirmation.
    /// </summary>
    /// <param name="prompt">The prompt text to display.</param>
    /// <returns>The new PIN as UTF-8 bytes in a memory owner, or null if cancelled or mismatch.</returns>
    public static IMemoryOwner<byte>? PromptForNewPin(string prompt = "Enter new PIN: ")
    {
        var options = Ctap2CredentialOptions.ForCtap2Pin() with
        {
            Prompt = prompt,
            ConfirmPrompt = "Confirm new PIN: "
        };
        return Reader.ReadCredentialWithConfirmation(options);
    }

    /// <summary>
    /// Gets a PIN from a CLI argument or interactive prompt.
    /// If the CLI argument is provided, wraps it in an <see cref="IMemoryOwner{T}"/>.
    /// </summary>
    /// <param name="cliArg">The CLI argument value, or null to prompt interactively.</param>
    /// <param name="prompt">The prompt text if interactive input is needed.</param>
    /// <returns>The PIN as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetPin(string? cliArg, string prompt = "Enter FIDO2 PIN: ")
    {
        if (cliArg is not null)
        {
            // Encode into the owned byte buffer; disposal cannot erase the original immutable CLI string.
            var byteCount = Encoding.UTF8.GetByteCount(cliArg);
            var buffer = new DisposableArrayPoolBuffer(byteCount, clearOnCreate: false);
            Encoding.UTF8.GetBytes(cliArg, buffer.Span);
            return buffer;
        }

        return PromptForPin(prompt);
    }

    /// <summary>
    /// Gets a new PIN from a CLI argument or interactive prompt with confirmation.
    /// </summary>
    /// <param name="cliArg">The CLI argument value, or null to prompt interactively with confirmation.</param>
    /// <param name="prompt">The prompt text if interactive input is needed.</param>
    /// <returns>The new PIN as UTF-8 bytes in a memory owner, or null if cancelled.</returns>
    public static IMemoryOwner<byte>? GetNewPin(string? cliArg, string prompt = "Enter new PIN: ")
    {
        if (cliArg is not null)
        {
            // Encode into the owned byte buffer; args/shell history remain outside this cleanup boundary.
            var byteCount = Encoding.UTF8.GetByteCount(cliArg);
            var buffer = new DisposableArrayPoolBuffer(byteCount, clearOnCreate: false);
            Encoding.UTF8.GetBytes(cliArg, buffer.Span);
            return buffer;
        }

        return PromptForNewPin(prompt);
    }
}
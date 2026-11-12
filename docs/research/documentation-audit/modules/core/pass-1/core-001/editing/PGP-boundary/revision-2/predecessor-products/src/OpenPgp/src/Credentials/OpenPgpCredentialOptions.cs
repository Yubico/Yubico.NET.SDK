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

using Yubico.YubiKit.Core.Credentials;

namespace Yubico.YubiKit.OpenPgp.Credentials;

/// <summary>
/// Supplies credential-reader limits and prompts for OpenPGP input.
/// </summary>
/// <remarks>
/// These factories configure text-reader limits in UTF-16 code units, before UTF-8 encoding. The caller
/// separately checks the card's credential format and encoded byte limits; neither these presets nor
/// the byte-taking session methods perform general card-profile validation. UTF-8 byte length can differ
/// from the reader's count.
/// </remarks>
public static class OpenPgpCredentialOptions
{
    /// <summary>
    /// Creates User PIN reader options with a length of 6–127 UTF-16 code units.
    /// </summary>
    public static CredentialReaderOptions ForOpenPgpPin() => new()
    {
        Prompt = "Enter OpenPGP PIN: ",
        ConfirmPrompt = "Confirm OpenPGP PIN: ",
        MinLength = 6,
        MaxLength = 127
    };

    /// <summary>
    /// Creates Admin PIN reader options with a length of 8–127 UTF-16 code units.
    /// </summary>
    public static CredentialReaderOptions ForOpenPgpAdminPin() => new()
    {
        Prompt = "Enter Admin PIN: ",
        ConfirmPrompt = "Confirm Admin PIN: ",
        MinLength = 8,
        MaxLength = 127
    };

    /// <summary>
    /// Creates Reset Code reader options with a length of 8–127 UTF-16 code units.
    /// </summary>
    public static CredentialReaderOptions ForOpenPgpResetCode() => new()
    {
        Prompt = "Enter Reset Code: ",
        ConfirmPrompt = "Confirm Reset Code: ",
        MinLength = 8,
        MaxLength = 127
    };
}

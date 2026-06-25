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
/// Credential reader options pre-configured for OpenPGP operations.
/// </summary>
public static class OpenPgpCredentialOptions
{
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
}

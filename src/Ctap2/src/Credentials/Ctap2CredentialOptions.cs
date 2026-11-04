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

namespace Yubico.YubiKit.Ctap2.Credentials;

/// <summary>
/// Credential reader options pre-configured for CTAP2 operations.
/// </summary>
public static class Ctap2CredentialOptions
{
    /// <summary>
    /// Creates a FIDO2 PIN prompt preset with console-reader limits of 4–63 UTF-16 characters.
    /// </summary>
    /// <remarks>
    /// ConsoleCredentialReader counts char values before converting with the default UTF-8 encoding,
    /// not UTF-8 bytes or Unicode code points. Non-ASCII text can exceed 63 encoded bytes. This preset
    /// does not enforce the authenticator's current minimum or full CTAP PIN rules; the input layer must
    /// validate NFC UTF-8, code-point/byte limits and no final NUL before submitting PIN bytes.
    /// Dispose the credential reader's returned memory owner after the awaited operation to clear its secret.
    /// </remarks>
    public static CredentialReaderOptions ForCtap2Pin() => new()
    {
        Prompt = "Enter FIDO2 PIN: ",
        ConfirmPrompt = "Confirm FIDO2 PIN: ",
        MinLength = 4,
        MaxLength = 63
    };
}
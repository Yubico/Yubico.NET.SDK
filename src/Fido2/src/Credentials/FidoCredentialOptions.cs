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

namespace Yubico.YubiKit.Fido2.Credentials;

/// <summary>
/// Credential reader options pre-configured for FIDO2 operations.
/// </summary>
public static class FidoCredentialOptions
{
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
}

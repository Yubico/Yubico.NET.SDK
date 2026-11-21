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

namespace Yubico.YubiKit.Oath.Credentials;

/// <summary>
/// Credential reader options pre-configured for OATH operations.
/// </summary>
public static class OathCredentialOptions
{
    /// <summary>
    /// Creates options configured for OATH password input.
    /// </summary>
    /// <remarks>
    ///     The 1..128 limit counts UTF-16 code units in the console reader, before UTF-8 conversion.
    ///     Interactive input filters entries at its input boundary; redirected lines are validated and
    ///     rejected when outside the limits. This is a UI preset, not a firmware or raw access-key limit.
    ///     Password byte buffers supplied directly to session APIs do not pass through this preset.
    /// </remarks>
    public static CredentialReaderOptions ForOathPassword() => new()
    {
        Prompt = "Enter OATH password: ",
        ConfirmPrompt = "Confirm OATH password: ",
        MinLength = 1,
        MaxLength = 128
    };
}
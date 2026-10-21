// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Core.Sessions;

/// <summary>Rejects unsupported shared options before an applet factory touches a transport.</summary>
internal static class SessionCreationOptionsValidation
{
    // Keep the applet-specific alternative in the caller: Core must not depend on WebAuthn.
    internal static void RejectUnsupportedCredentialPrompt(
        SessionCreationOptions? options,
        string application,
        string? supportedAlternative = null)
    {
        if (options?.CredentialPrompt is null)
            return;

        string message = supportedAlternative is null
            ? $"{application} does not support CredentialPrompt."
            : $"{application} sessions do not support on-demand prompting. Use {supportedAlternative}.";
        throw new ArgumentException(message, nameof(options));
    }
}

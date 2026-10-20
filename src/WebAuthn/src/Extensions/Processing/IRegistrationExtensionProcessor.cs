// Copyright Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing;

/// <summary>
/// Client-side processing of one extension for one registration attempt: contributes to the
/// authenticator request and interprets the authenticator's response for that extension.
/// </summary>
/// <remarks>
/// Instances are single-use: a PIN/UV token retry creates a fresh session and fresh processors, so any
/// per-attempt state (shared secrets, encrypted salts) is re-derived.
/// </remarks>
internal interface IRegistrationExtensionProcessor
{
    /// <summary>The extension identifier as it appears on the CTAP wire.</summary>
    string Name { get; }

    /// <summary>PIN/UV auth token permissions this extension needs, folded into the ceremony's token request.</summary>
    PinUvAuthTokenPermissions Permissions { get; }

    /// <summary>
    /// Performs any authenticator round trip the inputs depend on (key agreement for hmac-secret). Called
    /// once per attempt, after the token exists and before <c>PrepareInputs</c>. Default: nothing.
    /// </summary>
    ValueTask PrepareAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>Adds this extension's authenticator input to the CTAP2 builder.</summary>
    /// <param name="builder">The CTAP2 extension builder for the makeCredential request.</param>
    /// <param name="token">The ceremony's PIN/UV token, or null when the ceremony has none.</param>
    void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token);

    /// <summary>Derives this extension's client output from the authenticator's response.</summary>
    /// <param name="response">The CTAP2 makeCredential response.</param>
    /// <param name="authData">The response's authenticator data, decoded once by the session.</param>
    /// <param name="token">The ceremony's PIN/UV token, still live, or null.</param>
    /// <param name="outputs">Where to record the typed output.</param>
    /// <param name="cancellationToken">Cancellation token for any follow-up authenticator calls.</param>
    ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken);
}
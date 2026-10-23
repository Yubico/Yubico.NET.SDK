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
using Yubico.YubiKit.WebAuthn.Extensions.Codec;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// credBlob (CTAP 2.1-2.3 §12.2): stores a blob at registration (<paramref name="input"/> set) or
/// requests it at authentication (<c>getCredBlob</c>, <paramref name="input"/> null).
/// </summary>
internal sealed class CredBlobProcessor(CredBlobInput? input) : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor
{
    public string Name => ExtensionIdentifiers.CredBlob;
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token) =>
        CredBlobCodec.ApplyToBuilder(builder, input ?? throw new InvalidOperationException("credBlob registration requires a blob."));

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token) =>
        builder.WithGetCredBlob();

    public ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.CredBlob = CredBlobCodec.ParseRegistrationOutput(authData.ParsedExtensions);
        return ValueTask.CompletedTask;
    }

    public ValueTask PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        AuthenticationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.CredBlob = CredBlobCodec.ParseAuthenticationOutput(authData.ParsedExtensions);
        return ValueTask.CompletedTask;
    }
}
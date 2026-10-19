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
/// WebAuthn PRF (L3 §10.1.4). Today this forwards the literal "prf" input the Fido2 builder still
/// offers; the hmac-secret translation, salt hashing and per-credential selection land with #1/26a/26b.
/// </summary>
internal sealed class PrfProcessor(PrfInput input, IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials)
    : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor
{
    public string Name => ExtensionIdentifiers.Prf;
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token) =>
        PrfCodec.ApplyToBuilderForRegistration(builder, input);

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token) =>
        PrfCodec.ApplyToBuilderForAuthentication(builder, input, allowCredentials);

    public ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.Prf = PrfCodec.ParseRegistrationOutput(authData.ParsedExtensions);
        return ValueTask.CompletedTask;
    }

    public ValueTask PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        AuthenticationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.Prf = PrfCodec.ParseAuthenticationOutput(authData.ParsedExtensions);
        return ValueTask.CompletedTask;
    }
}
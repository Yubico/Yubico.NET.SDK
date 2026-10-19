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
using Yubico.YubiKit.WebAuthn.Preferences;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// previewSign (Yubico draft v4 §10.2.1): generates a signing key at registration, signs with
/// <c>signByCredential</c> at authentication. Credential selection for multi-entry maps lands with 26d.
/// </summary>
internal sealed class PreviewSignProcessor : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor
{
    private readonly PreviewSign.PreviewSignRegistrationInput? _registration;
    private readonly UserVerificationPreference _userVerification;
    private readonly PreviewSign.PreviewSignAuthenticationInput? _authentication;
    private readonly IReadOnlyList<PublicKeyCredentialDescriptor>? _allowCredentials;

    public PreviewSignProcessor(PreviewSign.PreviewSignRegistrationInput input, UserVerificationPreference userVerification)
    {
        _registration = input;
        _userVerification = userVerification;
    }

    public PreviewSignProcessor(PreviewSign.PreviewSignAuthenticationInput input, IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials)
    {
        _authentication = input;
        _allowCredentials = allowCredentials;
    }

    public string Name => ExtensionIdentifiers.PreviewSign;
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token) =>
        PreviewSignCodec.ApplyToBuilderForRegistration(builder, _registration!, _userVerification);

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token) =>
        PreviewSignCodec.ApplyToBuilderForAuthentication(builder, _authentication!, _allowCredentials);

    public ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.PreviewSign = PreviewSignCodec.ParseRegistrationOutput(authData, response.UnsignedExtensionOutputs);
        return ValueTask.CompletedTask;
    }

    public ValueTask PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        AuthenticationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.PreviewSign = PreviewSignCodec.ParseAuthenticationOutput(authData);
        return ValueTask.CompletedTask;
    }
}
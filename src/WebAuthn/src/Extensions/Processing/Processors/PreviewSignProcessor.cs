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
/// previewSign (Yubico draft v4 §10.2.1): generates a signing key at registration; at authentication
/// signs with the <c>signByCredential</c> entry of the credential the client selected.
/// </summary>
internal sealed class PreviewSignProcessor : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor
{
    private readonly PreviewSign.PreviewSignRegistrationInput? _registration;
    private readonly UserVerificationPreference _userVerification;
    private readonly PreviewSign.PreviewSignAuthenticationInput? _authentication;

    public PreviewSignProcessor(PreviewSign.PreviewSignRegistrationInput input, UserVerificationPreference userVerification)
    {
        _registration = input;
        _userVerification = userVerification;
    }

    private PreviewSignProcessor(PreviewSign.PreviewSignAuthenticationInput input)
    {
        _authentication = input;
    }

    /// <summary>
    /// Draft v4 §10.2.1 authentication: "If the size of extSign.signByCredential does not equal the size
    /// of pkOptions.allowCredentials, return a DOMException whose name is NotSupportedError" and, for an
    /// allowed credential without an entry, "If signInputs is undefined, return ... SyntaxError".
    /// </summary>
    public static PreviewSignProcessor ForAuthentication(
        PreviewSign.PreviewSignAuthenticationInput input,
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials)
    {
        if (allowCredentials is null || allowCredentials.Count != input.SignByCredential.Count)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "previewSign signByCredential must have exactly one entry per allowCredentials entry (previewSign draft v4 §10.2.1)");
        }

        foreach (var allowed in allowCredentials)
        {
            if (FindEntry(input, allowed) is null)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.InvalidRequest,
                    "previewSign signByCredential is missing an entry for an allowed credential (previewSign draft v4 §10.2.1)");
            }
        }

        return new PreviewSignProcessor(input);
    }

    private static PreviewSign.PreviewSignSigningParams? FindEntry(
        PreviewSign.PreviewSignAuthenticationInput input,
        PublicKeyCredentialDescriptor credential)
    {
        if (input.SignByCredential.TryGetValue(credential.Id, out var exact))
        {
            return exact;
        }

        // The relying party may have supplied a dictionary without ByteArrayKeyComparer; match by content.
        foreach (var (id, signingParams) in input.SignByCredential)
        {
            if (id.Span.SequenceEqual(credential.Id.Span))
            {
                return signingParams;
            }
        }

        return null;
    }

    public string Name => ExtensionIdentifiers.PreviewSign;
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token) =>
        PreviewSignCodec.ApplyToBuilderForRegistration(builder, _registration!, _userVerification);

    // Draft v4 §10.2.1: "Let signInputs be extSign.signByCredential[chosenCredentialIdB64]". No selected
    // credential (nothing on the key matched the allow list) means nothing to sign.
    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token)
    {
        if (selected is not null && FindEntry(_authentication!, selected) is { } signingParams)
        {
            builder.WithPreviewSign(PreviewSignCodec.ToCtap(signingParams));
        }
    }

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
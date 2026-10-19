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
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// WebAuthn prf (L3 §10.1.4) over CTAP hmac-secret (§12.7), as python-fido2's and yubikit-android's
/// <c>HmacSecretExtension</c>: registration asks for <c>hmac-secret: true</c> and reports <c>enabled</c>;
/// authentication hashes the relying party's inputs into salts, performs one key agreement, sends the
/// salts encrypted and authenticated under the shared secret, and decrypts the result with it.
/// </summary>
/// <remarks>
/// User verification is not overridden for PRF (maintainer decision: parity with python-fido2 and
/// yubikit-android, which do not implement the L3 §10.1.4 "overrides the UserVerificationRequirement"
/// sentence either). Relying parties that need stable PRF output across ceremonies should request
/// <c>userVerification: required</c>, since CTAP keeps separate PRFs for verified and unverified use.
/// </remarks>
internal sealed class PrfProcessor(PrfInput input, ExtensionContext context)
    : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor, IDisposable
{
    private PinUvSharedSecret? _sharedSecret;

    public string Name => ExtensionIdentifiers.HmacSecret;
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    private bool HasEvaluation => input.First is not null || input.EvalByCredential is { Count: > 0 };

    /// <summary>One key agreement per attempt when salts will be sent (python-fido2 get_assertion L342-343).</summary>
    public async ValueTask PrepareAsync(CancellationToken cancellationToken)
    {
        if (HasEvaluation && _sharedSecret is null)
        {
            _sharedSecret = await context.Backend.EncapsulateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Registration: L3 §10.1.4 "Set hmac-secret to true in the authenticator extensions input."
    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token) => builder.WithHmacSecret();

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token)
    {
        var ev = SelectInputs(selected);
        if (ev is null || _sharedSecret is null)
        {
            return;
        }

        // L3 §10.1.4: salt1 = SHA-256("WebAuthn PRF" || 0x00 || ev.first), salt2 likewise when present;
        // CTAP §12.7 getAssertion input: {1: keyAgreement, 2: saltEnc, 3: saltAuth, 4: pinUvAuthProtocol}.
        var salt1 = PrfInput.ComputeSalt(ev.First.Span);
        var salt2 = ev.Second is { } second ? PrfInput.ComputeSalt(second.Span) : null;
        try
        {
            builder.WithHmacSecret(context.Protocol, _sharedSecret.SharedSecret.Span, _sharedSecret.KeyAgreement, salt1, salt2);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(salt1);
            if (salt2 is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(salt2);
            }
        }
    }

    /// <summary>
    /// L3 §10.1.4: "If evalByCredential is present and contains an entry whose key is the base64url
    /// encoding of the credential ID that will be returned, let ev be the value of that entry. If ev is
    /// null and eval is present, then let ev be the value of eval."
    /// </summary>
    private PrfInputValues? SelectInputs(PublicKeyCredentialDescriptor? selected)
    {
        if (selected is not null
            && input.EvalByCredential is { } byCredential
            && byCredential.TryGetValue(Base64Url.Encode(selected.Id.Span), out var forCredential))
        {
            return forCredential;
        }

        return input.First is { } first ? new PrfInputValues { First = first, Second = input.Second } : null;
    }

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
        if (_sharedSecret is not null
            && authData.ParsedExtensions.TryGetValue(ExtensionIdentifiers.HmacSecret, out var rawOutput))
        {
            var reader = new System.Formats.Cbor.CborReader(rawOutput, System.Formats.Cbor.CborConformanceMode.Lax);
            var results = PrfCodec.DecryptResults(context.Protocol, _sharedSecret.SharedSecret.Span, reader.ReadByteString());
            outputs.Prf = results is null ? null : new PrfAuthenticationOutput(results);
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _sharedSecret?.Dispose();
        _sharedSecret = null;
    }
}
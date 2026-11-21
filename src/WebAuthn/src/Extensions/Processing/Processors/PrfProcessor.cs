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

using System.Formats.Cbor;
using System.Security.Cryptography;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions.Codec;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;
using Yubico.YubiKit.WebAuthn.Util;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// WebAuthn prf (L3 §10.1.4) over CTAP hmac-secret (2.1 §12.5; 2.2–2.3 §12.7) and
/// hmac-secret-mc (2.2–2.3 §12.8). Registration asks
/// for <c>hmac-secret: true</c> (plus a creation-time evaluation when the authenticator advertises
/// hmac-secret-mc) and reports <c>enabled</c>;
/// authentication hashes the relying party's inputs into salts, performs one key agreement, sends the
/// salts encrypted and authenticated under the shared secret, and decrypts the result with it.
/// </summary>
/// <remarks>
/// WebAuthn L3 REC 2026-08-25 §10.1.4 requires the verified PRF and a UV-preference override if needed.
/// Supported evaluation makes the ceremony require UV. Enablement alone does not. Results are not
/// exposed unless the authenticator data reports UV.
/// </remarks>
internal sealed class PrfProcessor : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor, IDisposable
{
    public static bool RequiresVerifiedCreationEvaluation(PrfInput? input, ExtensionContext context) =>
        input?.First is not null && context.Supports(ExtensionIdentifiers.HmacSecret)
        && context.Supports(ExtensionIdentifiers.HmacSecretMakeCredential);

    public static bool RequiresVerifiedAssertionEvaluation(PrfInput? input, ExtensionContext context, PublicKeyCredentialDescriptor? selected) =>
        input is not null && context.Supports(ExtensionIdentifiers.HmacSecret) && SelectInputs(input, selected) is not null;
    private readonly PrfInput _input;
    private readonly ExtensionContext _context;
    private readonly bool _evaluateAtCreation;
    private PinUvSharedSecret? _sharedSecret;

    private PrfProcessor(PrfInput input, ExtensionContext context, bool evaluateAtCreation)
    {
        _input = input;
        _context = context;
        _evaluateAtCreation = evaluateAtCreation;
    }

    /// <summary>L3 §10.1.4 registration: "If evalByCredential is present, return ... NotSupportedError".</summary>
    public static PrfProcessor ForRegistration(PrfInput input, ExtensionContext context)
    {
        if (input.EvalByCredential is not null)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "prf.evalByCredential is not allowed at registration (WebAuthn L3 §10.1.4)");
        }

        // L3 §10.1.4: evaluation at creation needs "a future extension to [FIDO-CTAP]"; CTAP 2.2 §12.8
        // hmac-secret-mc is that extension.
        bool evaluateAtCreation = input.First is not null && context.Supports(ExtensionIdentifiers.HmacSecretMakeCredential);
        return new PrfProcessor(input, context, evaluateAtCreation);
    }

    /// <summary>
    /// L3 §10.1.4 authentication: "If evalByCredential is not empty but allowCredentials is empty, return
    /// ... NotSupportedError. If any key in evalByCredential is the empty string, or is not a valid
    /// base64url encoding, or does not equal the id of some element of allowCredentials after performing
    /// base64url decoding, then return ... SyntaxError."
    /// </summary>
    public static PrfProcessor ForAuthentication(
        PrfInput input,
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials,
        ExtensionContext context)
    {
        if (input.EvalByCredential is { Count: > 0 } byCredential)
        {
            if (allowCredentials is null || allowCredentials.Count == 0)
            {
                throw new WebAuthnClientError(
                    WebAuthnClientErrorCode.NotSupported,
                    "prf.evalByCredential requires a non-empty allowCredentials (WebAuthn L3 §10.1.4)");
            }

            foreach (var key in byCredential.Keys)
            {
                if (!TryDecodeCredentialId(key, out var id)
                    || !allowCredentials.Any(descriptor => descriptor.Id.Span.SequenceEqual(id)))
                {
                    throw new WebAuthnClientError(
                        WebAuthnClientErrorCode.InvalidRequest,
                        "prf.evalByCredential keys must be the base64url credential ids of allowCredentials entries (WebAuthn L3 §10.1.4)");
                }
            }
        }

        return new PrfProcessor(input, context, evaluateAtCreation: false);
    }

    private static bool TryDecodeCredentialId(string key, out byte[] id)
    {
        id = [];
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        try
        {
            id = Base64Url.Decode(key);
            return id.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string Name => ExtensionIdentifiers.HmacSecret;
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    /// <summary>One key agreement per attempt, performed only when salts will be sent.</summary>
    public ValueTask PrepareAsync(CancellationToken cancellationToken) => PrepareSecretAsync(_evaluateAtCreation, cancellationToken);

    public ValueTask PrepareAsync(PublicKeyCredentialDescriptor? selected, CancellationToken cancellationToken) =>
        PrepareSecretAsync(SelectInputs(_input, selected) is not null, cancellationToken);

    internal bool InputsDiffer(IReadOnlyList<PublicKeyCredentialDescriptor> chunk)
    {
        var first = SelectInputs(_input, chunk[0]);
        for (int i = 1; i < chunk.Count; i++)
        {
            if (!SameInputs(first, SelectInputs(_input, chunk[i])))
                return true;
        }
        return false;
    }

    internal bool HasInput(PublicKeyCredentialDescriptor selected) => SelectInputs(_input, selected) is not null;

    private static bool SameInputs(PrfInputValues? left, PrfInputValues? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return CryptographicOperations.FixedTimeEquals(left.First.Span, right.First.Span)
            && left.Second.HasValue == right.Second.HasValue
            && CryptographicOperations.FixedTimeEquals(left.Second.GetValueOrDefault().Span, right.Second.GetValueOrDefault().Span);
    }

    private async ValueTask PrepareSecretAsync(bool needsSecret, CancellationToken cancellationToken)
    {
        if (needsSecret && _sharedSecret is null)
        {
            _sharedSecret = await _context.Backend.EncapsulateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Registration: L3 §10.1.4 "Set hmac-secret to true in the authenticator extensions input", plus the
    // §12.8 creation-time evaluation when available.
    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token)
    {
        if (_evaluateAtCreation && _sharedSecret is not null && _input.First is { } first)
        {
            WithSalts(first, _input.Second, (salt1, salt2) =>
                builder.WithHmacSecretMakeCredential(_context.Protocol, _sharedSecret.SharedSecret.Span, _sharedSecret.KeyAgreement, salt1, salt2));
            return;
        }

        builder.WithHmacSecret();
    }

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token)
        => PrepareAssertionInputs(builder, selected, omit: false);

    internal void PrepareAssertionInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, bool omit)
    {
        var ev = omit ? null : SelectInputs(_input, selected);
        if (ev is null)
        {
            // A previous rejected chunk may have needed key agreement. An assertion that carries no
            // PRF input must not use that secret to process an unsolicited hmac-secret output.
            Dispose();
            return;
        }
        if (_sharedSecret is null)
        {
            return;
        }

        // CTAP §12.7 getAssertion input: {1: keyAgreement, 2: saltEnc, 3: saltAuth, 4: pinUvAuthProtocol}.
        WithSalts(ev.First, ev.Second, (salt1, salt2) =>
            builder.WithHmacSecret(_context.Protocol, _sharedSecret.SharedSecret.Span, _sharedSecret.KeyAgreement, salt1, salt2));
    }

    private delegate void SaltAction(ReadOnlySpan<byte> salt1, ReadOnlySpan<byte> salt2);

    /// <summary>L3 §10.1.4: salt = SHA-256("WebAuthn PRF" || 0x00 || input); these salt arrays are zeroed after use.</summary>
    private static void WithSalts(ReadOnlyMemory<byte> first, ReadOnlyMemory<byte>? second, SaltAction action)
    {
        var salt1 = PrfInput.ComputeSalt(first.Span);
        byte[]? salt2 = null;
        try
        {
            salt2 = second is { } s ? PrfInput.ComputeSalt(s.Span) : null;
            action(salt1, salt2);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt1);
            if (salt2 is not null)
            {
                CryptographicOperations.ZeroMemory(salt2);
            }
        }
    }

    /// <summary>
    /// L3 §10.1.4: "If evalByCredential is present and contains an entry whose key is the base64url
    /// encoding of the credential ID that will be returned, let ev be the value of that entry. If ev is
    /// null and eval is present, then let ev be the value of eval."
    /// </summary>
    private static PrfInputValues? SelectInputs(PrfInput input, PublicKeyCredentialDescriptor? selected)
    {
        if (selected is not null
            && input.EvalByCredential is { } byCredential
            && byCredential.TryGetValue(Base64Url.Encode(selected.Id.Span), out var forCredential)
            && forCredential is not null)
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
        var output = PrfCodec.ParseRegistrationOutput(authData.ParsedExtensions);
        var results = _evaluateAtCreation && authData.UserVerified
            ? DecryptOutput(authData, ExtensionIdentifiers.HmacSecretMakeCredential) : null;
        outputs.Prf = output with { Results = results };
        return ValueTask.CompletedTask;
    }

    public ValueTask PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        AuthenticationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        var results = authData.UserVerified ? DecryptOutput(authData, ExtensionIdentifiers.HmacSecret) : null;
        outputs.Prf = results is null ? null : new PrfAuthenticationOutput(results);
        return ValueTask.CompletedTask;
    }

    private PrfEvaluationResults? DecryptOutput(WebAuthnAuthenticatorData authData, string extension)
    {
        if (_sharedSecret is null || !authData.ParsedExtensions.TryGetValue(extension, out var rawOutput))
        {
            return null;
        }

        var reader = new CborReader(rawOutput, CborConformanceMode.Lax);
        if (reader.PeekState() != CborReaderState.ByteString)
        {
            return null;
        }

        var ciphertext = reader.ReadByteString();
        try
        {
            return PrfCodec.DecryptResults(_context.Protocol, _sharedSecret.SharedSecret.Span, ciphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    public void Dispose()
    {
        _sharedSecret?.Dispose();
        _sharedSecret = null;
    }
}
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
using Yubico.YubiKit.Fido2.Credentials;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing;

/// <summary>
/// The extension processors for one authentication attempt. See <see cref="RegistrationExtensionSession"/>.
/// </summary>
internal sealed class AuthenticationExtensionSession : IDisposable
{
    private readonly IReadOnlyList<IAuthenticationExtensionProcessor> _processors;
    private readonly bool _requested;

    private AuthenticationExtensionSession(IReadOnlyList<IAuthenticationExtensionProcessor> processors, bool requested)
    {
        _processors = processors;
        _requested = requested;
    }

    /// <summary>
    /// Creates the session for the extensions the relying party asked for and the authenticator
    /// advertises; see <see cref="RegistrationExtensionSession.Create"/>.
    /// </summary>
    /// <param name="inputs">The relying party's extension inputs, or null.</param>
    /// <param name="allowCredentials">The relying party's allow list; per-credential inputs are validated against it.</param>
    /// <param name="context">GetInfo, PIN/UV protocol and backend for this ceremony.</param>
    public static AuthenticationExtensionSession Create(
        AuthenticationExtensionInputs? inputs,
        IReadOnlyList<PublicKeyCredentialDescriptor>? allowCredentials,
        ExtensionContext context)
    {
        if (inputs is null)
        {
            return new AuthenticationExtensionSession([], requested: false);
        }

        var processors = new List<IAuthenticationExtensionProcessor>();
        if (inputs.GetCredBlob && context.Supports(ExtensionIdentifiers.CredBlob)) processors.Add(new CredBlobProcessor(input: null));
        if (inputs.LargeBlob is not null && LargeBlobProcessor.IsSupported(context)) processors.Add(new LargeBlobProcessor(inputs.LargeBlob));
        if (inputs.Prf is not null && context.Supports(ExtensionIdentifiers.HmacSecret)) processors.Add(PrfProcessor.ForAuthentication(inputs.Prf, allowCredentials, context));
        if (inputs.PreviewSign is not null && context.Supports(ExtensionIdentifiers.PreviewSign)) processors.Add(PreviewSignProcessor.ForAuthentication(inputs.PreviewSign, allowCredentials));
        return new AuthenticationExtensionSession(processors, requested: true);
    }

    /// <summary>The union of the processors' token permissions.</summary>
    public PinUvAuthTokenPermissions Permissions =>
        _processors.Aggregate(PinUvAuthTokenPermissions.None, (acc, p) => acc | p.Permissions);

    /// <summary>Runs every processor's authenticator-dependent preparation (key agreement etc.).</summary>
    public async ValueTask PrepareAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
        {
            await processor.PrepareAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Builds the CTAP2 extensions map, or null when no processor adds an input.</summary>
    public ReadOnlyMemory<byte>? PrepareInputs(PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token)
    {
        var builder = new ExtensionBuilder();
        foreach (var processor in _processors)
        {
            processor.PrepareInputs(builder, selected, token);
        }

        return builder.Build();
    }

    /// <summary>
    /// Derives the client extension outputs for one assertion, or null when the relying party requested
    /// no extensions. A malformed or wrong-typed output from one extension drops that extension's output only.
    /// </summary>
    public async ValueTask<AuthenticationExtensionOutputs?> PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        CancellationToken cancellationToken)
    {
        if (!_requested)
        {
            return null;
        }

        var outputs = new AuthenticationExtensionOutputsBuilder();
        foreach (var processor in _processors)
        {
            try
            {
                await processor.PrepareOutputsAsync(response, authData, token, outputs, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                // Malformed or wrong-typed extension output: skip this extension only (WebAuthn L3 §9.4
                // "ignore any extension output it ... cannot process"). System.Formats.Cbor reports bad
                // encodings as CborContentException and a wrong major type as InvalidOperationException.
            }
        }

        return outputs.Build();
    }

    /// <summary>Releases per-attempt secrets held by processors (shared secrets).</summary>
    public void Dispose()
    {
        foreach (var processor in _processors)
        {
            (processor as IDisposable)?.Dispose();
        }
    }
}
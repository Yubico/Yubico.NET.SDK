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
using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;
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
    /// The client requires a nonempty original allow list for writes but deliberately accepts more than one entry.
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
        if (inputs.LargeBlob is not null && LargeBlobProcessor.ForAuthentication(inputs.LargeBlob, context) is { } largeBlob) processors.Add(largeBlob);
        if (inputs.Prf is not null && context.Supports(ExtensionIdentifiers.HmacSecret)) processors.Add(PrfProcessor.ForAuthentication(inputs.Prf, allowCredentials, context));
#pragma warning disable YKEXP001 // Bridge only explicitly requested previewSign inputs.
        if (inputs.PreviewSign is not null && context.Supports(ExtensionIdentifiers.PreviewSign)) processors.Add(PreviewSignProcessor.ForAuthentication(inputs.PreviewSign, allowCredentials));
#pragma warning restore YKEXP001
        return new AuthenticationExtensionSession(processors, requested: true);
    }

    /// <summary>The union of the processors' token permissions.</summary>
    public PinUvAuthTokenPermissions Permissions =>
        _processors.Aggregate(PinUvAuthTokenPermissions.None, (acc, p) => acc | p.Permissions);

    /// <summary>Runs every processor's authenticator-dependent preparation (key agreement etc.).</summary>
    public async ValueTask PrepareAsync(CancellationToken cancellationToken, PublicKeyCredentialDescriptor? selected = null)
    {
        try
        {
            foreach (var processor in _processors)
                await processor.PrepareAsync(selected, cancellationToken).ConfigureAwait(false);
        }
        catch (CtapException ex)
        {
            throw WebAuthnClient.MapCtapStatusToWebAuthnError(ex);
        }
    }

    /// <summary>Builds the CTAP2 extensions map, or null when no processor adds an input.</summary>
    public ReadOnlyMemory<byte>? PrepareInputs(PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token) =>
        PrepareInputs(selected, token, out _);

    public ReadOnlyMemory<byte>? PrepareInputs(PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token, out bool hasPreviewSign)
    {
        var builder = new ExtensionBuilder();
        foreach (var processor in _processors)
        {
            processor.PrepareInputs(builder, selected, token);
        }

        hasPreviewSign = builder.HasPreviewSign;
        return builder.Build();
    }

    /// <summary>
    /// Derives the client extension outputs for one assertion, or null when the relying party requested
    /// no extensions. CborContentException or InvalidOperationException from a processor omits that
    /// processor's output. A supported largeBlob read keeps its output present with a null Blob when
    /// the returned key is missing or invalid, no data is available, or a known storage or inflation
    /// failure prevents the read. Cancellation and other unhandled errors propagate even when the
    /// assertion itself succeeded.
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
        try
        {
            foreach (var processor in _processors)
            {
                try
                {
                    await processor.PrepareOutputsAsync(response, authData, token, outputs, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
                {
                    // SDK policy: omit malformed optional processor output, not cancellation or explicit client errors.
                }
            }
            return outputs.Build();
        }
        catch
        {
            Util.SensitiveMemory.Zero(outputs.CredBlob?.Blob);
            Util.SensitiveMemory.ZeroPrf(outputs.Prf?.Results);
            if (outputs.LargeBlob?.Blob is { } blob)
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(blob.Span);
            throw;
        }
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
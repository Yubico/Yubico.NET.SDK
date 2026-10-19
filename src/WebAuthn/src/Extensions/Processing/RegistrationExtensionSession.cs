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
using Yubico.YubiKit.WebAuthn.Client.Registration;
using Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing;

/// <summary>
/// The extension processors for one registration attempt. Created after GetInfo, consulted for the
/// token permissions, asked for inputs once the token exists and for outputs once the response is in.
/// </summary>
internal sealed class RegistrationExtensionSession
{
    private readonly IReadOnlyList<IRegistrationExtensionProcessor> _processors;
    private readonly bool _requested;

    private RegistrationExtensionSession(IReadOnlyList<IRegistrationExtensionProcessor> processors, bool requested)
    {
        _processors = processors;
        _requested = requested;
    }

    /// <summary>Creates the session for the extensions the relying party asked for.</summary>
    /// <param name="inputs">The relying party's extension inputs, or null.</param>
    /// <param name="options">The registration options (previewSign derives its flags from the UV preference).</param>
    /// <param name="requireResidentKey">The effective requireResidentKey the client resolved (credProps reports it).</param>
    public static RegistrationExtensionSession Create(
        RegistrationExtensionInputs? inputs,
        RegistrationOptions options,
        bool requireResidentKey)
    {
        if (inputs is null)
        {
            return new RegistrationExtensionSession([], requested: false);
        }

        var processors = new List<IRegistrationExtensionProcessor>();
        if (inputs.CredProtect is not null) processors.Add(new CredProtectProcessor(inputs.CredProtect));
        if (inputs.CredBlob is not null) processors.Add(new CredBlobProcessor(inputs.CredBlob));
        if (inputs.MinPinLength is not null) processors.Add(new MinPinLengthProcessor());
        if (inputs.LargeBlob is not null) processors.Add(new LargeBlobProcessor(inputs.LargeBlob));
        if (inputs.Prf is not null) processors.Add(new PrfProcessor(inputs.Prf, allowCredentials: null));
        if (inputs.CredProps is not null) processors.Add(new CredPropsProcessor(requireResidentKey));
        if (inputs.PreviewSign is not null) processors.Add(new PreviewSignProcessor(inputs.PreviewSign, options.UserVerification));
        return new RegistrationExtensionSession(processors, requested: true);
    }

    /// <summary>The union of the processors' token permissions.</summary>
    public PinUvAuthTokenPermissions Permissions =>
        _processors.Aggregate(PinUvAuthTokenPermissions.None, (acc, p) => acc | p.Permissions);

    /// <summary>Builds the CTAP2 extensions map, or null when no processor adds an input.</summary>
    public ReadOnlyMemory<byte>? PrepareInputs(PinUvAuthTokenSession? token)
    {
        var builder = new ExtensionBuilder();
        foreach (var processor in _processors)
        {
            processor.PrepareInputs(builder, token);
        }

        return builder.Build();
    }

    /// <summary>
    /// Derives the client extension outputs, or null when the relying party requested no extensions.
    /// A malformed or wrong-typed output from one extension drops that extension's output only.
    /// </summary>
    public async ValueTask<RegistrationExtensionOutputs?> PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        CancellationToken cancellationToken)
    {
        if (!_requested)
        {
            return null;
        }

        var outputs = new RegistrationExtensionOutputsBuilder();
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
}
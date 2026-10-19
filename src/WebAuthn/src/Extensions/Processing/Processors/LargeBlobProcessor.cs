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
using Yubico.YubiKit.Fido2.Ctap;
using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.Fido2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// WebAuthn largeBlob (L3 §10.1.5) over CTAP largeBlobKey (§12.3) and authenticatorLargeBlobs (§6.10),
/// as python-fido2's and yubikit-android's <c>LargeBlobExtension</c>: registration asks for the key when
/// the credential is discoverable and reports <c>supported</c> from response key 0x05; authentication
/// reads or writes the blob stored under the asserted credential's key.
/// </summary>
internal sealed class LargeBlobProcessor : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor
{
    private readonly LargeBlobInput _input;
    private readonly ExtensionContext _context;
    private readonly bool _requestKey;

    private LargeBlobProcessor(LargeBlobInput input, ExtensionContext context, bool requestKey)
    {
        _input = input;
        _context = context;
        _requestKey = requestKey;
    }

    /// <summary>CTAP §12.3: the largeBlobKey extension plus the largeBlobs option (python-fido2 LargeBlobExtension.is_supported).</summary>
    public static bool IsSupported(ExtensionContext context) =>
        context.Supports(ExtensionIdentifiers.LargeBlobKey) && context.OptionEnabled("largeBlobs");

    /// <summary>
    /// L3 §10.1.5 registration: "If read or write is present, return ... NotSupportedError"; "If support
    /// is required and the authenticator does not support large blobs, return ... NotSupportedError".
    /// The key is requested only for a discoverable credential (CTAP §12.3 rk rule); otherwise the
    /// credential cannot store blobs and <c>supported</c> is false.
    /// </summary>
    public static LargeBlobProcessor? ForRegistration(LargeBlobInput input, bool requireResidentKey, ExtensionContext context)
    {
        if (input.Read || input.Write is not null)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "largeBlob read and write are authentication inputs (WebAuthn L3 §10.1.5)");
        }

        bool supported = IsSupported(context);
        if (input.Support == LargeBlobSupport.Required && !supported)
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "largeBlob support is required but the authenticator does not support large blobs (WebAuthn L3 §10.1.5)");
        }

        return new LargeBlobProcessor(input, context, requestKey: supported && requireResidentKey);
    }

    /// <summary>
    /// L3 §10.1.5 authentication: "If support is present, or if both read and write are present, return
    /// ... NotSupportedError". An authenticator without large-blob support is skipped (session rule).
    /// </summary>
    public static LargeBlobProcessor? ForAuthentication(LargeBlobInput input, ExtensionContext context)
    {
        if (input.Support is not null || (input.Read && input.Write is not null))
        {
            throw new WebAuthnClientError(
                WebAuthnClientErrorCode.NotSupported,
                "largeBlob authentication takes either read or write, never support (WebAuthn L3 §10.1.5)");
        }

        return IsSupported(context) ? new LargeBlobProcessor(input, context, requestKey: true) : null;
    }

    public string Name => ExtensionIdentifiers.LargeBlobKey;

    /// <summary>CTAP §6.10.2: writing the array needs a token with the LargeBlobWrite permission (python-fido2 L467).</summary>
    public PinUvAuthTokenPermissions Permissions =>
        _input.Write is not null ? PinUvAuthTokenPermissions.LargeBlobWrite : PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token)
    {
        if (_requestKey)
        {
            builder.WithLargeBlobKey();
        }
    }

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token) =>
        builder.WithLargeBlobKey();

    // L3 §10.1.5: "If an authenticator is selected and the selected authenticator supports large blobs,
    // set supported to true, and false otherwise." CTAP §12.3 returns the key in response key 0x05,
    // "not in the extensions field of the authenticator data".
    public ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.LargeBlob = new LargeBlobRegistrationOutput(Supported: _requestKey && response.LargeBlobKey is not null);
        return ValueTask.CompletedTask;
    }

    public async ValueTask PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        AuthenticationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        if (response.LargeBlobKey is not { } largeBlobKey)
        {
            return; // credential created without a largeBlobKey: nothing to read or write
        }

        if (_input.Read)
        {
            // L3 §10.1.5: "set blob to the result" (python-fido2 L448-455).
            var storage = _context.Backend.CreateLargeBlobStorage(token: null);
            var blob = await storage.GetBlobAsync(largeBlobKey, cancellationToken).ConfigureAwait(false);
            outputs.LargeBlob = new LargeBlobAuthenticationOutput(Blob: blob);
        }
        else if (_input.Write is { } data)
        {
            // L3 §10.1.5: "set written to true if the write succeeded and false otherwise".
            bool written;
            try
            {
                var storage = _context.Backend.CreateLargeBlobStorage(token);
                await storage.SetBlobAsync(largeBlobKey, data, cancellationToken).ConfigureAwait(false);
                written = true;
            }
            catch (Exception ex) when (ex is CtapException or InvalidOperationException)
            {
                written = false;
            }

            outputs.LargeBlob = new LargeBlobAuthenticationOutput(Written: written);
        }
    }
}
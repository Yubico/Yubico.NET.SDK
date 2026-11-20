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

using Yubico.YubiKit.Ctap2;
using Yubico.YubiKit.Ctap2.Credentials;
using Yubico.YubiKit.Ctap2.Extensions;
using Yubico.YubiKit.Ctap2.Pin;
using Yubico.YubiKit.WebAuthn.Client;
using Yubico.YubiKit.WebAuthn.Extensions.Inputs;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// WebAuthn largeBlob (L3 §10.1.5) over CTAP largeBlobKey (§12.3) and authenticatorLargeBlobs (§6.10).
/// Registration asks for the key when the credential is discoverable and reports <c>supported</c> from
/// response key 0x05; authentication
/// reads or writes the blob stored under the asserted credential's key.
/// </summary>
/// <remarks>
/// Supports the largeBlobKey/storage-array route only, not the alternative direct largeBlob extension
/// in CTAP 2.2–2.3 §12.4. Decrypted read results currently have no disposal or zeroing guarantee.
/// </remarks>
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

    /// <summary>CTAP §12.3: supported when the authenticator advertises both the largeBlobKey extension and the largeBlobs option.</summary>
    public static bool IsSupported(ExtensionContext context) =>
        context.Supports(ExtensionIdentifiers.LargeBlobKey) && context.OptionEnabled("largeBlobs");

    /// <summary>
    /// Rejects registration read/write inputs and rejects Required when this authenticator lacks the
    /// supported storage route. The key is requested only for a discoverable credential (CTAP §12.3
    /// rk rule). Current limitation: Required does not reject a non-discoverable registration or a
    /// successful response without the requested key; those paths report supported as false.
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
    /// ... NotSupportedError". An authenticator without the supported storage route is skipped.
    /// The original allow list is not available here, so the required single-entry check for writes
    /// is currently missing; narrowing the final assertion request is not equivalent validation.
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

    /// <summary>The SDK storage path currently requires a token with LargeBlobWrite for every write.</summary>
    /// <remarks>
    /// CTAP §6.10.2 permits pinless writes on authenticators without configured verification when
    /// alwaysUv is not enabled; this SDK path does not.
    /// </remarks>
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

    // Report whether this credential returned the requested key, not merely device capability.
    // CTAP §12.3 places it in top-level response key 0x05, not authData.extensions. A missing key
    // currently produces false even for Required rather than failing registration.
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
            return; // No response key: omit the operation result; the cause is not diagnosed here.
        }

        if (_input.Read)
        {
            // Read errors are not normalized here: CTAP errors propagate; the session suppresses
            // InvalidOperationException. A null storage result converts to present empty memory below,
            // so nullable presence and length do not distinguish absent data from a stored empty blob.
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
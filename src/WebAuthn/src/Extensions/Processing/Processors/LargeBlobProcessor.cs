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
/// WebAuthn largeBlob (L3 §10.1.5) over CTAP largeBlobKey (§12.3). Registration requests the key and
/// reports <c>supported</c>; authentication read/write is not implemented yet (largeBlob audit group).
/// </summary>
internal sealed class LargeBlobProcessor(LargeBlobInput input) : IRegistrationExtensionProcessor, IAuthenticationExtensionProcessor
{
    public string Name => ExtensionIdentifiers.LargeBlobKey;

    /// <summary>python-fido2 LargeBlobExtension.is_supported: the largeBlobKey extension plus the largeBlobs option.</summary>
    public static bool IsSupported(ExtensionContext context) =>
        context.Supports(ExtensionIdentifiers.LargeBlobKey) && context.OptionEnabled("largeBlobs");
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token) =>
        LargeBlobCodec.ApplyToBuilder(builder, input);

    public void PrepareInputs(ExtensionBuilder builder, PublicKeyCredentialDescriptor? selected, PinUvAuthTokenSession? token) =>
        throw new WebAuthnClientError(
            WebAuthnClientErrorCode.NotSupported,
            "LargeBlob authentication operations are not yet implemented (Phase 6 scope deferred). Upgrade SDK for full support.");

    public ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.LargeBlob = LargeBlobCodec.ParseRegistrationOutput(authData.ParsedExtensions);
        return ValueTask.CompletedTask;
    }

    public ValueTask PrepareOutputsAsync(
        GetAssertionResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        AuthenticationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.LargeBlob = LargeBlobCodec.ParseAuthenticationOutput(authData.ParsedExtensions);
        return ValueTask.CompletedTask;
    }
}
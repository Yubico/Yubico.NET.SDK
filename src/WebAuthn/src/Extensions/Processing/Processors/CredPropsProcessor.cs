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
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing.Processors;

/// <summary>
/// credProps (WebAuthn L3 §10.1.3): no authenticator input. The <c>rk</c> output is "the value of
/// requireResidentKey that was used", which the client resolved against GetInfo before the request.
/// </summary>
internal sealed class CredPropsProcessor(bool requireResidentKey) : IRegistrationExtensionProcessor
{
    public string Name => "credProps";
    public PinUvAuthTokenPermissions Permissions => PinUvAuthTokenPermissions.None;

    public void PrepareInputs(ExtensionBuilder builder, PinUvAuthTokenSession? token)
    {
        // Client-side only: nothing is sent to the authenticator.
    }

    public ValueTask PrepareOutputsAsync(
        MakeCredentialResponse response,
        WebAuthnAuthenticatorData authData,
        PinUvAuthTokenSession? token,
        RegistrationExtensionOutputsBuilder outputs,
        CancellationToken cancellationToken)
    {
        outputs.CredProps = new CredPropsOutput(requireResidentKey);
        return ValueTask.CompletedTask;
    }
}
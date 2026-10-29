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

using Yubico.YubiKit.Fido2.Extensions;
using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions.Processing;

/// <summary>Mutable sink the authentication processors write into; <see cref="Build"/> yields the typed record.</summary>
internal sealed class AuthenticationExtensionOutputsBuilder
{
    public CredBlobAssertionOutput? CredBlob { get; set; }
    public LargeBlobAuthenticationOutput? LargeBlob { get; set; }
    public PrfAuthenticationOutput? Prf { get; set; }
    public PreviewSign.PreviewSignAuthenticationOutput? PreviewSign { get; set; }

    public AuthenticationExtensionOutputs Build() => new()
    {
        CredBlob = CredBlob,
        LargeBlob = LargeBlob,
        Prf = Prf,
        PreviewSign = PreviewSign
    };
}
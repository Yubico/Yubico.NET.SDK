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

/// <summary>Mutable sink the registration processors write into; <see cref="Build"/> yields the typed record.</summary>
internal sealed class RegistrationExtensionOutputsBuilder
{
    public CredProtectOutput? CredProtect { get; set; }
    public CredBlobMakeCredentialOutput? CredBlob { get; set; }
    public MinPinLengthOutput? MinPinLength { get; set; }
    public LargeBlobRegistrationOutput? LargeBlob { get; set; }
    public PrfRegistrationOutput? Prf { get; set; }
    public CredPropsOutput? CredProps { get; set; }
    public PreviewSign.PreviewSignRegistrationOutput? PreviewSign { get; set; }

    public RegistrationExtensionOutputs Build() => new()
    {
        CredProtect = CredProtect,
        CredBlob = CredBlob,
        MinPinLength = MinPinLength,
        LargeBlob = LargeBlob,
        Prf = Prf,
        CredProps = CredProps,
        PreviewSign = PreviewSign
    };
}
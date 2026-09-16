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

namespace Yubico.YubiKit.WebAuthn.Extensions;

/// <summary>
/// Extension outputs from WebAuthn registration (MakeCredential).
/// </summary>
public sealed record class RegistrationExtensionOutputs
{
    /// <summary>Gets the credential protection policy output.</summary>
    public Outputs.CredProtectOutput? CredProtect { get; init; }
    /// <summary>Gets the credential blob storage result.</summary>
    public CredBlobMakeCredentialOutput? CredBlob { get; init; }
    /// <summary>Gets the minimum PIN length output.</summary>
    public MinPinLengthOutput? MinPinLength { get; init; }
    /// <summary>Gets the large blob support result.</summary>
    public Outputs.LargeBlobRegistrationOutput? LargeBlob { get; init; }
    /// <summary>Gets the PRF support result.</summary>
    public Outputs.PrfRegistrationOutput? Prf { get; init; }
    /// <summary>Gets the credential properties output.</summary>
    public Outputs.CredPropsOutput? CredProps { get; init; }
    /// <summary>Gets the generated signing key details.</summary>
    public PreviewSign.PreviewSignRegistrationOutput? PreviewSign { get; init; }
}

/// <summary>
/// Extension outputs from WebAuthn authentication (GetAssertion).
/// </summary>
public sealed record class AuthenticationExtensionOutputs
{
    /// <summary>Gets the retrieved credential blob data.</summary>
    public CredBlobAssertionOutput? CredBlob { get; init; }
    /// <summary>Gets the large blob operation result.</summary>
    public Outputs.LargeBlobAuthenticationOutput? LargeBlob { get; init; }
    /// <summary>Gets the PRF evaluation results.</summary>
    public Outputs.PrfAuthenticationOutput? Prf { get; init; }
    /// <summary>Gets the signature over the to-be-signed data.</summary>
    public PreviewSign.PreviewSignAuthenticationOutput? PreviewSign { get; init; }
}
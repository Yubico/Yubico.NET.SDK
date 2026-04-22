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

using Yubico.YubiKit.WebAuthn.Extensions.Outputs;

namespace Yubico.YubiKit.WebAuthn.Extensions;

/// <summary>
/// Extension outputs from WebAuthn registration (MakeCredential).
/// </summary>
/// <param name="CredProtect">Credential protection policy output.</param>
/// <param name="CredBlob">Credential blob storage result.</param>
/// <param name="MinPinLength">Minimum PIN length output.</param>
/// <param name="LargeBlob">Large blob support result.</param>
/// <param name="Prf">PRF support result.</param>
/// <param name="CredProps">Credential properties output.</param>
public sealed record class RegistrationExtensionOutputs(
    CredProtectOutput? CredProtect = null,
    CredBlobOutput? CredBlob = null,
    MinPinLengthOutput? MinPinLength = null,
    LargeBlobRegistrationOutput? LargeBlob = null,
    PrfRegistrationOutput? Prf = null,
    CredPropsOutput? CredProps = null);

/// <summary>
/// Extension outputs from WebAuthn authentication (GetAssertion).
/// </summary>
/// <param name="CredBlob">Retrieved credential blob data.</param>
/// <param name="LargeBlob">Large blob operation result.</param>
/// <param name="Prf">PRF evaluation results.</param>
public sealed record class AuthenticationExtensionOutputs(
    CredBlobAssertionOutput? CredBlob = null,
    LargeBlobAuthenticationOutput? LargeBlob = null,
    PrfAuthenticationOutput? Prf = null);

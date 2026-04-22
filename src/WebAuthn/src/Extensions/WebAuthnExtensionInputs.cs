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

using Yubico.YubiKit.WebAuthn.Extensions.Inputs;

namespace Yubico.YubiKit.WebAuthn.Extensions;

/// <summary>
/// Extension inputs for WebAuthn registration (MakeCredential).
/// </summary>
/// <param name="CredProtect">Credential protection policy.</param>
/// <param name="CredBlob">Credential blob storage.</param>
/// <param name="MinPinLength">Minimum PIN length request.</param>
/// <param name="LargeBlob">Large blob support request.</param>
/// <param name="Prf">Pseudo-random function extension.</param>
/// <param name="CredProps">Credential properties request.</param>
public sealed record class RegistrationExtensionInputs(
    CredProtectInput? CredProtect = null,
    CredBlobInput? CredBlob = null,
    MinPinLengthInput? MinPinLength = null,
    LargeBlobInput? LargeBlob = null,
    PrfInput? Prf = null,
    CredPropsInput? CredProps = null);

/// <summary>
/// Extension inputs for WebAuthn authentication (GetAssertion).
/// </summary>
/// <param name="LargeBlob">Large blob operations (read/write).</param>
/// <param name="Prf">Pseudo-random function evaluation.</param>
public sealed record class AuthenticationExtensionInputs(
    LargeBlobInput? LargeBlob = null,
    PrfInput? Prf = null);

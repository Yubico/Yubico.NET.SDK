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

using Yubico.YubiKit.Ctap2.Extensions;

namespace Yubico.YubiKit.WebAuthn.Extensions;

/// <summary>
/// Extension inputs for WebAuthn registration (MakeCredential).
/// </summary>
/// <remarks>
/// Inputs request extensions; they do not guarantee results. Unsupported requests, including
/// previewSign, are generally omitted. LargeBlob has its own support checks, and credProps is
/// client-side. Inspect the returned extension results rather than assuming a request was honored.
/// </remarks>
/// <param name="CredProtect">Credential protection policy.</param>
/// <param name="CredBlob">Credential blob storage.</param>
/// <param name="MinPinLength">
/// Requests the current minimum PIN length in Unicode code points, not a policy change. The
/// authenticator returns it only for an RP ID authorized in its minimum-PIN-length RP list.
/// As a current input-handling limitation, any nonnull object requests the extension even if its
/// <c>Requested</c> property is false. Use null to omit it.
/// </param>
/// <param name="LargeBlob">Large blob support request.</param>
/// <param name="Prf">Pseudo-random function extension.</param>
/// <param name="CredProps">Credential properties request.</param>
/// <param name="PreviewSign">Signing key generation request (Yubico WebAuthn sign-extension draft version 4).</param>
public sealed record class RegistrationExtensionInputs(
    Inputs.CredProtectInput? CredProtect = null,
    CredBlobInput? CredBlob = null,
    MinPinLengthInput? MinPinLength = null,
    Inputs.LargeBlobInput? LargeBlob = null,
    Inputs.PrfInput? Prf = null,
    Inputs.CredPropsInput? CredProps = null,
    PreviewSign.PreviewSignRegistrationInput? PreviewSign = null);

/// <summary>
/// Extension inputs for WebAuthn authentication (GetAssertion).
/// </summary>
/// <remarks>
/// Inputs request extensions; unsupported requests, including previewSign, can be omitted. Results
/// can also be absent when evaluation was unavailable or output parsing was suppressed. Not every
/// extension error is ignored: storage and explicit previewSign errors can propagate. Inspect each
/// result and follow the individual input's allow-list, user-verification, and buffer-lifetime rules.
/// </remarks>
/// <param name="LargeBlob">Large blob operations (read/write).</param>
/// <param name="Prf">Pseudo-random function evaluation.</param>
/// <param name="PreviewSign">Arbitrary data signing request (Yubico WebAuthn sign-extension draft version 4).</param>
/// <param name="GetCredBlob">
/// The CTAP 2.1-2.3 §12.2.1 <c>getCredBlob</c> client input: when true the client sends
/// <c>"credBlob": true</c> and reports the stored blob in
/// <see cref="AuthenticationExtensionOutputs.CredBlob"/>.
/// </param>
public sealed record class AuthenticationExtensionInputs(
    Inputs.LargeBlobInput? LargeBlob = null,
    Inputs.PrfInput? Prf = null,
    PreviewSign.PreviewSignAuthenticationInput? PreviewSign = null,
    bool GetCredBlob = false);
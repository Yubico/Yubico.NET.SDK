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
/// Extension outputs from WebAuthn registration (MakeCredential).
/// </summary>
/// <remarks>
/// A null input container produces null client extension results. A supplied container produces this
/// record even if every property is null. An individual null result can mean unrequested,
/// unsupported, absent, or omitted after a parsing failure. Explicit false results, such as
/// <c>Prf.Enabled</c> or <c>LargeBlob.Supported</c>, are distinct from an absent result. Not every
/// extension error is suppressed; explicit previewSign errors can fail response construction.
/// </remarks>
public sealed record class RegistrationExtensionOutputs
{
    /// <summary>Gets the credential protection policy output.</summary>
    public Outputs.CredProtectOutput? CredProtect { get; init; }
    /// <summary>Gets the credential blob storage result.</summary>
    public CredBlobMakeCredentialOutput? CredBlob { get; init; }
    /// <summary>Gets the reported minimum PIN length in Unicode code points, when authorized for this RP ID.</summary>
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
/// <remarks>
/// <para>
/// A null input container produces null client extension results. A supplied container produces this
/// record even when all properties are null. Individual null results can mean unrequested,
/// unsupported, absent, unavailable evaluation, or omitted output after a parsing failure. They are
/// not a universal indication of success or failure; some output-preparation errors propagate after
/// the assertion succeeds. A false write result is distinct from an absent largeBlob result.
/// </para>
/// <para>
/// PRF results are derived secrets; credential blobs and decrypted large blobs can contain sensitive
/// application data. SDK-produced outputs have no disposal or zeroing guarantee, and client disposal
/// does not clear them. Clearing a caller-created copy does not clear the SDK-created storage.
/// Directly constructed results may wrap caller-owned memory; the caller retains its lifetime and
/// cleanup responsibilities. Do not log sensitive result contents.
/// </para>
/// </remarks>
public sealed record class AuthenticationExtensionOutputs
{
    /// <summary>Gets retrieved credential blob data; an empty blob is distinct from an absent result.</summary>
    public CredBlobAssertionOutput? CredBlob { get; init; }
    /// <summary>Gets the large blob operation result.</summary>
    public Outputs.LargeBlobAuthenticationOutput? LargeBlob { get; init; }
    /// <summary>Gets the PRF evaluation results.</summary>
    public Outputs.PrfAuthenticationOutput? Prf { get; init; }
    /// <summary>Gets the signature over the to-be-signed data.</summary>
    public PreviewSign.PreviewSignAuthenticationOutput? PreviewSign { get; init; }
}
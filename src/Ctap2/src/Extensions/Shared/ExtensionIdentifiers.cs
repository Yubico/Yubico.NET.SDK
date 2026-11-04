// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Yubico.YubiKit.Ctap2.Extensions;

/// <summary>
/// Identifiers for modeled CTAP extensions and the experimental Yubico Labs previewSign extension.
/// </summary>
/// <remarks>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-defined-extensions
/// That revision defines the base extensions, not hmac-secret-mc or previewSign. hmac-secret-mc is
/// defined in CTAP 2.2-2.3 §12.8. previewSign uses the separate Yubico Labs v4 Editor's Draft.
/// </remarks>
public static class ExtensionIdentifiers
{
    /// <summary>
    /// The hmac-secret extension identifier.
    /// </summary>
    /// <remarks>
    /// Allows deriving a secret value from a credential during assertions.
    /// Useful for disk encryption, password managers, etc.
    /// </remarks>
    public const string HmacSecret = "hmac-secret";

    /// <summary>
    /// The hmac-secret-mc extension identifier (during makeCredential).
    /// </summary>
    /// <remarks>
    /// Evaluates hmac-secret during credential creation (CTAP 2.2-2.3 §12.8), returning encrypted
    /// output. Requires separate advertisement and hmac-secret:true in the same request.
    /// </remarks>
    public const string HmacSecretMakeCredential = "hmac-secret-mc";

    /// <summary>
    /// The credProtect extension identifier.
    /// </summary>
    /// <remarks>
    /// Specifies the credential protection policy, controlling when
    /// user verification is required.
    /// </remarks>
    public const string CredProtect = "credProtect";

    /// <summary>
    /// The credBlob extension identifier.
    /// </summary>
    /// <remarks>
    /// Allows storing a small blob of data (up to maxCredBlobLength bytes)
    /// along with the credential. Retrieved during assertions.
    /// </remarks>
    public const string CredBlob = "credBlob";

    /// <summary>
    /// The largeBlob extension identifier.
    /// </summary>
    /// <remarks>
    /// CTAP 2.3 §12.4's direct storage alternative: registration requests support; assertion requests
    /// read/write. Outputs are unsigned, with compressed read data plus originalSize. This route is
    /// not implemented by ExtensionBuilder/LargeBlobStorage and is distinct from WebAuthn output.
    /// </remarks>
    public const string LargeBlob = "largeBlob";

    /// <summary>
    /// The largeBlobKey extension identifier.
    /// </summary>
    /// <remarks>
    /// Requests the credential's secret storage key for use with authenticatorLargeBlobs (§6.10).
    /// Returned at makeCredential key 5 or getAssertion key 7, not in authData extensions (§12.3).
    /// Check largeBlobKey advertisement and largeBlobs:true; it is mutually exclusive with direct largeBlob.
    /// </remarks>
    public const string LargeBlobKey = "largeBlobKey";

    /// <summary>
    /// The minPinLength extension identifier.
    /// </summary>
    /// <remarks>
    /// Returns the minimum PIN length required by the authenticator.
    /// Requested during makeCredential for an authorized RP; reports Unicode code points without
    /// changing policy or attesting all PIN complexity rules.
    /// </remarks>
    public const string MinPinLength = "minPinLength";


    /// <summary>
    /// The experimental previewSign identifier (Yubico Labs version 4 Editor's Draft, 26 August 2025).
    /// </summary>
    /// <remarks>
    /// Allows a CTAP2 credential to sign arbitrary data using a separate
    /// signing key bound to the same authenticator. Registration generates
    /// a new signing key pair; authentication signs data without clientDataJSON
    /// or authenticator data wrapping.
    /// See https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension.
    /// This is not CTAP version 4. Preview algorithms and identifiers may change; do not use in production.
    /// </remarks>
    public const string PreviewSign = "previewSign";
}
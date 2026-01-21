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

namespace Yubico.YubiKit.Fido2.Extensions;

/// <summary>
/// CTAP2 extension identifiers as defined in the FIDO2 specification.
/// </summary>
/// <remarks>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#sctn-defined-extensions
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
    /// Variant of hmac-secret that allows retrieving the secret during
    /// credential creation, not just assertion.
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
    /// Allows storing large blobs of data using a separate command.
    /// The extension during makeCredential/getAssertion only indicates
    /// support; actual data storage uses authenticatorLargeBlobs command.
    /// </remarks>
    public const string LargeBlob = "largeBlob";
    
    /// <summary>
    /// The largeBlobKey extension identifier.
    /// </summary>
    /// <remarks>
    /// Retrieved during assertion when largeBlob is supported. Contains
    /// the key used to encrypt large blob data for this credential.
    /// </remarks>
    public const string LargeBlobKey = "largeBlobKey";
    
    /// <summary>
    /// The minPinLength extension identifier.
    /// </summary>
    /// <remarks>
    /// Returns the minimum PIN length required by the authenticator.
    /// Can be requested during makeCredential if the RP needs to know
    /// the PIN complexity requirements.
    /// </remarks>
    public const string MinPinLength = "minPinLength";
    
    /// <summary>
    /// The prf extension identifier (WebAuthn).
    /// </summary>
    /// <remarks>
    /// Pseudo-random function extension. WebAuthn-level extension that
    /// maps to hmac-secret at the CTAP level. Allows deriving secrets
    /// from arbitrary inputs.
    /// </remarks>
    public const string Prf = "prf";
}

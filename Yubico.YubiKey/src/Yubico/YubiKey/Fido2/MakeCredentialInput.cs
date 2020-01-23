// Copyright 2021 Yubico AB
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

using System;
using System.Collections.Generic;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Input data for a FIDO2 MakeCredential operation.
    /// </summary>
    /// <remarks>
    /// <p>
    /// The details of each option and its individual characteristics are available in the 
    /// CTAP2 specification.
    /// </p>
    /// <p>
    /// Note that it's possible to construct invalid instances of this class; to validate
    /// a given instance of the class, call <c>Validate()</c>.
    /// </p>
    /// </remarks>
    [CborSerializable]
    internal class MakeCredentialInput : IValidatable
    {
        /// <summary>
        /// SHA256 hash of the client data - see the WebAuthn specification for more details.
        /// </summary>
        [CborLabelId(0x01)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] ClientDataHash { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The 'Relying Party' data, containing the name, ID, etc; 'rp' in CTAP2.
        /// </summary>
        [CborLabelId(0x02)]
        public RelyingParty RelyingParty { get; set; } = new RelyingParty();

        /// <summary>
        /// The user account data that the credential is being created for.
        /// </summary>
        [CborLabelId(0x03)]
        public PublicKeyCredentialUserEntity User { get; set; } = new PublicKeyCredentialUserEntity();

        /// <summary>
        /// A non-empty sequence of acceptable public key parameters to use, in order of preference.
        /// </summary>
        [CborLabelId(0x04)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public PublicKeyCredentialParameter[] PublicKeyCredentialParameters { get; set; } = Array.Empty<PublicKeyCredentialParameter>();

        /// <summary>
        /// An optional sequence of credentials that, if present on the authenticator, should cause the request to fail.
        /// </summary>
        [CborLabelId(0x05)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public PublicKeyCredentialDescriptor[]? ExcludeList { get; set; }

        /// <summary>
        /// An optional dictionary of extensions.
        /// </summary>
        [CborLabelId(0x06)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Dictionaries for CTAP properties")]
        public Dictionary<string, object>? Extensions { get; set; }

        /// <summary>
        /// An optional dictionary of options.
        /// </summary>
        [CborLabelId(0x07)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Dictionaries for CTAP properties")]
        public Dictionary<string, bool>? Options { get; set; }

        /// <summary>
        /// An optional proof that the platform has obtained the user PIN; 'pinAuth' in CTAP2
        /// </summary>
        /// <remarks>
        /// Specifically, this is the first 16 bytes of the HMAC-SHA256 of ClientDataHash using pinToken as the key.
        /// </remarks>
        [CborLabelId(0x08)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[]? PinUserVerificationAuthenticatorParameter { get; set; }

        /// <summary>
        /// An optional PIN protocol version number.
        /// </summary>
        [CborLabelId(0x09)]
        [CborSerializeAsUnsigned]
        public int? PinUserVerificationAuthenticatorProtocol { get; set; }

        public const int ExpectedClientDataHashLength = 32;
        public const int ExpectedPinUserVerificationAuthenticatorProtocolLength = 32;

        /// <inheritdoc/>
        public void Validate()
        {
            if (ClientDataHash is null || ClientDataHash.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.MissingCtap2Data) { PropertyName = nameof(ClientDataHash) };
            }

            if (ClientDataHash.Length != ExpectedClientDataHashLength)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidCtap2Data) { PropertyName = nameof(ClientDataHash) };
            }

            RelyingParty.Validate();

            User.Validate();

            if (PublicKeyCredentialParameters is null || PublicKeyCredentialParameters.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.MissingCtap2Data) { PropertyName = nameof(PublicKeyCredentialParameters) };
            }

            foreach (PublicKeyCredentialParameter pcp in PublicKeyCredentialParameters)
            {
                pcp.Validate();
            }

            if (!(ExcludeList is null))
            {
                foreach (PublicKeyCredentialDescriptor pkcd in ExcludeList)
                {
                    pkcd.Validate();
                }
            }

            if (!(PinUserVerificationAuthenticatorParameter is null) && PinUserVerificationAuthenticatorParameter.Length != ExpectedPinUserVerificationAuthenticatorProtocolLength )
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidCtap2Data) { PropertyName = nameof(PinUserVerificationAuthenticatorParameter) };
            }
        }
    }
}

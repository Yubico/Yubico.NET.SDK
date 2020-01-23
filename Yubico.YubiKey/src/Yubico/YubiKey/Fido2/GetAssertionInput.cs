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
using System.Runtime.CompilerServices;
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Input data for a FIDO2 GetAssertion operation.
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
    internal class GetAssertionInput : IValidatable
    {
        /// <summary>
        /// Relying party identifier, per the WebAuthn specification.
        /// </summary>
        [CborLabelId(0x01)]
        public string RelyingPartyId { get; set; } = string.Empty;

        /// <summary>
        /// SHA256 hash of the client data - see the WebAuthn specification for more details.
        /// </summary>
        [CborLabelId(0x02)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] ClientDataHash { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// An optional sequence of credentials allowed to be used to generate the assertion.
        /// </summary>
        /// <remarks>
        /// Must have length at least 1 or not be present.
        /// </remarks>
        [CborLabelId(0x03)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public PublicKeyCredentialDescriptor[]? AllowList { get; set; } = Array.Empty<PublicKeyCredentialDescriptor>();

        /// <summary>
        /// An optional dictionary of extensions.
        /// </summary>
        [CborLabelId(0x04)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Dictionaries for CTAP properties")]
        public Dictionary<string, object>? Extensions { get; set; }

        /// <summary>
        /// An optional dictionary of options.
        /// </summary>
        [CborLabelId(0x05)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Dictionaries for CTAP properties")]
        public Dictionary<string, bool>? Options { get; set; }

        /// <summary>
        /// An optional proof that the platform has obtained the user PIN; 'pinAuth' in CTAP2.
        /// </summary>
        /// <remarks>
        /// Specifically, this is the first 16 bytes of the HMAC-SHA256 of ClientDataHash using pinToken as the key.
        /// </remarks>
        [CborLabelId(0x06)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[]? PinUserVerificationAuthenticatorParameter { get; set; }

        /// <summary>
        /// An optional PIN protocol version number.
        /// </summary>
        [CborLabelId(0x07)]
        [CborSerializeAsUnsigned]
        public int? PinUserVerificationAuthenticatorProtocol { get; set; }

        public const int ExpectedClientDataHashLength = 32;
        public const int ExpectedPinUserVerificationAuthenticatorParameterLength = 32;

        /// <inheritdoc/>
        public void Validate()
        {
            if (ClientDataHash is null || ClientDataHash.Length == 0)
            {
                throw new Ctap2DataException(ExceptionMessages.InvalidCtap2Data)
                { 
                    PropertyName = nameof(ClientDataHash) 
                };
            }

            if (ClientDataHash.Length != ExpectedClientDataHashLength)
            {
                throw new Ctap2DataException() { PropertyName = nameof(ClientDataHash) };
            }

            if (!(AllowList is null))
            {
                if (AllowList.Length == 0)
                {
                    // Empty allowList is invalid
                    throw new Ctap2DataException() { PropertyName = nameof(AllowList) };
                }

                foreach (PublicKeyCredentialDescriptor pkcd in AllowList!)
                {
                    pkcd.Validate();
                }
            }

            if (!(PinUserVerificationAuthenticatorParameter is null) && PinUserVerificationAuthenticatorParameter.Length != ExpectedPinUserVerificationAuthenticatorParameterLength)
            {
                throw new Ctap2DataException() { PropertyName = nameof(PinUserVerificationAuthenticatorParameter) };
            }
        }
    }
}

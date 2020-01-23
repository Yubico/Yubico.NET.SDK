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
using Yubico.YubiKey.Fido2.Serialization;

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Data returned by a FIDO2 GetAssertion operation.
    /// </summary>
    [CborSerializable]
    internal class GetAssertionOutput
    {
        /// <summary>
        /// An optional value credential from which an assertion was obtained.
        /// </summary>
        [CborLabelId(0x01)]
        public PublicKeyCredentialDescriptor? Credential { get; set; }

        /// <summary>
        /// The raw 'authenticator data', per the WebAuthn specification.
        /// </summary>
        [CborLabelId(0x02)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] AuthenticatorData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The assertion signature, per the WebAuthn specification.
        /// </summary>
        /// <remarks>
        /// This is a signature over (authData || clientDataHash) using the credential private key.
        /// </remarks>
        [CborLabelId(0x03)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The user for which the credential was created; not always available.
        /// </summary>
        /// <remarks>
        /// This data is unavailable for U2F devices. Contains only 'id' when 
        /// there is only one credential on the authenticator for the RP; otherwise, 
        /// contains more data on name/displayName/icon.
        /// </remarks>
        [CborLabelId(0x04)]
        public AuthenticatorResponseUserEntity? User { get; set; }

        /// <summary>
        /// The number of credentials for the RP; only required when it is more than one.
        /// </summary>
        [CborLabelId(0x05)]
        public int? NumberOfCredentials { get; set; }

        /// <summary>
        /// CTAP2.1 property indicating that the credential was selected through direct interaction
        /// with the authenticator.
        /// </summary>
        [CborLabelId(0x05)]
        public bool? UserSelected { get; set; }
    }
}

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

namespace Yubico.YubiKey.Fido2
{
    /// <summary>
    /// Represents data returned by a FIDO2 MakeCredential operation.
    /// </summary>
    /// <remarks>
    /// This is a minimum interface that all possible responses must satisfy.
    /// </remarks>
    internal interface IMakeCredentialOutput
    {
        /// <summary>
        /// The 'attestation statement format identifier' string, per the WebAuthn specification.
        /// </summary>
        public string AttestationFormatIdentifier { get; set; }

        /// <summary>
        /// The raw 'authenticator data', per the WebAuthn specification.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Arrays for CTAP properties")]
        public byte[] AuthenticatorData { get; set; }
    }
}

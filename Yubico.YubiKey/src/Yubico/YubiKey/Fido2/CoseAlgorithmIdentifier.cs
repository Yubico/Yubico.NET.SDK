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
    /// Represents a COSE algorithm identifier.
    /// </summary>
    internal enum CoseAlgorithmIdentifier
    {
        /// <summary>
        /// ECDSA w/ SHA-256	
        /// </summary>
        ES256 = -7,
        /// <summary>
        /// EdDSA
        /// </summary>
        EdDSA = -8,
        /// <summary>
        /// ECDSA using secp256k1 curve and SHA-256	
        /// </summary>
        ES256K = -47,
        /// <summary>
        /// ECDSA w/ SHA-384
        /// </summary>
        ES384 = -35,
        /// <summary>
        /// RSASSA-PKCS1-v1_5 w/ SHA-256	
        /// </summary>
        RS256 = -257,
    }
}

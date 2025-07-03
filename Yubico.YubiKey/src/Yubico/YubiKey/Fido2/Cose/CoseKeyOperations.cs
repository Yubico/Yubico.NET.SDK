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

namespace Yubico.YubiKey.Fido2.Cose
{
    /// <summary>
    /// An enumeration of the operations that one can potentially perform with a COSE key.
    /// </summary>
    public enum CoseKeyOperations
    {
        /// <summary>
        /// The operation could not be determined.
        /// </summary>
        None = 0,

        /// <summary>
        /// The key is used to create signatures. Requires private key fields.
        /// </summary>
        Sign = 1,

        /// <summary>
        /// The key is used for verification of signatures.
        /// </summary>
        Verify = 2,

        /// <summary>
        /// The key is used for key transport encryption.
        /// </summary>
        Encrypt = 3,

        /// <summary>
        /// The key is used for key transport decryption. Requires private key fields.
        /// </summary>
        Decrypt = 4,

        /// <summary>
        /// The key is used for key wrap encryption.
        /// </summary>
        WrapKey = 5,

        /// <summary>
        /// The key is used for key wrap decryption.
        /// </summary>
        UnwrapKey = 6,

        /// <summary>
        /// The key is used for deriving keys. Requires private key fields.
        /// </summary>
        DeriveKey = 7,

        /// <summary>
        /// The key is used for deriving bits not to be used as a key. Requires private key fields.
        /// </summary>
        DeriveBits = 8,

        /// <summary>
        /// The key is used for creating MACs.
        /// </summary>
        MacCreate = 9,

        /// <summary>
        /// The key is used for validating MACs.
        /// </summary>
        MacVerify = 10,
    }
}

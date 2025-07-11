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

namespace Yubico.YubiKey.Piv
{
    /// <summary>
    /// The cryptographic algorithms supported by the PIV Application on the
    /// YubiKey.
    /// </summary>
    public enum PivAlgorithm
    {
        /// <summary>
        /// No algorithm (generally indicates a slot is empty).
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the algorithm is Triple-DES (Slot 9B). The key size is
        /// 192 bits (24 bytes).
        /// </summary>
        TripleDes = 0x03,

        /// <summary>
        /// Indicates that the algorithm is AES-128 (Slot 9B). The key size is
        /// 128 bits (16 bytes).
        /// </summary>
        Aes128 = 0x08,

        /// <summary>
        /// Indicates that the algorithm is AES-192 (Slot 9B). The key size is
        /// 192 bits (24 bytes).
        /// </summary>
        Aes192 = 0x0A,

        /// <summary>
        /// Indicates that the algorithm is AES-256 (Slot 9B). The key size is
        /// 256 bits (32 bytes).
        /// </summary>
        Aes256 = 0x0C,

        /// <summary>
        /// Indicates that the algorithm is RSA and the key size (modulus size) is
        /// 1024 bits.
        /// </summary>
        Rsa1024 = 0x06,

        /// <summary>
        /// Indicates that the algorithm is RSA and the key size (modulus size) is
        /// 2048 bits.
        /// </summary>
        Rsa2048 = 0x07,

        /// <summary>
        /// Indicates that the algorithm is RSA and the key size (modulus size) is
        /// 3072 bits.
        /// </summary>
        Rsa3072 = 0x05,

        /// <summary>
        /// Indicates that the algorithm is RSA and the key size (modulus size) is
        /// 4096 bits.
        /// </summary>
        Rsa4096 = 0x16,

        /// <summary>
        /// Indicates that the algorithm is ECC and the parameters are P-256,
        /// specified in FIPS 186-4 (moving to NIST SP 800-186).
        /// </summary>
        EccP256 = 0x11,

        /// <summary>
        /// Indicates that the algorithm is ECC and the parameters are P-384,
        /// specified in FIPS 186-4 (moving to NIST SP 800-186).
        /// </summary>
        EccP384 = 0x14,

        /// <summary>
        /// Indicates that the algorithm is ECC and the parameters are P-521,
        /// </summary>
        EccP521 = 0x15,

        /// <summary>
        /// Indicates that the slot contains a PIN or PUK (slots 80 and 81).
        /// While not a cryptographic algorithm, it is used in the PIV Metadata.
        /// </summary>
        Pin = 0xFF,

        /// <summary>
        /// Indicates that the algorithm is ECC and the parameters are Ed25519
        /// </summary>
        EccEd25519 = 0xE0,
        
        /// <summary>
        /// Indicates that the algorithm is ECC and the parameters are X25519
        /// </summary>
        EccX25519 = 0xE1
    }
}

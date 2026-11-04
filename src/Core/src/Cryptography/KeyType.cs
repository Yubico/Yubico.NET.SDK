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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Identifies a concrete supported cryptographic key variant, not an operation algorithm or wire identifier.
/// </summary>
/// <remarks>Numeric values are explicit. Enum names and ToString output are not a persistence contract.</remarks>
public enum KeyType
{
    /// <summary>No concrete key; not a catalog entry.</summary>
    None = 0,
    /// <summary>Ordinary EC representation on NIST P-256.</summary>
    P256 = 1,
    /// <summary>Ordinary EC representation on NIST P-384.</summary>
    P384 = 2,
    /// <summary>Ordinary EC representation on NIST P-521.</summary>
    P521 = 3,
    /// <summary>X25519 construction on the Montgomery Curve25519 curve.</summary>
    X25519 = 4,
    /// <summary>Ed25519 construction on the Edwards25519 curve.</summary>
    Ed25519 = 5,
    /// <summary>RSA with a 1024-bit modulus; historical metadata, not a recommendation.</summary>
    Rsa1024 = 6,
    /// <summary>RSA with a 2048-bit modulus.</summary>
    Rsa2048 = 7,
    /// <summary>RSA with a 3072-bit modulus.</summary>
    Rsa3072 = 8,
    /// <summary>RSA with a 4096-bit modulus.</summary>
    Rsa4096 = 9,
    /// <summary>Three-key Triple DES, stored in 24 bytes including parity.</summary>
    TripleDes = 10,
    /// <summary>AES with a 128-bit key, without an implied block mode.</summary>
    Aes128 = 11,
    /// <summary>AES with a 192-bit key, without an implied block mode.</summary>
    Aes192 = 12,
    /// <summary>AES with a 256-bit key, without an implied block mode.</summary>
    Aes256 = 13,
    /// <summary>Ordinary EC representation on secp256k1, distinct from P-256.</summary>
    Secp256k1 = 14,
    /// <summary>Ordinary EC representation on brainpoolP256r1.</summary>
    BrainpoolP256r1 = 15,
    /// <summary>Ordinary EC representation on brainpoolP384r1.</summary>
    BrainpoolP384r1 = 16,
    /// <summary>Ordinary EC representation on brainpoolP512r1.</summary>
    BrainpoolP512r1 = 17
}
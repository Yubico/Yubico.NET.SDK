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
    None = 0,
    P256 = 1,
    P384 = 2,
    P521 = 3,
    X25519 = 4,
    Ed25519 = 5,
    Rsa1024 = 6,
    Rsa2048 = 7,
    Rsa3072 = 8,
    Rsa4096 = 9,
    TripleDes = 10,
    Aes128 = 11,
    Aes192 = 12,
    Aes256 = 13
}
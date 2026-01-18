// Copyright 2024 Yubico AB
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

namespace Yubico.YubiKit.Piv;

/// <summary>
/// PIV key algorithms for private key generation and import.
/// </summary>
public enum PivAlgorithm : byte
{
    /// <summary>RSA 1024-bit key.</summary>
    Rsa1024 = 0x06,
    
    /// <summary>RSA 2048-bit key.</summary>
    Rsa2048 = 0x07,
    
    /// <summary>RSA 3072-bit key (requires YubiKey 5.7+).</summary>
    Rsa3072 = 0x05,
    
    /// <summary>RSA 4096-bit key (requires YubiKey 5.7+).</summary>
    Rsa4096 = 0x16,
    
    /// <summary>Elliptic Curve P-256 key.</summary>
    EccP256 = 0x11,
    
    /// <summary>Elliptic Curve P-384 key (requires YubiKey 4.0+).</summary>
    EccP384 = 0x14,
    
    /// <summary>Ed25519 signature key (requires YubiKey 5.7+).</summary>
    Ed25519 = 0xE0,
    
    /// <summary>X25519 key exchange key (requires YubiKey 5.7+).</summary>
    X25519 = 0xE1
}
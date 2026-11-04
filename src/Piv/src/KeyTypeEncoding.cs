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

using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Piv;

/// <summary>Explicit PIV wire identifiers; Core KeyType values are not protocol bytes.</summary>
internal static class KeyTypeEncoding
{
    internal static byte Encode(KeyType keyType) => keyType switch
    {
        KeyType.RSA1024 => 0x06,
        KeyType.RSA2048 => 0x07,
        KeyType.RSA3072 => 0x05,
        KeyType.RSA4096 => 0x16,
        KeyType.ECP256 => 0x11,
        KeyType.ECP384 => 0x14,
        KeyType.Ed25519 => 0xE0,
        KeyType.X25519 => 0xE1,
        _ => throw new NotSupportedException($"Key type {keyType} is not supported by PIV.")
    };

    internal static KeyType? Decode(byte algorithmId) => algorithmId switch
    {
        0x06 => KeyType.RSA1024,
        0x07 => KeyType.RSA2048,
        0x05 => KeyType.RSA3072,
        0x16 => KeyType.RSA4096,
        0x11 => KeyType.ECP256,
        0x14 => KeyType.ECP384,
        0xE0 => KeyType.Ed25519,
        0xE1 => KeyType.X25519,
        _ => null
    };
}
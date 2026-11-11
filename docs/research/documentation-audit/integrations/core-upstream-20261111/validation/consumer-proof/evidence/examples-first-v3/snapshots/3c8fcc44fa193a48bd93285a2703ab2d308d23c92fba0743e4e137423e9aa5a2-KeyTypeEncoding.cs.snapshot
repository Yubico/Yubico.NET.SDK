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

using System.Collections.Frozen;
using Yubico.YubiKit.Core.Cryptography;

namespace Yubico.YubiKit.Piv;

/// <summary>Explicit PIV wire identifiers; Core KeyType values are not protocol bytes.</summary>
internal static class KeyTypeEncoding
{
    // The single protocol table defines PIV identifiers, not numeric Core enum values.
    private static readonly FrozenDictionary<KeyType, byte> Identifiers = new Dictionary<KeyType, byte>
    {
        [KeyType.TripleDes] = 0x03,
        [KeyType.Aes128] = 0x08,
        [KeyType.Aes192] = 0x0A,
        [KeyType.Aes256] = 0x0C,
        [KeyType.Rsa1024] = 0x06,
        [KeyType.Rsa2048] = 0x07,
        [KeyType.Rsa3072] = 0x05,
        [KeyType.Rsa4096] = 0x16,
        [KeyType.P256] = 0x11,
        [KeyType.P384] = 0x14,
        [KeyType.Ed25519] = 0xE0,
        [KeyType.X25519] = 0xE1
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<byte, KeyType> Types =
        Identifiers.ToFrozenDictionary(pair => pair.Value, pair => pair.Key);

    internal static byte Encode(KeyType keyType) => Identifiers.TryGetValue(keyType, out byte id)
        ? id
        : throw new NotSupportedException($"Key type {keyType} is not supported by PIV.");

    internal static KeyType? Decode(byte algorithmId) => Types.TryGetValue(algorithmId, out var keyType) ? keyType : null;

    internal static byte EncodeAsymmetric(KeyType keyType)
    {
        if (!keyType.IsAsymmetric())
            throw new NotSupportedException($"Key type {keyType} is not an asymmetric PIV key type.");
        return Encode(keyType);
    }

    internal static byte EncodeManagement(KeyType keyType)
    {
        if (!keyType.IsSymmetric())
            throw new NotSupportedException($"Key type {keyType} is not a PIV management key type.");
        return Encode(keyType);
    }
}
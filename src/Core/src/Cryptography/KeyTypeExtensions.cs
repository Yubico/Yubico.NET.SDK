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

using Yubico.YubiKit.Core.Cryptography.Cose;

namespace Yubico.YubiKit.Core.Cryptography;

public static class KeyTypeExtensions
{
    public static KeyDefinition GetKeyDefinition(this KeyType keyType) => KeyDefinitions.GetByKeyType(keyType);
    public static string GetAlgorithmOid(this KeyType keyType) => keyType.GetKeyDefinition().AlgorithmOid;
    public static string? GetCurveOid(this KeyType keyType) => keyType.GetKeyDefinition().CurveOid;
    public static int GetKeySizeBits(this KeyType keyType) => keyType.GetKeyDefinition().LengthInBits;
    public static int GetKeySizeBytes(this KeyType keyType) => keyType.GetKeyDefinition().LengthInBytes;
    /// <summary>Whether this known key has registered COSE metadata; not a codec, operation, or hardware capability check.</summary>
    public static bool HasCoseMapping(this KeyType keyType) => keyType.GetCoseKeyType() is not null;
    /// <summary>Gets the COSE family, or null for a known key without a mapping. Invalid key types throw.</summary>
    public static CoseKeyType? GetCoseKeyType(this KeyType keyType) => keyType.GetKeyDefinition().CoseKeyDefinition?.Type;
    public static bool IsECDsa(this KeyType keyType) => keyType is KeyType.P256 or KeyType.P384 or KeyType.P521;
    public static bool IsCurve25519(this KeyType keyType) => keyType is KeyType.X25519 or KeyType.Ed25519;
    public static bool IsAsymmetric(this KeyType keyType) =>
        IsEllipticCurve(keyType) || IsRsa(keyType);
    public static bool IsSymmetric(this KeyType keyType) =>
        keyType is KeyType.TripleDes or KeyType.Aes128 or KeyType.Aes192 or KeyType.Aes256;
    public static bool IsEllipticCurve(this KeyType keyType) =>
        keyType is KeyType.P256 or KeyType.P384 or KeyType.P521 or KeyType.X25519 or KeyType.Ed25519;
    public static bool IsRsa(this KeyType keyType) =>
        keyType is KeyType.Rsa1024 or KeyType.Rsa2048 or KeyType.Rsa3072 or KeyType.Rsa4096;
}
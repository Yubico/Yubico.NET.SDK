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

namespace Yubico.YubiKey.Cryptography;

public static class KeyTypeExtensions
{
    public static KeyDefinition GetKeyDefinition(this KeyType keyType) => KeyDefinitions.GetByKeyType(keyType);
    public static string GetAlgorithmOid(this KeyType keyType) => Oids.GetOidsByKeyType(keyType).AlgorithmOid;    
    public static string? GetCurveOid(this KeyType keyType) => Oids.GetOidsByKeyType(keyType).Curveoid;    
    public static int GetKeySizeBits(this KeyType keyType) => keyType.GetKeyDefinition().LengthInBits;
    public static int GetKeySizeBytes(this KeyType keyType) => keyType.GetKeyDefinition().LengthInBytes;
    public static bool IsCoseKey(this KeyType keyType) => keyType.GetKeyDefinition().CoseKeyDefinition != null;
    public static bool IsECDsa(this KeyType keyType) => keyType is KeyType.ECP256 or KeyType.ECP384 or KeyType.ECP521;
    public static bool IsCurve25519(this KeyType keyType) => keyType is KeyType.X25519 or KeyType.Ed25519;
    public static bool IsAsymmetric(this KeyType keyType) => 
        IsEllipticCurve(keyType) || IsRSA(keyType);
    public static bool IsSymmetric(this KeyType keyType) => 
        keyType is KeyType.TripleDES or KeyType.AES128 or KeyType.AES192 or KeyType.AES256;
    public static bool IsEllipticCurve(this KeyType keyType) => 
        keyType is KeyType.ECP256 or KeyType.ECP384 or KeyType.ECP521 or KeyType.X25519 or KeyType.Ed25519;
    public static bool IsRSA(this KeyType keyType) =>
        keyType is KeyType.RSA1024 or KeyType.RSA2048 or KeyType.RSA3072 or KeyType.RSA4096;
}

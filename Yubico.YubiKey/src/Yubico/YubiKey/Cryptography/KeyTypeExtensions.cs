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
    public static string ToAlgorithmOid(this KeyType keyType) => KeyDefinitions.Oids.GetOidsForKeyType(keyType).AlgorithmOid;    
    public static string? ToCurveOid(this KeyType keyType) => KeyDefinitions.Oids.GetOidsForKeyType(keyType).Curveoid;    
    public static KeyDefinition GetKeyDefinition(this KeyType keyType) => KeyDefinitions.GetByKeyType(keyType);
    public static bool IsEcKey(this KeyType keyType) => keyType.GetKeyDefinition().IsEcKey;
    public static bool IsRsaKey(this KeyType keyType) => keyType.GetKeyDefinition().IsRsaKey;
    public static bool IsSymmetricKey(this KeyType keyType) => keyType.GetKeyDefinition().IsSymmetricKey;
    public static bool IsCoseKey(this KeyType keyType) => keyType.GetKeyDefinition().CoseKeyDefinition != null;
}

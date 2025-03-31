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

using System;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities;

public static class PivTestUtilities
{
    // public static IPrivateKeyParameters GetPrivateKeyParameters(
    //     ReadOnlyMemory<byte> encodedKey,
    //     KeyType keyType)
    // {
    //     return keyType switch
    //     {
    //         KeyType.P256 or KeyType.P384 or KeyType.P521 => ECPrivateKeyParameters.CreateFromPkcs8(encodedKey),
    //         KeyType.X25519 or KeyType.Ed25519 => Curve25519PrivateKeyParameters.CreateFromPkcs8(encodedKey),
    //         KeyType.RSA1024 or KeyType.RSA2048 or KeyType.RSA3072 or KeyType.RSA4096 => RSAPrivateKeyParameters
    //             .CreateFromPkcs8(encodedKey),
    //         _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
    //     };
    // }
    //
    // public static IPublicKeyParameters GetPublicKeyParameters(
    //     ReadOnlyMemory<byte> encodedKey,
    //     KeyType keyType)
    // {
    //     AsnPublicKeyReader.CreateKeyParameters()
    //     return keyType switch
    //     {
    //         KeyType.P256 or KeyType.P384 or KeyType.P521 => ECPublicKeyParameters.CreateFromPkcs8(encodedKey),
    //         KeyType.X25519 or KeyType.Ed25519 => Curve25519PublicKeyParameters.CreateFromPkcs8(encodedKey),
    //         KeyType.RSA1024 or KeyType.RSA2048 or KeyType.RSA3072 or KeyType.RSA4096 => RSAPublicKeyParameters
    //             .CreateFromPkcs8(encodedKey),
    //         _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null)
    //     };
    // }
}

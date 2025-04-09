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

namespace Yubico.YubiKey.Piv.Converters;

/// <summary>
/// Extension methods for encoding cryptographic key parameters into PIV-compatible formats.
/// </summary>
/// <remarks>
/// This class provides extension methods for both public and private key parameters,
/// supporting RSA, EC (Elliptic Curve), and Curve25519 key types. The encoded formats
/// follow the Personal Identity Verification (PIV) specifications.
/// </remarks>
public static class KeyExtensions
{
    /// <summary>
    /// Encodes a public key into the PIV format.
    /// </summary>
    /// <param name="parameters">The public key parameters to encode.</param>
    /// <returns>A BER encoded byte array containing the encoded public key.</returns>
    /// <exception cref="ArgumentException">Thrown when the key type is not supported.</exception>
    public static Memory<byte> EncodeAsPiv(this IPublicKey parameters)
    {
        return parameters switch
        {
            ECPublicKey p => PivKeyEncoder.EncodeECPublicKey(p),
            RSAPublicKey p => PivKeyEncoder.EncodeRSAPublicKey(p),
            Curve25519PublicKey p => PivKeyEncoder.EncodeCurve25519PublicKey(p),
            _ => throw new ArgumentException("The type conversion for the specified key type is not supported", nameof(parameters))
        };
    }
    
    /// <summary>
    /// Encodes a private key into the PIV format.
    /// </summary>
    /// <param name="parameters">The private key parameters to encode.</param>
    /// <returns>A BER encoded byte array containing the encoded private key.</returns>
    /// <exception cref="ArgumentException">Thrown when the key type is not supported or when RSA key components have invalid lengths.</exception>
    public static Memory<byte> EncodeAsPiv(this IPrivateKey parameters)
    {
        return parameters switch
        {
            ECPrivateKey p => PivKeyEncoder.EncodeECPrivateKey(p),
            RSAPrivateKey p => PivKeyEncoder.EncodeRSAPrivateKey(p),
            Curve25519PrivateKey p => PivKeyEncoder.EncodeCurve25519PrivateKey(p),
            _ => throw new ArgumentException("The type conversion for the specified key type is not supported", nameof(parameters))
        };
    }
}

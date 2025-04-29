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
using System.IO;
using System.Security.Cryptography;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities;

/// <summary>
/// Represents a cryptographic key for testing purposes, supporting both RSA and EC keys.
/// Provides conversion methods to standard .NET cryptographic types.
/// </summary>
public class TestKey : TestCrypto
{
    public readonly KeyType KeyType;
    private readonly bool _isPrivate;
    public KeyDefinition KeyDefinition { get; }

    /// <summary>
    /// Loads a test key from the TestData directory.
    /// </summary>
    /// <param name="filePath">The path to the PEM file containing the key data</param>
    /// <param name="keyType"></param>
    /// <param name="isPrivate"></param>
    /// <returns>A TestKey instance representing the loaded key</returns>
    private TestKey(
        string filePath,
        KeyType keyType,
        bool isPrivate) : base(filePath)
    {
        KeyDefinition = keyType.GetKeyDefinition();
        KeyType = keyType;
        _isPrivate = isPrivate;
    }

    public KeyDefinition GetKeyDefinition() => KeyDefinitions.GetByKeyType(KeyType);

    public byte[] GetExponent()
    {
        try
        {
            return AsRSA().ExportParameters(false).Exponent!;
        }
        catch { return []; }
    }

    public byte[] GetModulus()
    {
        try
        {
            return AsRSA().ExportParameters(false).Modulus!;
        }
        catch { return []; }
    }

    public byte[] GetPublicPoint()
    {
        if (KeyDefinition.IsRSA)
        {
            throw new InvalidOperationException(
                "Use AsRSA(), GetModulus() or GetExponent() instead for RSA keys");
        }

        if (_isPrivate)
        {
            return Load(KeyType, false).GetPublicPoint();
        }

        return KeyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA }
            ? ECPublicKey.CreateFromPkcs8(EncodedKey).PublicPoint.ToArray()
            : Curve25519PublicKey.CreateFromPkcs8(EncodedKey).PublicPoint.ToArray();
    }
    
    public IPublicKey GetPublicKey()
    {
        if (KeyDefinition.IsRSA)
        {
            return RSAPublicKey.CreateFromPkcs8(EncodedKey);
        }
        
        if (_isPrivate)
        {
            return Load(KeyType, false).GetPublicKey();
        }
        
        if (KeyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA })
        {
            return ECPublicKey.CreateFromPkcs8(EncodedKey);
        }

        return Curve25519PublicKey.CreateFromPkcs8(EncodedKey);
    }

    public byte[] GetPrivateKeyValue()
    {
        if (KeyDefinition.IsRSA)
        {
            throw new InvalidOperationException("Use AsRSA() instead for RSA keys");
        }

        if (!_isPrivate)
        {
            return Load(KeyType, true).GetPrivateKeyValue();
        }

        return KeyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA }
            ? ECPrivateKey.CreateFromPkcs8(EncodedKey).Parameters.D!
            : Curve25519PrivateKey.CreateFromPkcs8(EncodedKey).PrivateKey.ToArray();
    }

    public IPrivateKey GetPrivateKey()
    {
        if (KeyDefinition.IsRSA)
        {
            return RSAPrivateKey.CreateFromPkcs8(EncodedKey);
        }

        return !_isPrivate
            ? Load(KeyType, true).GetPrivateKey()
            : KeyDefinition is { IsEllipticCurve: true, AlgorithmOid: Oids.ECDSA }
                ? ECPrivateKey.CreateFromPkcs8(EncodedKey)
                : Curve25519PrivateKey.CreateFromPkcs8(EncodedKey);
    }

    /// <summary>
    /// Converts the key to an RSA instance if it represents an RSA key.
    /// </summary>
    /// <returns>RSA instance initialized with the key data</returns>
    /// <exception cref="InvalidOperationException">Thrown if the key is not an RSA key</exception>
    public RSA AsRSA()
    {
        if (!KeyType.IsRSA())
        {
            throw new InvalidOperationException("Not an RSA key");
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(_pemStringFull);

        return rsa;
    }

    /// <summary>
    /// Converts the key to an ECDsa instance if it represents an EC key.
    /// </summary>
    /// <returns>ECDsa instance initialized with the key data</returns>
    /// <exception cref="InvalidOperationException">Thrown if the key is not an EC key</exception>
    public ECDsa AsECDsa()
    {
        if (!KeyType.IsEllipticCurve())
        {
            throw new InvalidOperationException("Not an EC key");
        }

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(_pemStringFull);

        return ecdsa;
    }

    public static TestKey LoadPublicKey(
        KeyType keyType,
        int? index = null) =>
        Load(keyType, false, index);

    public static TestKey LoadPrivateKey(
        KeyType keyType,
        int? index = null) =>
        Load(keyType, true, index);

    private static TestKey Load(
        KeyType keyType,
        bool isPrivate,
        int? index = null)
    {
        if (index is 0 or 1)
        {
            index = null;
        }

        var curveName = keyType.ToString().ToLower();
        var fileName = $"{curveName}_{(isPrivate ? "private" : "public")}{(index.HasValue ? $"_{index}" : "")}.pem";
        var filePath = Path.Combine(TestDataDirectory, fileName);
        return new TestKey(filePath, keyType, isPrivate);
    }
}

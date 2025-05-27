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
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv.Converters;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

/// <summary>
///  This class tests the TestKey.EncodeAsPiv (PivKeyEncoder), PivKeyDecoder and AsnPublicKeyDecoder classes.
/// </summary>
public class PivEncoderDecoderTests
{
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void PivAndPkcsPublicEncodedKeys_AreEqual(KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var testPublicKey = testKey.AsPublicKey();
        var testPivEncodedPublicKey = testPublicKey.EncodeAsPiv();
        Assert.True(testPivEncodedPublicKey.Length > 0);

        // Act
        //  Convert from PivEncoding to PublicKey using PivKeyDecoder.
        var publicKeyFromPiv =
            PivKeyDecoder.CreatePublicKey(testPivEncodedPublicKey, keyType);

        //  Convert from PivEncoding to PublicKey using AsnPublicKeyDecoder
        var publicKeyFromPkcs = AsnPublicKeyDecoder.CreatePublicKey(testKey.EncodedKey);

        // Assert
        Assert.Equal(testPivEncodedPublicKey, publicKeyFromPiv.EncodeAsPiv());
        Assert.Equal(testPivEncodedPublicKey, publicKeyFromPkcs.EncodeAsPiv());

        //  Convert from PublicKey to PivEncoding and compare with test key
        switch (keyType)
        {
            case var _ when keyType.IsECDsa():
                {
                    var pivEncodedKey =
                        PivKeyEncoder.EncodeECPublicKey(publicKeyFromPiv.Cast<ECPublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivEncodedKey);
                    break;
                }
            case var _ when keyType.IsCurve25519():
                {
                    var pivEncodedKey =
                        PivKeyEncoder.EncodeCurve25519PublicKey(publicKeyFromPiv.Cast<Curve25519PublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivEncodedKey);
                    break;
                }
            case var _ when keyType.IsRSA():
                {
                    var pivEncodedKey =
                        PivKeyEncoder.EncodeRSAPublicKey(publicKeyFromPiv.Cast<RSAPublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivEncodedKey);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }
    }
    
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.RSA3072)]
    [InlineData(KeyType.RSA4096)]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void PivAndPkcsPrivateEncodedKeys_AreEqual(KeyType keyType)
    {
        // Arrange
        var testKey = TestKeys.GetTestPrivateKey(keyType);
        var testPrivateKey = testKey.AsPrivateKey();
        var testPivEncodedPrivateKey = testPrivateKey.EncodeAsPiv();
        Assert.True(testPivEncodedPrivateKey.Length > 0);

        // Act
        //  Convert from PivEncoding to PrivateKey using PivKeyDecoder.
        var publicKeyFromPiv =
            PivKeyDecoder.CreatePrivateKey(testPivEncodedPrivateKey, keyType);

        //  Convert from PivEncoding to PrivateKey using AsnPrivateKeyDecoder
        var publicKeyFromPkcs = AsnPrivateKeyDecoder.CreatePrivateKey(testKey.EncodedKey);

        // Assert
        Assert.Equal(testPivEncodedPrivateKey, publicKeyFromPiv.EncodeAsPiv());
        Assert.Equal(testPivEncodedPrivateKey, publicKeyFromPkcs.EncodeAsPiv());

        //  Convert from PrivateKey to PivEncoding and compare with test key
        switch (keyType)
        {
            case var _ when keyType.IsECDsa():
                {
                    var pivEncodedKey =
                        PivKeyEncoder.EncodeECPrivateKey(publicKeyFromPiv.Cast<ECPrivateKey>());
                    Assert.Equal(testPivEncodedPrivateKey, pivEncodedKey);
                    break;
                }
            case var _ when keyType.IsCurve25519():
                {
                    var pivEncodedKey =
                        PivKeyEncoder.EncodeCurve25519PrivateKey(publicKeyFromPiv.Cast<Curve25519PrivateKey>());
                    Assert.Equal(testPivEncodedPrivateKey, pivEncodedKey);
                    break;
                }
            case var _ when keyType.IsRSA():
                {
                    var pivEncodedKey =
                        PivKeyEncoder.EncodeRSAPrivateKey(publicKeyFromPiv.Cast<RSAPrivateKey>());
                    Assert.Equal(testPivEncodedPrivateKey, pivEncodedKey);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }
    }
}

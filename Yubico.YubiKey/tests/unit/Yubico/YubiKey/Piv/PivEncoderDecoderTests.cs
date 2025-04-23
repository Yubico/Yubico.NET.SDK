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
///  This class tests the TestKey.AsPivPublicKey, PivEncodingKeyConverter and PivKeyConverter classes.
/// </summary>
public class PivEncoderDecoderTests
{
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    public void PivAndPkcsEncodedKeys_AreEqual2(
        KeyType keyType)
    {
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var testPublicKey = testKey.GetPublicKey();
        var testPivEncodedPublicKey = testPublicKey.EncodeAsPiv();
        Assert.True(testPivEncodedPublicKey.Length > 0);

        // Convert from PivEncoding to PublicKey using Key Converter.
        var decodedPublicKey =
            PivKeyDecoder.CreatePublicKey(testPivEncodedPublicKey, keyType);

        // Convert from PivEncoding to PublicKey using Asn
        var pkFromAsnReaderAndTestKey = AsnPublicKeyDecoder.CreatePublicKey(testKey.EncodedKey);

        Assert.Equal(testPivEncodedPublicKey, decodedPublicKey.EncodeAsPiv());
        Assert.Equal(testPivEncodedPublicKey, pkFromAsnReaderAndTestKey.EncodeAsPiv());

        // Convert from PublicKey to PivEncoding and compare with test key
        switch (keyType)
        {
            case var _ when keyType.IsEllipticCurve():
                {
                    var pivKey =
                        PivKeyEncoder.EncodeECPublicKey(decodedPublicKey.Cast<ECPublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivKey);
                    break;
                }
            case var _ when keyType.IsCurve25519():
                {
                    var pivKey =
                        PivKeyEncoder.EncodeCurve25519PublicKey(decodedPublicKey
                            .Cast<Curve25519PublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivKey);
                    break;
                }
            case var _ when keyType.IsRSA():
                {
                    var pivKey =
                        PivKeyEncoder.EncodeRSAPublicKey(decodedPublicKey
                            .Cast<RSAPublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivKey);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }
    }
    
    [Theory]
    [InlineData(KeyType.RSA1024)]
    [InlineData(KeyType.RSA2048)]
    [InlineData(KeyType.ECP256)]
    [InlineData(KeyType.ECP384)]
    [InlineData(KeyType.Ed25519)]
    [InlineData(KeyType.X25519)]
    public void PivAndPkcsEncodedKeys_AreEqual(
        KeyType keyType)
    {
        var testKey = TestKeys.GetTestPublicKey(keyType);
        var testPublicKey = testKey.GetPublicKey();
        var testPivEncodedPublicKey = testPublicKey.EncodeAsPiv();
        Assert.True(testPivEncodedPublicKey.Length > 0);

        // Convert from PivEncoding to PublicKey using Key Converter.
        var decodedPublicKey =
            PivKeyDecoder.CreatePublicKey(testPivEncodedPublicKey, keyType);

        // Convert from PivEncoding to PublicKey using Asn
        var pkFromAsnReaderAndTestKey = AsnPublicKeyDecoder.CreatePublicKey(testKey.EncodedKey);

        Assert.Equal(testPivEncodedPublicKey, decodedPublicKey.EncodeAsPiv());
        Assert.Equal(testPivEncodedPublicKey, pkFromAsnReaderAndTestKey.EncodeAsPiv());

        // Convert from PublicKey to PivEncoding and compare with test key
        switch (keyType)
        {
            case var _ when keyType.IsCurve25519():
                {
                    var pivKey =
                        PivKeyEncoder.EncodeCurve25519PublicKey(decodedPublicKey
                            .Cast<Curve25519PublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivKey);
                    break;
                }
            case var _ when keyType.IsECDsa():
                {
                    var pivKey =
                        PivKeyEncoder.EncodeECPublicKey(decodedPublicKey.Cast<ECPublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivKey);
                    break;
                }
            case var _ when keyType.IsRSA():
                {
                    var pivKey =
                        PivKeyEncoder.EncodeRSAPublicKey(decodedPublicKey
                            .Cast<RSAPublicKey>());
                    Assert.Equal(testPivEncodedPublicKey, pivKey);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }
    }
}

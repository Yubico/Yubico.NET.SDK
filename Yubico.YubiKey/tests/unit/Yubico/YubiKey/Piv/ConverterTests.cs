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
///  This class tests the TestKey.AsPivPublicKey, PivEncodingKeyConverter and KeyToPivEncoding classes.
/// </summary>
public class ConverterTests
{
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

        // Get test key as PivPublicKey
        var testPivPublicKey = testKey.AsPivPublicKey();
        Assert.NotNull(testPivPublicKey);

        // Convert from PivEncoding to PublicKey using Key Converter.
        var pkFromKeyConverter =
            PivEncodingToKey.CreatePublicKey(testPivPublicKey.PivEncodedPublicKey, keyType);

        // Convert from PivEncoding to PublicKey using Asn
        var pkFromAsnReaderAndTestKey = AsnPublicKeyReader.CreateKey(testKey.EncodedKey);

        Assert.Equal(testPivPublicKey.PivEncodedPublicKey, pkFromKeyConverter.EncodeAsPiv());
        Assert.Equal(testPivPublicKey.PivEncodedPublicKey, pkFromAsnReaderAndTestKey.EncodeAsPiv());

        // Convert from PublicKey to PivEncoding and compare with test key
        switch (keyType)
        {
            case KeyType.ECP256:
            case KeyType.ECP384:
            case KeyType.ECP521:
                {
                    var pivKey =
                        KeyToPivEncoding.EncodeECPublicKey(pkFromKeyConverter.Cast<ECPublicKey>());
                    Assert.Equal(testPivPublicKey.PivEncodedPublicKey, pivKey);
                    break;
                }
            case KeyType.X25519:
            case KeyType.Ed25519:
                {
                    var pivKey =
                        KeyToPivEncoding.EncodeCurve25519PublicKey(pkFromKeyConverter
                            .Cast<Curve25519PublicKey>());
                    Assert.Equal(testPivPublicKey.PivEncodedPublicKey, pivKey);
                    break;
                }
            case KeyType.RSA1024:
            case KeyType.RSA2048:
            case KeyType.RSA3072:
            case KeyType.RSA4096:
                {
                    var pivKey =
                        KeyToPivEncoding.EncodeRSAPublicKey(pkFromKeyConverter
                            .Cast<RSAPublicKey>());
                    Assert.Equal(testPivPublicKey.PivEncodedPublicKey, pivKey);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(keyType), keyType, null);
        }
    }
}

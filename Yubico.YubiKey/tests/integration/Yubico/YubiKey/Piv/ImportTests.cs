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

using System;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv;

[Trait(TraitTypes.Category, TestCategories.Simple)]
public class ImportTests : PivSessionIntegrationTestBase
{
    [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
    [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
    [InlineData(KeyType.ECP256, StandardTestDevice.Fw5)]
    [InlineData(KeyType.ECP384, StandardTestDevice.Fw5)]
    [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
    [InlineData(KeyType.X25519, StandardTestDevice.Fw5)]
    public void ImportPrivateKey_with_PrivateKey_Succeeds_and_HasExpectedValues(
        KeyType keyType,
        StandardTestDevice testDeviceType)
    {
        // Arrange
        TestDeviceType = testDeviceType;
        var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
        var publicKey = testPublicKey.AsPublicKey();
        var privateKey = testPrivateKey.AsPrivateKey();

        const PivPinPolicy expectedPinPolicy = PivPinPolicy.Once;
        const PivTouchPolicy expectedTouchPolicy = PivTouchPolicy.Always;

        // Act
        Session.ImportPrivateKey(PivSlot.Retired1, privateKey, expectedPinPolicy, expectedTouchPolicy);

        // Assert
        var resultMetadata = Session.GetMetadata(PivSlot.Retired1);
        Assert.Equal(expectedPinPolicy, resultMetadata.PinPolicy);
        Assert.Equal(expectedTouchPolicy, resultMetadata.TouchPolicy);
        Assert.Equal(publicKey.ExportSubjectPublicKeyInfo(),
            resultMetadata.PublicKeyParameters?.ExportSubjectPublicKeyInfo());
        AssertKeyData(keyType, resultMetadata, testPrivateKey);
    }

    [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
    [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA1024, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA2048, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA3072, StandardTestDevice.Fw5)]
    [InlineData(KeyType.RSA4096, StandardTestDevice.Fw5)]
    public void Import_KeyAndMatchingCert(
        KeyType keyType,
        StandardTestDevice testDeviceType)
    {
        // Arrange
        TestDeviceType = testDeviceType;
        var testPrivateKey = TestKeys.GetTestPrivateKey(keyType);
        var testCert = TestKeys.GetTestCertificate(keyType);
        var privateKey = testPrivateKey.AsPrivateKey();

        // Act
        Session.ImportPrivateKey(0x90, privateKey);
        Session.ImportCertificate(0x90, testCert.AsX509Certificate2());

        // Assert
        var resultMetadata = Session.GetMetadata(0x90);
        AssertKeyData(keyType, resultMetadata, testPrivateKey);

        var resultCert = Session.GetCertificate(0x90);
        Assert.True(resultCert.Equals(testCert.AsX509Certificate2()));
    }

    [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
    [InlineData(KeyType.RSA1024, false)]
    [InlineData(KeyType.RSA2048, false)]
    [InlineData(KeyType.RSA3072, false)]
    [InlineData(KeyType.RSA4096, false)]
    [InlineData(KeyType.ECP256, false)]
    [InlineData(KeyType.ECP384, false)]
    [InlineData(KeyType.Ed25519, false)]
    [InlineData(KeyType.RSA1024, true)]
    [InlineData(KeyType.RSA2048, true)]
    [InlineData(KeyType.RSA3072, true)]
    [InlineData(KeyType.RSA4096, true)]
    [InlineData(KeyType.ECP256, true)]
    [InlineData(KeyType.ECP384, true)]
    [InlineData(KeyType.Ed25519, true)]
    public void ImportCertificate_ImportedCert_Equals_TestCert(
        KeyType keyType,
        bool compressed)
    {
        var testCertificate = TestKeys.GetTestCertificate(keyType);
        var testX509Certificate = testCertificate.AsX509Certificate2();

        Session.ImportCertificate(0x90, testX509Certificate, compressed);

        var resultCert = Session.GetCertificate(0x90);
        Assert.True(resultCert.Equals(testX509Certificate));
    }

    private static void AssertKeyData(
        KeyType keyType,
        PivMetadata resultMetadata,
        TestKey testPrivateKey)
    {
        Assert.Equal(keyType.GetPivAlgorithm(), resultMetadata.Algorithm);

        if (keyType.IsEllipticCurve())
        {
            var publicPoint = resultMetadata.PublicKeyParameters switch
            {
                ECPublicKey ecDsa => ecDsa.PublicPoint.ToArray(),
                Curve25519PublicKey edDsa => edDsa.PublicPoint.ToArray(),
                _ => throw new ArgumentException("Invalid public key type")
            };
            Assert.Equal(testPrivateKey.GetPublicPoint(), publicPoint);
        }
        else if (keyType.IsRSA())
        {
            var parameters = resultMetadata.PublicKeyParameters as RSAPublicKey;
            Assert.NotNull(parameters);

            var rsaParameters = testPrivateKey.AsRSA().ExportParameters(false);
            Assert.Equal(rsaParameters.Modulus, parameters.Parameters.Modulus);
            Assert.Equal(rsaParameters.Exponent, parameters.Parameters.Exponent);
        }

        var publicKey = TestKeyExtensions.AsPublicKey(TestKeys.GetTestPublicKey(keyType));
        Assert.Equal(publicKey.ExportSubjectPublicKeyInfo(),
            resultMetadata.PublicKeyParameters?.ExportSubjectPublicKeyInfo());
    }

    [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
    [InlineData(KeyType.ECP256, StandardTestDevice.Fw5)]
    [InlineData(KeyType.Ed25519, StandardTestDevice.Fw5)]
    [Obsolete("Legacy test", false)]
    public void Import_with_PivEccPrivateKey_Succeeds(
        KeyType keyType,
        StandardTestDevice testDeviceType)
    {
        TestDeviceType = testDeviceType;
        using var pivSession = GetSession();

        var testPrivateKey = TestKeys.GetTestPrivateKey(keyType);
        var piv = new PivEccPrivateKey(testPrivateKey.GetPrivateKeyValue(), keyType.GetPivAlgorithm());
        pivSession.ImportPrivateKey(0x90, piv);
    }
}

// Copyright 2021 Yubico AB
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
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class KeyAgreeTests
    {
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(KeyType.ECP256, PivPinPolicy.Always, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, PivPinPolicy.Never, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, PivPinPolicy.Always, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, PivPinPolicy.Never, StandardTestDevice.Fw5)]
        [InlineData(KeyType.X25519, PivPinPolicy.Never, StandardTestDevice.Fw5)]
        [InlineData(KeyType.X25519, PivPinPolicy.Always, StandardTestDevice.Fw5)]
        public void KeyAgree_Succeeds(
            KeyType keyType,
            PivPinPolicy pinPolicy,
            StandardTestDevice testDeviceType)
        {
            // Arrange
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(keyType);
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var privateKeyParameters = AsnPrivateKeyReader.CreatePrivateKey(testPrivateKey.EncodedKey);
            IPublicKey peerPublicKey;
            var peerPrivateKeyEcParameters = new ECParameters();

            if (keyType is KeyType.X25519)
            {
                var testSelectedPublicKeyPeer = TestKeys.GetTestPublicKey(keyType, 2);
                peerPublicKey = Curve25519PublicKey.CreateFromPkcs8(testSelectedPublicKeyPeer.EncodedKey);
            }
            else
            {
                var curve = ECCurve.CreateFromValue(keyType.GetCurveOid()!);
                var ecDsa = ECDsa.Create(curve);
                peerPrivateKeyEcParameters = ecDsa.ExportParameters(true);
                var peerPublicKeyEcParameters = ecDsa.ExportParameters(false);
                peerPublicKey = ECPublicKey.CreateFromParameters(peerPublicKeyEcParameters);
            }

            // -> Import Private Key
            using var pivSession = GetSession(testDevice);
            pivSession.ImportPrivateKey(0x85, privateKeyParameters, pinPolicy, PivTouchPolicy.Never);

            // Act
            var yubikeySecret = pivSession.KeyAgree(0x85, peerPublicKey);

            // Assert
            if (keyType is KeyType.X25519)
            {
                // We have pre-generated shared secrets for X25519
                const string keyAgreeFilename = "x25519_private_and_public2_shared_secret.bin";
                var expectedSharedSecret = TestCrypto.ReadTestData(keyAgreeFilename);
                Assert.Equal(expectedSharedSecret, yubikeySecret);
            }
            else
            {
                // Perform ECDH using generated key and the imported YK public key
                using var peerEcdh = ECDiffieHellman.Create(peerPrivateKeyEcParameters);
                var yubiKeyParametersPublic = testPublicKey.AsECDsa().ExportParameters(false);
                using var yubikeyEcdh = ECDiffieHellman.Create(yubiKeyParametersPublic);
                var peerSecret = peerEcdh.DeriveRawSecretAgreement(yubikeyEcdh.PublicKey);
                
                Assert.Equal(yubikeySecret.Length, peerSecret.Length);
                Assert.Equal(yubikeySecret, peerSecret);
            }
        }

        [Theory]
        [InlineData(KeyType.ECP256, 0x8a, RsaFormat.Sha1, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, 0x8a, RsaFormat.Sha256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, 0x8a, RsaFormat.Sha384, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP256, 0x8a, RsaFormat.Sha512, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, 0x8b, RsaFormat.Sha1, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, 0x8b, RsaFormat.Sha256, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, 0x8b, RsaFormat.Sha384, StandardTestDevice.Fw5)]
        [InlineData(KeyType.ECP384, 0x8b, RsaFormat.Sha512, StandardTestDevice.Fw5)]
        [Obsolete("Fix later")] // TODO
        public void KeyAgree_MatchesCSharp(
            KeyType keyType,
            byte slotNumber,
            int digestAlgorithm,
            StandardTestDevice testDeviceType)
        {
            // Build the correspondent objects.
            var isValid = SampleKeyPairs.GetKeysAndCertPem(keyType, true, out _, out _, out var privateKeyPem);
            Assert.True(isValid);
            var privateKey = new KeyConverter(privateKeyPem!.ToCharArray());

            var correspondentPub = privateKey.GetPivPublicKey();
            var correspondentEcc = (PivEccPublicKey)correspondentPub;

            var ecDsaObject = privateKey.GetEccObject();
            var ecParams = ecDsaObject.ExportParameters(true);
            var correspondentObject = ECDiffieHellman.Create(ecParams);
            privateKey.Clear();

            // Build the YubiKey objects.
            _ = SampleKeyPairs.GetKeysAndCertPem(keyType, false, out _, out _, out privateKeyPem);
            privateKey = new KeyConverter(privateKeyPem!.ToCharArray());
            var pivPrivateKey = privateKey.GetPivPrivateKey();

            ecDsaObject = privateKey.GetEccObject();
            ecParams = ecDsaObject.ExportParameters(false);
            var eccObject = ECDiffieHellman.Create(ecParams);
            privateKey.Clear();

            var hashAlgorithm = digestAlgorithm switch
            {
                RsaFormat.Sha256 => HashAlgorithmName.SHA256,
                RsaFormat.Sha384 => HashAlgorithmName.SHA384,
                RsaFormat.Sha512 => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA1,
            };

            // The correspondent computes the digest of the shared secret.
            var correspondentSecret = correspondentObject.DeriveKeyFromHash(eccObject.PublicKey, hashAlgorithm);

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            // The YubiKey computes the shared secret.
            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(slotNumber, pivPrivateKey, PivPinPolicy.Always, PivTouchPolicy.Never);

                var sharedSecret = pivSession.KeyAgree(slotNumber, correspondentEcc);

                using var digester = GetHashAlgorithm(digestAlgorithm);
                digester.Initialize();
                _ = digester.TransformFinalBlock(sharedSecret, 0, sharedSecret.Length);

                isValid = correspondentSecret.SequenceEqual(digester.Hash!);
                Assert.True(isValid);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        [Obsolete("Obsolete")]
        public void NoKeyInSlot_KeyAgree_Exception(
            StandardTestDevice testDeviceType)
        {
            _ = SampleKeyPairs.GetKeysAndCertPem(KeyType.ECP384, false, out _, out var publicKeyPem, out _);
            var publicKey = new KeyConverter(publicKeyPem!.ToCharArray());
            var pivPublicKey = publicKey.GetPivPublicKey();

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                _ = Assert.Throws<InvalidOperationException>(() => pivSession.KeyAgree(0x9a, pivPublicKey));
            }
        }

        private static HashAlgorithm GetHashAlgorithm(
            int digestAlgorithm) => digestAlgorithm switch
        {
            RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
            RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
            RsaFormat.Sha512 => CryptographyProviders.Sha512Creator(),
            _ => CryptographyProviders.Sha1Creator(),
        };

        private static PivSession GetSession(
            IYubiKeyDevice testDevice)
        {
            var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            return pivSession;
        }
    }
}

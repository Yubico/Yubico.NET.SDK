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
    public class KeyAgreeTests
    {
        [Theory]
        [InlineData(PivAlgorithm.EccP256, PivPinPolicy.Always, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, PivPinPolicy.Never, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, PivPinPolicy.Always, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, PivPinPolicy.Never, StandardTestDevice.Fw5)]
        public void KeyAgree_Succeeds(PivAlgorithm algorithm, PivPinPolicy pinPolicy, StandardTestDevice testDeviceType)
        {
            // Get the correspondent public key.
            SampleKeyPairs.GetPemKeyPair(algorithm, out string publicKeyPem, out _);
            var publicKey = new KeyConverter(publicKeyPem.ToCharArray());
            PivPublicKey pivPublicKey = publicKey.GetPivPublicKey();
            var eccPublicKey = (PivEccPublicKey)pivPublicKey;
            int expectedSecretLength = (eccPublicKey.PublicPoint.Length - 1) / 2;

            bool isValid = SampleKeyPairs.GetKeyAndCertPem(algorithm, true, out _, out string privateKeyPem);
            Assert.True(isValid);
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(0x85, pivPrivateKey, pinPolicy, PivTouchPolicy.Never);

                byte[] sharedSecret = pivSession.KeyAgree(0x85, eccPublicKey);
                Assert.Equal(expectedSecretLength, sharedSecret.Length);
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256, 0x8a, RsaFormat.Sha1, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, 0x8a, RsaFormat.Sha256, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, 0x8a, RsaFormat.Sha384, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, 0x8a, RsaFormat.Sha512, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, 0x8b, RsaFormat.Sha1, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, 0x8b, RsaFormat.Sha256, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, 0x8b, RsaFormat.Sha384, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, 0x8b, RsaFormat.Sha512, StandardTestDevice.Fw5)]
        public void KeyAgree_MatchesCSharp(PivAlgorithm algorithm, byte slotNumber, int digestAlgorithm, StandardTestDevice testDeviceType)
        {
            // Build the correspondent objects.
            bool isValid = SampleKeyPairs.GetKeyAndCertPem(algorithm, true, out _, out string privateKeyPem);
            Assert.True(isValid);
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());

            PivPublicKey correspondentPub = privateKey.GetPivPublicKey();
            var correspondentEcc = (PivEccPublicKey)correspondentPub;

            ECDsa ecDsaObject = privateKey.GetEccObject();
            ECParameters ecParams = ecDsaObject.ExportParameters(true);
            var correspondentObject = ECDiffieHellman.Create(ecParams);
            privateKey.Clear();

            // Build the YubiKey objects.
            SampleKeyPairs.GetPemKeyPair(algorithm, out _, out privateKeyPem);
            privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

            ecDsaObject = privateKey.GetEccObject();
            ecParams = ecDsaObject.ExportParameters(false);
            var eccObject = ECDiffieHellman.Create(ecParams);
            privateKey.Clear();

            HashAlgorithmName hashAlgorithm = digestAlgorithm switch
            {
                RsaFormat.Sha256 => HashAlgorithmName.SHA256,
                RsaFormat.Sha384 => HashAlgorithmName.SHA384,
                RsaFormat.Sha512 => HashAlgorithmName.SHA512,
                _ => HashAlgorithmName.SHA1,
            };

            // The correspondent computes the digest of the shared secret.
            byte[] correspondentSecret = correspondentObject.DeriveKeyFromHash(eccObject.PublicKey, hashAlgorithm);

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            // The YubiKey computes the shared secret.
            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(slotNumber, pivPrivateKey, PivPinPolicy.Always, PivTouchPolicy.Never);

                byte[] sharedSecret = pivSession.KeyAgree(slotNumber, correspondentEcc);

                using HashAlgorithm digester = GetHashAlgorithm(digestAlgorithm);
                digester.Initialize();
                _ = digester.TransformFinalBlock(sharedSecret, 0, sharedSecret.Length);

                isValid = correspondentSecret.SequenceEqual(digester.Hash!);
                Assert.True(isValid);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void NoKeyInSlot_KeyAgree_Exception(StandardTestDevice testDeviceType)
        {
            SampleKeyPairs.GetPemKeyPair(PivAlgorithm.EccP384, out string publicKeyPem, out _);
            var publicKey = new KeyConverter(publicKeyPem.ToCharArray());
            PivPublicKey pivPublicKey = publicKey.GetPivPublicKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                _ = Assert.Throws<InvalidOperationException>(() => pivSession.KeyAgree(0x9a, pivPublicKey));
            }
        }

        private static HashAlgorithm GetHashAlgorithm(int digestAlgorithm) => digestAlgorithm switch
        {
            RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
            RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
            RsaFormat.Sha512 => CryptographyProviders.Sha512Creator(),
            _ => CryptographyProviders.Sha1Creator(),
        };
    }
}

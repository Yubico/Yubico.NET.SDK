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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class CertSizeTests : PivSessionIntegrationTestBase
    {
        [Fact]
        public void SingleCertSize_3052()
        {
            // Arrange
            byte leafSlotNumber = 0x83;
            var extensionSize = 2100;
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(KeyType.RSA2048);
            var testPublicKeyRsa = testPublicKey.AsRSA();
            using var caCert = GetCACert(KeyType.RSA2048);
            using var rng = RandomObjectUtility.GetRandomObject(null);

            //  With an extension of 2100 bytes, the cert should be 3052 bytes.

            var extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            var newCert = GetCertWithRandomExtension(caCert, testPublicKeyRsa, extensionData);
            //            _output.WriteLine ("cert size: {0} from extension = {1}", newCert.RawData.Length, extensionSize);

            // A 3052-byte cert should work.

            Session.ResetApplication();

            // Act
            Session.ImportPrivateKey(leafSlotNumber, TestKeyExtensions.AsPrivateKey(testPrivateKey));
            Session.ImportCertificate(leafSlotNumber, newCert);

            //  Now use a 2108-byte extension, to get a 3053-byte cert.
            extensionSize += 3; //extensionSize++;
            extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            newCert = GetCertWithRandomExtension(caCert, testPublicKeyRsa, extensionData);
            //            _output.WriteLine ("cert size: {0} from extension = {1}", newCert.RawData.Length, extensionSize);

            // A 3053-byte cert should throw an exception.

            Session.ResetApplication();

            // Act
            Session.ImportPrivateKey(leafSlotNumber, TestKeyExtensions.AsPrivateKey(testPrivateKey));
            _ = Assert.Throws<InvalidOperationException>(() =>
                Session.ImportCertificate(leafSlotNumber, newCert));
        }

        [Fact]
        public void MultipleCerts_3052()
        {
            var extensionSize = 2100;
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(KeyType.RSA2048);
            var testPublicKeyRsa = testPublicKey.AsRSA();
            using var caCert = GetCACert(KeyType.RSA2048);
            using var rng = RandomObjectUtility.GetRandomObject(null);


            // With an extension of 2100 bytes, the cert should be 3052 bytes.
            var extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            using var newCert = GetCertWithRandomExtension(caCert, testPublicKeyRsa, extensionData);

            Session.ResetApplication();

            // We should be able to store 16 certs of this size.
            byte leafSlotNumber = 0x82;
            for (; leafSlotNumber <= 0x91; leafSlotNumber++)
            {
                Session.ImportPrivateKey(leafSlotNumber, TestKeyExtensions.AsPrivateKey(testPrivateKey));
                Session.ImportCertificate(leafSlotNumber, newCert);
                //                    _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);
            }

            // The next storage should fail.
            Session.ImportPrivateKey(leafSlotNumber, TestKeyExtensions.AsPrivateKey(testPrivateKey));
            _ = Assert.Throws<InvalidOperationException>(() =>
                Session.ImportCertificate(leafSlotNumber, newCert));
        }

        [Fact]
        public void AllSlot_2079()
        {
            // Arrange
            var (testPublicKey, testPrivateKey) = TestKeys.GetKeyPair(KeyType.RSA2048);
            var testPublicKeyRsa = testPublicKey.AsRSA();
            using var caCert = GetCACert(KeyType.RSA2048);
            using var rng = RandomObjectUtility.GetRandomObject(null);
            const int extensionSize = 1127;
            var privateKey = TestKeyExtensions.AsPrivateKey(testPrivateKey);

            // With an extension of 1127 bytes, the cert should be 2079 bytes.
            var extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            using var newCert = GetCertWithRandomExtension(caCert, testPublicKeyRsa, extensionData);
            //            _output.WriteLine ("cert size: {0} from extension = {1}", newCert.RawData.Length, extensionSize);

            Session.ResetApplication();

            // We should be able to store 24 certs of this size.
            // 20 retired slots and the four main slots.
            byte leafSlotNumber = 0x82;
            for (; leafSlotNumber <= 0x95; leafSlotNumber++)
            {
                Session.ImportPrivateKey(leafSlotNumber, privateKey);
                Session.ImportCertificate(leafSlotNumber, newCert);
                //                    _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);
            }

            leafSlotNumber = 0x9A;
            Session.ImportPrivateKey(leafSlotNumber, privateKey);
            Session.ImportCertificate(leafSlotNumber, newCert);
            //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);

            leafSlotNumber = 0x9C;
            Session.ImportPrivateKey(leafSlotNumber, privateKey);
            Session.ImportCertificate(leafSlotNumber, newCert);
            //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);

            leafSlotNumber = 0x9D;
            Session.ImportPrivateKey(leafSlotNumber, privateKey);
            Session.ImportCertificate(leafSlotNumber, newCert);
            //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);

            leafSlotNumber = 0x9E;
            Session.ImportPrivateKey(leafSlotNumber, privateKey);
            Session.ImportCertificate(leafSlotNumber, newCert);
            //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);
        }

        private static X509Certificate2 GetCertWithRandomExtension(
            X509Certificate2 caCert,
            RSA publicKey,
            byte[] extensionData)
        {
            var nameBuilder = new X500NameBuilder();
            nameBuilder.AddNameElement(X500NameElement.Country, "US");
            nameBuilder.AddNameElement(X500NameElement.State, "CA");
            nameBuilder.AddNameElement(X500NameElement.Locality, "Palo Alto");
            nameBuilder.AddNameElement(X500NameElement.Organization, "Fake");
            nameBuilder.AddNameElement(X500NameElement.CommonName, "Fake Leaf");
            var sampleCertName = nameBuilder.GetDistinguishedName();

            var certRequest = new CertificateRequest(
                sampleCertName,
                publicKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            var extension = new X509SubjectKeyIdentifierExtension(extensionData, false);
            certRequest.CertificateExtensions.Add(extension);

            var notBefore = DateTimeOffset.Now;
            var notAfter = notBefore.AddYears(1);
            byte[] serialNumber = { 0x02, 0x4A };

            var newCert = certRequest.Create(
                caCert,
                notBefore,
                notAfter,
                serialNumber);

            return newCert;
        }

        // Build a cert (containing the private key) that we'll be able to use as
        // a CA cert.
        private static X509Certificate2 GetCACert(
            KeyType keyType)
        {
            var certObj = TestKeys.GetTestCertificate(keyType).AsX509Certificate2();
            var rsaKey = TestKeys.GetTestPrivateKey(keyType).AsRSA();
            var caCert = certObj.CopyWithPrivateKey(rsaKey);
            return caCert;
        }
    }
}

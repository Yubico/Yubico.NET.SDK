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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait("Category", "Simple")]
    public class CertSizeTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void SingleCertSize_3052(StandardTestDevice testDeviceType)
        {
            byte leafSlotNumber = 0x83;
            int extensionSize = 2107;
            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);
            using X509Certificate2 caCert = GetCACert();

            SampleKeyPairs.GetPemKeyPair(PivAlgorithm.Rsa2048, out string pubKey, out string priKey);
            var convertPublic = new KeyConverter(pubKey.ToCharArray());
            RSA dotNetPublicKey = convertPublic.GetRsaObject();
            var convertPrivate = new KeyConverter(priKey.ToCharArray());
            PivPrivateKey pivPrivateKey = convertPrivate.GetPivPrivateKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            // With an extension of 2107 bytes, the cert should be 3052 bytes.
            byte[] extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            X509Certificate2 newCert = GetCertWithRandomExtension(caCert, dotNetPublicKey, extensionData);
            //            _output.WriteLine ("cert size: {0} from extension = {1}", newCert.RawData.Length, extensionSize);

            // A 3052-byte cert should work.
            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                pivSession.ImportCertificate(leafSlotNumber, newCert);
            }

            // Now use a 2108-byte extension, to get a 3053-byte cert.
            extensionSize += 3;//extensionSize++;
            extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            newCert = GetCertWithRandomExtension(caCert, dotNetPublicKey, extensionData);
            //            _output.WriteLine ("cert size: {0} from extension = {1}", newCert.RawData.Length, extensionSize);

            // A 3053-byte cert should throw an exception.
            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.ImportCertificate(leafSlotNumber, newCert));
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void MultipleCerts_3052(StandardTestDevice testDeviceType)
        {
            int extensionSize = 2107;
            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);
            using X509Certificate2 caCert = GetCACert();

            SampleKeyPairs.GetPemKeyPair(PivAlgorithm.Rsa2048, out string pubKey, out string priKey);
            var convertPublic = new KeyConverter(pubKey.ToCharArray());
            RSA dotNetPublicKey = convertPublic.GetRsaObject();
            var convertPrivate = new KeyConverter(priKey.ToCharArray());
            PivPrivateKey pivPrivateKey = convertPrivate.GetPivPrivateKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            // With an extension of 2107 bytes, the cert should be 3052 bytes.
            byte[] extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            X509Certificate2 newCert = GetCertWithRandomExtension(caCert, dotNetPublicKey, extensionData);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                // We should be able to store 16 certs of this size.
                byte leafSlotNumber = 0x82;
                for (; leafSlotNumber <= 0x91; leafSlotNumber++)
                {
                    pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                    pivSession.ImportCertificate(leafSlotNumber, newCert);
                    //                    _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);
                }

                // The next storage should fail.
                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                _ = Assert.Throws<InvalidOperationException>(() => pivSession.ImportCertificate(leafSlotNumber, newCert));
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void AllSlot_2079(StandardTestDevice testDeviceType)
        {
            int extensionSize = 1134;
            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);
            using X509Certificate2 caCert = GetCACert();

            SampleKeyPairs.GetPemKeyPair(PivAlgorithm.Rsa2048, out string pubKey, out string priKey);
            var convertPublic = new KeyConverter(pubKey.ToCharArray());
            RSA dotNetPublicKey = convertPublic.GetRsaObject();
            var convertPrivate = new KeyConverter(priKey.ToCharArray());
            PivPrivateKey pivPrivateKey = convertPrivate.GetPivPrivateKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            // With an extension of 1134 bytes, the cert should be 2079 bytes.
            byte[] extensionData = new byte[extensionSize];
            rng.GetBytes(extensionData, 0, extensionData.Length);

            X509Certificate2 newCert = GetCertWithRandomExtension(caCert, dotNetPublicKey, extensionData);
            //            _output.WriteLine ("cert size: {0} from extension = {1}", newCert.RawData.Length, extensionSize);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                // We should be able to store 24 certs of this size.
                // 20 retired slots and the four main slots.
                byte leafSlotNumber = 0x82;
                for (; leafSlotNumber <= 0x95; leafSlotNumber++)
                {
                    pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                    pivSession.ImportCertificate(leafSlotNumber, newCert);
                    //                    _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);
                }

                leafSlotNumber = 0x9A;
                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                pivSession.ImportCertificate(leafSlotNumber, newCert);
                //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);

                leafSlotNumber = 0x9C;
                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                pivSession.ImportCertificate(leafSlotNumber, newCert);
                //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);

                leafSlotNumber = 0x9D;
                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                pivSession.ImportCertificate(leafSlotNumber, newCert);
                //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);

                leafSlotNumber = 0x9E;
                pivSession.ImportPrivateKey(leafSlotNumber, pivPrivateKey);
                pivSession.ImportCertificate(leafSlotNumber, newCert);
                //                _output.WriteLine ("slot number: {0:X2}", (int)leafSlotNumber & 0xFF);
            }
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
            X500DistinguishedName sampleCertName = nameBuilder.GetDistinguishedName();

            var certRequest = new CertificateRequest(
                sampleCertName,
                publicKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            var extension = new X509SubjectKeyIdentifierExtension(extensionData, false);
            certRequest.CertificateExtensions.Add(extension);

            DateTimeOffset notBefore = DateTimeOffset.Now;
            DateTimeOffset notAfter = notBefore.AddYears(1);
            byte[] serialNumber = { 0x02, 0x4A };

            X509Certificate2 newCert = certRequest.Create(
                caCert,
                notBefore,
                notAfter,
                serialNumber);

            return newCert;
        }

        // Build a cert (containing the private key) that we'll be able to use as
        // a CA cert.
        private static X509Certificate2 GetCACert()
        {
            _ = SampleKeyPairs.GetKeyAndCertPem(
                PivAlgorithm.Rsa2048, true, out string certPem, out string privateKeyPem);

            var cert = new CertConverter(certPem.ToCharArray());
            X509Certificate2 certObj = cert.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            RSA dotnetObj = privateKey.GetRsaObject();
            X509Certificate2 certCopy = certObj.CopyWithPrivateKey(dotnetObj);

            return certCopy;
        }
    }
}

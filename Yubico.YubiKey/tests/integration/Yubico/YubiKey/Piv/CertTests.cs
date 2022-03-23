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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class CertTests
    {
        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void GetCert_Succeeds(StandardTestDevice testDeviceType)
        {
            bool isValid = SampleKeyPairs.GetKeyAndCertPem(
                PivAlgorithm.EccP256, true, out string certPem, out string privateKeyPem);
            Assert.True(isValid);

            var cert = new CertConverter(certPem.ToCharArray());
            X509Certificate2 certObj = cert.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(0x90, pivPrivateKey);
                pivSession.ImportCertificate(0x90, certObj);

                X509Certificate2 getCert = pivSession.GetCertificate(0x90);

                Assert.True(getCert.Equals(certObj));
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void GetCert_NoAuth_Succeeds(StandardTestDevice testDeviceType)
        {
            bool isValid = SampleKeyPairs.GetKeyAndCertPem(
                PivAlgorithm.EccP256, true, out string certPem, out string privateKeyPem);
            Assert.True(isValid);

            var cert = new CertConverter(certPem.ToCharArray());
            X509Certificate2 certObj = cert.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

            byte slotNumber = 0x8B;
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            LoadKeyAndCert (slotNumber, pivPrivateKey, certObj, testDevice);

            using (var pivSession = new PivSession(testDevice))
            {
                // Try to generate a key pair. This should not succeed because
                // the mgmt key has not been authenticated.
                var genPairCommand = new GenerateKeyPairCommand(
                    0x86, PivAlgorithm.EccP256, PivPinPolicy.Default, PivTouchPolicy.Never);
                GenerateKeyPairResponse genPairResponse =
                    pivSession.Connection.SendCommand(genPairCommand);
                // A generation success is a test failure.
                Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

                // If we reach this point, we know that the mgmt key has not been
                // authenticated, so get the cert. This should work.
                X509Certificate2 getCert = pivSession.GetCertificate(slotNumber);

                Assert.True(getCert.Equals(certObj));
            }
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void CertConverter_AllOperations_Succeed(PivAlgorithm algorithm)
        {
            bool isValid = SampleKeyPairs.GetKeyAndCertPem(algorithm, true, out string certPem, out _);
            Assert.True(isValid);

            var cert = new CertConverter(certPem.ToCharArray());

            Assert.Equal(cert.Algorithm, algorithm);

            X509Certificate2 getCert = cert.GetCertObject();
            Assert.False(getCert.HasPrivateKey);

            byte[] getDer = cert.GetCertDer();
            Assert.Equal(0x30, getDer[0]);

            char[] getPem = cert.GetCertPem();
            Assert.Equal('-', getPem[0]);

            PivPublicKey pubKey = cert.GetPivPublicKey();
            Assert.Equal(algorithm, pubKey.Algorithm);

            if (cert.KeySize > 384)
            {
                using RSA rsaObject = cert.GetRsaObject();
                Assert.Equal(cert.KeySize, rsaObject.KeySize);
            }
            else
            {
                using ECDsa eccObject = cert.GetEccObject();
                Assert.Equal(cert.KeySize, eccObject.KeySize);
            }
        }

        private static void LoadKeyAndCert(byte slotNumber, PivPrivateKey privateKey, X509Certificate2 certObject, IYubiKeyDevice testDevice)
        {
            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportPrivateKey(slotNumber, privateKey);
                pivSession.ImportCertificate(slotNumber, certObject);
            }
        }
    }
}

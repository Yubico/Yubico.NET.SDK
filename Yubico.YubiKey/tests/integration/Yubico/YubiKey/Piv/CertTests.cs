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
using Xunit;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class CertTests
    {
        [Trait("Category", "Simple")]
        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void GetCert_Succeeds(PivAlgorithm algorithm) 
        {
            _ = SampleKeyPairs.GetKeyAndCertPem(algorithm, true, out var certPem, out var privateKeyPem);

            var certConverter = new CertConverter(certPem.ToCharArray());
            X509Certificate2 certificate = certConverter.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportPrivateKey(0x90, pivPrivateKey);
            pivSession.ImportCertificate(0x90, certificate);

            X509Certificate2 getCert = pivSession.GetCertificate(0x90);
            Assert.True(getCert.Equals(certificate));
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void GetCert_NoAuth_Succeeds(PivAlgorithm algorithm)
        {
            var isValid = SampleKeyPairs.GetKeyAndCertPem(
                algorithm, true, out var certPem, out var privateKeyPem);
            Assert.True(isValid);

            var certConverter = new CertConverter(certPem.ToCharArray());
            X509Certificate2 certificate = certConverter.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            PivPrivateKey pivPrivateKey = privateKey.GetPivPrivateKey();

            byte slotNumber = 0x8B;
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(StandardTestDevice.Fw5);
            LoadKeyAndCert(slotNumber, pivPrivateKey, certificate, testDevice);

            using var pivSession = new PivSession(testDevice);
            // Try to generate a key pair. This should not succeed because
            // the mgmt key has not been authenticated.
            var genPairCommand = new GenerateKeyPairCommand(
                0x86, algorithm, PivPinPolicy.Default, PivTouchPolicy.Never);
            GenerateKeyPairResponse genPairResponse =
                pivSession.Connection.SendCommand(genPairCommand);
            // A generation success is a test failure.
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

            // If we reach this point, we know that the mgmt key has not been
            // authenticated, so get the cert. This should work.
            X509Certificate2 getCert = pivSession.GetCertificate(slotNumber);

            Assert.True(getCert.Equals(certificate));
        }

        private static void LoadKeyAndCert(
            byte slotNumber, PivPrivateKey privateKey, X509Certificate2 certObject, IYubiKeyDevice testDevice)
        {
            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportPrivateKey(slotNumber, privateKey);
            pivSession.ImportCertificate(slotNumber, certObject);
        }
    }
}

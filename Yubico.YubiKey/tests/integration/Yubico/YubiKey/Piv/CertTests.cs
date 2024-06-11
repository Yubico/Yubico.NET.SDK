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
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait("Category", "Simple")]
    public class CertTests
    {
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP256)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP384)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096)]
        public void GetCert_Succeeds(StandardTestDevice targetDevice, PivAlgorithm algorithm)
        {
            _ = SampleKeyPairs.GetKeysAndCertPem(algorithm, true, out var certPem, out string _, out var privateKeyPem);

            var certConverter = new CertConverter(certPem.ToCharArray());
            var certificate = certConverter.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            var pivPrivateKey = privateKey.GetPivPrivateKey();
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(targetDevice);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportPrivateKey(0x90, pivPrivateKey);
            pivSession.ImportCertificate(0x90, certificate);

            var getCert = pivSession.GetCertificate(0x90);
            Assert.True(getCert.Equals(certificate));
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP256)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.EccP384)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa1024)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa2048)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa3072)]
        [InlineData(StandardTestDevice.Fw5, PivAlgorithm.Rsa4096)]
        public void GetCert_NoAuth_Succeeds(StandardTestDevice targetDevice, PivAlgorithm algorithm)
        {
            var isValid = SampleKeyPairs.GetKeysAndCertPem(algorithm, true, out var certPem, out _, out var privateKeyPem);
            Assert.True(isValid);

            var certConverter = new CertConverter(certPem.ToCharArray());
            var certificate = certConverter.GetCertObject();
            var privateKey = new KeyConverter(privateKeyPem.ToCharArray());
            var pivPrivateKey = privateKey.GetPivPrivateKey();

            byte slotNumber = 0x8B;
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(targetDevice);
            LoadKeyAndCert(slotNumber, pivPrivateKey, certificate, testDevice);

            using var pivSession = new PivSession(testDevice);
            // Try to generate a key pair. This should not succeed because
            // the mgmt key has not been authenticated.
            var genPairCommand = new GenerateKeyPairCommand(
                0x86, algorithm, PivPinPolicy.Default, PivTouchPolicy.Never);
            var genPairResponse =
                pivSession.Connection.SendCommand(genPairCommand);
            // A generation success is a test failure.
            Assert.Equal(ResponseStatus.AuthenticationRequired, genPairResponse.Status);

            // If we reach this point, we know that the mgmt key has not been
            // authenticated, so get the cert. This should work.
            var getCert = pivSession.GetCertificate(slotNumber);

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

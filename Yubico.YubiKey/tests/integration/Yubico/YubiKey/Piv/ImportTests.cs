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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait("Category", "Simple")]
    public class ImportTests
    {
        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 0x86, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, 0x9a, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, 0x90, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, 0x91, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, 0x88, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, 0x89, StandardTestDevice.Fw5)]
        public void SimpleImport(PivAlgorithm algorithm, byte slotNumber, StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            PivPrivateKey privateKey = SampleKeyPairs.GetPrivateKey(algorithm);
            pivSession.ImportPrivateKey(slotNumber, privateKey);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        public void KeyAndCertImport(PivAlgorithm algorithm, StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var pivSession = new PivSession(testDevice);
            var isValid = PivSupport.ResetPiv(pivSession);
            Assert.True(isValid);

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            isValid = SampleKeyPairs.GetMatchingKeyAndCert(algorithm, out X509Certificate2 cert, out PivPrivateKey privateKey);
            Assert.True(isValid);

            pivSession.ImportPrivateKey(0x90, privateKey);
            pivSession.ImportCertificate(0x90, cert);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        public void CertImport(PivAlgorithm algorithm, StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(algorithm, out X509Certificate2 cert, out PivPrivateKey _);
            Assert.True(isValid);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportCertificate(0x90, cert);
        }
    }
}

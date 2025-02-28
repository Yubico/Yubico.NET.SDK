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
using System.Formats.Asn1;
using System.Numerics;
using Xunit;
using Yubico.YubiKey.TestUtilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class ImportTests
    {
        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(PivAlgorithm.Rsa1024, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP256, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccP384, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        public void SimpleImportSucceeds(
            PivAlgorithm algorithm,
            StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var pivSession = GetSession(testDevice);

            var testPrivateKey = TestKeys.GetPrivateKey(algorithm);
            var parser = new PrivateKeyInfoParser();
            switch (algorithm)
            {
                case PivAlgorithm.EccP256 or PivAlgorithm.EccP384:
                    {
                        var keyInfo = parser.ParsePrivateKey<EcPrivateKeyInfo>(testPrivateKey.KeyBytes);
                        var pivPrivateKey = new PivEccPrivateKey(keyInfo.PrivateKey, algorithm);
                        pivSession.ImportPrivateKey(PivSlot.Retired1, pivPrivateKey);
                        break;
                    }
                case PivAlgorithm.EccEd25519:
                    {
                        var keyInfo = parser.ParsePrivateKey<EdPrivateKeyInfo>(testPrivateKey.KeyBytes);
                        var pivPrivateKey = new PivEccPrivateKey(keyInfo.PrivateKey, algorithm);
                        pivSession.ImportPrivateKey(PivSlot.Retired1, pivPrivateKey);
                        break;
                    }
                case PivAlgorithm.Rsa1024 or PivAlgorithm.Rsa2048 or PivAlgorithm.Rsa3072 or PivAlgorithm.Rsa4096:
                    {
                        var keyInfo = parser.ParsePrivateKey<RsaPrivateKeyInfo>(testPrivateKey.KeyBytes);
                        var pivPrivateKey = new PivRsaPrivateKey(keyInfo.Prime1, keyInfo.Prime2, keyInfo.Exponent1,
                            keyInfo.Exponent2, keyInfo.Coefficient);
                        pivSession.ImportPrivateKey(PivSlot.Retired1, pivPrivateKey);
                        break;
                    }
                default:
                    throw new ArgumentException($"Unexpected algorithm {algorithm}", nameof(algorithm));
            }

            var slotMetadata = pivSession.GetMetadata(PivSlot.Retired1);
            var slotAlgorithm = slotMetadata.Algorithm;

            Assert.Equal(algorithm, slotAlgorithm);
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(PivAlgorithm.EccEd25519, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa1024, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        public void KeyAndCertImport(
            PivAlgorithm algorithm,
            StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var pivSession = new PivSession(testDevice);
            var isValid = PivSupport.ResetPiv(pivSession);
            Assert.True(isValid);

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            isValid = SampleKeyPairs.GetMatchingKeyAndCert(algorithm, out var cert, out var privateKey);
            Assert.True(isValid);

            pivSession.ImportPrivateKey(0x90, privateKey!);
            pivSession.ImportCertificate(0x90, cert!);
        }

        [SkippableTheory(typeof(NotSupportedException), typeof(DeviceNotFoundException))]
        [InlineData(PivAlgorithm.Rsa1024, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa2048, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa3072, StandardTestDevice.Fw5)]
        [InlineData(PivAlgorithm.Rsa4096, StandardTestDevice.Fw5)]
        public void CertImport(
            PivAlgorithm algorithm,
            StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var isValid = SampleKeyPairs.GetMatchingKeyAndCert(algorithm, out var cert, out var _);
            Assert.True(isValid);

            using var pivSession = new PivSession(testDevice);
            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

            pivSession.ImportCertificate(0x90, cert!);

            var getCert = pivSession.GetCertificate(0x90);
            Assert.True(getCert.Equals(cert));
        }

        private static PivSession GetSession(IYubiKeyDevice testDevice)
        {
            var pivSession = new PivSession(testDevice);
            Assert.True(testDevice.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            var collectorObj = new Simple39KeyCollector();
            pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
            return pivSession;
        }
    }
}

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
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Piv
{
    public class ImportTests
    {
        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 0x86)]
        [InlineData(PivAlgorithm.Rsa2048, 0x9a)]
        [InlineData(PivAlgorithm.EccP256, 0x88)]
        [InlineData(PivAlgorithm.EccP384, 0x89)]
        public void SimpleImport(PivAlgorithm algorithm, byte slotNumber)
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                PivPrivateKey privateKey = SampleKeyPairs.GetPrivateKey(algorithm);
                pivSession.ImportPrivateKey(slotNumber, privateKey);
            }
        }

        [Fact]
        public void KeyAndCertImport()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(yubiKey))
            {
                isValid = PivSupport.ResetPiv(pivSession);
                Assert.True(isValid);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                isValid = SampleKeyPairs.GetMatchingKeyAndCert(
                    out X509Certificate2 cert, out PivPrivateKey privateKey);
                Assert.True(isValid);

                pivSession.ImportPrivateKey(0x90, privateKey);

                pivSession.ImportCertificate(0x90, cert );
            }
        }

        [Fact]
        public void CertImport()
        {
            bool isValid = SelectSupport.TrySelectYubiKey(out IYubiKeyDevice yubiKey);
            Assert.True(isValid);
            Assert.True(yubiKey.EnabledUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            isValid = SampleKeyPairs.GetMatchingKeyAndCert(
                out X509Certificate2 cert, out PivPrivateKey privateKey);
            Assert.True(isValid);

            using (var pivSession = new PivSession(yubiKey))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ImportCertificate(0x90, cert);
            }
        }
    }
}

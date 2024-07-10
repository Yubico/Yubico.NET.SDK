// Copyright 2024 Yubico AB
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

using Xunit;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    public class CertConverterTests
    {
        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        public void CertConverter_AllOperations_Succeed(PivAlgorithm algorithm)
        {
            var isValid = SampleKeyPairs.GetKeysAndCertPem(algorithm, validAttest: true, out var certPem, out _, out _);
            Assert.True(isValid);

            var certConverter = new CertConverter(certPem.ToCharArray());

            Assert.Equal(certConverter.Algorithm, algorithm);

            var getCert = certConverter.GetCertObject();
            Assert.False(getCert.HasPrivateKey);

            var getDer = certConverter.GetCertDer();
            Assert.Equal(expected: 0x30, getDer[0]);

            var getPem = certConverter.GetCertPem();
            Assert.Equal(expected: '-', getPem[0]);

            var pubKey = certConverter.GetPivPublicKey();
            Assert.Equal(algorithm, pubKey.Algorithm);

            if (certConverter.KeySize > 384)
            {
                using var rsaObject = certConverter.GetRsaObject();
                Assert.Equal(certConverter.KeySize, rsaObject.KeySize);
            }
            else
            {
                using var eccObject = certConverter.GetEccObject();
                Assert.Equal(certConverter.KeySize, eccObject.KeySize);
            }
        }
    }
}

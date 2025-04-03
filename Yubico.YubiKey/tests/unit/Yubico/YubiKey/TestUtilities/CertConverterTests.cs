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

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Piv;

namespace Yubico.YubiKey.TestUtilities
{
    public class CertConverterTests
    {
        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.P256)]
        [InlineData(KeyType.P384)]
        [Obsolete("Obsolete")]
        public void CertConverter_AllOperations_Succeed(KeyType keyType)
        {
            bool isValid = SampleKeyPairs.GetKeysAndCertPem(keyType, true, out var certPem, out _, out _);
            Assert.True(isValid);

            var certConverter = new CertConverter(certPem!.ToCharArray());

            Assert.Equal(certConverter.Algorithm, keyType.GetPivAlgorithm());

            X509Certificate2 getCert = certConverter.GetCertObject();
            Assert.False(getCert.HasPrivateKey);

            byte[] getDer = certConverter.GetCertDer();
            Assert.Equal(0x30, getDer[0]);

            char[] getPem = certConverter.GetCertPem();
            Assert.Equal('-', getPem[0]);

            PivPublicKey pubKey = certConverter.GetPivPublicKey();
            Assert.Equal(keyType.GetPivAlgorithm(), pubKey.Algorithm);

            if (certConverter.KeySize > 384)
            {
                using RSA rsaObject = certConverter.GetRsaObject();
                Assert.Equal(certConverter.KeySize, rsaObject.KeySize);
            }
            else
            {
                using ECDsa eccObject = certConverter.GetEccObject();
                Assert.Equal(certConverter.KeySize, eccObject.KeySize);
            }
        }
    }
}

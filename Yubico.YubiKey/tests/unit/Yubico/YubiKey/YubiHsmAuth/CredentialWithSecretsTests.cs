// Copyright 2022 Yubico AB
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
using Xunit;

namespace Yubico.YubiKey.YubiHsmAuth
{
    public class CredentialWithSecretsTests
    {
        private const bool _touchRequired = true;

        private static readonly byte[] _sampleCredPassword =
            new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        private static readonly CryptographicKeyType _expectedKeyType = CryptographicKeyType.Aes128;

        private static readonly string _label = "abc";

        private SampleCredWithSecrets _sampleCredWithSecrets => new SampleCredWithSecrets(
            _sampleCredPassword,
            _expectedKeyType,
            _label,
            _touchRequired);

        [Fact]
        public void Constructor_KeyTypeAes128_ObjectKeyTypeAes128()
        {
            Assert.Equal(_expectedKeyType, _sampleCredWithSecrets.KeyType);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void Constructor_InvalidPasswordLength_ThrowsArgException(int len)
        {
            var password = new byte[len];

            _ = Assert.Throws<ArgumentException>(() =>
                new SampleCredWithSecrets(
                    password,
                    _expectedKeyType,
                    _label,
                    _touchRequired));
        }

        private class SampleCredWithSecrets : CredentialWithSecrets
        {
            public SampleCredWithSecrets(
                ReadOnlyMemory<byte> credentialPassword,
                CryptographicKeyType keyType,
                string label,
                bool touchRequired)
                : base(credentialPassword, keyType, label, touchRequired)
            {
            }
        }

        /* ADD CRED PASSWORD GET/SET TESTS */
    }
}

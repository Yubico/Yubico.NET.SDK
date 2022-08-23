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
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth
{
    public class Aes128CredentialWithSecretsTests
    {
        private static readonly byte[] _password =
            new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        private static readonly byte[] _encKey =
            new byte[16] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };
        private static readonly byte[] _macKey =
            new byte[16] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };

        private static readonly string _label = "abc";
        private const bool _touchRequired = true;

        private Aes128CredentialWithSecrets _aes128Cred => new Aes128CredentialWithSecrets(
                _password,
                _encKey,
                _macKey,
                _label,
                _touchRequired);

        #region constructor
        [Fact]
        public void Constructor_KeyTypeAes128()
        {
            Assert.Equal(CryptographicKeyType.Aes128, _aes128Cred.KeyType);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void Constructor_InvalidEncKeyLength_ThrowsArgException(int len)
        {
            byte[] invalidEncKey = new byte[len];

            _ = Assert.Throws<ArgumentException>(
                () => new Aes128CredentialWithSecrets(
                    _password,
                    invalidEncKey,
                    _macKey,
                    _label,
                    _touchRequired));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void Constructor_InvalidMacKeyLength_ThrowsArgException(int len)
        {
            byte[] invalidMacKey = new byte[len];

            _ = Assert.Throws<ArgumentException>(
                () => new Aes128CredentialWithSecrets(
                    _password,
                    _encKey,
                    invalidMacKey,
                    _label,
                    _touchRequired));
        }

        [Fact]
        public void Constructor_SetGetLabel()
        {
            Aes128CredentialWithSecrets aes128Cred = new Aes128CredentialWithSecrets(
                _password,
                _encKey,
                _macKey,
                _label,
                _touchRequired);

            Assert.Equal(_label, aes128Cred.Label);
        }

        [Fact]
        public void Constructor_SetGetTouchRequired()
        {
            Aes128CredentialWithSecrets aes128Cred = new Aes128CredentialWithSecrets(
                _password,
                _encKey,
                _macKey,
                _label,
                _touchRequired);

            Assert.Equal(_touchRequired, aes128Cred.TouchRequired);
        }
        #endregion

        /* ADD ENC & MAC KEY GET/SET TESTS */
    }
}

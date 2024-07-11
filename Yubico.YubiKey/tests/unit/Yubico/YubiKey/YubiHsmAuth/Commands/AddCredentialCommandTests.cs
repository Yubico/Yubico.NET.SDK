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

using System.Text;
using Xunit;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class AddCredentialCommandTests
    {
        private static readonly byte[] _mgmtKey =
            new byte[16] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 };

        private static readonly byte[] _password =
            new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        private static readonly byte[] _encKey =
            new byte[16] { 0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };

        private static readonly byte[] _macKey =
            new byte[16] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };

        private static readonly string _label = "abc";
        private static readonly bool _touchRequired = true;

        private Aes128CredentialWithSecrets _aes128Cred => new Aes128CredentialWithSecrets(
            _password,
            _encKey,
            _macKey,
            _label,
            _touchRequired);

        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x01()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0x01, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsMgmtKeyTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x7b)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x7b, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsMgmtKeyValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x7b)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var value = reader.ReadValue(tag).ToArray();

            Assert.Equal(_mgmtKey, value);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsLabelTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x71)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x71, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsLabelValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x71)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var value = reader.ReadString(tag, Encoding.UTF8);

            Assert.Equal(_label, value);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsTouchRequiredTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x7a)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x7a, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsTouchRequiredValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x7a)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var value = reader.ReadByte(tag) != 0;

            Assert.Equal(_touchRequired, value);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsKeyTypeTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x74)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x74, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsKeyTypeValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x74)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var keyType = reader.ReadByte(tag);

            Assert.Equal((byte)_aes128Cred.KeyType, keyType);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsCredPasswordTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x73)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x73, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsCredPasswordValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x73)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var password = reader.ReadValue(tag).ToArray();

            Assert.Equal(_password, password);
        }

        [Fact]
        public void CreateCommandApdu_GivenAes128Credential_DataContainsEncKeyTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x75)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x75, tag);
        }

        [Fact]
        public void CreateCommandApdu_GivenAes128Credential_DataContainsEncKeyValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x75)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var encKey = reader.ReadValue(tag).ToArray();

            Assert.Equal(_encKey, encKey);
        }

        [Fact]
        public void CreateCommandApdu_GivenAes128Credential_DataContainsMacKeyTag()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x76)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(expected: 0x76, tag);
        }

        [Fact]
        public void CreateCommandApdu_GivenAes128Credential_DataContainsMacKeyValue()
        {
            var command = new AddCredentialCommand(_mgmtKey, _aes128Cred);
            var apdu = command.CreateCommandApdu();

            var reader = new TlvReader(apdu.Data);
            var tag = reader.PeekTag();
            while (reader.HasData && tag != 0x76)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            var macKey = reader.ReadValue(tag).ToArray();

            Assert.Equal(_macKey, macKey);
        }
    }
}

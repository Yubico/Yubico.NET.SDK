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
using System.Text;
using Xunit;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class DeleteCredentialCommandTests
    {
        private static readonly byte[] _mgmtKey =
            new byte[16] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7 };

        private static readonly string _label = "abc";

        [Fact]
        public void ConstructorMgmtKey_GivenMgmtKey_NoException()
        {
            _ = new DeleteCredentialCommand(_mgmtKey);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void CtorMgmtKey_GivenInvalidLength_ThrowsArgException(int length)
        {
            var invalidMgmtKey = new byte[length];

            _ = Assert.Throws<ArgumentException>(
                () => new DeleteCredentialCommand(invalidMgmtKey));
        }

        [Fact]
        public void ConstructorMgmtKeyLabel_ValidInputs_LabelMatchesInput()
        {
            var cmd =
                new DeleteCredentialCommand(_mgmtKey, _label);

            Assert.Equal(_label, cmd.Label);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void CtorMgmtKeyLabel_InvalidMgmtKeyLength_ThrowsArgException(int length)
        {
            var invalidMgmtKey = new byte[length];

            _ = Assert.Throws<ArgumentException>(
                () => new DeleteCredentialCommand(invalidMgmtKey, _label));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        public void CtorMgmtKeyLabel_InvalidLabelLength_ThrowsArgException(int length)
        {
            var invalidLabel = new string(c: 'a', length);

            _ = Assert.ThrowsAny<ArgumentException>(
                () => new DeleteCredentialCommand(_mgmtKey, invalidLabel));
        }

        [Fact]
        public void Label_SetGetValidString_ReturnsMatchingString()
        {
            var cmd = new DeleteCredentialCommand(_mgmtKey)
            {
                Label = _label
            };

            Assert.Equal(_label, cmd.Label);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        public void Label_SetInvalidLabelLength_ThrowsArgException(int length)
        {
            var invalidLabel = new string(c: 'a', length);

            var cmd = new DeleteCredentialCommand(_mgmtKey);

            _ = Assert.ThrowsAny<ArgumentException>(() => cmd.Label = invalidLabel);
        }

        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            var command =
                new DeleteCredentialCommand(_mgmtKey);

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x02()
        {
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0x02, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0()
        {
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0()
        {
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsMgmtKeyTag()
        {
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
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
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
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
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
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
            var command =
                new DeleteCredentialCommand(_mgmtKey, _label);
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
    }
}

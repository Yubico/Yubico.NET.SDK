﻿// Copyright 2022 Yubico AB
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
using Yubico.Core.Iso7816;
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
            byte[] invalidMgmtKey = new byte[length];

            _ = Assert.Throws<ArgumentException>(
                () => new DeleteCredentialCommand(invalidMgmtKey));
        }

        [Fact]
        public void ConstructorMgmtKeyLabel_ValidInputs_LabelMatchesInput()
        {
            DeleteCredentialCommand cmd =
                new DeleteCredentialCommand(_mgmtKey, _label);

            Assert.Equal(_label, cmd.Label);
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public void CtorMgmtKeyLabel_InvalidMgmtKeyLength_ThrowsArgException(int length)
        {
            byte[] invalidMgmtKey = new byte[length];

            _ = Assert.Throws<ArgumentException>(
                () => new DeleteCredentialCommand(invalidMgmtKey, _label));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(65)]
        public void CtorMgmtKeyLabel_InvalidLabelLength_ThrowsArgException(int length)
        {
            string invalidLabel = new string('a', length);

            _ = Assert.ThrowsAny<ArgumentException>(
                () => new DeleteCredentialCommand(_mgmtKey, invalidLabel));
        }

        [Fact]
        public void Label_SetGetValidString_ReturnsMatchingString()
        {
            DeleteCredentialCommand cmd = new DeleteCredentialCommand(_mgmtKey)
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
            string invalidLabel = new string('a', length);

            DeleteCredentialCommand cmd = new DeleteCredentialCommand(_mgmtKey);

            _ = Assert.ThrowsAny<ArgumentException>(() => cmd.Label = invalidLabel);
        }

        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey);

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x02()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0x02, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsMgmtKeyTag()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x7b)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(0x7b, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsMgmtKeyValue()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x7b)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            byte[] value = reader.ReadValue(tag).ToArray();

            Assert.Equal(_mgmtKey, value);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsLabelTag()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x71)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(0x71, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsLabelValue()
        {
            DeleteCredentialCommand command =
                new DeleteCredentialCommand(_mgmtKey, _label);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x71)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            string value = reader.ReadString(tag, Encoding.UTF8);

            Assert.Equal(_label, value);
        }
    }
}

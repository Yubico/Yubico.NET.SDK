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
using Yubico.Core.Iso7816;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class GetAes128SessionKeysCommandTests
    {
        private const string _label = "abc";

        private static readonly byte[] _password =
            new byte[16] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        private static readonly byte[] _hostChallenge =
            new byte[8] { 0, 2, 4, 6, 8, 10, 12, 14 };

        private static readonly byte[] _hsmDeviceChallenge =
            new byte[8] { 1, 3, 5, 7, 9, 11, 13, 15 };

        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Theory]
        [InlineData(CredentialWithSecrets.RequiredCredentialPasswordLength - 1)]
        [InlineData(CredentialWithSecrets.RequiredCredentialPasswordLength + 1)]
        public void Constructor_GivenInvalidCredentialPasswordLength_ThrowsArgumentException(int passwordLength)
        {
            Action action = () => new GetAes128SessionKeysCommand(
                _label,
                new byte[passwordLength],
                _hostChallenge,
                _hsmDeviceChallenge);

            _ = Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(GetAes128SessionKeysCommand.RequiredChallengeLength - 1)]
        [InlineData(GetAes128SessionKeysCommand.RequiredChallengeLength + 1)]
        public void Constructor_GivenInvalidHostChallengeLength_ThrowsArgumentException(int hostChallengeLength)
        {
            Action action = () => new GetAes128SessionKeysCommand(
                _label,
                _password,
                new byte[hostChallengeLength],
                _hsmDeviceChallenge);

            _ = Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(GetAes128SessionKeysCommand.RequiredChallengeLength - 1)]
        [InlineData(GetAes128SessionKeysCommand.RequiredChallengeLength + 1)]
        public void Constructor_GivenInvalidHsmDeviceChallengeLength_ThrowsArgumentException(
            int hsmDeviceChallengeLength)
        {
            Action action = () => new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                new byte[hsmDeviceChallengeLength]);

            _ = Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);

            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x03()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);

            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0x03, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);

            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);

            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsLabelTag()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
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
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
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

        [Fact]
        public void CreateCommandApdu_DataContainsContextTag()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x77)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(0x77, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsContextLength16()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x77)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            byte[] value = reader.ReadValue(tag).ToArray();

            Assert.Equal(16, value.Length);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsContextValue()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x77)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            byte[] value = reader.ReadValue(tag).ToArray();
            byte[] hostChallenge = value[0..8];
            byte[] hsmDeviceChallenge = value[8..16];

            Assert.Equal(_hostChallenge, hostChallenge);
            Assert.Equal(_hsmDeviceChallenge, hsmDeviceChallenge);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsCredPasswordTag()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x73)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            Assert.Equal(0x73, tag);
        }

        [Fact]
        public void CreateCommandApdu_DataContainsCredPasswordValue()
        {
            GetAes128SessionKeysCommand command = new GetAes128SessionKeysCommand(
                _label,
                _password,
                _hostChallenge,
                _hsmDeviceChallenge);
            CommandApdu apdu = command.CreateCommandApdu();

            TlvReader reader = new TlvReader(apdu.Data);
            int tag = reader.PeekTag();
            while (reader.HasData && tag != 0x73)
            {
                _ = reader.ReadValue(tag);
                tag = reader.PeekTag();
            }

            byte[] password = reader.ReadValue(tag).ToArray();

            Assert.Equal(_password, password);
        }
    }
}

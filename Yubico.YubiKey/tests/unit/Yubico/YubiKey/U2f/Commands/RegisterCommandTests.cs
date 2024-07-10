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
using Xunit;
using Yubico.Core.Buffers;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class RegisterCommandTests
    {
        private const string clientDataHashHex = "000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F";

        private const string appIdHex = "000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F";

        private static RegisterCommand GetRegisterCommand()
        {
            return new RegisterCommand(
                Hex.HexToBytes(appIdHex),
                Hex.HexToBytes(clientDataHashHex)
            );
        }

        [Fact]
        public void SetClientDataHash_GivenIncorrectLengthData_ThrowsArgumentException()
        {
            var command = GetRegisterCommand();

            _ = Assert.Throws<ArgumentException>(() => command.ClientDataHash = new byte[16]);
        }

        [Fact]
        public void SetAppId_GivenIncorrectLengthData_ThrowsArgumentException()
        {
            var command = GetRegisterCommand();

            _ = Assert.Throws<ArgumentException>(() => command.ApplicationId = new byte[16]);
        }

        [Fact]
        public void SetClientDataHash_GivenData_SetsDataInPayload()
        {
            var clientDataHashHex = "000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F";
            var command = GetRegisterCommand();
            command.ClientDataHash = Hex.HexToBytes(clientDataHashHex);

            Assert.Equal(clientDataHashHex,
                Hex.BytesToHex(command.CreateCommandApdu().Data.Slice(start: 7, length: 32).ToArray()));
        }

        [Fact]
        public void SetAppId_GivenData_SetsDataInPayload()
        {
            var appIdHex = "000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F";
            var command = GetRegisterCommand();
            command.ApplicationId = Hex.HexToBytes(appIdHex);

            Assert.Equal(appIdHex,
                Hex.BytesToHex(command.CreateCommandApdu().Data.Slice(start: 39, length: 32).ToArray()));
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsInsToHex03()
        {
            var command = GetRegisterCommand();

            Assert.Equal(expected: 0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_DataHasLength39()
        {
            var command = GetRegisterCommand();

            Assert.Equal(expected: 71, command.CreateCommandApdu().Data.Length);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduClassTo0()
        {
            var command = GetRegisterCommand();

            Assert.Equal(expected: 0x00, command.CreateCommandApdu().Data.Span[index: 0]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fCommandToHex01()
        {
            var command = GetRegisterCommand();

            Assert.Equal(expected: 0x01, command.CreateCommandApdu().Data.Span[index: 1]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduP1To0()
        {
            var command = GetRegisterCommand();

            Assert.Equal(expected: 0x00, command.CreateCommandApdu().Data.Span[index: 2]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduP2To0()
        {
            var command = GetRegisterCommand();

            Assert.Equal(expected: 0x00, command.CreateCommandApdu().Data.Span[index: 3]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduLengthTo64()
        {
            var command = GetRegisterCommand();
            var data = command.CreateCommandApdu().Data;
            var dataSlice = data.Slice(start: 4, length: 3).Span;

            Assert.True(dataSlice.SequenceEqual(new byte[] { 0x00, 0x00, 0x40 }));
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x69, 0x85 });
            var command = GetRegisterCommand();

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<RegisterResponse>(response);
        }
    }
}

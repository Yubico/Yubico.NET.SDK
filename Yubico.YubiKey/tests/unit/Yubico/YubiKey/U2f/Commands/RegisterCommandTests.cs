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

        private static RegisterCommand GetRegisterCommand() => new RegisterCommand(
            Hex.HexToBytes(appIdHex),
            Hex.HexToBytes(clientDataHashHex)
        );

        [Fact]
        public void SetClientDataHash_GivenIncorrectLengthData_ThrowsArgumentException()
        {
            RegisterCommand command = GetRegisterCommand();

            _ = Assert.Throws<ArgumentException>(() => command.ClientDataHash = new byte[16]);
        }

        [Fact]
        public void SetAppId_GivenIncorrectLengthData_ThrowsArgumentException()
        {
            RegisterCommand command = GetRegisterCommand();

            _ = Assert.Throws<ArgumentException>(() => command.ApplicationId = new byte[16]);
        }

        [Fact]
        public void SetClientDataHash_GivenData_SetsDataInPayload()
        {
            string clientDataHashHex = "000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F";
            RegisterCommand command = GetRegisterCommand();
            command.ClientDataHash = Hex.HexToBytes(clientDataHashHex);

            Assert.Equal(clientDataHashHex, Hex.BytesToHex(command.CreateCommandApdu().Data.Slice(7, 32).ToArray()));
        }

        [Fact]
        public void SetAppId_GivenData_SetsDataInPayload()
        {
            string appIdHex = "000102030405060708090A0B0C0D0E0F000102030405060708090A0B0C0D0E0F";
            RegisterCommand command = GetRegisterCommand();
            command.ApplicationId = Hex.HexToBytes(appIdHex);

            Assert.Equal(appIdHex, Hex.BytesToHex(command.CreateCommandApdu().Data.Slice(39, 32).ToArray()));
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsInsToHex03()
        {
            RegisterCommand command = GetRegisterCommand();

            Assert.Equal(0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_DataHasLength39()
        {
            RegisterCommand command = GetRegisterCommand();

            Assert.Equal(71, command.CreateCommandApdu().Data.Length);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduClassTo0()
        {
            RegisterCommand command = GetRegisterCommand();

            Assert.Equal(0x00, command.CreateCommandApdu().Data.Span[0]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fCommandToHex01()
        {
            RegisterCommand command = GetRegisterCommand();

            Assert.Equal(0x01, command.CreateCommandApdu().Data.Span[1]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduP1To0()
        {
            RegisterCommand command = GetRegisterCommand();

            Assert.Equal(0x00, command.CreateCommandApdu().Data.Span[2]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduP2To0()
        {
            RegisterCommand command = GetRegisterCommand();

            Assert.Equal(0x00, command.CreateCommandApdu().Data.Span[3]);
        }

        [Fact]
        public void CreateCommandApdu_GivenSetup_SetsU2fSubApduLengthTo64()
        {
            RegisterCommand command = GetRegisterCommand();
            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;
            ReadOnlySpan<byte> dataSlice = data.Slice(4, 3).Span;

            Assert.True(dataSlice.SequenceEqual(new byte[] { 0x00, 0x00, 0x40 }));
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x69, 0x85 });
            RegisterCommand command = GetRegisterCommand();

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<RegisterResponse>(response);
        }
    }
}

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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    public class ReadNdefDataCommandTests
    {
        [Fact]
        public void Application_Get_AlwaysEqualsOtpNdef()
        {
            var command = new ReadNdefDataCommand();

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.OtpNdef, application);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new ReadNdefDataCommand();

            byte cla = command.CreateCommandApdu().Cla;

            Assert.Equal(0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexB0()
        {
            var command = new ReadNdefDataCommand();

            byte ins = command.CreateCommandApdu().Ins;

            Assert.Equal(0xB0, ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new ReadNdefDataCommand();

            byte p1 = command.CreateCommandApdu().P1;

            Assert.Equal(0, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new ReadNdefDataCommand();

            byte p2 = command.CreateCommandApdu().P2;

            Assert.Equal(0, p2);
        }

        [Fact]
        public void CreateCommandData_GetData_ReturnsEmpty()
        {
            var command = new ReadNdefDataCommand();

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void CreateCommandData_GetNc_ReturnsZero()
        {
            var command = new ReadNdefDataCommand();

            int nc = command.CreateCommandApdu().Nc;

            Assert.Equal(0, nc);
        }

        [Fact]
        public void CreateCommandData_GetNe_ReturnsZero()
        {
            var command = new ReadNdefDataCommand();

            int ne = command.CreateCommandApdu().Ne;

            Assert.Equal(0, ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new ReadNdefDataCommand();

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<ReadNdefDataResponse>(response);
        }
    }
}

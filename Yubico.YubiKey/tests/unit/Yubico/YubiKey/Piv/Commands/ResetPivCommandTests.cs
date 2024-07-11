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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class ResetPivCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var resetPivCommand = new ResetPivCommand();

            Assert.True(resetPivCommand is IYubiKeyCommand<ResetPivResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new ResetPivCommand();

            var application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var Cla = cmdApdu.Cla;

            Assert.Equal(expected: 0, Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexFB()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var Ins = cmdApdu.Ins;

            Assert.Equal(expected: 0xFB, Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var P1 = cmdApdu.P1;

            Assert.Equal(expected: 0, P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var P2 = cmdApdu.P2;

            Assert.Equal(expected: 0, P2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ReturnsEmpty()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var data = cmdApdu.Data;

            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_ReturnsZero()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var Nc = cmdApdu.Nc;

            Assert.Equal(expected: 0, Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            var cmdApdu = GetResetPivCommandApdu();

            var Ne = cmdApdu.Ne;

            Assert.Equal(expected: 0, Ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var sw1 = unchecked((byte)(SWConstants.Success >> 8));
            var sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new[] { sw1, sw2 });
            var resetPivCommand = new ResetPivCommand();

            var resetPivResponse = resetPivCommand.CreateResponseForApdu(responseApdu);

            Assert.True(resetPivResponse is ResetPivResponse);
        }

        private static CommandApdu GetResetPivCommandApdu()
        {
            var resetPivCommand = new ResetPivCommand();
            return resetPivCommand.CreateCommandApdu();
        }
    }
}

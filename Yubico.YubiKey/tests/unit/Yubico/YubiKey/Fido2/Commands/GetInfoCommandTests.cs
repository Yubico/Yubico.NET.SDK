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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetInfoCommandTests
    {
        private static CommandApdu GetGetInfoCommandApdu() => new GetInfoCommand().CreateCommandApdu();

        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var GetInfoCommand = new GetInfoCommand();

            _ = Assert.IsAssignableFrom< IYubiKeyCommand<GetInfoResponse>>(GetInfoCommand);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            Assert.Equal(0, GetGetInfoCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            Assert.Equal(0, GetGetInfoCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            Assert.Equal(0, GetGetInfoCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ReturnsLength1()
        {
            Assert.Equal(1, GetGetInfoCommandApdu().Data.Length);
        }

        [Fact]
        public void CreateCommandApdu_GetData_InsIsHex10()
        {
            Assert.Equal(0x10, GetGetInfoCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetData_FirstByteIsHex04()
        {
            Assert.Equal(0x04, GetGetInfoCommandApdu().Data.Span[0]);
        }
    }
}

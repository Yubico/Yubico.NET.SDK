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
using Yubico.Core.Iso7816;
using Xunit;
using Yubico.Core.Tlv;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class GetManagementKeyRetriesCommandTests
    {
        private GetManagementKeyRetriesCommand _command =>
            new GetManagementKeyRetriesCommand();

        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            GetManagementKeyRetriesCommand command =
                new GetManagementKeyRetriesCommand();

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            GetManagementKeyRetriesCommand command =
                new GetManagementKeyRetriesCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x09()
        {
            GetManagementKeyRetriesCommand command =
                new GetManagementKeyRetriesCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0x09, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0()
        {
            GetManagementKeyRetriesCommand command =
                new GetManagementKeyRetriesCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0()
        {
            GetManagementKeyRetriesCommand command =
                new GetManagementKeyRetriesCommand();
            CommandApdu apdu = command.CreateCommandApdu();

            Assert.Equal(0, apdu.P2);
        }
    }
}

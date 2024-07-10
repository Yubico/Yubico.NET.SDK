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

using Xunit;

namespace Yubico.YubiKey.YubiHsmAuth.Commands
{
    public class GetApplicationVersionCommandTests
    {
        [Fact]
        public void Application_Get_ReturnsYubiHsmAuth()
        {
            var command = new GetApplicationVersionCommand();

            Assert.Equal(YubiKeyApplication.YubiHsmAuth, command.Application);
        }

        [Fact]
        public void CreateCommandApdu_Cla0()
        {
            var command = new GetApplicationVersionCommand();
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.Cla);
        }

        [Fact]
        public void CreateCommandApdu_Ins0x07()
        {
            var command = new GetApplicationVersionCommand();
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0x07, apdu.Ins);
        }

        [Fact]
        public void CreateCommandApdu_P1Is0()
        {
            var command = new GetApplicationVersionCommand();
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.P1);
        }

        [Fact]
        public void CreateCommandApdu_P2Is0()
        {
            var command = new GetApplicationVersionCommand();
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.P2);
        }

        [Fact]
        public void CreateCommandApdu_DataLength0()
        {
            var command = new GetApplicationVersionCommand();
            var apdu = command.CreateCommandApdu();

            Assert.Equal(expected: 0, apdu.Data.Length);
        }
    }
}

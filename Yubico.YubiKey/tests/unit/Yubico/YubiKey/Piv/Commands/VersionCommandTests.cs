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
    public class VersionCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var versionCommand = new VersionCommand();

            Assert.True(versionCommand is IYubiKeyCommand<VersionResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var versionCommand = new VersionCommand();

            YubiKeyApplication application = versionCommand.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            Assert.Equal(0, GetVersionCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexFD()
        {
            Assert.Equal(0xFD, GetVersionCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            Assert.Equal(0, GetVersionCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            Assert.Equal(0, GetVersionCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ReturnsEmpty()
        {
            Assert.True(GetVersionCommandApdu().Data.IsEmpty);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_ReturnsZero()
        {
            Assert.Equal(0, GetVersionCommandApdu().Nc);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            Assert.Equal(0, GetVersionCommandApdu().Ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { 0x01, 0x02, 0x04, 0x90, 0x00 });
            var versionCommand = new VersionCommand();

            // Act
            var versionResponse = versionCommand.CreateResponseForApdu(responseApdu);

            // Assert
            Assert.True(versionResponse is VersionResponse);
        }

        private static CommandApdu GetVersionCommandApdu()
        {
            var versionCommand = new VersionCommand();
            return versionCommand.CreateCommandApdu();
        }
    }
}

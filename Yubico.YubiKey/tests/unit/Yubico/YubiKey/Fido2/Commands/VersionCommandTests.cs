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
using Moq;
using System.Security.Cryptography;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class VersionCommandTests
    {
        private static CommandApdu GetVersionCommandApdu() => new VersionCommand().CreateCommandApdu();

        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            var versionCommand = new VersionCommand();

            _ = Assert.IsAssignableFrom<IYubiKeyCommand<VersionResponse>>(versionCommand);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            Assert.Equal(0, GetVersionCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex06()
        {
            Assert.Equal(0x06, GetVersionCommandApdu().Ins);
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
        public void CreateCommandApdu_GetData_ReturnsLength8()
        {
            Assert.Equal(8, GetVersionCommandApdu().Data.Length);
        }

        [Fact]
        public void CreateCommandApdu_GivenRng_GetDataReturnsNonceFromRng()
        {
            var mock = new Mock<RandomNumberGenerator>();
            _ = mock
                .Setup(rng => rng.GetBytes(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback((byte[] a, int idx, int length) => {
                    for(int i = idx; i < idx + length; i++)
                    {
                        a[i] = 0xff;
                    }
                });

            var versionCommand = new VersionCommand(mock.Object);

            ReadOnlyMemory<byte> data = versionCommand.CreateCommandApdu().Data;

            Assert.True(data.Span.SequenceEqual(Hex.HexToBytes("ffffffffffffffff")));
        }
    }
}

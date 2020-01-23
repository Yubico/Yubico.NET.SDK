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
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Oath.Commands
{
    public class CalculateAllCredentialsCommandTests
    {
        readonly byte[] _fixedBytes = new byte[8] { 0xF1, 0x03, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85 };

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new CalculateAllCredentialsCommand();

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0xa4()
        {
            var command = new CalculateAllCredentialsCommand();

            Assert.Equal(0xa4, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new CalculateAllCredentialsCommand();

            Assert.Equal(0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new CalculateAllCredentialsCommand(ResponseFormat.Full);

            Assert.Equal(0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_Returns0x01()
        {
            var command = new CalculateAllCredentialsCommand(ResponseFormat.Truncated);

            Assert.Equal(0x01, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            var command = new CalculateAllCredentialsCommand();

            Assert.Equal(0, command.CreateCommandApdu().Ne);
        }

        [Fact]
        public void CreateCommandApdu_ReturnsCorrectLength()
        {
            RandomObjectUtility utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

            try
            {
                var command = new CalculateAllCredentialsCommand(ResponseFormat.Full);

                byte[] dataList = { 0x74, 0x08, 0xF1, 0x03, 0xDA, 0x89, 0x01, 0x02, 0x03, 0x04 };

                Assert.Equal(dataList.Length, command.CreateCommandApdu().Nc);
            }
            finally
            {
                utility.RestoreRandomProvider();
            }
        }

        [Fact]
        public void CreateCommandApdu_ReturnsCorrectData()
        {
            RandomObjectUtility utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

            try
            {
                var command = new CalculateAllCredentialsCommand(ResponseFormat.Full);

                byte[] dataList = { 0x74, 0x08, 0xF1, 0x03, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85 };

                ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

                Assert.True(data.Span.SequenceEqual(dataList));
            }
            finally
            {
                utility.RestoreRandomProvider();
            }
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new CalculateAllCredentialsCommand();
            var response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is CalculateAllCredentialsResponse);
        }
    }
}

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
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.YubiKey.TestUtilities;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using System.Buffers.Binary;

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

                byte[] dataList = { 0x74, 0x08 };
                
                int timePeriod = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (int)CredentialPeriod.Period30;
                byte[] bytes = BitConverter.GetBytes(timePeriod);
                byte[] challenge = bytes.Concat(new byte[8 - bytes.Length]).ToArray();
                var newDataList = dataList.Concat(challenge).ToArray();
                
                Assert.Equal(newDataList.Length, command.CreateCommandApdu().Nc);
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

                byte[] dataList = { 0x74, 0x08 };

                ulong timePeriod = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (uint)CredentialPeriod.Period30;
                byte[] bytes = new byte[8];
                BinaryPrimitives.WriteUInt64BigEndian(bytes, timePeriod);
                var newDataList = dataList.Concat(bytes).ToArray();
                
                ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

                Assert.True(data.Span.SequenceEqual(newDataList));
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

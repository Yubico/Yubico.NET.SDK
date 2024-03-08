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
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath.Commands
{
    public class CalculateCredentialCommandTests
    {
        readonly Credential _credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Totp, CredentialPeriod.Period30);
        readonly byte[] _fixedBytes = new byte[8] { 0xF1, 0x03, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85 };

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0xa2()
        {
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

            Assert.Equal(0xa2, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

            Assert.Equal(0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

            Assert.Equal(0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_Returns0x01()
        {
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Truncated);

            Assert.Equal(0x01, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNe_ReturnsZero()
        {
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

            Assert.Equal(0, command.CreateCommandApdu().Ne);
        }

        [Fact]
        public void CreateCommandApdu_TotpCredential_ReturnsCorrectLength()
        {
            RandomObjectUtility utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

            try
            {
                var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

                byte[] dataList =
                {
                    0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                    0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75, 0x74,
                    0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x74, 0x08
                };

                byte[] challenge = GenerateChallenge(_credential.Period);
                byte[]? newDataList = dataList.Concat(challenge).ToArray();
                ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

                Assert.Equal(newDataList.Length, command.CreateCommandApdu().Nc);
            }
            finally
            {
                utility.RestoreRandomProvider();
            }
        }

        [Fact]
        public void CreateCommandApdu_TotpCredential_ReturnsCorrectData()
        {
            RandomObjectUtility utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

            try
            {
                var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);

                byte[] dataList =
                {
                    0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                    0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75, 0x74,
                    0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x74, 0x08,
                };

                byte[] challenge = GenerateChallenge(_credential.Period);
                byte[]? newDataList = dataList.Concat(challenge).ToArray();
                ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

                Assert.True(data.Span.SequenceEqual(newDataList));
            }
            finally
            {
                utility.RestoreRandomProvider();
            }
        }

        [Fact]
        public void CreateCommandApdu_HotpCredential_ReturnsCorrectDataAndLength()
        {
            var hotpCredential = new Credential("Apple", "test@icloud.com", CredentialType.Hotp, CredentialPeriod.Undefined);
            var command = new CalculateCredentialCommand(hotpCredential, ResponseFormat.Full);

            byte[] dataList =
            {
                0x71, 0x15, 0x41, 0x70, 0x70, 0x6C, 0x65, 0x3A,
                0x74, 0x65, 0x73, 0x74, 0x40, 0x69, 0x63, 0x6C,
                0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0X6D, 0x74,
                0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(dataList.Length, command.CreateCommandApdu().Nc);
            Assert.True(data.Span.SequenceEqual(dataList));
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new CalculateCredentialCommand(_credential, ResponseFormat.Full);
            CalculateCredentialResponse? response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is CalculateCredentialResponse);
        }

        private byte[] GenerateChallenge(CredentialPeriod? period)
        {
            if (period is null)
            {
                period = CredentialPeriod.Period30;
            }

            ulong timePeriod = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (uint)period;
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(bytes, timePeriod);

            return bytes;
        }
    }
}

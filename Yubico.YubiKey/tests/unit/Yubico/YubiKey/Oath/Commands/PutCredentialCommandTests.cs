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

namespace Yubico.YubiKey.Oath.Commands
{
    public class PutCredentialCommandTests
    {
        readonly Credential credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Totp,
            HashAlgorithm.Sha1, "tt", CredentialPeriod.Period30, 6, 0, false);

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new PutCommand { Credential = credential };

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x01()
        {
            var command = new PutCommand { Credential = credential };

            Assert.Equal(0x01, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var command = new PutCommand { Credential = credential };

            Assert.Equal(0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new PutCommand { Credential = credential };

            Assert.Equal(0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetDataAndNcProperties_ReturnsCorrectDataAndLength()
        {
            var command = new PutCommand { Credential = credential };
            byte[] dataList =
            {
                0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75, 0x74,
                0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x73, 0x10,
                0x21, 0x06, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(dataList.Length, command.CreateCommandApdu().Nc);
            Assert.True(data.Span.SequenceEqual(dataList));
        }

        [Fact]
        public void CreateCommandApdu_GetDataAndNcProperties_WithTouchRequiredProperty_ReturnsCorrectDataAndLength()
        {
            var credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Totp, HashAlgorithm.Sha1,
                "tt", CredentialPeriod.Period30, 6, 0, true);
            var command = new PutCommand { Credential = credential };
            byte[] dataList =
            {
                0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75, 0x74,
                0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x73, 0x10,
                0x21, 0x06, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78, 0x02
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(dataList.Length, command.CreateCommandApdu().Nc);
            Assert.True(data.Span.SequenceEqual(dataList));
        }

        [Fact]
        public void
            CreateCommandApdu_GetDataAndNcProperties_CredentialWithHotpTypeAndTouchRequiredProperty_ReturnsCorrectDataAndLength()
        {
            var credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Hotp, HashAlgorithm.Sha1,
                "tt", CredentialPeriod.Undefined, 6, 4, true);
            var command = new PutCommand { Credential = credential };
            byte[] dataList =
            {
                0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75, 0x74,
                0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x73, 0x10,
                0x11, 0x06, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78, 0x02, 0x7A, 0x04,
                0x04, 0x00, 0x00, 0x00
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(dataList.Length, command.CreateCommandApdu().Nc);
            Assert.True(data.Span.SequenceEqual(dataList));
        }

        [Fact]
        public void CreateCommandApdu_GetDataAndNcProperties_CredentialWithHotpType_ReturnsCorrectDataAndLength()
        {
            var credential = new Credential("Microsoft", "test@outlook.com", CredentialType.Hotp, HashAlgorithm.Sha1,
                "tt", CredentialPeriod.Undefined, 6, 4, false);
            var command = new PutCommand { Credential = credential };
            byte[] dataList =
            {
                0x71, 0x1A, 0x4D, 0x69, 0x63, 0x72, 0x6F, 0x73, 0x6F, 0x66,
                0x74, 0x3A, 0x74, 0x65, 0x73, 0x74, 0x40, 0x6F, 0x75, 0x74,
                0x6C, 0x6F, 0x6F, 0x6B, 0x2E, 0x63, 0x6F, 0x6D, 0x73, 0x10,
                0x11, 0x06, 0x9C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7A, 0x04, 0x04, 0x00,
                0x00, 0x00
            };

            ReadOnlyMemory<byte> data = command.CreateCommandApdu().Data;

            Assert.Equal(dataList.Length, command.CreateCommandApdu().Nc);
            Assert.True(data.Span.SequenceEqual(dataList));
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new PutCommand { Credential = credential };
            OathResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is OathResponse);
        }
    }
}

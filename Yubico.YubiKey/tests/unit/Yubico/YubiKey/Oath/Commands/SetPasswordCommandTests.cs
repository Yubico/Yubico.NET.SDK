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
using System.Collections.Generic;
using System.Text;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Oath.Commands
{
    public class SetPasswordCommandTests
    {
        readonly byte[] _fixedBytes = new byte[8] { 0xF1, 0x03, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85 };
        private readonly byte[] _password = Encoding.UTF8.GetBytes("test");
        const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
        const byte sw2 = unchecked((byte)SWConstants.Success);

        private readonly ResponseApdu selectResponseApdu = new ResponseApdu(new byte[] {
            0x79, 0x03, 0x05, 0x02, 0x04, 0x71, 0x08, 0xC0, 0xE3, 0xAF,
            0x27, 0xCC, 0x7A, 0x20, 0xEE, sw1, sw2
        });

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();

            var command = new SetPasswordCommand(_password, oathData);

            Assert.Equal(0, command.CreateCommandApdu().Cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x03()
        {
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();

            var command = new SetPasswordCommand(_password, oathData);

            Assert.Equal(0x03, command.CreateCommandApdu().Ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsZero()
        {
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();

            var command = new SetPasswordCommand(_password, oathData);

            Assert.Equal(0, command.CreateCommandApdu().P1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();

            var command = new SetPasswordCommand(_password, oathData);

            Assert.Equal(0, command.CreateCommandApdu().P2);
        }

        [Fact]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrectLengthOfData()
        {
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();
            var utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

            try
            {
                var command = new SetPasswordCommand(_password, oathData);

                var dataList = new List<byte>
                {
                    0x73, 0x11, 0x01, 0x78, 0x0E, 0x45, 0xA0, 0x06, 0x52, 0xCC,
                    0xB0, 0x8C, 0x4B, 0xDA, 0xCD, 0xDA, 0xCA, 0x51, 0x34, 0x74,
                    0x08, 0xF1, 0x03, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85, 0x75,
                    0x14, 0x01, 0x1E, 0xE1, 0xFF, 0x2A, 0x98, 0x2D, 0x4D, 0xCC,
                    0xCD, 0x8E, 0xB3, 0x3A, 0x12, 0xE4, 0x88, 0x7E, 0xF5, 0xE0,
                    0x0C
                };

                Assert.Equal(dataList.Count, command.CreateCommandApdu().Nc);
            }
            finally
            {
                utility.RestoreRandomProvider();
            }
        }

        [Fact]
        public void CreateCommandApdu_GetDataProperty_ReturnsCorrectData()
        {
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();
            var utility = RandomObjectUtility.SetRandomProviderFixedBytes(_fixedBytes);

            try
            {
                var command = new SetPasswordCommand(_password, oathData);

                byte[] dataList =
                {
                    0x73, 0x11, 0x01, 0x78, 0x0E, 0x45, 0xA0, 0x06, 0x52, 0xCC,
                    0xB0, 0x8C, 0x4B, 0xDA, 0xCD, 0xDA, 0xCA, 0x51, 0x34, 0x74,
                    0x08, 0xF1, 0x03, 0xDA, 0x89, 0x58, 0xE4, 0x40, 0x85, 0x75,
                    0x14, 0x01, 0x1E, 0xE1, 0xFF, 0x2A, 0x98, 0x2D, 0x4D, 0xCC,
                    0xCD, 0x8E, 0xB3, 0x3A, 0x12, 0xE4, 0x88, 0x7E, 0xF5, 0xE0,
                    0x0C
                };

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
            var selectOathResponse = new SelectOathResponse(selectResponseApdu);
            OathApplicationData oathData = selectOathResponse.GetData();
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new SetPasswordCommand(_password, oathData);
            SetPasswordResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is SetPasswordResponse);
        }
    }
}

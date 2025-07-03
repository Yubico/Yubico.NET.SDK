// Copyright 2025 Yubico AB
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

namespace Yubico.YubiKey.U2f.Commands
{
    public class GetProtocolVersionResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullException()
        {
#nullable disable
            static void action() => _ = new GetProtocolVersionResponse(null);
#nullable enable

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new GetProtocolVersionResponse(responseApdu);

            Assert.Equal(SWConstants.Success, registerResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new GetProtocolVersionResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, registerResponse.Status);
        }

        [Fact]
        public void Constructor_ConditionsNotSatisfiedResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.InsNotSupported >> 8));
            byte sw2 = unchecked((byte)SWConstants.InsNotSupported);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new GetProtocolVersionResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, registerResponse.Status);
        }

        [Fact]
        public void GetData_EmptyResponseData_ReturnsEmptyString()
        {
            string expectedData = string.Empty;

            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new GetProtocolVersionResponse(responseApdu);
            string actualData = response.GetData();

            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void GetData_NonEmptyResponseData_ReturnsCorrectString()
        {
            var commandResponseData = new List<byte>();

            string expectedString = "ABCD";

            byte[] data = Encoding.ASCII.GetBytes(expectedString);
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);

            commandResponseData.AddRange(data);
            commandResponseData.Add(sw1);
            commandResponseData.Add(sw2);

            var responseApdu = new ResponseApdu(commandResponseData.ToArray());

            var response = new GetProtocolVersionResponse(responseApdu);
            string actualString = response.GetData();

            Assert.Equal(expectedString, actualString);
        }

        [Fact]
        public void GetData_ResponseApduFailed_ThrowsException()
        {
            byte sw1 = unchecked((byte)(SWConstants.InsNotSupported >> 8));
            byte sw2 = unchecked((byte)SWConstants.InsNotSupported);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new GetProtocolVersionResponse(responseApdu);
            void action() => response.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }
    }
}

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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.U2f.Commands
{
    public class EchoResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#nullable disable
            static void action() => _ = new EchoResponse(null);
#nullable enable

            _ = Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new EchoResponse(responseApdu);

            Assert.Equal(SWConstants.Success, registerResponse.StatusWord);
        }

        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new EchoResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, registerResponse.Status);
        }

        [Fact]
        public void Constructor_ConditionsNotSatisfiedResponseApdu_SetsStatusCorrectly()
        {
            byte sw1 = unchecked((byte)(SWConstants.ConditionsNotSatisfied >> 8));
            byte sw2 = unchecked((byte)SWConstants.ConditionsNotSatisfied);
            var responseApdu = new ResponseApdu(new byte[] { 0, 0, 0, sw1, sw2 });

            var registerResponse = new EchoResponse(responseApdu);

            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, registerResponse.Status);
        }

        [Fact]
        public void ThrowIfFailed_ResponseApduSucceeded_NoExceptionThrown()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new EchoResponse(responseApdu);
            void action() => response.GetData();

            Exception? ex = Record.Exception(action);
            Assert.Null(ex);
        }

        [Fact]
        public void ThrowIfFailed_ResponseApduFailed_ThrowsException()
        {
            byte sw1 = unchecked((byte)(SWConstants.FunctionError >> 8));
            byte sw2 = unchecked((byte)SWConstants.FunctionError);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new EchoResponse(responseApdu);
            void action() => response.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void GetData_EmptyResponseData_ReturnsEmptyArray()
        {
            ReadOnlyMemory<byte> expectedData = ReadOnlyMemory<byte>.Empty;

            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new EchoResponse(responseApdu);
            ReadOnlyMemory<byte> actualData = response.GetData();

            Assert.True(actualData.Span.SequenceEqual(expectedData.Span));
        }

        [Fact]
        public void GetData_NonEmptyResponseData_ReturnsCorrectArray()
        {
            var commandResponseData = new List<byte>();

            var expectedData = new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03 });
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);

            commandResponseData.AddRange(expectedData.ToArray());
            commandResponseData.Add(sw1);
            commandResponseData.Add(sw2);

            var responseApdu = new ResponseApdu(commandResponseData.ToArray());

            var response = new EchoResponse(responseApdu);
            ReadOnlyMemory<byte> actualData = response.GetData();

            Assert.True(actualData.Span.SequenceEqual(expectedData.Span));
        }

        [Fact]
        public void GetData_ResponseApduFailed_ThrowsException()
        {
            byte sw1 = unchecked((byte)(SWConstants.FunctionError >> 8));
            byte sw2 = unchecked((byte)SWConstants.FunctionError);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var response = new EchoResponse(responseApdu);
            void action() => response.GetData();

            _ = Assert.Throws<InvalidOperationException>(action);
        }
    }
}

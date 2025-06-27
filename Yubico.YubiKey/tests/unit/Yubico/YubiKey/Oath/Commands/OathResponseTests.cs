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

using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Oath.Commands
{
    public class OathResponseTests
    {
        [Fact]
        public void Constructor_SuccessResponseApdu_SetsStatusWordCorrectly()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var oathResponse = new OathResponse(responseApdu);

            Assert.Equal(SWConstants.Success, oathResponse.StatusWord);
        }

        [Fact]
        public void Status_SuccessResponseApdu_ReturnsSuccess()
        {
            const byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            const byte sw2 = unchecked((byte)SWConstants.Success);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var oathResponse = new OathResponse(responseApdu);

            Assert.Equal(ResponseStatus.Success, oathResponse.Status);
        }

        [Fact]
        public void Status_GenericErrorResponseApdu_ReturnsFailed()
        {
            const byte sw1 = unchecked((byte)(OathSWConstants.GenericError >> 8));
            const byte sw2 = unchecked((byte)OathSWConstants.GenericError);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var oathResponse = new OathResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, oathResponse.Status);
        }

        [Fact]
        public void Status_WrongSyntaxResponseApdu_ReturnsFailed()
        {
            const byte sw1 = unchecked((byte)(OathSWConstants.WrongSyntax >> 8));
            const byte sw2 = unchecked((byte)OathSWConstants.WrongSyntax);

            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            var oathResponse = new OathResponse(responseApdu);

            Assert.Equal(ResponseStatus.Failed, oathResponse.Status);
        }
    }
}

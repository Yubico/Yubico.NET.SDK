// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.Fido2.Commands
{
    public class ResponseMapTests
    {
        [Fact]
        public void Response_6F30_CorrectStatus()
        {
            byte[] response = new byte[] { 0x6F, 0x30 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            Assert.Equal(ResponseStatus.Failed, rsp.Status);
        }

        [Fact]
        public void Response_6F30_CorrectStatusMessage()
        {
            byte[] response = new byte[] { 0x6F, 0x30 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            int isEqual = string.Compare(rsp.StatusMessage, ResponseStatusMessages.Fido2NotAllowed, StringComparison.Ordinal);
            Assert.True(isEqual == 0);
        }

        [Fact]
        public void Response_6F31_CorrectStatus()
        {
            byte[] response = new byte[] { 0x6F, 0x31 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            Assert.Equal(ResponseStatus.ConditionsNotSatisfied, rsp.Status);
        }

        [Fact]
        public void Response_6F31_CorrectStatusMessage()
        {
            byte[] response = new byte[] { 0x6F, 0x31 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            int isEqual = string.Compare(rsp.StatusMessage, ResponseStatusMessages.Fido2PinNotVerified, StringComparison.Ordinal);
            Assert.True(isEqual == 0);
        }

        [Fact]
        public void Response_6F32_CorrectStatus()
        {
            byte[] response = new byte[] { 0x6F, 0x32 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            Assert.Equal(ResponseStatus.Failed, rsp.Status);
        }

        [Fact]
        public void Response_6F32_CorrectStatusMessage()
        {
            byte[] response = new byte[] { 0x6F, 0x32 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            int isEqual = string.Compare(rsp.StatusMessage, ResponseStatusMessages.Fido2PinBlocked, StringComparison.Ordinal);
            Assert.True(isEqual == 0);
        }

        [Fact]
        public void Response_6F35_CorrectStatus()
        {
            byte[] response = new byte[] { 0x6F, 0x35 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            Assert.Equal(ResponseStatus.Failed, rsp.Status);
        }

        [Fact]
        public void Response_6F35_CorrectStatusMessage()
        {
            byte[] response = new byte[] { 0x6F, 0x35 };

            var rsp = new Fido2Response(new ResponseApdu(response));

            int isEqual = string.Compare(rsp.StatusMessage, ResponseStatusMessages.Fido2PinNotSet, StringComparison.Ordinal);
            Assert.True(isEqual == 0);
        }

        [Fact]
        public void Response_6F3A_CorrectStatus()
        {
            byte[] response = new byte[] { 0x6F, 0x3A };

            var rsp = new Fido2Response(new ResponseApdu(response));

            Assert.Equal(ResponseStatus.Failed, rsp.Status);
        }

        [Fact]
        public void Response_6F3A_CorrectStatusMessage()
        {
            byte[] response = new byte[] { 0x6F, 0x3A };

            var rsp = new Fido2Response(new ResponseApdu(response));

            int isEqual = string.Compare(rsp.StatusMessage, ResponseStatusMessages.Fido2Timeout, StringComparison.Ordinal);
            Assert.True(isEqual == 0);
        }
    }
}

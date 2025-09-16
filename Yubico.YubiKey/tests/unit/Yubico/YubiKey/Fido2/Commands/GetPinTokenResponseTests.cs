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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Fido2.Commands
{
    public class GetPinTokenResponseTests
    {
        [Fact]
        public void Constructor_SuccessApdu_CreatesObject()
        {
            byte[] responseBytes = new byte[] {
                0xA1, 0x02, 0x58, 0x20,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x90, 0x00
            };
            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));

            Assert.NotNull(response);
        }

        [Fact]
        public void WP_Constructor_SuccessApdu_CreatesObject()
        {
            byte[] responseBytes = new byte[] {
                0xA1, 0x02, 0x58, 0x20,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x90, 0x00
            };
            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));

            Assert.NotNull(response);
        }

        [Fact]
        public void Constructor_SuccessApdu_SetsStatus()
        {
            byte[] responseBytes = new byte[] {
                0xA1, 0x02, 0x58, 0x20,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x90, 0x00
            };
            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void WP_Constructor_SuccessApdu_SetsStatus()
        {
            byte[] responseBytes = new byte[] {
                0xA1, 0x02, 0x58, 0x20,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x90, 0x00
            };
            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));

            Assert.Equal(ResponseStatus.Success, response.Status);
        }

        [Fact]
        public void Constructor_ErrorApdu_SetsStatus()
        {
            byte[] responseBytes = new byte[] {
                0x69, 0x00
            };
            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));

            Assert.Equal(ResponseStatus.Failed, response.Status);
        }

        [Fact]
        public void WP_Constructor_ErrorApdu_SetsStatus()
        {
            byte[] responseBytes = new byte[] {
                0x69, 0x00
            };
            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));

            Assert.Equal(ResponseStatus.Failed, response.Status);
        }

        [Fact]
        public void GetData_ReturnsCorrect()
        {
            byte[] responseBytes = new byte[] {
                0xA1, 0x02, 0x58, 0x20,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x90, 0x00
            };
            var expected = new ReadOnlyMemory<byte>(responseBytes);

            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));
            ReadOnlyMemory<byte> token = response.GetData();

            bool isValid = MemoryExtensions.SequenceEqual(expected.Slice(4, 32).Span, token.Span);
            Assert.True(isValid);
        }

        [Fact]
        public void WP_GetData_ReturnsCorrect()
        {
            byte[] responseBytes = new byte[] {
                0xA1, 0x02, 0x58, 0x20,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                0x90, 0x00
            };
            var expected = new ReadOnlyMemory<byte>(responseBytes);

            var response = new GetPinUvAuthTokenResponse(new ResponseApdu(responseBytes));
            ReadOnlyMemory<byte> token = response.GetData();

            bool isValid = MemoryExtensions.SequenceEqual(expected.Slice(4, 32).Span, token.Span);
            Assert.True(isValid);
        }
    }
}

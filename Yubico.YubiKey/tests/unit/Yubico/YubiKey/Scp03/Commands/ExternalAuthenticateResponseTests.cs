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

namespace Yubico.YubiKey.Scp03.Commands
{
    public class ExternalAuthenticateResponseTests
    {
        public static ResponseApdu GetResponseApdu()
        {
            return new ResponseApdu(new byte[] { 0x90, 0x00 });
        }

        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new ExternalAuthenticateResponse(responseApdu: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_GivenResponseApdu_SetsStatusWordCorrectly()
        {
            // Arrange
            var responseApdu = GetResponseApdu();

            // Act
            var externalAuthenticateResponse = new ExternalAuthenticateResponse(responseApdu);

            // Assert
            Assert.Equal(SWConstants.Success, externalAuthenticateResponse.StatusWord);
        }

        [Fact]
        public void ExternalAuthenticateResponse_GivenResponseApduWithData_ThrowsArgumentException()
        {
            var badResponseApdu = new ResponseApdu(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x90, 0x00 });

            _ = Assert.Throws<ArgumentException>(() => new ExternalAuthenticateResponse(badResponseApdu));
        }
    }
}

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
using Yubico.YubiKey.Scp.Commands;

namespace Yubico.YubiKey.Scp.Commands
{
    public class ScpResponseTests
    {
        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new ScpResponse(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void StatusWord_GivenResponseApdu_EqualsSWField()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { 24, 73 });

            // Act
            var scp03Response = new ScpResponse(responseApdu);

            // Assert
            Assert.Equal(responseApdu.SW, scp03Response.StatusWord);
        }

        [Fact]
        public void Status_GivenSuccessfulResponseApdu_ReturnsSuccess()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.Success, 0x00 });

            // Act
            var scp03Response = new ScpResponse(responseApdu);

            // Assert
            Assert.Equal(ResponseStatus.Success, scp03Response.Status);
        }

        [Fact]
        public void Status_GivenFailedResponseApdu_ReturnsFailed()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { SW1Constants.CommandNotAllowed, 0x00 });

            // Act
            var scp03Response = new ScpResponse(responseApdu);

            // Assert
            Assert.Equal(ResponseStatus.Failed, scp03Response.Status);
        }
    }
}

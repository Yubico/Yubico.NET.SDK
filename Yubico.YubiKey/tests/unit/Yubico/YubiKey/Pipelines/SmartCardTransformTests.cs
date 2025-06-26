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
using NSubstitute;
using Xunit;
using Yubico.Core.Devices.SmartCard;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    public class SmartCardTransformTests
    {
        [Fact]
        public void Constructor_GivenNullConnection_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new SmartCardTransform(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Cleanup_DoesNothing()
        {
            // Arrange
            var mockConnection = Substitute.For<ISmartCardConnection>();
            var transform = new SmartCardTransform(mockConnection);

            // Act
            Exception? exception = Record.Exception(() => transform.Cleanup());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void Invoke_GivenCommandApdu_CallsTrasmitWithExactApdu()
        {
            // Arrange
            var mockConnection = Substitute.For<ISmartCardConnection>();
            _ = mockConnection.Transmit(AnyCommandApdu())
                .Returns(SuccessResponse());

            var transform = new SmartCardTransform(mockConnection);
            var expectedApdu = new CommandApdu();

            // Act
            _ = transform.Invoke(expectedApdu, typeof(object), typeof(object));

            // Assert
            mockConnection.Received().Transmit(Arg.Is<CommandApdu>(a => a == expectedApdu));
        }

        [Fact]
        public void Invoke_GivenCommandApdu_ReturnsExactResponseFromTransmit()
        {
            // Arrange
            var mockConnection = Substitute.For<ISmartCardConnection>();
            ResponseApdu expectedResponse = SuccessResponse();
            _ = mockConnection.Transmit(AnyCommandApdu())
                .Returns(expectedResponse);

            var transform = new SmartCardTransform(mockConnection);

            // Act
            ResponseApdu actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        private static CommandApdu AnyCommandApdu() =>
            Arg.Any<CommandApdu>();

        private static ResponseApdu SuccessResponse() =>
            new ResponseApdu(new byte[] { 0x90, 0x00 });
    }
}

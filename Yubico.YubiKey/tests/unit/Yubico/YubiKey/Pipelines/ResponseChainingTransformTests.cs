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
using NSubstitute;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    public class ResponseChainingTransformTests
    {
        [Fact]
        public void Cleanup_Called_CallsNextTransformCleanupMethod()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            transform.Cleanup();

            // Assert
            mockTransform.Received().Cleanup();
        }

        [Fact]
        public void Setup_Called_CallsNextTransformSetupMethod()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            transform.Setup();

            // Assert
            mockTransform.Received().Setup();
        }

        [Fact]
        public void Invoke_NullArgument_ThrowsArgumentNullException()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
#pragma warning disable CS8625 // JUSTIFICATION: Null argument test case
            void Action() => _ = transform.Invoke(null, typeof(object), typeof(object));
#pragma warning restore CS8625

            // Assert
            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Invoke_Called_CallsNextTransformInvokeMethod()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(new ResponseApdu(new byte[] { 0x90, 0x00 }));
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            _ = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            mockTransform.Received(1).Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>());
        }

        [Fact]
        public void Invoke_SuccessfulResponseApdu_ReturnsResponse()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var expectedResponse = new ResponseApdu(new byte[] { 0x90, 0x00 });
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(expectedResponse);
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            ResponseApdu actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        [Fact]
        public void Invoke_FailedResponseApdu_ReturnsResponse()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var expectedResponse = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(expectedResponse);
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            ResponseApdu actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        [Fact]
        public void Invoke_BytesAvailableThenSuccess_CallsInvokeTwice()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { SW1Constants.Success, 0x00 });
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(response1, response2);
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            _ = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            mockTransform.Received(2).Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>());
        }

        [Fact]
        public void Invoke_BytesAvailableThenSuccess_CallsInvokeWithCorrectIns()
        {
            byte expectedIns = 0xC0;

            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { SW1Constants.Success, 0x00 });
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(response1, response2);
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            _ = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            mockTransform.Received(1).Invoke(Arg.Is<CommandApdu>(c => c.Ins == expectedIns), Arg.Any<Type>(), Arg.Any<Type>());
        }

        [Fact]
        public void Invoke_BytesAvailble_ConcatsAllBuffers()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { 1, 2, 3, 4, SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { 5, 6, 7, 8, SW1Constants.Success, 0x00 });
            byte[] expectedData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(response1, response2);
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            ResponseApdu actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.True(expectedData.AsSpan().SequenceEqual(actualResponse.Data.Span));
        }

        [Fact]
        public void Invoke_BytesAvailable_ReturnsLastStatusWord()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(response1, response2);
            var transform = new ResponseChainingTransform(mockTransform);

            // Act
            ResponseApdu actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Equal(SW1Constants.NoPreciseDiagnosis, actualResponse.SW1);
            Assert.Equal(0, actualResponse.SW2);
        }
    }
}

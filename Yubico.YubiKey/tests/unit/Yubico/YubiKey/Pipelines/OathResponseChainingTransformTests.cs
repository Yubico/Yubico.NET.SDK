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
using Moq;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    public class OathResponseChainingTransformTests
    {
        [Fact]
        public void Cleanup_Called_CallsNextTransformCleanupMethod()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            transform.Cleanup();

            // Assert
            mockTransform.Verify(x => x.Cleanup(), Times.Once());
        }

        [Fact]
        public void Setup_Called_CallsNextTransformSetupMethod()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            transform.Setup();

            // Assert
            mockTransform.Verify(x => x.Setup(), Times.Once());
        }

        [Fact]
        public void Invoke_NullArgument_ThrowsArgumentNullException()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
#pragma warning disable CS8625 // JUSTIFICATION: Null argument test case
            void Action()
            {
                _ = transform.Invoke(command: null, typeof(object), typeof(object));
            }
#pragma warning restore CS8625

            // Assert
            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Invoke_Called_CallsNextTransformInvokeMethod()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            _ = mockTransform
                .Setup(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(new ResponseApdu(new byte[] { 0x90, 0x00 }));
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            _ = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            mockTransform.Verify(x =>
                x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()), Times.Once());
        }

        [Fact]
        public void Invoke_SuccessfulResponseApdu_ReturnsResponse()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var expectedResponse = new ResponseApdu(new byte[] { 0x90, 0x00 });
            _ = mockTransform
                .Setup(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(expectedResponse);
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            var actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        [Fact]
        public void Invoke_FailedResponseApdu_ReturnsResponse()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var expectedResponse = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            _ = mockTransform
                .Setup(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(expectedResponse);
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            var actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        [Fact]
        public void Invoke_BytesAvailableThenSuccess_CallsInvokeTwice()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { SW1Constants.Success, 0x00 });
            _ = mockTransform
                .SetupSequence(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(response1)
                .Returns(response2);
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            var actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            mockTransform.Verify(x =>
                x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()), Times.Exactly(callCount: 2));
        }

        [Fact]
        public void Invoke_BytesAvailableThenSuccess_CallsInvokeWithCorrectIns()
        {
            byte expectedIns = 0xA5;

            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { SW1Constants.Success, 0x00 });
            _ = mockTransform
                .SetupSequence(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(response1)
                .Returns(response2);
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            var actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            mockTransform.Verify(x =>
                    x.Invoke(It.Is<CommandApdu>(c => c.Ins == expectedIns), It.IsAny<Type>(), It.IsAny<Type>()),
                Times.Once);
        }

        [Fact]
        public void Invoke_BytesAvailble_ConcatsAllBuffers()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { 1, 2, 3, 4, SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { 5, 6, 7, 8, SW1Constants.Success, 0x00 });
            var expectedData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            _ = mockTransform
                .SetupSequence(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(response1)
                .Returns(response2);
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            var actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.True(expectedData.AsSpan().SequenceEqual(actualResponse.Data.Span));
        }

        [Fact]
        public void Invoke_BytesAvailable_ReturnsLastStatusWord()
        {
            // Arrange
            var mockTransform = new Mock<IApduTransform>();
            var response1 = new ResponseApdu(new byte[] { SW1Constants.BytesAvailable, 0x00 });
            var response2 = new ResponseApdu(new byte[] { SW1Constants.NoPreciseDiagnosis, 0x00 });
            _ = mockTransform
                .SetupSequence(x => x.Invoke(It.IsAny<CommandApdu>(), It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns(response1)
                .Returns(response2);
            var transform = new OathResponseChainingTransform(mockTransform.Object);

            // Act
            var actualResponse = transform.Invoke(new CommandApdu(), typeof(object), typeof(object));

            // Assert
            Assert.Equal(SW1Constants.NoPreciseDiagnosis, actualResponse.SW1);
            Assert.Equal(expected: 0, actualResponse.SW2);
        }
    }
}

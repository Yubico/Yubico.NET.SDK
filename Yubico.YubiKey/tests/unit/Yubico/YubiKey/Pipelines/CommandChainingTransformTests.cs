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
using System.Linq;
using NSubstitute;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Pipelines
{
    public class CommandChainingTransformTests
    {
        [Fact]
        public void Cleanup_Called_CallsNextTransformCleanupMethod()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform);

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
            var transform = new CommandChainingTransform(mockTransform);

            // Act
            transform.Setup();

            // Assert
            mockTransform.Received().Setup();
        }

        [Fact]
        public void Invoke_NullCommandApdu_ThrowsArgumentNullException()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform);

            // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            void Action() => _ = transform.Invoke(null, typeof(object), typeof(object));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Assert
            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void Invoke_CommandApduWithoutData_InvokesNextTransformWithSameApdu()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform);
            var commandApdu = new CommandApdu();

            // Act
            _ = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            mockTransform.Verify(
                x =>
                    x.Invoke(Arg.Is<CommandApdu>(a => a == commandApdu), Arg.Any<Type>(), Arg.Any<Type>()));
        }

        [Fact]
        public void Invoke_CommandApduWithoutData_ReturnsNextTransformResponseAsIs()
        {
            // Arrange
            var expectedResponse = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var mockTransform = Substitute.For<IApduTransform>();
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(expectedResponse);

            var transform = new CommandChainingTransform(mockTransform);
            var commandApdu = new CommandApdu();

            // Act
            ResponseApdu actualResponse = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        [Fact]
        public void Invoke_CommandApduWithSmallData_InvokesNextTransformWithSameApdu()
        {
            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform);
            var commandApdu = new CommandApdu { Data = new byte[] { 0, 1, 2, 3 } };

            // Act
            _ = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            mockTransform.Verify(
                x =>
                    x.Invoke(Arg.Is<CommandApdu>(a => a == commandApdu), Arg.Any<Type>(), Arg.Any<Type>()));
        }

        [Fact]
        public void Invoke_CommandApduWithSmallData_ReturnsNextTransformResponseAsIs()
        {
            // Arrange
            var expectedResponse = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var mockTransform = Substitute.For<IApduTransform>();
            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(expectedResponse);

            var transform = new CommandChainingTransform(mockTransform);
            var commandApdu = new CommandApdu { Data = new byte[] { 0, 1, 2, 3 } };

            // Act
            ResponseApdu actualResponse = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            Assert.Same(expectedResponse, actualResponse);
        }

        [Fact]
        public void Invoke_CommandApduWithLargeDataBuffer_OrsHex10ToClaOnAllExceptLast()
        {
            var observedCla = new List<byte>();

            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform) { MaxChunkSize = 4 };
            var commandApdu = new CommandApdu { Data = Enumerable.Repeat<byte>(0xFF, 16).ToArray() };

            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(new ResponseApdu(new byte[] { 0x90, 0x00 }))
                .Callback<CommandApdu, Type, Type>((a, b, c) => observedCla.Add(a.Cla));

            // Act
            _ = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            Assert.Equal(new byte[] { 0x10, 0x10, 0x10, 0x00 }, observedCla);
        }

        [Fact]
        public void Invoke_CommandApduWithLargeDataBuffer_AllOtherApduPropertiesRemainUnchanged()
        {
            var observedApdus = new List<CommandApdu>();

            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform) { MaxChunkSize = 4 };
            var commandApdu = new CommandApdu
            {
                Ins = 1,
                P1 = 2,
                P2 = 3,
                Data = Enumerable.Repeat<byte>(0xFF, 16).ToArray()
            };

            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(new ResponseApdu(new byte[] { 0x90, 0x00 }))
                .Callback<CommandApdu, Type, Type>((a, b, c) => observedApdus.Add(a));

            // Act
            _ = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            foreach (CommandApdu apdu in observedApdus)
            {
                Assert.Equal(1, apdu.Ins);
                Assert.Equal(2, apdu.P1);
                Assert.Equal(3, apdu.P2);
            }
        }

        [Fact]
        public void Invoke_CommandApduWithLargeDataBuffer_SplitsDataAcrossInvokeCalls()
        {
            var observedApdus = new List<byte[]>();

            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform) { MaxChunkSize = 4 };
            var commandApdu = new CommandApdu
            {
                Data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(new ResponseApdu(new byte[] { 0x90, 0x00 }))
                .Callback<CommandApdu, Type, Type>((a, b, c) => observedApdus.Add(a.Data.ToArray()));

            // Act
            _ = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, observedApdus[0]);
            Assert.Equal(new byte[] { 5, 6, 7, 8 }, observedApdus[1]);
            Assert.Equal(new byte[] { 9, 10 }, observedApdus[2]);
        }
        
        [Fact]
        public void Invoke_CommandApduWithLargeDataBuffer_DoesntProcessAllBytes()
        {
            var observedApdus = new List<byte[]>();

            // Arrange
            var mockTransform = Substitute.For<IApduTransform>();
            var transform = new CommandChainingTransform(mockTransform) { MaxChunkSize = 4 };
            var commandApdu = new CommandApdu
            {
                Data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            _ = mockTransform.Invoke(Arg.Any<CommandApdu>(), Arg.Any<Type>(), Arg.Any<Type>())
                .Returns(new ResponseApdu([], 0x6700))
                .Callback<CommandApdu, Type, Type>((a, b, c) =>
                {
                    observedApdus.Add(a.Data.ToArray());
                });

            // Act
            _ = transform.Invoke(commandApdu, typeof(object), typeof(object));

            // Assert 
            // Should only make one pass with 4 bytes, before exiting 
            Assert.Equal(4, observedApdus[0].Length);
        }
    }
}

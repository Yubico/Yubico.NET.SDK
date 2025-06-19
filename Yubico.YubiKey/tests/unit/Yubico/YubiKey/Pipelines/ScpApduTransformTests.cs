// Copyright 2023 Yubico AB
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
using Yubico.YubiKey.InterIndustry.Commands;
using Yubico.YubiKey.Scp;

namespace Yubico.YubiKey.Pipelines
{
    public class ScpApduTransformTests
    {
        private readonly IApduTransform _previous = Substitute.For<IApduTransform>();
        private readonly Scp03KeyParameters _scp03KeyParams = Scp03KeyParameters.DefaultKey;

        [Fact]
        public void Constructor_NullPipeline_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ScpApduTransform(null!, _scp03KeyParams));
        }

        [Fact]
        public void Constructor_NullKeyParameters_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ScpApduTransform(_previous, null!));
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act
            var transform = new ScpApduTransform(_previous, _scp03KeyParams);

            // Assert
            Assert.NotNull(transform);
            Assert.Same(_scp03KeyParams, transform.KeyParameters);
        }

        [Fact]
        public void EncryptDataFunc_BeforeSetup_ThrowsInvalidOperationException()
        {
            // Arrange
            var transform = new ScpApduTransform(_previous, _scp03KeyParams);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => transform.EncryptDataFunc);
        }

        [Fact]
        public void Invoke_ExemptedCommands_BypassEncoding()
        {
            // Arrange
            var transform = new ScpApduTransform(_previous, _scp03KeyParams);

            var testCases = new[]
            {
                new CommandTestCase{
                    Command = new SelectApplicationCommand(YubiKeyApplication.SecurityDomain),
                    CommandType = typeof(SelectApplicationCommand),
                    ResponseType = typeof(ISelectApplicationResponse<GenericSelectApplicationData>)
                },
                new CommandTestCase{
                    Command =  new Oath.Commands.SelectOathCommand(),
                    CommandType = typeof(Oath.Commands.SelectOathCommand),
                    ResponseType = typeof(Oath.Commands.SelectOathResponse)
                },
                new CommandTestCase{
                    Command =  new Scp.Commands.ResetCommand(0x84, 0x01, 0x01, new byte[] { 0x00 }),
                    CommandType = typeof(Scp.Commands.ResetCommand),
                    ResponseType = typeof(YubiKeyResponse)
                }
            };

            foreach (var testCase in testCases)
            {
                _previous.Invoke(
                    Arg.Any<CommandApdu>(),
                    Arg.Any<Type>(),
                    Arg.Any<Type>())
                    .Returns(new ResponseApdu(new byte[] { 0x90, 0x00 }));

                // Act
                _ = transform.Invoke(
                    testCase.Command.CreateCommandApdu(),
                    testCase.CommandType,
                    testCase.ResponseType);

                // Assert that the previous pipeline was called (which is the one that doesn't do encoding)
                _previous.Received().Invoke(
                    Arg.Any<CommandApdu>(),
                    testCase.CommandType,
                    testCase.ResponseType);

                _previous.Reset();
            }
        }

        private struct CommandTestCase
        {
            public IYubiKeyCommand<IYubiKeyResponse> Command;
            public Type CommandType;
            public Type ResponseType;
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Arrange
            var transform = new ScpApduTransform(_previous, _scp03KeyParams);

            // Act & Assert
            transform.Dispose();
            transform.Dispose(); // Should not throw
        }

        [Fact]
        public void Cleanup_CallsUnderlyingPipelineCleanup()
        {
            // Arrange
            var transform = new ScpApduTransform(_previous, _scp03KeyParams);

            // Act
            transform.Cleanup();

            // Assert
            _previous.Received().Cleanup();
        }
    }
}

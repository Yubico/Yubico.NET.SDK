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
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Yubico.Core.Tlv.UnitTests
{
    public class TlvExceptionTests
    {
        [Fact]
        public void Constructor_GivenNoParameters_ReturnsCorrectException()
        {
            // Arrange

            // Act
            var exception = new TlvException();

            // Assert
            _ = Assert.IsType<TlvException>(exception);
        }

        [Fact]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit test")]
        public void Constructor_GivenMessageParameter_SetsMessageProperty()
        {
            // Arrange
            const string expectedMessage = "This is a test message.";

            // Act
            var exception = new TlvException(expectedMessage);

            // Assert
            Assert.Equal(expectedMessage, exception.Message);
        }

        [Fact]
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unit test")]
        public void Constructor_GivenInnerException_SetsInnerExceptionProperty()
        {
            // Arrange
            var expectedInnerException = new Exception("Test exception");
            const string message = "This is a test message.";

            // Act
            var exception = new TlvException(message, expectedInnerException);

            // Assert
            Assert.Equal(expectedInnerException, exception.InnerException);
        }
    }
}

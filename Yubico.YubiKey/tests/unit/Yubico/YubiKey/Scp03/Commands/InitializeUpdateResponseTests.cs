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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Scp03.Commands
{
    public class InitializeUpdateResponseTests
    {
        public static ResponseApdu GetResponseApdu()
        {
            return new ResponseApdu(new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 13, 14,
                15, 16, 17, 18, 19, 20, 21, 22,
                23, 24, 25, 26, 27, 28, 29,
                0x90, 0x00
            });
        }

        private static IReadOnlyCollection<byte> GetDiversificationData()
        {
            return GetResponseApdu().Data.Slice(start: 0, length: 10).ToArray();
        }

        private static IReadOnlyCollection<byte> GetKeyInfo()
        {
            return GetResponseApdu().Data.Slice(start: 10, length: 3).ToArray();
        }

        private static IReadOnlyCollection<byte> GetCardChallenge()
        {
            return GetResponseApdu().Data.Slice(start: 13, length: 8).ToArray();
        }

        private static IReadOnlyCollection<byte> GetCardCryptogram()
        {
            return GetResponseApdu().Data.Slice(start: 21, length: 8).ToArray();
        }

        [Fact]
        public void Constructor_GivenNullResponseApdu_ThrowsArgumentNullExceptionFromBase()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _ = Assert.Throws<ArgumentNullException>(() => new InitializeUpdateResponse(responseApdu: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public void Constructor_GivenResponseApdu_SetsStatusWordCorrectly()
        {
            // Arrange
            var responseApdu = GetResponseApdu();

            // Act
            var initializeUpdateResponse = new InitializeUpdateResponse(responseApdu);

            // Assert
            Assert.Equal(SWConstants.Success, initializeUpdateResponse.StatusWord);
        }

        [Fact]
        public void InitializeUpdateResponse_GivenResponseApdu_DiversificationDataEqualsBytes0To10()
        {
            // Arrange
            var responseApdu = GetResponseApdu();

            // Act
            var initializeUpdateResponse = new InitializeUpdateResponse(responseApdu);

            // Assert
            Assert.Equal(GetDiversificationData(), initializeUpdateResponse.DiversificationData);
        }

        [Fact]
        public void InitializeUpdateResponse_GivenResponseApdu_KeyInfoEqualsBytes10To13()
        {
            // Arrange
            var responseApdu = GetResponseApdu();

            // Act
            var initializeUpdateResponse = new InitializeUpdateResponse(responseApdu);

            // Assert
            Assert.Equal(GetKeyInfo(), initializeUpdateResponse.KeyInfo);
        }

        [Fact]
        public void InitializeUpdateResponse_GivenResponseApdu_CardChallengeEqualsBytes13To21()
        {
            // Arrange
            var responseApdu = GetResponseApdu();

            // Act
            var initializeUpdateResponse = new InitializeUpdateResponse(responseApdu);

            // Assert
            Assert.Equal(GetCardChallenge(), initializeUpdateResponse.CardChallenge);
        }

        [Fact]
        public void InitializeUpdateResponse_GivenResponseApdu_CardCryptogramEqualsBytes21To29()
        {
            // Arrange
            var responseApdu = GetResponseApdu();

            // Act
            var initializeUpdateResponse = new InitializeUpdateResponse(responseApdu);

            // Assert
            Assert.Equal(GetCardCryptogram(), initializeUpdateResponse.CardCryptogram);
        }
    }
}

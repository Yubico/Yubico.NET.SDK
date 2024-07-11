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

namespace Yubico.YubiKey.Otp.Commands
{
    public class ChallengeResponseCommandTests
    {
        [Fact]
        public void OtpSlot_SetInvalidOtpSlot_ThrowsArgumentException()
        {
            static void Action() => _ = new ChallengeResponseCommand(
                (Slot)0x5, // Some invalid value
                ChallengeResponseAlgorithm.HmacSha1,
                Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void Algorithm_SetInvalidAlgorithm_ThrowsArgumentException()
        {
            static void Action() => _ = new ChallengeResponseCommand(
                Slot.LongPress,
                (ChallengeResponseAlgorithm)0x5, // Some invalid value
                Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void Application_Get_AlwaysReturnsOtp()
        {
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                Array.Empty<byte>());

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Otp, application);
        }

        [Fact]
        public void FullConstructor_GivenParameters_SetsAllParameters()
        {
            Slot expectedSlot = Slot.LongPress;
            ChallengeResponseAlgorithm expectedAlgorithm = ChallengeResponseAlgorithm.HmacSha1;
            var command = new ChallengeResponseCommand(expectedSlot, expectedAlgorithm, Array.Empty<byte>());

            Slot actualSlot = command.OtpSlot;
            ChallengeResponseAlgorithm actualAlgorithm = command.Algorithm;

            Assert.Equal(expectedSlot, actualSlot);
            Assert.Equal(expectedAlgorithm, actualAlgorithm);
        }

        [Fact]
        public void FullConstructor_GivenChallenge_SetsChallengeInBuffer()
        {
            byte[] expectedChallenge = new byte[] { 0, 1, 2, 3 };
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                expectedChallenge);

            ReadOnlyMemory<byte> actualChallenge = command.CreateCommandApdu().Data;

            Assert.Equal(expectedChallenge, actualChallenge);
        }

        [Fact]
        public void FullConstructor_YubicoOtpGivenChallengeThatIsNot6Bytes_ThrowsArgumentException()
        {
            static void Action() => _ = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.YubicoOtp,
                Array.Empty<byte>());

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void FullConstructor_HmacSha1GivenChallengeGreaterThan64Bytes_ThrowsArgumentException()
        {
            static void Action() => _ = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                new byte[65]);

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_RetunsZero()
        {
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                Array.Empty<byte>());

            byte cla = command.CreateCommandApdu().Cla;

            Assert.Equal(0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex01()
        {
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                Array.Empty<byte>());

            byte ins = command.CreateCommandApdu().Ins;

            Assert.Equal(1, ins);
        }

        [Theory]
        [InlineData(Slot.ShortPress, ChallengeResponseAlgorithm.YubicoOtp, 0x20)]
        [InlineData(Slot.ShortPress, ChallengeResponseAlgorithm.HmacSha1, 0x30)]
        [InlineData(Slot.LongPress, ChallengeResponseAlgorithm.YubicoOtp, 0x28)]
        [InlineData(Slot.LongPress, ChallengeResponseAlgorithm.HmacSha1, 0x38)]
        public void CreateCommandApdu_GetP1Property_ReturnsCorrectValueForSlot(
            Slot otpSlot,
            ChallengeResponseAlgorithm algorithm,
            byte expectedSlotValue)
        {
            var command = new ChallengeResponseCommand(otpSlot, algorithm, new byte[] { 1, 2, 3, 4, 5, 6 });

            byte actualSlotValue = command.CreateCommandApdu().P1;

            Assert.Equal(expectedSlotValue, actualSlotValue);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                Array.Empty<byte>());

            byte p2 = command.CreateCommandApdu().P2;

            Assert.Equal(0, p2);
        }

        [Fact]
        public void CreateCommandApdu_GetNc_ReturnsChallengeSize()
        {
            byte[] challenge = new byte[] { 1, 2, 3, 4 };
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                challenge);

            int nc = command.CreateCommandApdu().Nc;

            Assert.Equal(challenge.Length, nc);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new ChallengeResponseCommand(
                Slot.LongPress,
                ChallengeResponseAlgorithm.HmacSha1,
                Array.Empty<byte>());

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<ChallengeResponseResponse>(response);
        }
    }
}

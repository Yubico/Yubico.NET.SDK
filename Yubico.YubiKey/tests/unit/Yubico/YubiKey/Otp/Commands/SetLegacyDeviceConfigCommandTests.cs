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

using System.Buffers.Binary;
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    public class SetLegacyDeviceConfigCommandTests
    {
        [Fact]
        public void Mode_SetGet_GetsTheSetValue()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);
            var expectedMode = YubiKeyCapabilities.Ccid;

            command.YubiKeyInterfaces = expectedMode;

            Assert.Equal(expectedMode, command.YubiKeyInterfaces);
        }

        [Fact]
        public void ChallengeResponseTimeout_SetGet_GetsTheSetValue()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);
            byte expectedTimeout = 7;

            command.ChallengeResponseTimeout = expectedTimeout;

            Assert.Equal(expectedTimeout, command.ChallengeResponseTimeout);
        }

        [Fact]
        public void AutoEjectTimeout_SetGet_GetsTheSetValue()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);
            var expectedTimeout = 1234;

            command.AutoEjectTimeout = expectedTimeout;

            Assert.Equal(expectedTimeout, command.AutoEjectTimeout);
        }

        [Fact]
        public void Application_Get_AlwaysOtp()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            Assert.Equal(YubiKeyApplication.Otp, command.Application);
        }

        [Fact]
        public void FullConstructor_GivenValues_SetsProperties()
        {
            var expectedMode = YubiKeyCapabilities.Ccid;
            byte expectedCrTimeout = 7;
            var touchEjectEnabled = true;
            var expectedAeTimeout = 1234;

            var command = new SetLegacyDeviceConfigCommand(
                expectedMode,
                expectedCrTimeout,
                touchEjectEnabled,
                expectedAeTimeout);

            Assert.Equal(expectedMode, command.YubiKeyInterfaces);
            Assert.Equal(expectedCrTimeout, command.ChallengeResponseTimeout);
            Assert.Equal(touchEjectEnabled, command.TouchEjectEnabled);
            Assert.Equal(expectedAeTimeout, command.AutoEjectTimeout);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var cla = command.CreateCommandApdu().Cla;

            Assert.Equal(expected: 0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns0x01()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var ins = command.CreateCommandApdu().Ins;

            Assert.Equal(expected: 1, ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_Returns0x11()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var p1 = command.CreateCommandApdu().P1;

            Assert.Equal(expected: 0x11, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var p2 = command.CreateCommandApdu().P2;

            Assert.Equal(expected: 0, p2);
        }

        [Fact]
        public void CreateCommandApdu_GetData_ModePlacedAsFirstByte()
        {
            byte expectedMode = 0x01;
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.Ccid,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var data = command.CreateCommandApdu().Data;

            var actualModeValue = (byte)(data.Span[index: 0] & 0b0111_1111);

            Assert.Equal(expectedMode, actualModeValue);
        }

        [Fact]
        public void CreateCommandApdu_GetData_CrTimeoutPlacedAsSecondByte()
        {
            byte expectedTimeout = 7;
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                expectedTimeout,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var data = command.CreateCommandApdu().Data;

            Assert.Equal(expectedTimeout, data.Span[index: 1]);
        }

        [Fact]
        public void CreateCommandApdu_GetData_TouchEjectPlacedInFirstByte()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: true,
                autoEjectTimeout: 0);

            var data = command.CreateCommandApdu().Data;

            byte expectedTouchEjectValue = 0x80;

            var actualTouchEjectValue = (byte)(data.Span[index: 0] & expectedTouchEjectValue);

            Assert.Equal(actualTouchEjectValue, expectedTouchEjectValue);
        }

        [Fact]
        public void CreateCommandApdu_GetData_AeTimeoutPlacedAsLastTwoBytes()
        {
            var expectedTimeout = 1234;
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: true,
                expectedTimeout);

            var data = command.CreateCommandApdu().Data;
            int actualTimeout = BinaryPrimitives.ReadUInt16LittleEndian(data.Span.Slice(start: 2));

            Assert.Equal(expectedTimeout, actualTimeout);
        }

        [Fact]
        public void CreateCommandApdu_GetLc_Returns0x04()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);
            var nc = command.CreateCommandApdu().Nc;

            Assert.Equal(expected: 4, nc);
        }

        [Fact]
        public void CreateCommandApdu_GetLe_ReturnsZero()
        {
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            var ne = command.CreateCommandApdu().Ne;

            Assert.Equal(expected: 0, ne);
        }

        [Fact]
        public void CreateResponseApdu_ReturnsCorrectType()
        {
            // Arrange
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new SetLegacyDeviceConfigCommand(
                YubiKeyCapabilities.All,
                challengeResponseTimeout: 0,
                touchEjectEnabled: false,
                autoEjectTimeout: 0);

            // Act
            var response = command.CreateResponseForApdu(responseApdu);

            // Assert
            Assert.True(response is ReadStatusResponse);
        }
    }
}

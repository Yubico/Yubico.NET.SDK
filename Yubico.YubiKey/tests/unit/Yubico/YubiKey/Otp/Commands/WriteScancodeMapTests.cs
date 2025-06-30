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
using Xunit;
using Yubico.Core.Devices.Hid;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Otp.Commands
{
    public class WriteScancodeMapTests
    {
        [Fact]
        public void Application_Get_ReturnsOtpApplication()
        {
            var command = new WriteScancodeMap();

            Assert.Equal(YubiKeyApplication.Otp, command.Application);
        }

        [Fact]
        public void ScancodeMap_SetWithIncorrectLength_ThrowsArgumentException()
        {
            byte[] scancodeMap = HidCodeTranslator.GetInstance(KeyboardLayout.en_US).GetHidCodes("abc");
            var command = new WriteScancodeMap();

            void Action() => command.ScancodeMap = scancodeMap;

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void ScancodeMap_SetFollowedByGet_ReturnsTheSetScancodeMap()
        {
            byte[] scancodeMap = HidCodeTranslator.GetInstance(KeyboardLayout.en_US).GetHidCodes(
                "123456789012345678901234567890123456789012345");

            var command = new WriteScancodeMap
            {
                ScancodeMap = scancodeMap
            };

            Assert.Equal(scancodeMap, command.ScancodeMap);
        }

        [Fact]
        public void DefaultConstructor_ConstructsObject()
        {
            var command = new WriteScancodeMap();

            Assert.NotNull(command);
        }

        [Fact]
        public void DefaultConstructor_ScancodeMap_SetToDefaultModhexMap()
        {
            Memory<byte> scancodeMap = HidCodeTranslator.GetInstance(KeyboardLayout.en_US).GetHidCodes(
                "cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\n");
            var command = new WriteScancodeMap();

            // Don't use Memory<T>.Equals() here as that effectively does a ReferenceEquals instead
            // of testing for the same array contents.
            Assert.True(scancodeMap.Span.SequenceEqual(command.ScancodeMap.Span));
        }

        [Fact]
        public void FullConstructor_GivenScancodeMap_SetsScanmodeMapProperty()
        {
            byte[] scancodeMap = HidCodeTranslator.GetInstance(KeyboardLayout.en_US).GetHidCodes(
                "123456789012345678901234567890123456789012345");

            var command = new WriteScancodeMap(scancodeMap);

            Assert.Equal(scancodeMap, command.ScancodeMap);
        }

        [Fact]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero()
        {
            var command = new WriteScancodeMap();

            byte cla = command.CreateCommandApdu().Cla;

            Assert.Equal(0, cla);
        }

        [Fact]
        public void CreateCommandApdu_GetInsProperty_Returns01()
        {
            var command = new WriteScancodeMap();

            byte ins = command.CreateCommandApdu().Ins;

            Assert.Equal(1, ins);
        }

        [Fact]
        public void CreateCommandApdu_GetP1Property_ReturnsHex12()
        {
            var command = new WriteScancodeMap();

            byte p1 = command.CreateCommandApdu().P1;

            Assert.Equal(0x12, p1);
        }

        [Fact]
        public void CreateCommandApdu_GetP2Property_ReturnsZero()
        {
            var command = new WriteScancodeMap();

            byte p2 = command.CreateCommandApdu().P2;

            Assert.Equal(0, p2);
        }

        [Fact]
        public void CreateCommandApdu_GetLcProperty_ReturnsScancodeMapSize()
        {
            var command = new WriteScancodeMap();

            int nc = command.CreateCommandApdu().Nc;

            Assert.Equal(45, nc);
        }

        [Fact]
        public void CreateCommandApdu_GetLeProperty_ReturnsZero()
        {
            var command = new WriteScancodeMap();

            int ne = command.CreateCommandApdu().Ne;

            Assert.Equal(0, ne);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 00 });
            var command = new WriteScancodeMap();

            IYubiKeyResponse response = command.CreateResponseForApdu(responseApdu);

            _ = Assert.IsAssignableFrom<ReadStatusResponse>(response);
        }
    }
}

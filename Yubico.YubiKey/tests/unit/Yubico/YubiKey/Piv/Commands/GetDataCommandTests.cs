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
using Xunit;
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class GetDataCommandTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void ClassType_DerivedFromPivCommand_IsTrue(int cStyle)
        {
            var command = GetCommandObject(cStyle, PivDataTag.Chuid);

            Assert.True(command is IYubiKeyCommand<GetDataResponse>);
        }

        [Fact]
        public void NoArgConstructor_DerivedFromPivCommand_IsTrue()
        {
            var command = new GetDataCommand();

            Assert.True(command is IYubiKeyCommand<GetDataResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var command = new GetDataCommand();

            var application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(PivDataTag.Chuid)]
        [InlineData(PivDataTag.Capability)]
        [InlineData(PivDataTag.Discovery)]
        [InlineData(PivDataTag.IrisImages)]
        [InlineData(PivDataTag.BiometricGroupTemplate)]
        public void Constructor_Property_Tag(PivDataTag tag)
        {
#pragma warning disable CS0618 // Testing an obsolete feature
            var command = new GetDataCommand(tag);

            var getTag = command.Tag;
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(tag, getTag);
        }

        [Theory]
        [InlineData(1, PivDataTag.SecurityObject)]
        [InlineData(2, PivDataTag.Signature)]
        [InlineData(3, PivDataTag.Retired1)]
        [InlineData(4, PivDataTag.Retired10)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle, PivDataTag tag)
        {
            var cmdApdu = GetDataCommandApdu(cStyle, tag);

            var Cla = cmdApdu.Cla;

            Assert.Equal(expected: 0, Cla);
        }

        [Theory]
        [InlineData(1, PivDataTag.Retired2)]
        [InlineData(2, PivDataTag.Capability)]
        [InlineData(3, PivDataTag.SecureMessageSigner)]
        [InlineData(4, PivDataTag.Retired10)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexCB(int cStyle, PivDataTag tag)
        {
            var cmdApdu = GetDataCommandApdu(cStyle, tag);

            var Ins = cmdApdu.Ins;

            Assert.Equal(expected: 0xCB, Ins);
        }

        [Theory]
        [InlineData(1, PivDataTag.Fingerprints)]
        [InlineData(2, PivDataTag.Signature)]
        [InlineData(3, PivDataTag.Printed)]
        [InlineData(4, PivDataTag.Capability)]
        public void CreateCommandApdu_GetP1Property_ReturnsHex3F(int cStyle, PivDataTag tag)
        {
            var cmdApdu = GetDataCommandApdu(cStyle, tag);

            var P1 = cmdApdu.P1;

            Assert.Equal(expected: 0x3F, P1);
        }

        [Theory]
        [InlineData(1, PivDataTag.KeyManagement)]
        [InlineData(2, PivDataTag.Retired4)]
        [InlineData(3, PivDataTag.Discovery)]
        [InlineData(4, PivDataTag.CardAuthentication)]
        public void CreateCommandApdu_GetP2Property_ReturnsHexFF(int cStyle, PivDataTag tag)
        {
            var cmdApdu = GetDataCommandApdu(cStyle, tag);

            var P2 = cmdApdu.P2;

            Assert.Equal(expected: 0xFF, P2);
        }

        [Theory]
        [InlineData(1, PivDataTag.Discovery, 3)]
        [InlineData(2, PivDataTag.CardAuthentication, 5)]
        [InlineData(3, PivDataTag.KeyManagement, 5)]
        [InlineData(4, PivDataTag.Retired3, 5)]
        public void CreateCommandApdu_GetLc_ReturnsCorrect(int cStyle, PivDataTag tag, int expectedLength)
        {
            var cmdApdu = GetDataCommandApdu(cStyle, tag);

            var Lc = cmdApdu.Nc;

            Assert.Equal(expectedLength, Lc);
        }

        [Theory]
        [InlineData(1, PivDataTag.Discovery)]
        [InlineData(2, PivDataTag.CardAuthentication)]
        [InlineData(3, PivDataTag.KeyManagement)]
        [InlineData(4, PivDataTag.Chuid)]
        public void CreateCommandApdu_GetLe_ReturnsZero(int cStyle, PivDataTag tag)
        {
            var cmdApdu = GetDataCommandApdu(cStyle, tag);

            var Le = cmdApdu.Ne;

            Assert.Equal(expected: 0, Le);
        }

        [Theory]
        [InlineData(PivDataTag.Chuid)]
        [InlineData(PivDataTag.Capability)]
        [InlineData(PivDataTag.Discovery)]
        [InlineData(PivDataTag.Authentication)]
        [InlineData(PivDataTag.Signature)]
        [InlineData(PivDataTag.KeyManagement)]
        [InlineData(PivDataTag.CardAuthentication)]
        [InlineData(PivDataTag.Retired1)]
        [InlineData(PivDataTag.Retired2)]
        [InlineData(PivDataTag.Retired3)]
        [InlineData(PivDataTag.Retired4)]
        [InlineData(PivDataTag.Retired5)]
        [InlineData(PivDataTag.Retired6)]
        [InlineData(PivDataTag.Retired7)]
        [InlineData(PivDataTag.Retired8)]
        [InlineData(PivDataTag.Retired9)]
        [InlineData(PivDataTag.Retired10)]
        [InlineData(PivDataTag.Retired11)]
        [InlineData(PivDataTag.Retired12)]
        [InlineData(PivDataTag.Retired13)]
        [InlineData(PivDataTag.Retired14)]
        [InlineData(PivDataTag.Retired15)]
        [InlineData(PivDataTag.Retired16)]
        [InlineData(PivDataTag.Retired17)]
        [InlineData(PivDataTag.Retired18)]
        [InlineData(PivDataTag.Retired19)]
        [InlineData(PivDataTag.Retired20)]
        [InlineData(PivDataTag.Printed)]
        [InlineData(PivDataTag.SecurityObject)]
        [InlineData(PivDataTag.KeyHistory)]
        [InlineData(PivDataTag.IrisImages)]
        [InlineData(PivDataTag.FacialImage)]
        [InlineData(PivDataTag.Fingerprints)]
        [InlineData(PivDataTag.BiometricGroupTemplate)]
        [InlineData(PivDataTag.SecureMessageSigner)]
        [InlineData(PivDataTag.PairingCodeReferenceData)]
        public void GetCommandApdu_Data_Correct(PivDataTag tag)
        {
            var cmdApdu = GetDataCommandApdu(cStyle: 4, tag);
            var expected = PivCommandResponseTestData.GetDataCommandExpectedApduData(tag);

            var data = cmdApdu.Data;

            var compareResult = expected.SequenceEqual(data.ToArray());

            Assert.True(compareResult);
        }

        [Fact]
        public void GetDiscovery_Data_Correct()
        {
            var cmdApdu = GetDataCommandApdu(cStyle: 2, PivDataTag.Discovery);

            var expected = new List<byte>(new byte[]
            {
                0x5C, 0x01, 0x7E
            });

            var data = cmdApdu.Data;

            var compareResult = expected.SequenceEqual(data.ToArray());

            Assert.True(compareResult);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });
            var command = new GetDataCommand((int)PivDataTag.Signature);

            var response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is GetDataResponse);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void Constructor_BadTag_CorrectException(int cStyle)
        {
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(cStyle, tag: 0));
        }

        [Fact]
        public void NoTag_GetApdu_CorrectException()
        {
            var command = new GetDataCommand();
            _ = Assert.Throws<InvalidOperationException>(() => command.CreateCommandApdu());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0x0000007F)]
        [InlineData(0x00001111)]
        [InlineData(0x015fff01)]
        public void IntTag_InvalidTag_Exception(int tag)
        {
            _ = Assert.Throws<ArgumentException>(() => new GetDataCommand(tag));
        }

        [Theory]
        [InlineData(0x0000007E)]
        [InlineData(0x00007F61)]
        [InlineData(0x005FFF01)]
        [InlineData(0x005FFF00)]
        [InlineData(0x005FFF10)]
        [InlineData(0x005FFF11)]
        [InlineData(0x005FFF12)]
        [InlineData(0x005FFF13)]
        [InlineData(0x005FFF14)]
        [InlineData(0x005FFF15)]
        [InlineData(0x005F0000)]
        [InlineData(0x005FFFFF)]
        public void IntTag_CmdApdu_Correct(int tag)
        {
            var command = new GetDataCommand(tag);
            var cmdApdu = command.CreateCommandApdu();
            var expected = PivCommandResponseTestData.GetDataCommandExpectedApduDataInt(tag);

            var data = cmdApdu.Data;

            var compareResult = expected.SequenceEqual(data.ToArray());

            Assert.True(compareResult);
        }

        private static CommandApdu GetDataCommandApdu(int cStyle, PivDataTag tag)
        {
            var command = GetCommandObject(cStyle, tag);
            return command.CreateCommandApdu();
        }

        // Construct a GetDataCommand using the style specified.
        // If the style arg is 1, this will build using the full constructor.
        // If it is 2, it will build it using object initializer constructor.
        // If it is 3, create it using the empty constructor and set the
        // properties later.
        private static GetDataCommand GetCommandObject(int cStyle, PivDataTag tag)
        {
            GetDataCommand cmd;

            switch (cStyle)
            {
                default:
#pragma warning disable CS0618 // Testing an obsolete feature
                    cmd = new GetDataCommand(tag);
#pragma warning restore CS0618 // Type or member is obsolete
                    break;

                case 2:
                    cmd = new GetDataCommand
                    {
                        DataTag = (int)tag
                    };
                    break;

#pragma warning disable IDE0017 // specifically testing this code model
                case 3:
                    cmd = new GetDataCommand();
#pragma warning disable CS0618 // Testing an obsolete feature
                    cmd.Tag = tag;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore IDE0017
                    break;

                case 4:
                    cmd = new GetDataCommand((int)tag);
                    break;
            }

            return cmd;
        }
    }
}

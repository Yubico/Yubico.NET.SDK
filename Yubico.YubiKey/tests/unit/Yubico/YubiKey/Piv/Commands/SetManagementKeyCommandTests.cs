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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    public class SetManagementKeyCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            byte[] mgmtKey = GetMgmtKeyArray();
            var command = new SetManagementKeyCommand(mgmtKey, PivTouchPolicy.Always, PivAlgorithm.TripleDes);

            Assert.True(command is IYubiKeyCommand<SetManagementKeyResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            byte[] mgmtKey = GetMgmtKeyArray();
            var command = new SetManagementKeyCommand(mgmtKey, PivTouchPolicy.Always, PivAlgorithm.TripleDes);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Fact]
        public void Constructor_Property_TouchPolicy()
        {
            byte[] mgmtKey = GetMgmtKeyArray();
            PivTouchPolicy touchPolicy = PivTouchPolicy.Always;
            var command = new SetManagementKeyCommand(mgmtKey, touchPolicy, PivAlgorithm.TripleDes);

            PivTouchPolicy getPolicy = command.TouchPolicy;

            Assert.Equal(touchPolicy, getPolicy);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, PivTouchPolicy.Always);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexFF(int cStyle)
        {
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, PivTouchPolicy.Always);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0xFF, Ins);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateCommandApdu_GetP1Property_ReturnsHexFF(int cStyle)
        {
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, PivTouchPolicy.Always);

            byte P1 = cmdApdu.P1;

            Assert.Equal(0xFF, P1);
        }

        [Theory]
        [InlineData(1, PivTouchPolicy.None, 0xFF)]
        [InlineData(2, PivTouchPolicy.Default, 0xFF)]
        [InlineData(3, PivTouchPolicy.Never, 0xFF)]
        [InlineData(1, PivTouchPolicy.Always, 0xFE)]
        [InlineData(2, PivTouchPolicy.Cached, 0xFD)]
        public void CreateCommandApdu_GetP2Property_ReturnsPolicy(int cStyle, PivTouchPolicy policy, byte p2Val)
        {
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, policy);

            byte P2 = cmdApdu.P2;

            Assert.Equal(p2Val, P2);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateCommandApdu_GetNc_Returns27(int cStyle)
        {
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, PivTouchPolicy.Always);

            int Nc = cmdApdu.Nc;

            Assert.Equal(27, Nc);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateCommandApdu_GetNe_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, PivTouchPolicy.Always);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateCommandApdu_GetDataProperty_ReturnsKey(int cStyle)
        {
            byte[] expected = new byte[27];
            expected[0] = 0x03;
            expected[1] = 0x9B;
            expected[2] = 0x18;
            byte[] mgmtKey = GetMgmtKeyArray();
            Array.Copy(mgmtKey, 0, expected, 3, 24);
            CommandApdu cmdApdu = GetSetManagementKeyCommandApdu(cStyle, PivTouchPolicy.Always);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            bool compareResult = data.Span.SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void CreateResponseForApdu_ReturnsCorrectType(int cStyle)
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });
            SetManagementKeyCommand cmd = GetCommandObject(cStyle, PivTouchPolicy.Default);

            SetManagementKeyResponse response = cmd.CreateResponseForApdu(responseApdu);

            Assert.True(response is SetManagementKeyResponse);
        }

        private static CommandApdu GetSetManagementKeyCommandApdu(int cStyle, PivTouchPolicy touchPolicy)
        {
            SetManagementKeyCommand cmd = GetCommandObject(cStyle, touchPolicy);

            CommandApdu returnValue = cmd.CreateCommandApdu();

            return returnValue;
        }

        // Construct a SetManagementKeyCommand using the style specified.
        // If the style arg is 1, this will build using the full constructor.
        // If it is 2, it will build it using object initializer constructor.
        // If it is 3, create it using the key only constructor and set the
        // property later.
        // If it is 4, create it using the key only constructor but
        // don't set the TouchPolicy (it should be default).
        private static SetManagementKeyCommand GetCommandObject(int cStyle, PivTouchPolicy touchPolicy)
        {
            SetManagementKeyCommand cmd;
            byte[] mgmtKey = GetMgmtKeyArray();

            switch (cStyle)
            {
                default:
                    cmd = new SetManagementKeyCommand(mgmtKey, touchPolicy, PivAlgorithm.TripleDes);
                    break;

                case 2:
                    cmd = new SetManagementKeyCommand(mgmtKey, PivAlgorithm.TripleDes)
                    {
                        TouchPolicy = touchPolicy,
                    };
                    break;


                case 3:
#pragma warning disable IDE0017 // Specifically testing this construction
                    cmd = new SetManagementKeyCommand(mgmtKey, PivAlgorithm.TripleDes);
                    cmd.TouchPolicy = touchPolicy;
                    break;
#pragma warning restore IDE0017

                case 4:
                    cmd = new SetManagementKeyCommand(mgmtKey, PivAlgorithm.TripleDes);
                    break;
            }

            return cmd;
        }

        private static byte[] GetMgmtKeyArray() =>
            new byte[]
        {
            0x46, 0x87, 0x19, 0x18, 0x87, 0x54, 0x54, 0x88,
            0x93, 0x54, 0x55, 0x60, 0x59, 0x55, 0x94, 0x84,
            0x13, 0x81, 0x23, 0x76, 0x00, 0x30, 0x53, 0x14
        };
    }
}

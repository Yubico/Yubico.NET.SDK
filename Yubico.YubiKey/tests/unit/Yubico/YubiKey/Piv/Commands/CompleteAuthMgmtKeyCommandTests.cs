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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Commands
{
    // Values in this test are based on the following bytes:
    //
    //   mgmt key =
    //     0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02,
    //     0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B,
    //     0x53, 0xEF, 0x4B, 0x8E, 0x3B, 0x91, 0x86, 0x04
    //
    //   challenge 1 (from YubiKey to app) =
    //     0x39, 0xA0, 0xA8, 0xE9, 0xF5, 0x28, 0x87, 0x75
    //
    //   correct response 1 (from app) =
    //     0xD0, 0xFE, 0x1A, 0x35, 0xA4, 0xE9, 0x40, 0xF8
    //
    //   challenge 2 (from app to YubiKey) =
    //     0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64
    //
    //   correct response 2 (from app) =
    //     0xAC, 0x29, 0xA4, 0x5E, 0x1F, 0x42, 0x8A, 0x23
    //
    // The random number generator will have to generate A4, C4, etc. when
    // building the Command APDU for mutual authentication.
    public class CompleteAuthMgmtKeyCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            CompleteAuthenticateManagementKeyCommand command = GetCommandObject(true, true);

            Assert.True(command is IYubiKeyCommand<CompleteAuthenticateManagementKeyResponse>);
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            CompleteAuthenticateManagementKeyCommand command = GetCommandObject(false, false);

            YubiKeyApplication Application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, Application);
        }

        [Fact]
        public void Mutual_CreateResponse_CorrectType()
        {
            byte successSw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte successSw2 = unchecked((byte)SWConstants.Success);
            byte[] apduMutual = new byte[]
            {
                0x7C, 0x0A, 0x82, 0x08, 0xAC, 0x29, 0xA4, 0x5E, 0x1F, 0x42, 0x8A, 0x23, successSw1, successSw2
            };

            CompleteAuthenticateManagementKeyCommand command = GetCommandObject(true, true);
            var responseApdu = new ResponseApdu(apduMutual);

            CompleteAuthenticateManagementKeyResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is IYubiKeyResponseWithData<AuthenticateManagementKeyResult>);
        }

        [Fact]
        public void Constructor_NullResponse_ThrowsException()
        {
            byte[] mgmtKey = GetMgmtKey();

#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new CompleteAuthenticateManagementKeyCommand(null, mgmtKey));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_NullMgmtKey_ThrowsException()
        {
            InitializeAuthenticateManagementKeyResponse response = GetInitResponse(false);

            _ = Assert.Throws<ArgumentException>(() => new CompleteAuthenticateManagementKeyCommand(response, null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        public void Constructor_BadKeyLen_ThrowsException(int difference)
        {
            InitializeAuthenticateManagementKeyResponse response = GetInitResponse(false);
            byte[] mgmtKey = GetMgmtKey();

            if (difference >= 0)
            {
                byte[] buffer = new byte[25];
                mgmtKey.CopyTo(buffer, 1);
                buffer[0] = 0x99;
                mgmtKey = buffer;
            }
            else
            {
                mgmtKey = mgmtKey.Take(23).ToArray();
            }

            _ = Assert.Throws<ArgumentException>(() => new CompleteAuthenticateManagementKeyCommand(response, mgmtKey));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(bool isMutual)
        {
            CommandApdu cmdApdu = GetCommandApdu(isMutual, true);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex87(bool isMutual)
        {
            CommandApdu cmdApdu = GetCommandApdu(isMutual, true);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0x87, Ins);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateCommandApdu_GetP1Property_ReturnsThree(bool isMutual)
        {
            CommandApdu cmdApdu = GetCommandApdu(isMutual, true);

            byte P1 = cmdApdu.P1;

            Assert.Equal(3, P1);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateCommandApdu_GetP2Property_ReturnsHex9B(bool isMutual)
        {
            CommandApdu cmdApdu = GetCommandApdu(isMutual, true);

            byte P2 = cmdApdu.P2;

            Assert.Equal(0x9B, P2);
        }

        [Theory]
        [InlineData(true, 24)]
        [InlineData(false, 12)]
        public void CreateCommandApdu_GetNc_ReturnsCorrect(bool isMutual, int length)
        {
            CommandApdu cmdApdu = GetCommandApdu(isMutual, true);

            int Nc = cmdApdu.Nc;

            Assert.Equal(length, Nc);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateCommandApdu_GetNe_ReturnsZero(bool isMutual)
        {
            CommandApdu cmdApdu = GetCommandApdu(isMutual, true);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Fact]
        public void CreateCommandApduSingle_GetData_ReturnsCorrect()
        {
            var expected = new List<byte>(
                new byte[12]
                {
                    0x7C, 0x0A, 0x82, 0x08, 0x54, 0xFE, 0xAA, 0x17, 0xAC, 0x05, 0x02, 0x36
                });

            CommandApdu cmdApdu = GetCommandApdu(false, true);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            bool compareResult = data.ToArray().SequenceEqual(expected);

            Assert.True(compareResult);
        }

        [Fact]
        public void CreateCommandApduMutual_GetData_ReturnsCorrect()
        {
            var expected = new List<byte>(
                new byte[24]
                {
                    0x7C, 0x16, 0x80, 0x08, 0xD0, 0xFE, 0x1A, 0x35, 0xA4, 0xE9, 0x40, 0xF8,
                    0x81, 0x08, 0xAC, 0x29, 0xA4, 0x5E, 0x1F, 0x42, 0x8A, 0x23, 0x82, 0x00
                }
            );

            CommandApdu cmdApdu = GetCommandApdu(true, true);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            bool compareResult = data.ToArray().SequenceEqual(expected);

            Assert.True(compareResult);
        }

        private static CommandApdu GetCommandApdu(bool isMutual, bool isRandomFixed)
        {
            CompleteAuthenticateManagementKeyCommand command = GetCommandObject(isMutual, isRandomFixed);

            return command.CreateCommandApdu();
        }

        private static CompleteAuthenticateManagementKeyCommand GetCommandObject(bool isMutual, bool isRandomFixed)
        {
            byte[] mgmtKey = GetMgmtKey();

            InitializeAuthenticateManagementKeyResponse response = GetInitResponse(isMutual);

            RandomObjectUtility? replacement = null;

            try
            {
                if (isRandomFixed)
                {
                    replacement = RandomObjectUtility.SetRandomProviderFixedBytes(GetFixedBytes());
                }

                return new CompleteAuthenticateManagementKeyCommand(response, mgmtKey);
            }
            finally
            {
                if (!(replacement is null))
                {
                    replacement.RestoreRandomProvider();
                }
            }
        }

        private static byte[] GetFixedBytes()
        {
            // This is 256 bytes so that other tests pass.
            // Because of threading and race conditions, it is possible other
            // tests will use a random object built with these bytes.
            // Currently, setting to 256 seems to prevent problems when the
            // threading race goes bad (this is because the maximum block size of
            // an RSA encryption/signature is 256).
            return new byte[256]
            {
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0xA4, 0xC4, 0xD9, 0x23, 0x74, 0x59, 0x7F, 0x64,
                0xA6, 0xD3, 0xCB, 0x2C, 0x10, 0xF0, 0xCD, 0x2D,
                0x57, 0xE9, 0x9F, 0x58, 0xC8, 0x57, 0x10, 0x6E,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
            };
        }

        private static byte[] GetMgmtKey() => new byte[]
        {
            0x8A, 0x98, 0xF1, 0x10, 0xD3, 0x49, 0x7B, 0x02,
            0x21, 0x00, 0xB7, 0x74, 0xDF, 0x0E, 0xF9, 0x9B,
            0x53, 0xEF, 0x4B, 0x8E, 0x3B, 0x91, 0x86, 0x04
        };

        private static InitializeAuthenticateManagementKeyResponse GetInitResponse(bool isMutualAuth)
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            byte tag1 = 0x81;
            if (isMutualAuth)
            {
                tag1 = 0x80;
            }

            var responseApdu = new ResponseApdu(
                new byte[]
                {
                    0x7C, 0x0A, tag1, 0x08, 0x39, 0xA0, 0xA8, 0xE9, 0xF5, 0x28, 0x87, 0x75, sw1, sw2
                });

            return new InitializeAuthenticateManagementKeyResponse(responseApdu);
        }
    }
}

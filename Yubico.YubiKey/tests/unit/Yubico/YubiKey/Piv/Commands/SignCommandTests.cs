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

namespace Yubico.YubiKey.Piv.Commands
{
    public class SignCommandTests
    {
        [Fact]
        public void ClassType_DerivedFromPivCommand_IsTrue()
        {
            byte[] digest = PivCommandResponseTestData.GetDigestData(PivAlgorithm.Rsa1024);
            var signCommand = new AuthenticateSignCommand(digest, 0x9A);

            Assert.True(signCommand is IYubiKeyCommand<AuthenticateSignResponse>);
        }

        [Fact]
        public void FullConstructor_NullDigestData_ThrowsException()
        {
            _ = Assert.Throws<ArgumentException>(() => _ = new AuthenticateSignCommand(null, 0x87));
        }

        [Theory]
        [InlineData(0x9B)]
        [InlineData(0x80)]
        [InlineData(0x81)]
        [InlineData(0x00)]
        [InlineData(0xF9)]
        [InlineData(0x99)]
        public void Constructor_BadSlotNumber_ThrowsException(byte slotNumber)
        {
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(slotNumber, PivAlgorithm.EccP256));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Constructor_BadDigestData_ThrowsException(int badKeyNum)
        {
            byte[] digest = GetBadDigestData(badKeyNum);
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateSignCommand(digest, 0x9A));
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            byte[] digest = PivCommandResponseTestData.GetDigestData(PivAlgorithm.Rsa2048);
            var command = new AuthenticateSignCommand(digest, 0x95);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(0x9A, PivAlgorithm.EccP256)]
        [InlineData(0x9C, PivAlgorithm.EccP384)]
        [InlineData(0x82, PivAlgorithm.Rsa1024)]
        [InlineData(0x83, PivAlgorithm.Rsa2048)]
        public void Constructor_Property_SlotNum(byte slotNumber, PivAlgorithm algorithm)
        {
            AuthenticateSignCommand command = GetCommandObject(slotNumber, algorithm);

            byte getSlotNum = command.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(PivAlgorithm algorithm)
        {
            CommandApdu cmdApdu = GetSignCommandApdu(0x8F, algorithm);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex87(PivAlgorithm algorithm)
        {
            CommandApdu cmdApdu = GetSignCommandApdu(0x90, algorithm);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0x87, Ins);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(PivAlgorithm algorithm)
        {
            CommandApdu cmdApdu = GetSignCommandApdu(0x91, algorithm);

            byte P1 = cmdApdu.P1;

            Assert.Equal((byte)algorithm, P1);
        }

        [Theory]
        [InlineData(0x9D, PivAlgorithm.EccP256)]
        [InlineData(0x9E, PivAlgorithm.EccP384)]
        [InlineData(0x92, PivAlgorithm.Rsa1024)]
        [InlineData(0x93, PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(byte slotNumber, PivAlgorithm algorithm)
        {
            CommandApdu cmdApdu = GetSignCommandApdu(slotNumber, algorithm);

            byte P2 = cmdApdu.P2;

            Assert.Equal(slotNumber, P2);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256, 38)]
        [InlineData(PivAlgorithm.EccP384, 54)]
        [InlineData(PivAlgorithm.Rsa1024, 136)]
        [InlineData(PivAlgorithm.Rsa2048, 266)]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(PivAlgorithm algorithm, int expected)
        {
            CommandApdu cmdApdu = GetSignCommandApdu(0x94, algorithm);

            int Nc = cmdApdu.Nc;

            Assert.Equal(expected, Nc);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa1024)]
        public void CreateCommandApdu_GetNeProperty_ReturnsZero(PivAlgorithm algorithm)
        {
            CommandApdu cmdApdu = GetSignCommandApdu(0x95, algorithm);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(PivAlgorithm.EccP256)]
        [InlineData(PivAlgorithm.EccP384)]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetData_ReturnsCorrect(PivAlgorithm algorithm)
        {
            byte[] prefix = GetDigestDataPrefix(algorithm);
            byte[] digest = PivCommandResponseTestData.GetDigestData(algorithm);
            var expected = new List<byte>(prefix);
            expected.AddRange(digest);

            CommandApdu cmdApdu = GetSignCommandApdu(0x85, algorithm);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            bool compareResult = data.Span.SequenceEqual(expected.ToArray());

            Assert.True(compareResult);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });

            AuthenticateSignCommand command = GetCommandObject(0x86, PivAlgorithm.EccP256);

            AuthenticateSignResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is AuthenticateSignResponse);
        }

        private static CommandApdu GetSignCommandApdu(byte slotNumber, PivAlgorithm algorithm)
        {
            AuthenticateSignCommand cmd = GetCommandObject(slotNumber, algorithm);

            return cmd.CreateCommandApdu();
        }

        private static AuthenticateSignCommand GetCommandObject(byte slotNumber, PivAlgorithm algorithm)
        {
            byte[] digest = PivCommandResponseTestData.GetDigestData(algorithm);
            var cmd = new AuthenticateSignCommand(digest, slotNumber);

            return cmd;
        }

        // Get some bad digest data.
        // if badDataNum is
        // 1 RSA-1024 with an extra byte
        // 2 RSA-1024 with a missing byte
        // 3 RSA-2048 with an extra byte
        // 4 RSA-2048 with a missing byte
        // 5 ECC-P256 with an extra byte
        // 6 ECC-P256 with a missing byte
        // 7 ECC-P384 with an extra byte
        // 8 ECC-P384 with a missing byte
        // Get regular data, and then if badDataNum is odd, add a byte, if even,
        // remove a byte.
        private static byte[] GetBadDigestData(int badDataNum)
        {
            byte[] digest;
            switch (badDataNum)
            {
                default:
                case 1:
                    digest = PivCommandResponseTestData.GetDigestData(PivAlgorithm.Rsa1024);
                    break;

                case 3:
                case 4:
                    digest = PivCommandResponseTestData.GetDigestData(PivAlgorithm.Rsa2048);
                    break;

                case 5:
                case 6:
                    digest = PivCommandResponseTestData.GetDigestData(PivAlgorithm.EccP256);
                    break;

                case 7:
                case 8:
                    digest = PivCommandResponseTestData.GetDigestData(PivAlgorithm.EccP384);
                    break;
            }

            if ((badDataNum & 1) != 0)
            {
                Array.Resize<byte>(ref digest, digest.Length + 1);
                digest[^1] = 0x44;
            }
            else
            {
                Array.Resize<byte>(ref digest, digest.Length - 1);
            }

            return digest;
        }

        // Get the TL TL TL prefix for each algorithm.
        private static byte[] GetDigestDataPrefix(PivAlgorithm algorithm) => algorithm switch
        {
            PivAlgorithm.Rsa2048 => new byte[] { 0x7C, 0x82, 0x01, 0x06, 0x82, 0x00, 0x81, 0x82, 0x01, 0x00 },
            PivAlgorithm.EccP256 => new byte[] { 0x7C, 0x24, 0x82, 0x00, 0x81, 0x20 },
            PivAlgorithm.EccP384 => new byte[] { 0x7C, 0x34, 0x82, 0x00, 0x81, 0x30 },
            _ => new byte[] { 0x7C, 0x81, 0x85, 0x82, 0x00, 0x81, 0x81, 0x80 },
        };
    }
}


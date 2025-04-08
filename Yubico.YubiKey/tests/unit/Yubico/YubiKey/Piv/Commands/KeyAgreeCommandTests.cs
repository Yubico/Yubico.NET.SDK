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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands
{
    public class KeyAgreeCommandTests
    {
        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        public void ClassType_DerivedFromPivCommand_IsTrue(
            KeyType keyType)
        {
            byte[] pubKey = GetPublicKey(keyType);
            var command = new AuthenticateKeyAgreeCommand(pubKey, 0x9A, keyType.GetPivAlgorithm());

            Assert.True(command is IYubiKeyCommand<AuthenticateKeyAgreeResponse>);
        }

        [Fact]
        [Obsolete("Obsolete")]
        public void FullConstructor_NullData_ThrowsException()
        {
            _ = Assert.Throws<ArgumentException>(() =>
                new AuthenticateKeyAgreeCommand(null, 0x87));
        }

        [Fact]
        [Obsolete("Obsolete")]
        public void InitConstructor_NullData_ThrowsException()
        {
            _ = Assert.Throws<ArgumentException>(() =>
                new AuthenticateKeyAgreeCommand(null, 0x9A));
        }

        [Theory]
        [InlineData(0x9B, KeyType.ECP256)]
        [InlineData(0x80, KeyType.ECP384)]
        [InlineData(0x81, KeyType.ECP256)]
        [InlineData(0x00, KeyType.ECP384)]
        [InlineData(0xF9, KeyType.ECP256)]
        [InlineData(0x99, KeyType.ECP384)]
        public void Constructor_BadSlotNumber_ThrowsException(
            byte slotNumber,
            KeyType keyType)
        {
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(slotNumber, keyType));
        }


        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        [Obsolete("Obsolete")]
        public void Constructor_BadData_ThrowsException(
            int badFlag)
        {
            byte[] pubKey = GetPublicKey(KeyType.ECP256);
            if (badFlag >= 0)
            {
                Array.Resize<byte>(ref pubKey, pubKey.Length + 1);
                pubKey[^1] = 0x44;
            }
            else
            {
                Array.Resize<byte>(ref pubKey, pubKey.Length - 1);
            }

            _ = Assert.Throws<ArgumentException>(() =>
                new AuthenticateKeyAgreeCommand(pubKey, 0x9A));
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            byte[] pubKey = GetPublicKey(KeyType.ECP256);
            var command = new AuthenticateKeyAgreeCommand(pubKey, 0x90, KeyType.ECP256.GetPivAlgorithm());

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(0x82, KeyType.ECP256)]
        [InlineData(0x83, KeyType.ECP384)]
        public void Constructor_Property_SlotNum(
            byte slotNumber,
            KeyType keyType)
        {
            AuthenticateKeyAgreeCommand command = GetCommandObject(slotNumber, keyType);

            byte getSlotNum = command.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(
            KeyType keyType)
        {
            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(0x86, keyType);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.ECP256)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex87(
            KeyType keyType)
        {
            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(0x90, keyType);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0x87, Ins);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(
            KeyType keyType)
        {
            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(0x91, keyType);

            byte P1 = cmdApdu.P1;

            Assert.Equal((byte)keyType.GetPivAlgorithm(), P1);
        }

        [Theory]
        [InlineData(0x93, KeyType.ECP384)]
        [InlineData(0x9E, KeyType.ECP256)]
        public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(
            byte slotNumber,
            KeyType keyType)
        {
            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(slotNumber, keyType);

            byte P2 = cmdApdu.P2;

            Assert.Equal(slotNumber, P2);
        }

        [Theory]
        [InlineData(KeyType.ECP384, 103)]
        [InlineData(KeyType.ECP256, 71)]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(
            KeyType keyType,
            int expected)
        {
            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(0x94, keyType);

            int Nc = cmdApdu.Nc;

            Assert.Equal(expected, Nc);
        }

        [Theory]
        [InlineData(KeyType.ECP384)]
        [InlineData(KeyType.ECP256)]
        public void CreateCommandApdu_GetNeProperty_ReturnsZero(
            KeyType keyType)
        {
            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(0x95, keyType);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(KeyType.ECP256)]
        [InlineData(KeyType.ECP384)]
        public void CreateCommandApdu_GetData_ReturnsCorrect(
            KeyType keyType)
        {
            byte[] prefix = GetKeyAgreeDataPrefix(keyType);
            byte[] pubKey = GetPublicKey(keyType);
            var expected = new List<byte>(prefix);
            expected.AddRange(pubKey);

            CommandApdu cmdApdu = GetKeyAgreeCommandApdu(0x9C, keyType);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            bool compareResult = data.ToArray().SequenceEqual(expected);

            Assert.True(compareResult);
        }

        private static CommandApdu GetKeyAgreeCommandApdu(
            byte slotNumber,
            KeyType keyType)
        {
            AuthenticateKeyAgreeCommand cmd = GetCommandObject(slotNumber, keyType);

            return cmd.CreateCommandApdu();
        }

        private static AuthenticateKeyAgreeCommand GetCommandObject(
            byte slotNumber,
            KeyType keyType)
        {
            byte[] pubKey = GetPublicKey(keyType);
            var cmd = new AuthenticateKeyAgreeCommand(pubKey, slotNumber, keyType.GetPivAlgorithm());

            return cmd;
        }

        private static byte[] GetPublicKey(
            KeyType keyType) => keyType switch
        {
            KeyType.ECP384 => new byte[]
            {
                0x04,
                0xD3, 0x8F, 0x39, 0xCF, 0x24, 0x39, 0x67, 0x3A, 0xD8, 0xCB, 0x44, 0xE7, 0xB4, 0x7F, 0x3D, 0xD4,
                0x68, 0xE8, 0x6B, 0x83, 0x65, 0xA7, 0x2B, 0x8C, 0xFE, 0x36, 0x9D, 0xE1, 0x15, 0x94, 0x26, 0xA0,
                0x6F, 0x3D, 0xBC, 0x4B, 0x97, 0x16, 0x5E, 0x07, 0x89, 0xF3, 0x9D, 0xB4, 0xBC, 0x84, 0x4B, 0xE9,
                0xB3, 0x43, 0x14, 0x0F, 0x31, 0xE7, 0xE1, 0xF0, 0xB4, 0xF8, 0x75, 0xC1, 0xB7, 0x9E, 0xF9, 0x6A,
                0x2D, 0xBC, 0x3A, 0xF8, 0x2F, 0x84, 0x4D, 0xFC, 0x42, 0x27, 0x21, 0xF1, 0x23, 0x13, 0x50, 0xEA,
                0x96, 0x05, 0x47, 0x7C, 0xBF, 0x0C, 0x97, 0x46, 0x6B, 0x1D, 0xA6, 0x5F, 0x80, 0xB9, 0x7B, 0x89
            },

            _ => new byte[]
            {
                0x04,
                0x11, 0x17, 0xB4, 0x11, 0xEE, 0x45, 0xD4, 0x1E, 0xB9, 0x75, 0x92, 0x55, 0x34, 0xE6, 0x2B, 0x1F,
                0x8A, 0x49, 0x20, 0x48, 0xAD, 0xE4, 0xD0, 0xF4, 0x2C, 0xDC, 0xF5, 0x80, 0xB7, 0x25, 0x49, 0x83,
                0xC5, 0xCD, 0x5B, 0x80, 0x0D, 0x9A, 0xBE, 0x1F, 0x1C, 0x57, 0x80, 0x83, 0xDA, 0x2E, 0x0A, 0x60,
                0xAD, 0x0E, 0xA2, 0x29, 0x9C, 0xD5, 0x82, 0x1A, 0x8C, 0x03, 0x4D, 0x87, 0x72, 0x66, 0x59, 0x94,
            },
        };

        // Get the TL TL TL prefix for each keyType.
        private static byte[] GetKeyAgreeDataPrefix(
            KeyType keyType) => keyType switch
        {
            KeyType.ECP384 => new byte[] { 0x7C, 0x65, 0x82, 0x00, 0x85, 0x61 },
            _ => new byte[] { 0x7C, 0x45, 0x82, 0x00, 0x85, 0x41 },
        };
    }
}

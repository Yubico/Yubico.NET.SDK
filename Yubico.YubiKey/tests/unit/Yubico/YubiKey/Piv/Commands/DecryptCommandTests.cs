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
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.Piv.Commands
{
    public class DecryptCommandTests
    {
        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        [InlineData(KeyType.RSA2048)]
        public void ClassType_DerivedFromPivCommand_IsTrue(KeyType keyType)
        {
            byte[] dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(keyType);
            var decryptCommand = new AuthenticateDecryptCommand(dataToDecrypt, 0x85);

            Assert.True(decryptCommand is IYubiKeyCommand<AuthenticateDecryptResponse>);
        }

        [Fact]
        public void FullConstructor_NullData_ThrowsException()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateDecryptCommand(null, 0x87));
#pragma warning restore CS8625
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
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(slotNumber, KeyType.RSA2048));
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        public void Constructor_BadData_ThrowsException(int badFlag)
        {
            byte[] dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(KeyType.RSA2048);
            if (badFlag >= 0)
            {
                Array.Resize<byte>(ref dataToDecrypt, dataToDecrypt.Length + 1);
                dataToDecrypt[^1] = 0x44;
            }
            else
            {
                Array.Resize<byte>(ref dataToDecrypt, dataToDecrypt.Length - 1);
            }

#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateDecryptCommand(dataToDecrypt, 0x9A));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            byte[] dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(KeyType.RSA1024);
            var command = new AuthenticateDecryptCommand(dataToDecrypt, 0x90);

            YubiKeyApplication application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(0x82, KeyType.RSA1024)]
        [InlineData(0x83, KeyType.RSA2048)]
        public void Constructor_Property_SlotNum(byte slotNumber, KeyType keyType)
        {
            AuthenticateDecryptCommand command = GetCommandObject(slotNumber, keyType);

            byte getSlotNum = command.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(KeyType keyType)
        {
            CommandApdu cmdApdu = GetDecryptCommandApdu(0x86, keyType);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        [InlineData(KeyType.RSA3072)]
        [InlineData(KeyType.RSA4096)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex87(KeyType keyType)
        {
            CommandApdu cmdApdu = GetDecryptCommandApdu(0x90, keyType);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0x87, Ins);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(KeyType expectedAlgorithm)
        {
            CommandApdu cmdApdu = GetDecryptCommandApdu(0x91, expectedAlgorithm);

            byte P1 = cmdApdu.P1;

            Assert.Equal((byte)expectedAlgorithm.GetPivAlgorithm(), P1);
        }

        [Theory]
        [InlineData(0x92, KeyType.RSA1024)]
        [InlineData(0x9E, KeyType.RSA2048)]
        public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(byte slotNumber, KeyType keyType)
        {
            CommandApdu cmdApdu = GetDecryptCommandApdu(slotNumber, keyType);

            byte P2 = cmdApdu.P2;

            Assert.Equal(slotNumber, P2);
        }

        [Theory]
        [InlineData(KeyType.RSA1024, 136)]
        [InlineData(KeyType.RSA2048, 266)]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(KeyType keyType, int expected)
        {
            CommandApdu cmdApdu = GetDecryptCommandApdu(0x94, keyType);

            int Nc = cmdApdu.Nc;

            Assert.Equal(expected, Nc);
        }

        [Theory]
        [InlineData(KeyType.RSA1024)]
        [InlineData(KeyType.RSA2048)]
        public void CreateCommandApdu_GetNeProperty_ReturnsZero(KeyType keyType)
        {
            CommandApdu cmdApdu = GetDecryptCommandApdu(0x95, keyType);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(KeyType.RSA2048)]
        public void CreateCommandApdu_GetData_ReturnsCorrect(KeyType keyType)
        {
            byte[] prefix = GetDecryptDataPrefix(keyType);
            byte[] encryptedData = PivCommandResponseTestData.GetEncryptedBlock(keyType);

            var expected = new List<byte>(prefix);
            expected.AddRange(encryptedData);

            CommandApdu cmdApdu = GetDecryptCommandApdu(0x9C, keyType);

            var result = cmdApdu.Data;
            Assert.Equal(expected.ToArray(), result);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });

            AuthenticateDecryptCommand command = GetCommandObject(0x86, KeyType.RSA1024);

            AuthenticateDecryptResponse response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is AuthenticateDecryptResponse);
        }

        private static CommandApdu GetDecryptCommandApdu(byte slotNumber, KeyType keyType)
        {
            AuthenticateDecryptCommand cmd = GetCommandObject(slotNumber, keyType);

            return cmd.CreateCommandApdu();
        }

        private static AuthenticateDecryptCommand GetCommandObject(byte slotNumber, KeyType keyType)
        {
            byte[] dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(keyType);
            var cmd = new AuthenticateDecryptCommand(dataToDecrypt, slotNumber);

            return cmd;
        }

        // Get the TL TL TL prefix for each keyType.
        private static byte[] GetDecryptDataPrefix(KeyType keyType) => keyType switch
        {
            KeyType.RSA2048 => new byte[] {
                0x7C, 0x82, 0x01, 0x06, 0x82, 0x00, 0x81, 0x82, 0x01, 0x00
            },
            _ => new byte[] {
                0x7C, 0x81, 0x85, 0x82, 0x00, 0x81, 0x81, 0x80
            },
        };
    }
}

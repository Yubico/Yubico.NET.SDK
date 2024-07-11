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
    public class DecryptCommandTests
    {
        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        [InlineData(PivAlgorithm.Rsa2048)]
        public void ClassType_DerivedFromPivCommand_IsTrue(PivAlgorithm algorithm)
        {
            var dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(algorithm);
            var decryptCommand = new AuthenticateDecryptCommand(dataToDecrypt, slotNumber: 0x85);

            Assert.True(decryptCommand is IYubiKeyCommand<AuthenticateDecryptResponse>);
        }

        [Fact]
        public void FullConstructor_NullData_ThrowsException()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentException>(() =>
                new AuthenticateDecryptCommand(dataToDecrypt: null, slotNumber: 0x87));
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
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(slotNumber, PivAlgorithm.Rsa2048));
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(1)]
        [InlineData(-1)]
        public void Constructor_BadData_ThrowsException(int badFlag)
        {
            var dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(PivAlgorithm.Rsa2048);
            if (badFlag >= 0)
            {
                Array.Resize(ref dataToDecrypt, dataToDecrypt.Length + 1);
                dataToDecrypt[^1] = 0x44;
            }
            else
            {
                Array.Resize(ref dataToDecrypt, dataToDecrypt.Length - 1);
            }

#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentException>(() => new AuthenticateDecryptCommand(dataToDecrypt, slotNumber: 0x9A));
#pragma warning restore CS8625
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            var dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(PivAlgorithm.Rsa1024);
            var command = new AuthenticateDecryptCommand(dataToDecrypt, slotNumber: 0x90);

            var application = command.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);
        }

        [Theory]
        [InlineData(0x82, PivAlgorithm.Rsa1024)]
        [InlineData(0x83, PivAlgorithm.Rsa2048)]
        public void Constructor_Property_SlotNum(byte slotNumber, PivAlgorithm algorithm)
        {
            var command = GetCommandObject(slotNumber, algorithm);

            var getSlotNum = command.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(PivAlgorithm algorithm)
        {
            var cmdApdu = GetDecryptCommandApdu(slotNumber: 0x86, algorithm);

            var Cla = cmdApdu.Cla;

            Assert.Equal(expected: 0, Cla);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        [InlineData(PivAlgorithm.Rsa3072)]
        [InlineData(PivAlgorithm.Rsa4096)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHex87(PivAlgorithm algorithm)
        {
            var cmdApdu = GetDecryptCommandApdu(slotNumber: 0x90, algorithm);

            var Ins = cmdApdu.Ins;

            Assert.Equal(expected: 0x87, Ins);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(PivAlgorithm expectedAlgorithm)
        {
            var cmdApdu = GetDecryptCommandApdu(slotNumber: 0x91, expectedAlgorithm);

            var P1 = cmdApdu.P1;

            Assert.Equal((byte)expectedAlgorithm, P1);
        }

        [Theory]
        [InlineData(0x92, PivAlgorithm.Rsa1024)]
        [InlineData(0x9E, PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(byte slotNumber, PivAlgorithm algorithm)
        {
            var cmdApdu = GetDecryptCommandApdu(slotNumber, algorithm);

            var P2 = cmdApdu.P2;

            Assert.Equal(slotNumber, P2);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024, 136)]
        [InlineData(PivAlgorithm.Rsa2048, 266)]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(PivAlgorithm algorithm, int expected)
        {
            var cmdApdu = GetDecryptCommandApdu(slotNumber: 0x94, algorithm);

            var Nc = cmdApdu.Nc;

            Assert.Equal(expected, Nc);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa1024)]
        [InlineData(PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetNeProperty_ReturnsZero(PivAlgorithm algorithm)
        {
            var cmdApdu = GetDecryptCommandApdu(slotNumber: 0x95, algorithm);

            var Ne = cmdApdu.Ne;

            Assert.Equal(expected: 0, Ne);
        }

        [Theory]
        [InlineData(PivAlgorithm.Rsa2048)]
        public void CreateCommandApdu_GetData_ReturnsCorrect(PivAlgorithm algorithm)
        {
            var prefix = GetDecryptDataPrefix(algorithm);
            var encryptedData = PivCommandResponseTestData.GetEncryptedBlock(algorithm);

            var expected = new List<byte>(prefix);
            expected.AddRange(encryptedData);

            var cmdApdu = GetDecryptCommandApdu(slotNumber: 0x9C, algorithm);

            var result = cmdApdu.Data;
            Assert.Equal(expected.ToArray(), result);
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            var responseApdu = new ResponseApdu(new byte[] { 0x90, 0x00 });

            var command = GetCommandObject(slotNumber: 0x86, PivAlgorithm.Rsa1024);

            var response = command.CreateResponseForApdu(responseApdu);

            Assert.True(response is AuthenticateDecryptResponse);
        }

        private static CommandApdu GetDecryptCommandApdu(byte slotNumber, PivAlgorithm algorithm)
        {
            var cmd = GetCommandObject(slotNumber, algorithm);

            return cmd.CreateCommandApdu();
        }

        private static AuthenticateDecryptCommand GetCommandObject(byte slotNumber, PivAlgorithm algorithm)
        {
            var dataToDecrypt = PivCommandResponseTestData.GetEncryptedBlock(algorithm);
            var cmd = new AuthenticateDecryptCommand(dataToDecrypt, slotNumber);

            return cmd;
        }

        // Get the TL TL TL prefix for each algorithm.
        private static byte[] GetDecryptDataPrefix(PivAlgorithm algorithm)
        {
            return algorithm switch
            {
                PivAlgorithm.Rsa2048 => new byte[]
                {
                    0x7C, 0x82, 0x01, 0x06, 0x82, 0x00, 0x81, 0x82, 0x01, 0x00
                },
                _ => new byte[]
                {
                    0x7C, 0x81, 0x85, 0x82, 0x00, 0x81, 0x81, 0x80
                }
            };
        }
    }
}

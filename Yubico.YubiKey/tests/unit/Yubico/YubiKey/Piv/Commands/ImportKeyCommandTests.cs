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
    [Obsolete("Replaced by KeyParameters")]
    public class ImportKeyCommandTestsObsolete
    {
        [Theory]
        [InlineData(1, KeyType.P256)]
        [InlineData(2, KeyType.P384)]
        [InlineData(3, KeyType.RSA1024)]
        [InlineData(4, KeyType.RSA2048)]
        public void ClassType_DerivedFromPivCommand_IsTrue(int cStyle, KeyType keyType)
        {
            ImportAsymmetricKeyCommand cmd = GetCommandObject(
                cStyle,
                PivSlot.Retired5,
                keyType,
                PivPinPolicy.Always,
                PivTouchPolicy.Never);

            Assert.True(cmd is IYubiKeyCommand<ImportAsymmetricKeyResponse>);
        }

        [Fact]
        public void FullConstructor_NullKeyData_ThrowsException()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new ImportAsymmetricKeyCommand(
                null,
                0x87,
                PivPinPolicy.Always,
                PivTouchPolicy.Never));
#pragma warning restore CS8625
        }

        [Fact]
        public void InitConstructor_NullKeyData_ThrowsException()
        {
#pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
            _ = Assert.Throws<ArgumentNullException>(() => new ImportAsymmetricKeyCommand(null)
            {
                SlotNumber = 0x87,
                PinPolicy = PivPinPolicy.Always,
                TouchPolicy = PivTouchPolicy.Never,
            });
#pragma warning restore CS8625
        }

        [Theory]
        [InlineData(1, 0x9B)]
        [InlineData(2, 0x80)]
        [InlineData(3, 0x81)]
        [InlineData(4, 0x00)]
        [InlineData(5, 0x96)]
        [InlineData(6, 0xF8)]
        [InlineData(7, 0xFA)]
        [InlineData(8, 0x99)]
        [InlineData(9, 0x9F)]
        public void Constructor_BadSlotNumber_ThrowsException(int cStyle, byte slotNumber)
        {
            _ = Assert.Throws<ArgumentException>(() => GetCommandObject(
                cStyle,
                slotNumber,
                KeyType.P256,
                PivPinPolicy.Once,
                PivTouchPolicy.Cached));
        }

        [Fact]
        public void Constructor_NoSlotNumber_ThrowsException()
        {
            PivPrivateKey keyData = GetKeyData(KeyType.P256);
            var cmd = new ImportAsymmetricKeyCommand(keyData)
            {
                PinPolicy = PivPinPolicy.Always,
                TouchPolicy = PivTouchPolicy.Never,
            };
            _ = Assert.Throws<InvalidOperationException>(() => cmd.CreateCommandApdu());
        }

        [Fact]
        public void Constructor_Application_Piv()
        {
            PivPrivateKey keyData = GetKeyData(KeyType.P256);
            var importKeyCommand = new ImportAsymmetricKeyCommand(
                keyData,
                PivSlot.Retired19,
                PivPinPolicy.Never,
                PivTouchPolicy.Never);

            YubiKeyApplication application = importKeyCommand.Application;

            Assert.Equal(YubiKeyApplication.Piv, application);

            keyData.Clear();
        }

        [Fact]
        public void Constructor_Property_SlotNum()
        {
            PivPrivateKey keyData = GetKeyData(KeyType.P256);
            byte slotNumber = PivSlot.Retired20;
            PivPinPolicy pinPolicy = PivPinPolicy.Always;
            PivTouchPolicy touchPolicy = PivTouchPolicy.Cached;
            var importKeyCommand = new ImportAsymmetricKeyCommand(
                keyData,
                slotNumber,
                pinPolicy,
                touchPolicy);

            byte getSlotNum = importKeyCommand.SlotNumber;

            Assert.Equal(slotNumber, getSlotNum);

            keyData.Clear();
        }

        [Fact]
        public void Constructor_Property_PinPolicy()
        {
            PivPrivateKey keyData = GetKeyData(KeyType.P256);
            byte slotNumber = PivSlot.Retired20;
            PivPinPolicy pinPolicy = PivPinPolicy.Always;
            PivTouchPolicy touchPolicy = PivTouchPolicy.Cached;
            var importKeyCommand = new ImportAsymmetricKeyCommand(
                keyData,
                slotNumber,
                pinPolicy,
                touchPolicy);

            PivPinPolicy getPolicy = importKeyCommand.PinPolicy;

            Assert.Equal(pinPolicy, getPolicy);

            keyData.Clear();
        }

        [Fact]
        public void Constructor_Property_TouchPolicy()
        {
            PivPrivateKey keyData = GetKeyData(KeyType.P256);
            byte slotNumber = PivSlot.Retired20;
            PivPinPolicy pinPolicy = PivPinPolicy.Always;
            PivTouchPolicy touchPolicy = PivTouchPolicy.Cached;
            var importKeyCommand = new ImportAsymmetricKeyCommand(
                keyData,
                slotNumber,
                pinPolicy,
                touchPolicy);

            PivTouchPolicy getPolicy = importKeyCommand.TouchPolicy;

            Assert.Equal(touchPolicy, getPolicy);

            keyData.Clear();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle, 0x94, KeyType.P256, PivPinPolicy.Default, PivTouchPolicy.Never);

            byte Cla = cmdApdu.Cla;

            Assert.Equal(0, Cla);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetInsProperty_ReturnsHexFE(int cStyle)
        {
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                0x92,
                KeyType.RSA1024,
                PivPinPolicy.Once,
                PivTouchPolicy.None);

            byte Ins = cmdApdu.Ins;

            Assert.Equal(0xFE, Ins);
        }

        [Theory]
        [InlineData(1, KeyType.P256)]
        [InlineData(2, KeyType.P384)]
        [InlineData(3, KeyType.RSA1024)]
        [InlineData(4, KeyType.RSA2048)]
        public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(int cStyle, KeyType keyType)
        {
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                0x9E,
                keyType,
                PivPinPolicy.None,
                PivTouchPolicy.Cached);

            byte P1 = cmdApdu.P1;

            Assert.Equal((byte)keyType.GetPivAlgorithm(), P1);
        }

        [Theory]
        [InlineData(1, 0x95)]
        [InlineData(2, 0x82)]
        [InlineData(3, 0x83)]
        [InlineData(4, 0x84)]
        [InlineData(5, 0x85)]
        [InlineData(6, 0x86)]
        [InlineData(7, 0x87)]
        [InlineData(8, 0x88)]
        [InlineData(9, 0x89)]
        public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(int cStyle, byte slotNumber)
        {
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                slotNumber,
                KeyType.RSA2048,
                PivPinPolicy.None,
                PivTouchPolicy.Cached);

            byte P2 = cmdApdu.P2;

            Assert.Equal(slotNumber, P2);
        }

        [Theory]
        [InlineData(1, KeyType.RSA1024, PivPinPolicy.None, PivTouchPolicy.None, 0)]
        [InlineData(2, KeyType.RSA2048, PivPinPolicy.None, PivTouchPolicy.Default, 0)]
        [InlineData(3, KeyType.P256, PivPinPolicy.None, PivTouchPolicy.Never, 3)]
        [InlineData(1, KeyType.P384, PivPinPolicy.None, PivTouchPolicy.Always, 3)]
        [InlineData(2, KeyType.RSA1024, PivPinPolicy.None, PivTouchPolicy.Cached, 3)]
        [InlineData(3, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.None, 0)]
        [InlineData(1, KeyType.P256, PivPinPolicy.Default, PivTouchPolicy.Default, 0)]
        [InlineData(2, KeyType.P384, PivPinPolicy.Default, PivTouchPolicy.Never, 3)]
        [InlineData(3, KeyType.RSA1024, PivPinPolicy.Default, PivTouchPolicy.Always, 3)]
        [InlineData(1, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.Cached, 3)]
        [InlineData(2, KeyType.P256, PivPinPolicy.Never, PivTouchPolicy.None, 3)]
        [InlineData(3, KeyType.P384, PivPinPolicy.Never, PivTouchPolicy.Default, 3)]
        [InlineData(1, KeyType.RSA1024, PivPinPolicy.Never, PivTouchPolicy.Never, 6)]
        [InlineData(2, KeyType.RSA2048, PivPinPolicy.Never, PivTouchPolicy.Always, 6)]
        [InlineData(3, KeyType.P256, PivPinPolicy.Never, PivTouchPolicy.Cached, 6)]
        [InlineData(1, KeyType.P384, PivPinPolicy.Once, PivTouchPolicy.None, 3)]
        [InlineData(2, KeyType.RSA1024, PivPinPolicy.Once, PivTouchPolicy.Default, 3)]
        [InlineData(3, KeyType.RSA2048, PivPinPolicy.Once, PivTouchPolicy.Never, 6)]
        [InlineData(1, KeyType.P256, PivPinPolicy.Once, PivTouchPolicy.Always, 6)]
        [InlineData(2, KeyType.P384, PivPinPolicy.Once, PivTouchPolicy.Cached, 6)]
        [InlineData(3, KeyType.RSA1024, PivPinPolicy.Always, PivTouchPolicy.None, 3)]
        [InlineData(1, KeyType.RSA2048, PivPinPolicy.Always, PivTouchPolicy.Default, 3)]
        [InlineData(2, KeyType.P256, PivPinPolicy.Always, PivTouchPolicy.Never, 6)]
        [InlineData(3, KeyType.P384, PivPinPolicy.Always, PivTouchPolicy.Always, 6)]
        [InlineData(1, KeyType.RSA1024, PivPinPolicy.Always, PivTouchPolicy.Cached, 6)]
        public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(
            int cStyle,
            KeyType keyType,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy,
            int expectedPolicyLength)
        {
            PivPrivateKey keyData = GetKeyData(keyType);
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                0x8E,
                keyType,
                pinPolicy,
                touchPolicy);

            int Nc = cmdApdu.Nc;

            int expectedLength = keyData.EncodedPrivateKey.Length + expectedPolicyLength;
            Assert.Equal(expectedLength, Nc);

            keyData.Clear();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void CreateCommandApdu_GetNeProperty_ReturnsZero(int cStyle)
        {
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                0x8F,
                KeyType.P256,
                PivPinPolicy.Always,
                PivTouchPolicy.Never);

            int Ne = cmdApdu.Ne;

            Assert.Equal(0, Ne);
        }

        [Theory]
        [InlineData(1, KeyType.RSA1024)]
        [InlineData(2, KeyType.RSA2048)]
        [InlineData(3, KeyType.P256)]
        [InlineData(4, KeyType.P384)]
        public void CreateCommandApdu_GetData_ReturnsKeyData(int cStyle, KeyType keyType)
        {
            PivPrivateKey keyData = GetKeyData(keyType);
            // Use Default policies so the only data will be the key data.
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                0x8F,
                keyType,
                PivPinPolicy.Default,
                PivTouchPolicy.Default);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            bool compareResult = data.Span.SequenceEqual(keyData.EncodedPrivateKey.Span);

            Assert.True(compareResult);

            keyData.Clear();
        }

        [Theory]
        [InlineData(1, PivPinPolicy.None, PivTouchPolicy.None)]
        [InlineData(2, PivPinPolicy.None, PivTouchPolicy.Default)]
        [InlineData(3, PivPinPolicy.None, PivTouchPolicy.Never)]
        [InlineData(1, PivPinPolicy.None, PivTouchPolicy.Cached)]
        [InlineData(2, PivPinPolicy.None, PivTouchPolicy.Always)]
        [InlineData(3, PivPinPolicy.Default, PivTouchPolicy.None)]
        [InlineData(1, PivPinPolicy.Default, PivTouchPolicy.Default)]
        [InlineData(2, PivPinPolicy.Default, PivTouchPolicy.Never)]
        [InlineData(3, PivPinPolicy.Default, PivTouchPolicy.Cached)]
        [InlineData(1, PivPinPolicy.Default, PivTouchPolicy.Always)]
        [InlineData(2, PivPinPolicy.Never, PivTouchPolicy.None)]
        [InlineData(3, PivPinPolicy.Never, PivTouchPolicy.Default)]
        [InlineData(1, PivPinPolicy.Never, PivTouchPolicy.Never)]
        [InlineData(2, PivPinPolicy.Never, PivTouchPolicy.Cached)]
        [InlineData(3, PivPinPolicy.Never, PivTouchPolicy.Always)]
        [InlineData(1, PivPinPolicy.Once, PivTouchPolicy.None)]
        [InlineData(2, PivPinPolicy.Once, PivTouchPolicy.Default)]
        [InlineData(3, PivPinPolicy.Once, PivTouchPolicy.Never)]
        [InlineData(1, PivPinPolicy.Once, PivTouchPolicy.Cached)]
        [InlineData(2, PivPinPolicy.Once, PivTouchPolicy.Always)]
        [InlineData(3, PivPinPolicy.Always, PivTouchPolicy.None)]
        [InlineData(1, PivPinPolicy.Always, PivTouchPolicy.Default)]
        [InlineData(2, PivPinPolicy.Always, PivTouchPolicy.Never)]
        [InlineData(3, PivPinPolicy.Always, PivTouchPolicy.Cached)]
        [InlineData(1, PivPinPolicy.Always, PivTouchPolicy.Always)]
        [InlineData(4, PivPinPolicy.None, PivTouchPolicy.Never)]
        [InlineData(5, PivPinPolicy.None, PivTouchPolicy.Cached)]
        [InlineData(6, PivPinPolicy.Once, PivTouchPolicy.None)]
        [InlineData(7, PivPinPolicy.Always, PivTouchPolicy.None)]
        [InlineData(8, PivPinPolicy.Default, PivTouchPolicy.Default)]
        [InlineData(9, PivPinPolicy.Default, PivTouchPolicy.Default)]
        public void CreateCommandApdu_GetData_ReturnsPolicy(
            int cStyle,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy)
        {
            PivPrivateKey keyData = GetKeyData(KeyType.P256);
            byte[] pinData = new byte[] { 0xAA, 0x01, (byte)pinPolicy };
            byte[] touchData = new byte[] { 0xAB, 0x01, (byte)touchPolicy };
            var expected = new List<byte>(keyData.EncodedPrivateKey.ToArray());
            if (pinPolicy != PivPinPolicy.None && pinPolicy != PivPinPolicy.Default)
            {
                expected.AddRange(pinData);
            }
            if (touchPolicy != PivTouchPolicy.None && touchPolicy != PivTouchPolicy.Default)
            {
                expected.AddRange(touchData);
            }
            CommandApdu cmdApdu = GetImportKeyCommandApdu(
                cStyle,
                0x8F,
                KeyType.P256,
                pinPolicy,
                touchPolicy);

            ReadOnlyMemory<byte> data = cmdApdu.Data;

            Assert.False(data.IsEmpty);
            if (data.IsEmpty)
            {
                return;
            }

            bool compareResult = data.Span.SequenceEqual(expected.ToArray());

            Assert.True(compareResult);

            keyData.Clear();
        }

        [Fact]
        public void CreateResponseForApdu_ReturnsCorrectType()
        {
            byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
            byte sw2 = unchecked((byte)SWConstants.Success);
            var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });

            PivPrivateKey keyData = GetKeyData(KeyType.P384);
            var importKeyCommand = new ImportAsymmetricKeyCommand(
                keyData,
                PivSlot.Retired18,
                PivPinPolicy.Default,
                PivTouchPolicy.Default);

            ImportAsymmetricKeyResponse response = importKeyCommand.CreateResponseForApdu(responseApdu);

            Assert.True(response is ImportAsymmetricKeyResponse);

            keyData.Clear();
        }

        // The constructorStyle is either 1, meaning construct the
        // ImportAsymmetricKeyCommand using the full constructor, or anything
        // other than 1, meaning use the object initializer constructor.
        private static CommandApdu GetImportKeyCommandApdu(
            int cStyle,
            byte slotNumber,
            KeyType keyType,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy)
        {
            ImportAsymmetricKeyCommand importKeyCommand = GetCommandObject(
                cStyle,
                slotNumber,
                keyType,
                pinPolicy,
                touchPolicy);

            return importKeyCommand.CreateCommandApdu();
        }

        // Construct a GenerateKeyPairCommand using the style specified.
        // If the style arg is 1, this will build using the full constructor.
        // If it is 2, it will build it using object initializer constructor.
        // If it is 3, create it using the empty constructor and set the
        // properties later.
        // If it is 4, create it using the object initializer constructor but
        // don't set the PinPolicy (it should be default).
        // If it is 5, create it using the empty constructor and set the
        // properties later, except don't set the PinPolicy.
        // If it is 6, create it using the object initializer constructor but
        // don't set the TouchPolicy (it should be default).
        // If it is 7, create it using the empty constructor and set the
        // properties later, except don't set the TouchPolicy.
        // If it is 8, create it using the object initializer constructor but
        // don't set the PinPolicy or the TouchPolicy.
        // If it is 9, create it using the empty constructor and set the
        // properties later, except don't set the PinPolicy or the TouchPolicy.
        private static ImportAsymmetricKeyCommand GetCommandObject(
            int cStyle,
            byte slotNumber,
            KeyType keyType,
            PivPinPolicy pinPolicy,
            PivTouchPolicy touchPolicy)
        {
            ImportAsymmetricKeyCommand cmd;

            PivPrivateKey keyData = GetKeyData(keyType);
#pragma warning disable IDE0017 // Testing this specific construction
            switch (cStyle)
            {
                default:
                    cmd = new ImportAsymmetricKeyCommand(keyData, slotNumber, pinPolicy, touchPolicy);
                    break;

                case 2:
                    cmd = new ImportAsymmetricKeyCommand(keyData)
                    {
                        SlotNumber = slotNumber,
                        PinPolicy = pinPolicy,
                        TouchPolicy = touchPolicy,
                    };
                    break;


                case 3:
                    cmd = new ImportAsymmetricKeyCommand(keyData);
                    cmd.SlotNumber = slotNumber;
                    cmd.PinPolicy = pinPolicy;
                    cmd.TouchPolicy = touchPolicy;
                    break;

                case 4:
                    cmd = new ImportAsymmetricKeyCommand(keyData)
                    {
                        SlotNumber = slotNumber,
                        TouchPolicy = touchPolicy,
                    };
                    break;

                case 5:
                    cmd = new ImportAsymmetricKeyCommand(keyData);
                    cmd.SlotNumber = slotNumber;
                    cmd.TouchPolicy = touchPolicy;
                    break;

                case 6:
                    cmd = new ImportAsymmetricKeyCommand(keyData)
                    {
                        SlotNumber = slotNumber,
                        PinPolicy = pinPolicy,
                    };
                    break;

                case 7:
                    cmd = new ImportAsymmetricKeyCommand(keyData);
                    cmd.SlotNumber = slotNumber;
                    cmd.PinPolicy = pinPolicy;
                    break;

                case 8:
                    cmd = new ImportAsymmetricKeyCommand(keyData)
                    {
                        SlotNumber = slotNumber,
                    };
                    break;

                case 9:
                    cmd = new ImportAsymmetricKeyCommand(keyData);
                    cmd.SlotNumber = slotNumber;
                    break;
            }
#pragma warning restore IDE0017
            return cmd;
        }

        private static PivPrivateKey GetKeyData(KeyType keyType) => keyType switch
        {
            KeyType.RSA1024 =>
                     PivPrivateKey.Create(new byte[] {
                         0x01, 0x40,
                         0xdf, 0x2c, 0x15, 0xe7, 0x9f, 0xf7, 0xf0, 0xe4, 0x36, 0xfd, 0x93, 0x1f, 0xd7, 0x36, 0x20, 0x2e,
                         0x70, 0xd2, 0x51, 0xe4, 0x4a, 0x5d, 0xf8, 0xbb, 0xfd, 0x2d, 0x66, 0xd1, 0xe5, 0x1d, 0x5e, 0x92,
                         0x9b, 0xa8, 0xba, 0x9c, 0xfd, 0x53, 0x72, 0x93, 0xee, 0x98, 0x33, 0xc6, 0xe5, 0x23, 0xcb, 0x79,
                         0x74, 0xad, 0x8f, 0x31, 0xb4, 0xa2, 0x6b, 0x46, 0xef, 0x8e, 0xad, 0x53, 0x6b, 0xb0, 0x5e, 0xc3,
                         0x02, 0x40,
                         0xd7, 0x96, 0x0f, 0x54, 0xfd, 0xc9, 0xa8, 0x6f, 0xcb, 0xc2, 0xea, 0x13, 0x58, 0xf6, 0x47, 0x87,
                         0x59, 0x84, 0xba, 0x1c, 0x68, 0x9d, 0xbf, 0x29, 0xbd, 0x20, 0x7d, 0x84, 0xcf, 0x12, 0xcf, 0xe7,
                         0xb5, 0x6f, 0x05, 0x37, 0xa3, 0x3a, 0x7c, 0x0a, 0x6f, 0x7c, 0x1b, 0xa0, 0x06, 0x23, 0x4b, 0x15,
                         0x09, 0xbb, 0x46, 0x5d, 0xba, 0x28, 0x3d, 0x36, 0x58, 0x66, 0x34, 0x7d, 0x7f, 0x88, 0x1a, 0xa9,
                         0x03, 0x40,
                         0x1e, 0xfb, 0x35, 0xc7, 0x43, 0xf3, 0xdd, 0xa3, 0x30, 0xe7, 0x1e, 0xe7, 0x8a, 0xae, 0xde, 0xe4,
                         0xd3, 0x90, 0xbf, 0x01, 0x9c, 0x39, 0x53, 0x70, 0x75, 0x83, 0x3a, 0x04, 0xe5, 0x73, 0xa0, 0x4f,
                         0x66, 0x00, 0x94, 0x77, 0x7a, 0xcb, 0x7c, 0xda, 0x80, 0x82, 0xec, 0x9d, 0x2d, 0xee, 0x3c, 0x2f,
                         0x0e, 0x3d, 0x91, 0xe5, 0x6a, 0x98, 0x29, 0xa0, 0x5d, 0x5d, 0x47, 0x3e, 0x8f, 0x72, 0x9a, 0x95,
                         0x04, 0x40,
                         0x8a, 0x40, 0xa5, 0x5c, 0x6f, 0xd4, 0x5e, 0xbc, 0x33, 0x03, 0xb0, 0x70, 0xef, 0xe0, 0x20, 0x46,
                         0xe0, 0x55, 0x89, 0xb4, 0xa6, 0x32, 0x63, 0x61, 0x34, 0xf4, 0x1d, 0x0a, 0x8a, 0x71, 0x19, 0xfb,
                         0x12, 0x13, 0x3c, 0x59, 0x4d, 0xc8, 0x37, 0xbb, 0xc9, 0x7a, 0xe1, 0x8c, 0x61, 0xe3, 0x48, 0x47,
                         0x19, 0x92, 0x8b, 0xb1, 0x97, 0xac, 0x2e, 0x75, 0x27, 0x83, 0x83, 0xad, 0xe7, 0x97, 0x34, 0xe1,
                         0x05, 0x40,
                         0x19, 0x51, 0x79, 0x1e, 0x4e, 0x1f, 0xb1, 0xe9, 0xc1, 0x59, 0x9c, 0x69, 0xc8, 0xda, 0x2d, 0x8d,
                         0xb7, 0x04, 0xa0, 0x7d, 0xd1, 0x45, 0x9f, 0xc8, 0xae, 0x97, 0xf7, 0x77, 0xe2, 0x53, 0x98, 0x48,
                         0xd5, 0x75, 0xfb, 0xff, 0xfa, 0x17, 0xb0, 0xc7, 0x4a, 0x46, 0xb0, 0xa7, 0xa6, 0x0e, 0x5e, 0x2c,
                         0xfd, 0xda, 0x5b, 0xbb, 0xc1, 0x0a, 0x77, 0x73, 0x0a, 0xaa, 0x1e, 0xc5, 0x66, 0x42, 0x96, 0xcf
                     }),

            KeyType.RSA2048 =>
                PivPrivateKey.Create(new byte[] {
                    0x02, 0x81, 0x80,
                    0xd0, 0xc0, 0x5a, 0xba, 0xec, 0x01, 0x46, 0x1b, 0x48, 0x98, 0xd2, 0x53, 0x97, 0x68, 0x47, 0xca,
                    0xb1, 0x8e, 0xbe, 0xf2, 0x92, 0x6b, 0x62, 0x0f, 0xa7, 0x1b, 0x8c, 0x50, 0x38, 0x23, 0x2d, 0x1a,
                    0x0a, 0xa4, 0x28, 0x14, 0xb5, 0xbe, 0x03, 0xe8, 0xf7, 0x88, 0x99, 0x7b, 0xa2, 0x20, 0xdc, 0xaa,
                    0x09, 0x73, 0x7c, 0x30, 0xe1, 0x33, 0xb3, 0xf7, 0x2b, 0x7b, 0x5a, 0x7a, 0x1d, 0x54, 0xd4, 0xcf,
                    0x99, 0xf5, 0x7f, 0x32, 0x46, 0x40, 0xf4, 0x8a, 0x5e, 0x71, 0x83, 0x72, 0x03, 0xee, 0xc4, 0xb3,
                    0x5c, 0x3d, 0xb8, 0xee, 0xc1, 0x1c, 0xed, 0x05, 0x82, 0x2d, 0x04, 0x0d, 0x59, 0x73, 0x79, 0x2a,
                    0x3b, 0x0e, 0x7d, 0xec, 0xc2, 0x3b, 0xbb, 0xa5, 0xa7, 0xa9, 0x13, 0x8b, 0xe3, 0x5e, 0xd7, 0x98,
                    0x56, 0xcc, 0x53, 0x61, 0x6a, 0x57, 0x82, 0xb5, 0x1a, 0x5f, 0xcf, 0xf0, 0x1c, 0x25, 0x47, 0xaf,
                    0x05, 0x81, 0x80,
                    0x58, 0xb3, 0x00, 0xb0, 0x52, 0x4a, 0xf5, 0xba, 0x87, 0x4c, 0x88, 0xfe, 0xde, 0x01, 0x6b, 0xfe,
                    0x0f, 0x07, 0x62, 0x9b, 0x93, 0x9c, 0xde, 0x34, 0xc6, 0x93, 0x5a, 0x8f, 0x3c, 0x7a, 0x1a, 0x15,
                    0x4a, 0xed, 0x2d, 0x45, 0x18, 0xd7, 0x02, 0xdb, 0x95, 0x36, 0xea, 0x98, 0x09, 0x6d, 0x94, 0x34,
                    0x4c, 0x58, 0xd2, 0x41, 0x8a, 0x87, 0x13, 0x50, 0xd5, 0x76, 0xf1, 0xae, 0x3a, 0x85, 0x26, 0x11,
                    0x92, 0x14, 0x95, 0xa9, 0x85, 0x3b, 0x9e, 0xf0, 0xbc, 0x02, 0xd7, 0x76, 0xb8, 0x5a, 0x4d, 0xa9,
                    0x4b, 0xd3, 0x0c, 0xa8, 0xb7, 0x96, 0xa4, 0x87, 0xe4, 0x6c, 0xeb, 0x85, 0xc8, 0x2c, 0xba, 0x76,
                    0x9a, 0x89, 0x53, 0x35, 0xcd, 0xb8, 0x77, 0xa3, 0x99, 0xdc, 0xab, 0x42, 0x75, 0xf9, 0xc5, 0x0a,
                    0xe6, 0x9d, 0xb4, 0x32, 0x63, 0x80, 0xe9, 0xce, 0xb2, 0x4b, 0x22, 0xe1, 0x13, 0xf7, 0x80, 0xb1,
                    0x03, 0x81, 0x80,
                    0x64, 0xf0, 0xf2, 0x94, 0x94, 0x41, 0x56, 0x13, 0x95, 0x93, 0xad, 0xd5, 0xce, 0x10, 0x26, 0x49,
                    0xda, 0xc7, 0x00, 0xcc, 0xb0, 0xea, 0x05, 0xe7, 0xd1, 0xb0, 0x0c, 0xf9, 0xe9, 0x19, 0x36, 0xf1,
                    0xe4, 0x23, 0x21, 0x6a, 0x64, 0x24, 0x3d, 0x32, 0xf6, 0x60, 0x16, 0x08, 0xa8, 0xd4, 0x87, 0x0f,
                    0xfe, 0x0a, 0x96, 0x7f, 0x20, 0xb5, 0xde, 0x37, 0x6e, 0xa3, 0x0f, 0xa9, 0x6a, 0xd9, 0x87, 0x99,
                    0x22, 0x85, 0xeb, 0x5a, 0x7f, 0xeb, 0xb0, 0xe8, 0x54, 0xe7, 0xfc, 0xd5, 0x95, 0x87, 0x0d, 0x4d,
                    0x5a, 0xbf, 0x19, 0x45, 0xef, 0xb7, 0xb5, 0xf4, 0xa0, 0x33, 0x28, 0x01, 0xa8, 0x9e, 0x3c, 0x86,
                    0x15, 0xb0, 0xad, 0x22, 0x83, 0x36, 0xb1, 0xa7, 0x86, 0xbd, 0xca, 0xe0, 0x40, 0x78, 0x5d, 0xf2,
                    0xea, 0x9d, 0x26, 0xf1, 0x30, 0xf0, 0x94, 0x96, 0x5c, 0x25, 0xb6, 0xa3, 0x89, 0x77, 0x6d, 0x89,
                    0x01, 0x81, 0x80,
                    0x00, 0xee, 0xea, 0xd9, 0xdb, 0x94, 0x8c, 0x25, 0xa8, 0x08, 0xde, 0x71, 0x45, 0x8d, 0xa6, 0x8f,
                    0xd2, 0xe4, 0x3e, 0x59, 0x1b, 0x13, 0xdb, 0x56, 0xf8, 0xae, 0xd9, 0xc1, 0xde, 0x9b, 0x13, 0xa5,
                    0xae, 0x63, 0xea, 0xb6, 0x6d, 0xc2, 0x51, 0xcc, 0x6b, 0xc3, 0x1e, 0x56, 0xa3, 0x3f, 0x55, 0x6f,
                    0xe7, 0x57, 0xd4, 0x2e, 0x5a, 0xc3, 0xf5, 0x99, 0x1b, 0xeb, 0x92, 0x67, 0xf1, 0x60, 0xd6, 0x5f,
                    0x16, 0x5d, 0xb8, 0xfe, 0x38, 0x30, 0x0f, 0x64, 0x6a, 0x86, 0x86, 0x93, 0x68, 0x35, 0xbd, 0xb2,
                    0xd7, 0x24, 0xf5, 0x69, 0xfd, 0xc5, 0x90, 0x9b, 0x4a, 0x95, 0xd4, 0xd3, 0x3f, 0xf5, 0x0d, 0x63,
                    0x8c, 0xbf, 0x39, 0x35, 0xb7, 0xe8, 0x0b, 0xf4, 0x09, 0xcd, 0xcf, 0x11, 0xab, 0x2c, 0xdf, 0xd6,
                    0xaf, 0x2b, 0xde, 0xea, 0x52, 0x86, 0x51, 0x35, 0x62, 0x1c, 0x7e, 0xd6, 0x73, 0xeb, 0xf2, 0x32,
                    0x04, 0x81, 0x80,
                    0xb7, 0xea, 0x81, 0x05, 0x66, 0xa0, 0xc8, 0xaf, 0x89, 0x0a, 0x7b, 0x64, 0x02, 0x65, 0x71, 0xba,
                    0xf7, 0x2c, 0x98, 0xb7, 0x06, 0xa1, 0x6d, 0x47, 0xf5, 0x26, 0xa4, 0x3c, 0x98, 0xf9, 0x04, 0xe7,
                    0x88, 0xb0, 0x90, 0x4f, 0x8e, 0xbf, 0xd6, 0x9a, 0x0e, 0x5c, 0x5f, 0x3d, 0x39, 0xde, 0x52, 0x0b,
                    0xcd, 0x3f, 0xde, 0x0f, 0x02, 0x9a, 0x96, 0xd8, 0x11, 0x8b, 0x20, 0x6f, 0xae, 0x1f, 0xeb, 0x4e,
                    0xec, 0x8a, 0x0a, 0x82, 0xb4, 0xba, 0xcd, 0xb8, 0x8a, 0xf4, 0xdf, 0xa6, 0x38, 0x28, 0xc9, 0x4c,
                    0x6c, 0xdc, 0x44, 0x6e, 0xb5, 0xdd, 0x52, 0x46, 0x24, 0x26, 0x9d, 0x07, 0x55, 0xe4, 0x12, 0xc0,
                    0x4e, 0x3f, 0xba, 0x5a, 0x39, 0xd6, 0x7e, 0xc0, 0xb8, 0x32, 0x92, 0x72, 0x10, 0xe4, 0xa2, 0x76,
                    0x29, 0x22, 0xe3, 0xe3, 0x53, 0xd9, 0xbd, 0xe2, 0xe9, 0x55, 0xb8, 0xd2, 0x07, 0x3a, 0x21, 0x29
                }),

            KeyType.P256 =>
                PivPrivateKey.Create(new byte[] {
                    0x06, 0x20,
                    0xba, 0x29, 0x7a, 0xc6, 0x64, 0x62, 0xef, 0x6c, 0xd0, 0x89, 0x76, 0x5c, 0xbd, 0x46, 0x52, 0x2b,
                    0xb0, 0x48, 0x0e, 0x85, 0x49, 0x15, 0x85, 0xe7, 0x7a, 0x74, 0x3c, 0x8e, 0x03, 0x59, 0x8d, 0x3a
                }),

            _ =>
                PivPrivateKey.Create(new byte[] {
                    0x06, 0x30,
                    0x47, 0x85, 0xde, 0x3a, 0xff, 0x10, 0x0d, 0x67, 0xa7, 0x26, 0x30, 0x62, 0x73, 0x45, 0xfd, 0xce,
                    0xeb, 0xb9, 0xbe, 0x4c, 0x93, 0x42, 0xcd, 0x6a, 0x84, 0xd6, 0x8e, 0x00, 0x70, 0x70, 0x4c, 0x66,
                    0x63, 0x53, 0xa0, 0x2c, 0xb9, 0xa7, 0x61, 0xcf, 0x56, 0xf0, 0x45, 0x07, 0xa6, 0xfb, 0x9f, 0x5a
                }),
        };
    }
    
    
    
    
    
    
//     public class ImportKeyCommandTests // TODO
//     {
//         [Theory]
//         [InlineData(1, KeyType.P256)]
//         [InlineData(2, KeyType.P384)]
//         [InlineData(3, KeyType.RSA1024)]
//         [InlineData(4, KeyType.RSA2048)]
//         public void ClassType_DerivedFromPivCommand_IsTrue(int cStyle, KeyType keyType)
//         {
//             ImportAsymmetricKeyCommand cmd = GetCommandObject(
//                 cStyle,
//                 PivSlot.Retired5,
//                 keyType,
//                 PivPinPolicy.Always,
//                 PivTouchPolicy.Never);
//
//             Assert.True(cmd is IYubiKeyCommand<ImportAsymmetricKeyResponse>);
//         }
//
//         [Fact]
//         public void FullConstructor_NullKeyData_ThrowsException()
//         {
// #pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
//             _ = Assert.Throws<ArgumentNullException>(() => new ImportAsymmetricKeyCommand(
//                 null,
//                 0x87,
//                 PivPinPolicy.Always,
//                 PivTouchPolicy.Never));
// #pragma warning restore CS8625
//         }
//
//         [Fact]
//         public void InitConstructor_NullKeyData_ThrowsException()
//         {
// #pragma warning disable CS8625 // testing null input, disable warning that null is passed to non-nullable arg.
//             _ = Assert.Throws<ArgumentNullException>(() => new ImportAsymmetricKeyCommand(null)
//             {
//                 SlotNumber = 0x87,
//                 PinPolicy = PivPinPolicy.Always,
//                 TouchPolicy = PivTouchPolicy.Never,
//             });
// #pragma warning restore CS8625
//         }
//
//         [Theory]
//         [InlineData(1, 0x9B)]
//         [InlineData(2, 0x80)]
//         [InlineData(3, 0x81)]
//         [InlineData(4, 0x00)]
//         [InlineData(5, 0x96)]
//         [InlineData(6, 0xF8)]
//         [InlineData(7, 0xFA)]
//         [InlineData(8, 0x99)]
//         [InlineData(9, 0x9F)]
//         public void Constructor_BadSlotNumber_ThrowsException(int cStyle, byte slotNumber)
//         {
//             _ = Assert.Throws<ArgumentException>(() => GetCommandObject(
//                 cStyle,
//                 slotNumber,
//                 KeyType.P256,
//                 PivPinPolicy.Once,
//                 PivTouchPolicy.Cached));
//         }
//
//         [Fact]
//         public void Constructor_NoSlotNumber_ThrowsException()
//         {
//             PivPrivateKey keyData = GetKeyData(KeyType.P256);
//             var cmd = new ImportAsymmetricKeyCommand(keyData)
//             {
//                 PinPolicy = PivPinPolicy.Always,
//                 TouchPolicy = PivTouchPolicy.Never,
//             };
//             _ = Assert.Throws<InvalidOperationException>(() => cmd.CreateCommandApdu());
//         }
//
//         [Fact]
//         public void Constructor_Application_Piv()
//         {
//             PivPrivateKey keyData = GetKeyData(KeyType.P256);
//             var importKeyCommand = new ImportAsymmetricKeyCommand(
//                 keyData,
//                 PivSlot.Retired19,
//                 PivPinPolicy.Never,
//                 PivTouchPolicy.Never);
//
//             YubiKeyApplication application = importKeyCommand.Application;
//
//             Assert.Equal(YubiKeyApplication.Piv, application);
//
//             keyData.Clear();
//         }
//
//         [Fact]
//         public void Constructor_Property_SlotNum()
//         {
//             PivPrivateKey keyData = GetKeyData(KeyType.P256);
//             byte slotNumber = PivSlot.Retired20;
//             PivPinPolicy pinPolicy = PivPinPolicy.Always;
//             PivTouchPolicy touchPolicy = PivTouchPolicy.Cached;
//             var importKeyCommand = new ImportAsymmetricKeyCommand(
//                 keyData,
//                 slotNumber,
//                 pinPolicy,
//                 touchPolicy);
//
//             byte getSlotNum = importKeyCommand.SlotNumber;
//
//             Assert.Equal(slotNumber, getSlotNum);
//
//             keyData.Clear();
//         }
//
//         [Fact]
//         public void Constructor_Property_PinPolicy()
//         {
//             PivPrivateKey keyData = GetKeyData(KeyType.P256);
//             byte slotNumber = PivSlot.Retired20;
//             PivPinPolicy pinPolicy = PivPinPolicy.Always;
//             PivTouchPolicy touchPolicy = PivTouchPolicy.Cached;
//             var importKeyCommand = new ImportAsymmetricKeyCommand(
//                 keyData,
//                 slotNumber,
//                 pinPolicy,
//                 touchPolicy);
//
//             PivPinPolicy getPolicy = importKeyCommand.PinPolicy;
//
//             Assert.Equal(pinPolicy, getPolicy);
//
//             keyData.Clear();
//         }
//
//         [Fact]
//         public void Constructor_Property_TouchPolicy()
//         {
//             PivPrivateKey keyData = GetKeyData(KeyType.P256);
//             byte slotNumber = PivSlot.Retired20;
//             PivPinPolicy pinPolicy = PivPinPolicy.Always;
//             PivTouchPolicy touchPolicy = PivTouchPolicy.Cached;
//             var importKeyCommand = new ImportAsymmetricKeyCommand(
//                 keyData,
//                 slotNumber,
//                 pinPolicy,
//                 touchPolicy);
//
//             PivTouchPolicy getPolicy = importKeyCommand.TouchPolicy;
//
//             Assert.Equal(touchPolicy, getPolicy);
//
//             keyData.Clear();
//         }
//
//         [Theory]
//         [InlineData(1)]
//         [InlineData(2)]
//         [InlineData(3)]
//         public void CreateCommandApdu_GetClaProperty_ReturnsZero(int cStyle)
//         {
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle, 0x94, KeyType.P256, PivPinPolicy.Default, PivTouchPolicy.Never);
//
//             byte Cla = cmdApdu.Cla;
//
//             Assert.Equal(0, Cla);
//         }
//
//         [Theory]
//         [InlineData(1)]
//         [InlineData(2)]
//         [InlineData(3)]
//         public void CreateCommandApdu_GetInsProperty_ReturnsHexFE(int cStyle)
//         {
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 0x92,
//                 KeyType.RSA1024,
//                 PivPinPolicy.Once,
//                 PivTouchPolicy.None);
//
//             byte Ins = cmdApdu.Ins;
//
//             Assert.Equal(0xFE, Ins);
//         }
//
//         [Theory]
//         [InlineData(1, KeyType.P256)]
//         [InlineData(2, KeyType.P384)]
//         [InlineData(3, KeyType.RSA1024)]
//         [InlineData(4, KeyType.RSA2048)]
//         public void CreateCommandApdu_GetP1Property_ReturnsAlgorithm(int cStyle, KeyType keyType)
//         {
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 0x9E,
//                 keyType,
//                 PivPinPolicy.None,
//                 PivTouchPolicy.Cached);
//
//             byte P1 = cmdApdu.P1;
//
//             Assert.Equal((byte)keyType, P1);
//         }
//
//         [Theory]
//         [InlineData(1, 0x95)]
//         [InlineData(2, 0x82)]
//         [InlineData(3, 0x83)]
//         [InlineData(4, 0x84)]
//         [InlineData(5, 0x85)]
//         [InlineData(6, 0x86)]
//         [InlineData(7, 0x87)]
//         [InlineData(8, 0x88)]
//         [InlineData(9, 0x89)]
//         public void CreateCommandApdu_GetP2Property_ReturnsSlotNum(int cStyle, byte slotNumber)
//         {
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 slotNumber,
//                 KeyType.RSA2048,
//                 PivPinPolicy.None,
//                 PivTouchPolicy.Cached);
//
//             byte P2 = cmdApdu.P2;
//
//             Assert.Equal(slotNumber, P2);
//         }
//
//         [Theory]
//         [InlineData(1, KeyType.RSA1024, PivPinPolicy.None, PivTouchPolicy.None, 0)]
//         [InlineData(2, KeyType.RSA2048, PivPinPolicy.None, PivTouchPolicy.Default, 0)]
//         [InlineData(3, KeyType.P256, PivPinPolicy.None, PivTouchPolicy.Never, 3)]
//         [InlineData(1, KeyType.P384, PivPinPolicy.None, PivTouchPolicy.Always, 3)]
//         [InlineData(2, KeyType.RSA1024, PivPinPolicy.None, PivTouchPolicy.Cached, 3)]
//         [InlineData(3, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.None, 0)]
//         [InlineData(1, KeyType.P256, PivPinPolicy.Default, PivTouchPolicy.Default, 0)]
//         [InlineData(2, KeyType.P384, PivPinPolicy.Default, PivTouchPolicy.Never, 3)]
//         [InlineData(3, KeyType.RSA1024, PivPinPolicy.Default, PivTouchPolicy.Always, 3)]
//         [InlineData(1, KeyType.RSA2048, PivPinPolicy.Default, PivTouchPolicy.Cached, 3)]
//         [InlineData(2, KeyType.P256, PivPinPolicy.Never, PivTouchPolicy.None, 3)]
//         [InlineData(3, KeyType.P384, PivPinPolicy.Never, PivTouchPolicy.Default, 3)]
//         [InlineData(1, KeyType.RSA1024, PivPinPolicy.Never, PivTouchPolicy.Never, 6)]
//         [InlineData(2, KeyType.RSA2048, PivPinPolicy.Never, PivTouchPolicy.Always, 6)]
//         [InlineData(3, KeyType.P256, PivPinPolicy.Never, PivTouchPolicy.Cached, 6)]
//         [InlineData(1, KeyType.P384, PivPinPolicy.Once, PivTouchPolicy.None, 3)]
//         [InlineData(2, KeyType.RSA1024, PivPinPolicy.Once, PivTouchPolicy.Default, 3)]
//         [InlineData(3, KeyType.RSA2048, PivPinPolicy.Once, PivTouchPolicy.Never, 6)]
//         [InlineData(1, KeyType.P256, PivPinPolicy.Once, PivTouchPolicy.Always, 6)]
//         [InlineData(2, KeyType.P384, PivPinPolicy.Once, PivTouchPolicy.Cached, 6)]
//         [InlineData(3, KeyType.RSA1024, PivPinPolicy.Always, PivTouchPolicy.None, 3)]
//         [InlineData(1, KeyType.RSA2048, PivPinPolicy.Always, PivTouchPolicy.Default, 3)]
//         [InlineData(2, KeyType.P256, PivPinPolicy.Always, PivTouchPolicy.Never, 6)]
//         [InlineData(3, KeyType.P384, PivPinPolicy.Always, PivTouchPolicy.Always, 6)]
//         [InlineData(1, KeyType.RSA1024, PivPinPolicy.Always, PivTouchPolicy.Cached, 6)]
//         public void CreateCommandApdu_GetNcProperty_ReturnsCorrect(
//             int cStyle,
//             KeyType keyType,
//             PivPinPolicy pinPolicy,
//             PivTouchPolicy touchPolicy,
//             int expectedPolicyLength)
//         {
//             PivPrivateKey keyData = GetKeyData(keyType);
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 0x8E,
//                 keyType,
//                 pinPolicy,
//                 touchPolicy);
//
//             int Nc = cmdApdu.Nc;
//
//             int expectedLength = keyData.EncodedPrivateKey.Length + expectedPolicyLength;
//             Assert.Equal(expectedLength, Nc);
//
//             keyData.Clear();
//         }
//
//         [Theory]
//         [InlineData(1)]
//         [InlineData(2)]
//         [InlineData(3)]
//         public void CreateCommandApdu_GetNeProperty_ReturnsZero(int cStyle)
//         {
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 0x8F,
//                 KeyType.P256,
//                 PivPinPolicy.Always,
//                 PivTouchPolicy.Never);
//
//             int Ne = cmdApdu.Ne;
//
//             Assert.Equal(0, Ne);
//         }
//
//         [Theory]
//         [InlineData(1, KeyType.RSA1024)]
//         [InlineData(2, KeyType.RSA2048)]
//         [InlineData(3, KeyType.P256)]
//         [InlineData(4, KeyType.P384)]
//         public void CreateCommandApdu_GetData_ReturnsKeyData(int cStyle, KeyType keyType)
//         {
//             PivPrivateKey keyData = GetKeyData(keyType);
//             // Use Default policies so the only data will be the key data.
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 0x8F,
//                 keyType,
//                 PivPinPolicy.Default,
//                 PivTouchPolicy.Default);
//
//             ReadOnlyMemory<byte> data = cmdApdu.Data;
//
//             Assert.False(data.IsEmpty);
//             if (data.IsEmpty)
//             {
//                 return;
//             }
//
//             bool compareResult = data.Span.SequenceEqual(keyData.EncodedPrivateKey.Span);
//
//             Assert.True(compareResult);
//
//             keyData.Clear();
//         }
//
//         [Theory]
//         [InlineData(1, PivPinPolicy.None, PivTouchPolicy.None)]
//         [InlineData(2, PivPinPolicy.None, PivTouchPolicy.Default)]
//         [InlineData(3, PivPinPolicy.None, PivTouchPolicy.Never)]
//         [InlineData(1, PivPinPolicy.None, PivTouchPolicy.Cached)]
//         [InlineData(2, PivPinPolicy.None, PivTouchPolicy.Always)]
//         [InlineData(3, PivPinPolicy.Default, PivTouchPolicy.None)]
//         [InlineData(1, PivPinPolicy.Default, PivTouchPolicy.Default)]
//         [InlineData(2, PivPinPolicy.Default, PivTouchPolicy.Never)]
//         [InlineData(3, PivPinPolicy.Default, PivTouchPolicy.Cached)]
//         [InlineData(1, PivPinPolicy.Default, PivTouchPolicy.Always)]
//         [InlineData(2, PivPinPolicy.Never, PivTouchPolicy.None)]
//         [InlineData(3, PivPinPolicy.Never, PivTouchPolicy.Default)]
//         [InlineData(1, PivPinPolicy.Never, PivTouchPolicy.Never)]
//         [InlineData(2, PivPinPolicy.Never, PivTouchPolicy.Cached)]
//         [InlineData(3, PivPinPolicy.Never, PivTouchPolicy.Always)]
//         [InlineData(1, PivPinPolicy.Once, PivTouchPolicy.None)]
//         [InlineData(2, PivPinPolicy.Once, PivTouchPolicy.Default)]
//         [InlineData(3, PivPinPolicy.Once, PivTouchPolicy.Never)]
//         [InlineData(1, PivPinPolicy.Once, PivTouchPolicy.Cached)]
//         [InlineData(2, PivPinPolicy.Once, PivTouchPolicy.Always)]
//         [InlineData(3, PivPinPolicy.Always, PivTouchPolicy.None)]
//         [InlineData(1, PivPinPolicy.Always, PivTouchPolicy.Default)]
//         [InlineData(2, PivPinPolicy.Always, PivTouchPolicy.Never)]
//         [InlineData(3, PivPinPolicy.Always, PivTouchPolicy.Cached)]
//         [InlineData(1, PivPinPolicy.Always, PivTouchPolicy.Always)]
//         [InlineData(4, PivPinPolicy.None, PivTouchPolicy.Never)]
//         [InlineData(5, PivPinPolicy.None, PivTouchPolicy.Cached)]
//         [InlineData(6, PivPinPolicy.Once, PivTouchPolicy.None)]
//         [InlineData(7, PivPinPolicy.Always, PivTouchPolicy.None)]
//         [InlineData(8, PivPinPolicy.Default, PivTouchPolicy.Default)]
//         [InlineData(9, PivPinPolicy.Default, PivTouchPolicy.Default)]
//         public void CreateCommandApdu_GetData_ReturnsPolicy(
//             int cStyle,
//             PivPinPolicy pinPolicy,
//             PivTouchPolicy touchPolicy)
//         {
//             PivPrivateKey keyData = GetKeyData(KeyType.P256);
//             byte[] pinData = new byte[] { 0xAA, 0x01, (byte)pinPolicy };
//             byte[] touchData = new byte[] { 0xAB, 0x01, (byte)touchPolicy };
//             var expected = new List<byte>(keyData.EncodedPrivateKey.ToArray());
//             if (pinPolicy != PivPinPolicy.None && pinPolicy != PivPinPolicy.Default)
//             {
//                 expected.AddRange(pinData);
//             }
//             if (touchPolicy != PivTouchPolicy.None && touchPolicy != PivTouchPolicy.Default)
//             {
//                 expected.AddRange(touchData);
//             }
//             CommandApdu cmdApdu = GetImportKeyCommandApdu(
//                 cStyle,
//                 0x8F,
//                 KeyType.P256,
//                 pinPolicy,
//                 touchPolicy);
//
//             ReadOnlyMemory<byte> data = cmdApdu.Data;
//
//             Assert.False(data.IsEmpty);
//             if (data.IsEmpty)
//             {
//                 return;
//             }
//
//             bool compareResult = data.Span.SequenceEqual(expected.ToArray());
//
//             Assert.True(compareResult);
//
//             keyData.Clear();
//         }
//
//         [Fact]
//         public void CreateResponseForApdu_ReturnsCorrectType()
//         {
//             byte sw1 = unchecked((byte)(SWConstants.Success >> 8));
//             byte sw2 = unchecked((byte)SWConstants.Success);
//             var responseApdu = new ResponseApdu(new byte[] { sw1, sw2 });
//
//             PivPrivateKey keyData = GetKeyData(KeyType.P384);
//             var importKeyCommand = new ImportAsymmetricKeyCommand(
//                 keyData,
//                 PivSlot.Retired18,
//                 PivPinPolicy.Default,
//                 PivTouchPolicy.Default);
//
//             ImportAsymmetricKeyResponse response = importKeyCommand.CreateResponseForApdu(responseApdu);
//
//             Assert.True(response is ImportAsymmetricKeyResponse);
//
//             keyData.Clear();
//         }
//
//         // The constructorStyle is either 1, meaning construct the
//         // ImportAsymmetricKeyCommand using the full constructor, or anything
//         // other than 1, meaning use the object initializer constructor.
//         private static CommandApdu GetImportKeyCommandApdu(
//             int cStyle,
//             byte slotNumber,
//             KeyType keyType,
//             PivPinPolicy pinPolicy,
//             PivTouchPolicy touchPolicy)
//         {
//             ImportAsymmetricKeyCommand importKeyCommand = GetCommandObject(
//                 cStyle,
//                 slotNumber,
//                 keyType,
//                 pinPolicy,
//                 touchPolicy);
//
//             return importKeyCommand.CreateCommandApdu();
//         }
//
//         // Construct a GenerateKeyPairCommand using the style specified.
//         // If the style arg is 1, this will build using the full constructor.
//         // If it is 2, it will build it using object initializer constructor.
//         // If it is 3, create it using the empty constructor and set the
//         // properties later.
//         // If it is 4, create it using the object initializer constructor but
//         // don't set the PinPolicy (it should be default).
//         // If it is 5, create it using the empty constructor and set the
//         // properties later, except don't set the PinPolicy.
//         // If it is 6, create it using the object initializer constructor but
//         // don't set the TouchPolicy (it should be default).
//         // If it is 7, create it using the empty constructor and set the
//         // properties later, except don't set the TouchPolicy.
//         // If it is 8, create it using the object initializer constructor but
//         // don't set the PinPolicy or the TouchPolicy.
//         // If it is 9, create it using the empty constructor and set the
//         // properties later, except don't set the PinPolicy or the TouchPolicy.
//         private static ImportAsymmetricKeyCommand GetCommandObject(
//             int cStyle,
//             byte slotNumber,
//             KeyType keyType,
//             PivPinPolicy pinPolicy,
//             PivTouchPolicy touchPolicy)
//         {
//             ImportAsymmetricKeyCommand cmd;
//
//             PivPrivateKey keyData = GetKeyData(keyType);
// #pragma warning disable IDE0017 // Testing this specific construction
//             switch (cStyle)
//             {
//                 default:
//                     cmd = new ImportAsymmetricKeyCommand(keyData, slotNumber, pinPolicy, touchPolicy);
//                     break;
//
//                 case 2:
//                     cmd = new ImportAsymmetricKeyCommand(keyData)
//                     {
//                         SlotNumber = slotNumber,
//                         PinPolicy = pinPolicy,
//                         TouchPolicy = touchPolicy,
//                     };
//                     break;
//
//
//                 case 3:
//                     cmd = new ImportAsymmetricKeyCommand(keyData);
//                     cmd.SlotNumber = slotNumber;
//                     cmd.PinPolicy = pinPolicy;
//                     cmd.TouchPolicy = touchPolicy;
//                     break;
//
//                 case 4:
//                     cmd = new ImportAsymmetricKeyCommand(keyData)
//                     {
//                         SlotNumber = slotNumber,
//                         TouchPolicy = touchPolicy,
//                     };
//                     break;
//
//                 case 5:
//                     cmd = new ImportAsymmetricKeyCommand(keyData);
//                     cmd.SlotNumber = slotNumber;
//                     cmd.TouchPolicy = touchPolicy;
//                     break;
//
//                 case 6:
//                     cmd = new ImportAsymmetricKeyCommand(keyData)
//                     {
//                         SlotNumber = slotNumber,
//                         PinPolicy = pinPolicy,
//                     };
//                     break;
//
//                 case 7:
//                     cmd = new ImportAsymmetricKeyCommand(keyData);
//                     cmd.SlotNumber = slotNumber;
//                     cmd.PinPolicy = pinPolicy;
//                     break;
//
//                 case 8:
//                     cmd = new ImportAsymmetricKeyCommand(keyData)
//                     {
//                         SlotNumber = slotNumber,
//                     };
//                     break;
//
//                 case 9:
//                     cmd = new ImportAsymmetricKeyCommand(keyData);
//                     cmd.SlotNumber = slotNumber;
//                     break;
//             }
// #pragma warning restore IDE0017
//             return cmd;
//         }
//
//         private static PivPrivateKey GetKeyData(KeyType keyType) => keyType switch
//         {
//             KeyType.RSA1024 =>
//                      PivPrivateKey.Create(new byte[] {
//                         0x01, 0x40,
//                         0xdf, 0x2c, 0x15, 0xe7, 0x9f, 0xf7, 0xf0, 0xe4, 0x36, 0xfd, 0x93, 0x1f, 0xd7, 0x36, 0x20, 0x2e,
//                         0x70, 0xd2, 0x51, 0xe4, 0x4a, 0x5d, 0xf8, 0xbb, 0xfd, 0x2d, 0x66, 0xd1, 0xe5, 0x1d, 0x5e, 0x92,
//                         0x9b, 0xa8, 0xba, 0x9c, 0xfd, 0x53, 0x72, 0x93, 0xee, 0x98, 0x33, 0xc6, 0xe5, 0x23, 0xcb, 0x79,
//                         0x74, 0xad, 0x8f, 0x31, 0xb4, 0xa2, 0x6b, 0x46, 0xef, 0x8e, 0xad, 0x53, 0x6b, 0xb0, 0x5e, 0xc3,
//                         0x02, 0x40,
//                         0xd7, 0x96, 0x0f, 0x54, 0xfd, 0xc9, 0xa8, 0x6f, 0xcb, 0xc2, 0xea, 0x13, 0x58, 0xf6, 0x47, 0x87,
//                         0x59, 0x84, 0xba, 0x1c, 0x68, 0x9d, 0xbf, 0x29, 0xbd, 0x20, 0x7d, 0x84, 0xcf, 0x12, 0xcf, 0xe7,
//                         0xb5, 0x6f, 0x05, 0x37, 0xa3, 0x3a, 0x7c, 0x0a, 0x6f, 0x7c, 0x1b, 0xa0, 0x06, 0x23, 0x4b, 0x15,
//                         0x09, 0xbb, 0x46, 0x5d, 0xba, 0x28, 0x3d, 0x36, 0x58, 0x66, 0x34, 0x7d, 0x7f, 0x88, 0x1a, 0xa9,
//                         0x03, 0x40,
//                         0x1e, 0xfb, 0x35, 0xc7, 0x43, 0xf3, 0xdd, 0xa3, 0x30, 0xe7, 0x1e, 0xe7, 0x8a, 0xae, 0xde, 0xe4,
//                         0xd3, 0x90, 0xbf, 0x01, 0x9c, 0x39, 0x53, 0x70, 0x75, 0x83, 0x3a, 0x04, 0xe5, 0x73, 0xa0, 0x4f,
//                         0x66, 0x00, 0x94, 0x77, 0x7a, 0xcb, 0x7c, 0xda, 0x80, 0x82, 0xec, 0x9d, 0x2d, 0xee, 0x3c, 0x2f,
//                         0x0e, 0x3d, 0x91, 0xe5, 0x6a, 0x98, 0x29, 0xa0, 0x5d, 0x5d, 0x47, 0x3e, 0x8f, 0x72, 0x9a, 0x95,
//                         0x04, 0x40,
//                         0x8a, 0x40, 0xa5, 0x5c, 0x6f, 0xd4, 0x5e, 0xbc, 0x33, 0x03, 0xb0, 0x70, 0xef, 0xe0, 0x20, 0x46,
//                         0xe0, 0x55, 0x89, 0xb4, 0xa6, 0x32, 0x63, 0x61, 0x34, 0xf4, 0x1d, 0x0a, 0x8a, 0x71, 0x19, 0xfb,
//                         0x12, 0x13, 0x3c, 0x59, 0x4d, 0xc8, 0x37, 0xbb, 0xc9, 0x7a, 0xe1, 0x8c, 0x61, 0xe3, 0x48, 0x47,
//                         0x19, 0x92, 0x8b, 0xb1, 0x97, 0xac, 0x2e, 0x75, 0x27, 0x83, 0x83, 0xad, 0xe7, 0x97, 0x34, 0xe1,
//                         0x05, 0x40,
//                         0x19, 0x51, 0x79, 0x1e, 0x4e, 0x1f, 0xb1, 0xe9, 0xc1, 0x59, 0x9c, 0x69, 0xc8, 0xda, 0x2d, 0x8d,
//                         0xb7, 0x04, 0xa0, 0x7d, 0xd1, 0x45, 0x9f, 0xc8, 0xae, 0x97, 0xf7, 0x77, 0xe2, 0x53, 0x98, 0x48,
//                         0xd5, 0x75, 0xfb, 0xff, 0xfa, 0x17, 0xb0, 0xc7, 0x4a, 0x46, 0xb0, 0xa7, 0xa6, 0x0e, 0x5e, 0x2c,
//                         0xfd, 0xda, 0x5b, 0xbb, 0xc1, 0x0a, 0x77, 0x73, 0x0a, 0xaa, 0x1e, 0xc5, 0x66, 0x42, 0x96, 0xcf
//                     }),
//
//             KeyType.RSA2048 =>
//                 PivPrivateKey.Create(new byte[] {
//                         0x02, 0x81, 0x80,
//                         0xd0, 0xc0, 0x5a, 0xba, 0xec, 0x01, 0x46, 0x1b, 0x48, 0x98, 0xd2, 0x53, 0x97, 0x68, 0x47, 0xca,
//                         0xb1, 0x8e, 0xbe, 0xf2, 0x92, 0x6b, 0x62, 0x0f, 0xa7, 0x1b, 0x8c, 0x50, 0x38, 0x23, 0x2d, 0x1a,
//                         0x0a, 0xa4, 0x28, 0x14, 0xb5, 0xbe, 0x03, 0xe8, 0xf7, 0x88, 0x99, 0x7b, 0xa2, 0x20, 0xdc, 0xaa,
//                         0x09, 0x73, 0x7c, 0x30, 0xe1, 0x33, 0xb3, 0xf7, 0x2b, 0x7b, 0x5a, 0x7a, 0x1d, 0x54, 0xd4, 0xcf,
//                         0x99, 0xf5, 0x7f, 0x32, 0x46, 0x40, 0xf4, 0x8a, 0x5e, 0x71, 0x83, 0x72, 0x03, 0xee, 0xc4, 0xb3,
//                         0x5c, 0x3d, 0xb8, 0xee, 0xc1, 0x1c, 0xed, 0x05, 0x82, 0x2d, 0x04, 0x0d, 0x59, 0x73, 0x79, 0x2a,
//                         0x3b, 0x0e, 0x7d, 0xec, 0xc2, 0x3b, 0xbb, 0xa5, 0xa7, 0xa9, 0x13, 0x8b, 0xe3, 0x5e, 0xd7, 0x98,
//                         0x56, 0xcc, 0x53, 0x61, 0x6a, 0x57, 0x82, 0xb5, 0x1a, 0x5f, 0xcf, 0xf0, 0x1c, 0x25, 0x47, 0xaf,
//                         0x05, 0x81, 0x80,
//                         0x58, 0xb3, 0x00, 0xb0, 0x52, 0x4a, 0xf5, 0xba, 0x87, 0x4c, 0x88, 0xfe, 0xde, 0x01, 0x6b, 0xfe,
//                         0x0f, 0x07, 0x62, 0x9b, 0x93, 0x9c, 0xde, 0x34, 0xc6, 0x93, 0x5a, 0x8f, 0x3c, 0x7a, 0x1a, 0x15,
//                         0x4a, 0xed, 0x2d, 0x45, 0x18, 0xd7, 0x02, 0xdb, 0x95, 0x36, 0xea, 0x98, 0x09, 0x6d, 0x94, 0x34,
//                         0x4c, 0x58, 0xd2, 0x41, 0x8a, 0x87, 0x13, 0x50, 0xd5, 0x76, 0xf1, 0xae, 0x3a, 0x85, 0x26, 0x11,
//                         0x92, 0x14, 0x95, 0xa9, 0x85, 0x3b, 0x9e, 0xf0, 0xbc, 0x02, 0xd7, 0x76, 0xb8, 0x5a, 0x4d, 0xa9,
//                         0x4b, 0xd3, 0x0c, 0xa8, 0xb7, 0x96, 0xa4, 0x87, 0xe4, 0x6c, 0xeb, 0x85, 0xc8, 0x2c, 0xba, 0x76,
//                         0x9a, 0x89, 0x53, 0x35, 0xcd, 0xb8, 0x77, 0xa3, 0x99, 0xdc, 0xab, 0x42, 0x75, 0xf9, 0xc5, 0x0a,
//                         0xe6, 0x9d, 0xb4, 0x32, 0x63, 0x80, 0xe9, 0xce, 0xb2, 0x4b, 0x22, 0xe1, 0x13, 0xf7, 0x80, 0xb1,
//                         0x03, 0x81, 0x80,
//                         0x64, 0xf0, 0xf2, 0x94, 0x94, 0x41, 0x56, 0x13, 0x95, 0x93, 0xad, 0xd5, 0xce, 0x10, 0x26, 0x49,
//                         0xda, 0xc7, 0x00, 0xcc, 0xb0, 0xea, 0x05, 0xe7, 0xd1, 0xb0, 0x0c, 0xf9, 0xe9, 0x19, 0x36, 0xf1,
//                         0xe4, 0x23, 0x21, 0x6a, 0x64, 0x24, 0x3d, 0x32, 0xf6, 0x60, 0x16, 0x08, 0xa8, 0xd4, 0x87, 0x0f,
//                         0xfe, 0x0a, 0x96, 0x7f, 0x20, 0xb5, 0xde, 0x37, 0x6e, 0xa3, 0x0f, 0xa9, 0x6a, 0xd9, 0x87, 0x99,
//                         0x22, 0x85, 0xeb, 0x5a, 0x7f, 0xeb, 0xb0, 0xe8, 0x54, 0xe7, 0xfc, 0xd5, 0x95, 0x87, 0x0d, 0x4d,
//                         0x5a, 0xbf, 0x19, 0x45, 0xef, 0xb7, 0xb5, 0xf4, 0xa0, 0x33, 0x28, 0x01, 0xa8, 0x9e, 0x3c, 0x86,
//                         0x15, 0xb0, 0xad, 0x22, 0x83, 0x36, 0xb1, 0xa7, 0x86, 0xbd, 0xca, 0xe0, 0x40, 0x78, 0x5d, 0xf2,
//                         0xea, 0x9d, 0x26, 0xf1, 0x30, 0xf0, 0x94, 0x96, 0x5c, 0x25, 0xb6, 0xa3, 0x89, 0x77, 0x6d, 0x89,
//                         0x01, 0x81, 0x80,
//                         0x00, 0xee, 0xea, 0xd9, 0xdb, 0x94, 0x8c, 0x25, 0xa8, 0x08, 0xde, 0x71, 0x45, 0x8d, 0xa6, 0x8f,
//                         0xd2, 0xe4, 0x3e, 0x59, 0x1b, 0x13, 0xdb, 0x56, 0xf8, 0xae, 0xd9, 0xc1, 0xde, 0x9b, 0x13, 0xa5,
//                         0xae, 0x63, 0xea, 0xb6, 0x6d, 0xc2, 0x51, 0xcc, 0x6b, 0xc3, 0x1e, 0x56, 0xa3, 0x3f, 0x55, 0x6f,
//                         0xe7, 0x57, 0xd4, 0x2e, 0x5a, 0xc3, 0xf5, 0x99, 0x1b, 0xeb, 0x92, 0x67, 0xf1, 0x60, 0xd6, 0x5f,
//                         0x16, 0x5d, 0xb8, 0xfe, 0x38, 0x30, 0x0f, 0x64, 0x6a, 0x86, 0x86, 0x93, 0x68, 0x35, 0xbd, 0xb2,
//                         0xd7, 0x24, 0xf5, 0x69, 0xfd, 0xc5, 0x90, 0x9b, 0x4a, 0x95, 0xd4, 0xd3, 0x3f, 0xf5, 0x0d, 0x63,
//                         0x8c, 0xbf, 0x39, 0x35, 0xb7, 0xe8, 0x0b, 0xf4, 0x09, 0xcd, 0xcf, 0x11, 0xab, 0x2c, 0xdf, 0xd6,
//                         0xaf, 0x2b, 0xde, 0xea, 0x52, 0x86, 0x51, 0x35, 0x62, 0x1c, 0x7e, 0xd6, 0x73, 0xeb, 0xf2, 0x32,
//                         0x04, 0x81, 0x80,
//                         0xb7, 0xea, 0x81, 0x05, 0x66, 0xa0, 0xc8, 0xaf, 0x89, 0x0a, 0x7b, 0x64, 0x02, 0x65, 0x71, 0xba,
//                         0xf7, 0x2c, 0x98, 0xb7, 0x06, 0xa1, 0x6d, 0x47, 0xf5, 0x26, 0xa4, 0x3c, 0x98, 0xf9, 0x04, 0xe7,
//                         0x88, 0xb0, 0x90, 0x4f, 0x8e, 0xbf, 0xd6, 0x9a, 0x0e, 0x5c, 0x5f, 0x3d, 0x39, 0xde, 0x52, 0x0b,
//                         0xcd, 0x3f, 0xde, 0x0f, 0x02, 0x9a, 0x96, 0xd8, 0x11, 0x8b, 0x20, 0x6f, 0xae, 0x1f, 0xeb, 0x4e,
//                         0xec, 0x8a, 0x0a, 0x82, 0xb4, 0xba, 0xcd, 0xb8, 0x8a, 0xf4, 0xdf, 0xa6, 0x38, 0x28, 0xc9, 0x4c,
//                         0x6c, 0xdc, 0x44, 0x6e, 0xb5, 0xdd, 0x52, 0x46, 0x24, 0x26, 0x9d, 0x07, 0x55, 0xe4, 0x12, 0xc0,
//                         0x4e, 0x3f, 0xba, 0x5a, 0x39, 0xd6, 0x7e, 0xc0, 0xb8, 0x32, 0x92, 0x72, 0x10, 0xe4, 0xa2, 0x76,
//                         0x29, 0x22, 0xe3, 0xe3, 0x53, 0xd9, 0xbd, 0xe2, 0xe9, 0x55, 0xb8, 0xd2, 0x07, 0x3a, 0x21, 0x29
//                 }),
//
//             KeyType.P256 =>
//                 PivPrivateKey.Create(new byte[] {
//                         0x06, 0x20,
//                         0xba, 0x29, 0x7a, 0xc6, 0x64, 0x62, 0xef, 0x6c, 0xd0, 0x89, 0x76, 0x5c, 0xbd, 0x46, 0x52, 0x2b,
//                         0xb0, 0x48, 0x0e, 0x85, 0x49, 0x15, 0x85, 0xe7, 0x7a, 0x74, 0x3c, 0x8e, 0x03, 0x59, 0x8d, 0x3a
//                 }),
//
//             _ =>
//                 PivPrivateKey.Create(new byte[] {
//                         0x06, 0x30,
//                         0x47, 0x85, 0xde, 0x3a, 0xff, 0x10, 0x0d, 0x67, 0xa7, 0x26, 0x30, 0x62, 0x73, 0x45, 0xfd, 0xce,
//                         0xeb, 0xb9, 0xbe, 0x4c, 0x93, 0x42, 0xcd, 0x6a, 0x84, 0xd6, 0x8e, 0x00, 0x70, 0x70, 0x4c, 0x66,
//                         0x63, 0x53, 0xa0, 0x2c, 0xb9, 0xa7, 0x61, 0xcf, 0x56, 0xf0, 0x45, 0x07, 0xa6, 0xfb, 0x9f, 0x5a
//                 }),
//         };
//     }
}

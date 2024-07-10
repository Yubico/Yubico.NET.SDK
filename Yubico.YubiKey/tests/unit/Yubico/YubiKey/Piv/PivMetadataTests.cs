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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class PivMetadataTests
    {
        [Fact]
        public void SessionMetadata_BadSlot_CorrectException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 3;
            yubiKey.AvailableUsbCapabilities = YubiKeyCapabilities.Piv;

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<ArgumentException>(() => pivSession.GetMetadata(slotNumber: 0xff));
            }
        }

        [Fact]
        public void SessionMetadata_BadVersion_CorrectException()
        {
            var yubiKey = new HollowYubiKeyDevice();
            yubiKey.FirmwareVersion.Major = 5;
            yubiKey.FirmwareVersion.Minor = 2;

            using (var pivSession = new PivSession(yubiKey))
            {
                _ = Assert.Throws<NotSupportedException>(() => pivSession.GetMetadata(slotNumber: 0x9A));
            }
        }

        [Fact]
        public void Constructor_InvalidSlot_CorrectException()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            _ = Assert.Throws<ArgumentException>(() => new PivMetadata(testData, slotNumber: 0));
        }

        [Fact]
        public void Constructor_ValidInputMgmtKey_CorrectAlgorithm()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.Algorithm == PivAlgorithm.TripleDes);
        }

        [Fact]
        public void Constructor_ValidInputMgmtKey_CorrectPolicy()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.PinPolicy == PivPinPolicy.Default);
            Assert.True(pivMetadata.TouchPolicy == PivTouchPolicy.Never);
        }

        [Fact]
        public void Constructor_ValidInputMgmtKey_CorrectStatus()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.KeyStatus == PivKeyStatus.Default);
        }

        [Fact]
        public void Constructor_ValidInputMgmtKey_NoKey()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.PublicKey.Algorithm == PivAlgorithm.None);
        }

        [Fact]
        public void Constructor_ValidInputMgmtKey_NoRetries()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.RetryCount == -1);
            Assert.True(pivMetadata.RetriesRemaining == -1);
        }

        [Fact]
        public void Constructor_InvalidTagLength01_CorrectException()
        {
            // Tag 01 must have a length of 1.
            byte[] testData =
            {
                0x01, 0x02, 0x03, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x01, 0x01
            };

            _ = Assert.Throws<InvalidOperationException>(() => new PivMetadata(testData, slotNumber: 0x9B));
        }

        [Fact]
        public void Constructor_InvalidTagLength02_CorrectException()
        {
            // Tag 02 must have a length of 2.
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x01, 0x01, 0x05, 0x01, 0x01
            };

            _ = Assert.Throws<InvalidOperationException>(() => new PivMetadata(testData, slotNumber: 0x9B));
        }

        [Fact]
        public void Constructor_InvalidTagLength05_CorrectException()
        {
            // Tag 05 must have a length of 1.
            byte[] testData =
            {
                0x01, 0x01, 0x03, 0x02, 0x02, 0x00, 0x01, 0x05, 0x02, 0x01, 0x01
            };

            _ = Assert.Throws<InvalidOperationException>(() => new PivMetadata(testData, slotNumber: 0x9B));
        }

        [Fact]
        public void Constructor_ValidInputPin_CorrectAlgorithm()
        {
            byte[] testData =
            {
                0x01, 0x01, 0xFF, 0x05, 0x01, 0x01, 0x06, 0x02, 0x05, 0x05
            };

            var pivMetadata = new PivMetadata(testData, PivSlot.Pin);

            Assert.True(pivMetadata.Algorithm == PivAlgorithm.Pin);
        }

        [Fact]
        public void Constructor_ValidInputPin_CorrectStatus()
        {
            byte[] testData =
            {
                0x01, 0x01, 0xFF, 0x05, 0x01, 0x00, 0x06, 0x02, 0x05, 0x05
            };

            var pivMetadata = new PivMetadata(testData, PivSlot.Pin);

            Assert.True(pivMetadata.KeyStatus == PivKeyStatus.NotDefault);
        }

        [Fact]
        public void Constructor_ValidInputPin_CorrectRetries()
        {
            byte[] testData =
            {
                0x01, 0x01, 0xFF, 0x05, 0x01, 0x00, 0x06, 0x02, 0x04, 0x05
            };

            var pivMetadata = new PivMetadata(testData, PivSlot.Pin);

            Assert.True(pivMetadata.RetryCount == 4);
            Assert.True(pivMetadata.RetriesRemaining == 5);
        }

        [Fact]
        public void Constructor_ValidInputPin_NoPolicy()
        {
            byte[] testData =
            {
                0x01, 0x01, 0xFF, 0x05, 0x01, 0x00, 0x06, 0x02, 0x04, 0x05
            };

            var pivMetadata = new PivMetadata(testData, PivSlot.Pin);

            Assert.True(pivMetadata.PinPolicy == PivPinPolicy.None);
            Assert.True(pivMetadata.TouchPolicy == PivTouchPolicy.None);
        }

        [Fact]
        public void Constructor_ValidInputPin_NoKey()
        {
            byte[] testData =
            {
                0x01, 0x01, 0xFF, 0x05, 0x01, 0x00, 0x06, 0x02, 0x04, 0x05
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x80);

            Assert.True(pivMetadata.PublicKey.Algorithm == PivAlgorithm.None);
        }

        [Fact]
        public void Constructor_InvalidTagLength06_CorrectException()
        {
            // Tag 06 must have a length of 2.
            byte[] testData =
            {
                0x01, 0x01, 0xFF, 0x05, 0x01, 0x00, 0x06, 0x03, 0x04, 0x05, 0x06
            };

            _ = Assert.Throws<InvalidOperationException>(() => new PivMetadata(testData, slotNumber: 0x9B));
        }

        [Fact]
        public void Constructor_ValidInputF9_CorrectAlgorithm()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x07, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x01, 0x02, 0x04, 0x82, 0x01, 0x09, 0x81, 0x82,
                0x01, 0x00, 0xAC, 0x45, 0x96, 0x66, 0x97, 0x9F,
                0x96, 0xFF, 0xD9, 0x04, 0x4F, 0x24, 0x5D, 0x5D,
                0x4F, 0x99, 0x3C, 0xD3, 0x21, 0xA5, 0xFD, 0x2A,
                0x63, 0xFC, 0x2B, 0xED, 0x8F, 0x58, 0xEF, 0xA7,
                0xC2, 0xE3, 0x79, 0x68, 0x94, 0x0D, 0x53, 0x30,
                0x23, 0xEC, 0x6C, 0xA6, 0x65, 0xB7, 0xE3, 0xCB,
                0xC6, 0x27, 0xBE, 0x72, 0x92, 0xB0, 0x38, 0xD4,
                0x7D, 0xE1, 0x54, 0x86, 0xBF, 0x75, 0x07, 0x16,
                0xF8, 0x07, 0xE4, 0x7E, 0xA3, 0x6B, 0xAB, 0xDF,
                0xD6, 0x9D, 0xE1, 0xC7, 0x7B, 0xA9, 0xE9, 0xD1,
                0x3E, 0x97, 0x8C, 0x3E, 0xA0, 0x57, 0xC9, 0x07,
                0x30, 0x58, 0xE4, 0x9B, 0xAC, 0x78, 0x69, 0xA1,
                0x6B, 0x71, 0x7C, 0xF0, 0x78, 0x9B, 0xC3, 0x7F,
                0x4A, 0xC1, 0xCB, 0x40, 0xBD, 0x94, 0x7F, 0xF4,
                0x19, 0x36, 0xBB, 0x41, 0xCC, 0x35, 0xCD, 0x2E,
                0xDB, 0xB1, 0x97, 0xA8, 0xB0, 0x05, 0x9C, 0xE7,
                0x00, 0x2D, 0xBF, 0x35, 0xC2, 0x2A, 0xE4, 0x92,
                0x91, 0xF3, 0xFC, 0x86, 0xC4, 0xD1, 0xB4, 0x58,
                0x3C, 0x46, 0x51, 0x5F, 0xBF, 0x94, 0xD4, 0xF0,
                0x7E, 0xE7, 0x4A, 0x1B, 0x85, 0xF1, 0xA3, 0x3A,
                0xEC, 0xB5, 0x1C, 0x0E, 0x86, 0x90, 0x5F, 0x22,
                0x09, 0xF1, 0xA5, 0xC8, 0x6B, 0xBB, 0x36, 0x5A,
                0x63, 0x80, 0xF5, 0xDE, 0x46, 0xE7, 0x51, 0xD8,
                0xF0, 0x21, 0x85, 0x73, 0x80, 0x08, 0x01, 0x14,
                0xA7, 0x3B, 0xB9, 0x5F, 0x80, 0x15, 0x15, 0xA1,
                0xE7, 0x7E, 0x53, 0x4D, 0xF3, 0x9E, 0x5B, 0xBA,
                0x7B, 0xB6, 0x3C, 0x1F, 0xB6, 0x85, 0x18, 0xA8,
                0x99, 0x0A, 0x29, 0x47, 0x06, 0x95, 0xC8, 0x94,
                0x78, 0x04, 0x06, 0xB8, 0xD0, 0x65, 0x76, 0x15,
                0x5D, 0x5E, 0x8D, 0x03, 0x10, 0x98, 0xCE, 0x54,
                0xD8, 0x2F, 0xE6, 0xEE, 0xDA, 0x47, 0x8E, 0xBB,
                0xE1, 0x59, 0x2E, 0xD3, 0xB8, 0xDD, 0x16, 0x1B,
                0x9A, 0x71, 0x82, 0x03, 0x01, 0x00, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0xF9);

            Assert.True(pivMetadata.Algorithm == PivAlgorithm.Rsa2048);
        }

        [Fact]
        public void Constructor_ValidInputF9_CorrectPolicy()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x07, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x01, 0x02, 0x04, 0x82, 0x01, 0x09, 0x81, 0x82,
                0x01, 0x00, 0xAC, 0x45, 0x96, 0x66, 0x97, 0x9F,
                0x96, 0xFF, 0xD9, 0x04, 0x4F, 0x24, 0x5D, 0x5D,
                0x4F, 0x99, 0x3C, 0xD3, 0x21, 0xA5, 0xFD, 0x2A,
                0x63, 0xFC, 0x2B, 0xED, 0x8F, 0x58, 0xEF, 0xA7,
                0xC2, 0xE3, 0x79, 0x68, 0x94, 0x0D, 0x53, 0x30,
                0x23, 0xEC, 0x6C, 0xA6, 0x65, 0xB7, 0xE3, 0xCB,
                0xC6, 0x27, 0xBE, 0x72, 0x92, 0xB0, 0x38, 0xD4,
                0x7D, 0xE1, 0x54, 0x86, 0xBF, 0x75, 0x07, 0x16,
                0xF8, 0x07, 0xE4, 0x7E, 0xA3, 0x6B, 0xAB, 0xDF,
                0xD6, 0x9D, 0xE1, 0xC7, 0x7B, 0xA9, 0xE9, 0xD1,
                0x3E, 0x97, 0x8C, 0x3E, 0xA0, 0x57, 0xC9, 0x07,
                0x30, 0x58, 0xE4, 0x9B, 0xAC, 0x78, 0x69, 0xA1,
                0x6B, 0x71, 0x7C, 0xF0, 0x78, 0x9B, 0xC3, 0x7F,
                0x4A, 0xC1, 0xCB, 0x40, 0xBD, 0x94, 0x7F, 0xF4,
                0x19, 0x36, 0xBB, 0x41, 0xCC, 0x35, 0xCD, 0x2E,
                0xDB, 0xB1, 0x97, 0xA8, 0xB0, 0x05, 0x9C, 0xE7,
                0x00, 0x2D, 0xBF, 0x35, 0xC2, 0x2A, 0xE4, 0x92,
                0x91, 0xF3, 0xFC, 0x86, 0xC4, 0xD1, 0xB4, 0x58,
                0x3C, 0x46, 0x51, 0x5F, 0xBF, 0x94, 0xD4, 0xF0,
                0x7E, 0xE7, 0x4A, 0x1B, 0x85, 0xF1, 0xA3, 0x3A,
                0xEC, 0xB5, 0x1C, 0x0E, 0x86, 0x90, 0x5F, 0x22,
                0x09, 0xF1, 0xA5, 0xC8, 0x6B, 0xBB, 0x36, 0x5A,
                0x63, 0x80, 0xF5, 0xDE, 0x46, 0xE7, 0x51, 0xD8,
                0xF0, 0x21, 0x85, 0x73, 0x80, 0x08, 0x01, 0x14,
                0xA7, 0x3B, 0xB9, 0x5F, 0x80, 0x15, 0x15, 0xA1,
                0xE7, 0x7E, 0x53, 0x4D, 0xF3, 0x9E, 0x5B, 0xBA,
                0x7B, 0xB6, 0x3C, 0x1F, 0xB6, 0x85, 0x18, 0xA8,
                0x99, 0x0A, 0x29, 0x47, 0x06, 0x95, 0xC8, 0x94,
                0x78, 0x04, 0x06, 0xB8, 0xD0, 0x65, 0x76, 0x15,
                0x5D, 0x5E, 0x8D, 0x03, 0x10, 0x98, 0xCE, 0x54,
                0xD8, 0x2F, 0xE6, 0xEE, 0xDA, 0x47, 0x8E, 0xBB,
                0xE1, 0x59, 0x2E, 0xD3, 0xB8, 0xDD, 0x16, 0x1B,
                0x9A, 0x71, 0x82, 0x03, 0x01, 0x00, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0xF9);

            Assert.True(pivMetadata.PinPolicy == PivPinPolicy.Once);
            Assert.True(pivMetadata.TouchPolicy == PivTouchPolicy.Never);
        }

        [Fact]
        public void Constructor_ValidInputF9_CorrectStatus()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x07, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x01, 0x02, 0x04, 0x82, 0x01, 0x09, 0x81, 0x82,
                0x01, 0x00, 0xAC, 0x45, 0x96, 0x66, 0x97, 0x9F,
                0x96, 0xFF, 0xD9, 0x04, 0x4F, 0x24, 0x5D, 0x5D,
                0x4F, 0x99, 0x3C, 0xD3, 0x21, 0xA5, 0xFD, 0x2A,
                0x63, 0xFC, 0x2B, 0xED, 0x8F, 0x58, 0xEF, 0xA7,
                0xC2, 0xE3, 0x79, 0x68, 0x94, 0x0D, 0x53, 0x30,
                0x23, 0xEC, 0x6C, 0xA6, 0x65, 0xB7, 0xE3, 0xCB,
                0xC6, 0x27, 0xBE, 0x72, 0x92, 0xB0, 0x38, 0xD4,
                0x7D, 0xE1, 0x54, 0x86, 0xBF, 0x75, 0x07, 0x16,
                0xF8, 0x07, 0xE4, 0x7E, 0xA3, 0x6B, 0xAB, 0xDF,
                0xD6, 0x9D, 0xE1, 0xC7, 0x7B, 0xA9, 0xE9, 0xD1,
                0x3E, 0x97, 0x8C, 0x3E, 0xA0, 0x57, 0xC9, 0x07,
                0x30, 0x58, 0xE4, 0x9B, 0xAC, 0x78, 0x69, 0xA1,
                0x6B, 0x71, 0x7C, 0xF0, 0x78, 0x9B, 0xC3, 0x7F,
                0x4A, 0xC1, 0xCB, 0x40, 0xBD, 0x94, 0x7F, 0xF4,
                0x19, 0x36, 0xBB, 0x41, 0xCC, 0x35, 0xCD, 0x2E,
                0xDB, 0xB1, 0x97, 0xA8, 0xB0, 0x05, 0x9C, 0xE7,
                0x00, 0x2D, 0xBF, 0x35, 0xC2, 0x2A, 0xE4, 0x92,
                0x91, 0xF3, 0xFC, 0x86, 0xC4, 0xD1, 0xB4, 0x58,
                0x3C, 0x46, 0x51, 0x5F, 0xBF, 0x94, 0xD4, 0xF0,
                0x7E, 0xE7, 0x4A, 0x1B, 0x85, 0xF1, 0xA3, 0x3A,
                0xEC, 0xB5, 0x1C, 0x0E, 0x86, 0x90, 0x5F, 0x22,
                0x09, 0xF1, 0xA5, 0xC8, 0x6B, 0xBB, 0x36, 0x5A,
                0x63, 0x80, 0xF5, 0xDE, 0x46, 0xE7, 0x51, 0xD8,
                0xF0, 0x21, 0x85, 0x73, 0x80, 0x08, 0x01, 0x14,
                0xA7, 0x3B, 0xB9, 0x5F, 0x80, 0x15, 0x15, 0xA1,
                0xE7, 0x7E, 0x53, 0x4D, 0xF3, 0x9E, 0x5B, 0xBA,
                0x7B, 0xB6, 0x3C, 0x1F, 0xB6, 0x85, 0x18, 0xA8,
                0x99, 0x0A, 0x29, 0x47, 0x06, 0x95, 0xC8, 0x94,
                0x78, 0x04, 0x06, 0xB8, 0xD0, 0x65, 0x76, 0x15,
                0x5D, 0x5E, 0x8D, 0x03, 0x10, 0x98, 0xCE, 0x54,
                0xD8, 0x2F, 0xE6, 0xEE, 0xDA, 0x47, 0x8E, 0xBB,
                0xE1, 0x59, 0x2E, 0xD3, 0xB8, 0xDD, 0x16, 0x1B,
                0x9A, 0x71, 0x82, 0x03, 0x01, 0x00, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0xF9);

            Assert.True(pivMetadata.KeyStatus == PivKeyStatus.Imported);
        }

        [Fact]
        public void Constructor_ValidInputF9_CorrectKey()
        {
            var keyOffset = 14;
            byte[] testData =
            {
                0x01, 0x01, 0x07, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x01, 0x02, 0x04, 0x82, 0x01, 0x09, 0x81, 0x82,
                0x01, 0x00, 0xAC, 0x45, 0x96, 0x66, 0x97, 0x9F,
                0x96, 0xFF, 0xD9, 0x04, 0x4F, 0x24, 0x5D, 0x5D,
                0x4F, 0x99, 0x3C, 0xD3, 0x21, 0xA5, 0xFD, 0x2A,
                0x63, 0xFC, 0x2B, 0xED, 0x8F, 0x58, 0xEF, 0xA7,
                0xC2, 0xE3, 0x79, 0x68, 0x94, 0x0D, 0x53, 0x30,
                0x23, 0xEC, 0x6C, 0xA6, 0x65, 0xB7, 0xE3, 0xCB,
                0xC6, 0x27, 0xBE, 0x72, 0x92, 0xB0, 0x38, 0xD4,
                0x7D, 0xE1, 0x54, 0x86, 0xBF, 0x75, 0x07, 0x16,
                0xF8, 0x07, 0xE4, 0x7E, 0xA3, 0x6B, 0xAB, 0xDF,
                0xD6, 0x9D, 0xE1, 0xC7, 0x7B, 0xA9, 0xE9, 0xD1,
                0x3E, 0x97, 0x8C, 0x3E, 0xA0, 0x57, 0xC9, 0x07,
                0x30, 0x58, 0xE4, 0x9B, 0xAC, 0x78, 0x69, 0xA1,
                0x6B, 0x71, 0x7C, 0xF0, 0x78, 0x9B, 0xC3, 0x7F,
                0x4A, 0xC1, 0xCB, 0x40, 0xBD, 0x94, 0x7F, 0xF4,
                0x19, 0x36, 0xBB, 0x41, 0xCC, 0x35, 0xCD, 0x2E,
                0xDB, 0xB1, 0x97, 0xA8, 0xB0, 0x05, 0x9C, 0xE7,
                0x00, 0x2D, 0xBF, 0x35, 0xC2, 0x2A, 0xE4, 0x92,
                0x91, 0xF3, 0xFC, 0x86, 0xC4, 0xD1, 0xB4, 0x58,
                0x3C, 0x46, 0x51, 0x5F, 0xBF, 0x94, 0xD4, 0xF0,
                0x7E, 0xE7, 0x4A, 0x1B, 0x85, 0xF1, 0xA3, 0x3A,
                0xEC, 0xB5, 0x1C, 0x0E, 0x86, 0x90, 0x5F, 0x22,
                0x09, 0xF1, 0xA5, 0xC8, 0x6B, 0xBB, 0x36, 0x5A,
                0x63, 0x80, 0xF5, 0xDE, 0x46, 0xE7, 0x51, 0xD8,
                0xF0, 0x21, 0x85, 0x73, 0x80, 0x08, 0x01, 0x14,
                0xA7, 0x3B, 0xB9, 0x5F, 0x80, 0x15, 0x15, 0xA1,
                0xE7, 0x7E, 0x53, 0x4D, 0xF3, 0x9E, 0x5B, 0xBA,
                0x7B, 0xB6, 0x3C, 0x1F, 0xB6, 0x85, 0x18, 0xA8,
                0x99, 0x0A, 0x29, 0x47, 0x06, 0x95, 0xC8, 0x94,
                0x78, 0x04, 0x06, 0xB8, 0xD0, 0x65, 0x76, 0x15,
                0x5D, 0x5E, 0x8D, 0x03, 0x10, 0x98, 0xCE, 0x54,
                0xD8, 0x2F, 0xE6, 0xEE, 0xDA, 0x47, 0x8E, 0xBB,
                0xE1, 0x59, 0x2E, 0xD3, 0xB8, 0xDD, 0x16, 0x1B,
                0x9A, 0x71, 0x82, 0x03, 0x01, 0x00, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0xF9);

            Assert.False(pivMetadata.PublicKey.YubiKeyEncodedPublicKey.IsEmpty);
            if (pivMetadata.PublicKey.YubiKeyEncodedPublicKey.IsEmpty)
            {
                return;
            }

            var keyDataSpan = new Span<byte>(testData);
            keyDataSpan = keyDataSpan[keyOffset..];

            var compareResult = keyDataSpan.SequenceEqual(pivMetadata.PublicKey.YubiKeyEncodedPublicKey.Span);

            Assert.True(compareResult);
        }

        [Fact]
        public void Constructor_ValidInputF9_NoRetries()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x07, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x01, 0x02, 0x04, 0x82, 0x01, 0x09, 0x81, 0x82,
                0x01, 0x00, 0xAC, 0x45, 0x96, 0x66, 0x97, 0x9F,
                0x96, 0xFF, 0xD9, 0x04, 0x4F, 0x24, 0x5D, 0x5D,
                0x4F, 0x99, 0x3C, 0xD3, 0x21, 0xA5, 0xFD, 0x2A,
                0x63, 0xFC, 0x2B, 0xED, 0x8F, 0x58, 0xEF, 0xA7,
                0xC2, 0xE3, 0x79, 0x68, 0x94, 0x0D, 0x53, 0x30,
                0x23, 0xEC, 0x6C, 0xA6, 0x65, 0xB7, 0xE3, 0xCB,
                0xC6, 0x27, 0xBE, 0x72, 0x92, 0xB0, 0x38, 0xD4,
                0x7D, 0xE1, 0x54, 0x86, 0xBF, 0x75, 0x07, 0x16,
                0xF8, 0x07, 0xE4, 0x7E, 0xA3, 0x6B, 0xAB, 0xDF,
                0xD6, 0x9D, 0xE1, 0xC7, 0x7B, 0xA9, 0xE9, 0xD1,
                0x3E, 0x97, 0x8C, 0x3E, 0xA0, 0x57, 0xC9, 0x07,
                0x30, 0x58, 0xE4, 0x9B, 0xAC, 0x78, 0x69, 0xA1,
                0x6B, 0x71, 0x7C, 0xF0, 0x78, 0x9B, 0xC3, 0x7F,
                0x4A, 0xC1, 0xCB, 0x40, 0xBD, 0x94, 0x7F, 0xF4,
                0x19, 0x36, 0xBB, 0x41, 0xCC, 0x35, 0xCD, 0x2E,
                0xDB, 0xB1, 0x97, 0xA8, 0xB0, 0x05, 0x9C, 0xE7,
                0x00, 0x2D, 0xBF, 0x35, 0xC2, 0x2A, 0xE4, 0x92,
                0x91, 0xF3, 0xFC, 0x86, 0xC4, 0xD1, 0xB4, 0x58,
                0x3C, 0x46, 0x51, 0x5F, 0xBF, 0x94, 0xD4, 0xF0,
                0x7E, 0xE7, 0x4A, 0x1B, 0x85, 0xF1, 0xA3, 0x3A,
                0xEC, 0xB5, 0x1C, 0x0E, 0x86, 0x90, 0x5F, 0x22,
                0x09, 0xF1, 0xA5, 0xC8, 0x6B, 0xBB, 0x36, 0x5A,
                0x63, 0x80, 0xF5, 0xDE, 0x46, 0xE7, 0x51, 0xD8,
                0xF0, 0x21, 0x85, 0x73, 0x80, 0x08, 0x01, 0x14,
                0xA7, 0x3B, 0xB9, 0x5F, 0x80, 0x15, 0x15, 0xA1,
                0xE7, 0x7E, 0x53, 0x4D, 0xF3, 0x9E, 0x5B, 0xBA,
                0x7B, 0xB6, 0x3C, 0x1F, 0xB6, 0x85, 0x18, 0xA8,
                0x99, 0x0A, 0x29, 0x47, 0x06, 0x95, 0xC8, 0x94,
                0x78, 0x04, 0x06, 0xB8, 0xD0, 0x65, 0x76, 0x15,
                0x5D, 0x5E, 0x8D, 0x03, 0x10, 0x98, 0xCE, 0x54,
                0xD8, 0x2F, 0xE6, 0xEE, 0xDA, 0x47, 0x8E, 0xBB,
                0xE1, 0x59, 0x2E, 0xD3, 0xB8, 0xDD, 0x16, 0x1B,
                0x9A, 0x71, 0x82, 0x03, 0x01, 0x00, 0x01
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0xF9);

            Assert.True(pivMetadata.RetryCount == -1);
            Assert.True(pivMetadata.RetriesRemaining == -1);
        }

        [Fact]
        public void Constructor_InvalidTagLength03_CorrectException()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x02, 0x01, 0x22, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            };

            _ = Assert.Throws<InvalidOperationException>(() => new PivMetadata(testData, slotNumber: 0x9A));
        }

        [Fact]
        public void Constructor_ValidInput9A_CorrectAlgorithm()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x02, 0x01, 0x03,
                0x01, 0x01, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9A);

            Assert.True(pivMetadata.Algorithm == PivAlgorithm.EccP256);
        }

        [Fact]
        public void Constructor_ValidInput9A_CorrectPolicy()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x03, 0x03, 0x03,
                0x01, 0x01, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9A);

            Assert.True(pivMetadata.PinPolicy == PivPinPolicy.Always);
            Assert.True(pivMetadata.TouchPolicy == PivTouchPolicy.Cached);
        }

        [Fact]
        public void Constructor_ValidInput9A_CorrectStatus()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x03, 0x03, 0x03,
                0x01, 0x01, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9A);

            Assert.True(pivMetadata.KeyStatus == PivKeyStatus.Generated);
        }

        [Fact]
        public void Constructor_ValidInput9A_CorrectKey()
        {
            var keyOffset = 12;
            byte[] testData =
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x03, 0x03, 0x03,
                0x01, 0x01, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0xF9);

            Assert.False(pivMetadata.PublicKey.YubiKeyEncodedPublicKey.IsEmpty);
            if (pivMetadata.PublicKey.YubiKeyEncodedPublicKey.IsEmpty)
            {
                return;
            }

            var keyDataSpan = new Span<byte>(testData);
            keyDataSpan = keyDataSpan[keyOffset..];

            var compareResult = keyDataSpan.SequenceEqual(pivMetadata.PublicKey.YubiKeyEncodedPublicKey.Span);

            Assert.True(compareResult);
        }

        [Fact]
        public void Constructor_ValidInput9A_NoRetries()
        {
            byte[] testData =
            {
                0x01, 0x01, 0x11, 0x02, 0x02, 0x03, 0x03, 0x03,
                0x01, 0x01, 0x04, 0x43, 0x86, 0x41, 0x04, 0xC4,
                0x17, 0x7F, 0x2B, 0x96, 0x8F, 0x9C, 0x00, 0x0C,
                0x4F, 0x3D, 0x2B, 0x88, 0xB0, 0xAB, 0x5B, 0x0C,
                0x3B, 0x19, 0x42, 0x63, 0x20, 0x8C, 0xA1, 0x2F,
                0xEE, 0x1C, 0xB4, 0xD8, 0x81, 0x96, 0x9F, 0xD8,
                0xC8, 0xD0, 0x8D, 0xD1, 0xBB, 0x66, 0x58, 0x00,
                0x26, 0x7D, 0x05, 0x34, 0xA8, 0xA3, 0x30, 0xD1,
                0x59, 0xDE, 0x66, 0x01, 0x0E, 0x3F, 0x21, 0x13,
                0x29, 0xC5, 0x98, 0x56, 0x07, 0xB5, 0x26
            };

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9A);

            Assert.True(pivMetadata.RetryCount == -1);
            Assert.True(pivMetadata.RetriesRemaining == -1);
        }

        [Fact]
        public void Constructor_NoData_NoAlgorithm()
        {
            var testData = Array.Empty<byte>();

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.Algorithm == PivAlgorithm.None);
        }

        [Fact]
        public void Constructor_NoData_NoPolicy()
        {
            var testData = Array.Empty<byte>();

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.PinPolicy == PivPinPolicy.None);
            Assert.True(pivMetadata.TouchPolicy == PivTouchPolicy.None);
        }

        [Fact]
        public void Constructor_NoData_NoStatus()
        {
            var testData = Array.Empty<byte>();

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x9B);

            Assert.True(pivMetadata.KeyStatus == PivKeyStatus.Unknown);
        }

        [Fact]
        public void Constructor_NoData_NoRetries()
        {
            var testData = Array.Empty<byte>();

            var pivMetadata = new PivMetadata(testData, slotNumber: 0x80);

            Assert.True(pivMetadata.RetryCount == -1);
            Assert.True(pivMetadata.RetriesRemaining == -1);
        }

        [Fact]
        public void Constructor_NoData_NoKey()
        {
            var testData = Array.Empty<byte>();

            var pivMetadata = new PivMetadata(testData, PivSlot.Pin);

            Assert.True(pivMetadata.PublicKey.Algorithm == PivAlgorithm.None);
        }
    }
}

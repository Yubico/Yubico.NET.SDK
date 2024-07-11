// Copyright 2022 Yubico AB
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

namespace Yubico.YubiKey.YubiHsmAuth
{
    public class CredentialTests
    {
        #region touch property

        [Fact]
        public void TouchRequired_SetGetTrue_ReturnsTrue()
        {
            var expectedTouch = true;

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                touchRequired: false)
            {
                TouchRequired = expectedTouch
            };

            Assert.Equal(expectedTouch, cred.TouchRequired);
        }

        #endregion

        #region constructor

        [Fact]
        public void Constructor_KeyTypeAes128_ObjectKeyTypeAes128()
        {
            var expectedKeyType = CryptographicKeyType.Aes128;

            var cred = new Credential(
                expectedKeyType,
                "test key",
                touchRequired: false);

            Assert.Equal(expectedKeyType, cred.KeyType);
        }

        [Fact]
        public void Constructor_KeyTypeNone_ThrowsArgOutOfRangeException()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => new Credential(
                    CryptographicKeyType.None,
                    "test key",
                    touchRequired: false));
        }

        [Fact]
        public void Constructor_KeyTypeNegative1_ThrowsArgOutOfRangeException()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => new Credential(
                    (CryptographicKeyType)(-1),
                    "test key",
                    touchRequired: false));
        }

        [Fact]
        public void Constructor_LabelTestKey_ObjectLabelTestKey()
        {
            var expectedLabel = "test key";

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                expectedLabel,
                touchRequired: false);

            Assert.Equal(expectedLabel, cred.Label);
        }

        [Theory]
        [InlineData(Credential.MinLabelByteCount - 1)]
        [InlineData(Credential.MaxLabelByteCount + 1)]
        public void Constructor_LabelInvalidLength_ThrowsArgException(int strLength)
        {
            var expectedLabel = new string(c: 'a', strLength);

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => new Credential(
                CryptographicKeyType.Aes128,
                expectedLabel,
                touchRequired: false));
        }

        [Fact]
        public void Constructor_TouchRequiredTrue_ObjectTouchRequiredTrue()
        {
            var expectedTouchRequired = true;

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                expectedTouchRequired);

            Assert.Equal(expectedTouchRequired, cred.TouchRequired);
        }

        #endregion

        #region KeyType Property

        [Fact]
        public void KeyType_GetSetAes128_KeyTypeIsAes128()
        {
            var expectedKeyType = CryptographicKeyType.Aes128;

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                touchRequired: false)
            {
                KeyType = expectedKeyType
            };

            Assert.Equal(expectedKeyType, cred.KeyType);
        }

        [Fact]
        public void KeyType_SetNegative1_ThrowsArgOutOfRange()
        {
            var invalidKeyType = (CryptographicKeyType)(-1);

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                touchRequired: false);

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => cred.KeyType = invalidKeyType);
        }

        [Fact]
        public void KeyType_SetNone_ThrowsArgOutOfRange()
        {
            var invalidKeyType = CryptographicKeyType.None;

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                touchRequired: false);

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => cred.KeyType = invalidKeyType);
        }

        #endregion

        #region label property

        [Fact]
        public void MinLabelLength_Get_Returns1()
        {
            Assert.Equal(expected: 1, Credential.MinLabelByteCount);
        }

        [Fact]
        public void MaxLabelLength_Get_Returns64()
        {
            Assert.Equal(expected: 64, Credential.MaxLabelByteCount);
        }

        [Theory]
        [InlineData(Credential.MinLabelByteCount)]
        [InlineData(Credential.MaxLabelByteCount)]
        public void Label_SetGetLabel_ReturnsMatchingString(int labelLength)
        {
            var expectedLabel = new string(c: 'a', labelLength);

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "old label",
                touchRequired: false)
            {
                Label = expectedLabel
            };

            Assert.Equal(expectedLabel, cred.Label);
        }

        [Fact]
        public void Label_SetNonUtf8Character_ThrowsArgException()
        {
            var expectedLabel = "abc\uD801\uD802d";

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "old label",
                touchRequired: false);

            _ = Assert.ThrowsAny<ArgumentException>(
                () => cred.Label = expectedLabel);
        }

        [Theory]
        [InlineData(Credential.MinLabelByteCount - 1)]
        [InlineData(Credential.MaxLabelByteCount + 1)]
        public void Label_SetInvalidLabelLength_ThrowsArgOutOfRangeException(int labelLength)
        {
            var expectedLabel = new string(c: 'a', labelLength);

            var cred = new Credential(
                CryptographicKeyType.Aes128,
                "old label",
                touchRequired: false);

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => cred.Label = expectedLabel);
        }

        #endregion
    }
}

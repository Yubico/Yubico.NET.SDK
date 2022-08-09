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
        #region constructor
        [Fact]
        public void Constructor_KeyTypeAes128_ObjectKeyTypeAes128()
        {
            CryptographicKeyType expectedKeyType = CryptographicKeyType.Aes128;
            
            Credential cred = new Credential(
                expectedKeyType,
                "test key",
                false);

            Assert.Equal(expectedKeyType, cred.KeyType);
        }

        [Fact]
        public void Constructor_KeyTypeNone_ThrowsArgOutOfRangeException()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => new Credential(
                    CryptographicKeyType.None,
                    "test key",
                    false));
        }

        [Fact]
        public void Constructor_KeyTypeNegative1_ThrowsArgOutOfRangeException()
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => new Credential(
                    (CryptographicKeyType)(-1),
                    "test key",
                    false));
        }

        [Fact]
        public void Constructor_LabelTestKey_ObjectLabelTestKey()
        {
            string expectedLabel = "test key";
            
            Credential cred = new Credential(
                CryptographicKeyType.Aes128,
                expectedLabel,
                false);

            Assert.Equal(expectedLabel, cred.Label);
        }

        [Theory]
        [InlineData(Credential.MinLabelLength - 1)]
        [InlineData(Credential.MaxLabelLength + 1)]
        public void Constructor_LabelInvalidLength_ThrowsArgException(int strLength)
        {
            string expectedLabel = new string('a', strLength);

            _ = Assert.Throws<ArgumentException>(() => new Credential(
                    CryptographicKeyType.Aes128,
                    expectedLabel,
                    false));
        }

        [Fact]
        public void Constructor_TouchRequiredTrue_ObjectTouchRequiredTrue()
        {
            bool expectedTouchRequired = true;

            Credential cred = new Credential(
                    CryptographicKeyType.Aes128,
                    "test key",
                    expectedTouchRequired);

            Assert.Equal(expectedTouchRequired, cred.TouchRequired);
        }
        #endregion

        #region KeyType Property
        [Fact]
        public void KeyType_SetNegative1_ThrowsArgOutOfRange()
        {
            CryptographicKeyType invalidKeyType = (CryptographicKeyType)(-1);

            Credential cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                false);

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => cred.KeyType = invalidKeyType);
        }

        [Fact]
        public void KeyType_SetNone_ThrowsArgOutOfRange()
        {
            CryptographicKeyType invalidKeyType = CryptographicKeyType.None;

            Credential cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                false);

            _ = Assert.Throws<ArgumentOutOfRangeException>(
                () => cred.KeyType = invalidKeyType);
        }
        #endregion

        #region label property
        [Fact]
        public void MinLabelLength_Get_Returns1()
        {
            Assert.Equal(1, Credential.MinLabelLength);
        }

        [Fact]
        public void MaxLabelLength_Get_Returns64()
        {
            Assert.Equal(64, Credential.MaxLabelLength);
        }

        [Theory]
        [InlineData(Credential.MinLabelLength)]
        [InlineData(Credential.MaxLabelLength)]
        public void Label_SetGetLabel_ReturnsMatchingString(int labelLength)
        {
            string expectedLabel = new string('a', labelLength);

            Credential cred = new Credential(
                CryptographicKeyType.Aes128,
                "old label",
                false);

            cred.Label = expectedLabel;

            Assert.Equal(expectedLabel, cred.Label);
        }

        [Theory]
        [InlineData(Credential.MinLabelLength - 1)]
        [InlineData(Credential.MaxLabelLength + 1)]
        public void Label_SetInvalidLabelLength_ThrowsArgException(int labelLength)
        {
            string expectedLabel = new string('a', labelLength);

            Credential cred = new Credential(
                CryptographicKeyType.Aes128,
                "old label",
                false);

            _ = Assert.Throws<ArgumentException>(
                () => cred.Label = expectedLabel);
        }
        #endregion

        #region touch property
        [Fact]
        public void TouchRequired_SetGetTrue_ReturnsTrue()
        {
            bool expectedTouch = true;

            Credential cred = new Credential(
                CryptographicKeyType.Aes128,
                "test key",
                false);

            cred.TouchRequired = expectedTouch;

            Assert.Equal(expectedTouch, cred.TouchRequired);
        }
        #endregion
    }
}

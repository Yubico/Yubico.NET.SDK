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

namespace Yubico.YubiKey.Cryptography;

public class HkdfUtilitiesTests
{
    [Fact]
    public void DeriveKey_Rfc5869TestCase1_ProducesExpectedOutput()
    {
        // RFC 5869 Test Case 1 - Basic test case with SHA-256
        var ikm = Convert.FromHexString("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b");
        var salt = Convert.FromHexString("000102030405060708090a0b0c");
        var info = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9");
        var expectedOkm =
            Convert.FromHexString(
                "3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865");

        var result = HkdfUtilities.DeriveKey(ikm, salt, info, 42);
        Assert.Equal(expectedOkm, result.ToArray());
    }

    [Fact]
    public void DeriveKey_Rfc5869TestCase2_ProducesExpectedOutput()
    {
        // RFC 5869 Test Case 2 - Test with longer inputs/outputs
        var ikm = Convert.FromHexString(
            "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f");
        var salt = Convert.FromHexString(
            "606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9fa0a1a2a3a4a5a6a7a8a9aaabacadaeaf");
        var info = Convert.FromHexString(
            "b0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecfd0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeeff0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
        var expectedOkm =
            Convert.FromHexString(
                "b11e398dc80327a1c8e7f78c596a49344f012eda2d4efad8a050cc4c19afa97c59045a99cac7827271cb41c65e590e09da3275600c2f09b8367793a9aca3db71cc30c58179ec3e87c14c01d5c1f3434f1d87");

        var result = HkdfUtilities.DeriveKey(ikm, salt, info, 82);

        Assert.Equal(expectedOkm, result.ToArray());
    }

    [Fact]
    public void DeriveKey_Rfc5869TestCase3_EmptySalt_ProducesExpectedOutput()
    {
        // RFC 5869 Test Case 3 - Test with zero-length salt
        var ikm = Convert.FromHexString("0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b");
        var expectedOkm =
            Convert.FromHexString(
                "8da4e775a563c18f715f802a063c5a31b8a11f5c5ee1879ec3454e5f3c738d2d9d201395faa4b61a96c8");

        var result = HkdfUtilities.DeriveKey(ikm, length: 42);

        Assert.Equal(expectedOkm, result.ToArray());
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(48)]
    [InlineData(64)]
    public void DeriveKey_VariousLengths_ReturnsCorrectSize(
        int length)
    {
        var ikm = "test_key_material"u8.ToArray();
        var info = "test_context"u8.ToArray();

        var result = HkdfUtilities.DeriveKey(ikm, contextInfo: info, length: length);

        Assert.Equal(length, result.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8161)] // 255 * 32 + 1 = 8161 bytes, which is invalid
    public void DeriveKey_InvalidLengths_ThrowsArgumentOutOfRange(
        int length)
    {
        var ikm = "test_key_material"u8.ToArray();
        var info = "test_context"u8.ToArray();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            HkdfUtilities.DeriveKey(ikm, contextInfo: info, length: length);
        });
    }

    [Fact]
    public void DeriveKey_DifferentInfo_ProducesDifferentKeys()
    {
        var ikm = "shared_secret"u8.ToArray();
        var salt = new byte[32];

        var key1 = HkdfUtilities.DeriveKey(ikm, salt, "encryption"u8);
        var key2 = HkdfUtilities.DeriveKey(ikm, salt, "authentication"u8);

        Assert.NotEqual(key1.ToArray(), key2.ToArray());
    }
}

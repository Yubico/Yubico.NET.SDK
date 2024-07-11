﻿// Copyright 2021 Yubico AB
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
using System.Text;
using Xunit;

namespace Yubico.YubiKey.Otp
{
    public class NdefConfigTests
    {
        [Fact]
        public void CreateUriConfig_WithNullUri_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void Action() => _ = NdefConfig.CreateUriConfig(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateUriConfig_OnSuccess_SecondByteAlwaysU()
        {
            byte[] config = NdefConfig.CreateUriConfig(new Uri("https://www.test.com/"));

            Assert.Equal((byte)'U', config[1]);
        }

        [Fact]
        public void CreateUriConfig_RecognizedPrefix_ThirdByteIsPrefixCode()
        {
            byte[] config = NdefConfig.CreateUriConfig(new Uri("https://www.test.com/"));

            Assert.Equal(2, config[2]);
        }

        [Fact]
        public void CreateUriConfig_RecognizedPrefix_UriWithoutPrefixInData()
        {
            byte[] config = NdefConfig.CreateUriConfig(new Uri("https://www.test.com/"));

            Span<byte> expected = Encoding.ASCII.GetBytes("test.com/").AsSpan();
            Span<byte> actual = config.AsSpan(3, 9);

            Assert.True(expected.SequenceEqual(actual));
        }

        [Fact]
        public void CreateUriConfig_UnrecognizedPrefix_PrefixCodeIsZero()
        {
            byte[] config = NdefConfig.CreateUriConfig(new Uri("test://www.test.com/"));

            Assert.Equal(0, config[2]);
        }

        [Fact]
        public void CreateUriConfig_UnrecognizedPrefix_FullUriInData()
        {
            byte[] config = NdefConfig.CreateUriConfig(new Uri("test://www.test.com/"));

            Span<byte> expected = Encoding.ASCII.GetBytes("test://www.test.com/");
            Span<byte> actual = config.AsSpan(3, expected.Length);

            Assert.True(expected.SequenceEqual(actual));
        }

        [Fact]
        public void CreateUriConfig_EncodedUriLongerThanDataSize_ThrowsArgumentException()
        {
            static void Action() => _ = NdefConfig.CreateUriConfig(
                new Uri("https://www.1234567890.com/1234567890123456789012345678901234567890"));

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateTextConfig_WithNullValue_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void Action() => _ = NdefConfig.CreateTextConfig(null, "foo");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateTextConfig_WithNullLanguageCode_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            static void Action() => _ = NdefConfig.CreateTextConfig("foo", null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            _ = Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateTextConfig_LargeValueAndLanguage_ThrowsArgumentException()
        {
            static void Action() => _ = NdefConfig.CreateTextConfig(
                "123456789012345678901234567890",
                "123456789012345678901234567890");

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateTextConfig_OnSuccess_FirstByteIsDataLength()
        {
            string value = "test";
            int expectedLength = (value.Length * 2) + 1;

            byte[] buffer = NdefConfig.CreateTextConfig(value, value);
            int actualLength = buffer[0];

            Assert.Equal(expectedLength, actualLength);
        }

        [Fact]
        public void CreateTextConfig_OnSuccess_SecondByteAlwaysT()
        {
            byte[] config = NdefConfig.CreateTextConfig("test", "test");

            Assert.Equal((byte)'T', config[1]);
        }

        [Fact]
        public void CreateTextConfig_LanguageFieldLength_EncodedInThirdByte()
        {
            string lang = "test";
            byte[] config = NdefConfig.CreateTextConfig("foo", lang);

            Assert.Equal(lang.Length, config[2] & 0x3F);
        }

        [Fact]
        public void CreateTextConfig_LanguageFieldLongerThan63_ThrowsArgumentException()
        {
            static void Action() => _ = NdefConfig.CreateTextConfig(
                "",
                "1234567890123456789012345678901234567890123456789012345678901234");

            _ = Assert.Throws<ArgumentException>(Action);
        }

        [Fact]
        public void CreateTextConfig_EncodeAsUtf16_SetsMostSignificantBitInThirdByte()
        {
            byte[] config = NdefConfig.CreateTextConfig("foo", "foo", true);

            Assert.Equal(0x80, config[2] & 0x80);
        }

        [Fact]
        public void CreateTextConfig_EncodeAsUtf16_MessageIsUtf16BigEndian()
        {
            string text = "test";
            byte[] config = NdefConfig.CreateTextConfig(text, "", true);

            Span<byte> expected = Encoding.BigEndianUnicode.GetBytes(text).AsSpan();
            Span<byte> actual = config.AsSpan(3, expected.Length);

            Assert.True(expected.SequenceEqual(actual));
        }

        [Fact]
        public void CreateTextConfig_OnSuccess_MessageIsUtf8()
        {
            string text = "test";
            byte[] config = NdefConfig.CreateTextConfig(text, "", false);

            Span<byte> expected = Encoding.UTF8.GetBytes(text).AsSpan();
            Span<byte> actual = config.AsSpan(3, expected.Length);

            Assert.True(expected.SequenceEqual(actual));
        }
    }
}

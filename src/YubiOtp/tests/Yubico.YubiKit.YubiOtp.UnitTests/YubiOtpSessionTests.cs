// Copyright 2026 Yubico AB
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

using System.Text;

namespace Yubico.YubiKit.YubiOtp.UnitTests;

public class YubiOtpSessionTests
{
    public class NdefUriEncoding
    {
        [Fact]
        public void BuildNdefPayload_HttpsWwwUri_CompressesPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("https://www.yubico.com/products", NdefType.Uri);

            // Prefix index 2 = "https://www."
            Assert.Equal(56, payload.Length);
            Assert.Equal((byte)NdefType.Uri, payload[1]);
            Assert.Equal(2, payload[2]); // prefix index

            var remaining = Encoding.UTF8.GetBytes("yubico.com/products");
            Assert.Equal(remaining.Length + 1, payload[0]); // length = prefix byte + remaining
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_HttpUri_CompressesCorrectPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("http://example.com", NdefType.Uri);

            // Prefix index 3 = "http://"
            Assert.Equal(3, payload[2]);

            var remaining = Encoding.UTF8.GetBytes("example.com");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_HttpWwwUri_CompressesLongestPrefix()
        {
            // "http://www." (index 1) is longer than "http://" (index 3)
            var payload = YubiOtpSession.BuildNdefPayload("http://www.example.com", NdefType.Uri);

            Assert.Equal(1, payload[2]); // index 1 = "http://www."

            var remaining = Encoding.UTF8.GetBytes("example.com");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_TelUri_CompressesPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("tel:+1234567890", NdefType.Uri);

            Assert.Equal(5, payload[2]); // index 5 = "tel:"

            var remaining = Encoding.UTF8.GetBytes("+1234567890");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_NoMatchingPrefix_UsesIndex0()
        {
            var payload = YubiOtpSession.BuildNdefPayload("custom://my-protocol", NdefType.Uri);

            Assert.Equal(0, payload[2]); // no prefix compression

            var remaining = Encoding.UTF8.GetBytes("custom://my-protocol");
            Assert.Equal(remaining.Length + 1, payload[0]); // length includes prefix byte
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_MailtoUri_CompressesPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("mailto:user@example.com", NdefType.Uri);

            Assert.Equal(6, payload[2]); // index 6 = "mailto:"

            var remaining = Encoding.UTF8.GetBytes("user@example.com");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_UrnNfcPrefix_CompressesCorrectly()
        {
            // "urn:nfc:" (index 35) should be preferred over "urn:" (index 19)
            var payload = YubiOtpSession.BuildNdefPayload("urn:nfc:ext:example", NdefType.Uri);

            Assert.Equal(35, payload[2]); // index 35 = "urn:nfc:"

            var remaining = Encoding.UTF8.GetBytes("ext:example");
            Assert.Equal(remaining, payload[3..(3 + remaining.Length)]);
        }

        [Fact]
        public void BuildNdefPayload_NullUri_ReturnsAllZeros()
        {
            var payload = YubiOtpSession.BuildNdefPayload(null, NdefType.Uri);

            Assert.Equal(56, payload.Length);
            Assert.All(payload, b => Assert.Equal(0, b));
        }

        [Fact]
        public void BuildNdefPayload_EmptyUri_UsesNoPrefix()
        {
            var payload = YubiOtpSession.BuildNdefPayload("", NdefType.Uri);

            Assert.Equal(1, payload[0]); // length = 1 (just the prefix byte)
            Assert.Equal(0, payload[2]); // no prefix
        }
    }

    public class NdefTextEncoding
    {
        [Fact]
        public void BuildNdefPayload_TextRecord_EncodesWithLanguageHeader()
        {
            var payload = YubiOtpSession.BuildNdefPayload("Hello World", NdefType.Text);

            Assert.Equal(56, payload.Length);
            Assert.Equal((byte)NdefType.Text, payload[1]);

            // Language header: [0x02]["en"]
            Assert.Equal(0x02, payload[2]); // language length
            Assert.Equal((byte)'e', payload[3]);
            Assert.Equal((byte)'n', payload[4]);

            var textBytes = Encoding.UTF8.GetBytes("Hello World");
            Assert.Equal(textBytes, payload[5..(5 + textBytes.Length)]);

            // Length = 1 (lang length byte) + 2 (lang code) + text length
            Assert.Equal(1 + 2 + textBytes.Length, payload[0]);
        }

        [Fact]
        public void BuildNdefPayload_NullText_ReturnsAllZeros()
        {
            var payload = YubiOtpSession.BuildNdefPayload(null, NdefType.Text);

            Assert.All(payload, b => Assert.Equal(0, b));
        }
    }

    public class HmacChallengePadding
    {
        [Fact]
        public void PadHmacChallenge_ShortChallenge_PadsTo64Bytes()
        {
            byte[] challenge = [0x01, 0x02, 0x03];

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(64, padded.Length);
            Assert.Equal(0x01, padded[0]);
            Assert.Equal(0x02, padded[1]);
            Assert.Equal(0x03, padded[2]);
        }

        [Fact]
        public void PadHmacChallenge_PadByteDiffersFromLastByte_NonZeroLastByte()
        {
            byte[] challenge = [0x01, 0x02, 0x05]; // last byte = 0x05 (non-zero)

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            // Pad byte should be 0x00 (differs from 0x05)
            Assert.Equal(0x00, padded[3]);
            Assert.All(padded[3..], b => Assert.Equal(0x00, b));
        }

        [Fact]
        public void PadHmacChallenge_PadByteDiffersFromLastByte_ZeroLastByte()
        {
            byte[] challenge = [0x01, 0x02, 0x00]; // last byte = 0x00

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            // Pad byte should be 0x01 (differs from 0x00)
            Assert.Equal(0x01, padded[3]);
            Assert.All(padded[3..], b => Assert.Equal(0x01, b));
        }

        [Fact]
        public void PadHmacChallenge_EmptyChallenge_PadsWithNonZero()
        {
            byte[] challenge = [];

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            // Empty challenge: lastByte defaults to 0, so pad with 0x01
            Assert.Equal(64, padded.Length);
            Assert.All(padded, b => Assert.Equal(0x01, b));
        }

        [Fact]
        public void PadHmacChallenge_FullChallenge_NoPadding()
        {
            var challenge = new byte[64];
            challenge[0] = 0xAA;
            challenge[63] = 0xBB;

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(64, padded.Length);
            Assert.Equal(0xAA, padded[0]);
            Assert.Equal(0xBB, padded[63]);
        }

        [Fact]
        public void PadHmacChallenge_SingleByte_PadsRemaining()
        {
            byte[] challenge = [0xFF];

            var padded = YubiOtpSession.PadHmacChallenge(challenge);

            Assert.Equal(0xFF, padded[0]);
            // Last byte is 0xFF (non-zero), pad with 0x00
            Assert.All(padded[1..], b => Assert.Equal(0x00, b));
        }
    }

    public class SlotMapping
    {
        [Theory]
        [InlineData(Slot.One, SlotOperation.Configure, ConfigSlot.Config1)]
        [InlineData(Slot.Two, SlotOperation.Configure, ConfigSlot.Config2)]
        [InlineData(Slot.One, SlotOperation.Update, ConfigSlot.Update1)]
        [InlineData(Slot.Two, SlotOperation.Update, ConfigSlot.Update2)]
        [InlineData(Slot.One, SlotOperation.Ndef, ConfigSlot.Ndef1)]
        [InlineData(Slot.Two, SlotOperation.Ndef, ConfigSlot.Ndef2)]
        [InlineData(Slot.One, SlotOperation.ChallengeHmac, ConfigSlot.ChalHmac1)]
        [InlineData(Slot.Two, SlotOperation.ChallengeHmac, ConfigSlot.ChalHmac2)]
        public void Map_ValidSlotAndOperation_ReturnsCorrectConfigSlot(
            Slot slot, SlotOperation operation, ConfigSlot expected)
        {
            var result = slot.Map(operation);
            Assert.Equal(expected, result);
        }
    }
}

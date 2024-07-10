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
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Fido2
{
    public class LargeBlobEntryTests
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            byte[] blobData =
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50
            };
            byte[] keyData =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };

            var entry = new LargeBlobEntry(blobData, keyData);
            Assert.Equal(expected: 80, entry.OriginalDataLength);
        }

        [Fact]
        public void Encode_Succeeds()
        {
            byte[] blobData =
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50
            };
            byte[] keyData =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            byte[] nonceBytes =
            {
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32
            };
            byte[] expectedEncoding =
            {
                0xa3, 0x01, 0x58, 0x23, 0x50, 0x96, 0x1b, 0x97, 0x8f, 0x59, 0xac, 0x39, 0xb9, 0x33, 0x14, 0x79,
                0x03, 0x7c, 0x60, 0xfa, 0x16, 0x42, 0xc4, 0x95, 0x32, 0xbc, 0x41, 0x9e, 0x0c, 0xc0, 0xd3, 0x0f,
                0x31, 0x06, 0x09, 0x22, 0xc9, 0x0a, 0xf7, 0x02, 0x4c, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36,
                0x37, 0x38, 0x39, 0x31, 0x32, 0x03, 0x18, 0x50
            };

            // The code will generate a random nonce. So to guarantee the nonce
            // we want, use the fixed byte RNG.
            var nonceGenerator =
                RandomObjectUtility.SetRandomProviderFixedBytes(nonceBytes);
            var isValid = false;

            try
            {
                var entry = new LargeBlobEntry(blobData, keyData);
                var encoding = entry.CborEncode();

                isValid = expectedEncoding.AsSpan().SequenceEqual(encoding.AsSpan());
            }
            finally
            {
                nonceGenerator.RestoreRandomProvider();
            }

            Assert.True(isValid);
        }

        [Fact]
        public void Decode_CorrectNonce()
        {
            byte[] encoding =
            {
                0xa3, 0x01, 0x58, 0x23, 0x50, 0x96, 0x1b, 0x97, 0x8f, 0x59, 0xac, 0x39, 0xb9, 0x33, 0x14, 0x79,
                0x03, 0x7c, 0x60, 0xfa, 0x16, 0x42, 0xc4, 0x95, 0x32, 0xbc, 0x41, 0x9e, 0x0c, 0xc0, 0xd3, 0x0f,
                0x31, 0x06, 0x09, 0x22, 0xc9, 0x0a, 0xf7, 0x02, 0x4c, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36,
                0x37, 0x38, 0x39, 0x31, 0x32, 0x03, 0x18, 0x50
            };
            byte[] nonceBytes =
            {
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32
            };

            var entry = new LargeBlobEntry(new ReadOnlyMemory<byte>(encoding));

            var isValid = nonceBytes.AsSpan().SequenceEqual(entry.Nonce.Span);
            Assert.True(isValid);
        }

        [Fact]
        public void DecodeDecrypt_CorrectPlaintext()
        {
            byte[] encoding =
            {
                0xa3, 0x01, 0x58, 0x23, 0x50, 0x96, 0x1b, 0x97, 0x8f, 0x59, 0xac, 0x39, 0xb9, 0x33, 0x14, 0x79,
                0x03, 0x7c, 0x60, 0xfa, 0x16, 0x42, 0xc4, 0x95, 0x32, 0xbc, 0x41, 0x9e, 0x0c, 0xc0, 0xd3, 0x0f,
                0x31, 0x06, 0x09, 0x22, 0xc9, 0x0a, 0xf7, 0x02, 0x4c, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36,
                0x37, 0x38, 0x39, 0x31, 0x32, 0x03, 0x18, 0x50
            };
            byte[] keyData =
            {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            byte[] blobData =
            {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50
            };

            var entry = new LargeBlobEntry(new ReadOnlyMemory<byte>(encoding));
            var isValid = entry.TryDecrypt(new ReadOnlyMemory<byte>(keyData), out var plaintext);
            Assert.True(isValid);

            isValid = blobData.AsSpan().SequenceEqual(plaintext.Span);
            Assert.True(isValid);
        }
    }
}

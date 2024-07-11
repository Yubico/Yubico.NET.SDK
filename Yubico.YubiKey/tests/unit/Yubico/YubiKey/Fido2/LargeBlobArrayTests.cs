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
    public class LargeBlobArrayTests
    {
        [Fact]
        public void InitialArray_CorrectDigest()
        {
            byte[] initialArray = new byte[] {
                0x80, 0x76, 0xbe, 0x8b, 0x52, 0x8d, 0x00, 0x75, 0xf7, 0xaa, 0xe9, 0x8d, 0x6f, 0xa5, 0x7a, 0x6d, 0x3c
            };

            var arrayMem = new ReadOnlyMemory<byte>(initialArray);
            var array = new SerializedLargeBlobArray(arrayMem);
            Assert.True(array.Digest.HasValue);

            if (array.Digest.HasValue)
            {
                bool isValid = MemoryExtensions.SequenceEqual(arrayMem.Slice(1).Span, array.Digest.Value.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        public void Array_AddEntry_DigestCleared()
        {
            SerializedLargeBlobArray array = GetInitialArray();
            AddFixedEntry(array);
            Assert.Null(array.Digest);
        }

        [Fact]
        void EncodeArray_CorrectEncoding()
        {
            byte[] expectedEncoding = new byte[] {
                0x81, 0xa3, 0x01, 0x58, 0x23, 0x50, 0x96, 0x1b, 0x97, 0x8f, 0x59, 0xac, 0x39, 0xb9, 0x33, 0x14,
                0x79, 0x03, 0x7c, 0x60, 0xfa, 0x16, 0x42, 0xc4, 0x95, 0x32, 0xbc, 0x41, 0x9e, 0x0c, 0xc0, 0xd3,
                0x0f, 0x31, 0x06, 0x09, 0x22, 0xc9, 0x0a, 0xf7, 0x02, 0x4c, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35,
                0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x03, 0x18, 0x50, 0x3e, 0xef, 0xb1, 0xd1, 0x0b, 0xcb, 0x69,
                0x3a, 0x1e, 0x5f, 0xb8, 0xca, 0x12, 0xab, 0xa1, 0xbb
            };

            SerializedLargeBlobArray array = GetInitialArray();
            AddFixedEntry(array);
            byte[] encoding = array.Encode();

            bool isValid = MemoryExtensions.SequenceEqual(expectedEncoding.AsSpan(), encoding.AsSpan());
            Assert.True(isValid);
        }

        [Fact]
        void EncodeArray_CorrectBlobArray()
        {
            byte[] expectedEncoding = new byte[] {
                0x81, 0xa3, 0x01, 0x58, 0x23, 0x50, 0x96, 0x1b, 0x97, 0x8f, 0x59, 0xac, 0x39, 0xb9, 0x33, 0x14,
                0x79, 0x03, 0x7c, 0x60, 0xfa, 0x16, 0x42, 0xc4, 0x95, 0x32, 0xbc, 0x41, 0x9e, 0x0c, 0xc0, 0xd3,
                0x0f, 0x31, 0x06, 0x09, 0x22, 0xc9, 0x0a, 0xf7, 0x02, 0x4c, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35,
                0x36, 0x37, 0x38, 0x39, 0x31, 0x32, 0x03, 0x18, 0x50,
            };

            SerializedLargeBlobArray array = GetInitialArray();
            AddFixedEntry(array);
            _ = array.Encode();
            _ = Assert.NotNull(array.EncodedArray);

            if (array.EncodedArray.HasValue)
            {
                bool isValid = MemoryExtensions.SequenceEqual(new Span<byte>(expectedEncoding), array.EncodedArray.Value.Span);
                Assert.True(isValid);
            }
        }

        [Fact]
        void EncodeArray_CorrectDigest()
        {
            byte[] expectedDigest = new byte[] {
                0x3e, 0xef, 0xb1, 0xd1, 0x0b, 0xcb, 0x69, 0x3a, 0x1e, 0x5f, 0xb8, 0xca, 0x12, 0xab, 0xa1, 0xbb
            };

            SerializedLargeBlobArray array = GetInitialArray();
            AddFixedEntry(array);
            _ = array.Encode();
            _ = Assert.NotNull(array.Digest);

            if (array.Digest.HasValue)
            {
                bool isValid = MemoryExtensions.SequenceEqual(new Span<byte>(expectedDigest), array.Digest.Value.Span);
                Assert.True(isValid);
            }
        }

        private static SerializedLargeBlobArray GetInitialArray()
        {
            byte[] initialArray = new byte[] {
                0x80, 0x76, 0xbe, 0x8b, 0x52, 0x8d, 0x00, 0x75, 0xf7, 0xaa, 0xe9, 0x8d, 0x6f, 0xa5, 0x7a, 0x6d, 0x3c
            };

            return new SerializedLargeBlobArray(new ReadOnlyMemory<byte>(initialArray));
        }

        private static void AddFixedEntry(SerializedLargeBlobArray array)
        {
            byte[] blobData = new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50
            };
            byte[] keyData = new byte[] {
                0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
                0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
            };
            byte[] nonceBytes = new byte[] {
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x31, 0x32
            };

            // The code will generate a random nonce. So to guarantee the nonce
            // we want, use the fixed byte RNG.
            RandomObjectUtility nonceGenerator =
                RandomObjectUtility.SetRandomProviderFixedBytes(nonceBytes);

            try
            {
                array.AddEntry(blobData, keyData);
            }
            finally
            {
                nonceGenerator.RestoreRandomProvider();
            }
        }
    }
}

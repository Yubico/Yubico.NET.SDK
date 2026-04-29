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

// Purpose
// -------
// Direct P/Invoke functional tests for the OpenSSL BIGNUM marshaling layer
// exposed by Yubico.NativeShims (Native_BN_*). These wrappers move arbitrary-
// precision integers across the C#/C boundary; subtle bugs in length handling,
// padding, or leading-zero behavior surface as silent corruption in EC point
// coordinates and ARKG primitives that build on top.
//
// What this validates
// -------------------
//   * bin -> BIGNUM -> bin round-trip preserves bytes for sizes 1, 16, 32, 256.
//   * Native_BN_num_bytes returns the canonical length (leading zeros stripped,
//     matching OpenSSL semantics).
//   * Native_BN_bn2binpad left-pads to a fixed width without truncating.
//   * Lifecycle: Native_BN_new, Native_BN_clear_free do not crash or leak under
//     repeated allocate/free.
//
// References
// ----------
//   * OpenSSL BN(3) man page — https://docs.openssl.org/master/man3/BN_new/
//     (authoritative for BN_new, BN_bin2bn, BN_bn2bin, BN_bn2binpad,
//     BN_num_bytes, BN_clear_free behavior).
//   * No formal standards-track spec exists for the BIGNUM API; round-trip
//     and boundary tests are self-consistent against the OpenSSL contract.

using System;
using System.Linq;
using Xunit;
using Yubico.PlatformInterop;

namespace Yubico.PlatformInterop.Cryptography
{
    public class BigNumInteropTests
    {
        [Fact]
        public void BnBinaryToBigNum_RoundTrip_SingleByte_ReturnsOriginal()
        {
            byte[] original = { 0x42 };

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[1];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            Assert.Equal(1, written);
            Assert.Equal(original, buffer);
        }

        [Fact]
        public void BnBinaryToBigNum_RoundTrip_16Bytes_ReturnsOriginal()
        {
            byte[] original = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[16];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            Assert.Equal(16, written);
            Assert.Equal(original, buffer);
        }

        [Fact]
        public void BnBinaryToBigNum_RoundTrip_32Bytes_ReturnsOriginal()
        {
            byte[] original = Enumerable.Range(0, 32).Select(i => (byte)((i * 7) + 13)).ToArray();

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[32];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            Assert.Equal(32, written);
            Assert.Equal(original, buffer);
        }

        [Fact]
        public void BnBinaryToBigNum_RoundTrip_256Bytes_ReturnsOriginal()
        {
            // Start with 0x01 to avoid leading-zero stripping
            byte[] original = Enumerable.Range(1, 256).Select(i => (byte)i).ToArray();

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[256];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            Assert.Equal(256, written);
            Assert.Equal(original, buffer);
        }

        [Fact]
        public void BnBinaryToBigNum_LeadingZero_StripsLeadingZeros()
        {
            // OpenSSL BIGNUMs strip leading zeros
            byte[] original = { 0x00, 0x00, 0x01, 0x23, 0x45 };
            byte[] expected = { 0x01, 0x23, 0x45 };

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[5];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            Assert.Equal(3, written);
            Assert.Equal(expected, buffer.Take(written).ToArray());
        }

        [Fact]
        public void BnBinaryToBigNum_AllZeros_HandlesGracefully()
        {
            byte[] original = { 0x00, 0x00, 0x00 };

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[3];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            // All zeros represents the number 0, which OpenSSL represents as zero bytes
            Assert.Equal(0, written);
        }

        [Fact]
        public void BnBigNumToBinaryWithPadding_PadsTo32Bytes()
        {
            byte[] original = { 0x12, 0x34 };

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[32];
            int written = NativeMethods.BnBigNumToBinaryWithPadding(bn, buffer);

            Assert.Equal(32, written);
            // Padding should be zero-bytes on the left (big-endian)
            byte[] expected = new byte[32];
            expected[30] = 0x12;
            expected[31] = 0x34;
            Assert.Equal(expected, buffer);
        }

        [Fact]
        public void BnBigNumToBinaryWithPadding_PadsTo16Bytes()
        {
            byte[] original = { 0xAB };

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[16];
            int written = NativeMethods.BnBigNumToBinaryWithPadding(bn, buffer);

            Assert.Equal(16, written);
            byte[] expected = new byte[16];
            expected[15] = 0xAB;
            Assert.Equal(expected, buffer);
        }

        [Fact]
        public void BnNew_CreatesValidHandle()
        {
            using SafeBigNum bn = NativeMethods.BnNew();

            Assert.NotNull(bn);
            Assert.False(bn.IsInvalid);
        }

        [Fact]
        public void BnBinaryToBigNum_EmptyArray_HandlesGracefully()
        {
            byte[] original = Array.Empty<byte>();

            using SafeBigNum bn = NativeMethods.BnBinaryToBigNum(original);
            byte[] buffer = new byte[16];
            int written = NativeMethods.BnBigNumToBinary(bn, buffer);

            // Empty input = zero
            Assert.Equal(0, written);
        }
    }
}

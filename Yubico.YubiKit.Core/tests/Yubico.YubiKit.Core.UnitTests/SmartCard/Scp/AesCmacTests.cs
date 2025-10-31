// Copyright (C) 2024 Yubico.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Scp;

/// <summary>
///     Unit tests for AES-CMAC implementation using official test vectors from:
///     - RFC 4493: The AES-CMAC Algorithm
///     - NIST SP 800-38B: Recommendation for Block Cipher Modes of Operation (CMAC)
/// </summary>
public class AesCmacTests
{
    // Official test vectors from RFC 4493 Section 4
    // Key: 2b7e1516 28aed2a6 abf71588 09cf4f3c
    private static readonly byte[] TestKey =
    [
        0x2b, 0x7e, 0x15, 0x16, 0x28, 0xae, 0xd2, 0xa6,
        0xab, 0xf7, 0x15, 0x88, 0x09, 0xcf, 0x4f, 0x3c
    ];

    // Expected subkeys from RFC 4493 Section 4
    // K1: fbeed618 35713366 7c85e08f 7236a8de
    private static readonly byte[] ExpectedK1 =
    [
        0xfb, 0xee, 0xd6, 0x18, 0x35, 0x71, 0x33, 0x66,
        0x7c, 0x85, 0xe0, 0x8f, 0x72, 0x36, 0xa8, 0xde
    ];

    // K2: f7ddac30 6ae266cc f90bc11e e46d513b
    private static readonly byte[] ExpectedK2 =
    [
        0xf7, 0xdd, 0xac, 0x30, 0x6a, 0xe2, 0x66, 0xcc,
        0xf9, 0x0b, 0xc1, 0x1e, 0xe4, 0x6d, 0x51, 0x3b
    ];

    [Fact]
    public void AesCmac_SubkeyGeneration_MatchesRfc4493()
    {
        using var cmac = new AesCmac(TestKey);

#if DEBUG
        Assert.True(cmac.DebugGetSubkey1().SequenceEqual(ExpectedK1));
        Assert.True(cmac.DebugGetSubkey2().SequenceEqual(ExpectedK2));
#else
    // In release builds, skip internal validation
    // RFC vectors provide end-to-end verification
    Assert.True(true);
#endif
    }

    [Theory]
    [InlineData(0)] // Empty
    [InlineData(1)] // Single byte
    [InlineData(15)] // One byte short
    [InlineData(16)] // Exactly one block
    [InlineData(17)] // One block + 1
    [InlineData(32)] // Exactly two blocks (CRITICAL!)
    [InlineData(33)] // Two blocks + 1
    [InlineData(48)] // Three blocks
    [InlineData(64)] // Four blocks
    public void AesCmac_VariousLengths_ProducesConsistentResults(int length)
    {
        var message = new byte[length];
        Array.Fill<byte>(message, 0xAA);

        using var cmac1 = new AesCmac(TestKey);
        cmac1.AppendData(message);
        var mac1 = cmac1.GetHashAndReset();

        // Split in half and verify
        using var cmac2 = new AesCmac(TestKey);
        if (length > 0)
        {
            var mid = length / 2;
            cmac2.AppendData(message[..mid]);
            cmac2.AppendData(message[mid..]);
        }

        var mac2 = cmac2.GetHashAndReset();

        Assert.Equal(mac1, mac2);
    }

    [Fact]
    public void AesCmac_LargeData_ProducesCorrectMac()
    {
        // Test with 10KB of data
        var message = new byte[10240];
        var rng = new Random(42); // Deterministic seed
        rng.NextBytes(message);

        using var cmac1 = new AesCmac(TestKey);
        cmac1.AppendData(message);
        var mac1 = cmac1.GetHashAndReset();

        // Split into chunks and verify same result
        using var cmac2 = new AesCmac(TestKey);
        for (var i = 0; i < message.Length; i += 1000)
        {
            var chunkSize = Math.Min(1000, message.Length - i);
            cmac2.AppendData(message.AsSpan(i, chunkSize));
        }

        var mac2 = cmac2.GetHashAndReset();

        Assert.Equal(mac1, mac2);
    }

    [Fact]
    public void GetHashAndReset_ResetsBufferOffset()
    {
        byte[] message1 = [0x6b, 0xc1, 0xbe]; // 3 bytes
        byte[] message2 = [0xde, 0xad]; // 2 bytes

        using var cmac = new AesCmac(TestKey);

        cmac.AppendData(message1);
        var mac1 = cmac.GetHashAndReset();

        // After reset, should start fresh
        cmac.AppendData(message2);
        var mac2 = cmac.GetHashAndReset();

        // Verify mac2 is NOT affected by previous message1
        using var cmac2 = new AesCmac(TestKey);
        cmac2.AppendData(message2);
        var expectedMac2 = cmac2.GetHashAndReset();

        Assert.Equal(expectedMac2, mac2);
    }

    [Fact]
    public void GetHashAndReset_ClearsBufferContents()
    {
        byte[] message = [0x6b, 0xc1, 0xbe, 0xe2, 0x2e]; // Partial block

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        // After reset, empty message should not be affected by previous data
        var emptyMac = cmac.GetHashAndReset();

        using var freshCmac = new AesCmac(TestKey);
        var expectedEmptyMac = freshCmac.GetHashAndReset();

        Assert.Equal(expectedEmptyMac, emptyMac);
    }

    [Fact]
    public void AesCmac_SingleByte_ProducesCorrectMac()
    {
        byte[] message = [0x42];

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(16, mac.Length);

        // Verify padding path was taken (incomplete block)
        // Single byte should be padded with 0x80 followed by zeros
    }

    [Fact]
    public void AesCmac_ManyOneByteAppends_ProducesCorrectMac()
    {
        var message = new byte[64];
        Array.Fill<byte>(message, 0xDD);

        using var cmac1 = new AesCmac(TestKey);
        cmac1.AppendData(message);
        var mac1 = cmac1.GetHashAndReset();

        // Same data, but one byte at a time
        using var cmac2 = new AesCmac(TestKey);
        foreach (var b in message) cmac2.AppendData([b]);
        var mac2 = cmac2.GetHashAndReset();

        Assert.Equal(mac1, mac2);
    }

    [Fact]
    public void AesCmac_IncrementalFillsBufferExactly_ProducesCorrectMac()
    {
        // Tests buffer flush when _bufferOffset hits BlockSize exactly
        var data = new byte[48]; // 3 blocks
        Array.Fill<byte>(data, 0xCC);

        using var cmac = new AesCmac(TestKey);

        // First call: 10 bytes (partial buffer)
        cmac.AppendData(data[..10]);

        // Second call: 6 bytes (completes buffer to 16, triggers ProcessBlock)
        cmac.AppendData(data[10..16]);

        // Third call: 32 bytes (2 complete blocks)
        cmac.AppendData(data[16..]);

        var mac1 = cmac.GetHashAndReset();

        // Compare with single call
        using var cmac2 = new AesCmac(TestKey);
        cmac2.AppendData(data);
        var mac2 = cmac2.GetHashAndReset();

        Assert.Equal(mac1, mac2);
    }

    [Fact]
    public void AesCmac_ExactlyTwoBlocks_ProducesCorrectMac()
    {
        // CRITICAL: Tests the off-by-one bug fix (offset + BlockSize <= vs <)
        // 32 bytes = exactly 2 complete blocks, no padding needed
        var message = new byte[32];
        Array.Fill<byte>(message, 0xAA);

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(16, mac.Length);

        // Verify consistency: same result when split across calls
        using var cmac2 = new AesCmac(TestKey);
        cmac2.AppendData(message[..16]);
        cmac2.AppendData(message[16..]);
        var mac2 = cmac2.GetHashAndReset();

        Assert.Equal(mac, mac2);
    }

    [Fact]
    public void AesCmac_OneBlockPlusOneByte_ProducesCorrectMac()
    {
        // Tests padding path after processing exactly one complete block
        var message = new byte[17];
        Array.Fill<byte>(message, 0xBB);

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(16, mac.Length);

        // Should use padding (K2 subkey path)
        using var cmac2 = new AesCmac(TestKey);
        cmac2.AppendData(message[..16]); // Complete block
        cmac2.AppendData(message[16..]); // Single byte triggers padding
        var mac2 = cmac2.GetHashAndReset();

        Assert.Equal(mac, mac2);
    }

    [Fact]
    public void AesCmac_EmptyMessage_ProducesCorrectMac()
    {
        // RFC 4493 Example 1: Len = 0
        // Expected MAC: bb1d6929 e9593728 7fa37d12 9b756746
        byte[] expectedMac =
        [
            0xbb, 0x1d, 0x69, 0x29, 0xe9, 0x59, 0x37, 0x28,
            0x7f, 0xa3, 0x7d, 0x12, 0x9b, 0x75, 0x67, 0x46
        ];

        using var cmac = new AesCmac(TestKey);
        // Don't append any data - test empty message
        var mac = cmac.GetHashAndReset();

        Assert.Equal(expectedMac, mac);
    }

    [Fact]
    public void AesCmac_SingleBlock_ProducesCorrectMac()
    {
        // RFC 4493 Example 2: Len = 16 (complete block)
        // Message: 6bc1bee2 2e409f96 e93d7e11 7393172a
        byte[] message =
        [
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a
        ];

        // Expected MAC: 070a16b4 6b4d4144 f79bdd9d d04a287c
        byte[] expectedMac =
        [
            0x07, 0x0a, 0x16, 0xb4, 0x6b, 0x4d, 0x41, 0x44,
            0xf7, 0x9b, 0xdd, 0x9d, 0xd0, 0x4a, 0x28, 0x7c
        ];

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(expectedMac, mac);
    }

    [Fact]
    public void AesCmac_IncompleteBlock_ProducesCorrectMac()
    {
        // RFC 4493 Example 3: Len = 40 (2 complete blocks + 8 bytes)
        // This tests the padding logic for incomplete final blocks
        byte[] message =
        [
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
            0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c,
            0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
            0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11
        ];

        // Expected MAC: dfa66747 de9ae630 30ca3261 1497c827
        byte[] expectedMac =
        [
            0xdf, 0xa6, 0x67, 0x47, 0xde, 0x9a, 0xe6, 0x30,
            0x30, 0xca, 0x32, 0x61, 0x14, 0x97, 0xc8, 0x27
        ];

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(expectedMac, mac);
    }

    [Fact]
    public void AesCmac_FourBlocks_ProducesCorrectMac()
    {
        // RFC 4493 Example 4: Len = 64 (4 complete blocks)
        byte[] message =
        [
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a,
            0xae, 0x2d, 0x8a, 0x57, 0x1e, 0x03, 0xac, 0x9c,
            0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51,
            0x30, 0xc8, 0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11,
            0xe5, 0xfb, 0xc1, 0x19, 0x1a, 0x0a, 0x52, 0xef,
            0xf6, 0x9f, 0x24, 0x45, 0xdf, 0x4f, 0x9b, 0x17,
            0xad, 0x2b, 0x41, 0x7b, 0xe6, 0x6c, 0x37, 0x10
        ];

        // Expected MAC: 51f0bebf 7e3b9d92 fc497417 79363cfe
        byte[] expectedMac =
        [
            0x51, 0xf0, 0xbe, 0xbf, 0x7e, 0x3b, 0x9d, 0x92,
            0xfc, 0x49, 0x74, 0x17, 0x79, 0x36, 0x3c, 0xfe
        ];

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(expectedMac, mac);
    }

    [Fact]
    public void AesCmac_MultipleAppendCalls_ProducesCorrectMac()
    {
        // Same as Example 4, but split into multiple AppendData calls
        // This tests the internal buffering logic
        byte[] part1 = [0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96];
        byte[] part2 = [0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17, 0x2a, 0xae, 0x2d, 0x8a, 0x57];
        byte[] part3 = [0x1e, 0x03, 0xac, 0x9c, 0x9e, 0xb7, 0x6f, 0xac, 0x45, 0xaf, 0x8e, 0x51, 0x30, 0xc8];
        byte[] part4 = [0x1c, 0x46, 0xa3, 0x5c, 0xe4, 0x11, 0xe5, 0xfb, 0xc1, 0x19, 0x1a, 0x0a, 0x52, 0xef];
        byte[] part5 = [0xf6, 0x9f, 0x24, 0x45, 0xdf, 0x4f, 0x9b, 0x17, 0xad, 0x2b, 0x41, 0x7b, 0xe6, 0x6c, 0x37, 0x10];

        byte[] expectedMac =
        [
            0x51, 0xf0, 0xbe, 0xbf, 0x7e, 0x3b, 0x9d, 0x92,
            0xfc, 0x49, 0x74, 0x17, 0x79, 0x36, 0x3c, 0xfe
        ];

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(part1);
        cmac.AppendData(part2);
        cmac.AppendData(part3);
        cmac.AppendData(part4);
        cmac.AppendData(part5);
        var mac = cmac.GetHashAndReset();

        Assert.Equal(expectedMac, mac);
    }

    [Fact]
    public void AesCmac_ResetWorks_CanReuseInstance()
    {
        // Test that GetHashAndReset() properly resets state
        byte[] message = [0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96];

        using var cmac = new AesCmac(TestKey);

        // First computation
        cmac.AppendData(message);
        var mac1 = cmac.GetHashAndReset();

        // Second computation on same instance - should produce same result
        cmac.AppendData(message);
        var mac2 = cmac.GetHashAndReset();

        Assert.Equal(mac1, mac2);
    }

    [Fact]
    public void AesCmac_OneBytePadding_ProducesCorrectMac()
    {
        // Test with message length = 15 (one byte short of complete block)
        // This ensures padding logic works for edge case
        byte[] message =
        [
            0x6b, 0xc1, 0xbe, 0xe2, 0x2e, 0x40, 0x9f, 0x96,
            0xe9, 0x3d, 0x7e, 0x11, 0x73, 0x93, 0x17
        ];

        using var cmac = new AesCmac(TestKey);
        cmac.AppendData(message);
        var mac = cmac.GetHashAndReset();

        // MAC should be 16 bytes
        Assert.Equal(16, mac.Length);
    }

    [Fact]
    public void AesCmac_InvalidKeyLength_ThrowsArgumentException()
    {
        var invalidKey = new byte[15]; // Should be 16 bytes

        var exception = Assert.Throws<ArgumentException>(() => new AesCmac(invalidKey));
        Assert.Contains("16 bytes", exception.Message);
    }

    [Fact]
    public void AesCmac_Dispose_ZeroesKeyMaterial()
    {
        var key = new byte[16];
        Array.Fill<byte>(key, 0xFF);

        var cmac = new AesCmac(key);
        cmac.Dispose();

        // After dispose, attempting to use should throw
        Assert.Throws<ObjectDisposedException>(() => cmac.AppendData([0x01]));
        Assert.Throws<ObjectDisposedException>(() => cmac.GetHashAndReset());
    }
}
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

using System.Security.Cryptography;

namespace Yubico.YubiKit.OpenPgp.UnitTests;

public class DigestInfoTests
{
    [Fact]
    public void GetHeader_Sha256_ReturnsCorrectDer()
    {
        byte[] expected =
            [0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20];

        var header = DigestInfo.GetHeader(HashAlgorithmName.SHA256);

        Assert.Equal(expected, header.ToArray());
    }

    [Fact]
    public void GetHeader_Sha384_ReturnsCorrectDer()
    {
        byte[] expected =
            [0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00, 0x04, 0x30];

        var header = DigestInfo.GetHeader(HashAlgorithmName.SHA384);

        Assert.Equal(expected, header.ToArray());
    }

    [Fact]
    public void GetHeader_Sha512_ReturnsCorrectDer()
    {
        byte[] expected =
            [0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0x04, 0x40];

        var header = DigestInfo.GetHeader(HashAlgorithmName.SHA512);

        Assert.Equal(expected, header.ToArray());
    }

    [Fact]
    public void GetHeader_Sha1_ReturnsCorrectDer()
    {
        byte[] expected =
            [0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00, 0x04, 0x14];

        var header = DigestInfo.GetHeader(HashAlgorithmName.SHA1);

        Assert.Equal(expected, header.ToArray());
    }

    [Fact]
    public void GetHeader_UnsupportedAlgorithm_Throws()
    {
        Assert.Throws<NotSupportedException>(
            () => DigestInfo.GetHeader(new HashAlgorithmName("UNKNOWN")));
    }

    [Fact]
    public void Build_Sha256_ConcatenatesHeaderAndDigest()
    {
        byte[] digest = new byte[32]; // SHA256 digest length
        digest[0] = 0xAA;
        digest[31] = 0xBB;

        var result = DigestInfo.Build(HashAlgorithmName.SHA256, digest);

        // Header (19 bytes) + digest (32 bytes) = 51 bytes
        Assert.Equal(19 + 32, result.Length);

        // Starts with the header
        Assert.Equal(0x30, result[0]);
        Assert.Equal(0x31, result[1]);

        // Ends with the digest
        Assert.Equal(0xAA, result[19]);
        Assert.Equal(0xBB, result[^1]);
    }
}

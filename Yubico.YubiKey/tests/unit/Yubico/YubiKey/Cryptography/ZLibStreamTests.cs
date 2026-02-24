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
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Yubico.YubiKey.Cryptography
{
    public class ZLibStreamTests
    {
        // "Hello, World!" compressed with zlib (RFC 1950).
        private static readonly byte[] ZLibCompressedHelloWorld =
        {
            0x78, 0x9C, 0xF3, 0x48, 0xCD, 0xC9, 0xC9, 0xD7,
            0x51, 0x08, 0xCF, 0x2F, 0xCA, 0x49, 0x51, 0x04,
            0x00, 0x20, 0x5E, 0x04, 0x8A
        };
        private const string HelloWorldText = "Hello, World!";

        [Fact]
        public void Decompress_ValidZLibData_ReturnsOriginalData()
        {
            // Pure zlib (RFC 1950) data without any prefix.
            string hex = "789c8b2c4dcaf4ce2c5148cb2f5270cc4b29cacf4c5128492d2e5148492c4904009f2e0aa4";

            byte[] data = Convert.FromHexString(hex);

            using var compressedStream = new MemoryStream(data);
            using var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();

            zlibStream.CopyTo(resultStream);
            string result = Encoding.UTF8.GetString(resultStream.ToArray());

            Assert.Equal("YubiKit for Android test data", result);
        }

        [Fact]
        public void Decompress_GidsFormat_StripsHeaderAndDecompresses()
        {
            // GIDS format: 4-byte header (01 00 = magic, 1D 00 = LE uncompressed length 29)
            // followed by standard zlib (RFC 1950) data.
            string hex = "01001d00789c8b2c4dcaf4ce2c5148cb2f5270cc4b29cacf4c5128492d2e5148492c4904009f2e0aa4";

            byte[] data = Convert.FromHexString(hex);

            // Strip 4-byte GIDS header, then decompress the zlib payload
            const int gidsHeaderLength = 4;
            using var compressedStream = new MemoryStream(data, gidsHeaderLength, data.Length - gidsHeaderLength);
            using var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();

            zlibStream.CopyTo(resultStream);
            string result = Encoding.UTF8.GetString(resultStream.ToArray());

            Assert.Equal("YubiKit for Android test data", result);
        }

        [Fact]
        public void Decompress_ReadByteArray_ReturnsOriginalData()
        {
            using var compressedStream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress);

            byte[] buffer = new byte[256];
            int totalRead = 0;
            int bytesRead;

            while ((bytesRead = zlibStream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
            {
                totalRead += bytesRead;
            }

            string result = Encoding.UTF8.GetString(buffer, 0, totalRead);
            Assert.Equal(HelloWorldText, result);
        }

        [Fact]
        public void Decompress_ReadByte_ReturnsCorrectFirstByte()
        {
            using var compressedStream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress);

            int firstByte = zlibStream.ReadByte();

            Assert.Equal((int)'H', firstByte);
        }

        [Fact]
        public void Compress_ThenDecompress_RoundTrips()
        {
            byte[] original = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog.");

            // Compress
            byte[] compressed;
            using (var compressedStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlibStream.Write(original, 0, original.Length);
                }

                compressed = compressedStream.ToArray();
            }

            // Verify zlib header is present
            Assert.Equal(0x78, compressed[0]);

            // Decompress
            byte[] decompressed;
            using (var compressedStream = new MemoryStream(compressed))
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(resultStream);
                        decompressed = resultStream.ToArray();
                    }
                }
            }

            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void Compress_EmptyData_RoundTrips()
        {
            byte[] original = Array.Empty<byte>();

            // Compress
            byte[] compressed;
            using (var compressedStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlibStream.Write(original, 0, original.Length);
                }

                compressed = compressedStream.ToArray();
            }

            // Decompress
            byte[] decompressed;
            using (var compressedStream = new MemoryStream(compressed))
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(resultStream);
                        decompressed = resultStream.ToArray();
                    }
                }
            }

            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void Compress_LargeData_RoundTrips()
        {
            // Create a large repetitive payload (~10KB)
            var sb = new StringBuilder();
            for (int i = 0; i < 500; i++)
            {
                sb.AppendLine($"Line {i}: The quick brown fox jumps over the lazy dog.");
            }

            byte[] original = Encoding.UTF8.GetBytes(sb.ToString());

            // Compress
            byte[] compressed;
            using (var compressedStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlibStream.Write(original, 0, original.Length);
                }

                compressed = compressedStream.ToArray();
            }

            // Should actually be smaller due to repetition
            Assert.True(compressed.Length < original.Length);

            // Decompress
            byte[] decompressed;
            using (var compressedStream = new MemoryStream(compressed))
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(resultStream);
                        decompressed = resultStream.ToArray();
                    }
                }
            }

            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void Decompress_InvalidHeader_ThrowsInvalidDataException()
        {
            // Invalid zlib header — checksum fails
            byte[] invalidData = { 0x78, 0x00, 0x00, 0x00 };

            using var stream = new MemoryStream(invalidData);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Throws<InvalidDataException>(() => zlibStream.ReadByte());
        }

        [Fact]
        public void Decompress_NonDeflateCompressionMethod_ThrowsInvalidDataException()
        {
            // CMF = 0x09 means compression method 9 (not deflate)
            // FLG must satisfy (CMF * 256 + FLG) % 31 == 0
            // 0x09 * 256 = 2304, 2304 % 31 = 10, so FLG = 31 - 10 = 21 = 0x15
            byte[] invalidData = { 0x09, 0x15, 0x00, 0x00 };

            using var stream = new MemoryStream(invalidData);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Throws<InvalidDataException>(() => zlibStream.ReadByte());
        }

        [Fact]
        public void Decompress_TruncatedHeader_ThrowsInvalidDataException()
        {
            byte[] truncatedData = { 0x78 };

            using var stream = new MemoryStream(truncatedData);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Throws<InvalidDataException>(() => zlibStream.ReadByte());
        }

        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => new ZLibStream(null, CompressionMode.Decompress));
#pragma warning restore CS8625
        }

        [Fact]
        public void CanRead_DecompressMode_ReturnsTrue()
        {
            using var stream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.True(zlibStream.CanRead);
            Assert.False(zlibStream.CanWrite);
            Assert.False(zlibStream.CanSeek);
        }

        [Fact]
        public void CanWrite_CompressMode_ReturnsTrue()
        {
            using var stream = new MemoryStream();
            using var zlibStream = new ZLibStream(stream, CompressionMode.Compress);

            Assert.True(zlibStream.CanWrite);
            Assert.False(zlibStream.CanRead);
            Assert.False(zlibStream.CanSeek);
        }

        [Fact]
        public void Write_InDecompressMode_ThrowsInvalidOperationException()
        {
            using var stream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Throws<InvalidOperationException>(() => zlibStream.Write(new byte[] { 1 }, 0, 1));
        }

        [Fact]
        public void Read_InCompressMode_ThrowsInvalidOperationException()
        {
            using var stream = new MemoryStream();
            using var zlibStream = new ZLibStream(stream, CompressionMode.Compress);

            Assert.Throws<InvalidOperationException>(() => zlibStream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void Seek_ThrowsNotSupportedException()
        {
            using var stream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Throws<NotSupportedException>(() => zlibStream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void Length_ThrowsNotSupportedException()
        {
            using var stream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Throws<NotSupportedException>(() => _ = zlibStream.Length);
        }

        [Fact]
        public void Dispose_ThenRead_ThrowsObjectDisposedException()
        {
            var stream = new MemoryStream(ZLibCompressedHelloWorld);
            var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            zlibStream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => zlibStream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void LeaveOpen_True_DoesNotDisposeBaseStream()
        {
            var stream = new MemoryStream(ZLibCompressedHelloWorld);
            var zlibStream = new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: true);

            zlibStream.Dispose();

            // Stream should still be accessible
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void LeaveOpen_False_DisposesBaseStream()
        {
            var stream = new MemoryStream(ZLibCompressedHelloWorld);
            var zlibStream = new ZLibStream(stream, CompressionMode.Decompress, leaveOpen: false);

            zlibStream.Dispose();

            // Stream should be disposed
            Assert.False(stream.CanRead);
        }

        [Fact]
        public void BaseStream_ReturnsUnderlyingStream()
        {
            var stream = new MemoryStream(ZLibCompressedHelloWorld);
            using var zlibStream = new ZLibStream(stream, CompressionMode.Decompress);

            Assert.Same(stream, zlibStream.BaseStream);
        }

        [Fact]
        public void CompressedOutput_HasValidZLibHeader()
        {
            byte[] compressed;
            using (var output = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                {
                    byte[] data = Encoding.UTF8.GetBytes("test");
                    zlibStream.Write(data, 0, data.Length);
                }

                compressed = output.ToArray();
            }

            // Verify CMF byte: deflate (method 8), window size 15 (CINFO 7)
            Assert.Equal(0x78, compressed[0]);

            // Verify header checksum: (CMF * 256 + FLG) % 31 == 0
            int headerCheck = (compressed[0] * 256) + compressed[1];
            Assert.Equal(0, headerCheck % 31);
        }

        [Fact]
        public void ComputeAdler32_EmptyInput_ReturnsOne()
        {
            uint result = ZLibStream.ComputeAdler32(Array.Empty<byte>());

            // For empty input, A=1, B=0, so Adler32 = (0 << 16) | 1 = 1
            Assert.Equal(1u, result);
        }

        [Fact]
        public void ComputeAdler32_KnownInput_ReturnsExpectedChecksum()
        {
            // "Wikipedia" Adler-32 is well-known: 0x11E60398
            byte[] data = Encoding.ASCII.GetBytes("Wikipedia");
            uint result = ZLibStream.ComputeAdler32(data);

            Assert.Equal(0x11E60398u, result);
        }

        [Fact]
        public void ComputeAdler32_WithOffset_ComputesCorrectly()
        {
            byte[] data = Encoding.ASCII.GetBytes("XXWikipediaYY");
            // Offset 2, count 9 = "Wikipedia"
            uint result = ZLibStream.ComputeAdler32(data, 2, 9);

            Assert.Equal(0x11E60398u, result);
        }

        [Fact]
        public void Compress_Fastest_ProducesValidOutput()
        {
            byte[] original = Encoding.UTF8.GetBytes("test data for fastest compression level");

            byte[] compressed;
            using (var compressedStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionLevel.Fastest, leaveOpen: true))
                {
                    zlibStream.Write(original, 0, original.Length);
                }

                compressed = compressedStream.ToArray();
            }

            // Verify valid header
            Assert.Equal(0x78, compressed[0]);
            int headerCheck = (compressed[0] * 256) + compressed[1];
            Assert.Equal(0, headerCheck % 31);

            // Verify decompression round-trip
            byte[] decompressed;
            using (var compressedStream = new MemoryStream(compressed))
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(resultStream);
                        decompressed = resultStream.ToArray();
                    }
                }
            }

            Assert.Equal(original, decompressed);
        }

        [Fact]
        public void Compress_NoCompression_ProducesValidOutput()
        {
            byte[] original = Encoding.UTF8.GetBytes("test data for no compression level");

            byte[] compressed;
            using (var compressedStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionLevel.NoCompression, leaveOpen: true))
                {
                    zlibStream.Write(original, 0, original.Length);
                }

                compressed = compressedStream.ToArray();
            }

            // Verify valid header
            Assert.Equal(0x78, compressed[0]);
            int headerCheck = (compressed[0] * 256) + compressed[1];
            Assert.Equal(0, headerCheck % 31);

            // Verify decompression round-trip
            byte[] decompressed;
            using (var compressedStream = new MemoryStream(compressed))
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zlibStream.CopyTo(resultStream);
                        decompressed = resultStream.ToArray();
                    }
                }
            }

            Assert.Equal(original, decompressed);
        }

        /// <summary>
        /// Simulates the GIDS format: 4-byte GIDS header (0x01, 0x00 magic +
        /// 2-byte LE uncompressed length) followed by standard zlib-compressed data.
        /// Verifies the decompression approach used in PivSession.KeyPairs.DecompressGids.
        /// </summary>
        [Fact]
        public void Decompress_GidsFormat_WithHeaderStripping_Works()
        {
            byte[] original = Encoding.UTF8.GetBytes("Certificate data for GIDS test");

            // Compress with zlib
            byte[] zlibCompressed;
            using (var compressedStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlibStream.Write(original, 0, original.Length);
                }

                zlibCompressed = compressedStream.ToArray();
            }

            // Prepend the 4-byte GIDS header: magic (0x01, 0x00) + LE uncompressed length
            int uncompressedLength = original.Length;
            byte[] gidsData = new byte[4 + zlibCompressed.Length];
            gidsData[0] = 0x01;
            gidsData[1] = 0x00;
            gidsData[2] = (byte)(uncompressedLength & 0xFF);
            gidsData[3] = (byte)((uncompressedLength >> 8) & 0xFF);
            Buffer.BlockCopy(zlibCompressed, 0, gidsData, 4, zlibCompressed.Length);

            // Decompress like PivSession.KeyPairs.DecompressGids does:
            // strip 4-byte header, then pass to ZLibStream
            const int gidsHeaderLength = 4;
            using (var dataStream = new MemoryStream(gidsData, gidsHeaderLength, gidsData.Length - gidsHeaderLength))
            {
                using (var decompressor = new ZLibStream(dataStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        decompressor.CopyTo(resultStream);
                        byte[] decompressed = resultStream.ToArray();

                        Assert.Equal(original, decompressed);
                    }
                }
            }
        }
    }
}

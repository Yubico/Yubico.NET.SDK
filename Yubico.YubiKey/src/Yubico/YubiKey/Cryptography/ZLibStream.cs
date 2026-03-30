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

// This implementation is based on RFC 1950 (ZLIB Compressed Data Format Specification).
// It handles the zlib framing (header + Adler-32 trailer) around raw deflate data,
// delegating the actual inflate/deflate work to the standard System.IO.Compression.DeflateStream.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Yubico.YubiKey.Cryptography
{
    /// <summary>
    /// Provides methods and properties used to compress and decompress streams by
    /// using the zlib data format specification (RFC 1950).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The zlib format wraps raw DEFLATE compressed data with a 2-byte header
    /// (CMF and FLG) and a 4-byte Adler-32 checksum trailer. This class handles
    /// the framing and delegates the actual compression/decompression to
    /// <see cref="DeflateStream"/>.
    /// </para>
    /// <para>
    /// During <b>compression</b>, an Adler-32 checksum of all written bytes is
    /// computed and appended as a 4-byte big-endian trailer when the stream is
    /// disposed, producing a fully RFC 1950-compliant zlib stream.
    /// </para>
    /// <para>
    /// During <b>decompression</b>, the 2-byte zlib header is validated (checksum,
    /// compression method, and FDICT flag), but the 4-byte Adler-32 trailer is
    /// <b>not</b> verified. Corruption that is not caught by the underlying DEFLATE
    /// decoder will go undetected.
    /// </para>
    /// <para>
    /// This implementation targets .NET Standard 2.0 / 2.1 / .NET Framework 4.7.2
    /// where <c>System.IO.Compression.ZLibStream</c> is not available.
    /// </para>
    /// </remarks>
    internal sealed class ZLibStream : Stream
    {
        /// <summary>
        /// The default zlib CMF byte: deflate method (CM=8), window size 2^15 (CINFO=7).
        /// </summary>
        private const byte DefaultCmf = 0x78;

        /// <summary>
        /// The FLG byte for default compression level.
        /// Chosen so that (DefaultCmf * 256 + DefaultFlg) % 31 == 0.
        /// </summary>
        private const byte DefaultFlg = 0x9C;

        private readonly CompressionMode _mode;
        private readonly bool _leaveOpen;
        private DeflateStream? _deflateStream;
        private bool _headerProcessed;
        private bool _disposed;

        // For compression: tracks written data for Adler-32 computation
        private uint _adlerA = 1;
        private uint _adlerB;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibStream"/> class by using the
        /// specified stream and compression mode.
        /// </summary>
        /// <param name="stream">The stream to which compressed data is written or from
        /// which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to
        /// compress data to the stream or decompress data from the stream.</param>
        public ZLibStream(Stream stream, CompressionMode mode)
            : this(stream, mode, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibStream"/> class by using the
        /// specified stream, compression mode, and whether to leave the stream open.
        /// </summary>
        /// <param name="stream">The stream to which compressed data is written or from
        /// which data to decompress is read.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to
        /// compress data to the stream or decompress data from the stream.</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the stream object open
        /// after disposing the <see cref="ZLibStream"/> object; otherwise,
        /// <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mode"/> is
        /// <see cref="CompressionMode.Decompress"/> and the stream does not support
        /// reading, or <paramref name="mode"/> is <see cref="CompressionMode.Compress"/>
        /// and the stream does not support writing.</exception>
        public ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _mode = mode;
            _leaveOpen = leaveOpen;

            if (mode == CompressionMode.Compress)
            {
                if (!stream.CanWrite)
                {
                    throw new ArgumentException(ExceptionMessages.StreamDoesNotSupportWriting, nameof(stream));
                }
            }
            else if (mode == CompressionMode.Decompress)
            {
                if (!stream.CanRead)
                {
                    throw new ArgumentException(ExceptionMessages.StreamDoesNotSupportReading, nameof(stream));
                }
            }
            else
            {
                throw new ArgumentException(ExceptionMessages.InvalidCompressionModeValue, nameof(mode));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibStream"/> class by using the
        /// specified stream and compression level.
        /// </summary>
        /// <param name="stream">The stream to which compressed data is written.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates
        /// whether to emphasize speed or compression efficiency when compressing data.</param>
        public ZLibStream(Stream stream, CompressionLevel compressionLevel)
            : this(stream, compressionLevel, leaveOpen: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZLibStream"/> class by using the
        /// specified stream, compression level, and whether to leave the stream open.
        /// </summary>
        /// <param name="stream">The stream to which compressed data is written.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates
        /// whether to emphasize speed or compression efficiency when compressing data.</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the stream object open
        /// after disposing the <see cref="ZLibStream"/> object; otherwise,
        /// <see langword="false"/>.</param>
        public ZLibStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            BaseStream = stream ?? throw new ArgumentNullException(nameof(stream));

            if (!stream.CanWrite)
            {
                throw new ArgumentException(ExceptionMessages.StreamDoesNotSupportWriting, nameof(stream));
            }

            _mode = CompressionMode.Compress;
            _leaveOpen = leaveOpen;

            // Write the zlib header immediately
            WriteZLibHeader(compressionLevel);

            _deflateStream = new DeflateStream(stream, compressionLevel, leaveOpen: true);
            _headerProcessed = true;
        }

        /// <inheritdoc/>
        public override bool CanRead => !_disposed && _mode == CompressionMode.Decompress;

        /// <inheritdoc/>
        public override bool CanWrite => !_disposed && _mode == CompressionMode.Compress;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a reference to the underlying stream.
        /// </summary>
        public Stream BaseStream { get; }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(ExceptionMessages.ReadingNotSupportedOnCompressionStreams);
            }

            EnsureDecompressionInitialized();

            return _deflateStream!.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(ExceptionMessages.ReadingNotSupportedOnCompressionStreams);
            }

            EnsureDecompressionInitialized();

            return _deflateStream!.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(ExceptionMessages.ReadingNotSupportedOnCompressionStreams);
            }

            EnsureDecompressionInitialized();

            return _deflateStream!.ReadByte();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Compress)
            {
                throw new InvalidOperationException(ExceptionMessages.WritingNotSupportedOnDecompressionStreams);
            }

            EnsureCompressionInitialized();

            // Track uncompressed data for Adler-32
            UpdateAdler32(buffer, offset, count);

            _deflateStream!.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Compress)
            {
                throw new InvalidOperationException(ExceptionMessages.WritingNotSupportedOnDecompressionStreams);
            }

            EnsureCompressionInitialized();

            // Track uncompressed data for Adler-32
            UpdateAdler32(buffer, offset, count);

            return _deflateStream!.WriteAsync(buffer, offset, count, cancellationToken);
        }

#if NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(ExceptionMessages.ReadingNotSupportedOnCompressionStreams);
            }

            EnsureDecompressionInitialized();

            return _deflateStream!.ReadAsync(buffer, cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Compress)
            {
                throw new InvalidOperationException(ExceptionMessages.WritingNotSupportedOnDecompressionStreams);
            }

            EnsureCompressionInitialized();

            // Track uncompressed data for Adler-32
            if (!buffer.IsEmpty)
            {
                byte[] temp = buffer.ToArray();
                UpdateAdler32(temp, 0, temp.Length);
            }

            return _deflateStream!.WriteAsync(buffer, cancellationToken);
        }
#endif

        /// <inheritdoc/>
        public override void Flush()
        {
            ThrowIfDisposed();
            _deflateStream?.Flush();
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_deflateStream != null)
            {
                return _deflateStream.FlushAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) =>
            throw new NotSupportedException();

#if NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc/>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(ExceptionMessages.CopyToNotSupportedOnCompressionStreams);
            }

            EnsureDecompressionInitialized();

            _deflateStream!.CopyTo(destination, bufferSize);
        }
#endif

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_mode != CompressionMode.Decompress)
            {
                throw new InvalidOperationException(ExceptionMessages.CopyToAsyncNotSupportedOnCompressionStreams);
            }

            EnsureDecompressionInitialized();

            return _deflateStream!.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_mode == CompressionMode.Compress && _deflateStream != null)
                    {
                        // Flush and close the deflate stream to finalize compressed data
                        _deflateStream.Dispose();
                        _deflateStream = null;

                        // Write the Adler-32 checksum trailer (big-endian)
                        WriteAdler32Trailer();
                    }
                    else
                    {
                        _deflateStream?.Dispose();
                        _deflateStream = null;
                    }

                    if (!_leaveOpen)
                    {
                        BaseStream.Dispose();
                    }
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Reads and validates the 2-byte zlib header (RFC 1950 section 2.2).
        /// After validation, creates the internal <see cref="DeflateStream"/>
        /// positioned at the start of the raw deflate data.
        /// </summary>
        /// <exception cref="InvalidDataException">The zlib header is invalid.</exception>
        private void ReadAndValidateZLibHeader()
        {
            int cmf = BaseStream.ReadByte();
            int flg = BaseStream.ReadByte();

            if (cmf == -1 || flg == -1)
            {
                throw new InvalidDataException(ExceptionMessages.UnexpectedEndOfZlibHeader);
            }

            // Validate the header checksum: (CMF * 256 + FLG) must be divisible by 31
            if (((cmf * 256) + flg) % 31 != 0)
            {
                throw new InvalidDataException(ExceptionMessages.InvalidZlibHeaderChecksum);
            }

            // Extract compression method (lower 4 bits of CMF)
            int compressionMethod = cmf & 0x0F;
            if (compressionMethod != 8)
            {
                throw new InvalidDataException(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        ExceptionMessages.UnsupportedZlibCompressionMethod,
                        compressionMethod));
            }

            // Check FDICT flag (bit 5 of FLG) - preset dictionary not supported
            bool hasPresetDictionary = (flg & 0x20) != 0;
            if (hasPresetDictionary)
            {
                throw new InvalidDataException(ExceptionMessages.ZlibPresetDictionaryNotSupported);
            }
        }

        /// <summary>
        /// Writes the 2-byte zlib header to the base stream.
        /// </summary>
        private void WriteZLibHeader(CompressionLevel compressionLevel)
        {
            byte cmf = DefaultCmf;
            byte flg;

            // Choose FLEVEL based on compression level and ensure header checksum is valid
            switch (compressionLevel)
            {
                case CompressionLevel.NoCompression:
                    // FLEVEL = 0 (compressor used fastest algorithm)
                    flg = ComputeFlg(cmf, 0);
                    break;
                case CompressionLevel.Fastest:
                    // FLEVEL = 1 (compressor used fast algorithm)
                    flg = ComputeFlg(cmf, 1);
                    break;
                default:
                    // FLEVEL = 2 (default) - covers Optimal and SmallestSize
                    flg = DefaultFlg;
                    break;
            }

            BaseStream.WriteByte(cmf);
            BaseStream.WriteByte(flg);
        }

        /// <summary>
        /// Computes the FLG byte given a CMF byte and desired FLEVEL (0-3).
        /// Ensures that (CMF * 256 + FLG) % 31 == 0 per RFC 1950.
        /// </summary>
        private static byte ComputeFlg(byte cmf, int flevel)
        {
            // FLG layout: FLEVEL (2 bits) | FDICT (1 bit, 0) | FCHECK (5 bits)
            int flgBase = (flevel & 0x03) << 6;
            int remainder = ((cmf * 256) + flgBase) % 31;
            int fcheck = (31 - remainder) % 31;

            return (byte)(flgBase | fcheck);
        }

        /// <summary>
        /// Writes the 4-byte Adler-32 checksum trailer in big-endian byte order.
        /// </summary>
        private void WriteAdler32Trailer()
        {
            uint checksum = (_adlerB << 16) | _adlerA;

            BaseStream.WriteByte((byte)(checksum >> 24));
            BaseStream.WriteByte((byte)(checksum >> 16));
            BaseStream.WriteByte((byte)(checksum >> 8));
            BaseStream.WriteByte((byte)checksum);
        }

        /// <summary>
        /// Updates the running Adler-32 checksum with the given data.
        /// </summary>
        /// <remarks>
        /// Adler-32 is defined in RFC 1950 section 9. It consists of two 16-bit
        /// checksums A and B: A = 1 + sum of all bytes, B = sum of all A values,
        /// both modulo 65521.
        /// </remarks>
        private void UpdateAdler32(byte[] buffer, int offset, int count)
        {
            const uint modAdler = 65521;

            for (int i = offset; i < offset + count; i++)
            {
                _adlerA = (_adlerA + buffer[i]) % modAdler;
                _adlerB = (_adlerB + _adlerA) % modAdler;
            }
        }

        /// <summary>
        /// Ensures the zlib header has been read and the internal DeflateStream
        /// is initialized for decompression.
        /// </summary>
        private void EnsureDecompressionInitialized()
        {
            if (!_headerProcessed)
            {
                ReadAndValidateZLibHeader();
                _deflateStream = new DeflateStream(BaseStream, CompressionMode.Decompress, leaveOpen: true);
                _headerProcessed = true;
            }
        }

        /// <summary>
        /// Ensures the zlib header has been written and the internal DeflateStream
        /// is initialized for compression.
        /// </summary>
        private void EnsureCompressionInitialized()
        {
            if (!_headerProcessed)
            {
                // Default compression level header
                BaseStream.WriteByte(DefaultCmf);
                BaseStream.WriteByte(DefaultFlg);
                _deflateStream = new DeflateStream(BaseStream, CompressionLevel.Optimal, leaveOpen: true);
                _headerProcessed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        /// <summary>
        /// Computes the Adler-32 checksum over an entire byte array.
        /// </summary>
        /// <param name="data">The data to compute the checksum for.</param>
        /// <returns>The 32-bit Adler-32 checksum value.</returns>
        internal static uint ComputeAdler32(byte[] data)
        {
            return ComputeAdler32(data, 0, data.Length);
        }

        /// <summary>
        /// Computes the Adler-32 checksum over a segment of a byte array.
        /// </summary>
        /// <param name="data">The data to compute the checksum for.</param>
        /// <param name="offset">The offset into the data to start from.</param>
        /// <param name="count">The number of bytes to include.</param>
        /// <returns>The 32-bit Adler-32 checksum value.</returns>
        internal static uint ComputeAdler32(byte[] data, int offset, int count)
        {
            const uint modAdler = 65521;
            uint a = 1;
            uint b = 0;

            for (int i = offset; i < offset + count; i++)
            {
                a = (a + data[i]) % modAdler;
                b = (b + a) % modAdler;
            }

            return (b << 16) | a;
        }
    }
}

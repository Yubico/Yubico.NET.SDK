// Copyright 2021 Yubico AB
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
using System.Diagnostics;
using System.Security.Cryptography;
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Cryptography
{
    [Trait("Category", "Simple")]
    public class RsaOaepTimingTests
    {
        private const int IterationCount1024 = 200000;
        private const int IterationCount2048 = 100000;

        private readonly ITestOutputHelper _output;
        private readonly RandomNumberGenerator _random;


        public RsaOaepTimingTests(ITestOutputHelper output)
        {
            _output = output;
            _random = RandomObjectUtility.GetRandomObject(fixedBytes: null);
        }

        [Theory]
        [InlineData(1024, RsaFormat.Sha1, 16)]
        [InlineData(1024, RsaFormat.Sha1, 24)]
        [InlineData(1024, RsaFormat.Sha1, 32)]
        [InlineData(1024, RsaFormat.Sha1, 48)]
        [InlineData(1024, RsaFormat.Sha256, 16)]
        [InlineData(1024, RsaFormat.Sha256, 24)]
        [InlineData(1024, RsaFormat.Sha256, 32)]
        [InlineData(1024, RsaFormat.Sha256, 48)]
        [InlineData(1024, RsaFormat.Sha384, 16)]
        [InlineData(1024, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha1, 16)]
        [InlineData(2048, RsaFormat.Sha1, 24)]
        [InlineData(2048, RsaFormat.Sha1, 32)]
        [InlineData(2048, RsaFormat.Sha1, 48)]
        [InlineData(2048, RsaFormat.Sha256, 16)]
        [InlineData(2048, RsaFormat.Sha256, 24)]
        [InlineData(2048, RsaFormat.Sha256, 32)]
        [InlineData(2048, RsaFormat.Sha256, 48)]
        [InlineData(2048, RsaFormat.Sha384, 16)]
        [InlineData(2048, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha384, 32)]
        [InlineData(2048, RsaFormat.Sha384, 48)]
        [InlineData(2048, RsaFormat.Sha512, 16)]
        [InlineData(2048, RsaFormat.Sha512, 24)]
        [InlineData(2048, RsaFormat.Sha512, 32)]
        [InlineData(2048, RsaFormat.Sha512, 48)]
        public void CorrectPad_Time(int keySizeBits, int digestFlag, int dataLength)
        {
            var dataToPad = new byte[dataLength];
            _random.GetBytes(dataToPad);
            var formattedData = RsaFormat.FormatPkcs1Oaep(dataToPad, digestFlag, keySizeBits);

            var totalTime = RunTimerOaep(formattedData, digestFlag, dataLength, out var isValid);

            WriteResult("OAEP", keySizeBits, digestFlag, dataLength, errorType: 0, totalTime);

            Assert.True(isValid);
        }

        [Theory]
        [InlineData(1024, RsaFormat.Sha1, 16)]
        [InlineData(1024, RsaFormat.Sha1, 24)]
        [InlineData(1024, RsaFormat.Sha1, 32)]
        [InlineData(1024, RsaFormat.Sha1, 48)]
        [InlineData(1024, RsaFormat.Sha256, 16)]
        [InlineData(1024, RsaFormat.Sha256, 24)]
        [InlineData(1024, RsaFormat.Sha256, 32)]
        [InlineData(1024, RsaFormat.Sha256, 48)]
        [InlineData(1024, RsaFormat.Sha384, 16)]
        [InlineData(1024, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha1, 16)]
        [InlineData(2048, RsaFormat.Sha1, 24)]
        [InlineData(2048, RsaFormat.Sha1, 32)]
        [InlineData(2048, RsaFormat.Sha1, 48)]
        [InlineData(2048, RsaFormat.Sha256, 16)]
        [InlineData(2048, RsaFormat.Sha256, 24)]
        [InlineData(2048, RsaFormat.Sha256, 32)]
        [InlineData(2048, RsaFormat.Sha256, 48)]
        [InlineData(2048, RsaFormat.Sha384, 16)]
        [InlineData(2048, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha384, 32)]
        [InlineData(2048, RsaFormat.Sha384, 48)]
        [InlineData(2048, RsaFormat.Sha512, 16)]
        [InlineData(2048, RsaFormat.Sha512, 24)]
        [InlineData(2048, RsaFormat.Sha512, 32)]
        [InlineData(2048, RsaFormat.Sha512, 48)]
        public void FirstByteWrong_Time(int keySizeBits, int digestFlag, int dataLength)
        {
            var dataToPad = new byte[dataLength];
            _random.GetBytes(dataToPad);
            var formattedData = RsaFormat.FormatPkcs1Oaep(dataToPad, digestFlag, keySizeBits);
            formattedData[0] = 0x23;

            var totalTime = RunTimerOaep(formattedData, digestFlag, expectedLength: 0, out var isValid);

            WriteResult("OAEP", keySizeBits, digestFlag, dataLength, errorType: 1, totalTime);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024, RsaFormat.Sha1, 16)]
        [InlineData(1024, RsaFormat.Sha1, 24)]
        [InlineData(1024, RsaFormat.Sha1, 32)]
        [InlineData(1024, RsaFormat.Sha1, 48)]
        [InlineData(1024, RsaFormat.Sha256, 16)]
        [InlineData(1024, RsaFormat.Sha256, 24)]
        [InlineData(1024, RsaFormat.Sha256, 32)]
        [InlineData(1024, RsaFormat.Sha256, 48)]
        [InlineData(1024, RsaFormat.Sha384, 16)]
        [InlineData(1024, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha1, 16)]
        [InlineData(2048, RsaFormat.Sha1, 24)]
        [InlineData(2048, RsaFormat.Sha1, 32)]
        [InlineData(2048, RsaFormat.Sha1, 48)]
        [InlineData(2048, RsaFormat.Sha256, 16)]
        [InlineData(2048, RsaFormat.Sha256, 24)]
        [InlineData(2048, RsaFormat.Sha256, 32)]
        [InlineData(2048, RsaFormat.Sha256, 48)]
        [InlineData(2048, RsaFormat.Sha384, 16)]
        [InlineData(2048, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha384, 32)]
        [InlineData(2048, RsaFormat.Sha384, 48)]
        [InlineData(2048, RsaFormat.Sha512, 16)]
        [InlineData(2048, RsaFormat.Sha512, 24)]
        [InlineData(2048, RsaFormat.Sha512, 32)]
        [InlineData(2048, RsaFormat.Sha512, 48)]
        public void LHashWrong_Time(int keySizeBits, int digestFlag, int dataLength)
        {
            var dataToPad = new byte[dataLength];
            _random.GetBytes(dataToPad);
            var formattedData = FormatOaepWrong(dataToPad, digestFlag, keySizeBits, errorType: 1);

            var totalTime = RunTimerOaep(formattedData, digestFlag, expectedLength: 0, out var isValid);

            WriteResult("OAEP", keySizeBits, digestFlag, dataLength, errorType: 2, totalTime);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024, RsaFormat.Sha1, 16)]
        [InlineData(1024, RsaFormat.Sha1, 24)]
        [InlineData(1024, RsaFormat.Sha1, 32)]
        [InlineData(1024, RsaFormat.Sha1, 48)]
        [InlineData(1024, RsaFormat.Sha256, 16)]
        [InlineData(1024, RsaFormat.Sha256, 24)]
        [InlineData(1024, RsaFormat.Sha256, 32)]
        [InlineData(1024, RsaFormat.Sha256, 48)]
        [InlineData(1024, RsaFormat.Sha384, 16)]
        [InlineData(1024, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha1, 16)]
        [InlineData(2048, RsaFormat.Sha1, 24)]
        [InlineData(2048, RsaFormat.Sha1, 32)]
        [InlineData(2048, RsaFormat.Sha1, 48)]
        [InlineData(2048, RsaFormat.Sha256, 16)]
        [InlineData(2048, RsaFormat.Sha256, 24)]
        [InlineData(2048, RsaFormat.Sha256, 32)]
        [InlineData(2048, RsaFormat.Sha256, 48)]
        [InlineData(2048, RsaFormat.Sha384, 16)]
        [InlineData(2048, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha384, 32)]
        [InlineData(2048, RsaFormat.Sha384, 48)]
        [InlineData(2048, RsaFormat.Sha512, 16)]
        [InlineData(2048, RsaFormat.Sha512, 24)]
        [InlineData(2048, RsaFormat.Sha512, 32)]
        [InlineData(2048, RsaFormat.Sha512, 48)]
        public void SeparatorWrong_Time(int keySizeBits, int digestFlag, int dataLength)
        {
            var dataToPad = new byte[dataLength];
            _random.GetBytes(dataToPad);
            var formattedData = FormatOaepWrong(dataToPad, digestFlag, keySizeBits, errorType: 2);

            var totalTime = RunTimerOaep(formattedData, digestFlag, expectedLength: 0, out var isValid);

            WriteResult("OAEP", keySizeBits, digestFlag, dataLength, errorType: 3, totalTime);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024, RsaFormat.Sha1, 16)]
        [InlineData(1024, RsaFormat.Sha1, 24)]
        [InlineData(1024, RsaFormat.Sha1, 32)]
        [InlineData(1024, RsaFormat.Sha1, 48)]
        [InlineData(1024, RsaFormat.Sha256, 16)]
        [InlineData(1024, RsaFormat.Sha256, 24)]
        [InlineData(1024, RsaFormat.Sha256, 32)]
        [InlineData(1024, RsaFormat.Sha256, 48)]
        [InlineData(1024, RsaFormat.Sha384, 16)]
        [InlineData(1024, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha1, 16)]
        [InlineData(2048, RsaFormat.Sha1, 24)]
        [InlineData(2048, RsaFormat.Sha1, 32)]
        [InlineData(2048, RsaFormat.Sha1, 48)]
        [InlineData(2048, RsaFormat.Sha256, 16)]
        [InlineData(2048, RsaFormat.Sha256, 24)]
        [InlineData(2048, RsaFormat.Sha256, 32)]
        [InlineData(2048, RsaFormat.Sha256, 48)]
        [InlineData(2048, RsaFormat.Sha384, 16)]
        [InlineData(2048, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha384, 32)]
        [InlineData(2048, RsaFormat.Sha384, 48)]
        [InlineData(2048, RsaFormat.Sha512, 16)]
        [InlineData(2048, RsaFormat.Sha512, 24)]
        [InlineData(2048, RsaFormat.Sha512, 32)]
        [InlineData(2048, RsaFormat.Sha512, 48)]
        public void NoSeparator_Time(int keySizeBits, int digestFlag, int dataLength)
        {
            var dataToPad = new byte[dataLength];
            _random.GetBytes(dataToPad);
            var formattedData = FormatOaepWrong(dataToPad, digestFlag, keySizeBits, errorType: 4);

            var totalTime = RunTimerOaep(formattedData, digestFlag, expectedLength: 0, out var isValid);

            WriteResult("OAEP", keySizeBits, digestFlag, dataLength, errorType: 4, totalTime);

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024, RsaFormat.Sha1, 16)]
        [InlineData(1024, RsaFormat.Sha1, 24)]
        [InlineData(1024, RsaFormat.Sha1, 32)]
        [InlineData(1024, RsaFormat.Sha1, 48)]
        [InlineData(1024, RsaFormat.Sha256, 16)]
        [InlineData(1024, RsaFormat.Sha256, 24)]
        [InlineData(1024, RsaFormat.Sha256, 32)]
        [InlineData(1024, RsaFormat.Sha256, 48)]
        [InlineData(1024, RsaFormat.Sha384, 16)]
        [InlineData(1024, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha1, 16)]
        [InlineData(2048, RsaFormat.Sha1, 24)]
        [InlineData(2048, RsaFormat.Sha1, 32)]
        [InlineData(2048, RsaFormat.Sha1, 48)]
        [InlineData(2048, RsaFormat.Sha256, 16)]
        [InlineData(2048, RsaFormat.Sha256, 24)]
        [InlineData(2048, RsaFormat.Sha256, 32)]
        [InlineData(2048, RsaFormat.Sha256, 48)]
        [InlineData(2048, RsaFormat.Sha384, 16)]
        [InlineData(2048, RsaFormat.Sha384, 24)]
        [InlineData(2048, RsaFormat.Sha384, 32)]
        [InlineData(2048, RsaFormat.Sha384, 48)]
        [InlineData(2048, RsaFormat.Sha512, 16)]
        [InlineData(2048, RsaFormat.Sha512, 24)]
        [InlineData(2048, RsaFormat.Sha512, 32)]
        [InlineData(2048, RsaFormat.Sha512, 48)]
        public void ComboWrong_Time(int keySizeBits, int digestFlag, int dataLength)
        {
            var dataToPad = new byte[dataLength];
            _random.GetBytes(dataToPad);
            var formattedData = FormatOaepWrong(dataToPad, digestFlag, keySizeBits, errorType: 3);
            formattedData[0] = 2;

            var totalTime = RunTimerOaep(formattedData, digestFlag, expectedLength: 0, out var isValid);

            WriteResult("OAEP", keySizeBits, digestFlag, dataLength, errorType: 5, totalTime);

            Assert.False(isValid);
        }

        private static long RunTimerOaep(byte[] formattedData, int digestFlag, int expectedLength, out bool isValid)
        {
            isValid = false;
            var iterationCount = IterationCount1024;
            if (formattedData.Length == 256)
            {
                iterationCount = IterationCount2048;
            }

            var outputData = Array.Empty<byte>();
            var timer = Stopwatch.StartNew();
            for (var index = 0; index < iterationCount; index++)
            {
                isValid = RsaFormat.TryParsePkcs1Oaep(formattedData, digestFlag, out outputData);
            }

            timer.Stop();

            if (expectedLength == 0)
            {
                Assert.Empty(outputData);
            }
            else
            {
                Assert.Equal(expectedLength, outputData.Length);
            }

            return timer.ElapsedMilliseconds;
        }

        private void WriteResult(
            string scheme, int keySizeBits, int digestFlag, int dataLength, int errorType, long totalTime)
        {
            var message = errorType switch
            {
                1 => "first byte wrong",
                2 => "incorrect lHash",
                3 => "incorrect separator",
                4 => "no separator",
                5 => "first, lHash, incorrect separator",
                6 => "lHash, incorrect separator",
                _ => "all correct"
            };

            var digestAlg = digestFlag switch
            {
                RsaFormat.Sha1 => "SHA-1",
                RsaFormat.Sha256 => "SHA-256",
                RsaFormat.Sha384 => "SHA-384",
                _ => "SHA-512"
            };

            _output.WriteLine(
                scheme + " " + keySizeBits + ", digest alg = " + digestAlg +
                ", data length = " + dataLength + ", " + message + "\n" + "total time: {0}", totalTime);
        }

        // If the 1 bit in errorType is set, make the lHash go bad
        // the 2, 01 byte wrong
        // the 4, no nonzero byte
        public static byte[] FormatOaepWrong(
            ReadOnlySpan<byte> inputData, int digestAlgorithm, int keySizeBits, int errorType)
        {
            var buffer = GetKeySizeBuffer(keySizeBits);

            var bufferAsSpan = new Span<byte>(buffer);

            using var digester = GetHashAlgorithm(digestAlgorithm);

            var digestLength = digester.HashSize / 8;

            if (inputData.Length == 0 || inputData.Length > buffer.Length - ((2 * digestLength) + 2))
            {
                throw new ArgumentException("invalid length");
            }

            // Build the buffer:
            //  00 || seed || lHash || PS || 01 || input data
            // Beginning with lHash is the DB
            //  DB = lHash || PS || 01 || input data
            using var randomObject = CryptographyProviders.RngCreator();
            // seed
            randomObject.GetBytes(buffer, offset: 1, digestLength);

            // lHash = digest of empty string.
            _ = digester.TransformFinalBlock(buffer, inputOffset: 0, inputCount: 0);
            Array.Copy(digester.Hash!, sourceIndex: 0, buffer, digestLength + 1, digestLength);
            if ((errorType & 1) != 0)
            {
                buffer[digestLength + 5]++;
            }

            // 01
            if ((errorType & 2) != 0)
            {
                buffer[^(inputData.Length + 1)] = 2;
            }
            else
            {
                buffer[^(inputData.Length + 1)] = 1;
            }

            inputData.CopyTo(bufferAsSpan[(buffer.Length - inputData.Length)..]);
            if ((errorType & 4) != 0)
            {
                for (var indexData = (2 * digestLength) + 1; indexData < buffer.Length; indexData++)
                {
                    buffer[indexData] = 0;
                }
            }

            // Use the seed to mask the DB.
            PerformMgf1(buffer, offsetSeed: 1, digestLength, buffer, digestLength + 1,
                buffer.Length - (digestLength + 1), digester);

            // Use the masked DB to mask the seed.
            PerformMgf1(buffer, digestLength + 1, buffer.Length - (digestLength + 1), buffer, offsetTarget: 1,
                digestLength, digester);

            return buffer;
        }

        private static byte[] GetKeySizeBuffer(int keySizeBits)
        {
            return keySizeBits switch
            {
                1024 => new byte[128],
                _ => new byte[256]
            };
        }

        private static void PerformMgf1(
            byte[] seed,
            int offsetSeed,
            int seedLength,
            byte[] target,
            int offsetTarget,
            int targetLength,
            HashAlgorithm digester)
        {
            var bytesRemaining = targetLength;
            var offset = offsetTarget;
            var digestLength = digester.HashSize / 8;

            var counter = new byte[4];
            while (bytesRemaining > 0)
            {
                var xorCount = bytesRemaining;
                if (digestLength <= bytesRemaining)
                {
                    xorCount = digestLength;
                }

                digester.Initialize();
                _ = digester.TransformBlock(seed, offsetSeed, seedLength, outputBuffer: null, outputOffset: 0);
                _ = digester.TransformFinalBlock(counter, inputOffset: 0, inputCount: 4);

                for (var index = 0; index < xorCount; index++)
                {
                    target[offset + index] ^= digester.Hash![index];
                }

                bytesRemaining -= xorCount;
                offset += xorCount;
                counter[3]++;
            }
        }

        private static HashAlgorithm GetHashAlgorithm(int digestAlgorithm)
        {
            return digestAlgorithm switch
            {
                RsaFormat.Sha1 => CryptographyProviders.Sha1Creator(),
                RsaFormat.Sha256 => CryptographyProviders.Sha256Creator(),
                RsaFormat.Sha384 => CryptographyProviders.Sha384Creator(),
                _ => CryptographyProviders.Sha512Creator()
            };
        }
    }
}

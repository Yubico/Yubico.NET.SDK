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
    public class RsaP1TimingTests
    {
        private const int IterationCount1024 = 10000000;
        private const int IterationCount2048 = 4500000;

        private readonly ITestOutputHelper _output;
        private readonly RandomNumberGenerator _random;

        public RsaP1TimingTests(ITestOutputHelper output)
        {
            _output = output;
            _random = RandomObjectUtility.GetRandomObject(fixedBytes: null);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void CorrectPad_Time(int keySizeBits)
        {
            var isValid = false;
            int[] dataLength = { 16, 24, 32, 48 };

            for (var index = 0; index < dataLength.Length; index++)
            {
                var dataToPad = new byte[dataLength[index]];
                _random.GetBytes(dataToPad);
                var formattedData = RsaFormat.FormatPkcs1Encrypt(dataToPad, keySizeBits);

                var totalTime = RunTimerP15(formattedData, dataLength[index], out isValid);

                WriteResult("P1.5", keySizeBits, dataLength[index], errorType: 0, totalTime);
            }

            Assert.True(isValid);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void FirstByteWrong_Time(int keySizeBits)
        {
            var isValid = false;
            int[] dataLength = { 16, 24, 32, 48 };

            for (var index = 0; index < dataLength.Length; index++)
            {
                var dataToPad = new byte[dataLength[index]];
                _random.GetBytes(dataToPad);
                var formattedData = RsaFormat.FormatPkcs1Encrypt(dataToPad, keySizeBits);
                formattedData[0] = 1;

                var totalTime = RunTimerP15(formattedData, expectedLength: 0, out isValid);

                WriteResult("P1.5", keySizeBits, dataLength[index], errorType: 1, totalTime);
            }

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void SecondByteWrong_Time(int keySizeBits)
        {
            var isValid = false;
            int[] dataLength = { 16, 24, 32, 48 };

            for (var index = 0; index < dataLength.Length; index++)
            {
                var dataToPad = new byte[dataLength[index]];
                _random.GetBytes(dataToPad);
                var formattedData = RsaFormat.FormatPkcs1Encrypt(dataToPad, keySizeBits);
                formattedData[1] = 0x06;

                var totalTime = RunTimerP15(formattedData, expectedLength: 0, out isValid);

                WriteResult("P1.5", keySizeBits, dataLength[index], errorType: 2, totalTime);
            }

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void NotEnoughPad_Time(int keySizeBits)
        {
            var isValid = false;
            int[] dataLength = { 16, 24, 32, 48 };

            for (var index = 0; index < dataLength.Length; index++)
            {
                var dataToPad = new byte[dataLength[index]];
                _random.GetBytes(dataToPad);
                var formattedData = RsaFormat.FormatPkcs1Encrypt(dataToPad, keySizeBits);
                formattedData[8] = 0;

                var totalTime = RunTimerP15(formattedData, expectedLength: 0, out isValid);

                WriteResult("P1.5", keySizeBits, dataLength[index], errorType: 3, totalTime);
            }

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void MultipleErrors_Time(int keySizeBits)
        {
            var isValid = false;
            int[] dataLength = { 16, 24, 32, 48 };

            for (var index = 0; index < dataLength.Length; index++)
            {
                var dataToPad = new byte[dataLength[index]];
                _random.GetBytes(dataToPad);
                var formattedData = RsaFormat.FormatPkcs1Encrypt(dataToPad, keySizeBits);
                formattedData[0] = 1;
                formattedData[1] = 3;
                formattedData[5] = 0;

                var totalTime = RunTimerP15(formattedData, expectedLength: 0, out isValid);

                WriteResult("P1.5", keySizeBits, dataLength[index], errorType: 5, totalTime);
            }

            Assert.False(isValid);
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(2048)]
        public void NoZeroByte_Time(int keySizeBits)
        {
            var bufferSize = keySizeBits / 8;
            var formattedData = new byte[bufferSize];
            _random.GetBytes(formattedData);
            formattedData[0] = 0;
            formattedData[1] = 2;
            for (var index = 2; index < bufferSize; index++)
            {
                if (formattedData[index] == 0)
                {
                    formattedData[index] = 0x86;
                }
            }

            var totalTime = RunTimerP15(formattedData, expectedLength: 0, out var isValid);

            Assert.False(isValid);

            WriteResult("P1.5", keySizeBits, bufferSize, errorType: 4, totalTime);
        }

        private static long RunTimerP15(byte[] formattedData, int expectedLength, out bool isValid)
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
                isValid = RsaFormat.TryParsePkcs1Decrypt(formattedData, out outputData);
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

        private void WriteResult(string scheme, int keySizeBits, int dataLength, int errorType, long totalTime)
        {
            var message = errorType switch
            {
                1 => "first byte wrong",
                2 => "second byte wrong",
                3 => "not enough pad",
                4 => "no zero byte",
                5 => "first, second, not enough pad",
                _ => "all correct"
            };

            _output.WriteLine(
                scheme + " " + keySizeBits + ", " + "data length = " + dataLength + ", "
                + message + "\n" + "  total time: {0}", totalTime);
        }
    }
}

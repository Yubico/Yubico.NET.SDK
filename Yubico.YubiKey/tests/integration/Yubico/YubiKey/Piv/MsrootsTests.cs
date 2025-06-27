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
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class MsrootsTests(ITestOutputHelper output) : PivSessionIntegrationTestBase
    {
        public RandomNumberGenerator Rng { get; set; } = RandomObjectUtility.GetRandomObject(null);

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void SimplePutDataCommand(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            var isValid = Session.TryAuthenticateManagementKey();
            Assert.True(isValid);

            byte[] putData = { 0x53, 0x04, 0x11, 0x22, 0x33, 0x44 };
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            putData = new byte[] { 0x53, 0x00 };
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            putData = new byte[] { 0x53, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 };
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            putData = new byte[] { 0x53, 0x06, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteDataSession(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            byte[] putData = { 0x11, 0x22, 0x33, 0x44 };
            Session.WriteMsroots(putData);

            Session.WriteMsroots(ReadOnlySpan<byte>.Empty);

            putData = new byte[] { 0x53, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 };
            Session.WriteMsroots(putData);

            putData = new byte[] { 0x53, 0x06, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
            Session.WriteMsroots(putData);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteDataSessionBig(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;

            var versionCommand = new VersionCommand();
            var versionResponse = Session.Connection.SendCommand(versionCommand);
            Assert.Equal(ResponseStatus.Success, versionResponse.Status);

            var versionNumber = versionResponse.GetData();
            var maxLength = 10150;
            if (versionNumber.Major >= 4)
            {
                maxLength = 14000;
            }

            var putData = new byte[maxLength];
            Rng.GetBytes(putData, 0, putData.Length);

            Session.WriteMsroots(putData);
            Session.WriteMsroots(ReadOnlySpan<byte>.Empty);

            Rng.GetBytes(putData, 0, putData.Length);
            var memStream = new MemoryStream(putData);
            Session.WriteMsrootsStream(memStream);

            Rng.GetBytes(putData, 0, putData.Length);
            memStream = new MemoryStream(putData);
            Session.WriteMsrootsStream(memStream);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteReadMsroots_ByteArray(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            Session.DeleteMsroots();

            var currentLength = 6000;
            var arbitraryData = new byte[currentLength];
            Rng.GetBytes(arbitraryData, 0, arbitraryData.Length);

            Session.WriteMsroots(arbitraryData);
            var getData = Session.ReadMsroots();
            Assert.True(getData.Length == currentLength);

            var compareResult = getData.SequenceEqual(arbitraryData);
            Assert.True(compareResult);

            Session.DeleteMsroots();
            getData = Session.ReadMsroots();
            Assert.Empty(getData);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteReadMsroots_Stream(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            const int currentLength = 6000;

            var arbitraryData = new byte[currentLength];
            Rng.GetBytes(arbitraryData, 0, arbitraryData.Length);
            Session.DeleteMsroots();
            Session.WriteMsroots(arbitraryData);

            var getData = Session.ReadMsrootsStream();
            var binReader = new BinaryReader(getData);
            var theData = binReader.ReadBytes((int)getData.Length);
            Assert.Equal(currentLength, theData.Length);
            Assert.True(theData.SequenceEqual(arbitraryData));

            Session.DeleteMsroots();
            getData = Session.ReadMsrootsStream();
            Assert.Equal(0, getData.Length);
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteMsroots_Commands(
            StandardTestDevice testDeviceType)
        {
            TestDeviceType = testDeviceType;
            for (var bufferSize = 2806; bufferSize <= 2808; bufferSize++)
            {
                output.WriteLine("buffer size: {0}", bufferSize);

                var isValid = Session.TryAuthenticateManagementKey();
                Assert.True(isValid);

                var putData = new byte[bufferSize];
                Rng.GetBytes(putData, 0, putData.Length);
                var dataLength = bufferSize - 4;
                putData[0] = 0x53;
                putData[1] = 0x82;
                putData[2] = (byte)(dataLength >> 8);
                putData[3] = (byte)dataLength;
                dataLength = bufferSize - 8;
                putData[4] = 0x83;
                putData[5] = 0x82;
                putData[6] = (byte)(dataLength >> 8);
                putData[7] = (byte)dataLength;

                isValid = DoWriteAndWrite(Session, putData);
                Assert.True(isValid);
            }
        }

        private static bool DoWriteAndWrite(
            PivSession Session,
            byte[] putData)
        {
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            byte[] noData = { 0x53, 0x00 };
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, noData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = Session.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            return true;
        }

        override protected void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                Rng.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

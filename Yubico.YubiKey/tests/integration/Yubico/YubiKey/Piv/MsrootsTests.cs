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
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv
{
    public class MsrootsTests
    {
        private readonly ITestOutputHelper _output;

        public MsrootsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void SimplePutDataCommand(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                var isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                pivSession.ResetApplication();

                isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                byte[] putData = { 0x53, 0x04, 0x11, 0x22, 0x33, 0x44 };
                for (var index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    var putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }

                putData = new byte[] { 0x53, 0x00 };
                for (var index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    var putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }

                putData = new byte[] { 0x53, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 };
                for (var index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    var putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }

                putData = new byte[] { 0x53, 0x06, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
                for (var index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    var putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteDataSession(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                byte[] putData = { 0x11, 0x22, 0x33, 0x44 };
                pivSession.WriteMsroots(putData);

                pivSession.WriteMsroots(ReadOnlySpan<byte>.Empty);

                putData = new byte[] { 0x53, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 };
                pivSession.WriteMsroots(putData);

                putData = new byte[] { 0x53, 0x06, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
                pivSession.WriteMsroots(putData);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteDataSessionBig(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var rng = RandomObjectUtility.GetRandomObject(fixedBytes: null);

            using (var pivSession = new PivSession(testDevice))
            {
                var versionCommand = new VersionCommand();
                var versionResponse = pivSession.Connection.SendCommand(versionCommand);
                Assert.Equal(ResponseStatus.Success, versionResponse.Status);

                var versionNumber = versionResponse.GetData();

                //                int maxLength = 10175;
                var maxLength = 10150;
                if (versionNumber.Major >= 4)
                {
                    //                    maxLength = 15295;
                    maxLength = 14000;
                }

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                var putData = new byte[maxLength];
                rng.GetBytes(putData, offset: 0, putData.Length);

                pivSession.WriteMsroots(putData);

                pivSession.WriteMsroots(ReadOnlySpan<byte>.Empty);

                rng.GetBytes(putData, offset: 0, putData.Length);
                var memStream = new MemoryStream(putData);
                pivSession.WriteMsrootsStream(memStream);

                rng.GetBytes(putData, offset: 0, putData.Length);
                memStream = new MemoryStream(putData);
                pivSession.WriteMsrootsStream(memStream);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteReadMsroots_ByteArray(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var rng = RandomObjectUtility.GetRandomObject(fixedBytes: null);

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.DeleteMsroots();

                var currentLength = 6000;
                var arbitraryData = new byte[currentLength];
                rng.GetBytes(arbitraryData, offset: 0, arbitraryData.Length);

                pivSession.WriteMsroots(arbitraryData);

                var getData = pivSession.ReadMsroots();
                Assert.True(getData.Length == currentLength);

                var compareResult = getData.SequenceEqual(arbitraryData);

                Assert.True(compareResult);

                pivSession.DeleteMsroots();
                getData = pivSession.ReadMsroots();
                Assert.True(getData.Length == 0);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteReadMsroots_Stream(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var rng = RandomObjectUtility.GetRandomObject(fixedBytes: null);

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.DeleteMsroots();

                var currentLength = 6000;
                var arbitraryData = new byte[currentLength];
                rng.GetBytes(arbitraryData, offset: 0, arbitraryData.Length);

                pivSession.WriteMsroots(arbitraryData);

                var getData = pivSession.ReadMsrootsStream();
                var binReader = new BinaryReader(getData);
                var theData = binReader.ReadBytes((int)getData.Length);
                Assert.True(theData.Length == currentLength);

                var compareResult = theData.SequenceEqual(arbitraryData);

                Assert.True(compareResult);

                pivSession.DeleteMsroots();
                getData = pivSession.ReadMsrootsStream();
                Assert.True(getData.Length == 0);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteMsroots_Commands(StandardTestDevice testDeviceType)
        {
            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using var rng = RandomObjectUtility.GetRandomObject(fixedBytes: null);

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                for (var bufferSize = 2806; bufferSize <= 2808; bufferSize++)
                {
                    _output.WriteLine("buffer size: {0}", bufferSize);

                    pivSession.ResetApplication();

                    var isValid = pivSession.TryAuthenticateManagementKey();
                    Assert.True(isValid);

                    var putData = new byte[bufferSize];
                    rng.GetBytes(putData, offset: 0, putData.Length);
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

                    isValid = DoWriteAndWrite(pivSession, putData);
                    Assert.True(isValid);
                }
            }
        }

        private static bool DoWriteAndWrite(PivSession pivSession, byte[] putData)
        {
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = pivSession.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            byte[] noData = { 0x53, 0x00 };
            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, noData);
                var putResponse = pivSession.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            for (var index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                var putResponse = pivSession.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            return true;
        }
    }
}

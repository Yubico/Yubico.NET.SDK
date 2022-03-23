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
using System.Security.Cryptography;
using Yubico.YubiKey.Piv.Commands;
using Yubico.YubiKey.TestUtilities;
using Xunit;
using Xunit.Abstractions;

namespace Yubico.YubiKey.Piv
{
    public class MsrootsTests
    {
        private readonly ITestOutputHelper _output;

        public MsrootsTests (ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void SimplePutDataCommand(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;
                bool isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                pivSession.ResetApplication();

                isValid = pivSession.TryAuthenticateManagementKey();
                Assert.True(isValid);

                byte[] putData = new byte[] { 0x53, 0x04, 0x11, 0x22, 0x33, 0x44 };
                for (int index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }

                putData = new byte[] { 0x53, 0x00 };
                for (int index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }

                putData = new byte[] { 0x53, 0x05, 0x11, 0x22, 0x33, 0x44, 0x55 };
                for (int index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }

                putData = new byte[] { 0x53, 0x06, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
                for (int index = 0; index < 5; index++)
                {
                    var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                    PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                    Assert.Equal(ResponseStatus.Success, putResponse.Status);
                }
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteDataSession(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using (var pivSession = new PivSession(testDevice))
            {
                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                byte[] putData = new byte[] { 0x11, 0x22, 0x33, 0x44 };
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
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);

            using (var pivSession = new PivSession(testDevice))
            {
                var versionCommand = new VersionCommand();
                VersionResponse versionResponse = pivSession.Connection.SendCommand(versionCommand);
                Assert.Equal(ResponseStatus.Success, versionResponse.Status);

                FirmwareVersion versionNumber = versionResponse.GetData();

//                int maxLength = 10175;
                int maxLength = 10150;
                if (versionNumber.Major >= 4)
                {
//                    maxLength = 15295;
                    maxLength = 14000;
                }

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.ResetApplication();

                byte[] putData = new byte[maxLength];
                rng.GetBytes(putData, 0, putData.Length);

                pivSession.WriteMsroots(putData);

                pivSession.WriteMsroots(ReadOnlySpan<byte>.Empty);

                rng.GetBytes(putData, 0, putData.Length);
                var memStream = new MemoryStream(putData);
                pivSession.WriteMsrootsStream(memStream);

                rng.GetBytes(putData, 0, putData.Length);
                memStream = new MemoryStream(putData);
                pivSession.WriteMsrootsStream(memStream);
            }
        }

        [Theory]
        [InlineData(StandardTestDevice.Fw5)]
        public void WriteReadMsroots_ByteArray(StandardTestDevice testDeviceType)
        {
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.DeleteMsroots();

                int currentLength = 6000;
                byte[] arbitraryData = new byte[currentLength];
                rng.GetBytes(arbitraryData, 0, arbitraryData.Length);

                pivSession.WriteMsroots(arbitraryData);

                byte[] getData = pivSession.ReadMsroots();
                Assert.True(getData.Length == currentLength);

                bool compareResult = getData.SequenceEqual(arbitraryData);

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
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                pivSession.DeleteMsroots();

                int currentLength = 6000;
                byte[] arbitraryData = new byte[currentLength];
                rng.GetBytes(arbitraryData, 0, arbitraryData.Length);

                pivSession.WriteMsroots(arbitraryData);

                Stream getData = pivSession.ReadMsrootsStream();
                var binReader = new BinaryReader(getData);
                byte[] theData = binReader.ReadBytes((int)getData.Length);
                Assert.True(theData.Length == currentLength);

                bool compareResult = theData.SequenceEqual(arbitraryData);

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
            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);

            Assert.True(testDevice.AvailableUsbCapabilities.HasFlag(YubiKeyCapabilities.Piv));

            using RandomNumberGenerator rng = RandomObjectUtility.GetRandomObject(null);

            using (var pivSession = new PivSession(testDevice))
            {
                Assert.NotNull(pivSession.Connection);

                var collectorObj = new Simple39KeyCollector();
                pivSession.KeyCollector = collectorObj.Simple39KeyCollectorDelegate;

                for (int bufferSize = 2806; bufferSize <= 2808; bufferSize++)
                {
                    _output.WriteLine ("buffer size: {0}", bufferSize);

                    pivSession.ResetApplication();

                    bool isValid = pivSession.TryAuthenticateManagementKey();
                    Assert.True(isValid);

                    byte[] putData = new byte[bufferSize];
                    rng.GetBytes(putData, 0, putData.Length);
                    int dataLength = bufferSize - 4;
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
            for (int index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            byte[] noData = new byte[] { 0x53, 0x00 };
            for (int index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, noData);
                PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            for (int index = 0; index < 5; index++)
            {
                var putCommand = new PutDataCommand(0x005fff11 + index, putData);
                PutDataResponse putResponse = pivSession.Connection.SendCommand(putCommand);
                Assert.Equal(ResponseStatus.Success, putResponse.Status);
            }

            return true;
        }
    }
}

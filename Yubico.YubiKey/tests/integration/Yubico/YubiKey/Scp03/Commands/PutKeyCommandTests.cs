// Copyright 2023 Yubico AB
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
using Xunit;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Scp03.Commands
{
    public class PutKeyCommandTests
    {
        // These may require that DeleteKeyCommandTests have been run first.
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips)]
        [InlineData(StandardTestDevice.Fw5)]
        public void ChangeDefaultKey_Succeeds(StandardTestDevice testDeviceType)
        {
            byte[] key1 =
            {
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff
            };
            byte[] key2 =
            {
                0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff, 0x11
            };
            byte[] key3 =
            {
                0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff, 0x11, 0x22
            };

            var currentKeys = new StaticKeys();
            var newKeys = new StaticKeys(key2, key1, key3);

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var isValid = testDevice.TryConnectScp03(YubiKeyApplication.Scp03, currentKeys,
                out var connection);

            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new PutKeyCommand(currentKeys, newKeys);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            var checksum = rsp.GetData();
            var isEqual = checksum.Span.SequenceEqual(cmd.ExpectedChecksum.Span);
            Assert.True(isEqual);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddNewKeySet_Succeeds(StandardTestDevice testDeviceType)
        {
            byte[] key1 =
            {
                0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff
            };
            byte[] key2 =
            {
                0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff, 0x11
            };
            byte[] key3 =
            {
                0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff, 0x11, 0x22
            };
            byte[] newKey1 =
            {
                0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee
            };
            byte[] newKey2 =
            {
                0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff, 0x11
            };
            byte[] newKey3 =
            {
                0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11, 0x22
            };

            var currentKeys = new StaticKeys(key2, key1, key3);
            var newKeys = new StaticKeys(newKey2, newKey1, newKey3)
            {
                KeyVersionNumber = 2
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var isValid = testDevice.TryConnectScp03(YubiKeyApplication.Scp03, currentKeys,
                out var connection);

            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new PutKeyCommand(currentKeys, newKeys);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            var checksum = rsp.GetData();
            var isEqual = checksum.Span.SequenceEqual(cmd.ExpectedChecksum.Span);
            Assert.True(isEqual);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips)]
        [InlineData(StandardTestDevice.Fw5)]
        public void AddThirdKeySet_Succeeds(StandardTestDevice testDeviceType)
        {
            byte[] key1 =
            {
                0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xee
            };
            byte[] key2 =
            {
                0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff, 0x11
            };
            byte[] key3 =
            {
                0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11, 0x22
            };
            byte[] newKey1 =
            {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd
            };
            byte[] newKey2 =
            {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff
            };
            byte[] newKey3 =
            {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11
            };

            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 2
            };
            var newKeys = new StaticKeys(newKey2, newKey1, newKey3)
            {
                KeyVersionNumber = 3
            };

            var testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var isValid = testDevice.TryConnectScp03(YubiKeyApplication.Scp03, currentKeys,
                out var connection);

            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new PutKeyCommand(currentKeys, newKeys);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
            var checksum = rsp.GetData();
            var isEqual = checksum.Span.SequenceEqual(cmd.ExpectedChecksum.Span);
            Assert.True(isEqual);
        }
    }
}

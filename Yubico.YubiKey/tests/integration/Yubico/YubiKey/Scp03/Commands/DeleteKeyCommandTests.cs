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
using Yubico.YubiKey.TestUtilities;
using Xunit;

namespace Yubico.YubiKey.Scp03.Commands
{
    public class DeleteKeyCommandTests
    {
        [Fact]
        public void DeleteKey_One_Succeeds()
        {
            byte[] applicationId = new byte[] {
                0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00
            };

            byte[] key1 = new byte[] {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            };
            byte[] key2 = new byte[] {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff,
            };
            byte[] key3 = new byte[] {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11,
            };
            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice(currentKeys);

            bool isValid = testDevice.TryConnect(applicationId, out IYubiKeyConnection? connection);
            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new DeleteKeyCommand(1, false);
            Scp03Response rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void DeleteKey_Two_Succeeds()
        {
            byte[] applicationId = new byte[] {
                0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00
            };

            byte[] key1 = new byte[] {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            };
            byte[] key2 = new byte[] {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff,
            };
            byte[] key3 = new byte[] {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11,
            };
            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice(currentKeys);

            bool isValid = testDevice.TryConnect(applicationId, out IYubiKeyConnection? connection);
            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new DeleteKeyCommand(2, false);
            Scp03Response rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [Fact]
        public void DeleteKey_Three_Succeeds()
        {
            byte[] applicationId = new byte[] {
                0xA0, 0x00, 0x00, 0x01, 0x51, 0x00, 0x00, 0x00
            };

            byte[] key1 = new byte[] {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            };
            byte[] key2 = new byte[] {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff,
            };
            byte[] key3 = new byte[] {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11,
            };
            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetScp03TestDevice(currentKeys);

            bool isValid = testDevice.TryConnect(applicationId, out IYubiKeyConnection? connection);
            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new DeleteKeyCommand(3, true);
            Scp03Response rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }
    }
}

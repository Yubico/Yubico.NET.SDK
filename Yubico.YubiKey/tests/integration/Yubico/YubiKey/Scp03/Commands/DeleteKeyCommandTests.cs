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
using Yubico.YubiKey.Scp;
using Yubico.YubiKey.Scp03;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Scp.Commands
{
    [Trait(TraitTypes.Category, TestCategories.Simple)]
    public class DeleteKeyCommandTests
    {
        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips)]
        [InlineData(StandardTestDevice.Fw5)]
        [Obsolete("Obsolete")]
        public void DeleteKey_One_Succeeds(StandardTestDevice testDeviceType)
        {
            byte[] key1 = {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            };
            byte[] key2 = {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff,
            };
            byte[] key3 = {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11,
            };
            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            var isValid = testDevice.TryConnectScp03(YubiKeyApplication.Scp03, currentKeys, out IScp03YubiKeyConnection? connection);
            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new Scp03.Commands.DeleteKeyCommand(1, false);
            Scp03.Commands.Scp03Response rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips)]
        [InlineData(StandardTestDevice.Fw5)]
        public void DeleteKey_Two_Succeeds(StandardTestDevice testDeviceType)
        {
            byte[] key1 = {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            };
            byte[] key2 = {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff,
            };
            byte[] key3 = {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11,
            };
            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            //TODO 
#pragma warning disable CS0618 // Type or member is obsolete
            var isValid = testDevice.TryConnectScp03(YubiKeyApplication.Scp03, currentKeys, out IScp03YubiKeyConnection? connection);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new Scp.Commands.DeleteKeyCommand(2, false);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }

        [SkippableTheory(typeof(DeviceNotFoundException))]
        [InlineData(StandardTestDevice.Fw5Fips)]
        [InlineData(StandardTestDevice.Fw5)]
        public void DeleteKey_Three_Succeeds(StandardTestDevice testDeviceType)
        {
            byte[] key1 = {
                0x33, 0xff, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd,
            };
            byte[] key2 = {
                0x33, 0xee, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xdd, 0xff,
            };
            byte[] key3 = {
                0x33, 0xdd, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xaa, 0xbb, 0xcc, 0xee, 0xff, 0x11,
            };
            var currentKeys = new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };

            IYubiKeyDevice testDevice = IntegrationTestDeviceEnumeration.GetTestDevice(testDeviceType);
            //TODO 
#pragma warning disable CS0618 // Type or member is obsolete
            var isValid = testDevice.TryConnectScp03(YubiKeyApplication.Scp03, currentKeys, out IScp03YubiKeyConnection? connection);
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.True(isValid);
            Assert.NotNull(connection);

            var cmd = new DeleteKeyCommand(3, true);
            var rsp = connection!.SendCommand(cmd);
            Assert.Equal(ResponseStatus.Success, rsp.Status);
        }
    }
}

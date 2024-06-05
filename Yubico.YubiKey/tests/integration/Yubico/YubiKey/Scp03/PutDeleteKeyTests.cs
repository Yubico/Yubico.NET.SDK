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

namespace Yubico.YubiKey.Scp03
{
    // Todo these tests seem to require a Fips key as well. And that DeleteKeyCommandTEsts have run
    [TestCaseOrderer(PriorityOrderer.TypeName, PriorityOrderer.AssembyName)]
    [Trait("Category", "Simple")]
    public class PutDeleteTests
    {
        [Fact]
        [TestPriority(3)]
        public void PutKey_Succeeds()
        {
            using var staticKeys = new StaticKeys();
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard,
                minimumFirmwareVersion: FirmwareVersion.V5_3_0);

            using (var scp03Session = new Scp03Session(device, staticKeys))
            {
                using StaticKeys newKeys = GetKeySet(1);
                scp03Session.PutKeySet(newKeys);

                using StaticKeys nextKeys = GetKeySet(2);
                scp03Session.PutKeySet(nextKeys);
            }

            using StaticKeys keySet2 = GetKeySet(2);

            using (var scp03Session = new Scp03Session(device, keySet2))
            {
                using StaticKeys keySet3 = GetKeySet(3);
                scp03Session.PutKeySet(keySet3);
            }
        }

        [Fact]
        [TestPriority(3)]
        public void ReplaceKey_Succeeds()
        {
            using StaticKeys staticKeys = GetKeySet(2);
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard,
                FirmwareVersion.V5_3_0);

            using (var scp03Session = new Scp03Session(device, staticKeys))
            {
                using StaticKeys newKeys = GetKeySet(1);
                newKeys.KeyVersionNumber = 2;
                scp03Session.PutKeySet(newKeys);
            }
        }

        [Fact]
        [TestPriority(0)]
        public void DeleteKey_Succeeds()
        {
            using StaticKeys staticKeys = GetKeySet(3);
            IYubiKeyDevice device = IntegrationTestDeviceEnumeration.GetTestDevice(
                Transport.SmartCard,
                FirmwareVersion.V5_3_0);

            using var scp03Session = new Scp03Session(device, staticKeys);
            scp03Session.DeleteKeySet(1);
            scp03Session.DeleteKeySet(2);

            scp03Session.DeleteKeySet(3, true);
        }

        // The setNumber is to be 1, 2, or 3
        private StaticKeys GetKeySet(int setNumber) => setNumber switch
        {
            1 => GetKeySet1(),
            2 => GetKeySet2(),
            _ => GetKeySet3()
        };

        private StaticKeys GetKeySet1()
        {
            var key1 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x11, 0x11, 0x11, 0x11, 0x49, 0x2f, 0x4d, 0x09, 0x22, 0xec, 0x3d, 0xb4, 0x6b, 0x20, 0x94, 0x7a
            });
            var key2 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x12, 0x12, 0x12, 0x12, 0x53, 0xB3, 0xE3, 0x78, 0x2A, 0x1D, 0xE5, 0xDC, 0x5A, 0xF4, 0xa6, 0x41
            });
            var key3 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x13, 0x13, 0x13, 0x13, 0x68, 0xDE, 0x7A, 0xB7, 0x74, 0x19, 0xBB, 0x7F, 0xB0, 0x55, 0x7d, 0x40
            });

            return new StaticKeys(key2, key1, key3);
        }

        private StaticKeys GetKeySet2()
        {
            var key1 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x21, 0x21, 0x21, 0x21, 0x20, 0x94, 0x7a, 0x49, 0x2f, 0x4d, 0x09, 0x22, 0xec, 0x3d, 0xb4, 0x6b
            });
            var key2 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x22, 0x22, 0x22, 0x22, 0xDC, 0x5A, 0xF4, 0xa6, 0x41, 0x53, 0xB3, 0xE3, 0x78, 0x2A, 0x1D, 0xE5
            });
            var key3 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x23, 0x23, 0x23, 0x23, 0x7d, 0x40, 0x68, 0xDE, 0x7A, 0xB7, 0x74, 0x19, 0xBB, 0x7F, 0xB0, 0x55
            });

            return new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 2
            };
        }

        private StaticKeys GetKeySet3()
        {
            var key1 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x21, 0x21, 0x21, 0x21, 0x20, 0xDC, 0x5A, 0xF4, 0xa6, 0x41, 0x94, 0x7a, 0x49, 0x2f, 0x4d, 0x09
            });
            var key2 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x22, 0x22, 0x22, 0x22, 0x22, 0xec, 0x3d, 0xb4, 0x6b, 0x53, 0xB3, 0xE3, 0x78, 0x2A, 0x1D, 0xE5
            });
            var key3 = new ReadOnlyMemory<byte>(new byte[]
            {
                0x23, 0x23, 0x23, 0x23, 0x7A, 0xB7, 0x74, 0x19, 0x7d, 0x40, 0x68, 0xDE, 0xBB, 0x7F, 0xB0, 0x55
            });

            return new StaticKeys(key2, key1, key3)
            {
                KeyVersionNumber = 3
            };
        }
    }
}

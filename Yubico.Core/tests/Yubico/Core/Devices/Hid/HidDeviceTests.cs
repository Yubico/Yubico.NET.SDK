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
using Xunit;

namespace Yubico.Core.Devices.Hid.UnitTests
{

#if false
    public class HidDeviceTests
    {
        [Fact]
        public void Constructor_GivenEmptyArguments_Succeeds()
        {
            _ = new HidDevice("", Guid.Empty, 0, 0);
        }

        [Fact]
        public void Constructor_GivenEmptyArguments_SetsUserMatchingHintTo0()
        {
            var hd = new HidDevice("", Guid.Empty, 0, 0);

            Assert.Equal(0, hd.UserMatchingHint);
        }

        [Fact]
        public void Constructor_GivenPath_SetsPath()
        {
            var hd = new HidDevice("xyz", Guid.Empty, 0, 0);

            Assert.Equal("xyz", hd.Path);
        }

        [Fact]
        public void Constructor_GivenContainerId_SetsPlatformMatchingHint()
        {
            var guid = new Guid(0x4d36e96b, 0xe325, 0x11ce, 0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18);

            var hd = new HidDevice("", guid, 0, 0);

            Assert.Equal(guid, hd.PlatformMatchingHint);
        }

        [Fact]
        public void Constructor_GivenStructuredPath_SetsVID()
        {
            var hd = new HidDevice("\\\\?\\HID#VID_1050&PID_0407&MI_00#7", Guid.Empty, 0, 0);

            Assert.Equal(0x1050, hd.VendorId);
        }

        [Fact]
        public void Constructor_GivenStructuredPath_SetsPID()
        {
            var hd = new HidDevice("\\\\?\\HID#VID_1050&PID_0407&MI_00#7", Guid.Empty, 0, 0);

            Assert.Equal(0x0407, hd.ProductId);
        }

        [Fact]
        public void Constructor_GivenUnstructuredPath_SetsVIDTo0()
        {
            var hd = new HidDevice("this is bad", Guid.Empty, 0, 0);

            Assert.Equal(0, hd.VendorId);
        }

        [Fact]
        public void Constructor_GivenUnstructuredPath_SetsPIDTo0()
        {
            var hd = new HidDevice("this is bad", Guid.Empty, 0, 0);

            Assert.Equal(0, hd.ProductId);
        }
    }
#endif
}

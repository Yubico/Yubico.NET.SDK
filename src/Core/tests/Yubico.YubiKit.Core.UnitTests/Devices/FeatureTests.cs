// Copyright 2026 Yubico AB
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

using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class FeatureTests
{
    [Fact]
    public void IsSupportedByFirmware_WhenFirmwareIsBelowFeatureVersion_ReturnsFalse()
    {
        var feature = new Feature("Test feature", 5, 8, 0);

        Assert.False(feature.IsSupportedByFirmware(new FirmwareVersion(5, 7, 9)));
    }

    [Fact]
    public void IsSupportedByFirmware_WhenFirmwareMeetsFeatureVersion_ReturnsTrue()
    {
        var feature = new Feature("Test feature", 5, 8, 0);

        Assert.True(feature.IsSupportedByFirmware(new FirmwareVersion(5, 8, 0)));
        Assert.True(feature.IsSupportedByFirmware(new FirmwareVersion(5, 8, 1)));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 255, 255)]
    public void IsSupportedByFirmware_WhenFirmwareIsSentinel_ReturnsTrue(int major, int minor, int patch)
    {
        var feature = new Feature("Test feature", 5, 8, 0);

        Assert.True(feature.IsSupportedByFirmware(new FirmwareVersion(major, minor, patch)));
    }
}
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

using System.Text;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Core.UnitTests.Devices;

public class FirmwareVersionSelectResponseTests
{
    [Theory]
    [InlineData("5.7.2", 5, 7, 2)]
    [InlineData("Firmware version 5.4.3", 5, 4, 3)]
    [InlineData("U2F_V2 1.0.1 beta", 1, 0, 1)]
    public void FromSelectResponse_WellFormed_ParsesVersion(string response, byte major, byte minor, byte patch)
    {
        FirmwareVersion? version = FirmwareVersion.FromSelectResponse(Encoding.UTF8.GetBytes(response));

        Assert.Equal(new FirmwareVersion(major, minor, patch), version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("YubiKey")]
    [InlineData("5.4")]
    [InlineData("5.4.x")]
    [InlineData("999.0.0")]
    [InlineData("5.4.3.2")]
    public void FromSelectResponse_Malformed_ReturnsNull(string response)
    {
        FirmwareVersion? version = FirmwareVersion.FromSelectResponse(Encoding.UTF8.GetBytes(response));

        Assert.Null(version);
    }

    [Fact]
    public void FromSelectResponse_NonUtf8Bytes_ReturnsNull()
    {
        FirmwareVersion? version = FirmwareVersion.FromSelectResponse([0xff, 0xfe, 0x20]);

        Assert.Null(version);
    }
}

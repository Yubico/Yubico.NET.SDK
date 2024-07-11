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
using System.Collections.Generic;
using Xunit;
using Yubico.Core.Buffers;

namespace Yubico.YubiKey
{
    public class YubikeyDeviceInfoTests
    {
        [Theory]
        [InlineData(YubiKeyCapabilities.None, "0000")]
        [InlineData(YubiKeyCapabilities.Fido2, "0001")]
        [InlineData(YubiKeyCapabilities.Piv, "0002")]
        [InlineData(YubiKeyCapabilities.OpenPgp, "0004")]
        [InlineData(YubiKeyCapabilities.Oath, "0008")]
        [InlineData(YubiKeyCapabilities.YubiHsmAuth, "0010")]
        [InlineData(YubiKeyCapabilities.Piv | YubiKeyCapabilities.Oath, "000A")]
        public void CreateFromResponseData_Returns_ExpectedFipsCapable(
            YubiKeyCapabilities expected, string? data = null)
        {
            const int fipsCapableTag = 0x14;
            Assert.Equal(expected, DeviceInfoFor(fipsCapableTag, FromHex(data)).FipsCapable);
        }

        [Theory]
        [InlineData(YubiKeyCapabilities.None, "0000")]
        [InlineData(YubiKeyCapabilities.Fido2, "0001")]
        [InlineData(YubiKeyCapabilities.Piv, "0002")]
        [InlineData(YubiKeyCapabilities.OpenPgp, "0004")]
        [InlineData(YubiKeyCapabilities.Oath, "0008")]
        [InlineData(YubiKeyCapabilities.YubiHsmAuth, "0010")]
        [InlineData(YubiKeyCapabilities.Piv | YubiKeyCapabilities.Oath, "000A")]
        public void CreateFromResponseData_Returns_ExpectedFipsApproved(
            YubiKeyCapabilities expected, string? data = null)
        {
            const int fipsApprovedTag = 0x15;
            Assert.Equal(expected, DeviceInfoFor(fipsApprovedTag, FromHex(data)).FipsApproved);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedSerialNumber()
        {
            const int serialNumberTag = 0x02;
            Assert.Null(DeviceInfoFor(serialNumberTag).SerialNumber);
            Assert.Equal(123456789, DeviceInfoFor(serialNumberTag, FromHex("075BCD15")).SerialNumber);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedFirmwareVersion()
        {
            Assert.Equal(new FirmwareVersion(5, 3, 4), DeviceInfoFor(0x05, FromHex("050304")).FirmwareVersion);
        }

        [Theory]
        [InlineData(FormFactor.Unknown)]
        [InlineData(FormFactor.UsbAKeychain, "01")]
        [InlineData(FormFactor.UsbANano, "02")]
        [InlineData(FormFactor.UsbCKeychain, "03")]
        [InlineData(FormFactor.UsbCNano, "04")]
        [InlineData(FormFactor.UsbCLightning, "05")]
        [InlineData(FormFactor.UsbABiometricKeychain, "06")]
        [InlineData(FormFactor.UsbCBiometricKeychain, "07")]
        [InlineData(FormFactor.UsbABiometricKeychain, "46")]
        [InlineData(FormFactor.UsbCNano, "84")]
        public void CreateFromResponseData_WithDifferentFormFactor_Returns_ExpectedFormFactor(
            FormFactor expected,
            string? data = null)
        {
            const int formFactorTag = 0x04;
            Assert.Equal(expected, DeviceInfoFor(formFactorTag, FromHex(data)).FormFactor);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedConfigurationLocked()
        {
            const int configurationLockedTag = 0x0a;
            Assert.False(DeviceInfoFor(configurationLockedTag).ConfigurationLocked);
            Assert.True(DeviceInfoFor(configurationLockedTag, FromHex("01")).ConfigurationLocked);
            Assert.False(DeviceInfoFor(configurationLockedTag, FromHex("00")).ConfigurationLocked);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedFipsSeries()
        {
            const int formFactorTag = 0x04;
            Assert.False(DeviceInfoFor(formFactorTag).IsFipsSeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("80"), FirmwareVersion.V5_4_2).IsFipsSeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("C0"), FirmwareVersion.V5_4_2).IsFipsSeries);
            Assert.False(DeviceInfoFor(formFactorTag, FromHex("40"), FirmwareVersion.V5_4_2).IsFipsSeries);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedIsSkySeries()
        {
            const int formFactorTag = 0x04;
            Assert.False(DeviceInfoFor(formFactorTag).IsSkySeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("40")).IsSkySeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("C0")).IsSkySeries);
            Assert.False(DeviceInfoFor(formFactorTag, FromHex("80")).IsSkySeries);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedPartNumber()
        {
            const int partNumberTag = 0x13;

            // Valid UTF-8
            Assert.Null(DeviceInfoFor(partNumberTag).PartNumber);
            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_=+-",
                DeviceInfoFor(partNumberTag,
                        FromHex(
                            "6162636465666768696A6B6C6D6E6F707172737475767778797A4142434445464748494A4B4C4D4E4F505152535455565758595A303132333435363738395F3D2B2D"))
                    .PartNumber);
            Assert.Equal("ÖÄÅöäåěščřžýáíúůĚŠČŘŽÝÁÍÚŮ",
                DeviceInfoFor(partNumberTag,
                        FromHex(
                            "C396C384C385C3B6C3A4C3A5C49BC5A1C48DC599C5BEC3BDC3A1C3ADC3BAC5AFC49AC5A0C48CC598C5BDC39DC381C38DC39AC5AE"))
                    .PartNumber);
            Assert.Equal("😀", DeviceInfoFor(partNumberTag, FromHex("F09F9880")).PartNumber);
            Assert.Equal("0123456789ABCDEF",
                DeviceInfoFor(partNumberTag, FromHex("30313233343536373839414243444546")).PartNumber);

            // Invalid UTF-8
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("c328")).PartNumber);
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("a0a1")).PartNumber);
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("e228a1")).PartNumber);
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("e28228")).PartNumber);
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("f0288cbc")).PartNumber);
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("f09028bc")).PartNumber);
            Assert.Null(DeviceInfoFor(partNumberTag, FromHex("f0288c28")).PartNumber);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedPinComplexity()
        {
            const int pinComplexityTag = 0x16;
            Assert.False(DeviceInfoFor(pinComplexityTag).IsPinComplexityEnabled);
            Assert.False(DeviceInfoFor(pinComplexityTag, FromHex("00")).IsPinComplexityEnabled);
            Assert.True(DeviceInfoFor(pinComplexityTag, FromHex("01")).IsPinComplexityEnabled);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedResetBlocked()
        {
            const int resetBlockedTag = 0x18;
            Assert.Equal(YubiKeyCapabilities.None, DeviceInfoFor(resetBlockedTag).ResetBlocked);
            Assert.Equal(YubiKeyCapabilities.Oath | YubiKeyCapabilities.Fido2,
                DeviceInfoFor(resetBlockedTag, FromHex("0220")).ResetBlocked);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedTemplateStorageVersion()
        {
            const int templateStorageVersionTag = 0x20;
            Assert.Null(DeviceInfoFor(templateStorageVersionTag).TemplateStorageVersion);
            Assert.Equal(new TemplateStorageVersion(5, 6, 6),
                DeviceInfoFor(templateStorageVersionTag, FromHex("050606")).TemplateStorageVersion);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedImageProcessorVersion()
        {
            const int imageProcessorVersionTag = 0x21;
            Assert.Null(DeviceInfoFor(imageProcessorVersionTag).ImageProcessorVersion);
            Assert.Equal(new ImageProcessorVersion(7, 0, 5),
                DeviceInfoFor(imageProcessorVersionTag, FromHex("070005")).ImageProcessorVersion);
        }

        private static YubiKeyDeviceInfo DeviceInfoFor(int tag, FirmwareVersion? version = null) =>
            DeviceInfoFor(tag, Array.Empty<byte>());

        private static YubiKeyDeviceInfo DeviceInfoFor(int tag, byte[] data, FirmwareVersion? version = null)
        {
            byte[] versionAsBytes = version is { }
                ? VersionToBytes(version)
                : VersionToBytes(FirmwareVersion.V2_2_0);

            var tlvs = new Dictionary<int, ReadOnlyMemory<byte>> { { tag, data } };
            const int versionTag = 0x5;
            if (tag != versionTag)
            {
                tlvs.Add(versionTag, versionAsBytes);
            }

            YubiKeyDeviceInfo info = data.Length == 0
                ? new YubiKeyDeviceInfo()
                : YubiKeyDeviceInfo.CreateFromResponseData(tlvs); //We're testing this method

            return info;
        }

        private static byte[] VersionToBytes(FirmwareVersion version) =>
            new[] { version.Major, version.Minor, version.Patch };

        private static byte[] FromHex(string? hex) => hex != null ? Base16.DecodeText(hex) : Array.Empty<byte>();
    }
}

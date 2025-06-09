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
using Yubico.Core.Tlv;

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
            Assert.Null(DefaultInfo().SerialNumber);
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
            Assert.False(DefaultInfo().ConfigurationLocked);
            Assert.True(DeviceInfoFor(configurationLockedTag, FromHex("01")).ConfigurationLocked);
            Assert.False(DeviceInfoFor(configurationLockedTag, FromHex("00")).ConfigurationLocked);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedFipsSeries()
        {
            const int formFactorTag = 0x04;
            Assert.False(DefaultInfo().IsFipsSeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("80"), FirmwareVersion.V5_4_2).IsFipsSeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("C0"), FirmwareVersion.V5_4_2).IsFipsSeries);
            Assert.False(DeviceInfoFor(formFactorTag, FromHex("40"), FirmwareVersion.V5_4_2).IsFipsSeries);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedIsSkySeries()
        {
            const int formFactorTag = 0x04;
            Assert.False(DefaultInfo().IsSkySeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("40")).IsSkySeries);
            Assert.True(DeviceInfoFor(formFactorTag, FromHex("C0")).IsSkySeries);
            Assert.False(DeviceInfoFor(formFactorTag, FromHex("80")).IsSkySeries);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedPartNumber()
        {
            const int partNumberTag = 0x13;

            // Valid UTF-8
            Assert.Null(DefaultInfo().PartNumber);
            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_=+-", DeviceInfoFor(partNumberTag, FromHex("6162636465666768696A6B6C6D6E6F707172737475767778797A4142434445464748494A4B4C4D4E4F505152535455565758595A303132333435363738395F3D2B2D")).PartNumber);
            Assert.Equal("ÖÄÅöäåěščřžýáíúůĚŠČŘŽÝÁÍÚŮ", DeviceInfoFor(partNumberTag, FromHex("C396C384C385C3B6C3A4C3A5C49BC5A1C48DC599C5BEC3BDC3A1C3ADC3BAC5AFC49AC5A0C48CC598C5BDC39DC381C38DC39AC5AE")).PartNumber);
            Assert.Equal("😀", DeviceInfoFor(partNumberTag, FromHex("F09F9880")).PartNumber);
            Assert.Equal("0123456789ABCDEF", DeviceInfoFor(partNumberTag, FromHex("30313233343536373839414243444546")).PartNumber);

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
            Assert.False(DefaultInfo().IsPinComplexityEnabled);
            Assert.False(DeviceInfoFor(pinComplexityTag, FromHex("00")).IsPinComplexityEnabled);
            Assert.True(DeviceInfoFor(pinComplexityTag, FromHex("01")).IsPinComplexityEnabled);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedResetBlocked()
        {
            const int resetBlockedTag = 0x18;
            Assert.Equal(YubiKeyCapabilities.None, DefaultInfo().ResetBlocked);
            Assert.Equal(YubiKeyCapabilities.Oath | YubiKeyCapabilities.Fido2,
                DeviceInfoFor(resetBlockedTag, FromHex("0220")).ResetBlocked);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedTemplateStorageVersion()
        {
            const int templateStorageVersionTag = 0x20;
            Assert.Null(DefaultInfo().TemplateStorageVersion);
            Assert.Equal(new TemplateStorageVersion(5, 6, 6),
                DeviceInfoFor(templateStorageVersionTag, FromHex("050606")).TemplateStorageVersion);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedImageProcessorVersion()
        {
            const int imageProcessorVersionTag = 0x21;
            Assert.Null(DefaultInfo().ImageProcessorVersion);
            Assert.Equal(new ImageProcessorVersion(7, 0, 5),
                DeviceInfoFor(imageProcessorVersionTag, FromHex("070005")).ImageProcessorVersion);
        }

        [Fact]
        public void CreateFromResponseData_WithEmptyData_SetsQualifierCorrectly()
        {
            var deviceInfo = DefaultInfo();
            Assert.Equal(DefaultVersionQualifier, deviceInfo.VersionQualifier);

            deviceInfo.VersionQualifier = new VersionQualifier(FirmwareVersion.V5_7_2, VersionQualifierType.Alpha, 15);
            deviceInfo.FirmwareVersion = FirmwareVersion.V3_1_0;

            Assert.Equal("5.7.2.alpha.15", deviceInfo.VersionQualifier.ToString());
            Assert.Equal("3.1.0", deviceInfo.FirmwareVersion.ToString());
        }

        [Fact]
        public void ParseVersionQualifier_VariousScenarios_ParsesCorrectly()
        {
            // Default version and qualifier
            var info = InfoOfVersion(null, null);
            Assert.Equal(DefaultVersion, info.FirmwareVersion);
            Assert.Equal(new VersionQualifier(DefaultVersion, VersionQualifierType.Final, 0), info.VersionQualifier);
            Assert.Equal("2.2.2", info.VersionName);

            // No qualifier provided
            info = InfoOfVersion(FromHex("030403"), null);
            Assert.Equal(new FirmwareVersion(3, 4, 3), info.FirmwareVersion);
            Assert.Equal(new VersionQualifier(new FirmwareVersion(3, 4, 3), VersionQualifierType.Final, 0), info.VersionQualifier);
            Assert.Equal("3.4.3", info.VersionName);

            // ALPHA version qualifier
            info = InfoOfVersion(FromHex("000001"), FromHex("0103050403020100030400000000"));
            Assert.Equal(new FirmwareVersion(5, 4, 3), info.FirmwareVersion);
            Assert.Equal("5.4.3.alpha.0", info.VersionQualifier.ToString());
            Assert.Equal("5.4.3.alpha.0", info.VersionName);

            // BETA version qualifier
            info = InfoOfVersion(FromHex("000001"), FromHex("01030507080201010304000000e9"));
            Assert.Equal(new FirmwareVersion(5, 7, 8), info.FirmwareVersion);
            Assert.Equal("5.7.8.beta.233", info.VersionQualifier.ToString());
            Assert.Equal("5.7.8.beta.233", info.VersionName);

            // FINAL version qualifier
            info = InfoOfVersion(FromHex("050404"), FromHex("0103050404020102030400000005"));
            Assert.Equal(new FirmwareVersion(5, 4, 4), info.FirmwareVersion);
            Assert.Equal("5.4.4.final.5", info.VersionQualifier.ToString());
            Assert.Equal("5.4.4", info.VersionName);

            info = InfoOfVersion(FromHex("050709"), FromHex("01030507090201020304FFFFFFFF"));
            Assert.Equal(new FirmwareVersion(5, 7, 9), info.FirmwareVersion);
            Assert.Equal("5.7.9.final.4294967295", info.VersionQualifier.ToString());
            Assert.Equal("5.7.9", info.VersionName);

            info = InfoOfVersion(FromHex("05070A"), FromHex("010305070A020102030480000000"));
            Assert.Equal(new FirmwareVersion(5, 7, 10), info.FirmwareVersion);
            Assert.Equal("5.7.10.final.2147483648", info.VersionQualifier.ToString());
            Assert.Equal("5.7.10", info.VersionName);

            info = InfoOfVersion(FromHex("05070B"), FromHex("010305070B02010203047FFFFFFF"));
            Assert.Equal(new FirmwareVersion(5, 7, 11), info.FirmwareVersion);
            Assert.Equal("5.7.11.final.2147483647", info.VersionQualifier.ToString());
            Assert.Equal("5.7.11", info.VersionName);
        }

        private static readonly FirmwareVersion DefaultVersion = new(2, 2, 2);
        private static readonly VersionQualifier DefaultVersionQualifier = new(new FirmwareVersion(), VersionQualifierType.Final, 0);

        private static YubiKeyDeviceInfo InfoOfVersion(byte[]? versionBytes, byte[]? qualifierBytes)
        {
            var tlvs = new Dictionary<int, ReadOnlyMemory<byte>>();

            if (versionBytes != null)
            {
                tlvs.Add(0x05, versionBytes);
            }
            else
            {
                tlvs.Add(0x05, new byte[] { 2, 2, 2 }); // Default version
            }

            if (qualifierBytes != null)
            {
                tlvs.Add(0x19, qualifierBytes);
            }


            return YubiKeyDeviceInfo.CreateFromResponseData(tlvs);
        }

        private static YubiKeyDeviceInfo DefaultInfo() => YubiKeyDeviceInfo.CreateFromResponseData([]);

        private static YubiKeyDeviceInfo DeviceInfoFor(int tag, byte[] deviceInfoData, FirmwareVersion? version = null)
        {
            if (deviceInfoData.Length == 0)
            {
                return YubiKeyDeviceInfo.CreateFromResponseData([]);
            }

            var tlvs = new Dictionary<int, ReadOnlyMemory<byte>> { { tag, deviceInfoData } };
            const byte versionTag = 0x5;
            if (tag == versionTag)
            {
                // No need to set version info as its already set
            }
            else
            {
                SetVersionTag(tag, version, tlvs);
            }


            return YubiKeyDeviceInfo.CreateFromResponseData(tlvs); //We're testing this method
        }

        private static void SetVersionTag(int tag, FirmwareVersion? version, Dictionary<int, ReadOnlyMemory<byte>> tlvs)
        {
            byte[] versionAsBytes = version is not null
                ? VersionToBytes(version)
                : VersionToBytes(FirmwareVersion.V2_2_0);

            const byte versionTag = 0x5;
            if (tag != versionTag)
            {
                tlvs.Add(versionTag, versionAsBytes);
            }
        }

        private static byte[] VersionToBytes(FirmwareVersion version) => [version.Major, version.Minor, version.Patch];

        private static byte[] FromHex(string? hex) => hex != null ? Base16.DecodeText(hex) : Array.Empty<byte>();
    }
}

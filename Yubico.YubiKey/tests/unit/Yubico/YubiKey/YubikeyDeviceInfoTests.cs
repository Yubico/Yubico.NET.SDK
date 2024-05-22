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
        public void CreateFromResponseData_Returns_ExpectedFipsCapable(YubiKeyCapabilities expected, string? data = null) 
            => Assert.Equal(expected, WithDeviceInfo(0x14, FromHex(data)).FipsCapable);

        [Theory]
        [InlineData(YubiKeyCapabilities.None, "0000")]
        [InlineData(YubiKeyCapabilities.Fido2, "0001")]
        [InlineData(YubiKeyCapabilities.Piv, "0002")]
        [InlineData(YubiKeyCapabilities.OpenPgp, "0004")]
        [InlineData(YubiKeyCapabilities.Oath, "0008")]
        [InlineData(YubiKeyCapabilities.YubiHsmAuth, "0010")]
        [InlineData(YubiKeyCapabilities.Piv | YubiKeyCapabilities.Oath, "000A")]
        public void CreateFromResponseData_Returns_ExpectedFipsApproved(YubiKeyCapabilities expected, string? data = null)
            => Assert.Equal(expected, WithDeviceInfo(0x15, FromHex(data)).FipsApproved);

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedSerialNumber()
        {
            Assert.Null(DefaultInfo.SerialNumber);
            Assert.Equal(123456789, WithDeviceInfo(0x02, FromHex("075BCD15")).SerialNumber);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedFirmwareVersion()
        {
            Assert.Equal(new FirmwareVersion(5, 3, 4), WithDeviceInfo(0x05, FromHex("050304")).FirmwareVersion);
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
            string? data = null) =>
            Assert.Equal(expected, WithDeviceInfo(0x04, FromHex(data)).FormFactor);

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedConfigurationLocked()
        {
            Assert.False(DefaultInfo.ConfigurationLocked);
            Assert.True(WithDeviceInfo(0x0a, FromHex("01")).ConfigurationLocked);
            Assert.False(WithDeviceInfo(0x0a, FromHex("00")).ConfigurationLocked);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedFipsSeries()
        {
            Assert.False(DefaultInfo.IsFipsSeries);
            Assert.True(WithDeviceInfo(0x04, FromHex("80")).IsFipsSeries);
            Assert.True(WithDeviceInfo(0x04, FromHex("C0")).IsFipsSeries);
            Assert.False(WithDeviceInfo(0x04, FromHex("40")).IsFipsSeries);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedIsSkySeries()
        {
            Assert.False(DefaultInfo.IsSkySeries);
            Assert.True(WithDeviceInfo(0x04, FromHex("40")).IsSkySeries);
            Assert.True(WithDeviceInfo(0x04, FromHex("C0")).IsSkySeries);
            Assert.False(WithDeviceInfo(0x04, FromHex("80")).IsSkySeries);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedPartNumber()
        {
            // Valid UTF-8
            Assert.Equal("", DefaultInfo.PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, Array.Empty<byte>()).PartNumber);
            Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_=+-",
                WithDeviceInfo(0x13,
                    FromHex("6162636465666768696A6B6C6D6E6F707172737475767778797A41" +
                            "42434445464748494A4B4C4D4E4F505152535455565758595A303132333435363738395" +
                            "F3D2B2D")).PartNumber);
            Assert.Equal("ÖÄÅöäåěščřžýáíúůĚŠČŘŽÝÁÍÚŮ",
                WithDeviceInfo(0x13, FromHex("C396C384C385C3B6C3A4C3A5C49BC5A1C48DC599C5BEC3BDC3A1C3" +
                                             "ADC3BAC5AFC49AC5A0C48CC598C5BDC39DC381C38DC39AC5AE")).PartNumber);
            Assert.Equal("😀", WithDeviceInfo(0x13, FromHex("F09F9880")).PartNumber);
            Assert.Equal("0123456789ABCDEF",
                WithDeviceInfo(0x13, FromHex("30313233343536373839414243444546")).PartNumber);

            // Invalid UTF-8
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("c328")).PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("a0a1")).PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("e228a1")).PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("e28228")).PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("f0288cbc")).PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("f09028bc")).PartNumber);
            Assert.Equal("", WithDeviceInfo(0x13, FromHex("f0288c28")).PartNumber);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedPinComplexity()
        {
            Assert.False(DefaultInfo.IsPinComplexityEnabled);
            Assert.False(WithDeviceInfo(0x16, FromHex("00")).IsPinComplexityEnabled);
            Assert.True(WithDeviceInfo(0x16, FromHex("01")).IsPinComplexityEnabled);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedResetBlocked()
        {
            // Assert.Equal(0, DefaultInfo.IsResetBlocked); TODO
            // Assert.Equal(1056, infoOf(0x18, fromHex("0420")).IsResetBlocked); TODO
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedFpsVersion()
        {
            Assert.Null(DefaultInfo.TemplateStorageVersion);
            Assert.Equal(new FirmwareVersion(5, 6, 6), WithDeviceInfo(0x20, FromHex("050606")).TemplateStorageVersion);
        }

        [Fact]
        public void CreateFromResponseData_Returns_ExpectedStmVersion()
        {
            Assert.Null(DefaultInfo.ImageProcessorVersion);
            Assert.Equal(new FirmwareVersion(7, 0, 5), WithDeviceInfo(0x21, FromHex("070005")).ImageProcessorVersion);
        }

        private static YubiKeyDeviceInfo DefaultInfo => new YubiKeyDeviceInfo();

        private static YubiKeyDeviceInfo WithDeviceInfo(int tag, byte[] data) =>
            data.Length == 0
                ? new YubiKeyDeviceInfo()
                : YubiKeyDeviceInfo.CreateFromResponseData(new Dictionary<int, ReadOnlyMemory<byte>>()
                    { { tag, data } });

        private static byte[] FromHex(string? hex) => hex != null ? Base16.DecodeText(hex) : Array.Empty<byte>();
    }
}

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
using System.Collections.Generic;
using Xunit;

namespace Yubico.YubiKey
{
    public class FirmwareVersionTests
    {
        [Fact]
        public void Parse_GivenValidString_ReturnsFirmwareVersion()
        {
            // Arrange
            
            string validString = "1.2.3";
            
            // Act
            
            var fw = FirmwareVersion.Parse(validString);
            
            // Assert
            
            Assert.Equal(1, fw.Major);
            Assert.Equal(2, fw.Minor);
            Assert.Equal(3, fw.Patch);
        }
        
        
        [Fact]
        public void Parse_GivenInvalidString_ThrowsArgumentException()
        {
            // Arrange
            
            string invalidString = "1.2";
            
            // Act & Assert
            
            Assert.Throws<ArgumentException>(() => FirmwareVersion.Parse(invalidString));
        }
        
        
        [Fact]
        public void Major_GetSet_Succeeds()
        {
            var fw = new FirmwareVersion();
            byte expectedValue = GetRandomByte();

            fw.Major = expectedValue;

            Assert.Equal(expectedValue, fw.Major);
        }

        [Fact]
        public void Minor_GetSet_Succeeds()
        {
            var fw = new FirmwareVersion();
            byte expectedValue = GetRandomByte();

            fw.Minor = expectedValue;

            Assert.Equal(expectedValue, fw.Minor);
        }

        [Fact]
        public void Patch_GetSet_Succeeds()
        {
            var fw = new FirmwareVersion();
            byte expectedValue = GetRandomByte();

            fw.Patch = expectedValue;

            Assert.Equal(expectedValue, fw.Patch);
        }

        [Fact]
        public void DefaultConstructor_CreatesFw0_0_0()
        {
            var fw = new FirmwareVersion();
            Assert.True(fw.Major == 0);
            Assert.True(fw.Minor == 0);
            Assert.True(fw.Patch == 0);
        }

        [Fact]
        public void Constructor_GivenMajor_Returns1_0_0()
        {
            var fw = new FirmwareVersion(1);
            Assert.True(fw.Major == 1);
            Assert.True(fw.Minor == 0);
            Assert.True(fw.Patch == 0);
        }

        [Fact]
        public void Constructor_GivenMajorMinor_Returns1_2_0()
        {
            var fw = new FirmwareVersion(1, 2);
            Assert.True(fw.Major == 1);
            Assert.True(fw.Minor == 2);
            Assert.True(fw.Patch == 0);
        }

        [Fact]
        public void Constructor_GivenMajorMinorPatch_Returns1_2_3()
        {
            var fw = new FirmwareVersion(1, 2, 3);
            Assert.True(fw.Major == 1);
            Assert.True(fw.Minor == 2);
            Assert.True(fw.Patch == 3);
        }

        [Theory]
        [InlineData(1, 0, 0, true)]
        [InlineData(1, 0, 255, true)]
        [InlineData(1, 255, 0, true)]
        [InlineData(2, 0, 0, false)]
        [InlineData(2, 0, 1, false)]
        [InlineData(2, 1, 0, false)]
        [InlineData(3, 0, 0, false)]
        public void VersionTwoZeroZero_GreaterThan_Operator(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0 > fwTest);
        }

        [Theory]
        [InlineData(1, 0, 0, false)]
        [InlineData(1, 0, 255, false)]
        [InlineData(1, 255, 0, false)]
        [InlineData(2, 0, 0, false)]
        [InlineData(2, 0, 1, true)]
        [InlineData(2, 1, 0, true)]
        [InlineData(3, 0, 0, true)]
        public void VersionTwoZeroZero_LessThan_Operator(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0 < fwTest);
        }

        [Theory]
        [InlineData(1, 0, 0, true)]
        [InlineData(1, 0, 255, true)]
        [InlineData(1, 255, 0, true)]
        [InlineData(2, 0, 0, true)]
        [InlineData(2, 0, 1, false)]
        [InlineData(2, 1, 0, false)]
        [InlineData(3, 0, 0, false)]
        public void VersionTwoZeroZero_GreaterThanEqualTo_Operator(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0 >= fwTest);
        }

        [Theory]
        [InlineData(1, 0, 0, false)]
        [InlineData(1, 0, 255, false)]
        [InlineData(1, 255, 0, false)]
        [InlineData(2, 0, 0, true)]
        [InlineData(2, 0, 1, true)]
        [InlineData(2, 1, 0, true)]
        [InlineData(3, 0, 0, true)]
        public void VersionTwoZeroZero_LessThanEqualTo_Operator(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0 <= fwTest);
        }

        [Theory]
        [InlineData(1, 0, 0, false)]
        [InlineData(1, 0, 255, false)]
        [InlineData(1, 255, 0, false)]
        [InlineData(2, 0, 0, true)]
        [InlineData(2, 0, 1, false)]
        [InlineData(2, 1, 0, false)]
        [InlineData(3, 0, 0, false)]
        public void VersionTwoZeroZero_EqualTo_Operator(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0 == fwTest);
        }

        [Theory]
        [InlineData(1, 0, 0, true)]
        [InlineData(1, 0, 255, true)]
        [InlineData(1, 255, 0, true)]
        [InlineData(2, 0, 0, false)]
        [InlineData(2, 0, 1, true)]
        [InlineData(2, 1, 0, true)]
        [InlineData(3, 0, 0, true)]
        public void VersionTwoZeroZero_NotEqualTo_Operator(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0 != fwTest);
        }

        [Theory]
        [InlineData(1, 0, 0, false)]
        [InlineData(1, 0, 255, false)]
        [InlineData(1, 255, 0, false)]
        [InlineData(2, 0, 0, true)]
        [InlineData(2, 0, 1, false)]
        [InlineData(2, 1, 0, false)]
        [InlineData(3, 0, 0, false)]
        public void VersionTwoZeroZero_ObjectEquals(byte major, byte minor, byte patch, bool expected)
        {
            object fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0.Equals(fwTest));
        }

        [Fact]
        public void ObjectEquals_NotFirmwareVersion_ReturnsFalse()
        {
            var byteList = new List<byte>();

            Assert.False(FirmwareVersion.V2_0_0.Equals(byteList));
        }

        [Fact]
        public void ObjectEquals_Null_ReturnsFalse()
        {
            List<byte>? byteList = null;

            Assert.False(FirmwareVersion.V2_0_0.Equals(byteList));
        }

        [Fact]
        public void ObjectEquals_SameObject_ReturnsTrue()
        {
            object? other = FirmwareVersion.V2_0_0;

            Assert.True(FirmwareVersion.V2_0_0.Equals(other));
        }

        [Theory]
        [InlineData(1, 0, 0, false)]
        [InlineData(1, 0, 255, false)]
        [InlineData(1, 255, 0, false)]
        [InlineData(2, 0, 0, true)]
        [InlineData(2, 0, 1, false)]
        [InlineData(2, 1, 0, false)]
        [InlineData(3, 0, 0, false)]
        public void VersionTwoZeroZero_IEquatableEquals(byte major, byte minor, byte patch, bool expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            Assert.Equal(expected, FirmwareVersion.V2_0_0.Equals(fwTest));
        }

        [Fact]
        public void VersionTwoZeroZero_IEquatableEquals_SameObject_ReturnsTrue()
        {
            FirmwareVersion other = FirmwareVersion.V2_0_0;

            Assert.True(FirmwareVersion.V2_0_0.Equals(other));
        }

        [Theory]
        [InlineData(1, 0, 0, 1)]
        [InlineData(1, 0, 255, 1)]
        [InlineData(1, 255, 0, 1)]
        [InlineData(2, 0, 0, 0)]
        [InlineData(2, 0, 1, -1)]
        [InlineData(2, 1, 0, -1)]
        [InlineData(3, 0, 0, -1)]
        public void VersionTwoZeroZero_IComparableCompareTo(byte major, byte minor, byte patch, int expected)
        {
            object fwTest = new FirmwareVersion(major, minor, patch);

            int result = FirmwareVersion.V2_0_0.CompareTo(fwTest);
            int normalizedResult = result == 0 ? 0 : result > 0 ? 1 : -1;

            Assert.Equal(expected, normalizedResult);
        }

        [Fact]
        public void VersionTwoZeroZero_IComparableCompareTo_NotFirmwareVersion_ThrowsArgumentException()
        {
            var byteList = new List<byte>();
            _ = Assert.Throws<ArgumentException>(() => FirmwareVersion.V2_0_0.CompareTo(byteList));
        }

        [Fact]
        public void VersionTwoZeroZero_IComparableCompareTo_Null_Returns1()
        {
            List<byte>? byteList = null;

            Assert.Equal(1, FirmwareVersion.V2_0_0.CompareTo(byteList));
        }

        [Fact]
        public void VersionTwoZeroZero_IComparableCompareTo_SameObject_Returns0()
        {
            object? other = FirmwareVersion.V2_0_0;

            Assert.Equal(0, FirmwareVersion.V2_0_0.CompareTo(other));
        }

        [Theory]
        [InlineData(1, 0, 0, 1)]
        [InlineData(1, 0, 255, 1)]
        [InlineData(1, 255, 0, 1)]
        [InlineData(2, 0, 0, 0)]
        [InlineData(2, 0, 1, -1)]
        [InlineData(2, 1, 0, -1)]
        [InlineData(3, 0, 0, -1)]
        public void VersionTwoZeroZero_IComparableFirmwareVersionCompareTo(byte major, byte minor, byte patch, int expected)
        {
            var fwTest = new FirmwareVersion(major, minor, patch);

            int result = FirmwareVersion.V2_0_0.CompareTo(fwTest);
            int normalizedResult = result == 0 ? 0 : result > 0 ? 1 : -1;

            Assert.Equal(expected, normalizedResult);
        }

        [Fact]
        public void VersionTwoZeroZero_IComparableFirmwareVersionCompareTo_Null_Returns1()
        {
#nullable disable
            FirmwareVersion fwTest = null;

            Assert.Equal(1, FirmwareVersion.V2_0_0.CompareTo(fwTest));
#nullable enable
        }

        [Fact]
        public void VersionTwoZeroZero_IComparableFirmwareVersionCompareTo_SameObject_Returns0()
        {
            FirmwareVersion other = FirmwareVersion.V2_0_0;

            Assert.Equal(0, FirmwareVersion.V2_0_0.CompareTo(other));
        }

        [Fact]
        public void Version_LessThan_HigherVersion_ReturnsTrue()
        {
            var versionLeft = new ImageProcessorVersion(3, 7, 9);
            var versionRight = new ImageProcessorVersion(3, 7, 10);

            Assert.True(versionLeft < versionRight);
        }

        private static byte GetRandomByte()
        {
            var rng = new Random();
            return (byte)rng.Next(0, 255);
        }
    }
}

// Copyright 2025 Yubico AB
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

using Xunit;

namespace Yubico.YubiKey;

public class VersionQualifierTests
{
    [Fact]
    public void TestVersion()
    {
        var version = new FirmwareVersion(5, 7, 2);
        Assert.Equal(
            version, new VersionQualifier(version, VersionQualifierType.Alpha, 1).FirmwareVersion);
    }

    [Fact]
    public void TestType()
    {
        Assert.Equal(
            VersionQualifierType.Alpha,
            new VersionQualifier(new FirmwareVersion(5, 7, 2), VersionQualifierType.Alpha, 1).Type);
        Assert.Equal(
            VersionQualifierType.Beta,
            new VersionQualifier(new FirmwareVersion(5, 7, 2), VersionQualifierType.Beta, 1).Type);
        Assert.Equal(
            VersionQualifierType.Final,
            new VersionQualifier(new FirmwareVersion(5, 7, 2), VersionQualifierType.Final, 1).Type);
    }

    [Fact]
    public void TestIteration()
    {
        var version = new FirmwareVersion(5, 7, 2);
        var type = VersionQualifierType.Alpha;
        Assert.Equal(0, new VersionQualifier(version, type, 0).Iteration);
        Assert.Equal(128, new VersionQualifier(version, type, 128).Iteration);
        Assert.Equal(255, new VersionQualifier(version, type, 255).Iteration);
    }

    [Fact]
    public void TestToString()
    {
        Assert.Equal(
            "5.7.2.alpha.0",
            new VersionQualifier(new FirmwareVersion(5, 7, 2), VersionQualifierType.Alpha, 0).ToString());
        Assert.Equal(
            "5.6.6.beta.16384",
            new VersionQualifier(new FirmwareVersion(5, 6, 6), VersionQualifierType.Beta, 16384).ToString());
        Assert.Equal(
            "3.4.0.final.2147483648",
            new VersionQualifier(new FirmwareVersion(3, 4), VersionQualifierType.Final, 0x80000000).ToString());
        Assert.Equal(
            "3.4.0.final.2147483647",
            new VersionQualifier(new FirmwareVersion(3, 4), VersionQualifierType.Final, 0x7fffffff).ToString());
    }

    [Fact]
    public void TestEqualsAndHashCode()
    {
        var version1 = new FirmwareVersion(1);
        var version2 = new FirmwareVersion(1);
        var qualifier1 = new VersionQualifier(version1, VersionQualifierType.Alpha, 1);
        var qualifier2 = new VersionQualifier(version2, VersionQualifierType.Alpha, 1);
        var qualifier3 = new VersionQualifier(version1, VersionQualifierType.Beta, 2);

        Assert.Equal(qualifier1, qualifier2);
        Assert.Equal(qualifier1.GetHashCode(), qualifier2.GetHashCode());
        Assert.NotEqual(qualifier1, qualifier3);
    }

    [Fact]
    public void TestTypeFromValue()
    {
        Assert.Equal(VersionQualifierType.Alpha, (VersionQualifierType)0);
        Assert.Equal(VersionQualifierType.Beta, (VersionQualifierType)1);
        Assert.Equal(VersionQualifierType.Final, (VersionQualifierType)2);
    }
}

// Copyright 2024 Yubico AB
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
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivTypesTests
{
    [Theory]
    [InlineData(PivSlot.Authentication, 0x9A)]
    [InlineData(PivSlot.Signature, 0x9C)]
    [InlineData(PivSlot.KeyManagement, 0x9D)]
    [InlineData(PivSlot.CardAuthentication, 0x9E)]
    [InlineData(PivSlot.Attestation, 0xF9)]
    public void PivSlot_HasCorrectValue(PivSlot slot, byte expected)
    {
        Assert.Equal(expected, (byte)slot);
    }

    [Theory]
    [InlineData(PivAlgorithm.Rsa1024, 0x06)]
    [InlineData(PivAlgorithm.Rsa2048, 0x07)]
    [InlineData(PivAlgorithm.EccP256, 0x11)]
    [InlineData(PivAlgorithm.EccP384, 0x14)]
    [InlineData(PivAlgorithm.Ed25519, 0xE0)]
    [InlineData(PivAlgorithm.X25519, 0xE1)]
    public void PivAlgorithm_HasCorrectValue(PivAlgorithm algo, byte expected)
    {
        Assert.Equal(expected, (byte)algo);
    }

    [Theory]
    [InlineData(PivManagementKeyType.TripleDes, 0x03)]
    [InlineData(PivManagementKeyType.Aes128, 0x08)]
    [InlineData(PivManagementKeyType.Aes192, 0x0A)]
    [InlineData(PivManagementKeyType.Aes256, 0x0C)]
    public void PivManagementKeyType_HasCorrectValue(PivManagementKeyType type, byte expected)
    {
        Assert.Equal(expected, (byte)type);
    }

    [Fact]
    public void PivFeatures_P384_RequiresVersion4()
    {
        var feature = PivFeatures.P384;
        Assert.Equal(new FirmwareVersion(4, 0, 0), feature.Version);
    }

    [Fact]
    public void PivFeatures_SupportsRsaGeneration_FalseFor426()
    {
        Assert.False(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(4, 2, 6)));
        Assert.False(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(4, 3, 0)));
        Assert.True(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(4, 3, 5)));
        Assert.True(PivFeatures.SupportsRsaGeneration(new FirmwareVersion(5, 0, 0)));
    }

    [Theory]
    [InlineData(PivPinPolicy.Default, 0x00)]
    [InlineData(PivPinPolicy.Never, 0x01)]
    [InlineData(PivPinPolicy.Once, 0x02)]
    [InlineData(PivPinPolicy.Always, 0x03)]
    public void PivPinPolicy_HasCorrectValue(PivPinPolicy policy, byte expected)
    {
        Assert.Equal(expected, (byte)policy);
    }

    [Theory]
    [InlineData(PivTouchPolicy.Default, 0x00)]
    [InlineData(PivTouchPolicy.Never, 0x01)]
    [InlineData(PivTouchPolicy.Always, 0x02)]
    [InlineData(PivTouchPolicy.Cached, 0x03)]
    public void PivTouchPolicy_HasCorrectValue(PivTouchPolicy policy, byte expected)
    {
        Assert.Equal(expected, (byte)policy);
    }
}
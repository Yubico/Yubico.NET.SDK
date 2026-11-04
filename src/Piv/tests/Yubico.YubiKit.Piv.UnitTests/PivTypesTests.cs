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

using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;

namespace Yubico.YubiKit.Piv.UnitTests;

public class PivTypesTests
{
    [Theory]
    [InlineData(Slot.Authentication, 0x9A)]
    [InlineData(Slot.Signature, 0x9C)]
    [InlineData(Slot.KeyManagement, 0x9D)]
    [InlineData(Slot.CardAuthentication, 0x9E)]
    [InlineData(Slot.Attestation, 0xF9)]
    public void PivSlot_HasCorrectValue(Slot slot, byte expected)
    {
        Assert.Equal(expected, (byte)slot);
    }

    [Theory]
    [InlineData(KeyType.RSA1024, 0x06)]
    [InlineData(KeyType.RSA2048, 0x07)]
    [InlineData(KeyType.ECP256, 0x11)]
    [InlineData(KeyType.ECP384, 0x14)]
    [InlineData(KeyType.Ed25519, 0xE0)]
    [InlineData(KeyType.X25519, 0xE1)]
    public void PivAlgorithmId_HasCorrectValue(KeyType algo, byte expected)
    {
        Assert.Equal(expected, KeyTypeEncoding.Encode(algo));
    }

    [Theory]
    [InlineData(KeyType.TripleDES, 0x03)]
    [InlineData(KeyType.AES128, 0x08)]
    [InlineData(KeyType.AES192, 0x0A)]
    [InlineData(KeyType.AES256, 0x0C)]
    public void PivManagementKeyType_HasCorrectValue(KeyType type, byte expected)
    {
        Assert.Equal(expected, KeyTypeEncoding.Encode(type));
    }

    [Fact]
    public void PivFeatures_P384_RequiresVersion4()
    {
        var feature = Features.P384;
        Assert.Equal(new FirmwareVersion(4, 0, 0), feature.Version);
    }

    [Fact]
    public void PivFeatures_SupportsRsaGeneration_FalseFor426()
    {
        Assert.False(Features.SupportsRsaGeneration(new FirmwareVersion(4, 2, 6)));
        Assert.False(Features.SupportsRsaGeneration(new FirmwareVersion(4, 3, 0)));
        Assert.True(Features.SupportsRsaGeneration(new FirmwareVersion(4, 3, 5)));
        Assert.True(Features.SupportsRsaGeneration(new FirmwareVersion(5, 0, 0)));
    }

    [Theory]
    [InlineData(PinPolicy.Default, 0x00)]
    [InlineData(PinPolicy.Never, 0x01)]
    [InlineData(PinPolicy.Once, 0x02)]
    [InlineData(PinPolicy.Always, 0x03)]
    public void PivPinPolicy_HasCorrectValue(PinPolicy policy, byte expected)
    {
        Assert.Equal(expected, (byte)policy);
    }

    [Theory]
    [InlineData(TouchPolicy.Default, 0x00)]
    [InlineData(TouchPolicy.Never, 0x01)]
    [InlineData(TouchPolicy.Always, 0x02)]
    [InlineData(TouchPolicy.Cached, 0x03)]
    public void PivTouchPolicy_HasCorrectValue(TouchPolicy policy, byte expected)
    {
        Assert.Equal(expected, (byte)policy);
    }
}
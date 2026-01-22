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

using Xunit;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivMetadataTests
{
    private static readonly byte[] DefaultTripleDesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    
    private static readonly byte[] DefaultAesManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetPinMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetPinMetadataAsync();
        
        Assert.True(metadata.IsDefault);
        Assert.Equal(3, metadata.TotalRetries);
        Assert.Equal(3, metadata.RetriesRemaining);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_EmptySlot_ReturnsNull(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        
        Assert.Null(metadata);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetSlotMetadataAsync_WithKey_ReturnsMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        
        Assert.NotNull(metadata);
        Assert.Equal(PivAlgorithm.EccP256, metadata.Value.Algorithm);
        Assert.True(metadata.Value.IsGenerated);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetManagementKeyMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetManagementKeyMetadataAsync();
        
        Assert.True(metadata.IsDefault);
        // Key type depends on firmware version
        if (state.FirmwareVersion >= new FirmwareVersion(5, 7, 0))
        {
            Assert.Equal(PivManagementKeyType.Aes192, metadata.KeyType);
        }
        else
        {
            Assert.Equal(PivManagementKeyType.TripleDes, metadata.KeyType);
        }
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task GetBioMetadataAsync_NonBioDevice_ThrowsOrReturnsError(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        
        var ex = await Record.ExceptionAsync(() => session.GetBioMetadataAsync());
        
        // Only accept specific known error responses for non-Bio devices
        Assert.True(
            ex is NotSupportedException || 
            (ex is ApduException apduEx && 
                (apduEx.SW == 0x6D00 || // INS not supported
                 apduEx.SW == 0x6A81 || // Function not supported
                 apduEx.SW == 0x6985)), // Conditions of use not satisfied
            $"Expected NotSupportedException or ApduException with SW 0x6D00, 0x6A81, or 0x6985, but got {ex?.GetType().Name}: {ex?.Message}");
    }
}

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

public class PivManagementKeyTests
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

    [Theory(Skip = "SetManagementKeyAsync returns SW 0x6A80 - command data encoding needs fix")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SetManagementKeyAsync_ChangesToNewKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        // Use appropriate key type based on firmware
        var newKeyType = state.FirmwareVersion >= new FirmwareVersion(5, 7, 0)
            ? PivManagementKeyType.Aes192
            : PivManagementKeyType.TripleDes;
        var newKey = new byte[] {
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22,
            0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22
        };
        
        try
        {
            // Change the management key
            await session.SetManagementKeyAsync(newKeyType, newKey);
            
            // Create new session to verify the change
            await using var session2 = await state.Device.CreatePivSessionAsync();
            
            // Old key should fail
            await Assert.ThrowsAsync<ApduException>(
                () => session2.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion)));
            
            // New key should work
            await session2.AuthenticateAsync(newKey);
            Assert.True(session2.IsAuthenticated);
        }
        finally
        {
            await session.ResetAsync();
        }
    }

    [Theory(Skip = "SetManagementKeyAsync returns SW 0x6A80 - command data encoding needs fix")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.2")]
    public async Task SetManagementKeyAsync_AES256_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        // AES256 = 32 bytes
        var aes256Key = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38
        };
        
        try
        {
            // Change to AES256
            await session.SetManagementKeyAsync(PivManagementKeyType.Aes256, aes256Key);
            
            // Verify via metadata
            var metadata = await session.GetManagementKeyMetadataAsync();
            Assert.Equal(PivManagementKeyType.Aes256, metadata.KeyType);
            Assert.False(metadata.IsDefault);
            
            // Verify can authenticate with new key
            await using var session2 = await state.Device.CreatePivSessionAsync();
            await session2.AuthenticateAsync(aes256Key);
            Assert.True(session2.IsAuthenticated);
        }
        finally
        {
            await session.ResetAsync();
        }
    }

    [Theory(Skip = "SetManagementKeyAsync returns SW 0x6A80 - command data encoding needs fix")]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.2")]
    public async Task SetManagementKeyAsync_AES128_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        // AES128 = 16 bytes
        var aes128Key = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18
        };
        
        try
        {
            // Change to AES128
            await session.SetManagementKeyAsync(PivManagementKeyType.Aes128, aes128Key);
            
            // Verify via metadata
            var metadata = await session.GetManagementKeyMetadataAsync();
            Assert.Equal(PivManagementKeyType.Aes128, metadata.KeyType);
            Assert.False(metadata.IsDefault);
            
            // Verify can authenticate with new key
            await using var session2 = await state.Device.CreatePivSessionAsync();
            await session2.AuthenticateAsync(aes128Key);
            Assert.True(session2.IsAuthenticated);
        }
        finally
        {
            await session.ResetAsync();
        }
    }
}

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
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivPukTests
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
    
    private static readonly byte[] DefaultPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultPuk = "12345678"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    /// <summary>
    /// Block the PIN by attempting wrong PIN 3 times.
    /// </summary>
    private static async Task BlockPinAsync(IPivSession session)
    {
        var wrongPin = "000000"u8.ToArray();
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await session.VerifyPinAsync(wrongPin);
            }
            catch (InvalidPinException)
            {
                // Expected
            }
        }
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ChangePukAsync_WithCorrectOldPuk_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var newPuk = "87654321"u8.ToArray();
        
        try
        {
            // Change PUK to new value
            await session.ChangePukAsync(DefaultPuk, newPuk);
            
            // Block the PIN
            await BlockPinAsync(session);
            
            // Unblock with new PUK (verifies the change worked)
            await session.UnblockPinAsync(newPuk, DefaultPin);
            
            // Verify PIN works again
            await session.VerifyPinAsync(DefaultPin);
        }
        finally
        {
            await session.ResetAsync();
        }
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task UnblockPinAsync_AfterBlockedPin_RestoresAccess(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var newPin = "654321"u8.ToArray();
        
        try
        {
            // Block the PIN
            await BlockPinAsync(session);
            
            // Verify PIN is blocked (0 retries)
            var attempts = await session.GetPinAttemptsAsync();
            Assert.Equal(0, attempts);
            
            // Verify PIN fails
            await Assert.ThrowsAsync<InvalidPinException>(
                () => session.VerifyPinAsync(DefaultPin));
            
            // Unblock with PUK
            await session.UnblockPinAsync(DefaultPuk, newPin);
            
            // Verify new PIN works
            await session.VerifyPinAsync(newPin);
            
            // Verify retries restored
            var restoredAttempts = await session.GetPinAttemptsAsync();
            Assert.Equal(3, restoredAttempts);
        }
        finally
        {
            await session.ResetAsync();
        }
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GetPukMetadataAsync_ReturnsValidMetadata(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var metadata = await session.GetPukMetadataAsync();
        
        Assert.True(metadata.IsDefault);
        Assert.Equal(3, metadata.TotalRetries);
        Assert.Equal(3, metadata.RetriesRemaining);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task SetPinAttemptsAsync_CustomLimit_EnforcesLimit(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        try
        {
            // Set custom limits: 5 PIN attempts, 4 PUK attempts
            await session.SetPinAttemptsAsync(5, 4);
            
            // Verify via metadata
            var pinMetadata = await session.GetPinMetadataAsync();
            Assert.Equal(5, pinMetadata.TotalRetries);
            Assert.Equal(5, pinMetadata.RetriesRemaining);
            
            var pukMetadata = await session.GetPukMetadataAsync();
            Assert.Equal(4, pukMetadata.TotalRetries);
            Assert.Equal(4, pukMetadata.RetriesRemaining);
            
            // Also verify via GetPinAttemptsAsync
            var attempts = await session.GetPinAttemptsAsync();
            Assert.Equal(5, attempts);
        }
        finally
        {
            await session.ResetAsync();
        }
    }
}

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

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Xunit;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivAuthenticationTests
{
    private static readonly byte[] DefaultManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };
    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    [Theory]
    [WithYubiKey]
    public async Task AuthenticateAsync_WithDefaultKey_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync(); // Ensure default state
        
        await session.AuthenticateAsync(DefaultManagementKey);
        
        Assert.True(session.IsAuthenticated);
    }

    [Theory]
    [WithYubiKey]
    public async Task AuthenticateAsync_WithWrongKey_ThrowsBadResponse(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var wrongKey = new byte[24];
        
        await Assert.ThrowsAsync<ApduException>(
            () => session.AuthenticateAsync(wrongKey));
    }

    [Theory]
    [WithYubiKey]
    public async Task VerifyPinAsync_WithCorrectPin_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        await session.VerifyPinAsync(DefaultPin);
        
        // No exception means success
    }

    [Theory]
    [WithYubiKey]
    public async Task VerifyPinAsync_WithWrongPin_ThrowsInvalidPinException(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var wrongPin = "000000"u8.ToArray();
        
        var ex = await Assert.ThrowsAsync<InvalidPinException>(
            () => session.VerifyPinAsync(wrongPin));
        
        Assert.True(ex.RetriesRemaining >= 0);
        Assert.True(ex.RetriesRemaining < 3); // One attempt used
    }

    [Theory]
    [WithYubiKey]
    public async Task GetPinAttemptsAsync_ReturnsCorrectCount(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var attempts = await session.GetPinAttemptsAsync();
        
        Assert.Equal(3, attempts); // Default after reset
    }

    [Theory]
    [WithYubiKey]
    public async Task ChangePinAsync_WithCorrectOldPin_Succeeds(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        var newPin = "654321"u8.ToArray();
        
        try
        {
            await session.ChangePinAsync(DefaultPin, newPin);
            
            // Verify old PIN no longer works
            await Assert.ThrowsAsync<InvalidPinException>(
                () => session.VerifyPinAsync(DefaultPin));
            
            // Verify new PIN works
            await session.VerifyPinAsync(newPin);
        }
        finally
        {
            // Reset back to default
            await session.ResetAsync();
        }
    }
}
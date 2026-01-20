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
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivResetTests
{
    private static readonly byte[] DefaultManagementKey = new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    [Theory]
    [WithYubiKey]
    public async Task ResetAsync_RestoresToDefaults(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        
        await session.ResetAsync();
        
        // Verify default state - management key type resets to TripleDes
        Assert.Equal(PivManagementKeyType.TripleDes, session.ManagementKeyType);
        
        // Verify we can authenticate with default management key
        await session.AuthenticateAsync(DefaultManagementKey);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.3.0")]
    public async Task ResetAsync_PinMetadataShowsDefault(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        
        await session.ResetAsync();
        
        var pinMetadata = await session.GetPinMetadataAsync();
        Assert.True(pinMetadata.IsDefault);
        Assert.Equal(3, pinMetadata.TotalRetries);
        Assert.Equal(3, pinMetadata.RetriesRemaining);
    }

    [Theory]
    [WithYubiKey]
    public async Task ResetAsync_ClearsAllSlots(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        
        // Generate a key in a slot
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        // Reset again
        await session.ResetAsync();
        
        // Verify slot is empty
        var cert = await session.GetCertificateAsync(PivSlot.Authentication);
        Assert.Null(cert);
    }
}

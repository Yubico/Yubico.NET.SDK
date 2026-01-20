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

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivKeyOperationsTests
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
    public async Task GenerateKeyAsync_EccP256_ReturnsPublicKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication, 
            PivAlgorithm.EccP256);
        
        Assert.NotNull(publicKey);
        Assert.IsType<ECPublicKey>(publicKey);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.0")]
    public async Task GenerateKeyAsync_Ed25519_ReturnsPublicKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519);
        
        Assert.NotNull(publicKey);
        Assert.IsType<Curve25519PublicKey>(publicKey);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "4.3.0")]
    public async Task AttestKeyAsync_GeneratedKey_ReturnsCertificate(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var attestation = await session.AttestKeyAsync(PivSlot.Authentication);
        
        Assert.NotNull(attestation);
        Assert.Contains("Yubico", attestation.Issuer);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.0")]
    public async Task MoveKeyAsync_MovesToNewSlot(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(DefaultManagementKey);
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        await session.MoveKeyAsync(PivSlot.Authentication, PivSlot.Retired1);
        
        var sourceMetadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        var destMetadata = await session.GetSlotMetadataAsync(PivSlot.Retired1);
        
        Assert.Null(sourceMetadata); // Source now empty
        Assert.NotNull(destMetadata); // Dest has key
    }
}
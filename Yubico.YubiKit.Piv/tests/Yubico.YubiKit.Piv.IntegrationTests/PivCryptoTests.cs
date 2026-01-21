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
using Xunit;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.Management;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.Piv.IntegrationTests;

public class PivCryptoTests
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

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SignOrDecryptAsync_EccP256Sign_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.EccP256,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);
        
        var dataToSign = SHA256.HashData("test data"u8);
        
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature, 
            PivAlgorithm.EccP256, 
            dataToSign);
        
        Assert.NotEmpty(signature.ToArray());
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(dataToSign, signature.Span));
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task CalculateSecretAsync_ECDH_ProducesSharedSecret(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var devicePublicKey = await session.GenerateKeyAsync(
            PivSlot.KeyManagement, 
            PivAlgorithm.EccP256);
        await session.VerifyPinAsync(DefaultPin);
        
        using var peerKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var peerPublicKeyBytes = peerKey.PublicKey.ExportSubjectPublicKeyInfo();
        var peerPublicKey = ECPublicKey.CreateFromSubjectPublicKeyInfo(peerPublicKeyBytes);
        
        var sharedSecret = await session.CalculateSecretAsync(
            PivSlot.KeyManagement, 
            peerPublicKey);
        
        Assert.Equal(32, sharedSecret.Length);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task SignOrDecryptAsync_Ed25519_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);
        
        var dataToSign = "test data"u8.ToArray();
        
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519, 
            dataToSign);
        
        Assert.NotEmpty(signature.ToArray());
        Assert.Equal(64, signature.Length);
    }
}

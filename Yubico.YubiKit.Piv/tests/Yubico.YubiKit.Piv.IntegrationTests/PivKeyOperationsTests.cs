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

public class PivKeyOperationsTests
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
    public async Task GenerateKeyAsync_EccP256_ReturnsPublicKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication, 
            PivAlgorithm.EccP256);
        
        Assert.NotNull(publicKey);
        Assert.IsType<ECPublicKey>(publicKey);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task GenerateKeyAsync_Ed25519_ReturnsPublicKey(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature, 
            PivAlgorithm.Ed25519);
        
        Assert.NotNull(publicKey);
        Assert.IsType<Curve25519PublicKey>(publicKey);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.0")]
    public async Task AttestKeyAsync_GeneratedKey_ReturnsCertificate(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        var attestation = await session.AttestKeyAsync(PivSlot.Authentication);
        
        Assert.NotNull(attestation);
        // The attestation certificate is issued by the PIV Attestation CA (issuer varies by YubiKey)
        Assert.False(string.IsNullOrEmpty(attestation.Issuer));
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task MoveKeyAsync_MovesToNewSlot_KeyRemainsFunctional(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        var publicKey = await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        await session.MoveKeyAsync(PivSlot.Authentication, PivSlot.Retired1);
        
        var sourceMetadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        var destMetadata = await session.GetSlotMetadataAsync(PivSlot.Retired1);
        
        Assert.Null(sourceMetadata); // Source now empty
        Assert.NotNull(destMetadata); // Dest has key
        
        // Verify key is functional by signing with the moved key
        await session.VerifyPinAsync(DefaultPin);
        var hash = SHA256.HashData("test data"u8);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Retired1, 
            PivAlgorithm.EccP256, 
            hash);
        
        // Verify signature with original public key
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ImportKeyAsync_EccP256_CanSign(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        // Generate software ECDSA key
        using var softwareKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pkcs8 = softwareKey.ExportPkcs8PrivateKey();
        var privateKey = ECPrivateKey.CreateFromPkcs8(pkcs8);
        
        // Import to YubiKey
        var detectedAlgorithm = await session.ImportKeyAsync(
            PivSlot.Authentication, 
            privateKey);
        
        Assert.Equal(PivAlgorithm.EccP256, detectedAlgorithm);
        
        // Sign with YubiKey
        await session.VerifyPinAsync(DefaultPin);
        var dataToSign = "test data"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Authentication, 
            PivAlgorithm.EccP256, 
            hash);
        
        // Verify with software public key
        Assert.True(softwareKey.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.0")]
    public async Task DeleteKeyAsync_RemovesKey_SlotBecomesEmpty(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        
        // Generate key
        await session.GenerateKeyAsync(PivSlot.Authentication, PivAlgorithm.EccP256);
        
        // Verify key exists
        var metadataBefore = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.NotNull(metadataBefore);
        
        // Delete key
        await session.DeleteKeyAsync(PivSlot.Authentication);
        
        // Verify slot is now empty
        var metadataAfter = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.Null(metadataAfter);
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task PutObjectAsync_GetObjectAsync_RoundTrip(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));
        await session.VerifyPinAsync(DefaultPin);
        
        var testData = "Hello, YubiKey!"u8.ToArray();
        
        try
        {
            // Write data to Printed object
            await session.PutObjectAsync(PivDataObject.PrintedInformation, testData);
            
            // Read it back
            var readData = await session.GetObjectAsync(PivDataObject.PrintedInformation);
            
            Assert.False(readData.IsEmpty);
            Assert.Equal(testData, readData.ToArray());
        }
        finally
        {
            // Clean up by deleting the object (write null/empty)
            try
            {
                await session.PutObjectAsync(PivDataObject.PrintedInformation, ReadOnlyMemory<byte>.Empty);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.0.0")]
    public async Task GetSerialNumberAsync_ReturnsDeviceSerial(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        
        var serial = await session.GetSerialNumberAsync();
        
        Assert.True(serial > 0);
        Assert.Equal(state.SerialNumber, serial);
    }
}
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

public class PivPolicyTests
{
    private static readonly byte[] DefaultTripleDesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultAesManagementKey =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    ];

    private static readonly byte[] DefaultPin = "123456"u8.ToArray();

    private static byte[] GetDefaultManagementKey(FirmwareVersion version) =>
        version >= new FirmwareVersion(5, 7, 0) ? DefaultAesManagementKey : DefaultTripleDesManagementKey;

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GenerateKey_PinPolicyNever_CanSignWithoutPin(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            PivPinPolicy.Never);

        Assert.NotNull(publicKey);

        // Verify metadata reflects the PIN policy
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Authentication);
        Assert.NotNull(metadata);
        Assert.Equal(PivPinPolicy.Never, metadata.Value.PinPolicy);

        // Sign WITHOUT verifying PIN first (should succeed with Never policy)
        var hash = SHA256.HashData("no-pin-required"u8);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Authentication,
            PivAlgorithm.EccP256,
            hash);

        Assert.False(signature.IsEmpty);

        // Verify signature with the generated public key
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.3.0")]
    public async Task GenerateKey_PinPolicyAlways_RequiresPinForEachSign(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            PivPinPolicy.Always);

        Assert.NotNull(publicKey);

        // Verify metadata reflects the PIN policy
        var metadata = await session.GetSlotMetadataAsync(PivSlot.Signature);
        Assert.NotNull(metadata);
        Assert.Equal(PivPinPolicy.Always, metadata.Value.PinPolicy);

        // Verify PIN and sign
        await session.VerifyPinAsync(DefaultPin);
        var hash = SHA256.HashData("pin-always-test"u8);
        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            hash);

        Assert.False(signature.IsEmpty);

        // Verify the signature is valid
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }
}

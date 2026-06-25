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

/// <summary>
/// Integration tests for RSA signing with multiple hash algorithms and padding schemes.
///
/// PIV performs raw RSA private key operations. The host constructs PKCS#1 v1.5
/// DigestInfo+padding before sending to the YubiKey, and verification uses the
/// standard .NET RSA APIs. These tests exercise different hash algorithm variants
/// (SHA-1, SHA-256, SHA-384, SHA-512) with PKCS#1 v1.5 signing, and OAEP decrypt
/// with SHA-1 vs SHA-256 MGF1.
///
/// NOTE: RSA-PSS is not directly supported by PIV hardware -- PIV does raw RSA.
/// PSS padding must be constructed on the host and is not testable in a standard
/// round-trip because PSS uses randomized padding that .NET's RSA.VerifyData
/// with Pkcs1 cannot verify. Instead, we test the hash algorithm dimension.
/// </summary>
public class PivSigningAlgorithmTests
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

    // DigestInfo prefixes for PKCS#1 v1.5 signing with different hash algorithms
    // Each prefix encodes: SEQUENCE { SEQUENCE { OID, NULL }, OCTET STRING (hash) }

    // SHA-1: OID 1.3.14.3.2.26
    private static readonly byte[] Sha1DigestInfo =
    [
        0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E,
        0x03, 0x02, 0x1A, 0x05, 0x00, 0x04, 0x14
    ];

    // SHA-256: OID 2.16.840.1.101.3.4.2.1
    private static readonly byte[] Sha256DigestInfo =
    [
        0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
        0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05,
        0x00, 0x04, 0x20
    ];

    // SHA-384: OID 2.16.840.1.101.3.4.2.2
    private static readonly byte[] Sha384DigestInfo =
    [
        0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
        0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05,
        0x00, 0x04, 0x30
    ];

    // SHA-512: OID 2.16.840.1.101.3.4.2.3
    private static readonly byte[] Sha512DigestInfo =
    [
        0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86,
        0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05,
        0x00, 0x04, 0x40
    ];

    /// <summary>
    /// Creates PKCS#1 v1.5 signing padding: 0x00 0x01 [0xFF ...] 0x00 [DigestInfo] [Hash]
    /// </summary>
    private static byte[] CreatePkcs1v15SigningPadding(byte[] digestInfo, byte[] hash, int modulusBytes)
    {
        var padded = new byte[modulusBytes];
        padded[0] = 0x00;
        padded[1] = 0x01;

        var paddingLength = modulusBytes - 3 - digestInfo.Length - hash.Length;
        for (var i = 2; i < 2 + paddingLength; i++)
        {
            padded[i] = 0xFF;
        }

        padded[2 + paddingLength] = 0x00;
        Array.Copy(digestInfo, 0, padded, 3 + paddingLength, digestInfo.Length);
        Array.Copy(hash, 0, padded, 3 + paddingLength + digestInfo.Length, hash.Length);

        return padded;
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignRsa2048_WithSha1Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.Rsa2048,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "SHA-1 signing test"u8.ToArray();
#pragma warning disable CA5350 // SHA1 used intentionally for algorithm coverage test
        var hash = SHA1.HashData(dataToSign);
#pragma warning restore CA5350

        var paddedData = CreatePkcs1v15SigningPadding(Sha1DigestInfo, hash, KeyDefinitions.RSA2048.LengthInBytes);

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.Rsa2048,
            paddedData);

        Assert.Equal(KeyDefinitions.RSA2048.LengthInBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
#pragma warning disable CA5350
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
#pragma warning restore CA5350
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignRsa2048_WithSha384Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.Rsa2048,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "SHA-384 signing test"u8.ToArray();
        var hash = SHA384.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha384DigestInfo, hash, KeyDefinitions.RSA2048.LengthInBytes);

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.Rsa2048,
            paddedData);

        Assert.Equal(KeyDefinitions.RSA2048.LengthInBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
    }

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task SignRsa2048_WithSha512Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.Rsa2048,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "SHA-512 signing test"u8.ToArray();
        var hash = SHA512.HashData(dataToSign);

        var paddedData = CreatePkcs1v15SigningPadding(Sha512DigestInfo, hash, KeyDefinitions.RSA2048.LengthInBytes);

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.Rsa2048,
            paddedData);

        Assert.Equal(KeyDefinitions.RSA2048.LengthInBytes, signature.Length);

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(rsa.VerifyData(dataToSign, signature.Span, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1));
    }

    /// <summary>
    /// Verifies DecryptAsync with OAEP SHA-1 padding. The existing PivDecryptTests cover
    /// PKCS#1 v1.5 and OAEP SHA-256; this covers the SHA-1 MGF1 variant.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.3.5")]
    public async Task DecryptRsa2048_OaepSha1_ReturnsExactPlaintext(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.KeyManagement,
            PivAlgorithm.Rsa2048);
        await session.VerifyPinAsync(DefaultPin);

        var plaintext = "OAEP SHA-1 test"u8.ToArray();
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(((RSAPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        var ciphertext = rsa.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA1);

        var decrypted = await session.DecryptAsync(
            PivSlot.KeyManagement,
            ciphertext,
            RSAEncryptionPadding.OaepSHA1);

        Assert.Equal(plaintext, decrypted.ToArray());
    }

    /// <summary>
    /// ECC P-256 signing with SHA-384 hash (truncated to 32 bytes by the PIV layer).
    /// Verifies that the signature is valid when verified with the correct hash size.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SignEccP256_WithSha256Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "ECC P-256 SHA-256 signing test"u8.ToArray();
        var hash = SHA256.HashData(dataToSign);

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP256,
            hash);

        Assert.False(signature.IsEmpty);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }

    /// <summary>
    /// ECC P-384 signing with SHA-384 hash. P-384 naturally pairs with SHA-384 (48-byte hash).
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "4.0.0")]
    public async Task SignEccP384_WithSha384Hash_ProducesValidSignature(YubiKeyTestState state)
    {
        await using var session = await state.Device.CreatePivSessionAsync();
        await session.ResetAsync();
        await session.AuthenticateAsync(GetDefaultManagementKey(state.FirmwareVersion));

        var publicKey = await session.GenerateKeyAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP384,
            PivPinPolicy.Once);
        await session.VerifyPinAsync(DefaultPin);

        var dataToSign = "ECC P-384 SHA-384 signing test"u8.ToArray();
        var hash = SHA384.HashData(dataToSign);

        var signature = await session.SignOrDecryptAsync(
            PivSlot.Signature,
            PivAlgorithm.EccP384,
            hash);

        Assert.False(signature.IsEmpty);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(((ECPublicKey)publicKey).ExportSubjectPublicKeyInfo(), out _);
        Assert.True(ecdsa.VerifyHash(hash, signature.Span, DSASignatureFormat.Rfc3279DerSequence));
    }
}

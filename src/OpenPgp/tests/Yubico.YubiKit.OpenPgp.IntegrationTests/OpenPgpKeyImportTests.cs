// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpKeyImportTests
{
    private const string DefaultUserPin = "123456";
    private const string DefaultAdminPin = "12345678";

    // ── RSA 2048 Import (CRT format) ────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ImportRsaKey_2048Crt_ThenSign_Succeeds(YubiKeyTestState state)
    {
        if (state.FirmwareVersion >= new FirmwareVersion(4, 2, 0) &&
            state.FirmwareVersion <= new FirmwareVersion(4, 3, 5))
        {
            return;
        }

        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Generate an RSA 2048 key pair in software
                using var rsa = RSA.Create(2048);
                var parameters = rsa.ExportParameters(includePrivateParameters: true);

                var template = new RsaKeyTemplate(
                    KeyRef.Sig,
                    e: parameters.Exponent!,
                    p: parameters.P!,
                    q: parameters.Q!);

                var attributes = RsaAttributes.Create(RsaSize.Rsa2048, RsaImportFormat.Standard);
                await session.PutKeyAsync(KeyRef.Sig, template, attributes);

                // Verify key was imported
                var keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Sig]);

                // Sign with the imported key
                var message = "Import RSA test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.Equal(256, signature.Length);
            });
    }

    // ── EC P-256 Import ─────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_P256_ThenSign_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var parameters = ecdsa.ExportParameters(includePrivateParameters: true);

                // Private key scalar
                var privateKey = parameters.D!;
                // Uncompressed public key point: 0x04 || X || Y
                var publicKey = new byte[1 + parameters.Q.X!.Length + parameters.Q.Y!.Length];
                publicKey[0] = 0x04;
                parameters.Q.X.CopyTo(publicKey, 1);
                parameters.Q.Y.CopyTo(publicKey, 1 + parameters.Q.X.Length);

                var template = new EcKeyTemplate(KeyRef.Sig, privateKey, publicKey);
                var attributes = EcAttributes.Create(KeyRef.Sig, CurveOid.Secp256R1);
                await session.PutKeyAsync(KeyRef.Sig, template, attributes);

                // Verify key was imported
                var keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Sig]);

                // Sign with the imported key
                var message = "Import EC P256 test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.True(signature.Length > 0);
            });

    // ── EC P-384 Import ─────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_P384_ThenSign_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
                var parameters = ecdsa.ExportParameters(includePrivateParameters: true);

                var privateKey = parameters.D!;
                var publicKey = new byte[1 + parameters.Q.X!.Length + parameters.Q.Y!.Length];
                publicKey[0] = 0x04;
                parameters.Q.X.CopyTo(publicKey, 1);
                parameters.Q.Y.CopyTo(publicKey, 1 + parameters.Q.X.Length);

                var template = new EcKeyTemplate(KeyRef.Sig, privateKey, publicKey);
                var attributes = EcAttributes.Create(KeyRef.Sig, CurveOid.Secp384R1);
                await session.PutKeyAsync(KeyRef.Sig, template, attributes);

                var keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Sig]);

                var message = "Import EC P384 test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA384);
                Assert.True(signature.Length > 0);
            });

    // ── Ed25519 Import ──────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_Ed25519_KeyInfoShowsImported(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Generate Ed25519 key material
                // Ed25519 private key is 32 bytes
                var privateKey = RandomNumberGenerator.GetBytes(32);

                try
                {
                    var template = new EcKeyTemplate(KeyRef.Sig, privateKey);
                    var attributes = EcAttributes.Create(KeyRef.Sig, CurveOid.Ed25519);
                    await session.PutKeyAsync(KeyRef.Sig, template, attributes);

                    var keyInfo = await session.GetKeyInformationAsync();
                    Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Sig]);

                    var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                    Assert.IsType<EcAttributes>(attrs);
                    Assert.Equal(CurveOid.Ed25519, ((EcAttributes)attrs).Oid);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            });

    // ── X25519 Import (Decryption slot) ─────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_X25519_ToDec_KeyInfoShowsImported(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // X25519 private key is 32 bytes
                var privateKey = RandomNumberGenerator.GetBytes(32);

                try
                {
                    var template = new EcKeyTemplate(KeyRef.Dec, privateKey);
                    var attributes = EcAttributes.Create(KeyRef.Dec, CurveOid.X25519);
                    await session.PutKeyAsync(KeyRef.Dec, template, attributes);

                    var keyInfo = await session.GetKeyInformationAsync();
                    Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Dec]);

                    var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Dec);
                    Assert.IsType<EcAttributes>(attrs);
                    Assert.Equal(CurveOid.X25519, ((EcAttributes)attrs).Oid);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            });

    // ── EC P-256 Import to Auth slot ────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task ImportEcKey_P256_ToAuth_ThenAuthenticate_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var parameters = ecdsa.ExportParameters(includePrivateParameters: true);

                var privateKey = parameters.D!;
                var publicKey = new byte[1 + parameters.Q.X!.Length + parameters.Q.Y!.Length];
                publicKey[0] = 0x04;
                parameters.Q.X.CopyTo(publicKey, 1);
                parameters.Q.Y.CopyTo(publicKey, 1 + parameters.Q.X.Length);

                var template = new EcKeyTemplate(KeyRef.Aut, privateKey, publicKey);
                var attributes = EcAttributes.Create(KeyRef.Aut, CurveOid.Secp256R1);
                await session.PutKeyAsync(KeyRef.Aut, template, attributes);

                var keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.Imported, keyInfo[KeyRef.Aut]);

                // Authenticate with the imported key
                var data = "Auth import test"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin, extended: true);

                var result = await session.AuthenticateAsync(data, HashAlgorithmName.SHA256);
                Assert.True(result.Length > 0);
            });
}

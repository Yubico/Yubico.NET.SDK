// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpAdvancedTests
{
    private const string DefaultUserPin = "123456";
    private const string DefaultAdminPin = "12345678";

    // ── X25519 Key Generation ───────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_X25519_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Dec, CurveOid.X25519);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Dec);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(CurveOid.X25519, ((EcAttributes)attrs).Oid);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Dec);
                Assert.True(pubKey.Length > 0);
            });

    // ── KDF Setup (Iterated-Salted-S2K) ─────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task SetKdf_IterSaltedS2k_ThenVerifyPin_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Generate random salts
                var saltUser = RandomNumberGenerator.GetBytes(8);
                var saltReset = RandomNumberGenerator.GetBytes(8);
                var saltAdmin = RandomNumberGenerator.GetBytes(8);

                // Compute initial hashes for the default PINs
                var kdf = new KdfIterSaltedS2k
                {
                    HashAlgorithm = KdfHashAlgorithm.Sha256,
                    IterationCount = 100000,
                    SaltUser = saltUser,
                    SaltReset = saltReset,
                    SaltAdmin = saltAdmin,
                };

                // Compute initial hashes for default PINs before setting KDF
                var initialHashUser = kdf.Process(Pw.User, DefaultUserPin);
                var initialHashAdmin = kdf.Process(Pw.Admin, DefaultAdminPin);

                try
                {
                    var kdfWithHashes = new KdfIterSaltedS2k
                    {
                        HashAlgorithm = KdfHashAlgorithm.Sha256,
                        IterationCount = 100000,
                        SaltUser = saltUser,
                        SaltReset = saltReset,
                        SaltAdmin = saltAdmin,
                        InitialHashUser = initialHashUser,
                        InitialHashAdmin = initialHashAdmin,
                    };

                    // Set the KDF, then change PINs to their KDF-derived values
                    await session.SetKdfAsync(kdfWithHashes);

                    // Change user PIN: old PIN is raw UTF-8, new PIN will be KDF-processed
                    await session.ChangePinAsync(DefaultUserPin, DefaultUserPin);

                    // Change admin PIN similarly
                    await session.ChangeAdminAsync(DefaultAdminPin, DefaultAdminPin);

                    // Verify the KDF is now active
                    var readKdf = await session.GetKdfAsync();
                    Assert.IsType<KdfIterSaltedS2k>(readKdf);

                    var readS2k = (KdfIterSaltedS2k)readKdf;
                    Assert.Equal(KdfHashAlgorithm.Sha256, readS2k.HashAlgorithm);
                    Assert.Equal(100000, readS2k.IterationCount);

                    // Verify PIN works with KDF active
                    await session.VerifyPinAsync(DefaultUserPin);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(initialHashUser);
                    CryptographicOperations.ZeroMemory(initialHashAdmin);
                }
            });

    // ── RSA 3072 Key Generation ─────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task GenerateRsaKey_3072_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateRsaKeyAsync(KeyRef.Sig, RsaSize.Rsa3072);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(attrs);
                Assert.Equal(3072, ((RsaAttributes)attrs).NLen);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });

    // ── RSA 4096 Key Generation ─────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task GenerateRsaKey_4096_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateRsaKeyAsync(KeyRef.Sig, RsaSize.Rsa4096);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(attrs);
                Assert.Equal(4096, ((RsaAttributes)attrs).NLen);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });

    // ── Ed25519 Key Generation and Sign ─────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_Ed25519_ThenSign_ProducesSignature(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Ed25519);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(CurveOid.Ed25519, ((EcAttributes)attrs).Oid);

                // Ed25519 signatures are 64 bytes
                var message = "Hello Ed25519"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.Equal(64, signature.Length);
            });
}

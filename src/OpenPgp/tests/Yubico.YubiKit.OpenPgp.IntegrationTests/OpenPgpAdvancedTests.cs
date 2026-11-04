// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpAdvancedTests
{
    private static readonly byte[] DefaultUserPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultAdminPin = "12345678"u8.ToArray();

    // ── X25519 Key Generation ───────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_X25519_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateKeyAsync(KeyRef.Dec, EcAttributes.Create(KeyRef.Dec, KeyType.X25519));

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Dec);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(KeyType.X25519, ((EcAttributes)attrs).KeyType);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Dec);
                Assert.True(pubKey.Length > 0);
            });

    // ── KDF Setup (Iterated-Salted-S2K) ─────────────────────────────

    [SkippableTheory]
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
                var initialHashUser = kdf.Process(Pw.User, DefaultUserPin.AsSpan());
                var initialHashAdmin = kdf.Process(Pw.Admin, DefaultAdminPin.AsSpan());

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

                    // Set F9 and retain this KDF instance in the session cache.
                    await session.SetKdfAsync(kdfWithHashes);

                    // ChangePinAsync derives both current and new inputs with that cached KDF.
                    // Equal input values leave this case unable to distinguish a real credential transition.
                    await session.ChangePinAsync(DefaultUserPin, DefaultUserPin);

                    // ChangeAdminAsync likewise derives both inputs.
                    await session.ChangeAdminAsync(DefaultAdminPin, DefaultAdminPin);

                    // GetKdfAsync returns the cached object here; these checks establish cached parameters.
                    var readKdf = await session.GetKdfAsync();
                    Assert.IsType<KdfIterSaltedS2k>(readKdf);

                    var readS2k = (KdfIterSaltedS2k)readKdf;
                    Assert.Equal(KdfHashAlgorithm.Sha256, readS2k.HashAlgorithm);
                    Assert.Equal(100000, readS2k.IterationCount);

                    // Ordinary User verification exercises the active KDF; extended mode/fresh-session
                    // reload and enable/change/disable transitions require separate evidence.
                    await session.VerifyPinAsync(DefaultUserPin);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(initialHashUser);
                    CryptographicOperations.ZeroMemory(initialHashAdmin);
                }
            });

    // ── RSA 3072 Key Generation ─────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task GenerateRsaKey_3072_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa3072));

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(attrs);
                Assert.Equal(3072, ((RsaAttributes)attrs).NLen);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });

    // ── RSA 4096 Key Generation ─────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    [Trait(TestCategories.Category, TestCategories.Slow)]
    public async Task GenerateRsaKey_4096_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateKeyAsync(KeyRef.Sig, RsaAttributes.Create(RsaSize.Rsa4096));

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<RsaAttributes>(attrs);
                Assert.Equal(4096, ((RsaAttributes)attrs).NLen);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);
            });

    // ── Ed25519 Key Generation and Sign ─────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_Ed25519_ThenSign_ProducesSignature(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateKeyAsync(KeyRef.Sig, EcAttributes.Create(KeyRef.Sig, KeyType.Ed25519));

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(KeyType.Ed25519, ((EcAttributes)attrs).KeyType);

                // Ed25519 signatures are 64 bytes
                var message = "Hello Ed25519"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA256);
                Assert.Equal(64, signature.Length);
            });
}
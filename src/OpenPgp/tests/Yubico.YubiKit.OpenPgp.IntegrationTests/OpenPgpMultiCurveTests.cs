// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpMultiCurveTests
{
    private static readonly byte[] DefaultUserPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultAdminPin = "12345678"u8.ToArray();

    // ── P-384 Key Generation and Signing ────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_P384_ThenSign_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp384R1);

                // Verify algorithm attributes were updated
                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(CurveOid.Secp384R1, ((EcAttributes)attrs).Oid);

                // Read back the public key
                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);

                // Sign with P-384 key using SHA-384
                var message = "Hello P-384 OpenPGP"u8.ToArray();
                await session.VerifyPinAsync(DefaultUserPin);

                var signature = await session.SignAsync(message, HashAlgorithmName.SHA384);
                Assert.True(signature.Length > 0);
            });

    // ── Brainpool P-256 R1 Key Generation ───────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateEcKey_BrainpoolP256R1_Succeeds(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.BrainpoolP256R1);

                var attrs = await session.GetAlgorithmAttributesAsync(KeyRef.Sig);
                Assert.IsType<EcAttributes>(attrs);
                Assert.Equal(CurveOid.BrainpoolP256R1, ((EcAttributes)attrs).Oid);

                var pubKey = await session.GetPublicKeyAsync(KeyRef.Sig);
                Assert.True(pubKey.Length > 0);

                // Verify key information shows generated status
                var keyInfo = await session.GetKeyInformationAsync();
                Assert.Equal(KeyStatus.Generated, keyInfo[KeyRef.Sig]);
            });

    // ── PIN Attempt Limit Configuration ─────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SetPinAttempts_CustomValues_ReflectedInStatus(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // SetPinAttempts requires Admin PIN verification
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Set a reset code so the card tracks reset attempts
                await session.SetResetCodeAsync("12345678"u8.ToArray());

                // Set custom retry counts: user=5, reset=3, admin=5
                await session.SetPinAttemptsAsync(5, 3, 5);

                // Read back and verify the new attempt limits
                var status = await session.GetPinStatusAsync();
                Assert.Equal(5, status.AttemptsUser);
                Assert.Equal(3, status.AttemptsReset);
                Assert.Equal(5, status.AttemptsAdmin);
            });
}

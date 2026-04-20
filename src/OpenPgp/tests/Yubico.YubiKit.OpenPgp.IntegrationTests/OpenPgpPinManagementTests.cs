// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.OpenPgp.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.OpenPgp.IntegrationTests;

public class OpenPgpPinManagementTests
{
    private static readonly byte[] DefaultUserPin = "123456"u8.ToArray();
    private static readonly byte[] DefaultAdminPin = "12345678"u8.ToArray();

    // ── PIN Reset via Reset Code ────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ResetPin_ViaResetCode_RestoresAccess(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // Admin must be verified to set the reset code
                await session.VerifyAdminAsync(DefaultAdminPin);

                byte[] resetCode = "12345678"u8.ToArray();
                await session.SetResetCodeAsync(resetCode);

                // Block the User PIN by exhausting all attempts
                for (var i = 0; i < 3; i++)
                {
                    try
                    {
                        await session.VerifyPinAsync("000000"u8.ToArray());
                    }
                    catch (ApduException)
                    {
                        // Expected: wrong PIN
                    }
                }

                // Confirm PIN is blocked
                var status = await session.GetPinStatusAsync();
                Assert.Equal(0, status.AttemptsUser);

                // Reset PIN using the reset code
                byte[] newPin = "654321"u8.ToArray();
                await session.ResetPinAsync(resetCode, newPin);

                // Verify the new PIN works
                await session.VerifyPinAsync(newPin);
            });

    // ── PIN Unverification ──────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.0")]
    public async Task UnverifyPin_ClearsVerificationState(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // Generate a signing key so we can test PIN-gated operations
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1);

                // Verify PIN for signing
                await session.VerifyPinAsync(DefaultUserPin);

                // Signing should succeed while PIN is verified
                var message = "Before unverify"u8.ToArray();
                var signature = await session.SignAsync(message, System.Security.Cryptography.HashAlgorithmName.SHA256);
                Assert.True(signature.Length > 0);

                // Unverify PIN — clears verification state
                await session.UnverifyPinAsync();

                // Signing should now fail because PIN is no longer verified
                await Assert.ThrowsAsync<ApduException>(
                    () => session.SignAsync(
                        "After unverify"u8.ToArray(),
                        System.Security.Cryptography.HashAlgorithmName.SHA256));
            });

    // ── Signature PIN Policy ────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task SetSignaturePinPolicy_ChangesPolicy(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // Default policy after reset should be "Once"
                var statusBefore = await session.GetPinStatusAsync();
                Assert.Equal(PinPolicy.Once, statusBefore.SignaturePinPolicy);

                // Change to "Always" (require PIN before every signature)
                await session.VerifyAdminAsync(DefaultAdminPin);
                await session.SetSignaturePinPolicyAsync(PinPolicy.Always);

                // Read back and verify
                var statusAfter = await session.GetPinStatusAsync();
                Assert.Equal(PinPolicy.Always, statusAfter.SignaturePinPolicy);

                // Restore to "Once"
                await session.SetSignaturePinPolicyAsync(PinPolicy.Once);

                var statusRestored = await session.GetPinStatusAsync();
                Assert.Equal(PinPolicy.Once, statusRestored.SignaturePinPolicy);
            });

    // ── Admin Requirement Validation ────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.2.0")]
    public async Task GenerateKey_WithoutAdminAuth_Fails(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // Attempt key generation without verifying Admin PIN
                // The card should reject the operation with a security status error
                await Assert.ThrowsAsync<ApduException>(
                    () => session.GenerateEcKeyAsync(KeyRef.Sig, CurveOid.Secp256R1));
            });

    // ── Reset PIN via Admin PIN ─────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard)]
    public async Task ResetPin_ViaAdminPin_RestoresAccess(YubiKeyTestState state) =>
        await state.WithOpenPgpSessionAsync(
            resetBeforeUse: true,
            action: async session =>
            {
                // Block the User PIN by exhausting all attempts
                for (var i = 0; i < 3; i++)
                {
                    try
                    {
                        await session.VerifyPinAsync("000000"u8.ToArray());
                    }
                    catch (ApduException)
                    {
                        // Expected: wrong PIN
                    }
                }

                // Confirm PIN is blocked
                var status = await session.GetPinStatusAsync();
                Assert.Equal(0, status.AttemptsUser);

                // Verify Admin PIN first, then reset User PIN via admin privilege.
                // Per OpenPGP spec, RESET RETRY COUNTER with P1=0x02 requires
                // that Admin PIN (PW3) has been verified beforehand.
                await session.VerifyAdminAsync(DefaultAdminPin);

                // Use a PIN that satisfies PIN complexity requirements (FW 5.7+
                // requires at least 2 unique characters; "111111" would fail with
                // SW=0x6985 "Conditions of use not satisfied").
                byte[] newPin = "654321"u8.ToArray();
                await session.ResetPinAsync(DefaultAdminPin, newPin, useAdmin: true);

                // Verify the new PIN works
                await session.VerifyPinAsync(newPin);
            });
}

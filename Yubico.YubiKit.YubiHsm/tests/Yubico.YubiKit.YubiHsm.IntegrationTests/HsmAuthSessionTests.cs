// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiHsm.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiHsm.IntegrationTests;

/// <summary>
///     Integration tests for the YubiHSM Auth applet.
///     All tests require a physical YubiKey with firmware 5.4.3+.
/// </summary>
public class HsmAuthSessionTests
{
    private static readonly CancellationTokenSource Cts = new(TimeSpan.FromSeconds(60));

    // Default management key (all zeros)
    private static readonly byte[] DefaultManagementKey = new byte[16];

    private const string TestLabel = "test-credential";
    private const string TestPassword = "password";

    // ─── Reset and List ──────────────────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task ResetAsync_ThenList_ReturnsEmptyList(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                var credentials = await session.ListCredentialsAsync(Cts.Token);
                Assert.Empty(credentials);
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Put Symmetric → List → Delete → List ───────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task PutSymmetric_ListDeleteList_RoundTrip(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Put a symmetric credential
                var keyEnc = RandomNumberGenerator.GetBytes(16);
                var keyMac = RandomNumberGenerator.GetBytes(16);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        keyEnc,
                        keyMac,
                        TestPassword,
                        cancellationToken: Cts.Token);

                    // List should contain exactly one credential
                    var credentials = await session.ListCredentialsAsync(Cts.Token);
                    Assert.Single(credentials);
                    Assert.Equal(TestLabel, credentials[0].Label);
                    Assert.Equal(HsmAuthAlgorithm.Aes128YubicoAuthentication, credentials[0].Algorithm);

                    // Delete it
                    await session.DeleteCredentialAsync(DefaultManagementKey, TestLabel, Cts.Token);

                    // List should now be empty
                    var credentialsAfterDelete = await session.ListCredentialsAsync(Cts.Token);
                    Assert.Empty(credentialsAfterDelete);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                }
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Put Derived → Verify Algorithm ──────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task PutDerived_VerifyAlgorithm_IsAes128(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                await session.PutCredentialDerivedAsync(
                    DefaultManagementKey,
                    TestLabel,
                    "my-derivation-password",
                    TestPassword,
                    cancellationToken: Cts.Token);

                var credentials = await session.ListCredentialsAsync(Cts.Token);
                Assert.Single(credentials);
                Assert.Equal(HsmAuthAlgorithm.Aes128YubicoAuthentication, credentials[0].Algorithm);
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Change Management Key Round-Trip ────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task PutManagementKey_RoundTrip_CanUseNewKey(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                var newKey = RandomNumberGenerator.GetBytes(16);
                try
                {
                    // Change from default to new key
                    await session.PutManagementKeyAsync(DefaultManagementKey, newKey, Cts.Token);

                    // Use the new key to store a credential (proves the key was changed)
                    var keyEnc = RandomNumberGenerator.GetBytes(16);
                    var keyMac = RandomNumberGenerator.GetBytes(16);
                    try
                    {
                        await session.PutCredentialSymmetricAsync(
                            newKey,
                            TestLabel,
                            keyEnc,
                            keyMac,
                            TestPassword,
                            cancellationToken: Cts.Token);

                        var credentials = await session.ListCredentialsAsync(Cts.Token);
                        Assert.Single(credentials);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(keyEnc);
                        CryptographicOperations.ZeroMemory(keyMac);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(newKey);
                }
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Get Retries == 8 After Reset ────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task GetManagementKeyRetries_AfterReset_Returns8(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                var retries = await session.GetManagementKeyRetriesAsync(Cts.Token);
                Assert.Equal(8, retries);
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Calculate Session Keys Symmetric → 48 bytes ─────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task CalculateSessionKeysSymmetric_Returns48Bytes(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Store a symmetric credential first
                var keyEnc = RandomNumberGenerator.GetBytes(16);
                var keyMac = RandomNumberGenerator.GetBytes(16);
                var context = RandomNumberGenerator.GetBytes(32);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        keyEnc,
                        keyMac,
                        TestPassword,
                        cancellationToken: Cts.Token);

                    // Calculate session keys
                    using var keys = await session.CalculateSessionKeysSymmetricAsync(
                        TestLabel,
                        context,
                        TestPassword,
                        cancellationToken: Cts.Token);

                    // Each key is 16 bytes
                    Assert.Equal(16, keys.SEnc.Length);
                    Assert.Equal(16, keys.SMac.Length);
                    Assert.Equal(16, keys.SRmac.Length);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                    CryptographicOperations.ZeroMemory(context);
                }
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Version-Gated: Generate Asymmetric + Get Public Key (5.6.0+) ────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.0")]
    public async Task GenerateAsymmetric_GetPublicKey_Returns65Bytes(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                await session.GenerateCredentialAsymmetricAsync(
                    DefaultManagementKey,
                    TestLabel,
                    TestPassword,
                    cancellationToken: Cts.Token);

                var credentials = await session.ListCredentialsAsync(Cts.Token);
                Assert.Single(credentials);
                Assert.Equal(HsmAuthAlgorithm.EcP256YubicoAuthentication, credentials[0].Algorithm);

                // Get public key - should be 65 bytes (uncompressed EC point)
                var publicKey = await session.GetPublicKeyAsync(TestLabel, Cts.Token);
                Assert.Equal(65, publicKey.Length);
                Assert.Equal(0x04, publicKey.Span[0]); // Uncompressed point marker
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Version-Gated: Change Credential Password (5.8.0+) ─────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.8.0")]
    public async Task ChangeCredentialPassword_ThenCalculate_Succeeds(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Store a credential with the original password
                var keyEnc = RandomNumberGenerator.GetBytes(16);
                var keyMac = RandomNumberGenerator.GetBytes(16);
                var context = RandomNumberGenerator.GetBytes(32);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        keyEnc,
                        keyMac,
                        TestPassword,
                        cancellationToken: Cts.Token);

                    // Change the password
                    const string newPassword = "new-password";
                    await session.ChangeCredentialPasswordAsync(
                        TestLabel,
                        TestPassword,
                        newPassword,
                        Cts.Token);

                    // Calculate session keys with the new password should succeed
                    using var keys = await session.CalculateSessionKeysSymmetricAsync(
                        TestLabel,
                        context,
                        newPassword,
                        cancellationToken: Cts.Token);

                    Assert.Equal(16, keys.SEnc.Length);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                    CryptographicOperations.ZeroMemory(context);
                }
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);

    // ─── Touch-Required Tests ────────────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    [Trait("Category", "RequiresUserPresence")]
    public async Task PutSymmetric_WithTouch_CredentialListShowsTouchRequired(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                var keyEnc = RandomNumberGenerator.GetBytes(16);
                var keyMac = RandomNumberGenerator.GetBytes(16);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        keyEnc,
                        keyMac,
                        TestPassword,
                        touchRequired: true,
                        cancellationToken: Cts.Token);

                    var credentials = await session.ListCredentialsAsync(Cts.Token);
                    Assert.Single(credentials);
                    Assert.True(credentials[0].TouchRequired);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                }
            },
            resetBeforeUse: true,
            cancellationToken: Cts.Token);
}

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
    // Per-test CancellationToken — do not use a static CTS (shared state causes
    // cancellation after cumulative timeout, breaking later tests in the suite).
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    // Default management key (all zeros after reset)
    private static ReadOnlyMemory<byte> DefaultManagementKey => new byte[16];

    // A management key that passes complexity requirements on Enhanced PIN devices.
    // Note: alpha/beta firmware has non-standard complexity checks — this specific
    // value is known to pass on 5.8.0-alpha YubiKeys. On production firmware,
    // any 16-byte non-zero key should work.
    private static ReadOnlyMemory<byte> ComplexManagementKey => new byte[]
    {
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
    };

    private const string TestLabel = "test-credential";
    private const string TestPassword = "password";

    // ─── Reset and List ──────────────────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task ResetAsync_ThenList_ReturnsEmptyList(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                var credentials = await session.ListCredentialsAsync(NewToken());
                Assert.Empty(credentials);
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

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
                        cancellationToken: NewToken());

                    // List should contain exactly one credential
                    var credentials = await session.ListCredentialsAsync(NewToken());
                    Assert.Single(credentials);
                    Assert.Equal(TestLabel, credentials[0].Label);
                    Assert.Equal(HsmAuthAlgorithm.Aes128YubicoAuthentication, credentials[0].Algorithm);

                    // Delete it
                    await session.DeleteCredentialAsync(DefaultManagementKey, TestLabel, NewToken());

                    // List should now be empty
                    var credentialsAfterDelete = await session.ListCredentialsAsync(NewToken());
                    Assert.Empty(credentialsAfterDelete);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyEnc);
                    CryptographicOperations.ZeroMemory(keyMac);
                }
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

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
                    cancellationToken: NewToken());

                var credentials = await session.ListCredentialsAsync(NewToken());
                Assert.Single(credentials);
                Assert.Equal(HsmAuthAlgorithm.Aes128YubicoAuthentication, credentials[0].Algorithm);
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

    // ─── Change Management Key Round-Trip ────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task PutManagementKey_RoundTrip_CanUseNewKey(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Use ComplexManagementKey — random bytes may fail PIN complexity
                // on Enhanced PIN devices that enforce entropy requirements.
                var newKey = ComplexManagementKey.ToArray();
                try
                {
                    // Change from default to new key
                    await session.PutManagementKeyAsync(DefaultManagementKey, newKey, NewToken());

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
                            cancellationToken: NewToken());

                        var credentials = await session.ListCredentialsAsync(NewToken());
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
            cancellationToken: NewToken());

    // ─── Get Retries == 8 After Reset ────────────────────────────────────────────

    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.4.3")]
    public async Task GetManagementKeyRetries_AfterReset_Returns8(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                var retries = await session.GetManagementKeyRetriesAsync(NewToken());
                Assert.Equal(8, retries);
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

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
                var context = RandomNumberGenerator.GetBytes(16);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        keyEnc,
                        keyMac,
                        TestPassword,
                        cancellationToken: NewToken());

                    // Calculate session keys
                    using var keys = await session.CalculateSessionKeysSymmetricAsync(
                        TestLabel,
                        context,
                        TestPassword,
                        cancellationToken: NewToken());

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
            cancellationToken: NewToken());

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
                    cancellationToken: NewToken());

                var credentials = await session.ListCredentialsAsync(NewToken());
                Assert.Single(credentials);
                Assert.Equal(HsmAuthAlgorithm.EcP256YubicoAuthentication, credentials[0].Algorithm);

                // Get public key - should be 65 bytes (uncompressed EC point)
                var publicKey = await session.GetPublicKeyAsync(TestLabel, NewToken());
                Assert.Equal(65, publicKey.Length);
                Assert.Equal(0x04, publicKey.Span[0]); // Uncompressed point marker
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

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
                var context = RandomNumberGenerator.GetBytes(16);

                try
                {
                    await session.PutCredentialSymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        keyEnc,
                        keyMac,
                        TestPassword,
                        cancellationToken: NewToken());

                    // Change the password
                    const string newPassword = "new-password";
                    await session.ChangeCredentialPasswordAsync(
                        TestLabel,
                        TestPassword,
                        newPassword,
                        NewToken());

                    // Calculate session keys with the new password should succeed
                    using var keys = await session.CalculateSessionKeysSymmetricAsync(
                        TestLabel,
                        context,
                        newPassword,
                        cancellationToken: NewToken());

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
            cancellationToken: NewToken());

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
                        cancellationToken: NewToken());

                    var credentials = await session.ListCredentialsAsync(NewToken());
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
            cancellationToken: NewToken());
}

// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;
using Yubico.YubiKit.YubiHsm.IntegrationTests.TestExtensions;

namespace Yubico.YubiKit.YubiHsm.IntegrationTests;

/// <summary>
///     Integration tests for YubiHSM Auth asymmetric operations.
///     Requires a physical YubiKey with firmware 5.6.0+.
/// </summary>
public class HsmAuthAsymmetricTests
{
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    private static ReadOnlyMemory<byte> DefaultManagementKey => new byte[16];
    private const string TestLabel = "test-asymmetric";
    private static ReadOnlyMemory<byte> TestPassword => "password"u8.ToArray();

    // ─── Put Asymmetric Credential ──────────────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.0")]
    public async Task PutCredentialAsymmetric_ThenList_ShowsEcP256(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Generate an EC P-256 key pair
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var parameters = ecdsa.ExportParameters(includePrivateParameters: true);
                var privateKey = parameters.D!;

                try
                {
                    await session.PutCredentialAsymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        privateKey,
                        TestPassword,
                        cancellationToken: NewToken());

                    var credentials = await session.ListCredentialsAsync(NewToken());
                    Assert.Single(credentials);
                    Assert.Equal(TestLabel, credentials[0].Label);
                    Assert.Equal(HsmAuthAlgorithm.EcP256YubicoAuthentication, credentials[0].Algorithm);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

    // ─── Get Challenge ──────────────────────────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.0")]
    public async Task GetChallenge_WithAsymmetricCredential_ReturnsEpk(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Generate and store an asymmetric credential
                await session.GenerateCredentialAsymmetricAsync(
                    DefaultManagementKey,
                    TestLabel,
                    TestPassword,
                    cancellationToken: NewToken());

                // Get challenge (EPK-OCE for asymmetric credentials)
                var challenge = await session.GetChallengeAsync(
                    TestLabel,
                    TestPassword,
                    NewToken());

                // This assertion checks only nonempty response data; point shape requires another check.
                Assert.True(challenge.Length > 0);
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

    // ─── Calculate Session Keys Asymmetric ──────────────────────────────────────

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.0")]
    public async Task CalculateSessionKeysAsymmetric_WithDummyCryptogram_ThrowsApduException(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                // Generate an asymmetric credential on-device
                await session.GenerateCredentialAsymmetricAsync(
                    DefaultManagementKey,
                    TestLabel,
                    TestPassword,
                    cancellationToken: NewToken());

                // Get the credential's public key (PK-OCE, 65-byte uncompressed EC point)
                var publicKey = await session.GetPublicKeyAsync(TestLabel, NewToken());
                Assert.Equal(65, publicKey.Length);

                // Get EPK-OCE from the YubiKey (ephemeral public key, 65 bytes)
                var epkOce = await session.GetChallengeAsync(
                    TestLabel,
                    TestPassword,
                    NewToken());
                Assert.Equal(65, epkOce.Length);

                // This rejection exercise generates a synthetic HSM ephemeral point locally.
                // A live flow uses the HSM's own EPK-SD and cryptogram, relayed by its connector.
                using var ephemeral = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                var ephemeralParams = ephemeral.ExportParameters(includePrivateParameters: false);
                var epkSd = new byte[65];
                epkSd[0] = 0x04; // Uncompressed point prefix
                ephemeralParams.Q.X!.CopyTo(epkSd.AsSpan(1));
                ephemeralParams.Q.Y!.CopyTo(epkSd.AsSpan(33));

                // Context = EPK-OCE || EPK-SD (130 bytes total)
                var context = new byte[130];
                epkOce.Span.CopyTo(context.AsSpan(0));
                epkSd.CopyTo(context.AsSpan(65));

                // Supply synthetic cryptogram bytes for rejection. The publicKey variable also
                // holds the credential's PK-OCE; a genuine handshake supplies the trusted HSM
                // static key in that argument. Multiple inputs therefore differ from a valid flow.
                var dummyCardCryptogram = new byte[8];
                RandomNumberGenerator.Fill(dummyCardCryptogram);

                try
                {
                    // Assert the exact general APDU exception type for these combined invalid inputs.
                    // A retry-specific subclass fails this exact-type assertion. The result alone
                    // neither isolates cryptogram verification nor proves a valid handshake encoding.
                    var ex = await Assert.ThrowsAsync<ApduException>(async () =>
                    {
                        using var keys = await session.CalculateSessionKeysAsymmetricAsync(
                            TestLabel,
                            context,
                            publicKey,
                            TestPassword,
                            dummyCardCryptogram,
                            NewToken());
                    });

                    // A nonzero status is the only status-word constraint asserted here; an absent
                    // instruction or another invalid input could also account for the rejection.
                    Assert.NotEqual((short)0, ex.SW);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(context);
                    CryptographicOperations.ZeroMemory(epkSd);
                    CryptographicOperations.ZeroMemory(dummyCardCryptogram);
                }
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

    // ─── Put Asymmetric with Touch ──────────────────────────────────────────────
    // This case asserts stored touch metadata; physical CALCULATE/touch enforcement is a separate task.

    [SkippableTheory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.6.0")]
    [Trait("Category", "RequiresUserPresence")]
    public async Task PutCredentialAsymmetric_WithTouch_ShowsTouchRequired(YubiKeyTestState state) =>
        await state.WithHsmAuthSessionAsync(
            async session =>
            {
                using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var parameters = ecdsa.ExportParameters(includePrivateParameters: true);
                var privateKey = parameters.D!;

                try
                {
                    await session.PutCredentialAsymmetricAsync(
                        DefaultManagementKey,
                        TestLabel,
                        privateKey,
                        TestPassword,
                        touchRequired: true,
                        cancellationToken: NewToken());

                    var credentials = await session.ListCredentialsAsync(NewToken());
                    Assert.Single(credentials);
                    Assert.True(credentials[0].TouchRequired);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());
}
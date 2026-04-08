// Copyright 2026 Yubico AB
// Licensed under the Apache License, Version 2.0.

using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.YubiHsm.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.YubiHsm.IntegrationTests;

/// <summary>
///     Integration tests for YubiHSM Auth asymmetric operations.
///     Requires a physical YubiKey with firmware 5.6.0+.
/// </summary>
public class HsmAuthAsymmetricTests
{
    private static CancellationToken NewToken(int timeoutSeconds = 30) =>
        new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;

    private static readonly byte[] DefaultManagementKey = new byte[16];
    private const string TestLabel = "test-asymmetric";
    private const string TestPassword = "password";

    // ─── Put Asymmetric Credential ──────────────────────────────────────────────

    [Theory]
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

    [Theory]
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

                // Challenge should be a non-empty value
                Assert.True(challenge.Length > 0);
            },
            resetBeforeUse: true,
            cancellationToken: NewToken());

    // ─── Calculate Session Keys Asymmetric ──────────────────────────────────────

    [Theory]
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

                // Generate an ephemeral EC P256 key pair for EPK-SD
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

                // Card cryptogram is required for asymmetric CALCULATE. In a real flow,
                // this comes from the YubiHSM 2 device. Without one, we use a dummy value.
                // The YubiKey will reject the dummy data — the exact status word depends on
                // firmware version and how far the firmware gets before detecting the error.
                var dummyCardCryptogram = new byte[8];
                RandomNumberGenerator.Fill(dummyCardCryptogram);

                try
                {
                    // We expect the operation to fail because the card cryptogram is dummy
                    // data and the ephemeral keys don't match a real YubiHSM 2 handshake.
                    // The YubiKey may reject with various status words:
                    //   0x6A80 — incorrect data (firmware validates cryptographic content)
                    //   0x6300 — wrong cryptogram verification
                    //   0x63Cx — credential password retry decrement
                    //   0x6982 — security status not satisfied
                    // All of these are acceptable — they confirm the APDU was received
                    // and processed (not rejected at the transport/framing level).
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

                    // Any APDU-level error is acceptable with dummy cryptographic data.
                    // The key validation is that we get a proper ApduException (not a
                    // serialization error, NullReferenceException, or other internal failure).
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

    [Theory]
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

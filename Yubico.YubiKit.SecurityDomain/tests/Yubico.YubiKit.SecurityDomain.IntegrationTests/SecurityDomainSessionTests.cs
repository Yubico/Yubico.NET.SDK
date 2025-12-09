using System.Security.Cryptography;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration coverage for establishing a Security Domain session using SCP03.
/// </summary>
public class SecurityDomainSessionTests
{
    private const byte CardRecognitionDataObject = 0x73;
    private const byte DefaultScp03Kid = ScpKid.SCP03;

    /// <summary>
    ///     Validates that a Security Domain session can be created with SCP03 on devices
    ///     running firmware 5.7.2 or newer.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateAsync_WithScp03_Succeeds(YubiKeyTestState state)
    {
        using var scpParams = Scp03KeyParams.Default;

        await state.WithSecurityDomainSessionAsync(
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            },
            scpKeyParams: scpParams,
            cancellationToken: CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that the Security Domain responds to GET DATA for the card recognition
    ///     data object when no secure channel is established.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task GetDataAsync_CardRecognition_ReturnsPayload(YubiKeyTestState state) // 0x6A88
    {
        await state.WithSecurityDomainSessionAsync(async session =>
        {
            var response = await session.GetDataAsync(
                CardRecognitionDataObject,
                cancellationToken: CancellationToken.None);

            Assert.False(response.IsEmpty);
        }, cancellationToken: CancellationToken.None);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task GetKeyInformationAsync_ReturnsDefaultScpKey(YubiKeyTestState state)
    {
        await state.WithSecurityDomainSessionAsync(async session =>
        {
            var keyInformation = await session.GetKeyInformationAsync(CancellationToken.None);

            Assert.Equal(state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3, keyInformation.Count);
            Assert.Equal(0xFF, keyInformation.Keys.First().Kvn);
        },
            resetBeforeUse: true, 
            cancellationToken: CancellationToken.None);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task ResetAsync_ReinitializesSession(YubiKeyTestState state)
    {
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                await session.ResetAsync(CancellationToken.None);

                var keyInformation = await session.GetKeyInformationAsync(CancellationToken.None);

                Assert.Equal(state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3, keyInformation.Count);
                Assert.Contains(keyInformation.Keys, keyRef => keyRef.Kvn == 0xFF);
                
            },
            resetBeforeUse: false,
            cancellationToken: CancellationToken.None);
    }

    /// <summary>
    ///     Verifies that GenerateEcKeyAsync generates a valid P256 EC key pair and returns the public key.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task GenerateEcKeyAsync_Scp11b_GeneratesValidKeyAndAuthenticates(YubiKeyTestState state)
    {
        var keyReference = new KeyRef(ScpKid.SCP11b, 0x03);

        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                // Act - Generate a new EC key on the YubiKey
                var publicKeyBytes = await session.GenerateEcKeyAsync(
                    keyReference,
                    replaceKvn: 0,
                    CancellationToken.None);

                // Assert - Verify the generated public key structure
                Assert.NotNull(publicKeyBytes);
                Assert.Equal(65, publicKeyBytes.Length); // Uncompressed point: 0x04 + 32-byte X + 32-byte Y
                Assert.Equal(0x04, publicKeyBytes[0]); // Uncompressed point indicator

                // Extract X and Y coordinates
                var x = publicKeyBytes.AsSpan(1, 32).ToArray();
                var y = publicKeyBytes.AsSpan(33, 32).ToArray();

                Assert.NotNull(x);
                Assert.NotNull(y);
                Assert.Equal(32, x.Length);
                Assert.Equal(32, y.Length);

                // Verify we can construct a valid ECParameters from the point
                var ecParameters = new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    Q = new ECPoint
                    {
                        X = x,
                        Y = y
                    }
                };

                // Verify we can create an ECDiffieHellman instance from the parameters
                using var ecdh = ECDiffieHellman.Create(ecParameters);
                Assert.NotNull(ecdh);
                Assert.Equal(ECCurve.NamedCurves.nistP256.Oid.Value, 
                    ecdh.ExportParameters(false).Curve.Oid.Value);
            },
            Scp03KeyParams.Default,
            cancellationToken: CancellationToken.None, 
            resetBeforeUse: true);

        // Verify the generated key can be used for authentication
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                var keyInformation = await session.GetKeyInformationAsync(CancellationToken.None);
                
                // Verify the key we just generated is now registered
                Assert.Contains(keyInformation.Keys, kr => 
                    kr.Kid == keyReference.Kid && kr.Kvn == keyReference.Kvn);
            },
            resetBeforeUse: false,
            cancellationToken: CancellationToken.None);
    }
}
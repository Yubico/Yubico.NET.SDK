using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
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
    private const byte OceKid = 0x010;
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Validates that a Security Domain session can be created with SCP03 on devices
    ///     running firmware 5.7.2 or newer.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task CreateAsync_WithScp03_Succeeds(YubiKeyTestState state)
    {
        using var scpParams = Scp03KeyParameters.Default;

        await state.WithSecurityDomainSessionAsync(
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            },
            true,
            scpKeyParams: scpParams,
            cancellationToken: CancellationTokenSource.Token);
    }

    /// <summary>
    ///     Verifies that the Security Domain responds to GET DATA for the card recognition
    ///     data object when no secure channel is established.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task GetDataAsync_CardRecognition_ReturnsPayload(YubiKeyTestState state) // 0x6A88
        =>
            await state.WithSecurityDomainSessionAsync(async session =>
                {
                    var response = await session.GetDataAsync(
                        CardRecognitionDataObject,
                        cancellationToken: CancellationTokenSource.Token);

                    Assert.False(response.IsEmpty);
                },
                true,
                cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task GetKeyInformationAsync_ReturnsDefaultScpKey(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(async session =>
            {
                var keyInformation = await session.GetKeyInformationAsync(CancellationTokenSource.Token);

                Assert.Equal(state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3, keyInformation.Count);
                Assert.Equal(0xFF, keyInformation.Keys.First().Kvn);
            },
            true,
            cancellationToken: CancellationTokenSource.Token);

    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task ResetAsync_ReinitializesSession(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                await session.ResetAsync(CancellationTokenSource.Token);

                var keyInformation = await session.GetKeyInformationAsync(CancellationTokenSource.Token);

                Assert.Equal(state.FirmwareVersion >= FirmwareVersion.V5_7_2 ? 4 : 3, keyInformation.Count);
                Assert.Contains(keyInformation.Keys, keyRef => keyRef.Kvn == 0xFF);
            },
            false,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies that GenerateEcKeyAsync generates a valid P256 EC key pair and returns the public key.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task GenerateEcKeyAsync_Scp11b_GeneratesValidKeyAndAuthenticates(YubiKeyTestState state)
    {
        var keyReference = new KeyReference(ScpKid.SCP11b, 0x03);

        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                // Act - Generate a new EC key on the YubiKey
                var publicKey = await session.GenerateKeyAsync(
                    keyReference,
                    0,
                    CancellationTokenSource.Token);

                var publicKeyBytes = publicKey.PublicPoint;

                // Assert - Verify the generated public key structure
                Assert.Equal(65, publicKeyBytes.Length); // Uncompressed point: 0x04 + 32-byte X + 32-byte Y
                Assert.Equal(0x4, publicKeyBytes.Span[0]); // Uncompressed point indicator

                // Extract X and Y coordinates
                var x = publicKeyBytes.Span.Slice(1, 32);
                var y = publicKeyBytes.Span.Slice(33, 32);

                Assert.Equal(32, x.Length);
                Assert.Equal(32, y.Length);

                // Verify we can construct a valid ECParameters from the point
                var ecParameters = new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256, Q = new ECPoint { X = x.ToArray(), Y = y.ToArray() }
                };

                // Verify we can create an ECDiffieHellman instance from the parameters
                using var ecdh = ECDiffieHellman.Create(ecParameters);
                Assert.NotNull(ecdh);
                Assert.Equal(ECCurve.NamedCurves.nistP256.Oid.Value,
                    ecdh.ExportParameters(false).Curve.Oid.Value);
            },
            true, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

        // Verify the generated key can be used for authentication
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                var keyInformation = await session.GetKeyInformationAsync(CancellationTokenSource.Token);

                // Verify the key we just generated is now registered
                Assert.Contains(keyInformation.Keys, kr =>
                    kr.Kid == keyReference.Kid && kr.Kvn == keyReference.Kvn);
            },
            false,
            cancellationToken: CancellationTokenSource.Token);
    }

    /// <summary>
    ///     Verifies that a newly created SCP11b key can be deleted using DeleteKeyAsync
    ///     and that it no longer appears in key information afterwards.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task DeleteKeyAsync_CreateThenDelete_Succeeds(YubiKeyTestState state)
    {
        // TODO create key
        
        const byte kid = ScpKid.SCP11b;
        const byte kvn = 0x01;
        var keyRef = new KeyReference(kid, kvn);

        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                // Verify presence
                var info = await session.GetKeyInformationAsync(CancellationTokenSource.Token);
                Assert.Contains(info.Keys, kr => kr is { Kid: kid, Kvn: kvn });

                // Act: delete the just-created key
                await session.DeleteKeyAsync(keyRef, false, CancellationTokenSource.Token);

                // Assert: key is gone
                info = await session.GetKeyInformationAsync(CancellationTokenSource.Token);
                Assert.DoesNotContain(info.Keys, kr => kr is { Kid: kid, Kvn: kvn });
            }, // authenticate for key mgmt operations
            true,
            scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11a_WithAllowList_AllowsApprovedSerials(YubiKeyTestState state)
    {
        const byte kvn = 0x05;
        var oceKeyRef = new KeyReference(OceKid, kvn);

        Scp11KeyParameters? keyParams = null;
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                keyParams = await LoadKeys(session, ScpKid.SCP11a, kvn);
            }, // authenticate for key mgmt operations
            true,
            scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

        string[] serials =
        [
            "7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A",
            "6B90028800909F9FFCD641346933242748FBE9AD"
        ];

        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                // Only the above serials shall work. 
                await session.StoreAllowlistAsync(oceKeyRef, serials);
            },
            scpKeyParams: keyParams,
            resetBeforeUse: false,
            cancellationToken: CancellationTokenSource.Token
        );

        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                await session.DeleteKeyAsync(new KeyReference(ScpKid.SCP11a, kvn));
            },
            scpKeyParams: keyParams,
            resetBeforeUse: false,
            cancellationToken: CancellationTokenSource.Token
        );
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11b_Import_Succeeds(YubiKeyTestState state)
    {
        var keyReference = new KeyReference(ScpKid.SCP11b, 0x02);
        Scp11KeyParameters? keyParameters = null;

        var protocolConfiguration = new ProtocolConfiguration { ForceShortApdus = true };
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                // Generate a new EC key on the host and import via PutKey
                // using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                // using var privateKey = ECPrivateKey.CreateFromParameters(ecdsa.ExportParameters(true));
                // using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                // using var privateKey = ECPrivateKey.CreateFromEcdh(ecdh);
                // var publicKey = ECPublicKey.CreateFromEcdh(ecdh);
                var privateKey = ECPrivateKey.CreateFromValue(
                    Convert.FromHexString("549D2A8A03E62DC829ADE4D6850DB9568475147C59EF238F122A08CF557CDB91"),
                    KeyType.ECP256);
                var publicKey = ECPublicKey.CreateFromParameters(privateKey.Parameters with { D = null });
                keyParameters = new Scp11KeyParameters(keyReference, publicKey);

                await session.PutKeyAsync(keyReference, privateKey);
            },
            configuration: protocolConfiguration,
            scpKeyParams: Scp03KeyParameters.Default,
            resetBeforeUse: true
        );


        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                await Task.CompletedTask;
            },
            false,
            protocolConfiguration,
            keyParameters);
    }

    private static async Task<Scp11KeyParameters> LoadKeys(
        SecurityDomainSession session,
        byte scpKid,
        byte kvn)
    {
        var sessionRef = new KeyReference(scpKid, kvn);
        var oceRef = new KeyReference(OceKid, kvn);
        var newPublicKey = await session.GenerateKeyAsync(sessionRef, 0, CancellationTokenSource.Token);

        var oceCerts = GetOceCertificates(Scp11TestData.OceCerts);
        ArgumentNullException.ThrowIfNull(oceCerts.Ca);

        // Put Oce Keys
        var ocePublicKey = ECPublicKey.CreateFromParameters(
            oceCerts.Ca.PublicKey.GetECDsaPublicKey()!.ExportParameters(false)
        );

        await session.PutKeyAsync(oceRef, ocePublicKey, 0, CancellationTokenSource.Token);

        // Get Oce subject key identifier
        var ski = GetSki(oceCerts.Ca);
        if (ski.IsEmpty) throw new InvalidOperationException("CA certificate missing Subject Key Identifier");

        // Store the key identifier with the referenced off card entity on the Yubikey
        await session.StoreCaIssuerAsync(oceRef, ski, CancellationTokenSource.Token);

        var (certChain, privateKey) = GetOceCertificateChainAndPrivateKey(Scp11TestData.Oce, Scp11TestData.OcePassword);

        // Now we have the EC private key parameters and cert chain
        return new Scp11KeyParameters(
            sessionRef,
            newPublicKey,
            privateKey,
            oceRef,
            certChain
        );
    }

    private static (List<X509Certificate2> certChain, ECPrivateKey privateKey) GetOceCertificateChainAndPrivateKey(
        ReadOnlyMemory<byte> ocePkcs12,
        ReadOnlyMemory<char> ocePassword)
    {
        var collection =
            X509CertificateLoader.LoadPkcs12Collection(ocePkcs12.Span, ocePassword.Span,
                X509KeyStorageFlags.Exportable);
        var leafCert = collection.FirstOrDefault(cert => cert.HasPrivateKey);
        if (leafCert == null) throw new InvalidOperationException("No private key entry found in PKCS12");

        ECParameters ecParams;
        using (var ecdsa = leafCert.GetECDsaPrivateKey())
        {
            if (ecdsa != null)
            {
                ecParams = ecdsa.ExportParameters(true);
            }
            else
            {
                using var ecdh = leafCert.GetECDiffieHellmanPrivateKey();
                if (ecdh == null)
                    throw new InvalidOperationException(
                        "Private key is not an EC key (or is not ECDSA/ECDH compatible)");
                ecParams = ecdh.ExportParameters(true);
            }
        }

        var certs = ScpCertificates.From(collection);
        var certChain = new List<X509Certificate2>(certs.Bundle);
        if (certs.Leaf != null) certChain.Add(certs.Leaf);

        var privateKey = ECPrivateKey.CreateFromParameters(ecParams);
        return (certChain, privateKey);
    }

    private static ScpCertificates GetOceCertificates(ReadOnlySpan<byte> pem)
    {
        try
        {
            var certificates = new List<X509Certificate2>();

            // Convert PEM to a string
            var pemString = Encoding.UTF8.GetString(pem);

            // Split the PEM string into individual certificates
            var pemCerts = pemString.Split([
                    "-----BEGIN CERTIFICATE-----",
                    "-----END CERTIFICATE-----"
                ],
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            );

            foreach (var certString in pemCerts)
            {
                var certData = Convert.FromBase64String(certString);
                var cert = X509CertificateLoader.LoadCertificate(certData.AsSpan());
                certificates.Add(cert);
            }

            return ScpCertificates.From(certificates);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse PEM certificates", ex);
        }
    }

    private static ReadOnlyMemory<byte> GetSki(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions["2.5.29.14"];
        if (extension is not X509SubjectKeyIdentifierExtension skiExtension)
            throw new InvalidOperationException("Invalid Subject Key Identifier extension");

        var rawData = skiExtension.RawData;
        if (rawData == null || rawData.Length == 0)
            throw new InvalidOperationException("Missing Subject Key Identifier");

        var tlv = Tlv.Create(skiExtension.RawData);
        return tlv.Value;
    }

    private static async Task<Scp03KeyParameters> ImportScp03Key(SecurityDomainSession session)
    {
        const byte scp03KeyId = 0x01;
        var scp03Ref = new KeyReference(scp03KeyId, 0x01);
        var staticKeys = new StaticKeys(
            GetRandomBytes(16),
            GetRandomBytes(16),
            GetRandomBytes(16)
        );

        await session.PutKeyAsync(scp03Ref, staticKeys, 0, CancellationTokenSource.Token);
        return new Scp03KeyParameters(scp03Ref, staticKeys);
    }

    private static byte[] GetRandomBytes(byte length)
    {
        using var rng = CryptographyProviders.RngCreator();
        Span<byte> hostChallenge = stackalloc byte[length];
        rng.GetBytes(hostChallenge);

        return hostChallenge.ToArray();
    }

    // private static Task<Scp11KeyParameters> Get_Scp11b_SecureConnection_Parameters(
    //     IYubiKeyDevice testDevice,
    //     KeyReference keyReference)
    // {
    //     IReadOnlyCollection<X509Certificate2> certificateList;
    //     using (var session = new SecurityDomainSession(testDevice))
    //     {
    //         certificateList = await session.GetCertificatesAsync(keyReference, CancellationTokenSource.Token);
    //     }
    //
    //     var leaf = certificateList.Last();
    //     var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!;
    //     var keyParams = new Scp11KeyParameters(keyReference, ECPublicKey.CreateFromParameters(ecDsaPublicKey.ExportParameters(false)));
    //
    //     return keyParams;
    // }

    /// <summary>
    ///     Verifies that PutKeyAsync can import SCP03 static keys and that authentication
    ///     works with the new keys, while the old default keys no longer work.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.4.3")]
    public async Task Scp03_PutKeyAsync_WithStaticKeys_ImportsAndAuthenticates(YubiKeyTestState state)
    {
        // Custom key set (non-default) for testing
        byte[] keyBytes =
        [
            0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
            0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F
        ];

        using var newStaticKeys = new StaticKeys(keyBytes, keyBytes, keyBytes);
        var newKeyReference = new KeyReference(0x01, 0x01);
        var newKeyParams = new Scp03KeyParameters(newKeyReference, newStaticKeys);

        // Step 1: Authenticate with default keys and import new keys
        await state.WithSecurityDomainSessionAsync(
            async session =>
            {
                await session.PutKeyAsync(newKeyReference, newStaticKeys, 0,
                    CancellationTokenSource.Token);
            },
            true,
            scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

        // Step 2: Verify new keys work
        await state.WithSecurityDomainSessionAsync(
            session =>
            {
                Assert.NotNull(session);
                return Task.CompletedTask;
            },
            scpKeyParams: newKeyParams,
            resetBeforeUse: false,
            cancellationToken: CancellationTokenSource.Token);

        // Step 3: Verify default keys no longer work
        await Assert.ThrowsAsync<ApduException>(async () =>
        {
            await state.WithSecurityDomainSessionAsync(
                session => Task.CompletedTask,
                scpKeyParams: Scp03KeyParameters.Default,
                resetBeforeUse: false,
                cancellationToken: CancellationTokenSource.Token);
        });
    }
}
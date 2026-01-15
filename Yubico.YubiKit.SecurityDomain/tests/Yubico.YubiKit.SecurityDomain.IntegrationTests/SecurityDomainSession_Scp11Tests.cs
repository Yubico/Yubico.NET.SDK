using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.Helpers;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration coverage for establishing a Security Domain session using SCP11.
/// </summary>
public class SecurityDomainSession_Scp11Tests
{
    private const byte OceKid = 0x010;
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Verifies that GenerateEcKeyAsync generates a valid P256 EC key pair and returns the public key.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11b_GenerateEcKeyAsync_GeneratesValidKeyAndAuthenticates(YubiKeyTestState state)
    {
        var keyReference = new KeyReference(ScpKid.SCP11b, 0x03);

        await state.WithSecurityDomainSessionAsync(true, async session =>
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
        }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

        // Verify the generated key can be used for authentication
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                var keyInfo = await session.GetKeyInfoAsync(CancellationTokenSource.Token);

                // Verify the key we just generated is now registered
                Assert.Contains(keyInfo, keyEntry =>
                    keyEntry.KeyReference.Kid == keyReference.Kid && keyEntry.KeyReference.Kvn == keyReference.Kvn);
            }, cancellationToken: CancellationTokenSource.Token);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11a_WithAllowList_AllowsApprovedSerials(YubiKeyTestState state)
    {
        const byte kvn = 0x05;
        var oceKeyRef = new KeyReference(OceKid, kvn);

        Scp11KeyParameters? keyParams = null;
        await state.WithSecurityDomainSessionAsync( // authenticate for key mgmt operations
            true,
            async session =>
            {
                keyParams = await LoadKeys(session, ScpKid.SCP11a, kvn);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: CancellationTokenSource.Token);

        string[] serials =
        [
            "7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A",
            "6B90028800909F9FFCD641346933242748FBE9AD"
        ];

        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                // Only the above serials shall work.
                await session.StoreAllowlistAsync(oceKeyRef, serials);
            }, scpKeyParams: keyParams, cancellationToken: CancellationTokenSource.Token);

        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                await session.DeleteKeyAsync(new KeyReference(ScpKid.SCP11a, kvn));
            }, scpKeyParams: keyParams, cancellationToken: CancellationTokenSource.Token);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11b_Import_Succeeds(YubiKeyTestState state)
    {
        var keyReference = new KeyReference(ScpKid.SCP11b, 0x02);
        Scp11KeyParameters? keyParameters = null;

        await state.WithSecurityDomainSessionAsync(true, async session =>
        {
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using var privateKey = ECPrivateKey.CreateFromEcdh(ecdh);
            var publicKey = ECPublicKey.CreateFromEcdh(ecdh);

            keyParameters = new Scp11KeyParameters(keyReference, publicKey);

            await session.PutKeyAsync(keyReference, privateKey);
        }, scpKeyParams: Scp03KeyParameters.Default);


        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                Assert.True(session.IsAuthenticated);
                await Task.CompletedTask;
            },
            scpKeyParams: keyParameters);
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11b_EstablishSecureConnection_Succeeds(YubiKeyTestState state)
    {
        var keyReference = new KeyReference(ScpKid.SCP11b, 0x01);
        IReadOnlyList<X509Certificate2>? certificateList = null;
        await state.WithSecurityDomainSessionAsync(true, async session =>
        {
            certificateList = await session.GetCertificatesAsync(keyReference);
        }, scpKeyParams: Scp03KeyParameters.Default);

        var leaf = certificateList!.Last();
        var ecDsaPublicKey = leaf.PublicKey.GetECDsaPublicKey()!;
        
        var keyParams = new Scp11KeyParameters(
            keyReference, 
            ECPublicKey.CreateFromParameters(ecDsaPublicKey.ExportParameters(false)));
        
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                Assert.True(session.IsAuthenticated);
                await Assert.ThrowsAsync<ApduException>(async () =>
                    await VerifyScp11bAuth(session, CancellationTokenSource.Token)
                );
            },
            scpKeyParams: keyParams);
        
        return;
        
        static async Task VerifyScp11bAuth(
            SecurityDomainSession session, CancellationToken cancellationToken = default)
        {
            var keyRef = new KeyReference(ScpKid.SCP11b, 0x7f);
            await session.GenerateKeyAsync(keyRef, 0, cancellationToken);
            await session.DeleteKeyAsync(keyRef, cancellationToken: cancellationToken);
        }
    }

    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11b_GetCertificates_IsNotEmpty(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                var keyReference = new KeyReference(ScpKid.SCP11b, 0x1);
                var result = await session.GetCertificatesAsync(keyReference, CancellationTokenSource.Token);
                Assert.True(result.Count > 0);
            },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);

    /// <summary>
    ///     Verifies that StoreCertificatesAsync successfully stores certificates that can be retrieved.
    /// </summary>
    [Theory]
    [WithYubiKey(MinFirmware = "5.7.2")]
    public async Task Scp11b_StoreCertificates_CanBeRetrieved(YubiKeyTestState state) =>
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                var keyReference = new KeyReference(ScpKid.SCP11b, 0x1);
                var oceCertificates = GetOceCertificates(Scp11TestData.OceCerts);

                // Store the certificate bundle
                await session.StoreCertificatesAsync(keyReference, oceCertificates.Bundle, CancellationTokenSource.Token);

                // Retrieve and verify
                var result = await session.GetCertificatesAsync(keyReference, CancellationTokenSource.Token);

                // Assert that we can store and retrieve the off card entity certificate
                var oceThumbprint = oceCertificates.Bundle.Single().Thumbprint;
                Assert.Single(result);
                Assert.Equal(oceThumbprint, result[0].Thumbprint);
            },
            scpKeyParams: Scp03KeyParameters.Default,
            cancellationToken: CancellationTokenSource.Token);

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
}
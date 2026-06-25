// Copyright 2025 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.Helpers;
using Yubico.YubiKit.SecurityDomain.IntegrationTests.TestExtensions;
using Yubico.YubiKit.Tests.Shared;
using Yubico.YubiKit.Tests.Shared.Infrastructure;

namespace Yubico.YubiKit.SecurityDomain.IntegrationTests;

/// <summary>
///     Integration tests for SCP11c authentication.
///     SCP11c provides mutual authentication with certificate chain (variant C).
/// </summary>
public class SecurityDomainSession_Scp11cTests
{
    private const byte OceKid = 0x010;
    private static readonly CancellationTokenSource CancellationTokenSource = new(TimeSpan.FromSeconds(100));

    /// <summary>
    ///     Generates an SCP11c key pair on the device, loads OCE certificates and keys,
    ///     then authenticates using SCP11c key parameters.
    /// </summary>
    [Theory]
    [WithYubiKey(ConnectionType = ConnectionType.SmartCard, MinFirmware = "5.7.2")]
    public async Task Scp11c_GenerateAndAuthenticate_Succeeds(YubiKeyTestState state)
    {
        var ct = CancellationTokenSource.Token;
        const byte kvn = 0x06;
        var oceKeyRef = new KeyReference(OceKid, kvn);

        Scp11KeyParameters? keyParams = null;

        // Session 1: Generate SCP11c key pair and load OCE credentials
        await state.WithSecurityDomainSessionAsync(true,
            async session =>
            {
                keyParams = await LoadScp11cKeys(session, kvn, ct);
            }, scpKeyParams: Scp03KeyParameters.Default, cancellationToken: ct);

        // Session 2: Authenticate using SCP11c and verify session is established
        await state.WithSecurityDomainSessionAsync(false,
            async session =>
            {
                Assert.True(session.IsAuthenticated);

                // Verify we can perform an authenticated operation
                var keyInfo = await session.GetKeyInfoAsync(ct);
                Assert.True(keyInfo.Count > 0);
            }, scpKeyParams: keyParams, cancellationToken: ct);
    }

    private static async Task<Scp11KeyParameters> LoadScp11cKeys(
        SecurityDomainSession session,
        byte kvn,
        CancellationToken cancellationToken)
    {
        var sessionRef = new KeyReference(ScpKid.SCP11c, kvn);
        var oceRef = new KeyReference(OceKid, kvn);

        // Generate EC key pair on device for SCP11c
        var newPublicKey = await session.GenerateKeyAsync(sessionRef, 0, cancellationToken);

        // Load OCE CA certificate and extract its public key
        var oceCerts = GetOceCertificates(Scp11TestData.OceCerts);
        ArgumentNullException.ThrowIfNull(oceCerts.Ca);

        var ocePublicKey = ECPublicKey.CreateFromParameters(
            oceCerts.Ca.PublicKey.GetECDsaPublicKey()!.ExportParameters(false)
        );

        // Import OCE CA public key to the device
        await session.PutKeyAsync(oceRef, ocePublicKey, 0, cancellationToken);

        // Store the CA issuer SKI for the OCE reference
        var ski = GetSki(oceCerts.Ca);
        if (ski.IsEmpty) throw new InvalidOperationException("CA certificate missing Subject Key Identifier");
        await session.StoreCaIssuerAsync(oceRef, ski, cancellationToken);

        // Extract private key and certificate chain from PKCS12
        var (certChain, privateKey) = GetOceCertificateChainAndPrivateKey(
            Scp11TestData.Oce, Scp11TestData.OcePassword);

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
        if (leafCert is null) throw new InvalidOperationException("No private key entry found in PKCS12");

        ECParameters ecParams;
        using (var ecdsa = leafCert.GetECDsaPrivateKey())
        {
            if (ecdsa is not null)
            {
                ecParams = ecdsa.ExportParameters(true);
            }
            else
            {
                using var ecdh = leafCert.GetECDiffieHellmanPrivateKey();
                if (ecdh is null)
                    throw new InvalidOperationException(
                        "Private key is not an EC key (or is not ECDSA/ECDH compatible)");
                ecParams = ecdh.ExportParameters(true);
            }
        }

        var certs = ScpCertificates.From(collection);
        var certChain = new List<X509Certificate2>(certs.Bundle);
        if (certs.Leaf is not null) certChain.Add(certs.Leaf);

        var privateKey = ECPrivateKey.CreateFromParameters(ecParams);
        return (certChain, privateKey);
    }

    private static ScpCertificates GetOceCertificates(ReadOnlySpan<byte> pem)
    {
        var certificates = new List<X509Certificate2>();
        var pemString = Encoding.UTF8.GetString(pem);

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

    private static ReadOnlyMemory<byte> GetSki(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions["2.5.29.14"];
        if (extension is not X509SubjectKeyIdentifierExtension skiExtension)
            throw new InvalidOperationException("Invalid Subject Key Identifier extension");

        var rawData = skiExtension.RawData;
        if (rawData is null || rawData.Length == 0)
            throw new InvalidOperationException("Missing Subject Key Identifier");

        var tlv = Tlv.Create(skiExtension.RawData);
        return tlv.Value;
    }
}

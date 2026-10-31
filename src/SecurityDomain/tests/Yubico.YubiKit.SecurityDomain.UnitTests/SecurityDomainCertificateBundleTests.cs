// Copyright 2026 Yubico AB
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
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Tests.Shared;

namespace Yubico.YubiKit.SecurityDomain.UnitTests;

public sealed class SecurityDomainCertificateBundleTests
{
    [Fact]
    [Trait("Audit", "YESDK-1608")]
    public async Task GetCertificatesAsync_LaterCertificateIsCorrupt_DisposesEveryLoadedCertificateAndThrows()
    {
        byte[] issuer = CreateCertificateData("issuer");
        byte[] leaf = CreateCertificateData("leaf");
        // A valid outer SEQUENCE TLV, but not an X.509 certificate. A valid certificate after it
        // must not be loaded: corrupt entries cannot be skipped to produce an incomplete bundle.
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], [.. issuer, .. leaf, 0x30, 0x01, 0x00, .. issuer, 0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);
        List<X509Certificate2> loaded = [];
        session.CertificateLoadedObserver = certificate =>
        {
            loaded.Add(certificate);
            Assert.NotEqual(IntPtr.Zero, certificate.Handle);
        };

        try
        {
            await Assert.ThrowsAnyAsync<CryptographicException>(() => session.GetCertificatesAsync(
                new KeyReference(ScpKid.SCP11a, 0x01), TestContext.Current.CancellationToken));

            Assert.Equal(2, loaded.Count);
            Assert.All(loaded, certificate => Assert.Equal(IntPtr.Zero, certificate.Handle));
        }
        finally
        {
            // Also release the deliberately leaked certificates when demonstrating RED.
            foreach (var certificate in loaded)
                certificate.Dispose();
        }
    }

    [Fact]
    [Trait("Audit", "YESDK-1608")]
    public async Task GetCertificatesAsync_ValidBundle_TransfersCertificatesInOrderToCaller()
    {
        byte[] issuer = CreateCertificateData("issuer");
        byte[] leaf = CreateCertificateData("leaf");
        var connection = new RecordingSmartCardConnection(
            [0x90, 0x00], [.. issuer, .. leaf, 0x90, 0x00]);
        await using var session = await SecurityDomainSession.CreateAsync(
            connection, cancellationToken: TestContext.Current.CancellationToken);
        List<X509Certificate2> loaded = [];
        session.CertificateLoadedObserver = loaded.Add;

        try
        {
            var certificates = await session.GetCertificatesAsync(
                new KeyReference(ScpKid.SCP11a, 0x01), TestContext.Current.CancellationToken);

            Assert.Equal(2, certificates.Count);
            Assert.Equal(2, loaded.Count);
            Assert.Same(loaded[0], certificates[0]);
            Assert.Same(loaded[1], certificates[1]);
            Assert.Equal(
                [0x00, 0xCA, 0xBF, 0x21, 0x06, 0xA6, 0x04, 0x83, 0x02, 0x11, 0x01, 0x00],
                connection.TransmittedCommands[1]);

            await session.DisposeAsync();

            Assert.All(certificates, certificate => Assert.NotEqual(IntPtr.Zero, certificate.Handle));
            Assert.Equal(issuer, certificates[0].RawData);
            Assert.Equal(leaf, certificates[1].RawData);
        }
        finally
        {
            foreach (var certificate in loaded)
                certificate.Dispose();
        }

        Assert.All(loaded, certificate => Assert.Equal(IntPtr.Zero, certificate.Handle));
    }

    private static byte[] CreateCertificateData(string commonName)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={commonName}", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
        return certificate.RawData;
    }
}
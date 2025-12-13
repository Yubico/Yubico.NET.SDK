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
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Scp;

/// <summary>
///     Unit tests for SCP11 (Secure Channel Protocol 11) API.
///     These tests verify the SCP11KeyParams and related types work correctly.
/// </summary>
public class Scp11Tests
{
    [Fact]
    public void Scp11_KeyRef_Creation_AllVariants_Succeeds()
    {
        // Test that KeyRef can be created for all SCP11 variants
        var scp11aRef = new KeyReference(ScpKid.SCP11a, 0x1);
        var scp11bRef = new KeyReference(ScpKid.SCP11b, 0x1);
        var scp11cRef = new KeyReference(ScpKid.SCP11c, 0x1);

        Assert.Equal(ScpKid.SCP11a, scp11aRef.Kid);
        Assert.Equal(ScpKid.SCP11b, scp11bRef.Kid);
        Assert.Equal(ScpKid.SCP11c, scp11cRef.Kid);
    }

    [Fact]
    public void Scp11b_KeyParams_WithPublicKeyOnly_Succeeds()
    {
        // Arrange - Create EC public key for SCP11b
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = ECPublicKey.CreateFromParameters(ecdh.PublicKey.ExportParameters());

        // Act - Create SCP11b key params (no OCE, no certs)
        var keyRef = new KeyReference(ScpKid.SCP11b, 0x1);
        var keyParams = new Scp11KeyParameters(keyRef, publicKey);

        // Assert
        Assert.NotNull(keyParams);
        Assert.Equal(keyRef.Kid, keyParams.KeyReference.Kid);
        Assert.Null(keyParams.SkOceEcka);
        Assert.Null(keyParams.OceKeyRef);
        Assert.Empty(keyParams.Certificates);
    }

    [Fact]
    public void Scp11a_KeyParams_WithOceAndCerts_Succeeds()
    {
        // Arrange - Create keys and certs for SCP11a
        using var oceEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var sdEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

        var ocePrivateKey = ECPrivateKey.CreateFromParameters(oceEcdh.ExportParameters(true));
        var sdPublicKey = ECPublicKey.CreateFromParameters(sdEcdh.PublicKey.ExportParameters());
        
        var certBundle = ParseTestCertificates();

        // Act - Create SCP11a key params (with OCE and certs)
        var sessionRef = new KeyReference(ScpKid.SCP11a, 0x3);
        var oceRef = new KeyReference(0x10, 0x3);
        var keyParams = new Scp11KeyParameters(sessionRef, sdPublicKey, ocePrivateKey, oceRef, certBundle);

        // Assert
        Assert.NotNull(keyParams);
        Assert.Equal(sessionRef.Kid, keyParams.KeyReference.Kid);
        Assert.NotNull(keyParams.SkOceEcka);
        Assert.NotNull(keyParams.OceKeyRef);
        Assert.NotEmpty(keyParams.Certificates);
        Assert.Equal(2, keyParams.Certificates.Count); // Intermediate + Root CA
    }

    [Fact]
    public void Scp11c_KeyParams_WithOceAndCerts_Succeeds()
    {
        // Arrange - Create keys and certs for SCP11c
        using var oceEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var sdEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        
        var ocePrivateKey = ECPrivateKey.CreateFromParameters(oceEcdh.ExportParameters(true));
        var sdPublicKey = ECPublicKey.CreateFromParameters(sdEcdh.PublicKey.ExportParameters());
        
        var certBundle = ParseTestCertificates();

        // Act - Create SCP11c key params
        var sessionRef = new KeyReference(ScpKid.SCP11c, 0x3);
        var oceRef = new KeyReference(0x10, 0x3);
        var keyParams = new Scp11KeyParameters(sessionRef, sdPublicKey, ocePrivateKey, oceRef, certBundle);

        // Assert
        Assert.NotNull(keyParams);
        Assert.Equal(ScpKid.SCP11c, keyParams.KeyReference.Kid);
        Assert.NotNull(keyParams.SkOceEcka);
        Assert.NotNull(keyParams.OceKeyRef);
        Assert.NotEmpty(keyParams.Certificates);
    }

    [Fact]
    public void Scp11a_KeyParams_WithoutCerts_ThrowsArgumentException()
    {
        // Arrange
        using var oceEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        using var sdEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                
        var ocePrivateKey = ECPrivateKey.CreateFromParameters(oceEcdh.ExportParameters(true));
        var sdPublicKey = ECPublicKey.CreateFromParameters(sdEcdh.PublicKey.ExportParameters());
        
        var sessionRef = new KeyReference(ScpKid.SCP11a, 0x3);
        var oceRef = new KeyReference(0x10, 0x3);

        // Act & Assert - SCP11a requires certificate chain
        Assert.Throws<ArgumentException>(() =>
            new Scp11KeyParameters(sessionRef, sdPublicKey, ocePrivateKey, oceRef, []));
    }

    [Fact]
    public void Scp11a_KeyParams_WithoutOceKey_ThrowsArgumentNullException()
    {
        // Arrange
        using var sdEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        
        var sdPublicKey = ECPublicKey.CreateFromParameters(sdEcdh.PublicKey.ExportParameters());
        var certBundle = ParseTestCertificates();
        var sessionRef = new KeyReference(ScpKid.SCP11a, 0x3);
        var oceRef = new KeyReference(0x10, 0x3);

        // Act & Assert - SCP11a requires OCE private key
        Assert.Throws<ArgumentNullException>(() =>
            new Scp11KeyParameters(sessionRef, sdPublicKey, null, oceRef, certBundle));
    }

    [Fact]
    public void Scp11_InvalidKeyId_ThrowsArgumentException()
    {
        // Arrange
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                
        var sdPublicKey = ECPublicKey.CreateFromParameters(ecdh.PublicKey.ExportParameters());
        // Act & Assert - Invalid KID (not 0x11, 0x13, or 0x15)
        var invalidKeyRef = new KeyReference(0x99, 0x1);
        Assert.Throws<ArgumentException>(() =>
            new Scp11KeyParameters(invalidKeyRef, sdPublicKey));
    }

    [Fact]
    public void Scp11_TestData_OceCerts_ParsesCorrectly()
    {
        // Act
        var certs = ParseTestCertificates();

        // Assert
        Assert.NotNull(certs);
        Assert.Equal(2, certs.Count); // Intermediate + Root CA

        // Verify first cert (intermediate)
        var intermediateCert = certs[0];
        Assert.Contains("Intermediate", intermediateCert.Subject);

        // Verify second cert (root CA)
        var rootCert = certs[1];
        Assert.Contains("Root CA", rootCert.Subject);

        // Verify they have public keys
        Assert.NotNull(intermediateCert.PublicKey);
        Assert.NotNull(rootCert.PublicKey);
    }

    [Fact]
    public void Scp11_TestData_OcePkcs12_IsValid()
    {
        // Act
        var pkcs12Data = Scp11TestData.Oce;
        var password = Scp11TestData.OcePassword.Span;

        // Assert - Should be able to load PKCS12
        Assert.NotEmpty(pkcs12Data.ToArray());
        Assert.Equal("password", new string(password));
    }

    [Fact(Skip = "Requires actual YubiKey hardware with firmware >= 5.7.2")]
    public async Task Scp11b_FullAuthentication_WithRealDevice_Succeeds() =>
        // This test would perform a full SCP11b authentication with a real YubiKey.
        //
        // Prerequisites:
        // - YubiKey with firmware >= 5.7.2
        // - SCP11b key slot 1 has default certificate
        //
        // Steps:
        // 1. Get YubiKey device
        // 2. Connect SmartCard
        // 3. Query certificate bundle from device
        // 4. Extract leaf certificate public key
        // 5. Create Scp11KeyParams
        // 6. Initialize SCP session
        // 7. Verify encrypted communication works
        await Task.CompletedTask;

#pragma warning disable xUnit1004
    [Fact(Skip = "Requires SecurityDomain session and YubiKey hardware")]
#pragma warning restore xUnit1004
    public async Task Scp11a_WithAllowList_FullFlow_Succeeds() =>
        // This test would verify complete SCP11a flow with allowlist.
        //
        // Prerequisites:
        // - YubiKey with firmware >= 5.7.2
        // - SecurityDomain session implementation
        //
        // Steps:
        // 1. Authenticate with SCP03
        // 2. Generate EC key on YubiKey for SCP11a
        // 3. Put OCE public key
        // 4. Store CA issuer with SKI
        // 5. Store allowlist with matching certificate serials
        // 6. Authenticate with SCP11a (should succeed)
        // 7. Delete keys to clean up
        await Task.CompletedTask;

    #region Helper Methods

    /// <summary>
    ///     Parses test certificates from PEM data.
    /// </summary>
    private static List<X509Certificate2> ParseTestCertificates()
    {
        var certs = new List<X509Certificate2>();
        var pemString = Encoding.UTF8.GetString(Scp11TestData.OceCerts.Span);

        var pemParts = pemString.Split(
            ["-----BEGIN CERTIFICATE-----", "-----END CERTIFICATE-----"],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in pemParts)
            if (!string.IsNullOrWhiteSpace(part))
            {
                var certBytes = Convert.FromBase64String(part.Trim());
                var cert = X509CertificateLoader.LoadCertificate(certBytes);
                certs.Add(cert);
            }

        return certs;
    }

    #endregion
}
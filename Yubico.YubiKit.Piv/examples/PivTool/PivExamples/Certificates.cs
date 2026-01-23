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
using System.Text;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates PIV certificate operations using the YubiKey.
/// </summary>
/// <remarks>
/// <para>
/// This class provides examples for managing certificates in PIV slots:
/// reading, importing, exporting, deleting, and generating self-signed certificates.
/// </para>
/// <para>
/// Most write operations require management key authentication.
/// Certificate generation requires PIN if the slot's PIN policy requires it.
/// </para>
/// </remarks>
public static class Certificates
{
    /// <summary>
    /// Gets the certificate from a PIV slot.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="slot">The slot to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the certificate or error information.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var result = await Certificates.GetCertificateAsync(session, PivSlot.Authentication, ct);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"Subject: {result.Certificate!.Subject}");
    /// }
    /// </code>
    /// </example>
    public static async Task<CertificateResult> GetCertificateAsync(
        IPivSession session,
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var cert = await session.GetCertificateAsync(slot, cancellationToken);
            return cert is not null
                ? CertificateResult.Succeeded(cert)
                : CertificateResult.Failed($"Slot {slot} is empty.");
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"Failed to read certificate: {ex.Message}");
        }
    }

    /// <summary>
    /// Imports a certificate from PEM or DER format.
    /// </summary>
    /// <param name="session">An authenticated PIV session (management key verified).</param>
    /// <param name="slot">The target slot.</param>
    /// <param name="certificateData">Certificate data in PEM or DER format.</param>
    /// <param name="compress">Whether to compress the certificate data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// await session.AuthenticateAsync(managementKey, ct);
    /// 
    /// var certData = await File.ReadAllBytesAsync("certificate.pem", ct);
    /// var result = await Certificates.ImportCertificateAsync(
    ///     session, PivSlot.Authentication, certData, compress: false, ct);
    /// </code>
    /// </example>
    public static async Task<CertificateResult> ImportCertificateAsync(
        IPivSession session,
        PivSlot slot,
        ReadOnlyMemory<byte> certificateData,
        bool compress = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            X509Certificate2? cert = null;
            var text = Encoding.UTF8.GetString(certificateData.Span);

            if (text.Contains("-----BEGIN CERTIFICATE-----"))
            {
                cert = X509Certificate2.CreateFromPem(text);
            }
            else
            {
                cert = X509CertificateLoader.LoadCertificate(certificateData.Span);
            }

            if (cert is null)
            {
                return CertificateResult.Failed("Certificate format not recognized. Expected PEM or DER.");
            }

            await session.StoreCertificateAsync(slot, cert, compress, cancellationToken);
            return CertificateResult.Stored();
        }
        catch (CryptographicException)
        {
            return CertificateResult.Failed("Certificate format not recognized. Expected PEM or DER.");
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"Import failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Exports a certificate in PEM format.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="slot">The slot to export from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the PEM string or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var result = await Certificates.ExportCertificatePemAsync(session, PivSlot.Authentication, ct);
    /// if (result.Success &amp;&amp; result.Certificate is not null)
    /// {
    ///     var pem = result.Certificate.ExportCertificatePem();
    ///     await File.WriteAllTextAsync("certificate.pem", pem, ct);
    /// }
    /// </code>
    /// </example>
    public static async Task<CertificateResult> ExportCertificatePemAsync(
        IPivSession session,
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var cert = await session.GetCertificateAsync(slot, cancellationToken);
            if (cert is null)
            {
                return CertificateResult.Failed($"Slot {slot} is empty.");
            }

            return CertificateResult.Succeeded(cert);
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a certificate from a slot.
    /// </summary>
    /// <param name="session">An authenticated PIV session (management key verified).</param>
    /// <param name="slot">The slot to delete from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// await session.AuthenticateAsync(managementKey, ct);
    /// 
    /// var result = await Certificates.DeleteCertificateAsync(session, PivSlot.Authentication, ct);
    /// </code>
    /// </example>
    public static async Task<CertificateResult> DeleteCertificateAsync(
        IPivSession session,
        PivSlot slot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            await session.DeleteCertificateAsync(slot, cancellationToken);
            return CertificateResult.Deleted();
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"Delete failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a self-signed certificate for an existing key.
    /// </summary>
    /// <param name="session">An authenticated PIV session (management key + PIN if required).</param>
    /// <param name="slot">The slot containing the key.</param>
    /// <param name="subject">Certificate subject (e.g., "CN=Test User").</param>
    /// <param name="validityDays">Number of days the certificate is valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the generated certificate or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// await session.AuthenticateAsync(managementKey, ct);
    /// await session.VerifyPinAsync(pin, ct);
    /// 
    /// var result = await Certificates.GenerateSelfSignedAsync(
    ///     session, PivSlot.Authentication, "CN=Test User", 365, ct);
    /// </code>
    /// </example>
    public static async Task<CertificateResult> GenerateSelfSignedAsync(
        IPivSession session,
        PivSlot slot,
        string subject,
        int validityDays,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return CertificateResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
            }

            var slotMetadata = metadata.Value;
            using var publicKey = slotMetadata.Algorithm.IsRsa()
                ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                : slotMetadata.GetECDsaPublicKey();

            if (publicKey is null)
            {
                return CertificateResult.Failed("Failed to extract public key from slot metadata.");
            }

            var subjectName = new X500DistinguishedName(subject);
            var hashAlgorithm = slotMetadata.Algorithm == PivAlgorithm.EccP384
                ? HashAlgorithmName.SHA384
                : HashAlgorithmName.SHA256;

            X509Certificate2? newCert = null;

            if (publicKey is RSA rsa)
            {
                var request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                newCert = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddDays(validityDays));
            }
            else if (publicKey is ECDsa ecdsa)
            {
                var request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                newCert = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddDays(validityDays));
            }

            if (newCert is null)
            {
                return CertificateResult.Failed("Failed to generate certificate.");
            }

            await session.StoreCertificateAsync(slot, newCert, false, cancellationToken);
            return CertificateResult.Succeeded(newCert);
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"Generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a Certificate Signing Request (CSR).
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="slot">The slot containing the key.</param>
    /// <param name="subject">Certificate subject (e.g., "CN=Test User").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the CSR in PEM format or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var result = await Certificates.GenerateCsrAsync(
    ///     session, PivSlot.Authentication, "CN=Test User", ct);
    /// 
    /// if (result.Success)
    /// {
    ///     await File.WriteAllTextAsync("request.csr", result.CsrPem!, ct);
    /// }
    /// </code>
    /// </example>
    public static async Task<CertificateResult> GenerateCsrAsync(
        IPivSession session,
        PivSlot slot,
        string subject,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var metadata = await session.GetSlotMetadataAsync(slot, cancellationToken);
            if (metadata is null)
            {
                return CertificateResult.Failed($"Slot {slot} is empty. Generate or import a key first.");
            }

            var slotMetadata = metadata.Value;
            using var publicKey = slotMetadata.Algorithm.IsRsa()
                ? (AsymmetricAlgorithm)slotMetadata.GetRsaPublicKey()
                : slotMetadata.GetECDsaPublicKey();

            if (publicKey is null)
            {
                return CertificateResult.Failed("Failed to extract public key from slot metadata.");
            }

            var subjectName = new X500DistinguishedName(subject);
            var hashAlgorithm = slotMetadata.Algorithm == PivAlgorithm.EccP384
                ? HashAlgorithmName.SHA384
                : HashAlgorithmName.SHA256;

            string? csr = null;

            if (publicKey is RSA rsa)
            {
                var request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                csr = request.CreateSigningRequestPem();
            }
            else if (publicKey is ECDsa ecdsa)
            {
                var request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                csr = request.CreateSigningRequestPem();
            }

            return csr is not null
                ? CertificateResult.CsrGenerated(csr)
                : CertificateResult.Failed("Failed to generate CSR.");
        }
        catch (Exception ex)
        {
            return CertificateResult.Failed($"CSR generation failed: {ex.Message}");
        }
    }
}

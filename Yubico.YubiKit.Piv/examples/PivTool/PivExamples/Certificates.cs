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
            if (slotMetadata.PublicKey.IsEmpty)
            {
                return CertificateResult.Failed($"Slot {slot} has no public key. Generate a key first.");
            }

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

            CertificateRequest request;
            X509SignatureGenerator signatureGenerator;

            if (publicKey is RSA rsa)
            {
                request = new CertificateRequest(subjectName, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);
                signatureGenerator = new YubiKeyRsaSignatureGenerator(session, slot, slotMetadata.Algorithm, rsa);
            }
            else if (publicKey is ECDsa ecdsa)
            {
                request = new CertificateRequest(subjectName, ecdsa, hashAlgorithm);
                signatureGenerator = new YubiKeyEcdsaSignatureGenerator(session, slot, slotMetadata.Algorithm, ecdsa);
            }
            else
            {
                return CertificateResult.Failed($"Unsupported key type: {publicKey.GetType().Name}");
            }

            // Add basic constraints extension (this is a self-signed cert, not a CA)
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));

            // Add key usage
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));

            // Generate serial number
            var serialNumber = new byte[20];
            RandomNumberGenerator.Fill(serialNumber);
            serialNumber[0] &= 0x7F; // Ensure positive

            // Create certificate using YubiKey for signing
            var newCert = request.Create(
                subjectName, // Self-signed: issuer = subject
                signatureGenerator,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(validityDays),
                serialNumber);

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
            if (slotMetadata.PublicKey.IsEmpty)
            {
                return CertificateResult.Failed($"Slot {slot} has no public key. Generate a key first.");
            }

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

/// <summary>
/// X509 signature generator that delegates RSA signing to the YubiKey.
/// </summary>
internal sealed class YubiKeyRsaSignatureGenerator(
    IPivSession session,
    PivSlot slot,
    PivAlgorithm algorithm,
    RSA publicKey) : X509SignatureGenerator
{
    public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
    {
        // RSA with PKCS#1 v1.5 signature algorithm identifiers
        // OID 1.2.840.113549.1.1.11 = sha256WithRSAEncryption
        // OID 1.2.840.113549.1.1.12 = sha384WithRSAEncryption
        // OID 1.2.840.113549.1.1.13 = sha512WithRSAEncryption
        if (hashAlgorithm == HashAlgorithmName.SHA256)
        {
            // SEQUENCE { OID sha256WithRSAEncryption, NULL }
            return [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0B, 0x05, 0x00];
        }
        else if (hashAlgorithm == HashAlgorithmName.SHA384)
        {
            // SEQUENCE { OID sha384WithRSAEncryption, NULL }
            return [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0C, 0x05, 0x00];
        }
        else if (hashAlgorithm == HashAlgorithmName.SHA512)
        {
            // SEQUENCE { OID sha512WithRSAEncryption, NULL }
            return [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x0D, 0x05, 0x00];
        }

        throw new ArgumentOutOfRangeException(nameof(hashAlgorithm), $"Unsupported hash algorithm: {hashAlgorithm}");
    }

    public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
    {
        // Hash the data first
        byte[] hash = hashAlgorithm.Name switch
        {
            "SHA256" => SHA256.HashData(data),
            "SHA384" => SHA384.HashData(data),
            "SHA512" => SHA512.HashData(data),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm}")
        };

        // Build DigestInfo structure for PKCS#1 v1.5 signing
        // DigestInfo ::= SEQUENCE { digestAlgorithm AlgorithmIdentifier, digest OCTET STRING }
        byte[] digestInfo = BuildDigestInfo(hash, hashAlgorithm);

        // Pad to key size with PKCS#1 v1.5 padding
        int keySize = publicKey.KeySize / 8;
        byte[] padded = ApplyPkcs1Padding(digestInfo, keySize);

        // Sign using YubiKey (blocking call - this is sync API required by X509SignatureGenerator)
        var signature = session.SignOrDecryptAsync(slot, algorithm, padded).GetAwaiter().GetResult();

        return signature.ToArray();
    }

    protected override PublicKey BuildPublicKey() => new(publicKey);

    private static byte[] BuildDigestInfo(byte[] hash, HashAlgorithmName hashAlgorithm)
    {
        // DigestInfo DER encoding
        byte[] algorithmOid = hashAlgorithm.Name switch
        {
            // OID 2.16.840.1.101.3.4.2.1 = SHA-256
            "SHA256" => [0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20],
            // OID 2.16.840.1.101.3.4.2.2 = SHA-384
            "SHA384" => [0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00, 0x04, 0x30],
            // OID 2.16.840.1.101.3.4.2.3 = SHA-512  
            "SHA512" => [0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0x04, 0x40],
            _ => throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm}")
        };

        byte[] result = new byte[algorithmOid.Length + hash.Length];
        algorithmOid.CopyTo(result, 0);
        hash.CopyTo(result, algorithmOid.Length);
        return result;
    }

    private static byte[] ApplyPkcs1Padding(byte[] digestInfo, int keySize)
    {
        // PKCS#1 v1.5 padding: 0x00 0x01 [0xFF padding] 0x00 [digestInfo]
        if (digestInfo.Length > keySize - 11)
        {
            throw new ArgumentException("Data too long for key size");
        }

        byte[] padded = new byte[keySize];
        padded[0] = 0x00;
        padded[1] = 0x01;

        int paddingLength = keySize - digestInfo.Length - 3;
        for (int i = 0; i < paddingLength; i++)
        {
            padded[2 + i] = 0xFF;
        }

        padded[2 + paddingLength] = 0x00;
        digestInfo.CopyTo(padded, 3 + paddingLength);

        return padded;
    }
}

/// <summary>
/// X509 signature generator that delegates ECDSA signing to the YubiKey.
/// </summary>
internal sealed class YubiKeyEcdsaSignatureGenerator(
    IPivSession session,
    PivSlot slot,
    PivAlgorithm algorithm,
    ECDsa publicKey) : X509SignatureGenerator
{
    public override byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm)
    {
        // ECDSA signature algorithm identifiers
        // OID 1.2.840.10045.4.3.2 = ecdsa-with-SHA256
        // OID 1.2.840.10045.4.3.3 = ecdsa-with-SHA384
        if (hashAlgorithm == HashAlgorithmName.SHA256)
        {
            // SEQUENCE { OID ecdsa-with-SHA256 }
            return [0x30, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x02];
        }
        else if (hashAlgorithm == HashAlgorithmName.SHA384)
        {
            // SEQUENCE { OID ecdsa-with-SHA384 }
            return [0x30, 0x0A, 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x04, 0x03, 0x03];
        }

        throw new ArgumentOutOfRangeException(nameof(hashAlgorithm), $"Unsupported hash algorithm: {hashAlgorithm}");
    }

    public override byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
    {
        // Hash the data first - ECDSA signs the hash directly
        byte[] hash = hashAlgorithm.Name switch
        {
            "SHA256" => SHA256.HashData(data),
            "SHA384" => SHA384.HashData(data),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {hashAlgorithm}")
        };

        // Sign using YubiKey (blocking call - this is sync API required by X509SignatureGenerator)
        // YubiKey returns signature in IEEE P1363 format (r || s)
        var signatureMemory = session.SignOrDecryptAsync(slot, algorithm, hash).GetAwaiter().GetResult();
        var signature = signatureMemory.Span;

        // Convert IEEE P1363 (r || s) to DER format for X.509
        return ConvertIeeeP1363ToDer(signature);
    }

    protected override PublicKey BuildPublicKey() => new(publicKey);

    private static byte[] ConvertIeeeP1363ToDer(ReadOnlySpan<byte> ieeeSignature)
    {
        // IEEE P1363 format: r || s (each coordinate is half the signature)
        int coordinateSize = ieeeSignature.Length / 2;
        var r = ieeeSignature[..coordinateSize];
        var s = ieeeSignature[coordinateSize..];

        // Convert to DER integers (may need leading 0x00 if high bit set)
        byte[] rDer = ToDerInteger(r);
        byte[] sDer = ToDerInteger(s);

        // Build SEQUENCE { INTEGER r, INTEGER s }
        int sequenceContentLength = rDer.Length + sDer.Length;
        int sequenceHeaderLength = sequenceContentLength < 128 ? 2 : 3;
        byte[] result = new byte[sequenceHeaderLength + sequenceContentLength];

        int offset = 0;
        result[offset++] = 0x30; // SEQUENCE tag
        if (sequenceContentLength < 128)
        {
            result[offset++] = (byte)sequenceContentLength;
        }
        else
        {
            result[offset++] = 0x81;
            result[offset++] = (byte)sequenceContentLength;
        }

        rDer.CopyTo(result, offset);
        offset += rDer.Length;
        sDer.CopyTo(result, offset);

        return result;
    }

    private static byte[] ToDerInteger(ReadOnlySpan<byte> value)
    {
        // Skip leading zeros
        int start = 0;
        while (start < value.Length - 1 && value[start] == 0)
        {
            start++;
        }

        // Need leading 0x00 if high bit is set (to indicate positive number)
        bool needsLeadingZero = (value[start] & 0x80) != 0;
        int length = value.Length - start + (needsLeadingZero ? 1 : 0);

        byte[] result = new byte[2 + length];
        result[0] = 0x02; // INTEGER tag
        result[1] = (byte)length;

        int offset = 2;
        if (needsLeadingZero)
        {
            result[offset++] = 0x00;
        }

        value[start..].CopyTo(result.AsSpan(offset));
        return result;
    }
}

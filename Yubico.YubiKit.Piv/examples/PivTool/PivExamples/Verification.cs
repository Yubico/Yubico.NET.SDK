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

using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates signature verification using certificates from PIV slots.
/// </summary>
/// <remarks>
/// <para>
/// Signature verification uses the public key from a certificate stored in a PIV slot.
/// This operation does not require PIN or touch as it only uses the public key.
/// </para>
/// </remarks>
public static class Verification
{
    /// <summary>
    /// Verifies a signature using the certificate in a PIV slot.
    /// </summary>
    /// <param name="session">A PIV session (does not need PIN verification).</param>
    /// <param name="slot">The slot containing the certificate with public key.</param>
    /// <param name="originalData">The original data that was signed.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="hashAlgorithm">Hash algorithm used during signing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether signature is valid.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(ct);
    /// 
    /// var result = await Verification.VerifySignatureAsync(
    ///     session,
    ///     PivSlot.Signature,
    ///     originalData,
    ///     signatureBytes,
    ///     HashAlgorithmName.SHA256,
    ///     ct);
    /// 
    /// if (result.Success &amp;&amp; result.IsValid)
    /// {
    ///     Console.WriteLine("Signature is valid!");
    /// }
    /// </code>
    /// </example>
    public static async Task<VerificationResult> VerifySignatureAsync(
        IPivSession session,
        PivSlot slot,
        ReadOnlyMemory<byte> originalData,
        ReadOnlyMemory<byte> signature,
        HashAlgorithmName hashAlgorithm,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var cert = await session.GetCertificateAsync(slot, cancellationToken);
            if (cert is null)
            {
                return VerificationResult.Failed($"Slot {slot} has no certificate.");
            }

            var stopwatch = Stopwatch.StartNew();
            var isValid = false;

            var rsaKey = cert.GetRSAPublicKey();
            if (rsaKey is not null)
            {
                isValid = rsaKey.VerifyData(
                    originalData.Span,
                    signature.Span,
                    hashAlgorithm,
                    RSASignaturePadding.Pkcs1);
            }
            else
            {
                var ecdsaKey = cert.GetECDsaPublicKey();
                if (ecdsaKey is not null)
                {
                    isValid = ecdsaKey.VerifyData(
                        originalData.Span,
                        signature.Span,
                        hashAlgorithm);
                }
            }

            stopwatch.Stop();
            return isValid
                ? VerificationResult.Valid(stopwatch.ElapsedMilliseconds)
                : VerificationResult.Invalid(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return VerificationResult.Failed($"Verification failed: {ex.Message}");
        }
    }
}

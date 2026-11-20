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

using Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples;

/// <summary>
/// Demonstrates PIV key attestation operations.
/// </summary>
/// <remarks>
/// <para>
/// Retrieve evidence for an on-device-generated key on YubiKey 4.3+. The caller validates
/// the chain against its trust policy and binds the attested public key to the intended slot.
/// </para>
/// <para>
/// The attestation certificate chain can be validated against
/// Yubico's root CA to prove authenticity.
/// </para>
/// </remarks>
public static class Attestation
{
    /// <summary>
    /// Gets an attestation certificate for a key in a PIV slot.
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="slot">The slot containing the key to attest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the attestation certificate or error.</returns>
    /// <example>
    /// <code>
    /// await using var session = await device.CreatePivSessionAsync(cancellationToken: ct);
    /// 
    /// var result = await Attestation.GetAttestationAsync(
    ///     session, Slot.Authentication, ct);
    /// 
    /// using var attestCert = result.AttestationCertificate;
    /// using var signerCert = result.IntermediateCertificate;
    /// if (result.Success &amp;&amp; attestCert is not null)
    /// {
    ///     Console.WriteLine($"Retrieved attestation: {attestCert.Subject}");
    /// }
    /// </code>
    /// </example>
    public static async Task<AttestationResult> GetAttestationAsync(
        IPivSession session,
        Slot slot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            var attestCert = await session.AttestKeyAsync(slot, cancellationToken);
            return AttestationResult.Succeeded(attestCert);
        }
        catch (NotSupportedException)
        {
            return AttestationResult.Failed("Attestation not supported on this YubiKey (requires 4.3+).");
        }
        catch (InvalidOperationException)
        {
            return AttestationResult.Failed($"Slot {slot} is empty. Generate a key first.");
        }
        catch (Exception ex)
        {
            return AttestationResult.Failed($"Attestation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the attestation intermediate certificate (Yubico PIV CA).
    /// </summary>
    /// <param name="session">A PIV session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the intermediate certificate or error.</returns>
    /// <example>
    /// <code>
    /// var result = await Attestation.GetIntermediateCertificateAsync(session, ct);
    /// // This helper returns F9 in AttestationCertificate; IntermediateCertificate remains null.
    /// using var f9Cert = result.AttestationCertificate;
    /// using var additionalCert = result.IntermediateCertificate;
    /// if (result.Success &amp;&amp; f9Cert is not null)
    /// {
    ///     Console.WriteLine(f9Cert.Subject);
    /// }
    /// </code>
    /// </example>
    public static async Task<AttestationResult> GetIntermediateCertificateAsync(
        IPivSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            // The attestation intermediate is stored in slot F9
            var intermediateCert = await session.GetCertificateAsync(Slot.Attestation, cancellationToken);
            if (intermediateCert is null)
            {
                return AttestationResult.Failed("No attestation intermediate certificate found.");
            }

            return AttestationResult.Succeeded(intermediateCert);
        }
        catch (Exception ex)
        {
            return AttestationResult.Failed($"Failed to get intermediate certificate: {ex.Message}");
        }
    }
}
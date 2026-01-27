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

using System.Security.Cryptography.X509Certificates;

namespace Yubico.YubiKit.Piv.Examples.PivTool.PivExamples.Results;

/// <summary>
/// Result of a PIV attestation operation.
/// </summary>
public sealed record AttestationResult
{
    /// <summary>
    /// Gets whether the attestation operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the attestation certificate for the key.
    /// </summary>
    public X509Certificate2? AttestationCertificate { get; init; }

    /// <summary>
    /// Gets the intermediate certificate (Yubico PIV CA).
    /// </summary>
    public X509Certificate2? IntermediateCertificate { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful attestation result.
    /// </summary>
    public static AttestationResult Succeeded(X509Certificate2 attestation, X509Certificate2? intermediate = null) =>
        new() { Success = true, AttestationCertificate = attestation, IntermediateCertificate = intermediate };

    /// <summary>
    /// Creates a failed attestation result.
    /// </summary>
    public static AttestationResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

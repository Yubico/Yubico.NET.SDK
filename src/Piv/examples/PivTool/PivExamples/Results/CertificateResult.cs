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
/// Result of a certificate operation.
/// </summary>
public sealed record CertificateResult
{
    /// <summary>
    /// Gets whether the certificate operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the certificate when the operation returns one.
    /// </summary>
    public X509Certificate2? Certificate { get; init; }

    /// <summary>
    /// Gets the CSR in PEM format when a CSR was generated.
    /// </summary>
    public string? CsrPem { get; init; }

    /// <summary>
    /// Gets the error message when the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result with a certificate.
    /// </summary>
    public static CertificateResult Succeeded(X509Certificate2 cert) =>
        new() { Success = true, Certificate = cert };

    /// <summary>
    /// Creates a successful result with a generated CSR.
    /// </summary>
    public static CertificateResult CsrGenerated(string csrPem) =>
        new() { Success = true, CsrPem = csrPem };

    /// <summary>
    /// Creates a successful result for a store operation.
    /// </summary>
    public static CertificateResult Stored() =>
        new() { Success = true };

    /// <summary>
    /// Creates a successful result for a delete operation.
    /// </summary>
    public static CertificateResult Deleted() =>
        new() { Success = true };

    /// <summary>
    /// Creates a failed certificate result.
    /// </summary>
    public static CertificateResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

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

using System.IO;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities;

/// <summary>
/// Represents an X.509 certificate for testing purposes.
/// Supports both regular and attestation certificates.
/// </summary>
public class TestCertificate : TestCrypto
{
    /// <summary>
    /// Indicates whether this certificate is an attestation certificate.
    /// </summary>
    public readonly bool IsAttestation;

    private TestCertificate(
        string filePath,
        bool isAttestation) : base(filePath)
    {
        IsAttestation = isAttestation;
    }

    /// <summary>
    /// Converts the certificate to an X509Certificate2 instance.
    /// </summary>
    /// <returns>X509Certificate2 instance initialized with the certificate data</returns>
    public X509Certificate2 AsX509Certificate2() => X509CertificateLoader.LoadCertificate(_bytes);

    /// <summary>
    /// Loads a certificate from the TestData directory.
    /// </summary>
    /// <param name="curve">The curve or key type associated with the certificate</param>
    /// <param name="keyType">keyType to load</param>
    /// <param name="isAttestation">True if loading an attestation certificate</param>
    /// <returns>A TestCertificate instance</returns>
    public static TestCertificate Load(
        KeyType keyType,
        bool isAttestation = false)
    {
        var curveName = keyType.ToString().ToLower();
        var fileName = $"{curveName}_cert{(isAttestation ? "_attest" : "")}.pem";
        var filePath = Path.Combine(TestDataDirectory, fileName);
        return new TestCertificate(filePath, isAttestation);
    }
}

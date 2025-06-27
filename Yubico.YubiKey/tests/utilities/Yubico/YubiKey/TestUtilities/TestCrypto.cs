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

namespace Yubico.YubiKey.TestUtilities;

/// <summary>
/// This class is used to load test keys and certificates from files.
/// The files contain PEM-encoded data such as RSA keys, EC keys, and X.509 certificates.
/// These keys and certificates are used in unit tests and have been generated through
/// the script `generate-test-data.sh` which is checked into this repository under Yubico/YubiKey/utilities/TestData.
/// </summary>
public abstract class TestCrypto
{
    public const string TestDataDirectory = "TestData";

    /// <summary>
    /// The raw byte representation of the cryptographic data in DER format.
    /// </summary>
    protected readonly byte[] _bytes;

    protected readonly string _pemStringFull;

    /// <summary>
    /// Initializes a new instance of TestCrypto with PEM-encoded data from a file.
    /// </summary>
    /// <param name="filePath">Path to the PEM file containing cryptographic data.</param>
    protected TestCrypto(
        string filePath)
    {
        _pemStringFull = File.ReadAllText(filePath);
        _bytes = PemHelper.GetBytesFromPem(_pemStringFull);
    }

    /// <summary>
    /// Returns the raw byte DER representation of the key data.
    /// </summary>
    /// <returns>Byte array containing the decoded cryptographic data.</returns>
    public byte[] EncodedKey => _bytes;

    /// <summary>
    /// Returns the complete PEM-encoded string representation.
    /// </summary>
    /// <returns>String containing the full PEM data including headers and footers.</returns>
    public string AsPemString() => _pemStringFull;

    /// <summary>
    /// Returns the Base64-encoded data without PEM headers and footers.
    /// </summary>
    /// <returns>Base64 string of the cryptographic data.</returns>
    public string AsBase64String() => PemHelper.AsBase64String(_pemStringFull);

    public static byte[] ReadTestData(
        string fileName) => File.ReadAllBytes(Path.Combine(TestDataDirectory, fileName));
}

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

namespace Yubico.YubiKit.OpenPgp;

/// <summary>
///     PKCS#1 v1.5 DigestInfo DER headers for RSA signature formatting.
/// </summary>
/// <remarks>
///     When signing with RSA using the OpenPGP applet, the caller must prepend the
///     appropriate DigestInfo header to the hash digest before sending it to the card.
///     These headers encode the hash algorithm OID and length per PKCS#1 v1.5 (RFC 8017 §9.2).
/// </remarks>
internal static class DigestInfo
{
    private static readonly byte[] Sha256Header =
        [0x30, 0x31, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20];

    private static readonly byte[] Sha384Header =
        [0x30, 0x41, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x02, 0x05, 0x00, 0x04, 0x30];

    private static readonly byte[] Sha512Header =
        [0x30, 0x51, 0x30, 0x0D, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0x04, 0x40];

    private static readonly byte[] Sha1Header =
        [0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00, 0x04, 0x14];

    /// <summary>
    ///     Gets the PKCS#1 v1.5 DigestInfo DER header for the specified hash algorithm.
    /// </summary>
    /// <param name="hashAlgorithm">The hash algorithm name.</param>
    /// <returns>The DER-encoded DigestInfo header bytes.</returns>
    /// <exception cref="NotSupportedException">Thrown when the hash algorithm is not supported for RSA signing.</exception>
    internal static ReadOnlySpan<byte> GetHeader(HashAlgorithmName hashAlgorithm) =>
        hashAlgorithm.Name switch
        {
            "SHA1" => Sha1Header,
            "SHA256" => Sha256Header,
            "SHA384" => Sha384Header,
            "SHA512" => Sha512Header,
            _ => throw new NotSupportedException(
                $"Unsupported hash algorithm for RSA DigestInfo: {hashAlgorithm.Name}"),
        };

    /// <summary>
    ///     Builds the DigestInfo structure: header + hash digest.
    /// </summary>
    internal static byte[] Build(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> digest)
    {
        var header = GetHeader(hashAlgorithm);
        var result = new byte[header.Length + digest.Length];
        header.CopyTo(result);
        digest.CopyTo(result.AsSpan(header.Length));
        return result;
    }
}
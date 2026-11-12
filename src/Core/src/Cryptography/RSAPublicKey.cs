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

namespace Yubico.YubiKit.Core.Cryptography;

/// <summary>
/// Represents an RSA public key.
/// </summary>
/// <remarks>
/// This sealed class encapsulates RSA public key parameters (Modulus and Exponent)
/// and provides factory methods for creating instances from RSA parameters or DER-encoded data.
/// </remarks>
public sealed class RSAPublicKey : PublicKey
{
    /// <summary>
    /// Gets the RSA modulus and public exponent.
    /// </summary>
    /// <value>
    /// A parameter structure containing Modulus and Exponent, with no private components.
    /// </value>
    /// <remarks>
    /// The structure contains references to key-owned mutable arrays. Treat them as borrowed and leave
    /// them unchanged; copying the structure does not copy the arrays. Construction copies the supplied
    /// arrays and selects a modeled modulus storage width, rather than performing full mathematical validation.
    /// </remarks>
    public RSAParameters Parameters { get; }

    /// <summary>
    /// Gets the metadata associated with this RSA public key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyInfo"/> object that describes the key's representation.
    /// </value>
    public override KeyInfo KeyInfo { get; }

    /// <inheritdoc />
    public override KeyType KeyType => KeyInfo.KeyType;

    private RSAPublicKey(RSAParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters.Modulus);
        Parameters = parameters.DeepCopy();
        KeyInfo = CryptographicDefinitions.Keys.GetByRsaLength(parameters.Modulus.Length * 8);
    }

    /// <inheritdoc />
    public override byte[] ExportSubjectPublicKeyInfo()
    {
        if (Parameters.Exponent is null ||
            Parameters.Modulus is null)
        {
            throw new InvalidOperationException("Cannot export public key, missing required parameters");
        }

        return AsnPublicKeyEncoder.EncodeToSubjectPublicKeyInfo(Parameters);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RSAPublicKey"/> from the given
    /// <paramref name="parameters"/>.
    /// </summary>
    /// <param name="parameters">
    /// The RSA parameters containing the public key data.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RSAPublicKey"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the parameters contain private key data.
    /// </exception>
    public static RSAPublicKey CreateFromParameters(RSAParameters parameters)
    {
        if (parameters.D is not null ||
            parameters.P is not null ||
            parameters.Q is not null ||
            parameters.DP is not null ||
            parameters.DQ is not null ||
            parameters.InverseQ is not null
           )
        {
            throw new ArgumentException("Parameters must not contain private key data");
        }

        return new RSAPublicKey(parameters);
    }

    /// <summary>
    /// Creates a new instance of <see cref="IPublicKey"/> from ASN.1 DER-encoded SubjectPublicKeyInfo.
    /// </summary>
    /// <param name="subjectPublicKeyInfo">The DER-encoded SubjectPublicKeyInfo.</param>
    /// <returns>A new instance of <see cref="IPublicKey"/>.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the public key is invalid.
    /// </exception>
    /// <exception cref="System.Formats.Asn1.AsnContentException">The ASN.1 structure or algorithm parameters are malformed.</exception>
    /// <remarks>
    /// The shared decoder checks the RSA component sequence and AlgorithmIdentifier; it does not
    /// uniformly check surrounding reader exhaustion or perform full RSA mathematical validation.
    /// </remarks>
    public static RSAPublicKey CreateFromSubjectPublicKeyInfo(ReadOnlyMemory<byte> subjectPublicKeyInfo) =>
        AsnPublicKeyDecoder
            .CreatePublicKey(subjectPublicKeyInfo)
            .Cast<RSAPublicKey>();
}
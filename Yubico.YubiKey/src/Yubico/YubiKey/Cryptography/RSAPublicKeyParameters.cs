// Copyright 2024 Yubico AB
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

using System;
using System.Security.Cryptography;

namespace Yubico.YubiKey.Cryptography;

public class RSAPublicKeyParameters : IPublicKeyParameters
{
    private KeyDefinition _keyDefinition { get; }
    public RSAParameters Parameters { get; }
    public KeyDefinition KeyDefinition => _keyDefinition;
    public KeyType KeyType => _keyDefinition.KeyType;

    [Obsolete("Use factory methods instead")]
    public RSAPublicKeyParameters(RSAParameters parameters)
    {
        if (parameters.D != null || 
            parameters.P != null || 
            parameters.Q != null ||
            parameters.DP != null ||
            parameters.DQ != null ||
            parameters.InverseQ != null
           )
        {
            throw new ArgumentException("Parameters must not contain private key data");
        }
        
        Parameters = parameters.DeepCopy();
        _keyDefinition = KeyDefinitions.GetByRSALength(parameters.Modulus.Length * 8);
    }

    public byte[] ExportSubjectPublicKeyInfo()
    {
        if (Parameters.Exponent == null ||
            Parameters.Modulus == null)
        {
            throw new InvalidOperationException("Cannot export public key, missing required parameters");
        }

        return AsnPublicKeyWriter.EncodeToSubjectPublicKeyInfo(Parameters);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RSAPublicKeyParameters"/> from the given
    /// <paramref name="parameters"/>.
    /// </summary>
    /// <param name="parameters">
    /// The RSA parameters containing the public key data.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RSAPublicKeyParameters"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the parameters contain private key data.
    /// </exception>
    #pragma warning disable CS0618 // Type or member is obsolete
    public static RSAPublicKeyParameters CreateFromParameters(RSAParameters parameters) => new(parameters);
    #pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Creates a new instance of <see cref="IPublicKeyParameters"/> from a DER-encoded public key.
    /// </summary>
    /// <param name="encodedKey">The DER-encoded public key.</param>
    /// <returns>A new instance of <see cref="IPublicKeyParameters"/>.</returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the public key is invalid.
    /// </exception>
    public static IPublicKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey) =>
        AsnPublicKeyReader.CreateKeyParameters(encodedKey);
}

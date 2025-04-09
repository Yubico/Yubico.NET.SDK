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

public sealed class RSAPrivateKey : PrivateKey
{

    /// <summary>
    /// Gets the RSA cryptographic parameters required for the private key operations.
    /// </summary>
    /// <value>
    /// A structure containing RSA parameters, including Modulus, Exponent, D, P, Q, DP, DQ, and InverseQ values.
    /// </value>
    /// <remarks>
    /// This property provides access to the fundamental mathematical components needed for RSA private key operations.
    /// The parameters are used in cryptographic operations such as decryption and digital signature creation.
    /// </remarks>
    public RSAParameters Parameters { get; }

    /// <summary>
    /// Gets the key definition associated with this RSA private key.
    /// </summary>
    /// <value>
    /// A <see cref="KeyDefinition"/> object that describes the key's properties, including its type and length.
    /// </value>
    public KeyDefinition KeyDefinition { get; }
    
    /// <inheritdoc />
    public override KeyType KeyType => KeyDefinition.KeyType;

    private RSAPrivateKey(RSAParameters parameters)
    {
        int keyLengthBits = parameters.DP?.Length * 8 * 2 ?? 0;
        
        Parameters = parameters.NormalizeParameters();
        KeyDefinition = KeyDefinitions.GetByRSALength(keyLengthBits);
    }

    /// <summary>
    /// Exports the RSA private key in PKCS#8 DER encoded format.
    /// </summary>
    /// <returns>A byte array containing the DER encoded private key.</returns>
    public override byte[] ExportPkcs8PrivateKey()
    {
        ThrowIfDisposed();
        return AsnPrivateKeyEncoder.EncodeToPkcs8(Parameters);
    }

    /// <summary>
    /// Securely clears the RSA private key by zeroing out all parameters.
    /// </summary>
    public override void Clear()
    {
        CryptographicOperations.ZeroMemory(Parameters.Modulus);
        CryptographicOperations.ZeroMemory(Parameters.Exponent);
        CryptographicOperations.ZeroMemory(Parameters.P);
        CryptographicOperations.ZeroMemory(Parameters.Q);
        CryptographicOperations.ZeroMemory(Parameters.D);
        CryptographicOperations.ZeroMemory(Parameters.DP);
        CryptographicOperations.ZeroMemory(Parameters.DQ);
        CryptographicOperations.ZeroMemory(Parameters.InverseQ);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RSAPrivateKey"/> from a DER-encoded
    /// PKCS#8 private key.
    /// </summary>
    /// <param name="encodedKey">
    /// The DER-encoded PKCS#8 private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RSAPrivateKey"/>.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the private key is invalid.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// When the RSA key length is not supported.
    /// </exception>
    public static RSAPrivateKey CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var parameters = AsnPrivateKeyDecoder.CreateRSAParameters(encodedKey);
        return new RSAPrivateKey(parameters);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RSAPrivateKey"/> from the given
    /// <paramref name="parameters"/>.
    /// </summary>
    /// <param name="parameters">
    /// The RSA parameters containing the private key data.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RSAPrivateKey"/>.
    /// </returns>
    public static RSAPrivateKey CreateFromParameters(RSAParameters parameters) => new(parameters);
}

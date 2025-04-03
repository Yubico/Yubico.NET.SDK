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

public class RSAPrivateKeyParameters : IPrivateKeyParameters
{
    public RSAParameters Parameters { get; }
    public KeyDefinition KeyDefinition { get; }
    public KeyType KeyType => KeyDefinition.KeyType;

    private RSAPrivateKeyParameters(RSAParameters parameters)
    {
        int keyLengthBits = parameters.DP?.Length * 8 * 2 ?? 0;
        
        Parameters = parameters.NormalizeParameters(); // TODO Clear
        KeyDefinition = KeyDefinitions.GetByRSALength(keyLengthBits);
    }

    /// <summary>
    /// Exports the RSA private key in PKCS#8 DER encoded format.
    /// </summary>
    /// <returns>A byte array containing the DER encoded private key.</returns>
    public byte[] ExportPkcs8PrivateKey() => AsnPrivateKeyWriter.EncodeToPkcs8(Parameters);

    /// <summary>
    /// Securely clears the RSA private key by zeroing out all parameters.
    /// </summary>
    public void Clear()
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
    /// Creates a new instance of <see cref="RSAPrivateKeyParameters"/> from a DER-encoded
    /// PKCS#8 private key.
    /// </summary>
    /// <param name="encodedKey">
    /// The DER-encoded PKCS#8 private key.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RSAPrivateKeyParameters"/>.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the private key is invalid.
    /// </exception>
    public static RSAPrivateKeyParameters CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
    {
        var parameters = AsnPrivateKeyReader.CreateRSAParameters(encodedKey);
        return new RSAPrivateKeyParameters(parameters);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RSAPrivateKeyParameters"/> from the given
    /// <paramref name="parameters"/>.
    /// </summary>
    /// <param name="parameters">
    /// The RSA parameters containing the private key data.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="RSAPrivateKeyParameters"/>.
    /// </returns>
    public static RSAPrivateKeyParameters CreateFromParameters(RSAParameters parameters) => new(parameters);
}

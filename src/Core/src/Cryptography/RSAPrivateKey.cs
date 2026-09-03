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
/// Represents the parameters for an RSA private key.
/// </summary>
/// <remarks>
/// This class encapsulates the parameters specific to RSA private keys 
/// and provides factory methods for creating instances from RSA parameters
/// or DER-encoded data.
/// </remarks>
public sealed class RSAPrivateKey : PrivateKey
{

    /// <summary>
    /// Gets the RSA cryptographic parameters required for the private key operations.
    /// </summary>
    /// <value>
    /// A structure containing the RSA parameters. The array fields are owned by this object, must
    /// not be modified or cleared by the caller, and are cleared when the object is disposed.
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

    private RSAPrivateKey(
        RSAParameters parameters,
        Action<RSAParameters>? parametersCopied = null)
    {
        var keyLengthBits = parameters.DP?.Length * 8 * 2 ?? 0;

        KeyDefinition = KeyDefinitions.GetByRSALength(keyLengthBits);
        Parameters = parameters.NormalizeParameters(parametersCopied);
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
    /// The borrowed DER-encoded PKCS#8 private key. This method copies the decoded key material and
    /// does not modify or clear the input.
    /// </param>
    /// <returns>
    /// A new disposable key that owns and clears its copied private-key material.
    /// </returns>
    /// <exception cref="CryptographicException">
    /// Thrown if the private key is invalid.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// When the RSA key length is not supported.
    /// </exception>
    public static RSAPrivateKey CreateFromPkcs8(ReadOnlyMemory<byte> encodedKey)
        => CreateFromPkcs8(encodedKey, parametersDecoded: null);

    // Test observation hook. The callback must not retain or mutate the decoded private arrays.
    internal static RSAPrivateKey CreateFromPkcs8(
        ReadOnlyMemory<byte> encodedKey,
        Action<RSAParameters>? parametersDecoded)
    {
        var parameters = AsnPrivateKeyDecoder.CreateRSAParameters(encodedKey);
        try
        {
            parametersDecoded?.Invoke(parameters);
            return new RSAPrivateKey(parameters);
        }
        finally
        {
            // On success the constructor copied these values; on failure this factory is
            // unwinding. Either way, the decoder's temporary private arrays are factory-owned.
            // CreateFromParameters is deliberately different because its input remains caller-owned.
            CryptographicOperations.ZeroMemory(parameters.D);
            CryptographicOperations.ZeroMemory(parameters.P);
            CryptographicOperations.ZeroMemory(parameters.Q);
            CryptographicOperations.ZeroMemory(parameters.DP);
            CryptographicOperations.ZeroMemory(parameters.DQ);
            CryptographicOperations.ZeroMemory(parameters.InverseQ);
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="RSAPrivateKey"/> from the given
    /// <paramref name="parameters"/>.
    /// </summary>
    /// <param name="parameters">
    /// The borrowed RSA parameters to copy. The caller retains ownership of every input array and
    /// remains responsible for clearing sensitive arrays.
    /// </param>
    /// <returns>
    /// A new disposable key that owns and clears its copied private-key material.
    /// </returns>
    public static RSAPrivateKey CreateFromParameters(RSAParameters parameters) => new(parameters);

    // Test observation hook. The callback must not retain or mutate the copied arrays.
    internal static RSAPrivateKey CreateFromParameters(
        RSAParameters parameters,
        Action<RSAParameters>? parametersCopied) =>
        new(parameters, parametersCopied);
}
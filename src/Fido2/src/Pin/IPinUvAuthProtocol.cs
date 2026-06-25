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

namespace Yubico.YubiKit.Fido2.Pin;

/// <summary>
/// PIN/UV authentication protocol for CTAP2 authenticators.
/// </summary>
/// <remarks>
/// <para>
/// The PIN/UV auth protocol provides cryptographic operations for authenticating
/// PIN/UV commands and protecting sensitive data in transit.
/// </para>
/// <para>
/// Two protocol versions are defined:
/// <list type="bullet">
///   <item><description>V1: Uses ECDH P-256, HKDF-SHA-256 (32 bytes), AES-256-CBC</description></item>
///   <item><description>V2: Uses ECDH P-256, HKDF-SHA-256 (64 bytes for HMAC+AES), AES-256-CBC</description></item>
/// </list>
/// </para>
/// <para>
/// See: https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#pinProto
/// </para>
/// </remarks>
public interface IPinUvAuthProtocol : IDisposable
{
    /// <summary>
    /// Gets the protocol version number (1 or 2).
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// Gets the length of authentication tags produced by this protocol.
    /// </summary>
    int AuthenticationTagLength { get; }
    
    /// <summary>
    /// Performs ECDH key agreement with the authenticator's public key and derives a shared secret.
    /// </summary>
    /// <param name="peerCoseKey">The authenticator's COSE EC2 public key.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>KeyAgreement: The platform's COSE public key to send to the authenticator.</description></item>
    ///   <item><description>SharedSecret: The derived shared secret for subsequent cryptographic operations.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="peerCoseKey"/> is null.</exception>
    /// <exception cref="ArgumentException">If the peer key is malformed or invalid.</exception>
    (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IReadOnlyDictionary<int, object?> peerCoseKey);
    
    /// <summary>
    /// Derives a symmetric key from a raw ECDH shared secret using the protocol's KDF.
    /// </summary>
    /// <param name="z">The raw ECDH shared secret (X coordinate of shared point).</param>
    /// <returns>The derived key material.</returns>
    byte[] Kdf(ReadOnlySpan<byte> z);
    
    /// <summary>
    /// Encrypts plaintext using AES-256-CBC with the protocol's key derivation.
    /// </summary>
    /// <param name="key">The shared secret key.</param>
    /// <param name="plaintext">The plaintext to encrypt. Must be a multiple of 16 bytes.</param>
    /// <returns>For V1: ciphertext only. For V2: IV || ciphertext.</returns>
    /// <exception cref="ArgumentException">If plaintext is not a multiple of 16 bytes.</exception>
    byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext);
    
    /// <summary>
    /// Decrypts ciphertext using AES-256-CBC with the protocol's key derivation.
    /// </summary>
    /// <param name="key">The shared secret key.</param>
    /// <param name="ciphertext">For V1: ciphertext only. For V2: IV || ciphertext.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="ArgumentException">If ciphertext has invalid length.</exception>
    byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext);
    
    /// <summary>
    /// Computes an authentication tag (MAC) over a message.
    /// </summary>
    /// <param name="key">The shared secret key.</param>
    /// <param name="message">The message to authenticate.</param>
    /// <returns>
    /// For V1: First 16 bytes of HMAC-SHA-256.
    /// For V2: All 32 bytes of HMAC-SHA-256.
    /// </returns>
    byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message);
    
    /// <summary>
    /// Verifies an authentication tag against an expected value.
    /// </summary>
    /// <param name="key">The shared secret key.</param>
    /// <param name="message">The message that was authenticated.</param>
    /// <param name="signature">The authentication tag to verify.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature);
}

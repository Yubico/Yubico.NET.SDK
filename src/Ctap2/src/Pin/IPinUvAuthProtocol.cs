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

namespace Yubico.YubiKit.Ctap2.Pin;

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
///   <item><description>V1: ECDH P-256, SHA-256(z) (32 bytes), AES-256-CBC with a zero IV, and 16-byte HMAC tags.</description></item>
///   <item><description>V2: ECDH P-256, two separate HKDF-SHA-256 derivations (32-byte HMAC key followed by 32-byte AES key), AES-256-CBC with a random IV, and 32-byte HMAC tags.</description></item>
/// </list>
/// </para>
/// <para>
/// Inputs are borrowed for the duration of each synchronous call. The caller owns every returned
/// byte array and must clear secret-bearing or secret-derived output when no longer needed,
/// including on failure in subsequent operations. The V1 and V2 implementations retain no key
/// material between calls; disposing them does not clear returned arrays. Their cryptographic
/// operations reject use after disposal, but their version and tag-length properties remain readable.
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
    /// <param name="peerCoseKey">
    /// The borrowed authenticator key and coordinate arrays, which are neither retained nor cleared.
    /// CTAP requires kty=2 (EC2), alg=-25, crv=1 (P-256),
    /// and 32-byte big-endian X and Y coordinates (-2 and -3), with no other optional parameters.
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><description>KeyAgreement: A new platform COSE public-key dictionary with exported public coordinates to send to the authenticator. These coordinates are not secret material.</description></item>
    ///   <item><description>SharedSecret: A new caller-owned array, 32 bytes for V1 or 64 bytes for V2. Clear it after the last operation that uses it.</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="peerCoseKey"/> is null.</exception>
    /// <remarks>
    /// The V1/V2 helper explicitly checks coordinate types and lengths, but currently does not
    /// validate the required kty, alg and crv labels or the optional-parameter restriction.
    /// Curve-point validation is delegated to the cryptographic provider; this limitation does
    /// not relax CTAP's required key structure. Each call generates a fresh ephemeral key pair.
    /// </remarks>
    /// <exception cref="ArgumentException">If a coordinate is missing, has the wrong type or length, or ECDH derivation raises a cryptographic error.</exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">A cryptographic provider may reject the peer key during import, before the derivation error is wrapped.</exception>
    (Dictionary<int, object?> KeyAgreement, byte[] SharedSecret) Encapsulate(
        IReadOnlyDictionary<int, object?> peerCoseKey);

    /// <summary>
    /// Derives a symmetric key from a raw ECDH shared secret using the protocol's KDF.
    /// </summary>
    /// <param name="z">The borrowed raw ECDH secret: the 32-byte big-endian X coordinate of the shared point. V1/V2 currently reject only empty input, not other lengths.</param>
    /// <returns>A new caller-owned array: SHA-256(z), 32 bytes, for V1; HMAC key || AES key, 64 bytes, for V2. Clear both the input secret and this result when no longer needed.</returns>
    /// <exception cref="ArgumentException">If <paramref name="z"/> is empty.</exception>
    byte[] Kdf(ReadOnlySpan<byte> z);

    /// <summary>
    /// Encrypts plaintext using AES-256-CBC without padding.
    /// </summary>
    /// <param name="key">The borrowed, already-derived shared secret: 32 bytes for V1 or 64 bytes for V2, which uses the final 32 bytes as its AES key. This method does not call Kdf.</param>
    /// <param name="plaintext">The borrowed plaintext, a nonempty multiple of 16 bytes. PIN right-zero-padding to 64 bytes is performed by ClientPin, not by AES.</param>
    /// <returns>A new caller-owned array: N bytes of ciphertext for V1, or a 16-byte random IV || N bytes of ciphertext for V2, where N is the plaintext length.</returns>
    /// <exception cref="ArgumentException">If the key length is invalid or plaintext is not a nonempty multiple of 16 bytes.</exception>
    byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// Decrypts ciphertext using AES-256-CBC without removing padding or verifying integrity.
    /// </summary>
    /// <param name="key">The borrowed, already-derived shared secret: 32 bytes for V1 or 64 bytes for V2, which uses the final 32 bytes as its AES key. This method does not call Kdf.</param>
    /// <param name="ciphertext">Borrowed V1 ciphertext, a nonempty multiple of 16 bytes; or V2 IV || ciphertext, at least 32 bytes and a multiple of 16 bytes.</param>
    /// <returns>A new caller-owned plaintext array: the input length for V1, or the input length minus 16 for V2. Clear it when no longer needed.</returns>
    /// <exception cref="ArgumentException">If the key or ciphertext length is invalid.</exception>
    byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> ciphertext);

    /// <summary>
    /// Computes an authentication tag (MAC) over a message.
    /// </summary>
    /// <param name="key">A borrowed token or derived shared secret: 16 or 32 bytes for V1; 32 or 64 bytes for V2, which uses the first 32 bytes as its HMAC key.</param>
    /// <param name="message">The borrowed message to authenticate; it may be empty.</param>
    /// <returns>
    /// A new caller-owned, secret-derived tag: the first 16 bytes of HMAC-SHA-256 for V1,
    /// or all 32 bytes for V2. Clear it after its last use.
    /// </returns>
    /// <exception cref="ArgumentException">If the key length is invalid.</exception>
    byte[] Authenticate(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message);

    /// <summary>
    /// Checks a local authentication tag using a constant-time comparison for equal-length tags.
    /// </summary>
    /// <param name="key">The borrowed token or shared secret accepted by Authenticate.</param>
    /// <param name="message">The borrowed message that was authenticated.</param>
    /// <param name="signature">The borrowed tag: 16 bytes for V1 or 32 bytes for V2.</param>
    /// <returns>True if the tag matches; false for a mismatch or wrong tag length. This does not check device token validity, permissions, RP binding or expiry.</returns>
    /// <exception cref="ArgumentException">If the tag has the expected length but the key length is invalid.</exception>
    bool Verify(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature);
}
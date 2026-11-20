// Copyright 2026 Yubico AB
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

using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>
///     Reads Security Domain metadata and manages its secure-channel keys and associated data.
/// </summary>
/// <remarks>
///     Read-only discovery does not require SCP. Module examples use SCP03 for administration.
///     SCP11 permissions are command-specific: the SCP11 v1.3.0.13 Public Review draft (August 2023,
///     §4.6) forbids PUT KEY, DELETE [keys], and SET STATUS over SCP11c. This draft restriction does not
///     establish final-specification or YubiKey-profile behavior. SCP11b authenticates the device, not
///     the host; channel establishment alone does not grant authority for a particular operation.
///     Cancellation does not roll back persistent changes already accepted by the device.
///     IsAuthenticated records Core channel setup, not ongoing channel health or administrative authority.
///     The same Public Review draft, §4.3, includes the first correct-MAC APDU after setup in a/c host
///     authentication. Await entire public tasks before disposal; individual guarded exchanges do not
///     serialize a complete reset or multi-query workflow.
/// </remarks>
public interface ISecurityDomainSession : IApplicationSession
{
    /// <summary>
    ///     Sends GET DATA to the selected Security Domain and returns its response data.
    /// </summary>
    /// <param name="dataObject">Data object identifier in the range 0x0000–0xFFFF, encoded as P1/P2.</param>
    /// <param name="requestData">Optional request payload sent alongside the GET DATA command.</param>
    /// <param name="expectedResponseLength">Le value, default zero; not an exact returned-byte count.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The response data, without the status word.</returns>
    /// <remarks>
    ///     The caller supplies any object-specific request encoding. The current implementation sends
    ///     only the low two bytes of <paramref name="dataObject" />; it does not validate that range.
    ///     Keep requestData valid and unchanged until the actual task completes. The selected formatter
    ///     appends Le: short format accepts 0..255 and encodes zero as 00; extended format accepts
    ///     0..65536 and encodes zero or 65536 as 0000. These zero encodings request the format's maximum
    ///     response length, not an empty response; the actual result still depends on the device.
    ///     The result aliases response storage and remains reachable after session disposal; it is not
    ///     a pooled loan. The facade hides the response's supported clear handle. Clearing a caller copy
    ///     does not clear that owner or earlier transport/SCP copies, so no complete secret-result erasure
    ///     guarantee is provided for arbitrary data objects.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The expected response length is negative.</exception>
    /// <exception cref="ArgumentException">The expected response length exceeds the selected formatter's bounds.</exception>
    Task<ReadOnlyMemory<byte>> GetDataAsync(
        int dataObject,
        ReadOnlyMemory<byte> requestData = default,
        int expectedResponseLength = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves key metadata exposed by the Security Domain via the key information data object.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Parsed key references and component descriptors, or an empty list for an empty response.</returns>
    /// <remarks>
    ///     This is metadata, not key material or a complete raw response. The current parser reads component
    ///     bytes in pairs and ignores an unmatched trailing byte; it does not validate every descriptor.
    /// </remarks>
    /// <exception cref="Yubico.YubiKit.Core.BadResponseException">A key-information value is shorter than its two-byte reference.</exception>
    Task<IReadOnlyList<KeyInfo>> ListKeyInformationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the Security Domain card recognition data (tag 0x73).
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The value bytes of nested tag 0x73 from GET DATA 0x66, without its tag or length.</returns>
    /// <exception cref="InvalidOperationException">The recognition value is absent or empty.</exception>
    Task<ReadOnlyMemory<byte>> GetCardRecognitionDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the certificate bundle associated with the supplied key reference.
    /// </summary>
    /// <param name="keyReference">Key reference identifying the certificate store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    ///     Certificates in the order received from the device. The caller owns each
    ///     returned certificate and must dispose it when no longer needed, independently of the session.
    /// </returns>
    /// <remarks>
    ///     Expected store order is issuer to leaf, with the Security Domain key's certificate last.
    ///     Loading does not sort certificates or validate signatures, trust, validity periods, chain
    ///     order, or association with the referenced key. Establish trust before using a retrieved public key.
    /// </remarks>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    ///     A certificate cannot be loaded. Already-loaded certificates are disposed and no partial bundle is returned.
    /// </exception>
    Task<IReadOnlyList<X509Certificate2>> GetCertificatesAsync(
        KeyReference keyReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the supported CA identifiers (KLOC/KLCC) exposed by the Security Domain.
    /// </summary>
    /// <param name="identifierTypes">KLOC, KLCC, or their bitwise combination; None is not a request.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Parsed key-reference/identifier pairs, with KLOC entries before KLCC entries.</returns>
    /// <remarks>
    ///     A group reported as data-not-found contributes no entries. The current parser assumes paired
    ///     identifier/reference TLVs and two-byte references; it does not validate every tag or length,
    ///     and ignores an unmatched trailing TLV. The result is not a complete raw response.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is None or contains unsupported flags.</exception>
    Task<IReadOnlyList<CaIdentifier>> GetCaIdentifiersAsync(
        CaIdentifierType identifierTypes,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a certificate bundle for the specified key reference.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the certificate bundle.</param>
    /// <param name="certificates">Certificates to persist, with the leaf certificate last.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Stores certificates associated with a Security Domain key, not the host's Off-Card Entity
    ///     authentication chain. The caller supplies the order and verifies the bundle's suitability;
    ///     the SDK sends each certificate's DER encoding without chain validation. Device-accepted
    ///     administrative authority is required. The caller retains responsibility for the certificates.
    /// </remarks>
    /// <exception cref="ArgumentException">A certificate's encoded data cannot be obtained.</exception>
    Task StoreCertificatesAsync(
        KeyReference keyReference,
        IReadOnlyList<X509Certificate2> certificates,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Imports an SCP03 static key set into the Security Domain.
    /// </summary>
    /// <param name="keyReference">SCP03 KID 0x01 and a custom KVN in the YubiKey range 0x01–0xFE.</param>
    /// <param name="staticKeys">AES-128 ENC/MAC/DEK values to load. The caller retains ownership.</param>
    /// <param name="replaceKvn">0 to import a new version, or the existing KVN to replace, in 0x01–0xFE.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Requires a session data encryptor and device-accepted administrative authority. On a factory
    ///     YubiKey, the first SCP03 import replaces the default set even when replaceKvn is 0. With custom
    ///     sets already installed, 0 adds a version; a nonzero value requests replacement. Escrow secrets
    ///     before import and verify a fresh session with the new keys before deleting any retained old set.
    ///     The SDK does not enforce every device range: replaceKvn is currently cast to one byte.
    ///     Response validation compares the three KCVs, not the returned version or all trailing data.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The static key set is null.</exception>
    /// <exception cref="ArgumentException">The reference does not use KID 0x01.</exception>
    /// <exception cref="InvalidOperationException">No data encryptor is available, or KCV validation fails.</exception>
    // The overloads represent distinct SCP03, EC public-key, and EC private-key payloads.
#pragma warning disable RS0026
    Task PutKeyAsync(
        KeyReference keyReference,
        StaticKeys staticKeys,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Puts an EC public key onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="publicKey">The EC public key parameters to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Use a NIST P-256 CA-KLOC public key reference when provisioning host authentication for SCP11a/c.
    ///     Device-accepted administrative authority is required; this overload does not check it locally.
    ///     Supply a one-byte replacement version: the current implementation casts replaceKvn to byte
    ///     without range validation. The caller retains responsibility for the supplied key.
    /// </remarks>
    /// <exception cref="ArgumentException">The public key is not NIST P-256.</exception>
    /// <exception cref="InvalidOperationException">The response checksum does not match the requested version.</exception>
    Task PutKeyAsync(
        KeyReference keyReference,
        ECPublicKey publicKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Puts an EC private key onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="privateKey">The EC private key parameters to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Imports a NIST P-256 Security Domain private key using the session's data encryptor.
    ///     Device-accepted administrative authority is also required. The caller retains responsibility
    ///     for the private key; keep it in a using scope. Supply a one-byte replacement version: the
    ///     current implementation casts replaceKvn to byte without range validation.
    /// </remarks>
    /// <exception cref="ArgumentException">The private key is not NIST P-256.</exception>
    /// <exception cref="InvalidOperationException">No data encryptor is available, or the response checksum does not match.</exception>
    Task PutKeyAsync(
        KeyReference keyReference,
        ECPrivateKey privateKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default);
#pragma warning restore RS0026

    /// <summary>
    ///     Generates a NIST P-256 SCP11 key pair on the device and returns its public key.
    /// </summary>
    /// <param name="keyReference">SCP11 device-key reference that will receive the generated pair.</param>
    /// <param name="replaceKvn">0 for a new version, or an existing version to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An ECPublicKey whose PublicPoint is the uncompressed point (0x04 || X || Y).</returns>
    /// <remarks>
    ///     Requires device-accepted administrative authority. Persist the public key through a trusted
    ///     provisioning path; the private key stays on the device. The method does not locally validate
    ///     the SCP11 KID. It checks firmware support only when an explicit version was supplied at creation;
    ///     reset currently clears that explicit-version indicator.
    /// </remarks>
    /// <exception cref="NotSupportedException">An explicitly supplied firmware version is below 5.7.2.</exception>
    /// <exception cref="Yubico.YubiKit.Core.BadResponseException">The response is empty.</exception>
    Task<ECPublicKey> GenerateKeyAsync(
        KeyReference keyReference,
        byte replaceKvn = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes keys matching the supplied reference.
    /// </summary>
    /// <param name="keyReference">KID/KVN filter. A zero field omits that filter; both must not be zero.</param>
    /// <param name="deleteLast">Sets the device flag permitting deletion of the last key; not a host-side safety check.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Requires device-accepted administrative authority. A filter may match multiple keys. SCP03
    ///     component KIDs 0x01, 0x02 and 0x03 are normalized to a KVN-only filter, so supply a nonzero KVN:
    ///     deletion affects the whole matching set, not just one component. Do not use SCP03 with KVN 0;
    ///     the current implementation does not reject the resulting empty filter.
    ///     Yubico documents last-SCP03-set deletion as restoring the default set. This is destructive,
    ///     not a way to leave the device protected with no keys.
    /// </remarks>
    /// <exception cref="ArgumentException">Both supplied filter fields are zero.</exception>
    Task DeleteKeyAsync(
        KeyReference keyReference,
        bool deleteLast = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores an allowlist of certificate serial numbers for a specified key reference.
    /// </summary>
    /// <param name="keyReference">The CA-KLOC public-key reference whose serial restriction will be replaced.</param>
    /// <param name="serials">
    ///     Nonempty collection of certificate serials as hexadecimal byte sequences in the required
    ///     device order. Supply nonempty entries; the SDK currently permits an empty individual string.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Replaces the referenced CA-KLOC key's serial restriction and requires host-authenticated
    ///     administrative authority, not SCP11b. The SCP11 v1.3.0.13 Public Review draft (August 2023,
    ///     §§4.3, 7.5.1 and 7.9) checks the certificate directly signed by the CA: either the OCE certificate
    ///     or the first intermediate, not every leaf serial. Removing the list lifts that restriction;
    ///     use ClearAllowListAsync deliberately, not an empty collection. A list is not a deny-all mechanism.
    ///     The SDK does not emit an SCP11c allowlist counter; SCP11c update applicability must be checked
    ///     against the device profile. This is not a claim of final-specification conformance.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The collection or a serial is null.</exception>
    /// <exception cref="ArgumentException">The collection is empty.</exception>
    /// <exception cref="FormatException">A serial has odd length or contains a non-hexadecimal character.</exception>
    Task StoreAllowListAsync(
        KeyReference keyReference,
        IReadOnlyCollection<string> serials,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes the referenced CA-KLOC key's certificate serial restriction.
    /// </summary>
    /// <param name="keyReference">The key reference that holds the allow list.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Sends a zero-length allowlist TLV. This lifts the serial restriction, allowing certificates
    ///     signed by the CA subject to the other authentication checks; it does not deny all hosts.
    ///     To stop trusting the CA, delete its key. Requires host-authenticated administrative authority.
    ///     The same draft/profile and SCP11c counter qualifications as StoreAllowListAsync apply.
    /// </remarks>
    Task ClearAllowListAsync(
        KeyReference keyReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores data in the Security Domain using the GlobalPlatform STORE DATA command.
    /// </summary>
    /// <param name="data">Caller-encoded BER-TLV payload for the selected Security Domain operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Sends one logical STORE DATA command with header 00 E2 90 00; Core may fragment it into
    ///     chained APDUs. The SDK does not validate the payload,
    ///     select a different application, or implement a multi-block personalization flow. The caller
    ///     supplies valid operation-specific encoding and device-accepted administrative authority;
    ///     lack of authority is not checked locally by this method.
    /// </remarks>
    Task StoreDataAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a CA Subject Key Identifier associated with a Security Domain key reference.
    /// </summary>
    /// <param name="keyReference">A reference to the key for which to store the CA issuer.</param>
    /// <param name="ski">The Subject Key Identifier to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Requires device-accepted administrative authority. SCP11a/b/c device-key KIDs select the
    ///     CA-KLCC identifier; other KIDs select CA-KLOC. This stores an identifier, not a CA certificate
    ///     or public key, and does not establish trust by itself.
    /// </remarks>
    Task StoreCaIssuerAsync(
        KeyReference keyReference,
        ReadOnlyMemory<byte> ski,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Attempts a destructive Security Domain reset through failed authentication, then reselects the application.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <remarks>
    ///     Use an unauthenticated session so retained keys do not prevent post-reset reinitialization.
    ///     The operation targets Security Domain keys and associated data, not other applets' credentials.
    ///     It sends at most 65 raw attempts per processed reference, skipping SCP03 component duplicates.
    ///     Device reset is intended to remove custom state; Yubico documents restoration of factory SCP03
    ///     defaults when the last SCP03 set is deleted. The SDK does not verify factory contents afterward:
    ///     ordinary transmission errors are retried, reaching the attempt limit does not throw, and an
    ///     empty key listing skips reset. Return alone therefore does not prove complete restoration.
    ///     Failure or cancellation can leave persistent partial changes and cannot roll them back.
    ///     Currently reinitialization omits the original protocol configuration and firmware override.
    ///     Dispose this session and create a fresh one with those options when they are required after reset.
    ///     SCP11 reinitialization can fail at the local version gate before a new handshake. After a
    ///     failed/cancelled reset, await completion and dispose/reopen rather than relying on retained
    ///     readiness/authentication flags; raw attempts can have changed device/channel state before flags clear.
    /// </remarks>
    /// <exception cref="SecureChannelException">
    ///     Retained SCP parameters cause a selected secure-channel configuration failure during reinitialization.
    ///     The original failure is preserved as InnerException.
    /// </exception>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
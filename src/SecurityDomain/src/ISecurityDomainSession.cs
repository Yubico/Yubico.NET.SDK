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
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.SecurityDomain;

public interface ISecurityDomainSession : IApplicationSession
{
    /// <summary>
    ///     Executes the GlobalPlatform GET DATA command for the specified data object.
    /// </summary>
    /// <param name="dataObject">Two-byte identifier, combined as {P1,P2}, selecting the data object.</param>
    /// <param name="requestData">Optional request payload sent alongside the GET DATA command.</param>
    /// <param name="expectedResponseLength">Optional expected response length encoded in the Le field.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<ReadOnlyMemory<byte>> GetDataAsync(
        int dataObject,
        ReadOnlyMemory<byte> requestData = default,
        int expectedResponseLength = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves key metadata exposed by the Security Domain via the key information data object.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<KeyInfo>> GetKeyInfoAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the Security Domain card recognition data (tag 0x73).
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The raw card recognition TLV payload.</returns>
    Task<ReadOnlyMemory<byte>> GetCardRecognitionDataAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the certificate bundle associated with the supplied key reference.
    /// </summary>
    /// <param name="keyReference">Key reference identifying the certificate store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A list of certificates where the leaf certificate appears last.</returns>
    Task<IReadOnlyList<X509Certificate2>> GetCertificatesAsync(
        KeyReference keyReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves the supported CA identifiers (KLOC/KLCC) exposed by the Security Domain.
    /// </summary>
    /// <param name="includeKloc">Whether to include Key Loading OCE Certificate identifiers.</param>
    /// <param name="includeKlcc">Whether to include Key Loading Card Certificate identifiers.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<CaIdentifier>> GetCaIdentifiersAsync(
        bool includeKloc,
        bool includeKlcc,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores a certificate bundle for the specified key reference.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the certificate bundle.</param>
    /// <param name="certificates">Certificates to persist, with the leaf certificate last.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task StoreCertificatesAsync(
        KeyReference keyReference,
        IReadOnlyList<X509Certificate2> certificates,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Imports an SCP03 static key set into the Security Domain.
    /// </summary>
    /// <param name="keyReference">Key reference that will receive the key set.</param>
    /// <param name="staticKeys">Static ENC/MAC/DEK values to load.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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
    Task PutKeyAsync(
        KeyReference keyReference,
        ECPrivateKey privateKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a new SCP11 key pair on the device and returns the public point.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the generated key pair.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The uncompressed EC public point (0x04 || X || Y).</returns>
    Task<ECPublicKey> GenerateKeyAsync(
        KeyReference keyReference,
        byte replaceKvn = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes keys matching the supplied reference.
    /// </summary>
    /// <param name="keyReference">Key reference describing the key(s) to delete.</param>
    /// <param name="deleteLast">Whether the last remaining key may be deleted.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task DeleteKeyAsync(
        KeyReference keyReference,
        bool deleteLast = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores an allowlist of certificate serial numbers for a specified key reference.
    /// </summary>
    /// <param name="keyReference">A reference to the key for which the allowlist will be stored.</param>
    /// <param name="serials">The list of certificate serial numbers to be stored in the allowlist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task StoreAllowListAsync(
        KeyReference keyReference,
        IReadOnlyCollection<string> serials,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Clears the allow list for the given <see cref="KeyReference" />.
    /// </summary>
    /// <param name="keyReference">The key reference that holds the allow list.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ClearAllowListAsync(
        KeyReference keyReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Stores data in the Security Domain using the GlobalPlatform STORE DATA command.
    /// </summary>
    /// <param name="data">The data to be stored, formatted as BER-TLV structures according to ISO 8825.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task StoreDataAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Store the SKI (Subject Key Identifier) for the CA of a given key.
    /// </summary>
    /// <param name="keyReference">A reference to the key for which to store the CA issuer.</param>
    /// <param name="ski">The Subject Key Identifier to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task StoreCaIssuerAsync(
        KeyReference keyReference,
        ReadOnlyMemory<byte> ski,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Performs a factory reset by blocking all registered key references and reinitializing the session.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

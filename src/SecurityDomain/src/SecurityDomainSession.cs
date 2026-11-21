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

using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.Devices;
using Yubico.YubiKit.Core.Protocols.SmartCard.Apdu;
using Yubico.YubiKit.Core.Protocols.SmartCard.Scp;
using Yubico.YubiKit.Core.Sessions;
using Yubico.YubiKit.Core.Transports.SmartCard;
using Yubico.YubiKit.Core.Utilities;
using Yubico.YubiKit.SecurityDomain.Backend;

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>
///     Entry point for interacting with the YubiKey Security Domain application.
///     Reads metadata and manages SCP keys, certificates, CA identifiers, and allowlists.
/// </summary>
public sealed class SecurityDomainSession : ApplicationSession, ISecurityDomainSession
{
    /// <summary>Security Domain application descriptor, with minimum firmware 5.3.0.</summary>
    public static readonly Feature FeatureSecurityDomain = new("Security Domain", 5, 3, 0);
    /// <summary>SCP03 descriptor, with minimum firmware 5.3.0.</summary>
    public static readonly Feature FeatureScp03 = new("SCP03", 5, 3, 0);
    /// <summary>SCP11 descriptor, with minimum firmware 5.7.2.</summary>
    public static readonly Feature FeatureScp11 = new("SCP11", 5, 7, 2);
    /// <summary>Key-attestation descriptor at firmware 5.7.0; this session exposes no attestation operation.</summary>
    public static readonly Feature FeatureKeyAttestation = new("Key Attestation", 5, 7, 0);
    /// <summary>Allowlist descriptor at firmware 5.7.0; not an independent check of protocol or device applicability.</summary>
    public static readonly Feature FeatureAllowList = new("Allow List", 5, 7, 0);

    private const byte ClaGlobalPlatform = 0x80;
    private const byte InsGetData = 0xCA;
    private const byte InsPutKey = 0xD8;
    private const byte InsInitializeUpdate = 0x50;
    private const byte InsPerformSecurityOperation = 0x2A;
    private const byte InsInternalAuthenticate = 0x88;
    private const byte InsExternalAuthenticate = 0x82;
    private const byte InsDelete = 0xE4;
    private const byte InsGenerateKey = 0xF1;

    private const byte TagKeyInformationTemplate = 0xE0;
    private const byte TagKeyInformationData = 0xC0;
    private const byte TagSerialsAllowList = 0x70;
    private const byte TagSerial = 0x93;

    private const byte TagControlReference = 0xA6;
    private const byte TagKidKvn = 0x83;
    private const byte TagCaIssuerSki = 0x42;

    private const byte DeleteLastFlag = 0x01; // P2 flag for "delete last"

    private const byte KeyTypeEccPrivateKey = 0xB1;
    private const byte KeyTypeEccPublicKey = 0xB0;
    private const byte KeyTypeEccKeyParams = 0xF0;

    private const byte TagCardData = 0x66;
    private const byte TagCardRecognitionData = 0x73;
    private const int TagCertificateStore = 0xBF21;
    private const int TagCaKlocIdentifiers = 0xFF33; // Key Loading OCE Certificate
    private const int TagCaKlccIdentifiers = 0xFF34; // Key Loading Card Certificate

    private const int ResetAttemptLimit = 65;
    private static readonly byte[] ResetAttemptPayload = new byte[8];
    private readonly ILogger _logger;

    private readonly ProtocolConfiguration? _configuration;
    private readonly FirmwareVersion _framingFirmwareVersion;
    private readonly bool _admitUnverifiedFirmware;
    private ISmartCardProtocol? _protocol;
    private ISecurityDomainBackend? _backend;
    private bool _endingAfterResetFailure;

    // Per-session test seam for observing real certificate lifetimes without replacing the loader.
    internal Action<X509Certificate2>? CertificateLoadedObserver { get; set; }

    /// <summary>
    ///     Retains the caller's connection and protocol configuration for asynchronous initialization.
    /// </summary>
    private SecurityDomainSession(
        ISmartCardConnection connection,
        SessionCreationOptions? options,
        DeviceInfo? cachedDeviceInfo)
        : base(connection)
    {
        _logger = Logger;
        _configuration = options?.ProtocolConfiguration;
        var overrideVersion = options?.FirmwareVersionOverride;
        var knownVersion = overrideVersion ?? cachedDeviceInfo?.FirmwareVersion;
        _framingFirmwareVersion = knownVersion ?? FirmwareVersion.V5_3_0;
        _admitUnverifiedFirmware = knownVersion?.IsAlphaOrBeta != false ||
            overrideVersion is null &&
            cachedDeviceInfo?.VersionQualifier is { Type: not VersionQualifierType.Final };
    }

    /// <summary>
    ///     Get the encryptor to encrypt any data for an SCP command.
    ///     <seealso cref="PcscProtocolScp.GetDataEncryptor" />
    /// </summary>
    /// <returns>
    ///     An encryptor function that takes the plaintext as a parameter and
    ///     returns the encrypted data.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     If the data encryption key has not been set on the session keys.
    /// </exception>
    private DataEncryptor EncryptData => _protocol is PcscProtocolScp scpConnection
        ? scpConnection.GetDataEncryptor()
        : throw new InvalidOperationException("No data encryptor available for secure connection.");

    /// <summary>
    ///     Factory helper that creates and initializes a Security Domain session.
    /// </summary>
    /// <param name="connection">Caller-owned SmartCard connection, left open when the session is disposed.</param>
    /// <param name="options">Optional protocol configuration, SCP parameters and firmware override.</param>
    /// <param name="cancellationToken">Token used to cancel creation.</param>
    /// <returns>An initialized session. Dispose it before reusing or disposing the connection.</returns>
    /// <remarks>
    ///     Without SCP parameters the session supports unauthenticated discovery. The caller retains
    ///     ownership of supplied key parameters and disposes them after the session. Security Domain
    ///     cannot detect firmware: this direct factory uses the supplied override or configures framing
    ///     with 5.3.0. Unknown and major-zero firmware admit firmware-gated attempts without promising
    ///     support. Reset reapplies configuration and
    ///     firmware policy, but switches to the public default SCP03 keys after confirmed stopping outcomes.
    ///     A known production version below 5.7.2 still rejects SCP11 locally. Keep supplied parameters and their contained
    ///     key/certificate owners alive through session disposal. The SDK does not dispose them.
    /// </remarks>
    /// <exception cref="SecureChannelException">
    ///     SCP parameters were supplied and secure-channel configuration threw ApduException,
    ///     BadResponseException, or NotSupportedException. The original failure is InnerException.
    ///     Earlier protocol preparation/application selection failures and cancellation are not wrapped here.
    /// </exception>
    public static Task<SecurityDomainSession> CreateAsync(
        ISmartCardConnection connection,
        SessionCreationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        CreateForDeviceAsync(connection, options, cachedDeviceInfo: null, cancellationToken);

    internal static Task<SecurityDomainSession> CreateForDeviceAsync(
        ISmartCardConnection connection,
        SessionCreationOptions? options,
        DeviceInfo? cachedDeviceInfo,
        CancellationToken cancellationToken) =>
        ConstructInitializedAsync(
            connection,
            options,
            () => new SecurityDomainSession(connection, options, cachedDeviceInfo),
            (session, token) => session.InitializeAsync(options?.ScpKeyParameters, token),
            cancellationToken);

    /// <summary>
    ///     Ensures the Security Domain application is selected and secure messaging is configured if requested.
    /// </summary>
    /// <param name="scpKeyParameters">Borrowed parameters for this handshake.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    private async Task InitializeAsync(
        ScpKeyParameters? scpKeyParameters,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (IsInitialized)
            return;

        var protocol = (ISmartCardProtocol)await PrepareProtocolAsync(scpKeyParameters, cancellationToken)
            .ConfigureAwait(false);
        ISecurityDomainBackend backend = new SecurityDomainBackend(protocol);
        await backend.SelectApplicationAsync(cancellationToken).ConfigureAwait(false);

        ISmartCardProtocol effectiveProtocol;
        try
        {
            effectiveProtocol = (ISmartCardProtocol)await ConfigureProtocolAsync(
                    protocol,
                    _framingFirmwareVersion,
                    _configuration,
                    scpKeyParameters,
                    cancellationToken,
                    scpEligibilityFirmwareVersion: _admitUnverifiedFirmware ? FirmwareVersion.Default : null)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (scpKeyParameters is not null &&
            ex is ApduException or BadResponseException or NotSupportedException)
        {
            // Wrap selected configuration failures only when SCP was requested. Preparation and
            // application selection occur before this catch; operation failures are outside it.
            throw new SecureChannelException(ex);
        }

        if (!ReferenceEquals(protocol, effectiveProtocol))
        {
            backend = new SecurityDomainBackend(effectiveProtocol);
        }

        _protocol = effectiveProtocol;
        _backend = backend;
    }

    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetDataAsync(
        int dataObject,
        ReadOnlyMemory<byte> requestData = default,
        int expectedResponseLength = 0,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (expectedResponseLength < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedResponseLength),
                "Expected response length cannot be negative.");

        var command = new ApduCommand
        {
            Cla = 0,
            Ins = InsGetData,
            P1 = (byte)(dataObject >> 8),
            P2 = (byte)(dataObject & 0xFF),
            Data = requestData,
            Le = expectedResponseLength
        };

        _logger.LogDebug("Sending GET DATA for object 0x{DataObject:X4}", dataObject);

        try
        {
            return await TransmitAndGetResponseDataAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            try
            {
                _logger.LogWarning(ex, "GET DATA for object 0x{DataObject:X4} failed", dataObject);
            }
            catch
            {
                // Diagnostics must not replace the error from the attempted GET DATA.
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KeyInfo>> ListKeyInformationAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(TagKeyInformationTemplate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.IsEmpty)
            return [];

        var keyInformation = new List<KeyInfo>();

        using var tlvList = DecodeListing(response.Span);
        foreach (var tlv in tlvList)
        {
            var valueSpan = tlv.Value.Span;
            if (tlv.Tag != TagKeyInformationData || valueSpan.Length < 2 || valueSpan.Length % 2 != 0)
                throw new BadResponseException("Malformed key information entry.");
            var keyRef = new KeyReference(valueSpan[0], valueSpan[1]);

            var components = new List<SecurityDomainKeyInfoComponent>();
            var currentValue = valueSpan[2..];

            while (currentValue.Length >= 2)
            {
                components.Add(new SecurityDomainKeyInfoComponent(currentValue[0], currentValue[1]));
                currentValue = currentValue[2..];
            }

            keyInformation.Add(new KeyInfo(keyRef, components));
        }

        return keyInformation;
    }


    /// <inheritdoc />
    public async Task<ReadOnlyMemory<byte>> GetCardRecognitionDataAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Getting card recognition data");

        var response = await GetDataAsync(TagCardData, cancellationToken: cancellationToken).ConfigureAwait(false);
        var cardRecognitionData = TlvHelper.GetValue(TagCardRecognitionData, response.Span);

        if (cardRecognitionData.IsEmpty)
            throw new InvalidOperationException("Card recognition data not found in response");

        return cardRecognitionData;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<X509Certificate2>> GetCertificatesAsync(
        KeyReference keyReference,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Getting certificates for Key Reference: {KeyReference}", keyReference);
        using var kidKvnTlv = new Tlv(TagKidKvn, keyReference.AsSpan());
        using var nestedTlvObj = new Tlv(TagControlReference, kidKvnTlv.AsSpan());
        var nestedTlv = nestedTlvObj.AsMemory();

        var certificateTlvData =
            await GetDataAsync(TagCertificateStore, nestedTlv, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        _logger.LogDebug("Certificates retrieved (KeyReference: {KeyReference})", keyReference);
        using var tlvList = TlvHelper.DecodeList(certificateTlvData);
        // Reserve capacity before loading so adding a certificate cannot require another allocation.
        var certificates = new List<X509Certificate2>(tlvList.Count);
        try
        {
            foreach (var tlv in tlvList)
            {
                var certificate = X509CertificateLoader.LoadCertificate(tlv.AsSpan());
                certificates.Add(certificate);
                CertificateLoadedObserver?.Invoke(certificate);
            }

            return certificates;
        }
        catch
        {
            foreach (var certificate in certificates)
                certificate.Dispose();

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaIdentifier>> GetCaIdentifiersAsync(
        CaIdentifierType identifierTypes,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        const CaIdentifierType supportedTypes = CaIdentifierType.Kloc | CaIdentifierType.Klcc;
        if (identifierTypes == CaIdentifierType.None || (identifierTypes & ~supportedTypes) != 0)
            throw new ArgumentOutOfRangeException(nameof(identifierTypes));

        _logger.LogDebug("Getting CA identifier groups {IdentifierTypes}", identifierTypes);

        var caIdentifiers = new List<CaIdentifier>();
        if (identifierTypes.HasFlag(CaIdentifierType.Kloc))
            try
            {
                var kloc = await GetDataAsync(TagCaKlocIdentifiers, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                AppendCaIdentifiers(kloc.Span, caIdentifiers);
            }
            catch (ApduException e) when (e.SW == SWConstants.DataNotFound) // A kloc might not be present
            {
            }

        if (identifierTypes.HasFlag(CaIdentifierType.Klcc))
            try
            {
                var klcc = await GetDataAsync(TagCaKlccIdentifiers, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                AppendCaIdentifiers(klcc.Span, caIdentifiers);
            }
            catch (ApduException e) when (e.SW == SWConstants.DataNotFound) // A klcc might not be present
            {
            }

        _logger.LogDebug("CA identifiers retrieved");
        return caIdentifiers;
    }

    private static void AppendCaIdentifiers(ReadOnlySpan<byte> response, List<CaIdentifier> caIdentifiers)
    {
        using var caTlvList = DecodeListing(response);
        var caTlvObjects = caTlvList.AsSpan();
        if (caTlvObjects.Length % 2 != 0)
            throw new BadResponseException("CA identifier listing contains an incomplete pair.");

        while (!caTlvObjects.IsEmpty)
        {
            var caIdentifierTlv = caTlvObjects[0];
            var keyReferenceTlv = caTlvObjects[1];

            var keyReferenceData = keyReferenceTlv.Value.Span;
            if (caIdentifierTlv.Tag != TagCaIssuerSki || keyReferenceTlv.Tag != TagKidKvn ||
                keyReferenceData.Length != 2)
                throw new BadResponseException("Malformed CA identifier or key reference.");

            var keyReference = new KeyReference(keyReferenceData[0], keyReferenceData[1]);
            caIdentifiers.Add(new CaIdentifier(keyReference, caIdentifierTlv.Value.ToArray()));

            caTlvObjects = caTlvObjects[2..];
        }

    }

    private static DisposableTlvList DecodeListing(ReadOnlySpan<byte> response)
    {
        try
        {
            return TlvHelper.DecodeList(response);
        }
        catch (ArgumentException)
        {
            throw new BadResponseException("Malformed TLV in Security Domain listing.");
        }
    }

    /// <inheritdoc />
    public async Task StoreCertificatesAsync(
        KeyReference keyReference,
        IReadOnlyList<X509Certificate2> certificates,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Storing certificate bundle (KeyReference: {KeyReference})", keyReference);

        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            // Write each certificate's DER-encoded data
            foreach (var cert in certificates)
            {
                try
                {
                    buffer.Write(cert.RawData);
                }
                catch (CryptographicException e)
                {
                    throw new ArgumentException("Failed to get encoded version of certificate", e);
                }
            }

            // Build the TLV structure:
            // - Control Reference (0xA6) containing Key ID/KVN (0x83)
            // - Certificate Store (0xBF21) containing the concatenated certificate data
            using var kidKvnTlv = new Tlv(TagKidKvn, keyReference.AsSpan());
            using var controlRefTlv = new Tlv(TagControlReference, kidKvnTlv.AsSpan());
            using var certStoreTlv = new Tlv(TagCertificateStore, buffer.WrittenSpan);
            var storeData = TlvHelper.EncodeList([controlRefTlv, certStoreTlv]);

            await StoreDataAsync(storeData, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Certificate bundle stored (KeyReference: {KeyReference})", keyReference);
        }
        finally
        {
            buffer.Clear();
        }
    }

    /// <inheritdoc />
    // The three overloads represent distinct SCP03, EC public-key, and EC private-key payloads.
#pragma warning disable RS0026
    public async Task PutKeyAsync(
        KeyReference keyReference,
        StaticKeys staticKeys,
        KeyReplacement replace,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(staticKeys);

        if (keyReference.Kid != 0x01)
            throw new ArgumentException("SCP03 keys must use KID 0x01", nameof(keyReference));

        _logger.LogInformation("Importing SCP03 static keys into Key Reference: {KeyReference}", keyReference);

        // Get session encryptor (requires active SCP session with DEK)
        var encryptor = EncryptData;

        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            // Write KVN
            buffer.Write([keyReference.Kvn]);

            // Encrypt and encode each key component (ENC, MAC, DEK)
            SecurityDomainKeyMaterial.EncodeKeyComponent(staticKeys.Enc, encryptor, buffer);
            SecurityDomainKeyMaterial.EncodeKeyComponent(staticKeys.Mac, encryptor, buffer);
            SecurityDomainKeyMaterial.EncodeKeyComponent(staticKeys.Dek, encryptor, buffer);

            // Build and send PUT KEY command
            // P2: 0x80 | KID (0x80 flag required by GlobalPlatform)
            var command = new ApduCommand(ClaGlobalPlatform, InsPutKey, replace.Kvn,
                (byte)(0x80 | keyReference.Kid),
                buffer.WrittenMemory);
            EnsureInitializedBackend();
            var response = await _backend.SendAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
            try
            {
                SecurityDomainKeyMaterial.ValidateKcv(staticKeys, keyReference.Kvn, response.Data.Span);
            }
            finally
            {
                response.Clear();
            }

            _logger.LogInformation("Successfully imported SCP03 keys for Key Reference: {KeyReference}",
                keyReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import SCP03 keys for Key Reference: {KeyReference}", keyReference);
            throw;
        }
        finally
        {
            buffer.Clear();
        }
    }
#pragma warning restore RS0026

    /// <inheritdoc />
    public Task DeleteKeyAsync(
        KeyReference keyReference,
        bool deleteLast = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var (kid, kvn) = SecurityDomainTlvEncoding.NormalizeDeletePolicy(keyReference);
        var payload = SecurityDomainTlvEncoding.EncodeDeleteFilter(kid, kvn);
        var command = new ApduCommand
        {
            Cla = ClaGlobalPlatform,
            Ins = InsDelete,
            P1 = 0x00,
            P2 = deleteLast ? DeleteLastFlag : (byte)0x00,
            Data = payload
        };

        _logger.LogDebug("Deleting keys matching {KeyRef}{Suffix}",
            keyReference, deleteLast ? " (allow delete last)" : string.Empty);

        return TransmitAndGetResponseDataAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ECPublicKey> GenerateKeyAsync(
        KeyReference keyReference,
        KeyReplacement replace,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (keyReference.Kid is not (ScpKid.SCP11a or ScpKid.SCP11b or ScpKid.SCP11c))
            throw new ArgumentException("GenerateKeyAsync requires an SCP11 key identifier (0x11, 0x13 or 0x15).",
                nameof(keyReference));

        if (!_admitUnverifiedFirmware)
            EnsureSupports(FeatureScp11);

        if (replace.IsNew)
            _logger.LogDebug("Generating EC key for {KeyRef}", keyReference);
        else
            _logger.LogDebug("Generating EC key for {KeyRef}, replacing KVN=0x{ReplaceKvn:X2}", keyReference,
                replace.Kvn);

        Span<byte> parameters = stackalloc byte[3];
        parameters[0] = KeyTypeEccKeyParams;
        parameters[1] = 0x01;
        parameters[2] = 0x00; // SECP256R1 curve indicator

        var commandData = new byte[1 + parameters.Length];
        commandData[0] = keyReference.Kvn;
        parameters.CopyTo(commandData.AsSpan(1));

        var command = new ApduCommand
        {
            Cla = ClaGlobalPlatform,
            Ins = InsGenerateKey,
            P1 = replace.Kvn,
            P2 = keyReference.Kid,
            Data = commandData
        };

        ReadOnlyMemory<byte> response;

        try
        {
            response = await TransmitAndGetResponseDataAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Generate EC key for {KeyRef} failed", keyReference);
            throw;
        }

        if (response.IsEmpty)
            throw new BadResponseException("Generate EC key response was empty.");

        var publicPoint = TlvHelper.GetValue(KeyTypeEccPublicKey, response.Span);
        return ECPublicKey.CreateFromValue(publicPoint, KeyType.P256);
    }

    /// <inheritdoc />
    // The EC public-key import overload has distinct key ownership and wire encoding.
#pragma warning disable RS0026
    public async Task PutKeyAsync(KeyReference keyReference, ECPublicKey publicKey, KeyReplacement replace,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var pkParams = publicKey.Parameters;
        if (pkParams.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            throw new ArgumentException("Public key must be of type NIST P-256");

        await PutEcKeyAsync(keyReference, KeyTypeEccPublicKey, publicKey.PublicPoint, replace,
            cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore RS0026

    /// <inheritdoc />
    // The EC private-key import overload encrypts caller-owned private key material before transfer.
#pragma warning disable RS0026
    public async Task PutKeyAsync(
        KeyReference keyReference,
        ECPrivateKey privateKey,
        KeyReplacement replace,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var parameters = privateKey.Parameters;
        if (parameters.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            throw new ArgumentException("Private key must be of type NIST P-256");

        var encryptedKey = EncryptData(parameters.D.AsSpan());
        // Do not clear the supplied key's private parameters here. The caller retains responsibility
        // for the ECPrivateKey and should keep it in a using scope.

        await PutEcKeyAsync(keyReference, KeyTypeEccPrivateKey, (ReadOnlyMemory<byte>)encryptedKey, replace,
            cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore RS0026

    /// <summary>
    ///     Shared helper for importing an EC key (public or private) into the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="keyType">The GlobalPlatform key type byte (e.g. 0xB0 for public, 0xB1 for private).</param>
    /// <param name="keyData">The key data to embed in the TLV payload (already encrypted for private keys).</param>
    /// <param name="replace">The version to replace, or an explicit request to add a version.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    private async Task PutEcKeyAsync(
        KeyReference keyReference,
        byte keyType,
        ReadOnlyMemory<byte> keyData,
        KeyReplacement replace,
        CancellationToken cancellationToken)
    {
        var keyKindLabel = keyType == KeyTypeEccPublicKey ? "public" : "private";
        _logger.LogInformation("Importing SCP11 {KeyKind} key into Key Reference: {KeyReference}",
            keyKindLabel, keyReference);

        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            buffer.Write([keyReference.Kvn]);

            // Write the EC key data TLV
            using var keyTlv = new Tlv(keyType, keyData.Span);
            buffer.Write(keyTlv.AsSpan());

            // Write the EC parameters
            using var paramsTlv = new Tlv(KeyTypeEccKeyParams, (ReadOnlySpan<byte>)[0x00]); // NIST P-256
            buffer.Write(paramsTlv.AsSpan());
            buffer.Write((ReadOnlySpan<byte>)[0]);

            // Create and send the command
            var command = new ApduCommand(ClaGlobalPlatform, InsPutKey, replace.Kvn, keyReference.Kid,
                buffer.WrittenMemory);
            var response = await TransmitAndGetResponseDataAsync(command, cancellationToken).ConfigureAwait(false);

            // Validate the checksum response
            ReadOnlySpan<byte> expectedResponseData = [keyReference.Kvn];
            SecurityDomainKeyMaterial.ValidateCheckSum(expectedResponseData, response.Span);

            _logger.LogInformation("Successfully put {KeyKind} key for Key Reference: {KeyReference}",
                keyKindLabel, keyReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put {KeyKind} key for Key Reference: {KeyReference}",
                keyKindLabel, keyReference);
            throw;
        }
        finally
        {
            buffer.Clear();
        }
    }

    /// <inheritdoc />
    public async Task StoreAllowListAsync(KeyReference keyReference, IReadOnlyCollection<string> serials,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(serials);
        if (serials.Count == 0)
            throw new ArgumentException(
                "An allowlist must contain at least one serial. Use ClearAllowListAsync to remove an allowlist.",
                nameof(serials));

        _logger.LogDebug("Storing allow list (KeyReference: {KeyReference})", keyReference);

        var buffer = new ArrayBufferWriter<byte>();
        foreach (var serial in serials)
        {
            using var serialTlv = EncodeAllowListSerial(serial);
            buffer.Write(serialTlv.AsSpan());
        }

        using var kidKvnTlv = new Tlv(TagKidKvn, keyReference.AsSpan());
        using var controlRefTlv = new Tlv(TagControlReference, kidKvnTlv.AsSpan());
        using var serialsListTlv = new Tlv(TagSerialsAllowList, buffer.WrittenSpan);
        var serialsDataTl = TlvHelper.EncodeList([controlRefTlv, serialsListTlv]);

        await StoreDataAsync(serialsDataTl, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Allow list stored (KeyReference: {KeyReference})", keyReference);
    }

    private static Tlv EncodeAllowListSerial(string serial)
    {
        ReadOnlySpan<byte> value = Convert.FromHexString(serial);
        if (value.IsEmpty)
            throw new ArgumentException("An allowlist serial cannot be empty.", nameof(serial));

        // Keep one byte for zero; add a sign byte only for a high-bit unsigned value.
        while (value.Length > 1 && value[0] == 0)
            value = value[1..];

        return (value[0] & 0x80) == 0
            ? new Tlv(TagSerial, value)
            : new Tlv(TagSerial, (ReadOnlySpan<byte>)[0x00, .. value]);
    }

    /// <inheritdoc />
    public async Task ClearAllowListAsync(KeyReference keyReference, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Clearing allow list (KeyReference: {KeyReference})", keyReference);

        // Explicit removal uses an empty tag 0x70; an empty input to StoreAllowListAsync is rejected.
        using var kidKvnTlv = new Tlv(TagKidKvn, keyReference.AsSpan());
        using var controlRefTlv = new Tlv(TagControlReference, kidKvnTlv.AsSpan());
        using var emptyListTlv = new Tlv(TagSerialsAllowList, []);
        var data = TlvHelper.EncodeList([controlRefTlv, emptyListTlv]);

        await StoreDataAsync(data, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Allow list cleared (KeyReference: {KeyReference})", keyReference);
    }

    /// <inheritdoc />
    public async Task StoreDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Storing data with length:{Length}", data.Length);

        var command = new ApduCommand(0x00, 0xE2, 0x90, 0x00, data);
        await TransmitAndGetResponseDataAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task StoreCaIssuerAsync(KeyReference keyReference, ReadOnlyMemory<byte> ski,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Storing CA issuer SKI (KeyReference: {KeyReference})", keyReference);

        byte klcc = keyReference.Kid switch //
        {
            ScpKid.SCP11a or ScpKid.SCP11b or ScpKid.SCP11c => 1,
            _ => 0
        };

        // Create and serialize data
        using var klccTlv = new Tlv(ClaGlobalPlatform, [klcc]);
        using var skiTlv = new Tlv(TagCaIssuerSki, ski.Span);
        using var kidKvnTlv = new Tlv(TagKidKvn, keyReference.AsSpan());
        var encodedList = TlvHelper.EncodeList([klccTlv, skiTlv, kidKvnTlv]);
        using var caIssuerData = new Tlv(TagControlReference, encodedList.Span);

        // Send store data command
        await StoreDataAsync(caIssuerData.AsMemory(), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("CA issuer SKI stored (KeyReference: {KeyReference})", keyReference);
    }

    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        var keyInfo = await ListKeyInformationAsync(cancellationToken).ConfigureAwait(false);
        if (!keyInfo.Any(static entry => entry.KeyReference.Kid is not (0x02 or 0x03)))
        {
            _logger.LogInformation("Security Domain reset skipped: no actionable key references reported");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            // A raw attempt can invalidate the selected secure channel and mutate device state.
            var oldProtocol = Protocol;
            Protocol = null;
            _protocol = null;
            _backend = null;
            IsAuthenticated = false;
            IsInitialized = false;
            oldProtocol?.Dispose();

            await BlockResetReferencesAsync(keyInfo, cancellationToken).ConfigureAwait(false);
            using var defaultKeys = Scp03KeyParameters.Default;
            await InitializeSessionAsync(token => InitializeAsync(defaultKeys, token), cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            _endingAfterResetFailure = true;
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task BlockResetReferencesAsync(IReadOnlyList<KeyInfo> keyInfo, CancellationToken cancellationToken)
    {
        var outcomes = new List<string>(keyInfo.Count);
        var failures = new List<Exception>();
        foreach (var entry in keyInfo)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var keyReference = entry.KeyReference;
            if (!TryGetResetParameters(keyReference, out var instruction, out var overrideKeyRef))
            {
                outcomes.Add($"{keyReference}: skipped");
                continue;
            }

            try
            {
                var status = await BlockKeyAsync(instruction, overrideKeyRef, cancellationToken).ConfigureAwait(false);
                outcomes.Add($"{keyReference}: stopped with SW=0x{status:X4}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                outcomes.Add($"{keyReference}: incomplete");
                failures.Add(new InvalidOperationException($"Reset did not complete for {keyReference}.", ex));
            }
        }

        if (failures.Count != 0)
            throw new AggregateException($"Security Domain reset incomplete: {string.Join("; ", outcomes)}.", failures);
    }

    [MemberNotNull(nameof(_protocol), nameof(_backend))]
    private void EnsureInitializedBackend()
    {
        ThrowIfDisposed();

        if (!IsInitialized)
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");

        if (_protocol is null)
            throw new InvalidOperationException("Security Domain protocol not available.");

        if (_backend is null)
            throw new InvalidOperationException("Security Domain backend not available.");
    }

    private async Task<ReadOnlyMemory<byte>> TransmitAndGetResponseDataAsync(
        ApduCommand command,
        CancellationToken cancellationToken)
    {
        EnsureInitializedBackend();
        var response = await _backend.SendAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        // This alias keeps final response storage alive but hides its supported Clear owner. A caller
        // copy has a separate lifetime; session disposal does not erase this result or earlier copies.
        return response.Data;
    }

    private bool TryGetResetParameters(KeyReference keyReference, out byte instruction,
        out KeyReference overrideKeyReference)
    {
        overrideKeyReference = keyReference;
        instruction = 0;

        switch (keyReference.Kid)
        {
            case ScpKid.SCP03: // SCP03_1
                overrideKeyReference = new KeyReference(0x00, 0x00);
                instruction = InsInitializeUpdate;
                return true;
            case 0x02: // SCP03_2
            case 0x03: // SCP03_3
                return false;
            case ScpKid.SCP11a:
            case ScpKid.SCP11c:
                instruction = InsExternalAuthenticate;
                return true;
            case ScpKid.SCP11b:
                instruction = InsInternalAuthenticate;
                return true;
            default:
                instruction = InsPerformSecurityOperation;
                return true;
        }
    }


    private async Task<short> BlockKeyAsync(byte instruction, KeyReference keyReference, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ResetAttemptLimit; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var statusWord = await TransmitResetAttemptAsync(instruction, keyReference, cancellationToken)
                .ConfigureAwait(false);

            if (statusWord is SWConstants.AuthenticationMethodBlocked or SWConstants.SecurityStatusNotSatisfied)
                return statusWord;

            if (statusWord is not (SWConstants.InvalidCommandDataParameter or SWConstants.Success))
                throw ApduException.FromStatusWord(statusWord, "Unexpected Security Domain reset status");
        }

        throw new InvalidOperationException($"No stopping status after {ResetAttemptLimit} reset attempts.");
    }

    private async Task<short> TransmitResetAttemptAsync(
        byte instruction,
        KeyReference keyReference,
        CancellationToken cancellationToken)
    {
        const int HeaderLength = 5; // CLA, INS, P1, P2, Lc
        var commandLength = HeaderLength + ResetAttemptPayload.Length;

        byte[]? rented = null;

        try
        {
            rented = ArrayPool<byte>.Shared.Rent(commandLength);
            var command = rented.AsSpan(0, commandLength);

            command[0] = ClaGlobalPlatform;
            command[1] = instruction;
            command[2] = keyReference.Kvn;
            command[3] = keyReference.Kid;
            command[4] = (byte)ResetAttemptPayload.Length;
            ResetAttemptPayload.CopyTo(command[5..]);

            var response = await ((ISmartCardConnection)Connection)
                .TransmitAndReceiveAsync(rented.AsMemory(0, commandLength), cancellationToken)
                .ConfigureAwait(false);

            if (response.Length < 2)
                throw new BadResponseException("Reset command response shorter than status word.");

            var responseSpan = response.Span;
            var statusWord = (short)((responseSpan[^2] << 8) | responseSpan[^1]);
            return statusWord;
        }
        finally
        {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented, true);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await base.DisposeAsyncCore().ConfigureAwait(false);
        }
        catch when (_endingAfterResetFailure)
        {
            // Do not cache a cleanup failure in the disposal gate: an enclosing await using
            // must not replay it over the primary reset or refresh failure.
        }
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                // These mirror the effective protocol outside ApplicationSession.Protocol. Clear them even
                // if base protocol disposal throws so this session cannot retain a disposed secure channel.
                _protocol = null;
                _backend = null;
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
}
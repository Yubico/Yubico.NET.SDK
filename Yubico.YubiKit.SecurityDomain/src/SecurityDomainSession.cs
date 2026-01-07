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
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.Cryptography;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.Utils;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.SecurityDomain;

/// <summary>
///     Entry point for interacting with the YubiKey Security Domain application.
///     Provides lifecycle management for SCP (Secure Channel Protocol) sessions and will
///     eventually expose key and data management operations.
/// </summary>
public sealed class SecurityDomainSession : ApplicationSession
{
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

    // TLV tags for DeleteKey filter parameters
    private const byte TagKid = 0xD0; // Key ID (KID)
    private const byte TagKvn = 0xD2; // Key Version Number (KVN)
    private const byte DeleteLastFlag = 0x01; // P2 flag for "delete last"

    private const byte KeyTypeAes = 0x88; // AES key type for SCP03
    private const byte KeyTypeEccPrivateKey = 0xB1;
    private const byte KeyTypeEccPublicKey = 0xB0;
    private const byte KeyTypeEccKeyParams = 0xF0;

    private const byte TagCardData = 0x66;
    private const byte TagCardRecognitionData = 0x73;
    private const ushort TagCertificateStore = 0xBF21;
    private const ushort TagCaKlocIdentifiers = 0xFF33; // Key Loading OCE Certificate
    private const ushort TagCaKlccIdentifiers = 0xFF34; // Key Loading Card Certificate

    private const int ResetAttemptLimit = 65;
    private static readonly byte[] ResetAttemptPayload = new byte[8];
    private readonly ISmartCardConnection _connection;
    private readonly ILogger<SecurityDomainSession> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ScpKeyParameters? _scpKeyParams;
    private ISmartCardProtocol? _baseProtocol;
    private bool _isInitialized;
    private ISmartCardProtocol? _protocol;

    /// <summary>
    ///     Entry point for interacting with the YubiKey Security Domain application.
    ///     Provides lifecycle management for SCP (Secure Channel Protocol) sessions and will
    ///     eventually expose key and data management operations.
    /// </summary>
    private SecurityDomainSession(
        ISmartCardConnection connection,
        ILoggerFactory loggerFactory,
        ScpKeyParameters? scpKeyParams = null)
    {
        _connection = connection;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<SecurityDomainSession>();
        _scpKeyParams = scpKeyParams;
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
    public static async Task<SecurityDomainSession> CreateAsync(
        ISmartCardConnection connection,
        ProtocolConfiguration? configuration = null,
        ILoggerFactory? loggerFactory = null,
        ScpKeyParameters? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        loggerFactory ??= NullLoggerFactory.Instance;
        var session = new SecurityDomainSession(connection, loggerFactory, scpKeyParams);

        await session.InitializeAsync(configuration, cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>
    ///     Ensures the Security Domain application is selected and secure messaging is configured if requested.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    private async Task InitializeAsync(
        ProtocolConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        var baseProtocol = EnsureBaseProtocol();
        await baseProtocol
            .SelectAsync(ApplicationIds.SecurityDomain, cancellationToken)
            .ConfigureAwait(false);

        // Security Domain is available on firmware 5.3.0 and newer.
        baseProtocol.Configure(FirmwareVersion.V5_3_0, configuration); // TODO detect actual version from Management???

        _protocol = _scpKeyParams is not null
            ? await baseProtocol
                .WithScpAsync(_scpKeyParams, cancellationToken)
                .ConfigureAwait(false)
            : baseProtocol;

        // _protocol.Configure(FirmwareVersion.V5_3_0, configuration);
        _isInitialized = true;
    }

    /// <summary>
    ///     Executes the GlobalPlatform GET DATA command for the specified data object.
    /// </summary>
    /// <param name="dataObject">Two-byte identifier, combined as {P1,P2}, selecting the data object.</param>
    /// <param name="requestData">Optional request payload sent alongside the GET DATA command.</param>
    /// <param name="expectedResponseLength">Optional expected response length encoded in the Le field.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<ReadOnlyMemory<byte>> GetDataAsync(
        ushort dataObject,
        ReadOnlyMemory<byte> requestData = default,
        int expectedResponseLength = 0,
        CancellationToken cancellationToken = default)
    {
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
            return await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GET DATA for object 0x{DataObject:X4} failed", dataObject);
            throw;
        }
    }

    /// <summary>
    ///     Retrieves key metadata exposed by the Security Domain via the key information data object.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<IReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>> GetKeyInformationAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(TagKeyInformationTemplate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.IsEmpty)
            return new ReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>(
                new Dictionary<KeyReference, IReadOnlyDictionary<byte, byte>>(0));

        var keyInformation = new Dictionary<KeyReference, IReadOnlyDictionary<byte, byte>>();

        using var tlvList = TlvHelper.DecodeList(response.Span);
        foreach (var tlv in tlvList)
        {
            var value = TlvHelper.GetValue(TagKeyInformationData, tlv.AsMemory().Span);
            var keyRef = new KeyReference(value.Span[0], value.Span[1]);
            var components = new Dictionary<byte, byte>();

            var currentValue = value.Span[2..];
            while (!currentValue.IsEmpty)
            {
                components.Add(currentValue[0], currentValue[1]);
                currentValue = currentValue[2..];
            }

            keyInformation.Add(keyRef, new ReadOnlyDictionary<byte, byte>(components));
        }

        return new ReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>(keyInformation);
    }


    /// <summary>
    ///     Retrieves the Security Domain card recognition data (tag 0x73).
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The raw card recognition TLV payload.</returns>
    public async Task<ReadOnlyMemory<byte>> GetCardRecognitionDataAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(TagCardData, cancellationToken: cancellationToken).ConfigureAwait(false);

        throw new NotImplementedException();
    }

    /// <summary>
    ///     Retrieves the certificate bundle associated with the supplied key reference.
    /// </summary>
    /// <param name="keyReference">Key reference identifying the certificate store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A list of certificates where the leaf certificate appears last.</returns>
    public Task<IReadOnlyList<X509Certificate2>> GetCertificatesAsync(
        KeyReference keyReference,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Retrieves the supported CA identifiers (KLOC/KLCC) exposed by the Security Domain.
    /// </summary>
    /// <param name="includeKloc">Whether to include Key Loading OCE Certificate identifiers.</param>
    /// <param name="includeKlcc">Whether to include Key Loading Card Certificate identifiers.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A dictionary keyed by key reference containing identifier payloads.</returns>
    public Task<IReadOnlyDictionary<KeyReference, ReadOnlyMemory<byte>>> GetSupportedCaIdentifiersAsync(
        bool includeKloc,
        bool includeKlcc,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Stores a certificate bundle for the specified key reference.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the certificate bundle.</param>
    /// <param name="certificates">Certificates to persist, with the leaf certificate last.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task StoreCertificatesAsync(
        KeyReference keyReference,
        IReadOnlyList<X509Certificate2> certificates,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Imports an SCP03 static key set into the Security Domain.
    /// </summary>
    /// <param name="keyReference">Key reference that will receive the key set.</param>
    /// <param name="staticKeys">Static ENC/MAC/DEK values to load.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when staticKeys is null.</exception>
    /// <exception cref="ArgumentException">Thrown when keyReference.Kid is not 0x01 (required for SCP03).</exception>
    /// <exception cref="InvalidOperationException">Thrown when no SCP session is active or KCV validation fails.</exception>
    public async Task PutKeyAsync(
        KeyReference keyReference,
        StaticKeys staticKeys,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(staticKeys);

        // Validation: SCP03 keys must use KID 0x01
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
            EncodeKeyComponent(staticKeys.Enc, encryptor, buffer);
            EncodeKeyComponent(staticKeys.Mac, encryptor, buffer);
            EncodeKeyComponent(staticKeys.Dek, encryptor, buffer);

            // Build and send PUT KEY command
            // P2: 0x80 | KID (0x80 flag required by GlobalPlatform)
            var command = new ApduCommand(ClaGlobalPlatform, InsPutKey, (byte)replaceKvn,
                (byte)(0x80 | keyReference.Kid),
                buffer.WrittenMemory);
            var response = await TransmitAsync(command, cancellationToken).ConfigureAwait(false);

            // Validate response contains expected KCVs
            ValidateKcv(staticKeys, response.Span);

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

    /// <summary>
    ///     Deletes keys matching the supplied reference.
    /// </summary>
    /// <param name="keyReference">Key reference describing the key(s) to delete.</param>
    /// <param name="deleteLast">Whether the last remaining key may be deleted.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task DeleteKeyAsync(
        KeyReference keyReference,
        bool deleteLast = false,
        CancellationToken cancellationToken = default)
    {
        var (kid, kvn) = NormalizeDeletePolicy(keyReference);
        var payload = EncodeDeleteFilter(kid, kvn);
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

        return TransmitAsync(command, cancellationToken);
    }

    private static (byte Kid, byte Kvn) NormalizeDeletePolicy(KeyReference keyReference)
    {
        var kid = keyReference.Kid;
        var kvn = keyReference.Kvn;

        if (kid == 0 && kvn == 0)
            throw new ArgumentException("At least one of KID or KVN must be non-zero", nameof(keyReference));

        // SCP03 keys (KIDs 0x01, 0x02, 0x03) may only be deleted by KVN.
        // If KVN is provided, zero KID (wildcard).
        if (kid is 0x01 or 0x02 or 0x03) kid = 0; // wildcard KID when KVN specified for SCP03

        return (kid, kvn);
    }

    private static ReadOnlyMemory<byte> EncodeDeleteFilter(byte kid, byte kvn)
    {
        if (kid == 0 && kvn == 0)
            return ReadOnlyMemory<byte>.Empty;

        var dict = new Dictionary<int, byte[]?>(2);
        if (kid != 0)
            dict[TagKid] = [kid];
        if (kvn != 0)
            dict[TagKvn] = [kvn];

        return TlvHelper.EncodeDictionary(dict);
    }

    /// <summary>
    ///     Generates a new SCP11 key pair on the device and returns the public point.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the generated key pair.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The uncompressed EC public point (0x04 || X || Y).</returns>
    public async Task<ECPublicKey> GenerateKeyAsync(
        KeyReference keyReference,
        byte replaceKvn = 0,
        CancellationToken cancellationToken = default)
    {
        if (replaceKvn == 0)
            _logger.LogDebug("Generating EC key for {KeyRef}", keyReference);
        else
            _logger.LogDebug("Generating EC key for {KeyRef}, replacing KVN=0x{ReplaceKvn:X2}", keyReference,
                replaceKvn);

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
            P1 = replaceKvn,
            P2 = keyReference.Kid,
            Data = commandData
        };

        ReadOnlyMemory<byte> response;

        try
        {
            response = await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
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
        return ECPublicKey.CreateFromValue(publicPoint, KeyType.ECP256);
    }

    /// <summary>
    ///     Puts an EC public key onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="publicKey">The EC public key parameters to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key (Default value is 0).</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException">Thrown when the public key is not of type SECP256R1.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the new key set's checksum failed to verify, or some other SCP related error
    ///     described in the exception message.
    /// </exception>
    public async Task PutKeyAsync(KeyReference keyReference, ECPublicKey publicKey, int replaceKvn = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing SCP11 public key into KeyReference: {KeyReference}", keyReference);

        var pkParams = publicKey.Parameters;
        if (pkParams.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            throw new ArgumentException("Public key must be of type NIST P-256");

        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            buffer.Write([keyReference.Kvn]);

            // Write the EC public key
            using var publicKeyTlv = new Tlv(KeyTypeEccPublicKey, publicKey.PublicPoint.Span);
            buffer.Write(publicKeyTlv.AsMemory().Span);

            // Write the EC parameters
            using var paramsTlv = new Tlv(KeyTypeEccKeyParams, stackalloc byte[1]);
            buffer.Write(paramsTlv.AsMemory().Span);
            buffer.Write(new byte[] { 0 });

            // Create and send the command
            var command = new ApduCommand(ClaGlobalPlatform, InsPutKey, (byte)replaceKvn, keyReference.Kid,
                buffer.WrittenMemory);
            var response = await TransmitAsync(command, cancellationToken).ConfigureAwait(false);

            // Get and validate the response
            Span<byte> expectedResponseData = new[] { keyReference.Kvn };
            ValidateCheckSum(expectedResponseData, response.Span);

            _logger.LogInformation("Successfully put public key for KeyReference: {KeyReference}", keyReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put public key for KeyReference: {KeyReference}", keyReference);
            throw;
        }
        finally
        {
            buffer.Clear();
        }
    }

    /// <summary>
    ///     Puts an EC private key onto the YubiKey using the Security Domain.
    /// </summary>
    /// <param name="keyReference">The key reference identifying where to store the key.</param>
    /// <param name="privateKey">The EC private key parameters to store.</param>
    /// <param name="replaceKvn">The key version number to replace, or 0 for a new key (Default value is 0).</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException">Thrown when the private key is not of type NIST P-256.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no secure session is established.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the new key set's checksum failed to verify, or some other SCP related error
    ///     described in the exception message.
    /// </exception>
    public async Task PutKeyAsync(
        KeyReference keyReference,
        ECPrivateKey privateKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing SCP11 private key into Key Reference: {KeyReference}", keyReference);

        var parameters = privateKey.Parameters;
        if (parameters.Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            throw new ArgumentException("Private key must be of type NIST P-256");

        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            buffer.Write([keyReference.Kvn]);

            var encryptedKey = EncryptData(parameters.D.AsSpan());
            using var encryptedKeyTlv = new Tlv(KeyTypeEccPrivateKey, encryptedKey);
            buffer.Write(encryptedKeyTlv.AsMemory().Span);

            using var paramsTlv = new Tlv(KeyTypeEccKeyParams, [0x00]);
            buffer.Write(paramsTlv.AsMemory().Span);
            buffer.Write(new byte[] { 0 });

            var command = new ApduCommand(ClaGlobalPlatform, InsPutKey, (byte)replaceKvn, keyReference.Kid,
                buffer.WrittenMemory);
            var response = await TransmitAsync(command, cancellationToken);

            Span<byte> expectedResponseData = new[] { keyReference.Kvn };
            ValidateCheckSum(response.Span, expectedResponseData);

            _logger.LogInformation("Successfully put private key for Key Reference: {KeyReference}", keyReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to put private key for Key Reference: {KeyReference}", keyReference);
            throw;
        }
        finally
        {
            buffer.Clear();
        }
    }

    /// <summary>
    ///     Stores an allowlist of certificate serial numbers for a specified key reference using the GlobalPlatform STORE DATA
    ///     command.
    /// </summary>
    /// <remarks>
    ///     This method requires off-card entity verification. If an allowlist is not stored, any
    ///     certificate signed by the CA can be used.
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    /// </remarks>
    /// <param name="keyReference">A reference to the key for which the allowlist will be stored.</param>
    /// <param name="serials">
    ///     The list of certificate serial numbers (in hexadecimal string format) to be stored in the
    ///     allowlist for the given <see cref="KeyReference" />.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException">Thrown when a serial number cannot be encoded properly.</exception>
    public async Task StoreAllowlistAsync(KeyReference keyReference, IReadOnlyCollection<string> serials,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Storing allow list (KeyReference: {KeyReference})", keyReference);

        var buffer = new ArrayBufferWriter<byte>();
        foreach (var serial in serials)
        {
            var serialTlvBytes = new Tlv(TagSerial, Convert.FromHexString(serial)).AsMemory();
            buffer.Write(serialTlvBytes.Span);
        }

        var serialsDataTl = TlvHelper.EncodeMany(
            new Tlv(TagControlReference, new Tlv(TagKidKvn, keyReference.AsBytes.Span).AsMemory().Span),
            new Tlv(TagSerialsAllowList, buffer.WrittenSpan)
        );

        await StoreDataAsync(serialsDataTl, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Allow list stored (KeyReference: {KeyReference})", keyReference);
    }

    /// <summary>
    ///     Clears the allow list for the given <see cref="KeyReference" />
    /// </summary>
    /// <seealso cref="StoreAllowlistAsync" />
    /// <param name="keyReference">The key reference that holds the allow list</param>
    public Task ClearAllowListAsync(KeyReference keyReference) => StoreAllowlistAsync(keyReference, []);

    /// <summary>
    ///     Stores data in the Security Domain or targeted Application on the YubiKey using the GlobalPlatform STORE DATA
    ///     command.
    /// </summary>
    /// <remarks>
    ///     The STORE DATA command is used to transfer data to either the Security Domain itself or to an Application
    ///     being personalized. The data must be formatted as BER-TLV structures according to ISO 8825.
    ///     <para>
    ///         This implementation:
    ///         - Uses a single block transfer (P1.b8=1 indicating last block)
    ///         - Requires BER-TLV formatted data (P1.b5-b4=10)
    ///         - Does not provide encryption information (P1.b7-b6=00)
    ///     </para>
    ///     <para>See GlobalPlatform Technology Card Specification v2.3.1 §11 APDU Command Reference for more information.</para>
    ///     The <see cref="SecurityDomainSession" /> makes use of this method to store data in the Security Domain. Such as the
    ///     <see cref="StoreCaIssuerAsync" />, <see cref="StoreCertificatesAsync" />, <see cref="StoreAllowlistAsync" />, and
    ///     other data.
    /// </remarks>
    /// <param name="data">
    ///     The data to be stored, which must be formatted as BER-TLV structures according to ISO 8825.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when no secure connection is available or the security context is invalid.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when there was an SCP error, described in the exception message.</exception>
    public async Task StoreDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Storing data with length:{Length}", data.Length);

        var command = new ApduCommand(0x00, 0xE2, 0x90, 0x00, data);
        await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Store the SKI (Subject Key Identifier) for the CA of a given key.
    ///     Requires off-card entity verification.
    /// </summary>
    /// <param name="keyReference">A reference to the key for which to store the CA issuer.</param>
    /// <param name="ski">The Subject Key Identifier to store.</param>
    /// <param name="cancellationToken"></param>
    public async Task StoreCaIssuerAsync(KeyReference keyReference, ReadOnlyMemory<byte> ski,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Storing CA issuer SKI (KeyReference: {KeyReference})", keyReference);

        byte klcc = keyReference.Kid switch //
        {
            ScpKid.SCP11a or ScpKid.SCP11b or ScpKid.SCP11c => 1,
            _ => 0
        };

        // Create and serialize data
        var caIssuerData = new Tlv(
            TagControlReference, TlvHelper.EncodeMany(
                new Tlv(ClaGlobalPlatform, [klcc]),
                new Tlv(0x42, ski.Span), // GlobalPlatform tag for CA issuer SKI
                new Tlv(TagKidKvn, keyReference.AsBytes.Span)).Span);

        // Send store data command
        await StoreDataAsync(caIssuerData.AsMemory(), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("CA issuer SKI stored (KeyReference: {KeyReference})", keyReference);
    }

    /// <summary>
    ///     Performs a factory reset by blocking all registered key references and reinitializing the session.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var keyInformation = await GetKeyInformationAsync(cancellationToken).ConfigureAwait(false);
        if (keyInformation.Count == 0)
        {
            _logger.LogInformation("Security Domain reset skipped: no keys reported");
            return;
        }

        foreach (var keyReference in keyInformation.Keys)
        {
            if (!TryGetResetParameters(keyReference, out var instruction, out var overrideKeyRef))
            {
                _logger.LogTrace("Reset skipping unsupported key reference {KeyRef}", keyReference);
                continue;
            }

            await BlockKeyAsync(instruction, overrideKeyRef, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Security Domain reset complete; reinitializing session");

        _protocol = null;
        _isInitialized = false;

        await InitializeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<X509Certificate2> ParseCertificateBundle(ReadOnlySpan<byte> data)
    {
        var certificates = new List<X509Certificate2>();
        using var tlvList = TlvHelper.DecodeList(data);

        foreach (var tlv in tlvList)
            if (tlv.Tag == 0x30) // X.509 certificate DER tag
                certificates.Add(X509CertificateLoader.LoadCertificate(tlv.AsSpan()));

        return certificates;
    }

    private static void ParseCaIdentifiers(ReadOnlySpan<byte> data,
        Dictionary<KeyReference, ReadOnlyMemory<byte>> results)
    {
        if (data.IsEmpty) return;

        // GlobalPlatform CA identifiers are typically returned as pairs of TLVs: [Identifier, KeyReference]
        // or as single TLVs with nested KeyReference. We handle both for robustness.
        using var tlvList = TlvHelper.DecodeList(data);
        var tlvArray = tlvList.ToArray();

        for (var i = 0; i < tlvArray.Length; i++)
        {
            var tlv = tlvArray[i];

            // Case 1: Adjacent KeyReference (as in legacy C# SDK)
            // The CA identifiers are returned as a list of TLVs where the identifier tag
            // is followed by a TagKidKvn (0x83) tag identifying which key it belongs to.
            if (i + 1 < tlvArray.Length && tlvArray[i + 1].Tag == TagKidKvn)
            {
                var keyRefData = tlvArray[i + 1].Value.Span;
                if (keyRefData.Length >= 2)
                {
                    var keyRef = new KeyReference(keyRefData[0], keyRefData[1]);
                    // We store the first TLV's value as the identifier
                    results[keyRef] = tlv.Value.ToArray();
                    i++; // Skip the next TLV as we processed it
                    continue;
                }
            }

            // Case 2: Nested KeyReference (as in Java SDK)
            if (TlvHelper.TryFindValue(TagKidKvn, tlv.Value.Span, out var kidKvn) && kidKvn.Length >= 2)
            {
                var keyRef = new KeyReference(kidKvn.Span[0], kidKvn.Span[1]);
                results[keyRef] = tlv.Value.ToArray();
            }
        }
    }

    private static ReadOnlyMemory<byte> EncodeControlReference(KeyReference keyRef)
    {
        var keyRefEncoded = EncodeKeyReference(keyRef);
        using var crt = new Tlv(TagControlReference, keyRefEncoded.Span);
        return crt.AsMemory().ToArray();
    }

    private static ReadOnlyMemory<byte> EncodeKeyReference(KeyReference keyRef)
    {
        var dict = new Dictionary<int, byte[]?> { [TagKidKvn] = [keyRef.Kid, keyRef.Kvn] };

        return TlvHelper.EncodeDictionary(dict);
    }

    private ISmartCardProtocol EnsureBaseProtocol()
    {
        if (_baseProtocol is not null) // Already initialized
            return _baseProtocol;

        var smartCardProtocol = PcscProtocolFactory<ISmartCardConnection> // TODO static dependency. Good bad?
            .Create(_loggerFactory)
            .Create(_connection);

        _baseProtocol = smartCardProtocol;
        return _baseProtocol;
    }

    private void EnsureInitializedProtocol()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");

        if (_protocol is null) throw new InvalidOperationException("Security Domain protocol not available.");
    }

    private Task<ReadOnlyMemory<byte>> TransmitAsync(ApduCommand command, CancellationToken cancellationToken)
    {
        EnsureInitializedProtocol();
        return _protocol!.TransmitAndReceiveAsync(command, cancellationToken);
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


    private async Task BlockKeyAsync(byte instruction, KeyReference keyReference, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ResetAttemptLimit; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            short statusWord;
            try
            {
                statusWord = await TransmitResetAttemptAsync(instruction, keyReference, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reset attempt {Attempt} for key {KeyRef} failed to transmit", attempt,
                    keyReference);
                continue;
            }

            if (statusWord is SWConstants.AuthenticationMethodBlocked or SWConstants.SecurityStatusNotSatisfied)
            {
                _logger.LogDebug(
                    "Key {KeyRef} blocked after {AttemptCount} attempts (SW=0x{Status:X4})",
                    keyReference,
                    attempt,
                    statusWord);
                break;
            }

            if (statusWord is SWConstants.InvalidCommandDataParameter or SWConstants.Success)
                continue;

            _logger.LogTrace(
                "Reset attempt {Attempt} for key {KeyRef} returned SW=0x{Status:X4}",
                attempt,
                keyReference,
                statusWord);
        }
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

            var response = await _connection
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

    private static void ValidateCheckSum(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            throw new InvalidOperationException("ExceptionMessages.ChecksumError");
    }

    /// <summary>
    ///     Encodes a single key component (ENC, MAC, or DEK) for PUT KEY command.
    /// </summary>
    /// <param name="key">The 16-byte key to encode.</param>
    /// <param name="encryptor">The data encryptor from the SCP session.</param>
    /// <param name="buffer">The buffer to write the encoded TLV to.</param>
    private static void EncodeKeyComponent(
        ReadOnlySpan<byte> key,
        DataEncryptor encryptor,
        ArrayBufferWriter<byte> buffer)
    {
        // Calculate KCV (Key Check Value)
        Span<byte> kcv = stackalloc byte[3];
        CalculateKcv(key, kcv);

        // Encrypt the key - rent buffer to avoid allocation on key parameter
        byte[]? keyBuffer = null;
        try
        {
            keyBuffer = ArrayPool<byte>.Shared.Rent(key.Length);
            key.CopyTo(keyBuffer);
            var encrypted = encryptor(keyBuffer.AsSpan(0, key.Length));

            // Write TLV with just the encrypted key data
            using var keyTlv = new Tlv(KeyTypeAes, encrypted);
            buffer.Write(keyTlv.AsMemory().Span);

            // Write KCV length and KCV bytes after the TLV
            buffer.Write([(byte)kcv.Length]);
            buffer.Write(kcv);
        }
        finally
        {
            if (keyBuffer is not null)
            {
                CryptographicOperations.ZeroMemory(keyBuffer.AsSpan(0, key.Length));
                ArrayPool<byte>.Shared.Return(keyBuffer);
            }
        }
    }

    /// <summary>
    ///     Calculates the Key Check Value (KCV) for an AES key.
    /// </summary>
    /// <param name="key">The 16-byte AES key.</param>
    /// <param name="output">The 3-byte output span for the KCV.</param>
    /// <remarks>
    ///     KCV is calculated as the first 3 bytes of AES-CBC encryption of 16 bytes (all 0x01) using a zero IV.
    ///     This matches the implementation in the Java SDK.
    /// </remarks>
    private static void CalculateKcv(ReadOnlySpan<byte> key, Span<byte> output)
    {
        using var aes = Aes.Create();

        aes.SetKey(key);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        // KCV = first 3 bytes of AES-CBC encrypt of 16 bytes (all 0x01) using zero IV
        Span<byte> kcvInput = stackalloc byte[16];
        kcvInput.Fill(0x01);

        Span<byte> iv = stackalloc byte[16];
        iv.Clear();

        Span<byte> encrypted = stackalloc byte[16];
        aes.EncryptCbc(kcvInput, iv, encrypted, PaddingMode.None);

        encrypted[..3].CopyTo(output);
    }

    /// <summary>
    ///     Validates that the Key Check Values in the response match the expected values.
    /// </summary>
    /// <param name="staticKeys">The static keys that were imported.</param>
    /// <param name="response">The response data from the PUT KEY command.</param>
    /// <exception cref="InvalidOperationException">Thrown when KCV validation fails.</exception>
    private static void ValidateKcv(StaticKeys staticKeys, ReadOnlySpan<byte> response)
    {
        // Response format: KVN || kcv_enc || kcv_mac || kcv_dek
        if (response.Length < 10) // 1 byte KVN + 3*3 bytes kcv
            throw new InvalidOperationException("Response too short for KCV validation");

        // Calculate expected KCVs
        Span<byte> expectedKcvEnc = stackalloc byte[3];
        Span<byte> expectedKcvMac = stackalloc byte[3];
        Span<byte> expectedKcvDek = stackalloc byte[3];

        CalculateKcv(staticKeys.Enc, expectedKcvEnc);
        CalculateKcv(staticKeys.Mac, expectedKcvMac);
        CalculateKcv(staticKeys.Dek, expectedKcvDek);

        // Extract actual KCVs from response (skip first byte = KVN)
        var actualKcvEnc = response.Slice(1, 3);
        var actualKcvMac = response.Slice(4, 3);
        var actualKcvDek = response.Slice(7, 3);

        // Validate each KCV using constant-time comparison
        if (!CryptographicOperations.FixedTimeEquals(expectedKcvEnc, actualKcvEnc))
            throw new InvalidOperationException("ENC key check value mismatch");

        if (!CryptographicOperations.FixedTimeEquals(expectedKcvMac, actualKcvMac))
            throw new InvalidOperationException("MAC key check value mismatch");

        if (!CryptographicOperations.FixedTimeEquals(expectedKcvDek, actualKcvDek))
            throw new InvalidOperationException("DEK key check value mismatch");
    }

    /// <summary>
    ///     Disposes the session and releases managed resources associated with the underlying protocol.
    /// </summary>
    /// <param name="disposing">Indicates whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        _protocol?.Dispose();
        _protocol = null;
        _baseProtocol?.Dispose();
        _baseProtocol = null;
        _isInitialized = false;

        base.Dispose(disposing);
    }
}
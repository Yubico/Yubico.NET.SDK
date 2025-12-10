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

using System.Buffers;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yubico.YubiKit.Core;
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
public sealed class SecurityDomainSession(
    ISmartCardConnection connection,
    IProtocolFactory<ISmartCardConnection> protocolFactory,
    ILogger<SecurityDomainSession> logger,
    ScpKeyParams? scpKeyParams = null)
    : ApplicationSession
{
    private ISmartCardProtocol? _baseProtocol;
    private ISmartCardProtocol? _protocol;
    private bool _isInitialized;

    private const byte ClaGlobalPlatform = 0x80;
    private const byte InsGetData = 0xCA;
    private const byte InsInitializeUpdate = 0x50;
    private const byte InsPerformSecurityOperation = 0x2A;
    private const byte InsInternalAuthenticate = 0x88;
    private const byte InsExternalAuthenticate = 0x82;
    private const byte InsDelete = 0xE4;
    private const byte InsGenerateKey = 0xF1;

    private const int TagKeyInformationTemplate = 0xE0;
    private const int TagKeyInformationData = 0xC0;

    // TLV tags for DeleteKey filter parameters
    private const int TagKid = 0xD0;          // Key ID (KID)
    private const int TagKvn = 0xD2;          // Key Version Number (KVN)
    private const byte DeleteLastFlag = 0x01; // P2 flag for "delete last"

    private const int KeyTypeEccPublicKey = 0xB0;
    private const int KeyTypeEccKeyParams = 0xF0;

    private const int ResetAttemptLimit = 65;
    private static readonly byte[] ResetAttemptPayload = new byte[8];

    /// <summary>
    ///     Factory helper that creates and initializes a Security Domain session.
    /// </summary>
    public static async Task<SecurityDomainSession> CreateAsync(
        ISmartCardConnection connection,
        ILogger<SecurityDomainSession>? logger = null,
        ScpKeyParams? scpKeyParams = null,
        CancellationToken cancellationToken = default)
    {
        logger ??= NullLogger<SecurityDomainSession>.Instance;
        var protocolFactory = PcscProtocolFactory<ISmartCardConnection>.Create();
        var session = new SecurityDomainSession(connection, protocolFactory, logger, scpKeyParams);

        await session.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return session;
    }

    /// <summary>
    ///     Ensures the Security Domain application is selected and secure messaging is configured if requested.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        var baseProtocol = EnsureBaseProtocol();

        await baseProtocol
            .SelectAsync(ApplicationIds.SecurityDomain, cancellationToken)
            .ConfigureAwait(false);

        // Security Domain is available on firmware 5.3.0 and newer.
        baseProtocol.Configure(FirmwareVersion.V5_3_0);

        _protocol = scpKeyParams is not null
            ? await baseProtocol
                .WithScpAsync(scpKeyParams, cancellationToken)
                .ConfigureAwait(false)
            : baseProtocol;

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
            throw new ArgumentOutOfRangeException(nameof(expectedResponseLength), "Expected response length cannot be negative.");

        var command = new CommandApdu
        {
            Cla = 0,
            Ins = InsGetData,
            P1 = (byte)(dataObject >> 8),
            P2 = (byte)(dataObject & 0xFF),
            Data = requestData,
            Le = expectedResponseLength
        };

        logger.LogDebug("Sending GET DATA for object 0x{DataObject:X4}", dataObject);

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
            logger.LogWarning(ex, "GET DATA for object 0x{DataObject:X4} failed", dataObject);
            throw;
        }
    }

    /// <summary>
    ///     Retrieves key metadata exposed by the Security Domain via the key information data object.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task<IReadOnlyDictionary<KeyRef, IReadOnlyDictionary<byte, byte>>> GetKeyInformationAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(TagKeyInformationTemplate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.IsEmpty)
            return new ReadOnlyDictionary<KeyRef, IReadOnlyDictionary<byte, byte>>(new Dictionary<KeyRef, IReadOnlyDictionary<byte, byte>>(0));

        var keyInformation = new Dictionary<KeyRef, IReadOnlyDictionary<byte, byte>>();

        using var tlvList = TlvHelper.Decode(response.Span);
        foreach (var tlv in tlvList)
        {
            var value = TlvHelper.GetValue(TagKeyInformationData, tlv.GetBytes().Span);
            var keyRef = new KeyRef(value.Span[0], value.Span[1]);
            var components = new Dictionary<byte, byte>();

            var currentValue = value.Span[2..];
            while (!currentValue.IsEmpty)
            {
                components.Add(currentValue[0], currentValue[1]);
                currentValue = currentValue[2..];
            }

            keyInformation.Add(keyRef, new ReadOnlyDictionary<byte, byte>(components));
        }

        return new ReadOnlyDictionary<KeyRef, IReadOnlyDictionary<byte, byte>>(keyInformation);
    }

    /// <summary>
    ///     Establishes secure messaging with the provided key parameters, replacing any existing SCP session.
    /// </summary>
    /// <param name="scpKeyParams">SCP key material to use when authenticating.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The data encryptor derived from the newly established secure channel, if available.</returns>
    public Task<DataEncryptor?> AuthenticateAsync(
        ScpKeyParams scpKeyParams,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Retrieves the Security Domain card recognition data (tag 0x73).
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The raw card recognition TLV payload.</returns>
    public Task<ReadOnlyMemory<byte>> GetCardRecognitionDataAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Retrieves the certificate bundle associated with the supplied key reference.
    /// </summary>
    /// <param name="keyReference">Key reference identifying the certificate store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A list of certificates where the leaf certificate appears last.</returns>
    public Task<IReadOnlyList<X509Certificate2>> GetCertificatesAsync(
        KeyRef keyReference,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Retrieves the supported CA identifiers (KLOC/KLCC) exposed by the Security Domain.
    /// </summary>
    /// <param name="includeKloc">Whether to include Key Loading OCE Certificate identifiers.</param>
    /// <param name="includeKlcc">Whether to include Key Loading Card Certificate identifiers.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A dictionary keyed by key reference containing identifier payloads.</returns>
    public Task<IReadOnlyDictionary<KeyRef, ReadOnlyMemory<byte>>> GetSupportedCaIdentifiersAsync(
        bool includeKloc,
        bool includeKlcc,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Stores the CA issuer subject key identifier for the given key reference.
    /// </summary>
    /// <param name="keyReference">Key reference identifying the CA issuer record.</param>
    /// <param name="subjectKeyIdentifier">Subject key identifier bytes to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task StoreCaIssuerAsync(
        KeyRef keyReference,
        ReadOnlyMemory<byte> subjectKeyIdentifier,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Stores a certificate bundle for the specified key reference.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the certificate bundle.</param>
    /// <param name="certificates">Certificates to persist, with the leaf certificate last.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task StoreCertificatesAsync(
        KeyRef keyReference,
        IReadOnlyList<X509Certificate2> certificates,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Persists the allowlist of certificate serial numbers associated with the provided key reference.
    /// </summary>
    /// <param name="keyReference">Key reference whose allowlist is being updated.</param>
    /// <param name="serials">Collection of serial numbers (hex strings) to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task StoreAllowlistAsync(
        KeyRef keyReference,
        IReadOnlyCollection<string> serials,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Removes all entries from the allowlist tied to the specified key reference.
    /// </summary>
    /// <param name="keyReference">Key reference whose allowlist should be cleared.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task ClearAllowListAsync(
        KeyRef keyReference,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Executes a STORE DATA command with the supplied TLV encoded payload.
    /// </summary>
    /// <param name="data">Pre-encoded BER-TLV data to transmit.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task StoreDataAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Imports an SCP03 static key set into the Security Domain.
    /// </summary>
    /// <param name="keyReference">Key reference that will receive the key set.</param>
    /// <param name="staticKeys">Static ENC/MAC/DEK values to load.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task PutKeyAsync(
        KeyRef keyReference,
        StaticKeys staticKeys,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Imports an SCP11 private key into the Security Domain.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the private key.</param>
    /// <param name="privateKey">Private key material to load.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task PutKeyAsync(
        KeyRef keyReference,
        ECDiffieHellman privateKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Imports an SCP11 public key into the Security Domain.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the public key.</param>
    /// <param name="publicKey">Public key material to load.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task PutKeyAsync(
        KeyRef keyReference,
        ECDiffieHellmanPublicKey publicKey,
        int replaceKvn = 0,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Deletes keys matching the supplied reference.
    /// </summary>
    /// <param name="keyReference">Key reference describing the key(s) to delete.</param>
    /// <param name="deleteLast">Whether the last remaining key may be deleted.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task DeleteKeyAsync(
        KeyRef keyReference,
        bool deleteLast = false,
        CancellationToken cancellationToken = default)
    {
        EnsureInitializedProtocol();

        var (kid, kvn) = NormalizeDeletePolicy(keyReference);

        logger.LogDebug("Deleting keys matching {KeyRef}{Suffix}",
            keyReference, deleteLast ? " (allow delete last)" : string.Empty);

        var payload = EncodeDeleteFilter(kid, kvn);

        var command = new CommandApdu
        {
            Cla = ClaGlobalPlatform,
            Ins = InsDelete,
            P1 = 0x00,
            P2 = deleteLast ? DeleteLastFlag : (byte)0x00,
            Data = payload
        };

        return TransmitDeleteAsync(command, cancellationToken);
    }

    private static (byte Kid, byte Kvn) NormalizeDeletePolicy(KeyRef keyRef)
    {
        var kid = keyRef.Kid;
        var kvn = keyRef.Kvn;

        if (kid == 0 && kvn == 0)
            throw new ArgumentException("At least one of KID or KVN must be non-zero", nameof(keyRef));

        // SCP03 keys (KIDs 0x01, 0x02, 0x03) may only be deleted by KVN.
        // If KVN is provided, zero KID (wildcard).
        if (kid is 0x01 or 0x02 or 0x03)
        {
            kid = 0; // wildcard KID when KVN specified for SCP03
        }

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

    private async Task TransmitDeleteAsync(CommandApdu command, CancellationToken cancellationToken)
    {
        try
        {
            _ = await TransmitAsync(command, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Keys deleted");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Delete keys command failed");
            throw;
        }
    }

    /// <summary>
    ///     Generates a new SCP11 key pair on the device and returns the public point.
    /// </summary>
    /// <param name="keyReference">Key reference that will own the generated key pair.</param>
    /// <param name="replaceKvn">Optional key version number to replace.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The uncompressed EC public point (0x04 || X || Y).</returns>
    public Task<byte[]> GenerateEcKeyAsync(
        KeyRef keyReference,
        byte replaceKvn = 0,
        CancellationToken cancellationToken = default) =>
        GenerateEcKeyInternalAsync(keyReference, replaceKvn, cancellationToken);

    private async Task<byte[]> GenerateEcKeyInternalAsync(
        KeyRef keyReference,
        byte replaceKvn,
        CancellationToken cancellationToken)
    {
        if (replaceKvn == 0)
            logger.LogDebug("Generating EC key for {KeyRef}", keyReference);
        else
            logger.LogDebug("Generating EC key for {KeyRef}, replacing KVN=0x{ReplaceKvn:X2}", keyReference, replaceKvn);

        Span<byte> parameters = stackalloc byte[3];
        parameters[0] = (byte)KeyTypeEccKeyParams;
        parameters[1] = 0x01;
        parameters[2] = 0x00; // SECP256R1 curve indicator

        var commandData = new byte[1 + parameters.Length];
        commandData[0] = keyReference.Kvn;
        parameters.CopyTo(commandData.AsSpan(1));

        var command = new CommandApdu
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
            logger.LogWarning(ex, "Generate EC key for {KeyRef} failed", keyReference);
            throw;
        }

        if (response.IsEmpty)
            throw new BadResponseException("Generate EC key response was empty.");

        using var tlvs = TlvHelper.Decode(response.Span);
        foreach (var tlv in tlvs.AsSpan())
        {
            if (tlv.Tag == KeyTypeEccPublicKey)
                return tlv.Value.ToArray();
        }

        throw new BadResponseException(
            $"Generate EC key response missing public key tag 0x{KeyTypeEccPublicKey:X2}.");
    }

    /// <summary>
    ///     Performs a factory reset by blocking all registered key references and reinitializing the session.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var keyInformation = await GetKeyInformationAsync(cancellationToken).ConfigureAwait(false);
        if (keyInformation.Count == 0)
        {
            logger.LogInformation("Security Domain reset skipped: no keys reported");
            return;
        }

        foreach (var keyReference in keyInformation.Keys)
        {
            if (!TryGetResetParameters(keyReference, out var instruction, out var overrideKeyRef))
            {
                logger.LogTrace("Reset skipping unsupported key reference {KeyRef}", keyReference);
                continue;
            }

            await BlockKeyAsync(instruction, overrideKeyRef, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Security Domain reset complete; reinitializing session");

        _protocol = null;
        _isInitialized = false;

        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private ISmartCardProtocol EnsureBaseProtocol()
    {
        if (_baseProtocol is not null)
            return _baseProtocol;

        var protocol = protocolFactory.Create(connection);
        if (protocol is not ISmartCardProtocol smartCardProtocol)
        {
            protocol.Dispose();
            throw new NotSupportedException("Security Domain requires a smart card protocol implementation.");
        }

        _baseProtocol = smartCardProtocol;
        return _baseProtocol;
    }

    private void EnsureInitializedProtocol()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");

        if (_protocol is null)
            throw new InvalidOperationException("Security Domain protocol not available.");
    }

    /// <summary>
    ///     Centralized APDU transmit helper that ensures the protocol is initialized.
    /// </summary>
    private Task<ReadOnlyMemory<byte>> TransmitAsync(CommandApdu command, CancellationToken cancellationToken)
    {
        EnsureInitializedProtocol();
        return _protocol!.TransmitAndReceiveAsync(command, cancellationToken);
    }

    private bool TryGetResetParameters(KeyRef keyReference, out byte instruction, out KeyRef overrideKeyRef)
    {
        overrideKeyRef = keyReference;
        instruction = 0;

        switch (keyReference.Kid)
        {
            case ScpKid.SCP03:
                overrideKeyRef = new KeyRef(0x00, 0x00);
                instruction = InsInitializeUpdate;
                return true;
            case 0x02:
            case 0x03:
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

    private async Task BlockKeyAsync(byte instruction, KeyRef keyReference, CancellationToken cancellationToken)
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
                logger.LogWarning(ex, "Reset attempt {Attempt} for key {KeyRef} failed to transmit", attempt,
                    keyReference);
                continue;
            }

            if (statusWord is SWConstants.AuthenticationMethodBlocked or SWConstants.SecurityStatusNotSatisfied)
            {
                logger.LogDebug(
                    "Key {KeyRef} blocked after {AttemptCount} attempts (SW=0x{Status:X4})",
                    keyReference,
                    attempt,
                    statusWord);
                break;
            }

            if (statusWord is SWConstants.InvalidCommandDataParameter or SWConstants.Success)
                continue;

            logger.LogTrace(
                "Reset attempt {Attempt} for key {KeyRef} returned SW=0x{Status:X4}",
                attempt,
                keyReference,
                statusWord);
        }
    }

    private async Task<short> TransmitResetAttemptAsync(
        byte instruction,
        KeyRef keyReference,
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

            var response = await connection
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
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }

    /// <summary>
    ///     Disposes the session and releases managed resources associated with the underlying protocol.
    /// </summary>
    /// <param name="disposing">Indicates whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _protocol?.Dispose();
        _protocol = null;
        _baseProtocol = null;
        base.Dispose(true);
    }
}
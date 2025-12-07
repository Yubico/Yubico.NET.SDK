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

using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly ISmartCardConnection _connection = connection;
    private readonly IProtocolFactory<ISmartCardConnection> _protocolFactory = protocolFactory;
    private readonly ILogger<SecurityDomainSession> _logger = logger;
    private readonly ScpKeyParams? _scpKeyParams = scpKeyParams;

    private ISmartCardProtocol? _protocol;
    private bool _isInitialized;

    private const byte ClaGlobalPlatform = 0x80;
    private const byte InsGetData = 0xCA;
    private const ushort DataObjectKeyInformation = 0x00E0;
    private const int TagKeyInformationTemplate = 0xE0;
    private const int TagKeyInformationData = 0xC0;

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

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        var protocol = _protocolFactory.Create(_connection);
        if (protocol is not ISmartCardProtocol smartCardProtocol)
        {
            protocol.Dispose();
            throw new NotSupportedException("Security Domain requires a smart card protocol implementation.");
        }

        try
        {
            await smartCardProtocol
                .SelectAsync(ApplicationIds.SecurityDomain, cancellationToken)
                .ConfigureAwait(false);

            // Security Domain is available on firmware 5.3.0 and newer.
            smartCardProtocol.Configure(new FirmwareVersion(5, 3, 0));

            _protocol = _scpKeyParams is not null
                ? await smartCardProtocol
                    .WithScpAsync(_scpKeyParams, cancellationToken)
                    .ConfigureAwait(false)
                : smartCardProtocol;

            _isInitialized = true;
        }
        catch
        {
            protocol.Dispose();
            throw;
        }
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
        if (!_isInitialized)
            throw new InvalidOperationException("Session not initialized. Call InitializeAsync first.");

        if (_protocol is null)
            throw new InvalidOperationException("Security Domain protocol not available.");

        if (expectedResponseLength < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedResponseLength), "Expected response length cannot be negative.");

        var command = new CommandApdu
        {
            Cla = ClaGlobalPlatform,
            Ins = InsGetData,
            P1 = (byte)(dataObject >> 8),
            P2 = (byte)(dataObject & 0xFF),
            Data = requestData,
            Le = expectedResponseLength
        };

        _logger.LogDebug("Sending GET DATA for object 0x{DataObject:X4}", dataObject);

        try
        {
            return await _protocol
                .TransmitAndReceiveAsync(command, cancellationToken)
                .ConfigureAwait(false);
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

    public async Task<IReadOnlyDictionary<KeyRef, IReadOnlyDictionary<byte, byte>>> GetKeyInformationAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await GetDataAsync(DataObjectKeyInformation, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.IsEmpty)
            return new ReadOnlyDictionary<KeyRef, IReadOnlyDictionary<byte, byte>>(new Dictionary<KeyRef, IReadOnlyDictionary<byte, byte>>(0));

        var keyInformation = new Dictionary<KeyRef, IReadOnlyDictionary<byte, byte>>();

        using var keyTemplates = TlvHelper.Decode(response.Span);
        foreach (var template in keyTemplates)
        {
            if (template.Tag != TagKeyInformationTemplate)
            {
                _logger.LogDebug("Unexpected key information tag 0x{Tag:X2}", template.Tag);
                continue;
            }

            using var innerTlvs = TlvHelper.Decode(template.Value.Span);

            var dataTlv = innerTlvs.FirstOrDefault(candidate => candidate.Tag == TagKeyInformationData);
            if (dataTlv is null)
                throw new BadResponseException("Key information entry missing C0 data template.");

            var payload = dataTlv.Value.Span;
            if (payload.Length < 2)
                throw new BadResponseException("Key information payload shorter than key reference.");

            if ((payload.Length - 2) % 2 != 0)
                throw new BadResponseException("Key information payload has incomplete component pair.");

            var keyRef = new KeyRef(payload[0], payload[1]);
            var components = new Dictionary<byte, byte>((payload.Length - 2) / 2);
            var componentData = payload[2..];
            for (var i = 0; i < componentData.Length; i += 2)
                components[componentData[i]] = componentData[i + 1];

            keyInformation[keyRef] = new ReadOnlyDictionary<byte, byte>(components);
        }

        return new ReadOnlyDictionary<KeyRef, IReadOnlyDictionary<byte, byte>>(keyInformation);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _protocol?.Dispose();
        base.Dispose(true);
    }
}

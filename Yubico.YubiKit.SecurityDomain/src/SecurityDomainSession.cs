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
using Yubico.YubiKit.Core;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
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

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        _protocol?.Dispose();
        base.Dispose(true);
    }
}

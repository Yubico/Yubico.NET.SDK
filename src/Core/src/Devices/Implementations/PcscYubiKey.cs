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
using Yubico.YubiKit.Core.Abstractions;
using Yubico.YubiKit.Core.Transports.SmartCard;

namespace Yubico.YubiKit.Core.Devices;

internal class PcscYubiKey(
    IPcscDevice pcscDevice,
    ISmartCardConnectionFactory connectionFactory,
    ILogger<PcscYubiKey> logger)
    : IYubiKeyConnectionSlot, IDiscoveryConnectionProvider
{
    private readonly string _readerName = pcscDevice.ReaderName;

    private async Task<ISmartCardConnection> CreateConnection(CancellationToken cancellationToken = default)
    {
        var connection = await connectionFactory.CreateAsync(pcscDevice, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Connected to YubiKey in reader {ReaderName}", _readerName);

        return connection;
    }

    public static PcscYubiKey Create(IPcscDevice pcscDevice, ILogger<PcscYubiKey>? logger) => new(pcscDevice,
        SmartCardConnectionFactory.CreateDefault(), logger ?? NullLogger<PcscYubiKey>.Instance);

    public string DeviceId { get; } = $"pcsc:{pcscDevice.ReaderName}";
    public ConnectionType AvailableConnections => ConnectionType.SmartCard;

    public Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
        => ConnectRegisteredAsync<TConnection>(cancellationToken);

    private async Task<TConnection> ConnectRegisteredAsync<TConnection>(CancellationToken cancellationToken)
        where TConnection : class, IConnection
    {
        if (typeof(TConnection) != typeof(ISmartCardConnection))
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");

        // A pre-merge adapter claims only its own interface and releases it on disposal.
        var ownership = await DeviceConnectionRegistry
            .AcquireConnectionAsync(DeviceId, cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var connection = await CreateConnection(cancellationToken).ConfigureAwait(false);
            var registered = new RegisteredSmartCardConnection(connection, ownership);
            return (TConnection)(object)registered;
        }
        catch
        {
            ownership.Dispose();
            throw;
        }
    }

    public async Task<IConnection> OpenRawConnectionAsync(
        ConnectionType connection,
        CancellationToken cancellationToken)
    {
        if (connection != ConnectionType.SmartCard)
            throw new NotSupportedException($"Connection type {connection} is not supported by this YubiKey device.");

        return await CreateConnection(cancellationToken).ConfigureAwait(false);
    }

    Task<IConnection> IDiscoveryConnectionProvider.ConnectForDiscoveryAsync(
        ConnectionType connection,
        CancellationToken cancellationToken) =>
        OpenRawConnectionAsync(connection, cancellationToken);
}
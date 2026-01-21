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
using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Core.YubiKey;

internal class PcscYubiKey(
    IPcscDevice pcscDevice,
    ISmartCardConnectionFactory connectionFactory,
    ILogger<PcscYubiKey> logger)
    : IYubiKey
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

    #region IYubiKey Members

    public string DeviceId { get; } = $"pcsc:{pcscDevice.ReaderName}";
    public ConnectionType ConnectionType => ConnectionType.Smartcard;

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : class, IConnection
    {
        if (typeof(TConnection) != typeof(ISmartCardConnection))
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");

        var connection = await CreateConnection(cancellationToken).ConfigureAwait(false);
        return connection as TConnection ??
               throw new InvalidOperationException("Connection is not of the expected type.");
    }

    #endregion
}
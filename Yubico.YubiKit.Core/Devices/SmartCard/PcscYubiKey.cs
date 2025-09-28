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
using Yubico.YubiKit.Core.Connections;
using Yubico.YubiKit.Core.PlatformInterop.Desktop.SCard;

namespace Yubico.YubiKit.Core.Devices.SmartCard;

internal class PcscYubiKey : IYubiKey
{
    private readonly ISmartCardConnectionFactory _connectionFactory;
    private readonly ILogger<PcscYubiKey> _logger;
    private readonly ISmartCardDevice _pcscDevice;
    private ISmartCardConnection? _connection;

    internal PcscYubiKey(
        ILogger<PcscYubiKey> logger,
        ISmartCardDevice pcscDevice,
        ISmartCardConnectionFactory connectionFactory)
    {
        _logger = logger;
        _pcscDevice = pcscDevice;
        _connectionFactory = connectionFactory;
    }

    #region IYubiKey Members

    public async Task<TConnection> ConnectAsync<TConnection>(CancellationToken cancellationToken = default)
        where TConnection : IConnection
    {
        if (typeof(TConnection) != typeof(ISmartCardConnection))
            throw new NotSupportedException(
                $"Connection type {typeof(TConnection).Name} is not supported by this YubiKey device.");

        _connection = await _connectionFactory.CreateAsync(_pcscDevice);
        return (TConnection)_connection;
    }

    #endregion
}
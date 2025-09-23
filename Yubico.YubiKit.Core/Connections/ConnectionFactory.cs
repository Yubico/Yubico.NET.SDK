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
using Yubico.YubiKit.Core.Devices.SmartCard;

namespace Yubico.YubiKit.Core.Connections;

public interface IConnectionFactory<TConnectionType>
{
    Task<TConnectionType> CreateAsync(ISmartCardDevice smartCardDevice);
}

public class ConnectionFactory<TConnectionType> : IConnectionFactory<TConnectionType>
    where TConnectionType : IYubiKeyConnection
{
    private readonly ILoggerFactory _loggerFactory;

    public ConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    #region IConnectionFactory<TConnectionType> Members

    public async Task<TConnectionType> CreateAsync(ISmartCardDevice smartCardDevice)
    {
        var connectionType = typeof(TConnectionType);
        object connection = connectionType switch
        {
            not null when connectionType == typeof(ISmartCardConnection) =>
                await PcscSmartCardConnection.CreateAsync(
                    _loggerFactory.CreateLogger<PcscSmartCardConnection>(),
                    smartCardDevice
                ),

            _ => throw new NotSupportedException($"The type {typeof(TConnectionType).FullName} is not supported.")
        };

        return (TConnectionType)connection;
    }

    #endregion
}
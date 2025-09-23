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
using Yubico.YubiKit.Core.Core.Devices.SmartCard;

namespace Yubico.YubiKit.Core.Connections;

public interface IConnectionFactory
{
    TConnectionType Create<TConnectionType>(ISmartCardDevice smartCardDevice)
        where TConnectionType : IYubiKeyConnection;
}

public class ConnectionFactory(ILoggerFactory loggerFactory) : IConnectionFactory
{
    #region IConnectionFactory Members

    public TConnectionType Create<TConnectionType>(ISmartCardDevice smartCardDevice)
        where TConnectionType : IYubiKeyConnection
    {
        Type? connectionType = typeof(TConnectionType);
        object connection = connectionType switch
        {
            not null when connectionType == typeof(ISmartCardConnection) =>
                new PcscSmartCardConnection(
                    loggerFactory.CreateLogger<PcscSmartCardConnection>(),
                    smartCardDevice
                ),

            _ => throw new NotSupportedException($"The type {typeof(TConnectionType).FullName} is not supported.")
        };

        return (TConnectionType)connection;
    }

    #endregion
}
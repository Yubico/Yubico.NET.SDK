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

namespace Yubico.YubiKit.Core.SmartCard;

public interface IProtocolFactory<in TConnection>
    where TConnection : IConnection
{
    ISmartCardProtocol Create(TConnection connection);
}

public class PcscProtocolFactory<TConnection>(ILoggerFactory loggerFactory)
    : IProtocolFactory<TConnection>
    where TConnection : IConnection
{
    #region IProtocolFactory<TConnection> Members

    public ISmartCardProtocol Create(TConnection connection) 
    {
        if (connection is not ISmartCardConnection scConnection)
            throw new NotSupportedException(
                $"The connection type {typeof(TConnection).Name} is not supported by this protocol factory.");
        
        return new PcscProtocol(scConnection, logger: loggerFactory.CreateLogger<PcscProtocol>());
    }

    #endregion

    public static PcscProtocolFactory<TConnection> Create(ILoggerFactory? loggerFactory = null) =>
        new(loggerFactory ?? YubiKitLogging.LoggerFactory);
}

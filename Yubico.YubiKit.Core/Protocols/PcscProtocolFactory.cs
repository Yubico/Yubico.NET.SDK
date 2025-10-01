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
using Yubico.YubiKit.Core.Protocols;

namespace Yubico.YubiKit.Core;

public interface IProtocolFactory<TConnection>
    where TConnection : IConnection
{
    IProtocol Create(TConnection connection);
}

public class PcscProtocolFactory<TConnection>(ILoggerFactory loggerFactory)
    : IProtocolFactory<TConnection>
    where TConnection : IConnection
{
    #region IProtocolFactory<TConnection> Members

    public IProtocol Create(TConnection connection) =>
        connection switch
        {
            ISmartCardConnection scConnection =>
                (IProtocol)new PcscProtocol(
                    loggerFactory.CreateLogger<PcscProtocol>(),
                    scConnection),
            _ => throw new NotSupportedException(
                $"The connection type {typeof(TConnection).Name} is not supported by this protocol factory.")
        };

    #endregion
}
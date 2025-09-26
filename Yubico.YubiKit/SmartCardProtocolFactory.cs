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

namespace Yubico.YubiKit;

public interface IProtocolFactory<TConnection, TProtocol>
    where TConnection : IConnection
    where TProtocol : IProtocol
{
    TProtocol Create(TConnection connection);
}

public class SmartCardProtocolFactory<TConnection, TProtocol>(ILoggerFactory loggerFactory)
    : IProtocolFactory<TConnection, TProtocol>
    where TConnection : IConnection
    where TProtocol : IProtocol
{
    #region IProtocolFactory<TConnection,TProtocol> Members

    public TProtocol Create(TConnection connection) => throw new NotImplementedException(); // TODO

    #endregion

    public IProtocol Create(ISmartCardConnection connection) =>
        new SmartCardProtocol(loggerFactory.CreateLogger<SmartCardProtocol>(), connection);
}
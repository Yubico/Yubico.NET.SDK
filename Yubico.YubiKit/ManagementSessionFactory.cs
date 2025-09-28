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

namespace Yubico.YubiKit;

public interface IManagementSessionFactory<TConnection>
    where TConnection : IConnection
{
    ManagementSession<TConnection> Create(TConnection connection);
}

internal class ManagementSessionFactory<TConnection> : IManagementSessionFactory<TConnection>
    where TConnection : IConnection
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IProtocolFactory<TConnection> _protocolFactory;

    public ManagementSessionFactory(
        ILoggerFactory loggerFactory,
        IProtocolFactory<TConnection> protocolFactory)
    {
        _loggerFactory = loggerFactory;
        _protocolFactory = protocolFactory;
    }

    #region IManagementSessionFactory<TConnection> Members

    public ManagementSession<TConnection> Create(TConnection connection) =>
        connection switch
        {
            ISmartCardConnection => new ManagementSession<TConnection>(
                _loggerFactory.CreateLogger<ManagementSession<TConnection>>(),
                connection,
                _protocolFactory),
            _ => throw new NotSupportedException(
                $"The connection type {connection.GetType().FullName} is not supported.")
        };

    #endregion
}
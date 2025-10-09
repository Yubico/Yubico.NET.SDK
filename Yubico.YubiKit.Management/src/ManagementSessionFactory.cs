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
using Yubico.YubiKit.Core.SmartCard;

namespace Yubico.YubiKit.Management;

public interface IManagementSessionFactory<TConnection>
    where TConnection : IConnection
{
    Task<ManagementSession<TConnection>> CreateAsync(TConnection connection);
}

internal class ManagementSessionFactory<TConnection>(
    ILoggerFactory loggerFactory,
    IProtocolFactory<TConnection> protocolFactory) : IManagementSessionFactory<TConnection>
    where TConnection : IConnection
{
    #region IManagementSessionFactory<TConnection> Members

    public Task<ManagementSession<TConnection>> CreateAsync(TConnection connection) =>
        connection switch
        {
            ISmartCardConnection cardConnection => ForSmartCard(cardConnection),
            _ => throw new NotSupportedException(
                $"The connection type {connection.GetType().FullName} is not supported.")
        };

    #endregion

    private async Task<ManagementSession<TConnection>> ForSmartCard(ISmartCardConnection connection)
    {
        var session = new ManagementSession<TConnection>((TConnection)connection,
            protocolFactory, loggerFactory.CreateLogger<ManagementSession<TConnection>>());

        await session.InitializeAsync().ConfigureAwait(false);
        return session;
    }
}
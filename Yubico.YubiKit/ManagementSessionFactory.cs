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

public interface IManagementSessionFactory
{
    ManagementSession Create(IYubiKeyConnection connection);
}

internal class ManagementSessionFactory : IManagementSessionFactory
{
    private readonly ILogger<ManagementSession> _logger;

    public ManagementSessionFactory(ILogger<ManagementSession> logger)
    {
        _logger = logger;
    }

    #region IManagementSessionFactory Members

    public ManagementSession Create(IYubiKeyConnection connection) =>
        connection switch
        {
            ISmartCardConnection smartCardConnection => new ManagementSession(_logger, smartCardConnection),
            _ => throw new NotSupportedException(
                $"The connection type {connection.GetType().FullName} is not supported.")
        };

    #endregion
}
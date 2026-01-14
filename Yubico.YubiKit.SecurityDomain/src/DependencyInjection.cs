// Copyright 2026 Yubico AB
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.SecurityDomain;

public delegate Task<SecurityDomainSession> SecurityDomainSessionFactory(
    ISmartCardConnection connection,
    ScpKeyParameters? scpKeyParams,
    CancellationToken cancellationToken);

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddYubiKeySecurityDomain()
        {
            services.AddSingleton<SecurityDomainSessionFactory>(
                (conn, scp, ct) => SecurityDomainSession.CreateAsync(conn, null, scp, cancellationToken: ct));

            return services;
        }
    }
}

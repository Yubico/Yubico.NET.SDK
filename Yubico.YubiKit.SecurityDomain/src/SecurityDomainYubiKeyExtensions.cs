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

using System.Threading;
using System.Threading.Tasks;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.SecurityDomain;

internal static class SecurityDomainYubiKeyExtensions
{
    internal static async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
        this IYubiKey yubiKey,
        CancellationToken cancellationToken = default)
    {
        var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken).ConfigureAwait(false);
        return await SecurityDomainSession.CreateAsync(connection, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

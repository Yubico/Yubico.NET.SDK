// Copyright 2026 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License");
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

using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;
using Yubico.YubiKit.Core.YubiKey;

namespace Yubico.YubiKit.SecurityDomain;

public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        public async Task<SecurityDomainSession> CreateSecurityDomainSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            FirmwareVersion? firmwareVersion = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken);
            return await SecurityDomainSession.CreateAsync(
                    connection,
                    configuration,
                    scpKeyParams,
                    firmwareVersion,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<KeyReference, IReadOnlyDictionary<byte, byte>>> GetSecurityDomainKeyInfoAsync(
            ScpKeyParameters? scpKeyParams = null,
            CancellationToken cancellationToken = default)
        {
            using var session = await yubiKey.CreateSecurityDomainSessionAsync(
                    scpKeyParams,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await session.GetKeyInformationAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

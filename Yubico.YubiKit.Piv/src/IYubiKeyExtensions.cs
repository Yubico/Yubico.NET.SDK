// Copyright 2024 Yubico AB
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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.Piv;

/// <summary>
/// Extension methods for <see cref="IYubiKey"/> to create PIV sessions.
/// </summary>
public static class IYubiKeyExtensions
{
    /// <summary>
    /// Creates a PIV session with the YubiKey.
    /// </summary>
    /// <param name="yubiKey">The YubiKey device.</param>
    /// <param name="scpKeyParams">Optional SCP key parameters for secure channel.</param>
    /// <param name="configuration">Optional protocol configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized PIV session.</returns>
    /// <exception cref="ArgumentNullException">If yubiKey is null.</exception>
    /// <exception cref="NotSupportedException">If the YubiKey does not support PIV or SmartCard connections.</exception>
    public static async Task<PivSession> CreatePivSessionAsync(
        this IYubiKey yubiKey,
        ScpKeyParameters? scpKeyParams = null,
        ProtocolConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(yubiKey);

        var connection = await yubiKey.ConnectAsync<ISmartCardConnection>(cancellationToken)
            .ConfigureAwait(false);

        return await PivSession.CreateAsync(connection, configuration, scpKeyParams, cancellationToken)
            .ConfigureAwait(false);
    }
}
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

using Yubico.YubiKit.Core.Interfaces;
using Yubico.YubiKit.Core.SmartCard;
using Yubico.YubiKit.Core.SmartCard.Scp;

namespace Yubico.YubiKit.YubiOtp;

/// <summary>
/// Convenience extension methods for YubiOTP operations on <see cref="IYubiKey"/>.
/// </summary>
public static class IYubiKeyExtensions
{
    extension(IYubiKey yubiKey)
    {
        /// <summary>
        /// Gets the OTP slot configuration state from the YubiKey.
        /// Creates a session, queries state, and disposes automatically.
        /// </summary>
        public async Task<ConfigState> GetConfigStateAsync(CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return session.GetConfigState();
        }

        /// <summary>
        /// Writes a slot configuration to the YubiKey.
        /// Creates a session, writes the configuration, and disposes automatically.
        /// </summary>
        public async ValueTask PutConfigurationAsync(
            Slot slot,
            SlotConfiguration config,
            ReadOnlyMemory<byte> accessCode = default,
            ReadOnlyMemory<byte> currentAccessCode = default,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await session.PutConfigurationAsync(slot, config, accessCode, currentAccessCode, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Performs an HMAC-SHA1 challenge-response operation.
        /// Creates a session, computes the response, and disposes automatically.
        /// </summary>
        public async Task<ReadOnlyMemory<byte>> CalculateHmacSha1Async(
            Slot slot,
            ReadOnlyMemory<byte> challenge,
            CancellationToken cancellationToken = default)
        {
            await using var session = await yubiKey
                .CreateYubiOtpSessionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return await session.CalculateHmacSha1Async(slot, challenge, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a YubiOTP session for the device. The caller owns the session lifetime.
        /// </summary>
        public async Task<YubiOtpSession> CreateYubiOtpSessionAsync(
            ScpKeyParameters? scpKeyParams = null,
            ProtocolConfiguration? configuration = null,
            CancellationToken cancellationToken = default)
        {
            var connection = await yubiKey.ConnectAsync(cancellationToken).ConfigureAwait(false);
            return await YubiOtpSession.CreateAsync(
                    connection,
                    configuration,
                    scpKeyParams,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
